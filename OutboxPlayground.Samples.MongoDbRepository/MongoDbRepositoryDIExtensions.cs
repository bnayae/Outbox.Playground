using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.MongoDbRepository
{
    public static class MongoDbRepositoryDIExtensions
    {
        public static IServiceCollection AddPaymentMongoDbRepository(this IServiceCollection services, string connStr = "mongoDbConnection")
        {
            var mongoClient = new MongoClient(connStr);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase("outbox");
            services.AddSingleton(mongoDatabase);
            services.AddKeyedScoped<IPaymentRepository, PaymentRepository>("default");
            //services.AddKeyedScoped<IPaymentRepository, PaymentMultiOutboxRepository>("multi-outbox");

            return services;
        }
    }
}
