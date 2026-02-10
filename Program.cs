using Azure.Identity;
using EmailReader.SemanticKernel;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.Messages.Item.Move;
using Microsoft.Kiota.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace EmailReader;

internal class Program
{

    /// <summary>
    /// Moves a email to another folder
    /// </summary>
    /// <param name="messageId">Message Id</param>
    /// <param name="destinationFolderId">Destination Folder Id</param>
    /// <returns></returns>
    public static async Task MoveEmailToFolderAsync(string messageId, string destinationFolderId)
    {
        GraphServiceClient graphClient = GetGraphClient();
        // Define the request body with the destination folder ID
        var requestBody = new MovePostRequestBody
        {
            DestinationId = destinationFolderId
        };

        try
        {
            // Call the Move API method
            await graphClient.Users[Config.Instance.UserPrincipleName].Messages[messageId].Move.PostAsync(requestBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving email: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a Mail folder
    /// </summary>
    /// <param name="folderName"></param>
    /// <returns></returns>
    public static async Task<MailFolder?> CreateMailFolder(string folderName)
    {
        GraphServiceClient graphClient = GetGraphClient();
        var newMailFolder = new MailFolder
        {
            DisplayName = folderName
        };

        try
        {
            var createdFolder = await graphClient.Users[Config.Instance.UserPrincipleName].MailFolders
                .PostAsync(newMailFolder);
            return createdFolder;
        }
        catch (ApiException ex)
        {
            return null;
        }
    }

    /// <summary>
    /// Finds a folder in the Mailbox
    /// </summary>
    /// <param name="targetFolderName"></param>
    /// <returns></returns>
    private static async Task<string> FindFolder(string targetFolderName)
    {
        GraphServiceClient graphClient = GetGraphClient();
        MailFolderCollectionResponse? collectionResponse =
            await graphClient.Users[Config.Instance.UserPrincipleName].MailFolders.GetAsync();
        foreach (var folder in collectionResponse.Value.Where(folder =>
                     folder.DisplayName.Equals(targetFolderName, StringComparison.InvariantCultureIgnoreCase)))
            return folder.Id;
        while (collectionResponse.OdataNextLink != null)
        {
            collectionResponse = await graphClient.Users[Config.Instance.UserPrincipleName].MailFolders
                .WithUrl(collectionResponse.OdataNextLink).GetAsync();
            foreach (var folder in collectionResponse.Value.Where(folder =>
                         folder.DisplayName.Equals(targetFolderName, StringComparison.InvariantCultureIgnoreCase)))
                return folder.Id;
        }

        return "";
    }

    /// <summary>
    /// Fetches a Graph Client
    /// </summary>
    /// <returns></returns>
    public static GraphServiceClient GetGraphClient()
    {
        string tenantId = Config.Instance.TenantId;
        string clientId = Config.Instance.ClientId;
        string clientSecret = Config.Instance.ClientSecret;
        // User Principal Name (UPN) or ID of the user whose mailbox you want to access
        //const string userPrincipalName = "user@yourtenant.onmicrosoft.com";
        ClientSecretCredentialOptions options = new ClientSecretCredentialOptions();
        // Initialize the credential with the client secret
        ClientSecretCredential clientSecretCredential = new ClientSecretCredential(
            tenantId, clientId, clientSecret, options);
        return new GraphServiceClient(clientSecretCredential);
    }

    /// <summary>
    /// Checks if a folder exists in the user principle's mailbox
    /// </summary>
    /// <param name="targetFolderName"></param>
    /// <returns></returns>
    public static async Task<bool> CheckIfFolderExists(string targetFolderName)
    {
        try
        {
            string r = await FindFolder(targetFolderName);
            return !string.IsNullOrEmpty(r);
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public static async Task<bool> IsPromotional(string emailBody)
    {
        PromptExecutionSettings settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(autoInvoke: true),
            ModelId = Config.Instance.Model
        };
        SemanticKernelConstructor constructor = new SemanticKernelConstructor();
        ChatHistory history = [];
        history.AddMessage(AuthorRole.User, "Please provide as brief of a answer as possible.");
        history.AddMessage(AuthorRole.User, "Is this a promotional email selling something or advertising?");
        history.AddMessage(AuthorRole.User, emailBody);
        var chat = constructor.Kernel.GetRequiredService<IChatCompletionService>();

        IReadOnlyList<ChatMessageContent> result =
            await chat.GetChatMessageContentsAsync(history, settings, constructor.Kernel);

        Console.WriteLine(result[0].Content);

        return !result[0].Content.StartsWith("No", StringComparison.InvariantCultureIgnoreCase);
    }

    private static async Task Main(string[] args)
    {
        Config.Instance.Load();

        GraphServiceClient graphClient = GetGraphClient();

        if (!await CheckIfFolderExists(Config.Instance.PromotionsMailFolder))
            await CreateMailFolder(Config.Instance.PromotionsMailFolder);

        var promotionFolderId = await FindFolder(Config.Instance.PromotionsMailFolder);

        // Retrieve messages from the user's Inbox folder
        // You can use well-known folder names like 'Inbox', 'SentItems', etc.
        MessageCollectionResponse? messages = await graphClient.Users[Config.Instance.UserPrincipleName]
            .MailFolders["Inbox"]
            .Messages
            .GetAsync();


        if (messages!.Value != null)
        {
            foreach (Message message in messages.Value)
                if (message.Body is { Content: not null })
                    if (await IsPromotional(message.Body.Content))
                        await MoveEmailToFolderAsync(message.Id, promotionFolderId);


            while (messages.OdataNextLink != null)
            {
                messages = await graphClient.Users[Config.Instance.UserPrincipleName]
                    .MailFolders["Inbox"]
                    .Messages
                    .WithUrl(messages.OdataNextLink)
                    .GetAsync();
                if (messages!.Value == null)
                    continue;
                foreach (Message message in messages.Value)
                    if (message.Body is { Content: not null })
                        if (await IsPromotional(message.Body.Content))
                            await MoveEmailToFolderAsync(message.Id, promotionFolderId);

            }
        }
    }
}