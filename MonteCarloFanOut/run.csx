#load "..\shared\datamodel.csx"
#load "..\shared\collectionsutils.csx"

#r "Microsoft.ServiceBus"
#r "System.Runtime.Serialization"

using System;
using System.Runtime.Serialization;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

public static async Task Run(PricingParameters pricingRequest, TraceWriter log)
{
    log.Info($"MonteCarloFanOut function start: " + pricingRequest);

    var connectionString = Environment.GetEnvironmentVariable("pricinglpmc_RootManageSharedAccessKey_SERVICEBUS")+";EntityPath=path-generation";
    QueueClient queueClient = QueueClient.CreateFromConnectionString(connectionString);

    int batchSize = Convert.ToInt32(Environment.GetEnvironmentVariable("SimulationBatchSize"));
    var batchCount = (pricingRequest.SimulationCount + batchSize - 1) / batchSize;

    var paths = Enumerable.Range(0, batchCount)
        .Select(i => new SimulationRequest
                    {
                        Pricing = pricingRequest,
                        SimulationId = i,
                        PathsCount = batchSize
        })
        .Select(p => new BrokeredMessage(p));

    var chunks = paths.ChunkBy(x => 300, MaxServiceBusMessage);
    foreach (var chunk in chunks)
    {
        await queueClient.SendBatchAsync(chunk);
    }
}

