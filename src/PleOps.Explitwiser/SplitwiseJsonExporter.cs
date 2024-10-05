namespace PleOps.Explitwiser;

using System;
using System.Runtime.CompilerServices;
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
    private static readonly HttpClient resourcesClient = new();
    private static readonly JsonSerializerOptions jsonOptions = new() {
        WriteIndented = true,
    };

    private readonly SplitwiseClient client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitwiseJsonExporter"/> class.
    /// </summary>
    /// <param name="client">The API client.</param>
    public SplitwiseJsonExporter(SplitwiseClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        this.client = client;
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

        if (downloadImages && response.User?.Picture is not null) {
            response.User.Picture.Small = await DownloadResourceAsync(response.User.Picture.Small, outputDirectory);
            response.User.Picture.Medium = await DownloadResourceAsync(response.User.Picture.Medium, outputDirectory);
            response.User.Picture.Large = await DownloadResourceAsync(response.User.Picture.Large, outputDirectory);
        }

        await SerializeDataAsync(response, outputDirectory, "profile.json");
    }

    private static async Task SerializeDataAsync<T>(T data, string outputDirectory, string name)
    {
        string serializedData = JsonSerializer.Serialize(data, jsonOptions);

        string outputFile = Path.Combine(outputDirectory, name);
        EnsureDirectoryExists(outputFile);

        await File.WriteAllTextAsync(outputFile, serializedData);
    }

    private static async Task<string> DownloadResourceAsync(string? url, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(url)) {
            return string.Empty;
        }

        var uri = new Uri(url);
        string outputFile = Path.Combine(outputDirectory, uri.LocalPath[1..]);
        outputFile = Path.GetFullPath(outputFile); // normalize \ / path separators
        if (File.Exists(outputFile)) {
            return outputFile;
        }

        EnsureDirectoryExists(outputFile);

        using var outputStream = new FileStream(outputFile, FileMode.Create);
        using Stream responseStream = await resourcesClient.GetStreamAsync(uri);

        await responseStream.CopyToAsync(outputStream);
        return outputFile;
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        string directoryPath = Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
