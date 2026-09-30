// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    /// <summary>
    /// Retains process identities while the launcher is alive, including descendants that later
    /// become orphaned. Never selects processes by name across the machine.
    /// </summary>
    internal sealed class OwnedProcessTree : IDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<int, Process> _processes = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _tracking;

        internal Process Root { get; }

        internal OwnedProcessTree(Process root)
        {
            Root = root;
            _processes.Add(root.Id, root);
            _tracking = Task.Run(TrackAsync);
        }

        internal Process[] GetProcesses()
        {
            lock (_gate)
            {
                return _processes.Values.Where(IsAlive).ToArray();
            }
        }

        private static bool IsAlive(Process process)
        {
            try
            {
                if (process.HasExited)
                    return false;

                // Retained Process instances can outlive a PID on Unix. Revalidate the creation
                // time before using an observed descendant, not just when first discovering it.
                using var current = Process.GetProcessById(process.Id);
                return current.StartTime == process.StartTime;
            }
            catch (ArgumentException) { return false; }
            catch (Win32Exception) { return false; }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private async Task TrackAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    var parents = ProcessUtil.GetParentProcessIds();
                    lock (_gate)
                    {
                        bool added;
                        do
                        {
                            added = false;
                            foreach (var pair in parents)
                            {
                                if (_processes.ContainsKey(pair.Key) ||
                                    !_processes.TryGetValue(pair.Value, out var parent) || !IsAlive(parent))
                                {
                                    continue;
                                }

                                Process? child = null;
                                try
                                {
                                    child = Process.GetProcessById(pair.Key);
                                    // Guard against a recycled PID in a stale parent snapshot.
                                    if (child.StartTime >= parent.StartTime && IsAlive(parent))
                                    {
                                        _processes.Add(pair.Key, child);
                                        child = null;
                                        added = true;
                                    }
                                }
                                catch (ArgumentException) { }
                                catch (Win32Exception) { }
                                catch (InvalidOperationException) { }
                                finally
                                {
                                    child?.Dispose();
                                }
                            }
                        } while (added);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Unable to refresh owned process tree: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), _stop.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        internal void Kill()
        {
            // Kill the launcher tree first, then any previously observed children now orphaned.
            foreach (var process in GetProcesses())
            {
                if (IsAlive(process))
                    ProcessUtil.KillTree(process);
            }
        }

        public void Dispose()
        {
            _stop.Cancel();
            Kill();
            // WMI enumeration must not hold up timeout cleanup. Dispose after tracking has stopped.
            _ = _tracking.ContinueWith(_ =>
            {
                lock (_gate)
                {
                    foreach (var process in _processes.Values)
                    {
                        if (process != Root)
                            process.Dispose();
                    }
                }
                _stop.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
