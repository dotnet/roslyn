// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    /// <summary>
    /// Helper type that abstract out JsonRpc communication with extra capability of
    /// using raw stream to move over big chunk of data
    /// </summary>
    internal abstract class JsonRpcEx : IDisposable
    {
        private readonly JsonRpc _rpc;

        private JsonRpcDisconnectedEventArgs _debuggingLastDisconnectReason;
        private string _debuggingLastDisconnectCallstack;

        public JsonRpcEx(Stream stream, object callbackTarget, bool useThisAsCallback)
        {
            Contract.Requires(stream != null);

            var target = useThisAsCallback ? this : callbackTarget;

            _rpc = new JsonRpc(new JsonRpcMessageHandler(stream, stream), target);
            _rpc.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);

            _rpc.Disconnected += OnDisconnected;
        }

        protected abstract void Dispose(bool disposing);

        public async Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {

                await _rpc.InvokeWithCancellationAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                // if any exception is thrown unrelated to cancellation, then we will rethrow the exception
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public async Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _rpc.InvokeWithCancellationAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                // if any exception is thrown unrelated to cancellation, then we will rethrow the exception
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public Task InvokeAsync(
            string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            return Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        }

        public Task<T> InvokeAsync<T>(
            string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            return Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        }

        private bool ReportUnlessCanceled(Exception ex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return true;
            }

            // save extra info using NFW
            ReportExtraInfoAsNFW(ex);

            // make it to explicitly crash to get better info
            FatalError.Report(ex);

            GC.KeepAlive(_debuggingLastDisconnectReason);
            GC.KeepAlive(_debuggingLastDisconnectCallstack);

            return Contract.FailWithReturn<bool>("shouldn't be able to reach here");
        }

        private void ReportExtraInfoAsNFW(Exception ex)
        {
            WatsonReporter.Report("RemoteHost Failed", ex, u =>
            {
                try
                {
                    // we will record dumps for all service hub processes
                    foreach (var p in Process.GetProcessesByName("ServiceHub.RoslynCodeAnalysisService32"))
                    {
                        // include all remote host processes
                        u.AddProcessDump(p.Id);
                    }

                    // include all service hub logs as well
                    var logPath = Path.Combine(Path.GetTempPath(), "servicehub", "logs");
                    if (Directory.Exists(logPath))
                    {
                        // attach all log files that are modified less than 1 day before.
                        var now = DateTime.UtcNow;
                        var oneDay = TimeSpan.FromDays(1);

                        foreach (var file in Directory.EnumerateFiles(logPath, "*.log"))
                        {
                            var lastWrite = File.GetLastWriteTimeUtc(file);
                            if (now - lastWrite > oneDay)
                            {
                                continue;
                            }

                            u.AddFile(file);
                        }
                    }
                }
                catch
                {
                    // ignore issue
                }

                // 0 means send watson
                return 0;
            });
        }

        protected void Disconnect()
        {
            _rpc.Dispose();
        }

        protected void StartListening()
        {
            // due to this issue - https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950
            // _rpc need to be explicitly started
            _rpc.StartListening();
        }

        protected virtual void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            // do nothing
            _debuggingLastDisconnectReason = e;
            _debuggingLastDisconnectCallstack = new StackTrace().ToString();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
