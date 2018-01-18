#load "..\shared\datamodel.csx"
#load "..\shared\Constants.csx"

using System.Net;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using System.Collections.Generic;
using System.Linq;

class PricingResult
{
    public double Price;
    public double Progress;
    public int ProcessedPathCount;
    public int TotalPathCount;
    public double Spot;
    public double Volatility;
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

    var storage = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable(AZUREWEBJOBSSTORAGE_CONNECTIONSTRING_KEY));
    var tableClient = storage.CreateCloudTableClient();
    var table = tableClient.GetTableReference(PRICINGRESULTS_TABLE);
    // Construct the query operation for all customer entities where PartitionKey="Smith".
    TableQuery<PricingResultEntity> query = new TableQuery<PricingResultEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pricingRequestId));

    var queryResult = table.ExecuteQuery(query);
    var outputs = queryResult.Select(result => new PricingResult
    {
        Price = result.PathsSum == 0.0 ? 0.0 : result.IndicatorSum / result.PathsSum,
        Progress = result.PathsSum / result.TotalPathsCount,
        ProcessedPathCount = result.PathsSum,
        TotalPathCount = result.TotalPathsCount,
        Spot = result.Spot,
        Volatility = result.Volatility
    });

    return req.CreateResponse(HttpStatusCode.OK, outputs.ToArray());
}
