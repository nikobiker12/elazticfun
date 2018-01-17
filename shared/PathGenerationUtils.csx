#load "..\shared\datamodel.csx"


using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Collections.Generic;


public class MarketState
{
    public double T { get; set; }
    public double S { get; set; }
}

public static List<double> GenerateTimePoints(SimulationRequest simulationRequest, TraceWriter log)
{
    const double dt = 1.0 / 12;
    var list = Enumerable.Range(1, 1000)
        .Select(i => i * dt)
        .TakeWhile(t => t < simulationRequest.Maturity + dt)
        .ToList();

    return list;
}

public static Path GeneratePath(int pathId, SimulationRequest simulationRequest, List<double> timePoints, TraceWriter log)
{
    PathGenerator pathGenerator = new PathGenerator(pathId, simulationRequest.Volatility, simulationRequest.Maturity);

    var spots = timePoints
        .Aggregate(new List<MarketState>() { new MarketState { T = 0, S = simulationRequest.Spot } }, pathGenerator.Aggregator);

    return new Path { PathId = pathId, Spots = spots.Select(ms => ms.S).ToList() };
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
