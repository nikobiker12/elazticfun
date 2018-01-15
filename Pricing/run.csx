using System.Net;

public static async Task<HttpResponseMessage> Run(
    HttpRequestMessage req,
    IAsyncCollector<PricingParameters> pricingRequests,
    TraceWriter log)
{
     log.Info("C# HTTP trigger function processed a request.");

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    double? strike = data?.strike;
    if (strike == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass strike in the request body");

    double? maturity = data?.maturity;
    if (maturity == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass maturity in the request body");

    double? spot = data?.spot;
    if (spot == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass spot in the request body");

    double? volatility = data?.volatility;
    if (volatility == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass volatility in the request body");

    int? simulationCount = data?.simulationCount;
    if (simulationCount == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass simulationCount in the request body");

    PricingParameters pricingParameters = new PricingParameters{
        Id = Guid.NewGuid().ToString(),
        Strike = strike.GetValueOrDefault(),
        Maturity = maturity.GetValueOrDefault(),
        Spot = spot.GetValueOrDefault(),
        Volatility = volatility.GetValueOrDefault(),
        SimulationCount = simulationCount.GetValueOrDefault() };

    await pricingRequests.AddAsync(pricingParameters);

    return req.CreateResponse(HttpStatusCode.OK, "Instrument Id: " + pricingParameters.Id);
}

public class PricingParameters {
    public string Id {get; set;}
    public double Strike {get; set;}
    public double Maturity {get; set;}
    public double Spot {get; set;}
    public double Volatility{get; set;}
    public int SimulationCount{get; set;}
}
