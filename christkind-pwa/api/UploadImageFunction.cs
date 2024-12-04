using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Christkind.PWA.Function
{
    public class UploadImageFunction
    {
        private readonly ILogger<UploadImageFunction> _logger;

        public UploadImageFunction(ILogger<UploadImageFunction> logger)
        {
            _logger = logger;
        }

        [Function("UploadImageFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload")] HttpRequest req)
        {
            try
            {
                var formCollection = await req.ReadFormAsync();
                var file = formCollection.Files["image"];
                var description = formCollection["description"];

                if (file == null || file.Length == 0)
                {
                    return new BadRequestObjectResult("Image file is missing.");
                }

                // Upload image to Azure Blob Storage
                var blobServiceClient = new BlobServiceClient(new Uri(Environment.GetEnvironmentVariable("AzureWebJobsStorage")), new DefaultAzureCredential());
                var blobContainerClient = blobServiceClient.GetBlobContainerClient("images");
                var blobClient = blobContainerClient.GetBlobClient(file.FileName);

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                // Store description in Azure Table Storage
                var tableServiceClient = new TableServiceClient(new Uri(Environment.GetEnvironmentVariable("TableStorageUri")), new DefaultAzureCredential());
                var tableClient = tableServiceClient.GetTableClient("ImageDescriptions");

                var entity = new TableEntity(Guid.NewGuid().ToString(), file.FileName)
                {
                    { "Description", description.ToString() }
                };

                await tableClient.AddEntityAsync(entity);

                return new OkObjectResult("Image uploaded and description stored successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image and storing description.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }


    }
}
