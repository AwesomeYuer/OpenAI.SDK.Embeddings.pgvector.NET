// See https://aka.ms/new-console-template for more information
#if NET6_0_OR_GREATER
//using LaserCatEyes.HttpClientListener;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenAI.GPT3.Extensions;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using PgVectors.NET;
using PgVectors.Npgsql;
using System.Data;
using System.Data.Common;
//using OpenAI.Playground.TestHelpers;

var builder = new ConfigurationBuilder()
    .AddJsonFile("ApiSettings.json")
    .AddUserSecrets<Program>();

IConfiguration configuration = builder.Build();
var serviceCollection = new ServiceCollection();
serviceCollection.AddScoped(_ => configuration);

#if NET6_0_OR_GREATER
// Laser cat eyes is a tool that shows your requests and responses between OpenAI server and your client.
// Get your app key from https://lasercateyes.com for FREE and put it under ApiSettings.json or secrets.json.
// It is in Beta version, if you don't want to use it just comment out below line.
//serviceCollection.AddLaserCatEyesHttpClientListener();
#endif

serviceCollection.AddOpenAIService();
//// DeploymentId and ResourceName are only for Azure OpenAI. If you want to use Azure OpenAI services you have to set Provider type To Azure.
//serviceCollection.AddOpenAIService(options =>
//{
//    options.ProviderType = ProviderType.Azure;
//    options.ApiKey = "Test";
//    options.DeploymentId = "MyDeploymentId";
//    options.ResourceName = "MyResourceName";
//});

var serviceProvider = serviceCollection.BuildServiceProvider();
var sdk = serviceProvider.GetRequiredService<IOpenAIService>();
Console.WriteLine("Hello, World!");

var inputEmbeddings = new[]
{
      "The food was delicious and the waiter..."
    , "The food was terrible and the waiter..."
    , "麻婆豆腐是美味"
    , "coffee is not good"
    , "tea is good"
    , "coke is bad"
    , "apple is very very good"
    , "臭豆腐"
    , "屎"
    , "尿"
    , "屁"
    , "龙虾"
}
;

var i = 0;
var sql = inputEmbeddings
                    .Select
                        (
                            (x) =>
                            {
                                return
                                    $"(${++i},${++i})";
                            }
                        )
                    .Aggregate
                        (
                            (x, y) =>
                            {
                                return
                                    $@"{x},{y}";
                            }
                        );

sql = $"INSERT INTO items (content, embedding) VALUES {sql}";

var result = await sdk.Embeddings.CreateEmbedding(new EmbeddingCreateRequest()
{
      InputAsList = //new List<string> { "The quick brown fox jumped over the lazy dog." }
                    inputEmbeddings.ToList()
    , Model = Models.TextEmbeddingAdaV2
});

i = 0;
var embeddings = result
                    .Data!
                    .Select
                        (
                            (e) =>
                            {
                                return
                                    (
                                        inputEmbeddings[i ++]
                                        , new PgVector
                                                    (
                                                        e
                                                            .Embedding!
                                                            .Select
                                                                (
                                                                    (ee) =>
                                                                    {
                                                                        return (float) ee;
                                                                    }
                                                                )
                                                            .ToArray()
                                                    )
                                    );
                            }
                        )
                        ;

var connectionString = "Host=localhost;Database=pgvectors;User Id=sa;Password=!@#123QWE";

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();

using var npgsqlDataSource = dataSourceBuilder.Build();
using var connection = npgsqlDataSource.OpenConnection();

// Preserve the vectors of contents of embeddings for match
await using (var sqlCommand = new NpgsqlCommand(sql, connection))
{
    foreach (var (content , pgVector) in embeddings)
    {
        sqlCommand.Parameters.AddWithValue(content);
        sqlCommand.Parameters.AddWithValue(pgVector);
    }
    await sqlCommand.ExecuteNonQueryAsync();
}

var adHocQuery = "shit";
adHocQuery = "苹果";
adHocQuery = "佛跳墙";
adHocQuery = "尿素";
adHocQuery = "好吃的";
adHocQuery = "C#";
adHocQuery = "php";
adHocQuery = "Java";
adHocQuery = "螃蟹好吃";

Console.WriteLine($@"{nameof(adHocQuery)}: ""{adHocQuery}"" match similarity:");
Console.WriteLine();
Console.WriteLine();

result = await sdk.Embeddings.CreateEmbedding
(
    new EmbeddingCreateRequest()
    {
          Input = adHocQuery 
        , Model = Models.TextEmbeddingAdaV2
    }
);

// Query match similarity
// Query order by ascending the distance between the vector of ad-hoc query key words's embedding and the vectors of preserved contents of embeddings in database
// The distance means similarity
sql = "SELECT content FROM items ORDER BY embedding <= $1";
sql = "SELECT content FROM items ORDER BY cosine_distance(embedding,$1::vector)";
sql = "SELECT content FROM items ORDER BY embedding <-> $1::vector";

sql = "SELECT content, avg(embedding <-> $1::vector) as AverageDistance FROM items GROUP BY content ORDER BY 2" ;

var adHocQueryEmbedding = result
                                .Data!
                                .First()
                                .Embedding!
                                .Select
                                    (
                                        (x) =>
                                        {
                                            return (float) x;
                                        }
                                    )
                                .ToArray()
                                ;

await using (var sqlCommand = new NpgsqlCommand(sql, connection))
{
    sqlCommand.Parameters.AddWithValue(adHocQueryEmbedding);
    var seperator = "\t\t\t\t";
    await using (DbDataReader dataReader = await sqlCommand.ExecuteReaderAsync())
    {
        while (await dataReader.ReadAsync())
        {
            IDataRecord dataRecord = dataReader;
            var averageDistance = dataReader.GetDouble(dataRecord.GetOrdinal("AverageDistance"));
            var preservedContent = dataReader.GetString(dataRecord.GetOrdinal("content"));
            Console
                .WriteLine
                        (
                            $@"{nameof(adHocQuery)}: ""{adHocQuery}"", {nameof(averageDistance)}: [{averageDistance}]{seperator}, {nameof(preservedContent)}: ""{preservedContent}"""
                        );
        }
    }
}
