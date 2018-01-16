#load "..\shared\datamodel.csx"

#r "System.Runtime.Serialization"

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

public static async Task Run(PathBatch pathBatch, IAsyncCollector<PricingResult> outputTable, TraceWriter log)
{
    log.Info($"Queue trigger function for batch {pathBatch.PathBatchId} with {pathBatch.Paths.Count} paths");

    var price = pathBatch.Paths.Select(p => VanillaCallPayoff(p.States, pathBatch.PricingParameters))
        .Sum();

    var result = new PricingResult
    {
        PartitionKey = pathBatch.PricingParameters.Id,
        RowKey = pathBatch.PathBatchId.ToString(),
        PriceSum = price,
        PathsCount = pathBatch.Paths.Count,
        TotalPathsCount = pathBatch.PricingParameters.SimulationCount,
    };

    await outputTable.AddAsync(result);
}

public static double VanillaCallPayoff(List<MarketState> states, PricingParameters pricingParameters)
{
    var lastSpot = states[states.Count - 1].S;
    return Math.Max(lastSpot - pricingParameters.Strike, 0);
}
