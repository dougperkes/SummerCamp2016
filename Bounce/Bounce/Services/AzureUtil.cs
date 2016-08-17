using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bounce.Services
{
    class AzureUtil
    {
        public static async void SendMessage(string message)
        {
            var credentials = new StorageCredentials(Models.StorageSettings.StorageAccountName, Models.StorageSettings.StorageAccountKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            // Parse the connection string and return a reference to the storage account.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            CloudQueue queue = queueClient.GetQueueReference(Models.StorageSettings.StorageAccountQueue);

            // Create the queue if it doesn't already exist
            //queue.CreateIfNotExists();

            // Create a message and add it to the queue.
            CloudQueueMessage qmessage = new CloudQueueMessage(message);
            await queue.AddMessageAsync(qmessage);
        }
    }
}
