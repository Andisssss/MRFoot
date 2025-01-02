using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DAid.Servers
{
    /// <summary>
    /// Provides extension methods for stream operations with cancellation and timeout support.
    /// </summary>
    public static class StreamExtensions
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Reads a buffer of specified length from the stream with cancellation and timeout support.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="length">Length of the buffer to create and fill.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <param name="timeout">Optional timeout in milliseconds (default: infinite).</param>
        /// <returns>A filled byte buffer.</returns>
        /// <exception cref="EndOfStreamException">Thrown when the end of the stream is reached prematurely.</exception>
        /// <exception cref="TimeoutException">Thrown if the read operation exceeds the specified timeout.</exception>
        public static async Task<byte[]> ReadAllAsync(this Stream stream, int length, CancellationToken cancellationToken, int timeout = Timeout.Infinite)
        {
            byte[] buffer = new byte[length];
            await stream.ReadAllAsync(buffer, cancellationToken, timeout);
            return buffer;
        }

        /// <summary>
        /// Fills a provided buffer from the stream with cancellation and timeout support.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="buffer">Buffer to fill.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <param name="timeout">Optional timeout in milliseconds (default: infinite).</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="EndOfStreamException">Thrown when the end of the stream is reached prematurely.</exception>
        /// <exception cref="TimeoutException">Thrown if the read operation exceeds the specified timeout.</exception>
        public static async Task<int> ReadAllAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken, int timeout = Timeout.Infinite)
        {
            if (buffer.Length == 0) return 0;

            int read = 0;
            int check = 0;

            CancellationTokenSource timeoutCts = null;
            CancellationTokenSource linkedCts = null;

            try
            {
                timeoutCts = new CancellationTokenSource(timeout);
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                while ((read += (check = await stream.ReadAsync(buffer, read, buffer.Length - read, linkedCts.Token))) < buffer.Length)
                {
                    if (check == 0) throw new EndOfStreamException("Reached the end of the stream prematurely.");
                }
            }
            catch (OperationCanceledException ex) when (timeoutCts != null && timeoutCts.IsCancellationRequested)
            {
                logger.Error($"Stream read operation timed out: {ex.Message}");
                throw new TimeoutException("Stream read operation timed out.", ex);
            }
            catch (Exception ex)
            {
                logger.Error($"Error during stream read: {ex.Message}");
                throw;
            }
            finally
            {
                timeoutCts?.Dispose();
                linkedCts?.Dispose();
            }

            return read;
        }
    }
}
