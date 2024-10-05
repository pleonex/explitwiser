namespace PleOps.Explitwiser;

using PleOps.Splitwise.Client.Models;

/// <summary>
/// Export resources such as images into the local disk.
/// </summary>
/// <remarks>
/// After saving the resource, it updates the model to the relative path of the file.
/// </remarks>
public class SplitwiseResourcesExporter
{
    private readonly HttpClient client = new();

    /// <summary>
    /// Export the avatar pictures of the user.
    /// </summary>
    /// <param name="user">User to export resources.</param>
    /// <param name="outputDirectory">The root output directory.</param>
    /// <returns>Asynchronous task.</returns>
    public async Task ExportUserAsync(User? user, string outputDirectory)
    {
        if (user?.Picture is null) {
            return;
        }

        user.Picture.Small = await DownloadResourceAsync(user.Picture.Small, outputDirectory);
        user.Picture.Medium = await DownloadResourceAsync(user.Picture.Medium, outputDirectory);
        user.Picture.Large = await DownloadResourceAsync(user.Picture.Large, outputDirectory);
    }

    /// <summary>
    /// Export the avatar, cover photo and user pictures of a group.
    /// </summary>
    /// <param name="group">Group to export resources.</param>
    /// <param name="outputDirectory">The root output directory.</param>
    /// <returns>Asynchronous task.</returns>
    public async Task ExportGroupAsync(Group group, string outputDirectory)
    {
        if (group.Avatar is not null) {
            group.Avatar.Original = await DownloadResourceAsync(group.Avatar.Original, outputDirectory);
            group.Avatar.Small = await DownloadResourceAsync(group.Avatar.Small, outputDirectory);
            group.Avatar.Medium = await DownloadResourceAsync(group.Avatar.Medium, outputDirectory);
            group.Avatar.Large = await DownloadResourceAsync(group.Avatar.Large, outputDirectory);
            group.Avatar.Xlarge = await DownloadResourceAsync(group.Avatar.Xlarge, outputDirectory);
            group.Avatar.Xxlarge = await DownloadResourceAsync(group.Avatar.Xxlarge, outputDirectory);
        }

        if (group.CoverPhoto is not null) {
            group.CoverPhoto.Xlarge = await DownloadResourceAsync(group.CoverPhoto.Xlarge, outputDirectory);
            group.CoverPhoto.Xxlarge = await DownloadResourceAsync(group.CoverPhoto.Xxlarge, outputDirectory);
        }

        foreach (User member in group.Members ?? []) {
            await ExportUserAsync(member, outputDirectory);
        }
    }

    /// <summary>
    /// Export avatar of related users and the receipt.
    /// </summary>
    /// <param name="expense">Expense to export resources.</param>
    /// <param name="outputDirectory">The root output directory.</param>
    /// <returns>Asynchronous task.</returns>
    public async Task ExportExpenseAsync(Expense expense, string outputDirectory)
    {
        await ExportUserAsync(expense.CreatedBy, outputDirectory);
        await ExportUserAsync(expense.UpdatedBy, outputDirectory);
        await ExportUserAsync(expense.DeletedBy, outputDirectory);

        if (expense.Receipt is not null) {
            expense.Receipt.Original = await DownloadResourceAsync(expense.Receipt.Original, outputDirectory);
            expense.Receipt.Large = await DownloadResourceAsync(expense.Receipt.Large, outputDirectory);
        }

        foreach (Share user in expense.Users ?? []) {
            await ExportCommentUserAsync(user.User, outputDirectory);
        }

        foreach (Comment comment in expense.Comments ?? []) {
            await ExportCommentUserAsync(comment.User, outputDirectory);
        }
    }

    /// <summary>
    /// Export the image of the notification.
    /// </summary>
    /// <param name="notification">Notification to export resources.</param>
    /// <param name="outputDirectory">The root output directory.</param>
    /// <returns>Asynchronous task.</returns>
    public async Task ExportNotificationAsync(Notification notification, string outputDirectory)
    {
        notification.ImageUrl = await DownloadResourceAsync(notification.ImageUrl, outputDirectory);
    }

    private async Task ExportCommentUserAsync(Comment_user? commentUser, string outputDirectory)
    {
        if (commentUser?.Picture is null) {
            return;
        }

        commentUser.Picture.Medium = await DownloadResourceAsync(commentUser.Picture.Medium, outputDirectory);
    }

    private async Task<string> DownloadResourceAsync(string? url, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(url)) {
            return string.Empty;
        }

        var uri = new Uri(url);
        string relativePath = uri.LocalPath[1..];
        string outputFile = Path.GetFullPath(relativePath, outputDirectory);
        if (File.Exists(outputFile)) {
            return relativePath;
        }

        string directoryPath = Path.GetDirectoryName(outputFile)!;
        _ = Directory.CreateDirectory(directoryPath);

        using var outputStream = new FileStream(outputFile, FileMode.Create);
        using Stream responseStream = await client.GetStreamAsync(uri);

        await responseStream.CopyToAsync(outputStream);
        return relativePath;
    }
}
