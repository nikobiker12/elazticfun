#load "..\shared\datamodel.csx"
#load "..\shared\collectionsutils.csx"

#r "Microsoft.ServiceBus"
#r "System.Runtime.Serialization"

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Runtime.Serialization;
using System.Collections.Generic;

public static async Task Run(SimulationRequest simulationRequest, TraceWriter log)
{
    log.Info($"C# ServiceBus queue trigger function processed message: " + simulationRequest.Pricing.Id
        + ", " + simulationRequest.SimulationId + " for " + simulationRequest.PathsCount + " paths");

    var paths = Enumerable.Range(simulationRequest.PathsCount * simulationRequest.SimulationId, simulationRequest.PathsCount)
        .Select(pathId => GeneratePath(pathId, simulationRequest.Pricing, log));

    log.Info($"Sending path batch to ServiceBus: " + simulationRequest.Pricing.Id);

    await SendMessagesAsync(simulationRequest.SimulationId, paths, simulationRequest.Pricing, log);
}

public static async Task SendMessagesAsync(int pathBatchId, IEnumerable<Path> paths, PricingParameters pricingParameters, TraceWriter log)
{
    var connectionString = Environment.GetEnvironmentVariable("pricinglpmc_RootManageSharedAccessKey_SERVICEBUS") + ";EntityPath=path-payoff";
    QueueClient queueClient = QueueClient.CreateFromConnectionString(connectionString);

    log.Info($"Chunk paths batch to max size {MaxServiceBusMessage}");

    var chunks = paths.ChunkBy(x => new BrokeredMessage(x).Size, MaxServiceBusMessage);
    var messages = chunks.Select(chunk => new PathBatch { PathBatchId = chunk.First().PathId, PricingParameters = pricingParameters, Paths = chunk })
        .ToList();

    foreach(var mess in messages.Where(m => m != null && m.Paths != null))
    {
        log.Info($"Sending path batch to ServiceBus (count = {mess.Paths.Count}, size = {new BrokeredMessage(mess).Size})");
        await queueClient.SendAsync(new BrokeredMessage(mess));
    }
}

public static Path GeneratePath(int pathId, PricingParameters pricing, TraceWriter log)
{
    PathGenerator pathGenerator = new PathGenerator(pathId, pricing.Volatility, pricing.Maturity);

    const double dt = 1.0 / 12;
    var list = Enumerable.Range(1, 1000)
        .Select(i => i * dt)
        .TakeWhile(t => t < pricing.Maturity + dt)
        .Aggregate(new List<MarketState>() { new MarketState { T = 0, S = pricing.Spot } }, pathGenerator.Aggregator);

    return new Path { PathId = pathId, States = list.ToList() };
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
        states.Add(Next(states[states.Count - 1], t));
        return states;
    }

    public MarketState Next(MarketState inState, double t)
    {
        MarketState outState = new MarketState();
        outState.T = Math.Min(t, _maturity);
        double dt = outState.T - inState.T;
        double dLogS = -.5 * _volatility * _volatility * dt + _volatility * NextGaussian(dt);
        outState.S = inState.S * Math.Exp(dLogS);
        return outState;
    }

    double NextGaussian(double dt)
    {
        double u1 = 1.0 - _random.NextDouble(); //uniform(0,1] random doubles
        double u2 = 1.0 - _random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        double randNormal = Math.Sqrt(dt) * randStdNormal; //random normal(0,dt^2)
        return randNormal;
    }
}

