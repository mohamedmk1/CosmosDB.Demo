using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetSdkDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await DeleteDocument("Families", "Families");
        }

        #region Working with database

        private async static Task ViewDatabases()
        {
            var iterator = Shared.Client.GetDatabaseQueryIterator<DatabaseProperties>();
            var databases = await iterator.ReadNextAsync();

            foreach (var db in databases)
            {
                Console.WriteLine($"Database: {db.Id}, Last modified: {db.LastModified}");
            }

            Console.ReadLine();
        }

        private async static Task CreateDatabase(string databaseName)
        {
            var result = await Shared.Client.CreateDatabaseAsync(databaseName);
            var database = result.Resource;

            Console.WriteLine($"Database: {database.Id}, Last modified: {database.LastModified}");
            Console.ReadLine();
        }

        private async static Task DeleteDatabase(string databaseName)
        {
            var result = await Shared.Client.GetDatabase(databaseName).DeleteAsync();
            var database = result.Resource;

            Console.ReadLine();
        }
        #endregion

        #region Working with containers
        private async static Task ViewContainers()
        {
            string databaseName = "Families";
            var currentDatabase = Shared.Client.GetDatabase(databaseName);
            var iterator = currentDatabase.GetContainerQueryIterator<ContainerProperties>();
            var containers = await iterator.ReadNextAsync();

            var count = 0;
            foreach (var container in containers)
            {
                count++;
                Console.WriteLine($"Container #{count}");
                await ViewContainer(container, databaseName);
            }
            Console.WriteLine($"Total containers {count}");
        }

        private async static Task ViewContainer(ContainerProperties containerPropreties, string databaseName)
        {
            Console.WriteLine($"   Container ID: {containerPropreties.Id}");
            Console.WriteLine($"   Container Last modified: {containerPropreties.LastModified}");
            Console.WriteLine($"   Container Parttion key: {containerPropreties.PartitionKeyPath}");

            var container = Shared.Client.GetContainer(databaseName, containerPropreties.Id);
            var throughput = await container.ReadThroughputAsync();
            Console.WriteLine($"   Throughput: {throughput}");
        }

        private async static Task CreateContainer(string databaseId, string containerId, int throughput = 400, string partitionKey = "/partitionKey")
        {
            var database = Shared.Client.GetDatabase(databaseId);
            var containerDef = new ContainerProperties
            {
                Id = containerId,
                PartitionKeyPath = partitionKey,
            };

            var result = await database.CreateContainerAsync(containerDef, throughput);

            Console.WriteLine($"Creteated new container {result.Resource.Id}");
        }

        private async static Task DeleteContainer(string databaseId, string containerId)
        {
            var container = Shared.Client.GetContainer(databaseId, containerId);

            await container.DeleteContainerAsync();

            Console.WriteLine($"Deleted container {containerId} for database {databaseId}");
        }
        #endregion

        #region Working with documents
        private async static Task CreateDocument(string databaseId, string containerId)
        {
            var container = Shared.Client.GetContainer(databaseId, containerId);
            dynamic documentDynamic = new
            {
                id = Guid.NewGuid(),
                name = "New customer",
                location = new
                {
                    state = "New York",
                    country = "United States",
                    city = "Brooklyn"
                }
            };

            await container.CreateItemAsync(documentDynamic);
            Console.WriteLine($"Created new document {documentDynamic.id}");
        }

        private async static Task ReplaceDocument(string databaseId, string containerId)
        {
            var container = Shared.Client.GetContainer(databaseId, containerId);
            var sql = "SELECT * FROM c WHERE c.name = 'New customer'";
            var documents = (await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync()).ToList();

            if (documents.Count == 1)
            {
                foreach (var document in documents)
                {
                    document.name = "Test";
                    var result = await container.ReplaceItemAsync(document, (string)document.id);
                    Console.WriteLine($"Updated document 'Name': {document.Name}");
                }
            }
        }

        private async static Task DeleteDocument(string databaseId, string containerId)
        {
            var container = Shared.Client.GetContainer(databaseId, containerId);
            var sql = "SELECT * FROM c WHERE c.name = 'Test'";
            var documents = (await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync()).ToList();

            if (documents.Count == 1)
            {
                foreach (var document in documents)
                {
                    string id = document.id;
                    string pk = document.location.state;
                    var result = await container.DeleteItemAsync<dynamic>(id, new PartitionKey(pk));
                    Console.WriteLine($"Deleted document 'id': {id}");
                }
            }

        } 
        #endregion
    }
}
