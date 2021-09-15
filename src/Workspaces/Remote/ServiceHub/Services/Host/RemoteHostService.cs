// Licensed to the .NET Foundation under one or more agreements.
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
        static RemoteHostService()
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

        public RemoteHostService(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
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
