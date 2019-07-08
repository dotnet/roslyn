using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class LanguageServer : IDisposable
    {
        private static int s_instanceId;
        protected readonly int InstanceId;
        private bool _disposed;
        protected readonly TraceSource Logger;
        private readonly JsonRpc _rpc;

        [Obsolete("don't use RPC directly but use it through StartService and InvokeAsync", error: true)]
        protected readonly JsonRpc Rpc;

        public LanguageServer(Stream stream, IServiceProvider serviceProvider)
        {
            InstanceId = Interlocked.Add(ref s_instanceId, 1);
            _disposed = false;

            Logger = (TraceSource)serviceProvider.GetService(typeof(TraceSource));
            Logger.TraceInformation($"{DebugInstanceString} Service instance created");

            // due to this issue - https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950
            // all sub type must explicitly start JsonRpc once everything is
            // setup. 
            // we also wires given json converters when creating JsonRpc so that razor or SBD can register
            // their own converter when they create their own service
            Debugger.Launch();
            _rpc = stream.CreateStreamJsonRpc(target: this, Logger, SpecializedCollections.EmptyEnumerable<JsonConverter>());
            _rpc.Disconnected += OnRpcDisconnected;

            // we do this since we want to mark Rpc as obsolete but want to set its value for
            // partner teams until they move. we can't use Rpc directly since we will get
            // obsolete error and we can't suppress it since it is an error
            this.GetType().GetField("Rpc", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, _rpc);

            StartService();
        }

        protected void StartService()
        {
            _rpc.StartListening();
        }

        protected string DebugInstanceString => $"{GetType()} ({InstanceId})";

        protected event EventHandler Disconnected;

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);

            // either service naturally went away since nobody is using 
            // or the other side closed the connection such as closing VS
            if (e.Reason != DisconnectedReason.LocallyDisposed &&
                e.Reason != DisconnectedReason.RemotePartyTerminated)
            {
                // we no longer close connection forcefully. so connection shouldn't go away 
                // in normal situation. if it happens, log why it did in more detail.
                LogError($@"Client stream disconnected unexpectedly: 
{nameof(e.Description)}: {e.Description}
{nameof(e.Reason)}: {e.Reason}
{nameof(e.LastMessage)}: {e.LastMessage}
{nameof(e.Exception)}: {e.Exception?.ToString()}");
            }
        }

        protected void LogError(string message)
        {
            Log(TraceEventType.Error, message);
        }

        protected void Log(TraceEventType errorType, string message)
        {
            Logger.TraceEvent(errorType, 0, $"{DebugInstanceString} : " + message);
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(int? processId, string rootPath, Uri rootUri, ClientCapabilities capabilities, TraceSetting trace)
        {
            var c = new ServerCapabilities();
            c.WorkspaceSymbolProvider = true;
            var result = new InitializeResult();
            result.Capabilities = c;

            return result;
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public void Shutdown()
        {
        }

        [JsonRpcMethod(Methods.WorkspaceSymbolName)]
        public SymbolInformation[] WorkspaceSymbol(string query)
        {
            foreach (var project in SolutionService.PrimaryWorkspace.CurrentSolution.Projects)
            {
                return RunServiceAsync(async () =>
                {
                    using (UserOperationBooster.Boost())
                    {
                        //var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                        //var project = solution.GetProject(projectId);
                        //var priorityDocuments = priorityDocumentIds.Select(d => solution.GetDocument(d))
                        //                                           .ToImmutableArray();

                        var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                            project, ImmutableArray<Document>.Empty, query, ImmutableArray<string>.Empty.ToImmutableHashSet(), CancellationToken.None).ConfigureAwait(false);

                        return Convert(result);
                    }
                }, CancellationToken.None).Result;
            }

            return null;
        }

        private SymbolInformation[] Convert(
            ImmutableArray<INavigateToSearchResult> results)
        {
            var symbols = new SymbolInformation[results.Length];

            for (int i = 0; i < results.Length; i++)
            {
                symbols[i] = new SymbolInformation()
                {
                    Name = results[i].Name,
                    ContainerName = results[i].Summary,
                    Kind = VisualStudio.LanguageServer.Protocol.SymbolKind.Method,
                    Location = new VisualStudio.LanguageServer.Protocol.Location()
                };
            }

            return symbols;
        }

        protected async Task<T> RunServiceAsync<T>(Func<Task<T>> callAsync, CancellationToken cancellationToken)
        {
            //AssetStorage.UpdateLastActivityTime();

            try
            {
                return await callAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private bool LogUnlessCanceled(Exception ex, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LogException(ex);
            }

            return false;
        }

        protected void LogException(Exception ex)
        {
            LogError("Exception: " + ex.ToString());

            LogExtraInformation(ex);

            var callStack = new StackTrace().ToString();
            LogError("From: " + callStack);
        }

        private void LogExtraInformation(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            if (ex is ReflectionTypeLoadException reflection)
            {
                foreach (var loaderException in reflection.LoaderExceptions)
                {
                    LogError("LoaderException: " + loaderException.ToString());
                    LogExtraInformation(loaderException);
                }
            }

            if (ex is FileNotFoundException file)
            {
                LogError("FusionLog: " + file.FusionLog);
            }

            if (ex is AggregateException agg)
            {
                foreach (var innerException in agg.InnerExceptions)
                {
                    LogExtraInformation(innerException);
                }
            }
            else
            {
                LogExtraInformation(ex.InnerException);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                // guard us from double disposing. this can happen in unit test
                // due to how we create test mock service hub stream that tied to
                // remote host service
                return;
            }

            _disposed = true;
            _rpc.Dispose();

            Logger.TraceInformation($"{DebugInstanceString} Service instance disposed");
        }
    }
}
