#r "Microsoft.ServiceBus"
#r "System.Runtime.Serialization"

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Runtime.Serialization;
using System.Collections.Generic;

public static async Task Run(PathGeneration pathGenerationBatch, TraceWriter log)
{
    log.Info($"C# ServiceBus queue trigger function processed message: " + pathGenerationBatch.Pricing.Id
        + ", " + pathGenerationBatch.SimulationId);

    int batchSize = Math.Min(1000,pathGenerationBatch.Pricing.SimulationCount-1000*pathGenerationBatch.SimulationId);
    var paths = Enumerable.Range(1000*pathGenerationBatch.SimulationId, batchSize)
        .Select(i => new PathGeneration {
            Pricing = pathGenerationBatch.Pricing,
            SimulationId = i})
        .Select(p => GeneratePath(p, log));

    log.Info($"Sending path batch to ServiceBus: " + pathGenerationBatch.Pricing.Id);
 
    await SendMessagesAsync(batch);
}

public static async Task SendMessagesAsync(IEnumerable<Path> paths, PricingParameters pricingParameters)
{
    var connectionString = Environment.GetEnvironmentVariable("pricinglpmc_RootManageSharedAccessKey_SERVICEBUS")+";EntityPath=path-generation";
    QueueClient queueClient = QueueClient.CreateFromConnectionString(connectionString);

    var chunks = paths.ChunkBy(x => 512, MaxServiceBusMessage);
    var messages = chunks.Select(chunk => new PathBatch { PricingParameters = pricingParameters, Paths = chunk })
        .Select(batch => new BrokeredMessage(batch))
        .ToList();

    await queueClient.SendBatchAsync(messages);
}

public static Path GeneratePath(PathGeneration pathGeneration, TraceWriter log)
{
    PathGenerator pathGenerator = new PathGenerator(
        pathGeneration.SimulationId,
        pathGeneration.Pricing.Volatility,
        pathGeneration.Pricing.Maturity);


    const double dt = 1.0/12;
    var list = Enumerable.Range(1,1000)
        .Select(i => i*dt)
        .TakeWhile(t => t<pathGeneration.Pricing.Maturity+dt)
        .Aggregate(new List<MarketState>() {new MarketState{T=0,S=pathGeneration.Pricing.Spot}}, pathGenerator.Aggregator);
    
    return new Path {SimulationId =  pathGeneration.SimulationId, States = list};
}

public class PathGenerator
{
    Random _random;
    double _volatility;
    double _maturity;

    public PathGenerator(int seed, double volatility, double maturity)
    {
        _random = new Random(seed);
        _volatility = volatility;
        _maturity = maturity;
    }

    public List<MarketState> Aggregator(List<MarketState> states, double t)
    {
        states.Add(Next(states[states.Count - 1], t))
        return states;
    }

    public MarketState Next(MarketState inState, double t)
    {
        MarketState outState = new MarketState();
        outState.T = Math.Min(t,_maturity);
        double dt = outState.T - inState.T;
        double dLogS = -.5*_volatility*_volatility*dt + _volatility*NextGaussian(dt);
        outState.S = inState.S * Math.Exp(dLogS);
        return outState;
    }

    double NextGaussian(double dt)
    {
        double u1 = 1.0-_random.NextDouble(); //uniform(0,1] random doubles
        double u2 = 1.0-_random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        double randNormal = Math.Sqrt(dt) * randStdNormal; //random normal(0,dt^2)
        return randNormal;
    }
}

public class MarketState
{
    public double T {get; set;}
    public double S {get; set;}
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

[DataContract]
public class PathAndOption
{
    [DataMember]
    public string PricingId {get; set;}
    [DataMember]
    public int SimulationId {get; set;}
    [DataMember]
    public int SimulationCount {get; set;}
    [DataMember]
    public double Spot {get; set;}
    [DataMember]
    public double Strike {get; set;}
}

[DataContract]
public class Path
{
    [DataMember]
    public int SimulationId {get; set;}
    [DataMember]
    public List<MarketState> {get; set;}
}

[DataContract]
public class PathBatch
{
    [DataMember]
    public List<Path> Paths {get;set;}
    
    [DataMember]
    public PricingParameters PricingParameters {get;set;}
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
