using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;

internal class Program
{
    static async Task Main()
    {
        // Change these
        string subscriptionId = "3efa8668-cdc2-4856-a3ee-cac9a40e316d";
        string location = "westeurope"; // e.g. uksouth, westeurope, eastus
        string resourceGroupName = "rg-blob-demo-001";

        // Storage account names must be globally unique, 3-24 chars, lowercase/numbers only
        string storageAccountName = "blob" + Guid.NewGuid().ToString("N")[..12];
        string containerName = "samples";

        TokenCredential credential = new DefaultAzureCredential();
        ArmClient armClient = new ArmClient(credential, subscriptionId);

        // 1) Get subscription
        SubscriptionResource subscription = await armClient.GetDefaultSubscriptionAsync();

        // 2) Create resource group
        ResourceGroupCollection rgCollection = subscription.GetResourceGroups();
        var rgData = new ResourceGroupData(location);
        ArmOperation<ResourceGroupResource> rgLro =
            await rgCollection.CreateOrUpdateAsync(
                WaitUntil.Completed,
                resourceGroupName,
                rgData);

        ResourceGroupResource resourceGroup = rgLro.Value;
        Console.WriteLine($"Created resource group: {resourceGroup.Data.Name}");

        // 3) Create storage account
        StorageAccountCollection storageAccounts = resourceGroup.GetStorageAccounts();

        var storageSku = new StorageSku(StorageSkuName.StandardLrs);

        var storageCreateParams = new StorageAccountCreateOrUpdateContent(
            storageSku,
            StorageKind.StorageV2,
            location)
        {
            AccessTier = StorageAccountAccessTier.Hot,
            AllowBlobPublicAccess = false
        };

        ArmOperation<StorageAccountResource> storageLro =
            await storageAccounts.CreateOrUpdateAsync(
                WaitUntil.Completed,
                storageAccountName,
                storageCreateParams);

        StorageAccountResource storageAccount = storageLro.Value;
        Console.WriteLine($"Created storage account: {storageAccount.Data.Name}");

        // 4) Create a blob container using data-plane SDK + Entra auth
        Uri blobServiceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
        BlobServiceClient blobServiceClient = new BlobServiceClient(blobServiceUri, credential);

        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        Console.WriteLine($"Created blob container: {containerName}");
        Console.WriteLine($"Blob endpoint: {blobServiceUri}");
    }
}