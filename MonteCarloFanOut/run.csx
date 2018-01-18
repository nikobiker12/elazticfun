#load "..\shared\Constants.csx"
#load "..\shared\datamodel.csx"
#load "..\shared\QueueClientExtensions.csx"

#r "Microsoft.ServiceBus"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task Run(PricingParameters pricingRequest, TraceWriter log)
{
    log.Info($"Function 'MonteCarloFanOut' triggered. SimulationCount: {pricingRequest.SimulationCount}, " +
        $"SpotBumpSize: {pricingRequest.SpotBumpSize}, SpotBumpCount:{pricingRequest.SpotBumpCount}, VolBumpSize:{pricingRequest.VolBumpSize}, VolBumpCount:{pricingRequest.VolBumpCount}");

    var simulationRequestsList = GenrerateSimulationRequests(pricingRequest, log);

    InitializePricingResults(simulationRequestsList);

    var splittedSimulationRequestsList = SplitSimulationRequestsList(simulationRequestsList, pricingRequest);

    var connectionString = Environment.GetEnvironmentVariable(AZURESERVICEBUS_CONNECTIONSTRING_KEY) + ";EntityPath=path-generation";
    QueueClient queueClient = QueueClient.CreateFromConnectionString(connectionString);

    var messagesList = splittedSimulationRequestsList.Select(sr => new BrokeredMessage(sr));
    await SendPartitionedBatchAsync(queueClient, messagesList, false);
}

public static List<SimulationRequest> GenrerateSimulationRequests(PricingParameters pricingRequest, TraceWriter log)
{
    return Enumerable.Range(-pricingRequest.SpotBumpCount, 2 * pricingRequest.SpotBumpCount + 1)
        .SelectMany(p => Enumerable.Range(-pricingRequest.VolBumpCount, 2 * pricingRequest.VolBumpCount + 1),
        (spotOffset, volOffset) => (BumpedSpot: pricingRequest.Spot * (1 + spotOffset * pricingRequest.SpotBumpSize),
                                    BumpedVol: pricingRequest.Volatility * (1 + volOffset * pricingRequest.VolBumpSize)))
        .Select((t, i) => new SimulationRequest
        {
            RequestId = pricingRequest.Id,
            SimulationId = i,
            OptionType = pricingRequest.OptionType,
            PayoffName = pricingRequest.PayoffName,
            Maturity = pricingRequest.Maturity,
            Spot = t.BumpedSpot,
            Strike = pricingRequest.Strike,
            Volatility = t.BumpedVol,
            SimulationCount = pricingRequest.SimulationCount
        })
        .ToList();
}

public static void InitializePricingResults(List<SimulationRequest> simulationRequestsList)
{
    var storage = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable(AZUREWEBJOBSSTORAGE_CONNECTIONSTRING_KEY));
    var tableClient = storage.CreateCloudTableClient();
    var table = tableClient.GetTableReference(PRICINGRESULTS_TABLE);

    table.CreateIfNotExists();

    var batchOp = simulationRequestsList.Select(sr => new PricingResultEntity
    {
        PartitionKey = sr.RequestId,
        RowKey = sr.SimulationId.ToString(),
        Spot = sr.Spot,
        Volatility = sr.Volatility,
        IndicatorSum = 0.0,
        PathsSum = 0,
        TotalPathsCount = sr.SimulationCount
    })
    .Aggregate(new TableBatchOperation(), (batchOpAgg, pr) => { batchOpAgg.Add(TableOperation.Insert(pr)); return batchOpAgg; });

    var result = table.ExecuteBatch(batchOp);
}

public static IEnumerable<SimulationRequest> SplitSimulationRequestsList(IEnumerable<SimulationRequest> simulationRequestsList, PricingParameters pricingRequest)
{
    int batchSize = Convert.ToInt32(Environment.GetEnvironmentVariable(SIMULATIONBATCHSIZE_KEY));
    int batchCount = (pricingRequest.SimulationCount + batchSize - 1) / batchSize;

    var splittedList = simulationRequestsList.SelectMany(sr => Enumerable.Range(0, batchCount),
        (sr, batchIndex) => new SimulationRequest
        {
            RequestId = sr.RequestId,
            SimulationId = sr.SimulationId,
            BatchStartIndex = batchIndex*batchSize,
            BatchPatchsCount = Math.Min(batchSize, pricingRequest.SimulationCount - batchIndex*batchSize),
            OptionType = sr.OptionType,
            PayoffName = sr.PayoffName,
            Strike = sr.Strike,
            Maturity = sr.Maturity,
            Spot = sr.Spot,
            Volatility = sr.Volatility,
            SimulationCount = sr.SimulationCount,
        }
    );

    return splittedList;
}