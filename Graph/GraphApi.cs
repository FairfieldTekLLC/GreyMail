using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Users.Item.Messages.Item.Move;
using Microsoft.Kiota.Abstractions;
using Message = Microsoft.Graph.Models.Message;

namespace GreyMail.Graph;

internal static class GraphApi
{
    /// <summary>
    ///     Moves a email to another folder
    /// </summary>
    /// <param name="messageId">Message Id</param>
    /// <param name="destinationFolderId">Destination Folder Id</param>
    /// <returns></returns>
    public static async Task MoveEmailToFolderAsync(string messageId, string destinationFolderId)
    {
        GraphServiceClient graphClient = GetGraphClient();
        // Define the request body with the destination folder ID
        MovePostRequestBody requestBody = new MovePostRequestBody
        {
            DestinationId = destinationFolderId
        };

        try
        {
            await AddBanner(messageId);
            // Call the Move API method
            await graphClient.Users[Config.Instance.UserPrincipleName].Messages[messageId].Move.PostAsync(requestBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving email: {ex.Message}");
        }
    }

    /// <summary>
    ///     Creates a Mail folder
    /// </summary>
    /// <param name="folderName"></param>
    /// <returns></returns>
    public static async Task<MailFolder?> CreateMailFolder(string folderName)
    {
        GraphServiceClient graphClient = GetGraphClient();
        MailFolder newMailFolder = new MailFolder
        {
            DisplayName = folderName
        };

        try
        {
            MailFolder? createdFolder = await graphClient.Users[Config.Instance.UserPrincipleName].MailFolders
                .PostAsync(newMailFolder);
            return createdFolder;
        }
        catch (ApiException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Finds a folder in the Mailbox
    /// </summary>
    /// <param name="targetFolderName"></param>
    /// <returns></returns>
    public static async Task<string> FindFolder(string targetFolderName)
    {
        try
        {
            GraphServiceClient graphClient = GetGraphClient();
            MailFolderCollectionResponse? collectionResponse =
                await graphClient.Users[Config.Instance.UserPrincipleName].MailFolders.GetAsync();


            foreach (MailFolder folder in collectionResponse.Value.Where(folder =>
                         folder.DisplayName.Equals(targetFolderName, StringComparison.InvariantCultureIgnoreCase)))
                return folder.Id;


            while (collectionResponse.OdataNextLink != null)
            {
                collectionResponse = await graphClient.Users[Config.Instance.UserPrincipleName].MailFolders
                    .WithUrl(collectionResponse.OdataNextLink).GetAsync();
                foreach (MailFolder folder in collectionResponse.Value.Where(folder =>
                             folder.DisplayName.Equals(targetFolderName, StringComparison.InvariantCultureIgnoreCase)))
                    return folder.Id;
            }
        }
        catch (Exception)
        {
            //Nothing
        }

        return "";
    }

    /// <summary>
    ///     Fetches a Graph Client
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
    ///     Checks if a folder exists in the user principle's mailbox
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

    /// <summary>
    ///     Retrieves emails from the users inbox
    /// </summary>
    /// <param name="odataNextLink"></param>
    /// <returns></returns>
    public static async Task<MessageCollectionResponse?> GetEmails(string? odataNextLink = null)
    {
        while (true)
            try
            {
                GraphServiceClient graphClient = GetGraphClient();
                if (odataNextLink == null)
                    return await graphClient.Users[Config.Instance.UserPrincipleName]
                        .MailFolders["Inbox"]
                        .Messages
                        .GetAsync();

                return await graphClient.Users[Config.Instance.UserPrincipleName]
                    .MailFolders["Inbox"]
                    .Messages
                    .WithUrl(odataNextLink)
                    .GetAsync();
            }
            catch (Exception)
            {
                await Task.Delay(2000);
            }
    }

    /// <summary>
    ///     Adds a banner to the email
    /// </summary>
    /// <param name="messageId"></param>
    /// <returns></returns>
    public static async Task AddBanner(string messageId)
    {
        GraphServiceClient graphClient = GetGraphClient();
        // Define the request body with the destination folder ID


        try
        {
            // Call the Move API method
            Message? msg = await graphClient.Users[Config.Instance.UserPrincipleName].Messages[messageId].GetAsync();

            string bannerHtml =
                "<div style='background-color:#f0a300; color:#ffffff; padding:10px; font-weight:bold;'>IMPORTANT: This is a promotional email.</div>";

            string emailBody = $"{bannerHtml}<p>{msg.Body.Content}</p>";

            ItemBody messageBody = new ItemBody
            {
                ContentType = BodyType.Html, // or BodyType.Text
                Content = emailBody
            };

            Message messageToUpdate = new Message
            {
                Body = messageBody
            };

            try
            {
                // The PATCH request updates only the specified properties.
                await graphClient.Users[Config.Instance.UserPrincipleName]
                    .Messages[messageId]
                    .PatchAsync(messageToUpdate);

                //Console.ForegroundColor = ConsoleColor.Blue;
                //Console.WriteLine($"Message body for message ID {messageId} updated successfully.");
            }
            catch (ODataError odataError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error updating message body: {odataError.Error.Code} - {odataError.Error.Message}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving email: {ex.Message}");
        }
    }

    public static async Task<bool> IsAlreadyProcessed(string messageId)
    {
        GraphServiceClient graphClient = GetGraphClient();
        // Define the request body with the destination folder ID


        try
        {
            // Call the Move API method
            Message? msg = await graphClient.Users[Config.Instance.UserPrincipleName].Messages[messageId].GetAsync();
            return msg.Body.Content.Contains(
                "IMPORTANT: This is a promotional email.",
                StringComparison.InvariantCultureIgnoreCase);
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}