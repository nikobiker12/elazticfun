#load "..\shared\datamodel.csx"
#load "..\shared\QueueClientExtensions.csx"

#r "Microsoft.ServiceBus"

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

public static async Task Run(PricingParameters pricingRequest, TraceWriter log)
{
    log.Info($"MonteCarloFanOut function start: " + pricingRequest);

     var messages = GenrerateSimulationRequests(pricingRequest, log)
        .Select(p => new BrokeredMessage(p));

    var connectionString = Environment.GetEnvironmentVariable("pricinglpmc_RootManageSharedAccessKey_SERVICEBUS") + ";EntityPath=path-generation";
    QueueClient queueClient = QueueClient.CreateFromConnectionString(connectionString);
    await SendPartitionedBatchAsync(queueClient, messages, false);
}

public static List<SimulationRequest> GenrerateSimulationRequests(PricingParameters pricingRequest, TraceWriter log)
{
    return Enumerable.Range(-pricingRequest.SpotBumpCount, pricingRequest.SpotBumpCount)
        .SelectMany(p => Enumerable.Range(-pricingRequest.VolBumpCount, pricingRequest.VolBumpCount),
        (spotOffset, volOffset) => (pricingRequest.Spot * (1 + spotOffset * pricingRequest.SpotBumpSize),
                                    pricingRequest.Volatility * (1 + volOffset * pricingRequest.VolBumpSize)))
        .Select((t, i) => new SimulationRequest
        {
                    RequestId = pricingRequest.Id,
                    SimulationId = i,
                    OptionType = pricingRequest.OptionType,
                    PayoffName = pricingRequest.PayoffName,
                    Maturity = pricingRequest.Maturity,
                    Spot = t.Item1,
                    Strike = pricingRequest.Strike,
                    Volatility = t.Item2,
                    SimulationCount = pricingRequest.SimulationCount
                   
                })
        .ToList();
}

