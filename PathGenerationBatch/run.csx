#load "..\shared\datamodel.csx"
#load "..\shared\PathGenerationUtils.csx"
#load "..\shared\QueueClientExtensions.csx"

#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;

public static async Task Run(SimulationRequest simulationRequest, TraceWriter log)
{
    log.Info($"C# ServiceBus queue trigger function processed message: " + simulationRequest.RequestId
        + ", " + simulationRequest.SimulationId + " for " + simulationRequest.SimulationCount + " paths");

    int batchSize = Convert.ToInt32(Environment.GetEnvironmentVariable("SimulationBatchSize"));

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
        PublishResult(payoffsList, simulationRequest);
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
                    JArray prices = JArray.Parse(response);
                    return prices.Values<double>();
                }
            }
        }
    }
    catch (Exception e)
    {
        log.Info($"Cannot get response from patch batch id {pathBatch.SimulationRequest.SimulationId} : {e}");
    }
    return Enumerable.Repeat(0.0, pathBatch.Paths.Count);
}

public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int batchSize)
{
    if (source == null)
        return new List<List<T>>();
    if (source.Count() < batchSize)
        return new List<List<T>> { new List<T>(source) };

    return source
        .Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / batchSize)
        .Select(x => x.Select(v => v.Value));
}

public static void PublishResult(IEnumerable<double> payoffList, SimulationRequest simulationRequest)
{
    //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

    //// Create the table client.
    //CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

    //TableOperation retrieveOperation = TableOperation.Retrieve(simulationRequest.RequestId, simulationRequest.SimulationId.ToString());
    //TableResult retrievedResult = table.Execute(retrieveOperation);
    //TableEntity entity = (PricingResult) retrievedResult.Result;
}