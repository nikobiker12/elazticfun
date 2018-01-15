using System;

public static void Run(PathAndOption pathOption, TraceWriter log)
{
    log.Info($"PayoffCall function processed: " + pathOption.PricingId + ", path " 
        + pathOption.SimulationId + " / " + pathOption.SimulationCount);

    double payoff = Math.Max(pathOption.Spot - pathOption.Strike,0);

    log.Info($"PayoffCall function result: " + payoff);
}

public class PathAndOption
{
    public string PricingId {get; set;}
    public int SimulationId {get; set;}
    public int SimulationCount {get; set;}
    public double Spot {get; set;}
    public double Strike {get; set;}
}