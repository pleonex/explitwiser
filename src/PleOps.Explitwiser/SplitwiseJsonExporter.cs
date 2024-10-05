namespace PleOps.Explitwiser;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using PleOps.Splitwise.Client;
using PleOps.Splitwise.Client.Get_comments;
using PleOps.Splitwise.Client.Get_current_user;
using PleOps.Splitwise.Client.Get_expenses;
using PleOps.Splitwise.Client.Get_friends;
using PleOps.Splitwise.Client.Get_groups;
using PleOps.Splitwise.Client.Get_notifications;
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
    /// Default number of expenses to retrieve per query.
    /// </summary>
    public const int DefaultExpensesPerQuery = 500;

    /// <summary>
    /// Default maximum notifications to retrieve.
    /// </summary>
    public const int DefaultNotificationsLimit = 10_000;


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

        if (downloadImages) {
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

    /// <summary>
    /// Export the list of friends in Splitwise.
    /// </summary>
    /// <param name="outputDirectory">Directory to save the export.</param>
    /// <param name="downloadImages">Value indicating whether the linked images should be downloaded or kept as URLs.</param>
    /// <returns>Asynchronous operation.</returns>
    public async Task ExportFriendsAsync(string outputDirectory, bool downloadImages)
    {
        Get_friendsGetResponse response = await client.Get_friends.GetAsync()
            ?? throw new InvalidDataException("Unexpected data response");

        if (downloadImages && response.Friends is { Count: > 0 }) {
            foreach (Friend friend in response.Friends) {
                await resourcesExporter.ExportUserAsync(friend, outputDirectory);
            }
        }

        await SerializeDataAsync(response, outputDirectory, "friends.json");
    }

    /// <summary>
    /// Export all the expenses where the user is part of.
    /// </summary>
    /// <param name="outputDirectory">Directory to save the export.</param>
    /// <param name="downloadImages">Value indicating whether the linked images should be downloaded or kept as URLs.</param>
    /// <param name="downloadComments">Value indicating whether it will fetch the expense comments.</param>
    /// <param name="expensesPerQuery">Number of expenses to retrieve per API query.</param>
    /// <returns>Asynchronous operation.</returns>
    public async Task ExportExpensesAsync(
        string outputDirectory,
        bool downloadImages,
        bool downloadComments,
        int expensesPerQuery = DefaultExpensesPerQuery)
    {
        int index = 0;
        Get_expensesGetResponse? response;
        do {

            response = await client.Get_expenses.GetAsync(config => {
                config.QueryParameters.Offset = index * expensesPerQuery;
                config.QueryParameters.Limit = expensesPerQuery;
            }) ?? throw new InvalidDataException("Unexpected data response");

            foreach (Expense expense in response.Expenses ?? []) {
                if (downloadComments) {
                    await FetchExpenseCommentsAsync(expense);
                }

                if (downloadImages) {
                    await resourcesExporter.ExportExpenseAsync(expense, outputDirectory);
                }
            }

            await SerializeDataAsync(response, outputDirectory, $"expenses_{index}.json");
            index++;
        } while (response.Expenses?.Count == expensesPerQuery);
    }

    /// <summary>
    /// Export the notifications / activity messages of the account.
    /// </summary>
    /// <param name="outputDirectory">Directory to save the export.</param>
    /// <param name="downloadImages">Value indicating whether the linked images should be downloaded or kept as URLs.</param>
    /// <param name="limit">Maximum number of entries.</param>
    /// <returns>Asynchronous task</returns>
    public async Task ExportNotificationsAsync(
        string outputDirectory,
        bool downloadImages,
        int limit = DefaultNotificationsLimit)
    {
        // Unfortunately this API doesn't allow to iterate to get all but it has a very high limit.
        Get_notificationsGetResponse response = await client.Get_notifications.GetAsync(
            config => config.QueryParameters.Limit = limit)
            ?? throw new InvalidDataException("Unexpected data response");

        if (downloadImages) {
            foreach (Notification notification in response.Notifications ?? []) {
                await resourcesExporter.ExportNotificationAsync(notification, outputDirectory);
            }
        }

        await SerializeDataAsync(response, outputDirectory, "activity.json");
    }

    private async Task FetchExpenseCommentsAsync(Expense expense)
    {
        if (expense.CommentsCount == 0) {
            return;
        }

        Get_commentsGetResponse response = await client.Get_comments.GetAsync(
            config => config.QueryParameters.ExpenseId = expense.Id)
             ?? throw new InvalidDataException("Unexpected data response");
        expense.Comments = response.Comments;
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
