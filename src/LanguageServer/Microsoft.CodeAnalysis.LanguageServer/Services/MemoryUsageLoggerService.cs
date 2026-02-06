// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

/// <summary>
/// A service that periodically logs memory usage information for diagnostic purposes.
/// Logs total committed memory, heap size (allocated), and free memory not released to OS.
/// </summary>
internal sealed class MemoryUsageLoggerService : IDisposable
{
    private static readonly TimeSpan LoggingInterval = TimeSpan.FromSeconds(30);

    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public MemoryUsageLoggerService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MemoryUsageLoggerService>();
        _cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(() => LogMemoryUsagePeriodicAsync(_cancellationTokenSource.Token));
    }

    private async Task LogMemoryUsagePeriodicAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var gcMemoryInfo = GC.GetGCMemoryInfo();

                // Total committed memory - the total memory committed by the managed heap (reserved from OS)
                var totalCommittedBytes = gcMemoryInfo.TotalCommittedBytes;

                // Heap size - the actual allocated memory in the managed heap
                var heapSizeBytes = gcMemoryInfo.HeapSizeBytes;

                // Free memory not released to OS = Committed - HeapSize
                // This represents memory that the CLR has committed but is not currently in use
                var freeNotReleasedBytes = totalCommittedBytes - heapSizeBytes;

                _logger.LogInformation(
                    "Memory usage: Total Committed={TotalCommittedMB:F2}MB, Allocated={AllocatedMB:F2}MB, Free (not released to OS)={FreeMB:F2}MB",
                    BytesToMegabytes(totalCommittedBytes),
                    BytesToMegabytes(heapSizeBytes),
                    BytesToMegabytes(freeNotReleasedBytes));

                await Task.Delay(LoggingInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while logging memory usage");
            }
        }
    }

    private static double BytesToMegabytes(long bytes)
        => bytes / (1024.0 * 1024.0);

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        // Don't wait synchronously - the cancellation token will signal the task to stop
        // and it will complete on its own. Synchronous waiting can cause deadlocks.
        _cancellationTokenSource.Dispose();
    }
}
