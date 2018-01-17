#load "..\shared\datamodel.csx"
#load "..\shared\PathGenerationUtils.csx"
#load "..\shared\QueueClientExtensions.csx"

#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;

using Microsoft.WindowsAzure.Storage.Table;

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
        PublishResult(payoffsList, simulationRequest);
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

public static void PublishResult(List<double> payoffList, SimulationRequest simulationRequest)
{
    //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

    //// Create the table client.
    //CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

    //TableOperation retrieveOperation = TableOperation.Retrieve(simulationRequest.RequestId, simulationRequest.SimulationId.ToString());
    //TableResult retrievedResult = table.Execute(retrieveOperation);
    //TableEntity entity = (PricingResult) retrievedResult.Result;
}