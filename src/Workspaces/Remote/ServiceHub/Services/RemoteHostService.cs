// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Serialization;
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
    internal partial class RemoteHostService : ServiceBase, IRemoteHostService, IAssetSource
    {
        private static readonly TimeSpan s_reportInterval = TimeSpan.FromMinutes(2);
        private readonly CancellationTokenSource _shutdownCancellationSource;

        // it is saved here more on debugging purpose.
        private static Func<FunctionId, bool> s_logChecker = _ => false;

#pragma warning disable IDE0052 // Remove unread private members
        private PerformanceReporter? _performanceReporter;
#pragma warning restore

        static RemoteHostService()
        {
            // this is the very first service which will be called from client (VS)
            // we set up logger here
            RoslynLogger.SetLogger(new EtwLogger(s_logChecker));

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
        public void InitializeGlobalState(string host, int uiCultureLCID, int cultureLCID, string? serializedSession, CancellationToken cancellationToken)
        {
            RunService(() =>
            {
                // initialize global asset storage
                AssetStorage.Initialize(this);

                // serializedSession may be null for testing
                if (serializedSession != null)
                {
                    SetGlobalContext(uiCultureLCID, cultureLCID, serializedSession);
                }

                // log telemetry that service hub started
                RoslynLogger.Log(FunctionId.RemoteHost_Connect, KeyValueLogMessage.Create(m =>
                {
                    m["Host"] = host;
                    m["InstanceId"] = InstanceId;
                }));

                if (serializedSession != null)
                {
                    // Set this process's priority BelowNormal.
                    // this should let us to freely try to use all resources possible without worrying about affecting
                    // host's work such as responsiveness or build.
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                }
            }, cancellationToken);
        }

        Task<ImmutableArray<(Checksum, object)>> IAssetSource.GetAssetsAsync(int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            return RunServiceAsync(() =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_GetAssetsAsync, (serviceId, checksums) => $"{serviceId} - {Checksum.GetChecksumsLogInfo(checksums)}", scopeId, checksums, cancellationToken))
                {
                    return EndPoint.InvokeAsync(
                        nameof(IRemoteHostServiceCallback.GetAssetsAsync),
                        new object[] { scopeId, checksums.ToArray() },
                        (stream, cancellationToken) => Task.FromResult(RemoteHostAssetSerialization.ReadData(stream, scopeId, checksums, serializerService, cancellationToken)),
                        cancellationToken);
                }
            }, cancellationToken);
        }

        // TODO: remove (https://github.com/dotnet/roslyn/issues/43477)
        Task<bool> IAssetSource.IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            return RunServiceAsync(() =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_IsExperimentEnabledAsync, experimentName, cancellationToken))
                {
                    return EndPoint.InvokeAsync<bool>(
                        nameof(IRemoteHostServiceCallback.IsExperimentEnabledAsync),
                        new object[] { experimentName },
                        cancellationToken);
                }
            }, cancellationToken);
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

        private void SetGlobalContext(int uiCultureLCID, int cultureLCID, string serializedSession)
        {
            var session = new TelemetrySession(serializedSession);
            session.Start();

            EnsureCulture(uiCultureLCID, cultureLCID);

            WatsonReporter.InitializeFatalErrorHandlers(session);
            WatsonReporter.InitializeLogger(Logger);

            // set roslyn loggers
            RoslynServices.SetTelemetrySession(session);

            RoslynLogger.SetLogger(AggregateLogger.Create(new VSTelemetryLogger(session), RoslynLogger.GetLogger()));

            // start performance reporter
            var diagnosticAnalyzerPerformanceTracker = SolutionService.PrimaryWorkspace.Services.GetService<IPerformanceTrackerService>();
            if (diagnosticAnalyzerPerformanceTracker != null)
            {
                var globalOperationNotificationService = SolutionService.PrimaryWorkspace.Services.GetService<IGlobalOperationNotificationService>();
                _performanceReporter = new PerformanceReporter(Logger, diagnosticAnalyzerPerformanceTracker, globalOperationNotificationService, s_reportInterval, _shutdownCancellationSource.Token);
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

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task SynchronizePrimaryWorkspaceAsync(PinnedSolutionInfo solutionInfo, Checksum checksum, int workspaceVersion, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizePrimaryWorkspaceAsync, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
                {
                    var solutionService = CreateSolutionService(solutionInfo);
                    await solutionService.UpdatePrimaryWorkspaceAsync(checksum, workspaceVersion, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task SynchronizeTextAsync(DocumentId documentId, Checksum baseTextChecksum, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizeTextAsync, Checksum.GetChecksumLogInfo, baseTextChecksum, cancellationToken))
                {
                    var service = SolutionService.PrimaryWorkspace.Services.GetService<ISerializerService>();
                    if (service == null)
                    {
                        return;
                    }

                    var text = await TryGetSourceTextAsync().ConfigureAwait(false);
                    if (text == null)
                    {
                        // it won't bring in base text if it is not there already.
                        // text needed will be pulled in when there is request
                        return;
                    }

                    var newText = new WrappedText(text.WithChanges(textChanges));
                    var newChecksum = service.CreateChecksum(newText, cancellationToken);

                    // save new text in the cache so that when asked, the data is most likely already there
                    //
                    // this cache is very short live. and new text created above is ChangedText which share
                    // text data with original text except the changes.
                    // so memory wise, this doesn't put too much pressure on the cache. it will not duplicates
                    // same text multiple times.
                    //
                    // also, once the changes are picked up and put into Workspace, normal Workspace 
                    // caching logic will take care of the text
                    AssetStorage.TryAddAsset(newChecksum, newText);
                }

                async Task<SourceText?> TryGetSourceTextAsync()
                {
                    // check the cheap and fast one first.
                    // see if the cache has the source text
                    if (AssetStorage.TryGetAsset<SourceText>(baseTextChecksum, out var sourceText))
                    {
                        return sourceText;
                    }

                    // do slower one
                    // check whether existing solution has it
                    var document = SolutionService.PrimaryWorkspace.CurrentSolution.GetDocument(documentId);
                    if (document == null)
                    {
                        return null;
                    }

                    // check checksum whether it is there.
                    // since we lazily synchronize whole solution (SynchronizePrimaryWorkspaceAsync) when things are idle,
                    // soon or later this will get hit even if text changes got out of sync due to issues in VS side
                    // such as file is first opened and there is no SourceText in memory yet.
                    if (!document.State.TryGetStateChecksums(out var state) ||
                        !state.Text.Equals(baseTextChecksum))
                    {
                        return null;
                    }

                    return await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// workaround until (https://github.com/dotnet/roslyn/issues/26305) is fixed.
        /// 
        /// this will always return whole file as changed.
        /// </summary>
        private class WrappedText : SourceText
        {
            private readonly SourceText _text;

            public WrappedText(SourceText text)
            {
                _text = text;
            }

            public override char this[int position] => _text[position];
            public override Encoding? Encoding => _text.Encoding;
            public override int Length => _text.Length;
            public override SourceText GetSubText(TextSpan span) => _text.GetSubText(span);
            public override SourceText WithChanges(IEnumerable<TextChange> changes) => _text.WithChanges(changes);
            public override void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default)
                => _text.Write(writer, span, cancellationToken);
            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
                => _text.CopyTo(sourceIndex, destination, destinationIndex, count);
            public override IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
                => ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldText.Length), _text.Length));
            public override int GetHashCode() => _text.GetHashCode();
            public override bool Equals(object? obj) => _text.Equals(obj);
            public override string ToString() => _text.ToString();
            public override string ToString(TextSpan span) => _text.ToString(span);
        }
    }
}
