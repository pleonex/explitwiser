namespace PleOps.Explitwiser;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using PleOps.Splitwise.Client;
using PleOps.Splitwise.Client.Get_current_user;
using PleOps.Splitwise.Client.Get_groups;
using PleOps.Splitwise.Client.Models;

/// <summary>
/// Exporter of the Splitwise user data into JSON format.
/// </summary>
/// <remarks>
/// The output format is the same as the API response. The data is not transformed
/// except for the paths for the downloaded images.
/// </remarks>
public class SplitwiseJsonExporter
{
    private static readonly JsonSerializerOptions jsonOptions = new() {
        WriteIndented = true,
    };

    private readonly SplitwiseClient client;
    private readonly SplitwiseResourcesExporter resourcesExporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitwiseJsonExporter"/> class.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="resourcesExporter">Exporter of resources.</param>
    public SplitwiseJsonExporter(SplitwiseClient client, SplitwiseResourcesExporter resourcesExporter)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(resourcesExporter);

        this.client = client;
        this.resourcesExporter = resourcesExporter;
    }

    /// <summary>
    /// Export the information of the current user.
    /// </summary>
    /// <param name="outputDirectory">Directory to save the export.</param>
    /// <param name="downloadImages">Value indicating whether the linked images should be downloaded or kept as URLs.</param>
    /// <returns>Asynchronous operation.</returns>
    public async Task ExportUserProfileAsync(string outputDirectory, bool downloadImages)
    {
        Get_current_userGetResponse response = await client.Get_current_user.GetAsync()
            ?? throw new InvalidDataException("Unexpected data response");

        if (downloadImages && response.User is not null) {
            await resourcesExporter.ExportUserAsync(response.User, outputDirectory);
        }

        await SerializeDataAsync(response, outputDirectory, "profile.json");
    }

    /// <summary>
    /// Export the information of all groups the user belongs.
    /// </summary>
    /// <param name="outputDirectory">Directory to save the export.</param>
    /// <param name="downloadImages">Value indicating whether the linked images should be downloaded or kept as URLs.</param>
    /// <returns>Asynchronous operation.</returns>
    public async Task ExportGroupsAsync(string outputDirectory, bool downloadImages)
    {
        Get_groupsGetResponse response = await client.Get_groups.GetAsync()
            ?? throw new InvalidDataException("Unexpected data response");

        if (downloadImages && response.Groups is { Count: > 0 }) {
            foreach (Group group in response.Groups) {
                await resourcesExporter.ExportGroupAsync(group, outputDirectory);
            }
        }

        await SerializeDataAsync(response, outputDirectory, "groups.json");
    }

    private static async Task SerializeDataAsync<T>(T data, string outputDirectory, string name)
    {
        string serializedData = JsonSerializer.Serialize(data, jsonOptions);

        string outputFile = Path.Combine(outputDirectory, name);
        string directoryPath = Path.GetDirectoryName(outputFile)!;
        _ = Directory.CreateDirectory(directoryPath);

        await File.WriteAllTextAsync(outputFile, serializedData);
    }
}
