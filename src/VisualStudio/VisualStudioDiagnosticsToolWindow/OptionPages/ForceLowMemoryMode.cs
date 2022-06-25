// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    internal sealed class ForceLowMemoryMode
    {
        private const string FeatureName = "ForceLowMemoryMode";

        public static readonly Option2<bool> Enabled = new(FeatureName, "Enabled", defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\ForceLowMemoryMode\Enabled"));

        public static readonly Option2<int> SizeInMegabytes = new(FeatureName, "SizeInMegabytes", defaultValue: 500,
            storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\ForceLowMemoryMode\SizeInMegabytes"));

        private readonly IGlobalOptionService _globalOptions;
        private MemoryHogger? _hogger;

        public ForceLowMemoryMode(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;

            globalOptions.OptionChanged += Options_OptionChanged;

            RefreshFromSettings();
        }

        private void Options_OptionChanged(object sender, OptionChangedEventArgs e)
        {
            if (e.Option.Feature == FeatureName)
            {
                RefreshFromSettings();
            }
        }

        private void RefreshFromSettings()
        {
            var enabled = _globalOptions.GetOption(Enabled);

            if (_hogger != null)
            {
                _hogger.Cancel();
                _hogger = null;
            }

            if (enabled)
            {
                _hogger = new MemoryHogger();
                _ = _hogger.PopulateAndMonitorAsync(_globalOptions.GetOption(SizeInMegabytes));
            }
        }

        private class MemoryHogger
        {
            private const int BlockSize = 1024 * 1024; // megabyte blocks
            private const int MonitorDelay = 10000; // 10 seconds

            private readonly List<byte[]> _blocks = new List<byte[]>();
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            public MemoryHogger()
            {
            }

            public int Count
            {
                get { return _blocks.Count; }
            }

            public void Cancel()
            {
                _cancellationTokenSource.Cancel();
            }

            public Task PopulateAndMonitorAsync(int size)
            {
                // run on background thread
                return Task.Factory.StartNew(() => this.PopulateAndMonitorWorkerAsync(size), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            }

            private async Task PopulateAndMonitorWorkerAsync(int size)
            {
                try
                {
                    try
                    {
                        for (var n = 0; n < size; n++)
                        {
                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                            var block = new byte[BlockSize];

                            // initialize block bits (so the memory actually gets allocated.. silly runtime!)
                            for (var i = 0; i < BlockSize; i++)
                            {
                                block[i] = 0xFF;
                            }

                            _blocks.Add(block);

                            // don't hog the thread
                            await Task.Yield();
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                    }

                    // monitor memory to keep it paged in
                    while (true)
                    {
                        _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                        try
                        {
                            // access all block bytes
                            for (var b = 0; b < _blocks.Count; b++)
                            {
                                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                                var block = _blocks[b];

                                byte tmp;
                                for (var i = 0; i < block.Length; i++)
                                {
                                    tmp = block[i];
                                }

                                // don't hog the thread
                                await Task.Yield();
                            }
                        }
                        catch (OutOfMemoryException)
                        {
                        }

                        await Task.Delay(MonitorDelay, _cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    _blocks.Clear();

                    // force garbage collection
                    for (var i = 0; i < 5; i++)
                    {
                        GC.Collect(GC.MaxGeneration);
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
        }
    }
}
