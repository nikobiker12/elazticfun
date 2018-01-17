#load "..\shared\datamodel.csx"

using System;
using System.Linq;
using System.Collections.Generic;

public static async Task Run(PathBatch pathBatch, IAsyncCollector<PricingResult> outputTable, TraceWriter log)
{
    log.Info($"Payoff trigger function for batch {pathBatch.PathBatchId} with {pathBatch.Paths.Count} paths");

    var price = pathBatch.Paths.Select(p => VanillaCallPayoff(pathBatch.Times, p.Spots, pathBatch.PricingParameters))
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

public static double VanillaCallPayoff(List<double> times, List<double> spots, PricingParameters pricingParameters)
{
    var lastSpot = spots[spots.Count - 1];
    return Math.Max(lastSpot - pricingParameters.Strike, 0);
}
