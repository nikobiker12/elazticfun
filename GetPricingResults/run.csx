#load "..\shared\datamodel.csx"
#load "..\shared\Constants.csx"

#r "Newtonsoft.Json"

using System.Net;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

class PricingResult
{
    public double Price;
    public int ProgressInPercent;
    public double Spot;
    public double Volatility;
    public int RiskIndex;
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
        ProgressInPercent = 100 * result.PathsSum / result.TotalPathsCount,
        Spot = result.Spot,
        Volatility = result.Volatility,
        RiskIndex = int.Parse(result.RowKey)
    });

    dynamic json = new JArray(outputs.ToArray());
    string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
    log.Info($"JSON sent back to GUI = {jsonString}");

    return req.CreateResponse(HttpStatusCode.OK, jsonString);
}
