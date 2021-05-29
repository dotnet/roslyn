﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Service that client will connect to to make service hub alive even when there is
    /// no other people calling service hub.
    /// 
    /// basically, this is used to manage lifetime of the service hub.
    /// </summary>
    [Obsolete("Supports non-brokered services")]
    internal partial class RemoteHostService : ServiceBase, IRemoteHostService, IAssetSource
    {
        private static readonly TimeSpan s_reportInterval = TimeSpan.FromMinutes(2);
        private readonly CancellationTokenSource _shutdownCancellationSource;

        // it is saved here more on debugging purpose.
        private static Func<FunctionId, bool> s_logChecker = _ => false;

#if DEBUG
#pragma warning disable IDE0052 // Remove unread private members
        private PerformanceReporter? _performanceReporter;
#pragma warning restore
#endif

        static RemoteHostService()
        {
            if (GCSettings.IsServerGC)
            {
                // Server GC runs processor-affinitized threads with high priority. To avoid interfering with other
                // applications while still allowing efficient out-of-process execution, slightly reduce the process
                // priority when using server GC.
                Process.GetCurrentProcess().TrySetPriorityClass(ProcessPriorityClass.BelowNormal);
            }

            // this is the very first service which will be called from client (VS)
            // we set up logger here
            RoslynLogger.SetLogger(new EtwLogger(s_logChecker));

#if DEBUG
            // Make sure debug assertions in ServiceHub result in exceptions instead of the assertion UI
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ThrowingTraceListener());
#endif

            SetNativeDllSearchDirectories();
        }

        public RemoteHostService(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            _shutdownCancellationSource = new CancellationTokenSource();

            // this service provide a way for client to make sure remote host is alive
            StartService();
        }

        /// <summary>
        /// Remote API. Initializes ServiceHub process global state.
        /// </summary>
        public void InitializeGlobalState(int uiCultureLCID, int cultureLCID, CancellationToken cancellationToken)
        {
            RunService(() =>
            {
                // initialize global asset storage
                WorkspaceManager.InitializeAssetSource(this);

                if (uiCultureLCID != 0)
                {
                    EnsureCulture(uiCultureLCID, cultureLCID);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API. Initializes ServiceHub process global state.
        /// </summary>
        public void InitializeTelemetrySession(int hostProcessId, string serializedSession, CancellationToken cancellationToken)
        {
            RunService(() =>
            {
                var services = GetWorkspace().Services;

                var telemetryService = (RemoteWorkspaceTelemetryService)services.GetRequiredService<IWorkspaceTelemetryService>();
                var telemetrySession = new TelemetrySession(serializedSession);
                telemetrySession.Start();

                telemetryService.InitializeTelemetrySession(telemetrySession);
                telemetryService.RegisterUnexpectedExceptionLogger(Logger);

                // log telemetry that service hub started
                RoslynLogger.Log(FunctionId.RemoteHost_Connect, KeyValueLogMessage.Create(m =>
                {
                    m["Host"] = hostProcessId;
                    m["InstanceId"] = InstanceId;
                }));

#if DEBUG
                // start performance reporter
                var diagnosticAnalyzerPerformanceTracker = services.GetService<IPerformanceTrackerService>();
                if (diagnosticAnalyzerPerformanceTracker != null)
                {
                    var globalOperationNotificationService = services.GetService<IGlobalOperationNotificationService>();
                    _performanceReporter = new PerformanceReporter(Logger, telemetrySession, diagnosticAnalyzerPerformanceTracker, globalOperationNotificationService, s_reportInterval, _shutdownCancellationSource.Token);
                }
#endif
            }, cancellationToken);
        }

        async ValueTask<ImmutableArray<(Checksum, object)>> IAssetSource.GetAssetsAsync(int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            return await RunServiceAsync(() =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_GetAssetsAsync, (serviceId, checksums) => $"{serviceId} - {Checksum.GetChecksumsLogInfo(checksums)}", scopeId, checksums, cancellationToken))
                {
                    return EndPoint.InvokeAsync(
                        nameof(IRemoteHostServiceCallback.GetAssetsAsync),
                        new object[] { scopeId, checksums.ToArray() },
                        (stream, cancellationToken) => Task.FromResult(RemoteHostAssetSerialization.ReadData(stream, scopeId, checksums, serializerService, cancellationToken)),
                        cancellationToken);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        // TODO: remove (https://github.com/dotnet/roslyn/issues/43477)
        async ValueTask<bool> IAssetSource.IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            return await RunServiceAsync(() =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_IsExperimentEnabledAsync, experimentName, cancellationToken))
                {
                    return EndPoint.InvokeAsync<bool>(
                        nameof(IRemoteHostServiceCallback.IsExperimentEnabledAsync),
                        new object[] { experimentName },
                        cancellationToken);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public void SetLoggingFunctionIds(List<string> loggerTypes, List<string> functionIds, CancellationToken cancellationToken)
        {
            RunService(() =>
            {
                var functionIdType = typeof(FunctionId);

                var set = new HashSet<FunctionId>();
                foreach (var functionIdString in functionIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        set.Add((FunctionId)Enum.Parse(functionIdType, functionIdString.Trim(), ignoreCase: true));
                    }
                    catch
                    {
                        // unknown functionId, move on
                        continue;
                    }
                }

                Func<FunctionId, bool> logChecker = id => set.Contains(id);
                lock (s_logChecker)
                {
                    // holding onto it for debugging purpose
                    s_logChecker = logChecker;
                }

                // we only support 2 types of loggers
                SetRoslynLogger(loggerTypes, () => new EtwLogger(logChecker));
                SetRoslynLogger(loggerTypes, () => new TraceLogger(logChecker));

            }, cancellationToken);
        }

        private static void SetRoslynLogger<T>(List<string> loggerTypes, Func<T> creator) where T : ILogger
        {
            if (loggerTypes.Contains(typeof(T).Name))
            {
                RoslynLogger.SetLogger(AggregateLogger.AddOrReplace(creator(), RoslynLogger.GetLogger(), l => l is T));
            }
            else
            {
                RoslynLogger.SetLogger(AggregateLogger.Remove(RoslynLogger.GetLogger(), l => l is T));
            }
        }

        private static void EnsureCulture(int uiCultureLCID, int cultureLCID)
        {
            // this follows what VS does
            // http://index/?leftProject=Microsoft.VisualStudio.Platform.AppDomainManager&leftSymbol=wok83tw8yxy7&file=VsAppDomainManager.cs&line=106
            try
            {
                // set default culture for Roslyn OOP
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(uiCultureLCID);
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(cultureLCID);
            }
            catch (Exception ex) when (ExpectedCultureIssue(ex))
            {
                // ignore expected culture issue
            }
        }

        private static bool ExpectedCultureIssue(Exception ex)
        {
            // report exception
            WatsonReporter.ReportNonFatal(ex);

            // ignore expected exception
            return ex is ArgumentOutOfRangeException || ex is CultureNotFoundException;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr AddDllDirectory(string directory);

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
