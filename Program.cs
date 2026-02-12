using GreyMail.Graph;
using GreyMail.SemanticKernel;
using GreyMail.Sqlite;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GreyMail;

internal class Program
{
    public static async Task<bool?> IsPromotional(string emailBody)
    {
        while (true)
            try
            {
                PromptExecutionSettings settings = new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Required(autoInvoke: true),
                    ModelId = Config.Instance.Model
                };
                ISemanticKernelConstructorOllama constructor;
                if (Config.Instance.Mode.ToLower().Equals("ollama"))
                    constructor = new SemanticKernelConstructorOllama();
                else if (Config.Instance.Mode.ToLower().Equals("google"))
                    constructor = new SemanticKernelConstructorGoogle();
                else
                    throw new IOException("NOT WORKING");
                ChatHistory history = [];
                history.AddMessage(AuthorRole.User, "Please provide as brief of a answer as possible.");
                history.AddMessage(AuthorRole.User, "Is this a promotional email selling something or advertising?");
                history.AddMessage(AuthorRole.User, emailBody);
                IChatCompletionService chat = constructor.Kernel.GetRequiredService<IChatCompletionService>();

                IReadOnlyList<ChatMessageContent> result =
                    await chat.GetChatMessageContentsAsync(history, settings, constructor.Kernel);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(result[0].Content);
                Console.ForegroundColor = ConsoleColor.Blue;

                return !result[0].Content.StartsWith("No", StringComparison.InvariantCultureIgnoreCase);
            }
            catch (IOException h)
            {
                return null;
            }
            catch (Exception e)
            {
                await Task.Delay(2000);
            }
    }


    public static void UserExited()
    {
        if (!Console.KeyAvailable)
            return;
        string? text = Console.ReadLine();
        if (text == null)
            return;
        if (text.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            Environment.Exit(1);
    }


    public static async Task ProcessEmail(Message message, string promotionFolderId)
    {
        if (!SqlLiteProvider.CheckIfMessageProcessed(message.Id))
        {
            if (!await GraphApi.IsAlreadyProcessed(message.Id))
            {
                string domain = message.Sender.EmailAddress.Address.GetDomainFromEmail();
                if (!SqlLiteProvider.IsWhiteListed(domain))
                {
                    bool? promo = await IsPromotional(message.Body.Content);
                    if (promo == true)
                        await GraphApi.MoveEmailToFolderAsync(message.Id, promotionFolderId);
                    else 
                    
                    if (promo == null)
                    {
                        Console.WriteLine("Your GOOGLE API CODE IS NOT VALID");
                        throw new Exception("Google API key Invalid.");
                    }
                }
                else
                {
                    Console.WriteLine(DateTime.Now.ToString("g") + "Domain whitelisted.");
                }
            }
            else
            {
                string domain = message.From.EmailAddress.Address.GetDomainFromEmail();
                SqlLiteProvider.SaveWhiteList(domain);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("WhiteListed - " + domain);
            }

            SqlLiteProvider.SaveMessageId(message.Id);
        }
        else
        {
            Console.WriteLine(DateTime.Now.ToString("g") + "Message already processed.");
        }
    }


    private static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Type '");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("exit");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("' to exit.");
        Console.WriteLine();

        Config.Instance.Load();
        SqlLiteProvider.Initialize();
        foreach (string domain in Config.Instance.WhiteList) SqlLiteProvider.SaveWhiteList(domain);


        if (!await GraphApi.CheckIfFolderExists(Config.Instance.PromotionsMailFolder))
            await GraphApi.CreateMailFolder(Config.Instance.PromotionsMailFolder);

        string promotionFolderId = await GraphApi.FindFolder(Config.Instance.PromotionsMailFolder);
        while (true)
        {
            MessageCollectionResponse? messages = await GraphApi.GetEmails();
            if (messages!.Value == null)
                continue;
            foreach (Message message in messages.Value)
                if (message.Body is { Content: not null })
                {
                    UserExited();
                    await ProcessEmail(message, promotionFolderId);
                    UserExited();
                }

            while (messages.OdataNextLink != null)
            {
                messages = await GraphApi.GetEmails(messages.OdataNextLink);
                if (messages!.Value == null)
                    continue;
                foreach (Message message in messages.Value)
                    if (message.Body is { Content: not null })
                    {
                        UserExited();
                        await ProcessEmail(message, promotionFolderId);
                        UserExited();
                    }
            }
        }
    }
}