#load "..\shared\datamodel.csx"
#load "..\shared\Constants.csx"

using System.Net;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;

class PricingResult
{
    public string PricingRequestId;
    public double Price;
    public double Progress;
    public int ProcessedPathCount;
    public int TotalPathCount;
    public double Spot;
    public double Volatility;
}

private static PricingResult[] GetRandomResults(string pricingRequestId)
{
    Random random = new Random();
    var progress = random.NextDouble();
    var totalPathCount = 25000;
    var spot = random.NextDouble() * 12.0;
    var volatility = random.NextDouble() * 12.0;
    var results = Enumerable.Range(1, 9).Select(i =>
        new PricingResult()
        {
            PricingRequestId = pricingRequestId,
            Price = (int)(random.NextDouble() * 100.0),
            ProcessedPathCount = (int)(progress * totalPathCount),
            Progress = progress,
            TotalPathCount = totalPathCount,
            Spot = spot + (0.53 * i),
            Volatility = volatility + (0.12 * i),
        }
    ).ToArray();

    return results;
}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    string pricingRequestId = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "pricingRequestId", true) == 0)
        .Value;

    if (pricingRequestId == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a pricingRequestId in a GET request");
    
    var results = GetRandomResults(pricingRequestId);

    log.Info($"Sending mock results response.");

    return req.CreateResponse(HttpStatusCode.OK, results);
}
