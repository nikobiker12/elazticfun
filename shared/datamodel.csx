#r "System.Runtime.Serialization"

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

[DataContract]
public class PricingParameters
{
    [DataMember]
    public string Id { get; set; }
    [DataMember]
    public string PayoffName { get; set; }
    [DataMember]
    public double Strike { get; set; }
    [DataMember]
    public double Maturity { get; set; }
    [DataMember]
    public double Spot { get; set; }
    [DataMember]
    public double Volatility { get; set; }
    [DataMember]
    public int SimulationCount { get; set; }
}


[DataContract]
public class MarketState
{
    [DataMember]
    public double T { get; set; }
    [DataMember]
    public double S { get; set; }
}

[DataContract]
public class Path
{
    [DataMember]
    public int SimulationId { get; set; }
    [DataMember]
    public List<MarketState> States { get; set; }
}

[DataContract]
public class PathBatch
{
    [DataMember]
    public List<Path> Paths { get; set; }

    [DataMember]
    public PricingParameters PricingParameters { get; set; }
}



[DataContract]
public class PathGeneration
{
    [DataMember]
    public PricingParameters Pricing { get; set; }
    [DataMember]
    public int SimulationId { get; set; }
}

[DataContract]
public class PathAndOption
{
    [DataMember]
    public string PricingId { get; set; }
    [DataMember]
    public int SimulationId { get; set; }
    [DataMember]
    public int SimulationCount { get; set; }
    [DataMember]
    public double Spot { get; set; }
    [DataMember]
    public double Strike { get; set; }
}

