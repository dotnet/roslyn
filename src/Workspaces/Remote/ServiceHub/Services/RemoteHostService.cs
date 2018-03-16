// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Services;
using Microsoft.CodeAnalysis.Remote.Storage;
using Microsoft.CodeAnalysis.Storage;
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
    internal partial class RemoteHostService : ServiceHubServiceBase, IRemoteHostService
    {
        private readonly static TimeSpan s_reportInterval = TimeSpan.FromMinutes(2);

        // it is saved here more on debugging purpose.
        private static Func<FunctionId, bool> s_logChecker = _ => false;

        private string _host;
        private int _primaryInstance;
        private PerformanceReporter _performanceReporter;

        static RemoteHostService()
        {
            // this is the very first service which will be called from client (VS)
            // we set up logger here
            RoslynLogger.SetLogger(new EtwLogger(s_logChecker));

            SetNativeDllSearchDirectories();
        }

        public RemoteHostService(Stream stream, IServiceProvider serviceProvider) :
            base(serviceProvider, stream)
        {
            // this service provide a way for client to make sure remote host is alive
            Rpc.StartListening();
        }

        public string Connect(string host, int uiCultureLCID, int cultureLCID, string serializedSession, CancellationToken cancellationToken)
        {
            return RunService(token =>
            {
                token.ThrowIfCancellationRequested();

                _primaryInstance = InstanceId;

                var existing = Interlocked.CompareExchange(ref _host, host, null);

                SetGlobalContext(uiCultureLCID, cultureLCID, serializedSession);

                if (existing != null && existing != host)
                {
                    LogError($"{host} is given for {existing}");
                }

                // log telemetry that service hub started
                RoslynLogger.Log(FunctionId.RemoteHost_Connect, KeyValueLogMessage.Create(SetSessionInfo));

                // serializedSession will be null for testing
                if (serializedSession != null)
                {
                    // Set this process's priority BelowNormal.
                    // this should let us to freely try to use all resources possible without worrying about affecting
                    // host's work such as responsiveness or build.
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                }

                return _host;
            }, cancellationToken);
        }

        public Task SynchronizePrimaryWorkspaceAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizePrimaryWorkspaceAsync, Checksum.GetChecksumLogInfo, checksum, token))
                {
                    var solutionController = (ISolutionController)RoslynServices.SolutionService;
                    await solutionController.UpdatePrimaryWorkspaceAsync(checksum, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task SynchronizeGlobalAssetsAsync(Checksum[] checksums, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizeGlobalAssetsAsync, Checksum.GetChecksumsLogInfo, checksums, token))
                {
                    var assets = await RoslynServices.AssetService.GetAssetsAsync<object>(checksums, token).ConfigureAwait(false);

                    foreach (var asset in assets)
                    {
                        AssetStorage.TryAddGlobalAsset(asset.Item1, asset.Item2);
                    }
                }
            }, cancellationToken);
        }

        public void UpdateSolutionStorageLocation(SolutionId solutionId, string storageLocation, CancellationToken cancellationToken)
        {
            RunService(_ =>
            {
                var persistentStorageService = GetPersistentStorageService();
                persistentStorageService.UpdateStorageLocation(solutionId, storageLocation);
            }, cancellationToken);
        }

        public void OnGlobalOperationStarted(string unused)
        {
            RunService(_ =>
            {
                var globalOperationNotificationService = GetGlobalOperationNotificationService();
                globalOperationNotificationService?.OnStarted();
            }, CancellationToken.None);
        }

        public void OnGlobalOperationStopped(IReadOnlyList<string> operations, bool cancelled)
        {
            RunService(_ =>
            {
                var globalOperationNotificationService = GetGlobalOperationNotificationService();
                globalOperationNotificationService?.OnStopped(operations, cancelled);
            }, CancellationToken.None);
        }

        public void SetLoggingFunctionIds(List<string> loggerTypes, List<string> functionIds, CancellationToken cancellationToken)
        {
            RunService(token =>
            {
                var functionIdType = typeof(FunctionId);

                var set = new HashSet<FunctionId>();
                foreach (var functionIdString in functionIds)
                {
                    token.ThrowIfCancellationRequested();

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

        private void SetSessionInfo(Dictionary<string, object> m)
        {
            m["Host"] = _host;
            m["InstanceId"] = _primaryInstance;
        }

        private void SetGlobalContext(int uiCultureLCID, int cultureLCID, string serializedSession)
        {
            // set global telemetry session
            var session = GetTelemetrySession(serializedSession);
            if (session == null)
            {
                return;
            }

            EnsureCulture(uiCultureLCID, cultureLCID);

            // set roslyn loggers
            WatsonReporter.SetTelemetrySession(session);

            RoslynLogger.SetLogger(AggregateLogger.Create(new VSTelemetryLogger(session), RoslynLogger.GetLogger()));

            // set both handler as NFW
            FatalError.Handler = WatsonReporter.Report;
            FatalError.NonFatalHandler = WatsonReporter.Report;

            // start performance reporter
            var diagnosticAnalyzerPerformanceTracker = SolutionService.PrimaryWorkspace.Services.GetService<IPerformanceTrackerService>();
            if (diagnosticAnalyzerPerformanceTracker != null)
            {
                _performanceReporter = new PerformanceReporter(Logger, diagnosticAnalyzerPerformanceTracker, s_reportInterval, ShutdownCancellationToken);
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
            WatsonReporter.Report(ex);

            // ignore expected exception
            return ex is ArgumentOutOfRangeException || ex is CultureNotFoundException;
        }

        private static TelemetrySession GetTelemetrySession(string serializedSession)
        {
            var session = serializedSession != null ? new TelemetrySession(serializedSession) : null;

            // actually starting the session
            session?.Start();

            return session;
        }

        private static RemotePersistentStorageLocationService GetPersistentStorageService()
        {
            return (RemotePersistentStorageLocationService)SolutionService.PrimaryWorkspace.Services.GetService<IPersistentStorageLocationService>();
        }

        private RemoteGlobalOperationNotificationService GetGlobalOperationNotificationService()
        {
            var workspace = SolutionService.PrimaryWorkspace;
            var notificationService = workspace.Services.GetService<IGlobalOperationNotificationService>() as RemoteGlobalOperationNotificationService;
            return notificationService;
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
                // The AppDomain base directory is specified in VisualStudio\Setup.Next\codeAnalysisService.servicehub.service.json
                // to be the directory where devenv.exe is -- which is exactly the directory we need to add to the search paths:
                //
                //   "appBasePath": "%VSAPPIDDIR%"
                //

                var loadDir = AppDomain.CurrentDomain.BaseDirectory;

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
