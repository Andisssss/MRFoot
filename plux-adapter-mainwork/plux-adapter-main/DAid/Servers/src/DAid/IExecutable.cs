using System.Threading;
using System.Threading.Tasks;

namespace DAid.Servers
{
    /// <summary>
    /// Represents an executable server component with start and stop lifecycle methods.
    /// </summary>
    public interface IExecutable
    {
        /// <summary>
        /// Starts the execution loop asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>An integer indicating the loop's exit reason.</returns>
        Task<int> StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the execution loop. This is thread-safe.
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets the current status of the executable.
        /// </summary>
        string Status { get; }
    }
}
