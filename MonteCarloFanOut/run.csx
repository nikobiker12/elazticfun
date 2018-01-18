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
    log.Info($"MonteCarloFanOut function start: " + pricingRequest);

    var simulationRequestsList = GenrerateSimulationRequests(pricingRequest, log);

    InitializePricingResults(simulationRequestsList);

    var connectionString = Environment.GetEnvironmentVariable(AZURESERVICEBUS_CONNECTIONSTRING_KEY) + ";EntityPath=path-generation";
    QueueClient queueClient = QueueClient.CreateFromConnectionString(connectionString);

    var messagesList = simulationRequestsList.Select(sr => new BrokeredMessage(sr));
    await SendPartitionedBatchAsync(queueClient, messagesList, false);
}

public static List<SimulationRequest> GenrerateSimulationRequests(PricingParameters pricingRequest, TraceWriter log)
{
    return Enumerable.Range(-pricingRequest.SpotBumpCount, pricingRequest.SpotBumpCount)
        .SelectMany(p => Enumerable.Range(-pricingRequest.VolBumpCount, pricingRequest.VolBumpCount),
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

    var batchOp = simulationRequestsList.Select(sr => new PricingResult
    {
        PartitionKey = sr.RequestId,
        RowKey = sr.SimulationId.ToString(),
        IndicatorSum = 0.0,
        PathsSum = 0,
        TotalPathsCount = sr.SimulationCount
    })
    .Aggregate(new TableBatchOperation(), (batchOpAgg, pr) => { batchOpAgg.Add(TableOperation.Insert(pr)); return batchOpAgg; });

    var result = table.ExecuteBatch(batchOp);
}
