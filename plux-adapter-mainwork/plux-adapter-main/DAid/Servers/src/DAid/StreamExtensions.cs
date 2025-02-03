using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DAid.Servers
{
    public static class StreamExtensions
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const int DefaultTimeout = 5000; // Default timeout: 5 seconds

        public static async Task<byte[]> ReadAllAsync(this Stream stream, int length, CancellationToken cancellationToken, int timeout = DefaultTimeout)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

            byte[] buffer = new byte[length];
            await stream.ReadAllAsync(buffer, cancellationToken, timeout);
            return buffer;
        }

        public static async Task<int> ReadAllAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken, int timeout = DefaultTimeout)
{
    if (stream == null) throw new ArgumentNullException(nameof(stream));
    if (buffer == null) throw new ArgumentNullException(nameof(buffer));
    if (buffer.Length == 0) return 0;

    int totalRead = 0;

    var timeoutCts = new CancellationTokenSource(timeout);
    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

    try
    {
        logger.Debug("Starting stream read operation...");

        using (timeoutCts) // Explicit using block for C# 7.3
        using (linkedCts)
        {
            while (totalRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, linkedCts.Token);
                if (read == 0)
                    throw new EndOfStreamException($"Reached end of stream after reading {totalRead} bytes.");

                totalRead += read;
                logger.Debug($"Read {totalRead} bytes so far...");
            }
        }

        logger.Debug("Stream read operation completed successfully.");
    }
    catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
    {
        logger.Error($"Stream read operation timed out after {timeout} ms: {ex.Message}");
        throw new TimeoutException($"Stream read operation exceeded timeout of {timeout} ms.", ex);
    }
    catch (OperationCanceledException)
    {
        logger.Warn("Stream read operation was canceled by the caller.");
        throw;
    }
    catch (Exception ex)
    {
        logger.Error($"Error during stream read: {ex.Message}");
        throw;
    }

    return totalRead;
}

}
}
