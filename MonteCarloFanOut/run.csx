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

    var paths = Enumerable.Range(0,(pricingRequest.SimulationCount+999)/1000)
        .Select(i => new PathGeneration{
            Pricing = pricingRequest,
            SimulationId = i})
        .Select(p => new BrokeredMessage(p));

    var chunks = paths.ChunkBy(x => 500, MaxServiceBusMessage);
    foreach (var chunk in chunks)
    {
        await queueClient.SendBatchAsync(chunk);
    }
}

