// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote.Storage;
using Microsoft.CodeAnalysis.Storage;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Service that client will connect to to make service hub alive even when there is
    /// no other people calling service hub.
    /// 
    /// basically, this is used to manage lifetime of the service hub.
    /// </summary>
    internal class RemoteHostService : ServiceHubServiceBase
    {
        private const string LoggingFunctionIdTextFileName = "ServiceHubFunctionIds.txt";

        private string _host;

        static RemoteHostService()
        {
            // this is the very first service which will be called from client (VS)
            // we set up logger here
            RoslynLogger.SetLogger(new EtwLogger(GetLoggingChecker()));

            // Set this process's priority BelowNormal.
            // this should let us to freely try to use all resources possible without worrying about affecting
            // host's work such as responsiveness or build.
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }

        public RemoteHostService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
            // this service provide a way for client to make sure remote host is alive
        }

        public string Connect(string host)
        {
            var existing = Interlocked.CompareExchange(ref _host, host, null);

            if (existing != null && existing != host)
            {
                LogError($"{host} is given for {existing}");
            }

            return _host;
        }

        public async Task SynchronizeAsync(byte[] solutionChecksum)
        {
            var checksum = new Checksum(solutionChecksum);

            using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_Synchronize, c => c.ToString(), checksum, CancellationToken))
            {
                try
                {
                    // cause all assets belong to the given solution to sync to remote host
                    await RoslynServices.AssetService.SynchronizeSolutionAssetsAsync(checksum, CancellationToken).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // stream to send over assets has closed before we
                    // had chance to check cancellation
                }
                catch (OperationCanceledException)
                {
                    // rpc connection has closed.
                    // this can happen if client side cancelled the
                    // operation
                }
            }
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

        #region PersistentStorageService messages

        public void PersistentStorageService_RegisterPrimarySolutionId(
            byte[] solutionIdGuidBytes, string solutionIdDebugName, byte[] solutionChecksum)
        {
            var solutionId = CreateSolutionId(solutionIdGuidBytes, solutionIdDebugName);

            var persistentStorageService = GetPersistentStorageService();
            persistentStorageService?.RegisterPrimarySolution(solutionId);
        }

        private static PersistentStorageService GetPersistentStorageService()
        {
            // A bit slimy.  We just create an adhoc workspace so it will create the singleton
            // PersistentStorageService.  This service will be shared among all Workspaces we 
            // create in this process.  So updating it will be seen by all.
            var workspace = new AdhocWorkspace(RoslynServices.HostServices);
            var persistentStorageService = workspace.Services.GetService<IPersistentStorageService>() as PersistentStorageService;
            return persistentStorageService;
        }

        public void PersistentStorageService_UnregisterPrimarySolutionId(
            byte[] solutionIdGuidBytes, string solutionIdDebugName, bool synchronousShutdown, byte[] solutionChecksum)
        {
            var solutionId = CreateSolutionId(solutionIdGuidBytes, solutionIdDebugName);
            var persistentStorageService = GetPersistentStorageService();
            persistentStorageService?.UnregisterPrimarySolution(solutionId, synchronousShutdown);
        }

        public void PersistentStorageService_UpdateSolutionIdStorageLocation(
            byte[] solutionIdGuidBytes, string solutionIdDebugName, string storageLocation, byte[] solutionChecksum)
        {
            var solutionId = CreateSolutionId(solutionIdGuidBytes, solutionIdDebugName);
            RemotePersistentStorageLocationService.UpdateStorageLocation(
                solutionId, storageLocation);
        }

        private static SolutionId CreateSolutionId(byte[] solutionIdGuidBytes, string solutionIdDebugName)
        {
            return SolutionId.CreateFromSerialized(
                new Guid(solutionIdGuidBytes), solutionIdDebugName);
        }

        #endregion
    }
}