﻿namespace PleOps.Explitwiser.Console;

using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PleOps.Splitwise.Client;
using Spectre.Console;
using Spectre.Console.Cli;

[Description("Export all the data of the current user")]
internal class ExportAllCommand : AsyncCommand<ExportAllCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[Output]")]
        [Description("The root output directory")]
        public required string Output { get; set; }

        [CommandOption("--skip-images")]
        [Description("Do not download the associated images like avatars or receipts")]
        public bool SkipImages { get; set; }

        [CommandOption("--skip-comments")]
        [Description("Do not export expenses comments (takes longer)")]
        public bool SkipComments { get; set; }

        [CommandOption("--expenses-per-query")]
        [Description("Number of expenses to fetch per API query")]
        [DefaultValue(SplitwiseJsonExporter.DefaultExpensesPerQuery)]
        public int ExpensesPerQuery { get; set; }

        [CommandOption("--notifications-limit")]
        [Description("Maximum number of notifications to retrieve")]
        [DefaultValue(SplitwiseJsonExporter.DefaultNotificationsLimit)]
        public int NotificationsLimit { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables(prefix: "SPLITWISE_")
            .AddJsonFile("secrets.json", optional: true)
            .Build();

        string? apiKey = configuration["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) {
            AnsiConsole.MarkupLine("[red]Missing API key[/]");
            AnsiConsole.WriteLine("Specify it via the environment variable SPLITWISE_ApiKey or in the file secrets.json");
            return 1;
        }

        SplitwiseClient client = SplitwiseClientFactory.CreateWithApiKeyAuth(apiKey);
        var resourcesExporter = new SplitwiseResourcesExporter();
        var exporter = new SplitwiseJsonExporter(client, resourcesExporter);

        string outputDirectory = Path.GetFullPath(settings.Output);
        bool downloadResources = !settings.SkipImages;
        bool downloadComments = !settings.SkipComments;

        await AnsiConsole.Status().StartAsync(
            "Exporting user profile",
            async _ => await exporter.ExportUserProfileAsync(outputDirectory, downloadResources));
        AnsiConsole.MarkupLine("User profile [green]exported[/]");

        await AnsiConsole.Status().StartAsync(
            "Exporting friend list",
            async _ => await exporter.ExportFriendsAsync(outputDirectory, downloadResources));
        AnsiConsole.MarkupLine("Friend list [green]exported[/]");

        await AnsiConsole.Status().StartAsync(
            "Exporting groups",
            async _ => await exporter.ExportGroupsAsync(outputDirectory, downloadResources));
        AnsiConsole.MarkupLine("Groups [green]exported[/]");

        await AnsiConsole.Status().StartAsync(
            "Exporting expenses",
            async _ => await exporter.ExportExpensesAsync(
                outputDirectory,
                downloadResources,
                downloadComments,
                settings.ExpensesPerQuery));

        await AnsiConsole.Status().StartAsync(
            "Exporting notifications",
            async _ => await exporter.ExportNotificationsAsync(
                outputDirectory,
                downloadResources,
                settings.NotificationsLimit));

        return 0;
    }
}
