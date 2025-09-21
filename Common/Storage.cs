using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Storage
    {
        private static string ResolveConnString()
        {
            // 1) CloudConfigurationManager (radi i u web.config i u .cscfg)
            var cs = CloudConfigurationManager.GetSetting("DataConnectionString");

            // 2) Web.config appSettings fallback
            if (string.IsNullOrWhiteSpace(cs))
                cs = ConfigurationManager.AppSettings["DataConnectionString"];

            // 3) Env var (ako je koristiš)
            if (string.IsNullOrWhiteSpace(cs))
                cs = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            // 4) Safe default za lokalni rad (Azurite / Storage Emulator)
            if (string.IsNullOrWhiteSpace(cs))
                cs = "UseDevelopmentStorage=true";

            return cs;
        }

        public static CloudStorageAccount Account =>
            CloudStorageAccount.Parse(ResolveConnString());

        public static CloudTable GetTable(string name)
        {
            var client = Account.CreateCloudTableClient();
            var t = client.GetTableReference(name);
            t.CreateIfNotExists();
            return t;
        }

        public static CloudQueue GetQueue(string name)
        {
            var client = Account.CreateCloudQueueClient();
            var q = client.GetQueueReference(name);
            q.CreateIfNotExists();
            return q;
        }

        public static CloudBlobContainer GetContainer(string name)
        {
            var client = Account.CreateCloudBlobClient();
            var container = client.GetContainerReference(name);

            if (container.CreateIfNotExists())
            {
                var permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                container.SetPermissions(permissions);
            }

            return container;
        }
    }
}


