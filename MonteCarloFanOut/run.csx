#r "Microsoft.ServiceBus"
#r "System.Runtime.Serialization"

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Runtime.Serialization;

public static async Task Run(
    PricingParameters pricingRequest,
    TraceWriter log)
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

[DataContract]
public class PricingParameters {
    [DataMember]
    public string Id {get; set;}
    [DataMember]
    public double Strike {get; set;}
    [DataMember]
    public double Maturity {get; set;}
    [DataMember]
    public double Spot {get; set;}
    [DataMember]
    public double Volatility{get; set;}
    [DataMember]
    public int SimulationCount{get; set;}
}

[DataContract]
public class PathGeneration {
    [DataMember]
    public PricingParameters Pricing {get; set;}
    [DataMember]
    public int SimulationId {get; set;}
}

public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, Func<T, long> metric, long maxChunkSize)
{
    return source
        .Aggregate(
            new
            {
                Sum = 0L,
                Current = (List<T>)null,
                Result = new List<List<T>>()
            },
            (agg, item) =>
            {
                var value = metric(item);
                if (agg.Current == null || agg.Sum + value > maxChunkSize)
                {
                    var current = new List<T> { item };
                    agg.Result.Add(current);
                    return new { Sum = value, Current = current, agg.Result };
                }

                agg.Current.Add(item);
                return new { Sum = agg.Sum + value, agg.Current, agg.Result };
            })
        .Result;
}

private const long MaxServiceBusMessage = 256000;
