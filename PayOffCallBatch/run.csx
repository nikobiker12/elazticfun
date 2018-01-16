#load "..\shared\datamodel.csx"

#r "System.Runtime.Serialization"

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

public static void Run(PathBatch pathBatch, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {pathBatch}");

    var price = pathBatch.Paths.Select(p => VanillaCallPayoff(p.States, pathBatch.PricingParameters))
        .Sum();

    // Write sum and count in azure table

}

public static double VanillaCallPayoff(List<MarketState> states, PricingParameters pricingParameters)
{
    var lastSpot = states[states.Count - 1].S;
    return Math.Max(lastSpot - pricingParameters.Strike, 0);
}
