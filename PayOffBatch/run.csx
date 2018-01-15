using System;
using System.Linq;
using System.Collections.Generic;

public static void Run(PathBatch pathBatch, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {pathBatch}");

    var price = pathBatch.Paths.Select(p => VanillaCallPayoff(p.States, pathBatch.PricingParameters))
        .Sum();

    // Write sum and count in azure table
}

public double VanillaCallPayoff(List<MarketState> states, PricingParameters pricingParameters)
{
    var lastSpot = p.States[p.States.Count - 1].Spot;
    return Math.Max(lastSpot - pricingParameters.Strike, 0);
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
