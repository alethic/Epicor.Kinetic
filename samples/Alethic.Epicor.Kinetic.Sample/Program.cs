// Command-line explorer for a Kinetic instance. Credentials come from user secrets or environment variables
// (Kinetic__Username, Kinetic__Password, Kinetic__ApiKey), never from source.
//
//   dotnet run -- function ExampleFunctions CalculateCharge '{"orderNum":123}'
//   dotnet run -- call Erp.BO.CustomerSvc GetByID '{"custNum":1}'
//   dotnet run -- baq MyBaq --filter "Customer_CustID eq 'ACME'" --top 10 StartDate=2026-01-01
//   dotnet run -- odata Erp.BO.CustomerSvc Customers --filter "CustID eq 'ACME'" --select CustNum,Name --top 5

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OData.Client;
using Alethic.Epicor.Kinetic.Client;
using Alethic.Epicor.Kinetic.Sample;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Services.AddKineticClient(builder.Configuration.GetSection(KineticOptions.SectionName));

using var host = builder.Build();
var output = KineticJson.Create();
output.WriteIndented = true;

try
{
    var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
    object? result = command switch
    {
        "function" when args.Length >= 3 => await InvokeFunctionAsync(args[1], args[2], args.ElementAtOrDefault(3)),
        "call" when args.Length >= 3 => await CallServiceAsync(args[1], args[2], args.ElementAtOrDefault(3)),
        "baq" when args.Length >= 2 => await QueryBaqAsync(args[1], CommandLine.Parse(args, 2)),
        "odata" when args.Length >= 3 => await QueryODataAsync(args[1], args[2], CommandLine.Parse(args, 3)),
        "customer" when args.Length >= 2 => await FindCustomerAsync(args[1]),
        "get" when args.Length >= 2 => await GetTextAsync(args[1], CommandLine.Parse(args, 2)),
        _ => null,
    };

    if (result is null)
    {
        PrintUsage();
        return 2;
    }

    Console.WriteLine(JsonSerializer.Serialize(result, output));
    return 0;
}
catch (KineticApiException ex)
{
    Console.Error.WriteLine(ex.Message);
    if (ex.ErrorType is not null)
        Console.Error.WriteLine($"  type: {ex.ErrorType}");

    foreach (var detail in ex.ErrorDetails)
        Console.Error.WriteLine($"  {detail.Type}: {detail.Message} [{detail.Table}.{detail.Field}]");

    if (ex.CorrelationId is not null)
        Console.Error.WriteLine($"  correlation: {ex.CorrelationId}");

    return 1;
}

/// <summary>
/// Invokes an Epicor Function with an optional JSON request body.
/// </summary>
Task<JsonElement> InvokeFunctionAsync(string library, string function, string? json)
{
    var client = host.Services.GetRequiredService<IKineticFunctionClient>();
    return client.InvokeAsync<JsonElement>(library, function, ParseJson(json));
}

/// <summary>
/// Calls a business object method with an optional JSON parameter object.
/// </summary>
Task<JsonElement> CallServiceAsync(string service, string method, string? json)
{
    var client = host.Services.GetRequiredService<IKineticServiceClient>();
    return client.CallAsync<JsonElement>(service, method, ParseJson(json));
}

/// <summary>
/// Runs a BAQ with OData flags and BAQ parameters from the command line.
/// </summary>
async Task<object> QueryBaqAsync(string baqId, CommandLine options)
{
    var client = host.Services.GetRequiredService<IKineticBaqClient>();
    var query = new BaqQuery
    {
        Filter = options.Flag("filter"),
        Select = options.Flag("select"),
        OrderBy = options.Flag("orderby"),
        Top = options.Int("top"),
        Skip = options.Int("skip"),
        Parameters = options.Parameters,
    };

    return await client.QueryAsync<JsonElement>(baqId, query);
}

/// <summary>
/// Reads an entity set of a service through the raw OData endpoint, without needing entity types.
/// </summary>
async Task<object> QueryODataAsync(string service, string entitySet, CommandLine options)
{
    var client = host.Services.GetRequiredService<IKineticServiceClient>();
    var company = host.Services.GetRequiredService<IOptions<KineticOptions>>().Value.Company;
    var query = new List<string>();
    foreach (var option in new[] { "filter", "select", "expand", "orderby", "top", "skip" })
    {
        if (options.Flag(option) is { } value)
            query.Add($"${option}={Uri.EscapeDataString(value)}");
    }

    var path = $"api/v2/odata/{Uri.EscapeDataString(company)}/{Uri.EscapeDataString(service)}/{Uri.EscapeDataString(entitySet)}";
    if (query.Count > 0)
        path += "?" + string.Join("&", query);

    var result = await client.SendAsync<JsonElement>(HttpMethod.Get, path);
    return result.TryGetProperty("value", out var rows) ? rows : result;
}

/// <summary>
/// Looks a customer up by ID through a Microsoft.OData.Client context, which loads the service metadata first.
/// </summary>
async Task<object> FindCustomerAsync(string custId)
{
    var factory = host.Services.GetRequiredService<IKineticODataContextFactory>();
    var context = factory.ForService("Erp.BO.CustomerSvc");
    var query = (DataServiceQuery<Customer>)context.CreateQuery<Customer>("Customers").Where(c => c.CustID == custId).Take(1);
    return (await query.ExecuteAsync()).ToList();
}

/// <summary>
/// Fetches any path under the instance root through the authenticated HttpClient. Prints the body, or saves it to
/// <c>--out</c> when given. <c>--accept</c> overrides the Accept header. Useful for the help site and metadata documents.
/// </summary>
async Task<object> GetTextAsync(string relativePath, CommandLine options)
{
    var outputFile = options.Flag("out");
    var http = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(KineticServiceCollectionExtensions.HttpClientName);
    using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(options.Flag("accept") ?? "*/*"));
    using var response = await http.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();

    if (outputFile is null)
    {
        Console.WriteLine(body);
    }
    else
    {
        File.WriteAllText(outputFile, body);
    }

    return new
    {
        status = (int)response.StatusCode,
        contentType = response.Content.Headers.ContentType?.ToString(),
        length = body.Length,
        savedTo = outputFile is null ? null : Path.GetFullPath(outputFile),
    };
}

/// <summary>
/// Parses an optional JSON argument into an element the clients can serialise back out.
/// </summary>
static object? ParseJson(string? json)
{
    return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<JsonElement>(json);
}

/// <summary>
/// Prints the supported commands and how to configure credentials.
/// </summary>
static void PrintUsage()
{
    Console.Error.WriteLine("""
        Usage:
          function <Library> <Function> [jsonBody]
          call <Service> <Method> [jsonBody]
          baq <BaqId> [--filter f] [--select s] [--orderby o] [--top n] [--skip n] [Param=value ...]
          odata <Service> <EntitySet> [--filter f] [--select a,b] [--expand e] [--orderby o] [--top n] [--skip n]
          customer <CustID>
          get <relativePath> [--out file] [--accept mediaType]

        Configure credentials once with user secrets:
          dotnet user-secrets set "Kinetic:Username" "<user>"
          dotnet user-secrets set "Kinetic:Password" "<password>"
          dotnet user-secrets set "Kinetic:ApiKey" "<api key>"
        """);
}
