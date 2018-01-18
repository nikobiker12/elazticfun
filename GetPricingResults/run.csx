using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    string pricingRequestId = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "pricingRequestId", true) == 0)
        .Value;


    return pricingRequestId == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK, "Hello " + pricingRequestId);
}
