// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Base type for Roslyn brokered services hosted in ServiceHub.
    /// </summary>
    internal abstract partial class BrokeredServiceBase : IDisposable
    {
        protected readonly TraceSource TraceLogger;
        protected readonly RemoteWorkspaceManager WorkspaceManager;

        protected readonly SolutionAssetSource SolutionAssetSource;
        protected readonly ServiceBrokerClient ServiceBrokerClient;

        // test data are only available when running tests:
        internal readonly RemoteHostTestData? TestData;

        static BrokeredServiceBase()
        {
            if (GCSettings.IsServerGC)
            {
                // Server GC runs processor-affinitized threads with high priority. To avoid interfering with other
                // applications while still allowing efficient out-of-process execution, slightly reduce the process
                // priority when using server GC.
                Process.GetCurrentProcess().TrySetPriorityClass(ProcessPriorityClass.BelowNormal);
            }

            // Make encodings that is by default present in desktop framework but not in corefx available to runtime.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if DEBUG
            // Make sure debug assertions in ServiceHub result in exceptions instead of the assertion UI
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ThrowingTraceListener());
#endif

            SetNativeDllSearchDirectories();
        }

        protected BrokeredServiceBase(in ServiceConstructionArguments arguments)
        {
            var traceSource = (TraceSource?)arguments.ServiceProvider.GetService(typeof(TraceSource));
            Contract.ThrowIfNull(traceSource);
            TraceLogger = traceSource;

            TestData = (RemoteHostTestData?)arguments.ServiceProvider.GetService(typeof(RemoteHostTestData));
            WorkspaceManager = TestData?.WorkspaceManager ?? RemoteWorkspaceManager.Default;

#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
            ServiceBrokerClient = new ServiceBrokerClient(arguments.ServiceBroker);
#pragma warning restore

            SolutionAssetSource = new SolutionAssetSource(ServiceBrokerClient);
        }

        public virtual void Dispose()
            => ServiceBrokerClient.Dispose();

        public RemoteWorkspace GetWorkspace()
            => WorkspaceManager.GetWorkspace();

        public SolutionServices GetWorkspaceServices()
            => GetWorkspace().Services.SolutionServices;

        protected void Log(TraceEventType errorType, string message)
            => TraceLogger.TraceEvent(errorType, 0, $"{GetType()}: {message}");

        protected async ValueTask<T> RunWithSolutionAsync<T>(
            Checksum solutionChecksum,
            Func<Solution, ValueTask<T>> implementation,
            CancellationToken cancellationToken)
        {
            var workspace = GetWorkspace();
            var assetProvider = workspace.CreateAssetProvider(solutionChecksum, WorkspaceManager.SolutionAssetCache, SolutionAssetSource);
            var (_, result) = await workspace.RunWithSolutionAsync(
                assetProvider,
                solutionChecksum,
                implementation,
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        protected ValueTask<T> RunServiceAsync<T>(Func<CancellationToken, ValueTask<T>> implementation, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();
            return RunServiceImplAsync(implementation, cancellationToken);
        }

        protected ValueTask<T> RunServiceAsync<T>(
            Checksum solutionChecksum, Func<Solution, ValueTask<T>> implementation, CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                c => RunWithSolutionAsync(solutionChecksum, implementation, c), cancellationToken);
        }

        internal static async ValueTask<T> RunServiceImplAsync<T>(Func<CancellationToken, ValueTask<T>> implementation, CancellationToken cancellationToken)
        {
            try
            {
                return await implementation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        protected ValueTask RunServiceAsync(Func<CancellationToken, ValueTask> implementation, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();
            return RunServiceImplAsync(implementation, cancellationToken);
        }

        protected ValueTask RunServiceAsync(
            Checksum solutionChecksum, Func<Solution, ValueTask> implementation, CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                async c =>
                {
                    await RunWithSolutionAsync(
                        solutionChecksum,
                        async s =>
                        {
                            await implementation(s).ConfigureAwait(false);
                            // bridge this void 'implementation' callback to the non-void type the underlying api needs.
                            return false;
                        }, c).ConfigureAwait(false);
                }, cancellationToken);
        }

        protected ValueTask RunServiceAsync(
            Checksum solutionChecksum1,
            Checksum solutionChecksum2,
            Func<Solution, Solution, ValueTask> implementation,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                solutionChecksum1,
                s1 => RunServiceAsync(
                    solutionChecksum2,
                    s2 => implementation(s1, s2),
                    cancellationToken),
                cancellationToken);
        }

        internal static async ValueTask RunServiceImplAsync(Func<CancellationToken, ValueTask> implementation, CancellationToken cancellationToken)
        {
            try
            {
                await implementation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// Use for on-demand retrieval of language-specific options from the client.
        /// 
        /// If the service doesn't know up-front for which languages it will need to retrieve specific options,
        /// its ICallback interface should implement <see cref="IRemoteOptionsCallback{TOptions}"/> and use this
        /// method to create the options provider to be passed to the feature implementation.
        /// </summary>
        protected static OptionsProvider<TOptions> GetClientOptionsProvider<TOptions, TCallback>(RemoteCallback<TCallback> callback, RemoteServiceCallbackId callbackId)
            where TCallback : class, IRemoteOptionsCallback<TOptions>
           => new ClientOptionsProvider<TOptions, TCallback>(callback, callbackId);

        private static void SetNativeDllSearchDirectories()
        {
            if (PlatformInformation.IsWindows)
            {
                // Set LoadLibrary search directory to %VSINSTALLDIR%\Common7\IDE so that the compiler
                // can P/Invoke to Microsoft.DiaSymReader.Native when emitting Windows PDBs.
                //
                // The AppDomain base directory is specified in VisualStudio\Setup\codeAnalysisService.servicehub.service.json
                // to be the directory where devenv.exe is -- which is exactly the directory we need to add to the search paths:
                //
                //   "appBasePath": "%VSAPPIDDIR%"
                //

                var loadDir = AppDomain.CurrentDomain.BaseDirectory!;

                try
                {
                    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
                    static extern IntPtr AddDllDirectory(string directory);

                    if (AddDllDirectory(loadDir) == IntPtr.Zero)
                    {
                        throw new Win32Exception();
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    // AddDllDirectory API might not be available on Windows 7.
                    Environment.SetEnvironmentVariable("MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH", loadDir);
                }
            }
        }
    }
}
