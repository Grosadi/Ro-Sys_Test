using Microsoft.Extensions.Configuration;

namespace Ro_Sys_Test
{
    public static class ConfigurationHelper
    {
        private static readonly IConfiguration _configuration;

        static ConfigurationHelper()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        public static string GetConnectionString(string connectionName)
        {
            var connectionString = _configuration.GetConnectionString(connectionName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string cannot be found: {connectionName}.");
            }

            return connectionString;
        }
    }
}
