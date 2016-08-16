using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure; // Namespace for CloudConfigurationManager 
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Namespace for Queue storage types

namespace VizAccess
{
    internal class AzureUtil
    {
        // using environment variables to prevent private info from getting into GitHub
        private static readonly string AzureStorageAccountName = System.Environment.GetEnvironmentVariable("VizAccess_AzureStorageAccount", EnvironmentVariableTarget.Machine);
        private static readonly string AzureStorageAccountKey = System.Environment.GetEnvironmentVariable("VizAccess_AzureStorageKey", EnvironmentVariableTarget.Machine);
        private static readonly string AzureQueueName = System.Environment.GetEnvironmentVariable("VizAccess_AzureQueueName", EnvironmentVariableTarget.Machine);
        private static string AzureStorageConnectionString
        {
            get
            {
                return string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", AzureStorageAccountName, AzureStorageAccountKey);
            }
        }
        public static void SendMessage(string message)
        {
            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(AzureStorageConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            CloudQueue queue = queueClient.GetQueueReference(AzureQueueName);

            // Create the queue if it doesn't already exist
            queue.CreateIfNotExists();

            // Create a message and add it to the queue.
            CloudQueueMessage qmessage = new CloudQueueMessage(message);
            queue.AddMessage(qmessage);
        }
    }
}
