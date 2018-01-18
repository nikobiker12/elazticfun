#load "..\shared\Constants.csx"
#load "..\shared\datamodel.csx"
#load "..\shared\PathGenerationUtils.csx"
#load "..\shared\QueueClientExtensions.csx"

#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task Run(SimulationRequest simulationRequest, TraceWriter log)
{
    log.Info($"Function 'PathGenerationBatch' triggered. Processing simulation : {simulationRequest.RequestId} "
        + $", {simulationRequest.SimulationId} ({simulationRequest.Spot}, {simulationRequest.Volatility}) for {simulationRequest.SimulationCount} paths");

    int batchSize = Convert.ToInt32(Environment.GetEnvironmentVariable("SimulationBatchSize"));

    var timesPoints = GenerateTimePoints(simulationRequest, log);
    var pathBatchLists = Enumerable.Range(0, simulationRequest.SimulationCount)
        .Select(pathId => GeneratePath(pathId, simulationRequest, timesPoints, log))
        .Chunk(batchSize)
        .Select(paths => new PathBatch { SimulationRequest = simulationRequest, Paths = paths.ToList(), Times = timesPoints });

    Func<PathBatch, Task<List<double>>> payoffSumFunc;
    switch (simulationRequest.PayoffName)
    {
        case "vanilla-call":
            payoffSumFunc = VanillaCallPayoff;
            break;
        default:
            payoffSumFunc = CustomHttpPayOff;
            break;
    }

    foreach (var pathBatch in pathBatchLists)
    {
        var payoffsList = await payoffSumFunc(pathBatch);
        PublishPricingResults(payoffsList, pathBatch, log);
    }
}

public static Task<List<double>> VanillaCallPayoff(PathBatch pathBatch)
{
    return Task.FromResult(pathBatch.Paths.Select(path =>
    {
        var lastSpot = path.Spots[path.Spots.Count - 1];
        return Math.Max(lastSpot - pathBatch.SimulationRequest.Strike, 0);
    }).ToList());
}

public static Task<List<double>> CustomHttpPayOff(PathBatch pathBatch)
{
    return null;
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

public static void PublishPricingResults(List<double> payoffList, PathBatch pathBatch, TraceWriter log)
{
    var storage = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
    var tableClient = storage.CreateCloudTableClient();
    var table = tableClient.GetTableReference(PRICING_RESULTS_TABLE);

    // https://azure.microsoft.com/en-us/blog/managing-concurrency-in-microsoft-azure-storage-2/
    const int MaxRetries = 5;
    for (int i = 0; i < MaxRetries; ++i)
    {
        try
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<PricingResult>(pathBatch.SimulationRequest.RequestId, pathBatch.SimulationRequest.SimulationId.ToString());
            TableResult retrievedResult = table.Execute(retrieveOperation);
            PricingResult pricingResult = (PricingResult) retrievedResult.Result;
            pricingResult.IndicatorSum += payoffList.Sum();
            pricingResult.PathsSum += payoffList.Count;


            TableOperation replaceOperation = TableOperation.Replace(pricingResult);
            TableResult replaceResult = table.Execute(replaceOperation);

            break;
        }
        catch (StorageException ex)
        {
            if (ex.RequestInformation.HttpStatusCode == 412)
                log.Warning("Optimistic concurrency violation � entity has changed since it was retrieved.");
            else
                throw;
        }
    }
}