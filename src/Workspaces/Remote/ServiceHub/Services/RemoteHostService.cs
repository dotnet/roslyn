﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote.Storage;
using Microsoft.CodeAnalysis.Remote.Telemetry;
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
    internal class RemoteHostService : ServiceHubServiceBase, IRemoteHostService
    {
        private const string LoggingFunctionIdTextFileName = "ServiceHubFunctionIds.txt";

        private string _host;
        private int _primaryInstance;

        static RemoteHostService()
        {
            // this is the very first service which will be called from client (VS)
            // we set up logger here
            RoslynLogger.SetLogger(new EtwLogger(GetLoggingChecker()));

            // Set this process's priority BelowNormal.
            // this should let us to freely try to use all resources possible without worrying about affecting
            // host's work such as responsiveness or build.
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            SetNativeDllSearchDirectories();
        }

        public RemoteHostService(Stream stream, IServiceProvider serviceProvider) :
            base(serviceProvider, stream)
        {
            // this service provide a way for client to make sure remote host is alive
            Rpc.StartListening();
        }

        public string Connect(string host, string serializedSession)
        {
            _primaryInstance = InstanceId;

            var existing = Interlocked.CompareExchange(ref _host, host, null);

            SetGlobalContext(serializedSession);

            if (existing != null && existing != host)
            {
                LogError($"{host} is given for {existing}");
            }

            // log telemetry that service hub started
            RoslynLogger.Log(FunctionId.RemoteHost_Connect, KeyValueLogMessage.Create(SetSessionInfo));

            return _host;
        }

        public async Task SynchronizePrimaryWorkspaceAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizePrimaryWorkspaceAsync, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
            {
                var solutionController = (ISolutionController)RoslynServices.SolutionService;
                await solutionController.UpdatePrimaryWorkspaceAsync(checksum, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SynchronizeGlobalAssetsAsync(Checksum[] checksums, CancellationToken cancellationToken)
        {
            using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizeGlobalAssetsAsync, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
            {
                var assets = await RoslynServices.AssetService.GetAssetsAsync<object>(checksums, cancellationToken).ConfigureAwait(false);

                foreach (var asset in assets)
                {
                    AssetStorage.TryAddGlobalAsset(asset.Item1, asset.Item2);
                }
            }
        }

        public void RegisterPrimarySolutionId(SolutionId solutionId, string storageLocation, CancellationToken cancellationToken)
        {
            var persistentStorageService = GetPersistentStorageService();
            persistentStorageService?.RegisterPrimarySolution(solutionId);
            RemotePersistentStorageLocationService.UpdateStorageLocation(solutionId, storageLocation);
        }

        public void UnregisterPrimarySolutionId(SolutionId solutionId, bool synchronousShutdown, CancellationToken cancellationToken)
        {
            var persistentStorageService = GetPersistentStorageService();
            persistentStorageService?.UnregisterPrimarySolution(solutionId, synchronousShutdown);
        }

        private static Func<FunctionId, bool> GetLoggingChecker()
        {
            try
            {
                var loggingConfigFile = Path.Combine(typeof(RemoteHostService).Assembly.Location, LoggingFunctionIdTextFileName);

                if (File.Exists(loggingConfigFile))
                {
                    var set = new HashSet<FunctionId>();

                    var functionIdType = typeof(FunctionId);
                    var functionIdStrings = File.ReadAllLines(loggingConfigFile);

                    foreach (var functionIdString in functionIdStrings)
                    {
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

                    return id => set.Contains(id);
                }
            }
            catch
            {
                // we don't care any exception here. 
                // this is for debugging and performance investigation purpose.
            }

            // if there was any kind of issue, 
            // don't log anything
            return _ => false;
        }

        private void SetSessionInfo(Dictionary<string, object> m)
        {
            m["Host"] = _host;
            m["InstanceId"] = _primaryInstance;
        }

        private static void SetGlobalContext(string serializedSession)
        {
            // set global telemetry session
            var session = GetTelemetrySession(serializedSession);
            if (session == null)
            {
                return;
            }

            // set roslyn loggers
            WatsonReporter.SetTelemetrySession(session);

            RoslynLogger.SetLogger(AggregateLogger.Create(new VSTelemetryLogger(session), RoslynLogger.GetLogger()));

            // set both handler as NFW
            FatalError.Handler = WatsonReporter.Report;
            FatalError.NonFatalHandler = WatsonReporter.Report;
        }

        private static TelemetrySession GetTelemetrySession(string serializedSession)
        {
            var session = serializedSession != null ? new TelemetrySession(serializedSession) : null;

            // actually starting the session
            session?.Start();

            return session;
        }

        private static AbstractPersistentStorageService GetPersistentStorageService()
        {
            // A bit slimy.  We just create an adhoc workspace so it will create the singleton
            // PersistentStorageService.  This service will be shared among all Workspaces we 
            // create in this process.  So updating it will be seen by all.
            var workspace = new AdhocWorkspace(RoslynServices.HostServices);
            var persistentStorageService = workspace.Services.GetService<IPersistentStorageService>() as AbstractPersistentStorageService;
            return persistentStorageService;
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

                var cookie = AddDllDirectory(AppDomain.CurrentDomain.BaseDirectory);
                if (cookie == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
            }
        }
    }
}
