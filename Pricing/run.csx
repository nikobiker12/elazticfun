#load "..\shared\datamodel.csx"

using System.Net;

public static async Task<HttpResponseMessage> Run(
    HttpRequestMessage req,
    IAsyncCollector<PricingParameters> pricingRequests,
    TraceWriter log)
{
    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    string optionType = data?.optionType;
    if (String.IsNullOrEmpty(optionType))
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass optionType in the request body"));
    if (!System.Enum.TryParse(optionType, out PricingParameters.EOptionType optionTypeE))
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError($"optionType \"{optionType}\" is not valid. Must be Call or Put."));

    string payoffName = data?.payoffName;
    if (String.IsNullOrEmpty(payoffName))
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass payoffName in the request body"));

    double? strike = data?.strike;
    if (strike == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass strike in the request body"));

    double? maturity = data?.maturity;
    if (maturity == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass maturity in the request body"));

    double? spot = data?.spot;
    if (spot == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass spot in the request body"));

    double? volatility = data?.volatility;
    if (volatility == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass volatility in the request body"));

    int? simulationCount = data?.simulationCount;
    if (simulationCount == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass simulationCount in the request body"));

    int? spotBumpCount = data?.spotBumpCount;
    if (spotBumpCount == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass spotBumpCount in the request body"));

    int? volBumpCount = data?.volBumpCount;
    if (volBumpCount == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass volBumpCount in the request body"));

    double? spotBumpSize = data?.spotBumpSize;
    if (spotBumpSize == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass spotBumpSize in the request body"));

    double? volBumpSize = data?.volBumpSize;
    if (volBumpSize == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new GetRequestError("Please pass volBumpSize in the request body"));

    PricingParameters pricingParameters = new PricingParameters
    {
        Id = Guid.NewGuid().ToString(),
        OptionType = optionTypeE,
        PayoffName = payoffName,
        Strike = strike.GetValueOrDefault(),
        Maturity = maturity.GetValueOrDefault(),
        Spot = spot.GetValueOrDefault(),
        Volatility = volatility.GetValueOrDefault(),
        SimulationCount = simulationCount.GetValueOrDefault(),
        SpotBumpCount = spotBumpCount.Value,
        VolBumpCount = volBumpCount.Value,
        SpotBumpSize = spotBumpSize.Value,
        VolBumpSize = volBumpSize.Value,
    };

    await pricingRequests.AddAsync(pricingParameters);

    return req.CreateResponse(HttpStatusCode.OK, new GetRequestResult(pricingParameters.Id));
}
