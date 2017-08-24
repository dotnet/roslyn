// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
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
                // any exception can be thrown from StreamJsonRpc if JsonRpc is disposed in the middle of read/write.
                // until we move to newly added cancellation support in JsonRpc, we will catch exception and translate to
                // cancellation exception here. if any exception is thrown unrelated to cancellation, then we will rethrow
                // the exception
                cancellationToken.ThrowIfCancellationRequested();

                // this is to make us not crash. we should remove this once we figure out
                // what is causing this
                ThrowOwnCancellationToken();
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
                // any exception can be thrown from StreamJsonRpc if JsonRpc is disposed in the middle of read/write.
                // until we move to newly added cancellation support in JsonRpc, we will catch exception and translate to
                // cancellation exception here. if any exception is thrown unrelated to cancellation, then we will rethrow
                // the exception
                cancellationToken.ThrowIfCancellationRequested();

                // this is to make us not crash. we should remove this once we figure out
                // what is causing this
                ThrowOwnCancellationToken();
                return Contract.FailWithReturn<T>("can't reach here");
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

        // these are for debugging purpose. once we find out root cause of the issue
        // we will remove these.
        private static JsonRpcDisconnectedEventArgs s_debuggingLastDisconnectReason;
        private static string s_debuggingLastDisconnectCallstack;

        private bool ReportUnlessCanceled(Exception ex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return true;
            }

            if (ex is OperationCanceledException && ((IDisposableObservable)_rpc).IsDisposed)
            {
                // JsonRpc will throw its own cancellation token if connection it is attached to
                // is going away. we can get into this sitatuion when VS is shutting down since we dispose
                // all connections that are open.

                // return false so that we let cancellation from JsonRpc to propagate. we do this
                // since it is one of safest way to gracefully shutdown the call. since Host is going
                // away we are fine as long as there is no crash/NFW/Infobar
                return false;
            }

            s_debuggingLastDisconnectReason = _debuggingLastDisconnectReason;
            s_debuggingLastDisconnectCallstack = _debuggingLastDisconnectCallstack;

            // send NFW to figure out why this is happening
            ReportExtraInfoAsNFW(ex);

            GC.KeepAlive(_debuggingLastDisconnectReason);
            GC.KeepAlive(_debuggingLastDisconnectCallstack);

            return true;
        }

        private static bool s_reported = false;

        /// <summary>
        /// Show info bar and throw its own cancellation exception until 
        /// we figure out this issue.
        /// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/453544
        /// 
        /// the issue is basically we are getting unexpected exception from InvokeAsync
        /// and we don't know exactly why that is happening.
        /// </summary>
        private void ThrowOwnCancellationToken()
        {
            if (CodeAnalysis.PrimaryWorkspace.Workspace != null && !s_reported)
            {
                // do not report it multiple times
                s_reported = true;

                // use info bar to show warning to users
                CodeAnalysis.PrimaryWorkspace.Workspace.Services.GetService<IErrorReportingService>()?.ShowGlobalErrorInfo(
                    ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio);
            }

            // create its own cancellation token and throw it
            using (var ownCancellationSource = new CancellationTokenSource())
            {
                ownCancellationSource.Cancel();
                ownCancellationSource.Token.ThrowIfCancellationRequested();
            }
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
