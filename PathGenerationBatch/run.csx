#load "..\shared\Constants.csx"
#load "..\shared\datamodel.csx"
#load "..\shared\PathGenerationUtils.csx"
#load "..\shared\CollectionsExtensions.csx"

#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

public static async Task Run(SimulationRequest simulationRequest, TraceWriter log)
{
    log.Info("ServiceBus queue trigger function 'PathGenerationBatch'.");
    log.Info($"Processing simulation : {simulationRequest.RequestId}, {simulationRequest.SimulationId} for {simulationRequest.SimulationCount} paths.");

    int batchSize = Convert.ToInt32(Environment.GetEnvironmentVariable(SIMULATIONBATCHSIZE_KEY));

    var timesPoints = GenerateTimePoints(simulationRequest, log);
    var pathBatchLists = Enumerable.Range(0, simulationRequest.SimulationCount)
        .Select(pathId => GeneratePath(pathId, simulationRequest, timesPoints, log))
        .Chunk(batchSize)
        .Select(paths => new PathBatch { SimulationRequest = simulationRequest, Paths = paths.ToList(), Times = timesPoints });

    Func<PathBatch, TraceWriter, Task<IEnumerable<double>>> payoffSumFunc;
    switch (simulationRequest.PayoffName)
    {
        case "vanilla":
            payoffSumFunc = VanillaPayoff;
            break;
        default:
            payoffSumFunc = CustomHttpPayOff;
            break;
    }

    foreach (var pathBatch in pathBatchLists)
    {
        var payoffsList = await payoffSumFunc(pathBatch, log);
        PublishPricingResults(payoffsList, pathBatch, log);
    }
}

public static Task<IEnumerable<double>> VanillaPayoff(PathBatch pathBatch, TraceWriter log)
{
    return Task.FromResult(pathBatch.Paths.Select(path =>
    {
        var lastSpot = path.Spots[path.Spots.Count - 1];
        int direction = (int)pathBatch.SimulationRequest.OptionType;
        return Math.Max(direction * (lastSpot - pathBatch.SimulationRequest.Strike), 0);
    }));
}

public static async Task<IEnumerable<double>> CustomHttpPayOff(PathBatch pathBatch, TraceWriter log)
{
    dynamic json = new JObject();
    json.Paths = new JArray(pathBatch.Paths.Select(path => new JArray(path.Spots.ToArray())));
    json.Direction = (int)pathBatch.SimulationRequest.OptionType;
    json.Strike = pathBatch.SimulationRequest.Strike;
    string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
    log.Info($"JSON sent to custom script = {jsonString}");

    string url = System.Environment.GetEnvironmentVariable($"PAYOFFMETHODURI_{pathBatch.SimulationRequest.PayoffName}");
    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
    request.Method = "POST";
    request.ContentType = "application/json";
    request.ContentLength = jsonString.Length;
    using (Stream webStream = await request.GetRequestStreamAsync())
    using (StreamWriter requestWriter = new StreamWriter(webStream, System.Text.Encoding.ASCII))
    {
        await requestWriter.WriteAsync(jsonString);
    }

    try
    {
        using (WebResponse webResponse = await request.GetResponseAsync())
        using (Stream webStream = webResponse.GetResponseStream())
        {
            if (webStream != null)
            {
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = await responseReader.ReadToEndAsync();
                    log.Info($"JSON received from custom script = {response}");
                    JArray prices = JArray.Parse(response);
                    return prices.Values<double>();
                }
            }
        }
    }
    catch (Exception e)
    {
        log.Warning($"Cannot get response from patch batch id {pathBatch.SimulationRequest.SimulationId} : {e}");
    }
    return Enumerable.Repeat(0.0, pathBatch.Paths.Count);
}

public static void PublishPricingResults(IEnumerable<double> payoffList, PathBatch pathBatch, TraceWriter log)
{
    var storage = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable(AZUREWEBJOBSSTORAGE_CONNECTIONSTRING_KEY));
    var tableClient = storage.CreateCloudTableClient();
    var table = tableClient.GetTableReference(PRICINGRESULTS_TABLE);

    // https://azure.microsoft.com/en-us/blog/managing-concurrency-in-microsoft-azure-storage-2/
    const int MaxRetries = 5;
    for (int i = 0; i < MaxRetries; ++i)
    {
        try
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<PricingResultEntity>(pathBatch.SimulationRequest.RequestId, pathBatch.SimulationRequest.SimulationId.ToString());
            TableResult retrievedResult = table.Execute(retrieveOperation);
            PricingResultEntity pricingResult = (PricingResultEntity) retrievedResult.Result;
            pricingResult.IndicatorSum += payoffList.Sum();
            pricingResult.PathsSum += payoffList.Count();

            TableOperation replaceOperation = TableOperation.Replace(pricingResult);
            TableResult replaceResult = table.Execute(replaceOperation);

            break;
        }
        catch (StorageException ex)
        {
            if (ex.RequestInformation.HttpStatusCode == 412)
                log.Warning("Optimistic concurrency violation ! Entity has changed since it was retrieved.");
            else
                throw;
        }
    }
}