#r "System.Runtime.Serialization"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

[DataContract]
public class PricingParameters
{
    public enum EOptionType { call = 1, put = -1 }
    [DataMember]
    public string Id { get; set; }
    [DataMember]
    public EOptionType OptionType { get; set; }
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
    [DataMember]
    public int SpotBumpCount { get; set; }
    [DataMember]
    public int VolBumpCount { get; set; }
    [DataMember]
    public double SpotBumpSize { get; set; }
    [DataMember]
    public double VolBumpSize { get; set; }
}


public class SimulationRequest
{
    [DataMember]
    public string RequestId { get; set; }
    [DataMember]
    public int SimulationId { get; set; }
    [DataMember]
    public PricingParameters.EOptionType OptionType { get; set; }
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
    public List<double> Times { get; set; }

    [DataMember]
    public List<Path> Paths { get; set; }

    [DataMember]
    public SimulationRequest SimulationRequest { get; set; }
}


public class PricingResultEntity : TableEntity
{
    public double Spot { get; set; }
    public double Volatility { get; set; }
    public double IndicatorSum { get; set; }
    public int PathsSum { get; set; }
    public int TotalPathsCount { get; set; }
}

[DataContract]
public class PricingRequestErrorResponse
{
    public PricingRequestErrorResponse(string error)
    {
        Error = error;
    }

    [DataMember]
    public string Error { get; set; }
}