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
    public int PathId { get; set; }
    [DataMember]
    public List<double> Spots { get; set; }
}

[DataContract]
public class PathBatch
{
    [DataMember]
    public int PathBatchId { get; set; }

    [DataMember]
    public List<double> Times { get; set; }

    [DataMember]
    public List<Path> Paths { get; set; }

    [DataMember]
    public PricingParameters PricingParameters { get; set; }
}


[DataContract]
public class SimulationRequest
{
    [DataMember]
    public PricingParameters Pricing { get; set; }
    [DataMember]
    public int SimulationId { get; set; }
    [DataMember]
    public int PathsCount { get; set; }
}


public class PricingResult
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public double PriceSum { get; set; }
    public int PathsCount { get; set; }
    public int TotalPathsCount { get; set; }
}


