using System;
using System.Threading;
using System.Threading.Tasks;
using DAid.Clients;
using DAid.Servers;

namespace DAid.Clients
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing DAid system...");

            using (var cancellationTokenSource = new CancellationTokenSource()) // Traditional `using`
            {
                // Initialize server
                var server = new Server();

                // Start server in a background task
                var serverTask = server.StartProcessingAsync(cancellationTokenSource.Token);

                // Initialize and start client
                var client = new Client(server);
                await client.StartAsync(cancellationTokenSource.Token);

                // Signal the server to stop after the client exits
                cancellationTokenSource.Cancel();
                await serverTask;
            }

            Console.WriteLine("DAid system has stopped.");
        }
    }
}
