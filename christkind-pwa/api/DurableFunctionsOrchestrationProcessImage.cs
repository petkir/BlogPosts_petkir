using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using OpenAI.Chat;

namespace Christkind.PWA.Function
{
    public static class DurableFunctionsOrchestrationProcessImage
    {
        [Function(nameof(DurableFunctionsOrchestrationProcessImage))]
        public static async Task<bool> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context, string imageUrl)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(DurableFunctionsOrchestrationProcessImage));
            var data = await context.CallActivityAsync<ImageProperties>(nameof(GetImageDescription), imageUrl);
            var result = await context.CallActivityAsync<bool>(nameof(SaveResult), data);
            return result;
        }

        [Function(nameof(GetImageDescription))]
        public static async Task<ImageProperties> GetImageDescription([ActivityTrigger] string imageUrl, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("GetImageDescription");
            logger.LogInformation("Downloading image from {imageUrl}.", imageUrl);

            // Use managed identity to authenticate and download the image from Azure Blob Storage
            var blobUri = new Uri(imageUrl);
            var blobClient = new BlobClient(blobUri, new DefaultAzureCredential());

            var response = await blobClient.DownloadAsync();
            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            string imageBase64 = Convert.ToBase64String(imageBytes);

            // Create a chat message
            var chatMessages = new ChatMessage[]
            {
            ChatMessage.CreateUserMessage("Analyze the attached image and describe whats in this image?"),
            ChatMessage.CreateSystemMessage($"<image:{imageBase64}>")
            };

            // Create a chat request
            logger.LogInformation("Sending image to OpenAI service for description.");

            var openAiApiKey = Environment.GetEnvironmentVariable("OpenAIServiceApiKey");
            var openAiClient = new OpenAI.OpenAIClient(openAiApiKey);

            var chatclient = new OpenAI.Chat.ChatClient("gpt-4o-mini", openAiApiKey);
             var chatResponse = await chatclient.CompleteChatAsync( chatMessages);

            logger.LogInformation("Received image description from OpenAI service.");
            string openAiResponseText = string.Join(' ',chatResponse.Value.Content.Select(p=> p.Text).ToArray());
            return new ImageProperties
            {
                ImageDescription = openAiResponseText,
                ImageTags = "",//TODO: make Tags request at 1st chat message or make a second or use the desciption and make the tags
                ImageTitle = imageUrl
            };
        }
    


    [Function(nameof(SaveResult))]
    public static bool SaveResult([ActivityTrigger] ImageProperties data, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("SaveResult");
        logger.LogInformation("Saving image properties to Dataverse.");

        try
        {
            var serviceClient = new ServiceClient(
                new Uri(Environment.GetEnvironmentVariable("DataVerseConnection")),
                Environment.GetEnvironmentVariable("ClientID"),
                Environment.GetEnvironmentVariable("Sec"), true
            );

            var entity = new Entity("new_imageproperties");
            entity["new_imagedescription"] = data.ImageDescription;
            entity["new_imagetags"] = data.ImageTags;
            entity["new_imagetitle"] = data.ImageTitle;

            serviceClient.Create(entity);

            logger.LogInformation("Image properties saved successfully.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while saving image properties: {ex.Message}");
            return false;
        }
    }

    [Function("DurableFunctionsOrchestrationProcessImage_HttpStart")]
    public static async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("DurableFunctionsOrchestrationProcessImage_HttpStart");

        var imageUrl = req.Query["imageUrl"];
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DurableFunctionsOrchestrationProcessImage), imageUrl);
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}

   
}
