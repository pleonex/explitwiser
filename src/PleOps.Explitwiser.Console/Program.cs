using Microsoft.Extensions.Configuration;
using PleOps.Explitwiser;
using PleOps.Splitwise.Client;
using Spectre.Console;

string outputDirectory = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "export");

IConfigurationRoot configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables(prefix: "SPLITWISE_")
    .AddJsonFile("secrets.json", optional: true)
    .Build();

string? apiKey = configuration["ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey)) {
    AnsiConsole.MarkupLine("[red]Missing API key[/]");
    AnsiConsole.WriteLine("Specify it via the environment variable SPLITWISE_API_KEY or in the file secrets.json");
    return;
}

SplitwiseClient client = SplitwiseClientFactory.CreateWithApiKeyAuth(apiKey);
var exporter = new SplitwiseJsonExporter(client);

await AnsiConsole.Status()
    .StartAsync(
        "Exporting user profile",
        async _ => await exporter.ExportUserProfileAsync(outputDirectory, true));
AnsiConsole.MarkupLine("User profile [green]exported[/]");

await AnsiConsole.Status()
    .StartAsync(
        "Exporting user groups",
        async _ => await exporter.ExportGroupsAsync(outputDirectory, true));
AnsiConsole.MarkupLine("User groups [green]exported[/]");
