// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Scripting;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;
using StreamJsonRpc;
using Scripting::Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        private sealed class LazyRemoteService
        {
            private readonly AsyncLazy<InitializedRemoteService> _lazyInitializedService;
            private readonly CancellationTokenSource _cancellationSource;

            public readonly InteractiveHostOptions Options;
            public readonly InteractiveHost Host;
            public readonly bool SkipInitialization;
            public readonly int InstanceId;

            public LazyRemoteService(InteractiveHost host, InteractiveHostOptions options, int instanceId, bool skipInitialization)
            {
                _lazyInitializedService = AsyncLazy.Create(static (self, cancellationToken) => self.TryStartAndInitializeProcessAsync(cancellationToken), this);
                _cancellationSource = new CancellationTokenSource();
                InstanceId = instanceId;
                Options = options;
                Host = host;
                SkipInitialization = skipInitialization;
            }

            public void Dispose()
            {
                // Cancel the creation of the process if it is in progress.
                // If it is the cancellation will clean up all resources allocated during the creation.
                _cancellationSource.Cancel();

                // If the value has been calculated already, dispose the service.
                if (_lazyInitializedService.TryGetValue(out var initializedService))
                {
                    initializedService.Service?.Dispose();
                }
            }

            internal Task<InitializedRemoteService> GetInitializedServiceAsync()
                => _lazyInitializedService.GetValueAsync(_cancellationSource.Token);

            internal InitializedRemoteService? TryGetInitializedService()
                => _lazyInitializedService.TryGetValue(out var service) ? service : default;

            private async Task<InitializedRemoteService> TryStartAndInitializeProcessAsync(CancellationToken cancellationToken)
            {
                try
                {
                    var remoteService = await TryStartProcessAsync(Options.HostPath, Options.Culture, Options.UICulture, cancellationToken).ConfigureAwait(false);
                    if (remoteService == null)
                    {
                        return default;
                    }

                    RemoteExecutionResult result;

                    if (SkipInitialization)
                    {
                        result = new RemoteExecutionResult(
                            success: true,
                            sourcePaths: ImmutableArray<string>.Empty,
                            referencePaths: ImmutableArray<string>.Empty,
                            workingDirectory: Host._initialWorkingDirectory,
                            initializationResult: new RemoteInitializationResult(
                                initializationScript: null,
                                metadataReferencePaths: ImmutableArray.Create(typeof(object).Assembly.Location, typeof(InteractiveScriptGlobals).Assembly.Location),
                                imports: ImmutableArray<string>.Empty));

                        Host.ProcessInitialized?.Invoke(remoteService.PlatformInfo, Options, result);
                        return new InitializedRemoteService(remoteService, result);
                    }

                    bool initializing = true;
                    cancellationToken.Register(() =>
                    {
                        if (initializing)
                        {
                            // kill the process without triggering auto-reset:
                            remoteService.Dispose();
                        }
                    });

                    // try to execute initialization script:
                    var isRestarting = InstanceId > 1;
                    result = await ExecuteRemoteAsync(remoteService, nameof(Service.InitializeContextAsync), Options.InitializationFilePath, isRestarting).ConfigureAwait(false);

                    initializing = false;
                    if (!result.Success)
                    {
                        Host.ReportProcessExited(remoteService.Process);
                        remoteService.Dispose();

                        return default;
                    }

                    Contract.ThrowIfNull(result.InitializationResult);

                    // Hook up a handler that initiates restart when the process exits.
                    // Note that this is just so that we restart the process as soon as we see it dying and it doesn't need to be 100% bullet-proof.
                    // If we don't receive the "process exited" event we will restart the process upon the next remote operation.
                    remoteService.HookAutoRestartEvent();

                    Host.ProcessInitialized?.Invoke(remoteService.PlatformInfo, Options, result);

                    return new InitializedRemoteService(remoteService, result);
                }
#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
                // await ExecuteRemoteAsync above does not take cancellationToken
                // - we don't currently support cancellation of the RPC call,
                // but JsonRpc.InvokeAsync that we use still claims it may throw OperationCanceledException..
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
#pragma warning restore CA2016
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private async Task<RemoteService?> TryStartProcessAsync(string hostPath, CultureInfo culture, CultureInfo uiCulture, CancellationToken cancellationToken)
            {
                int currentProcessId = Process.GetCurrentProcess().Id;
                var pipeName = typeof(InteractiveHost).FullName + Guid.NewGuid();

                var newProcess = new Process
                {
                    StartInfo = new ProcessStartInfo(hostPath)
                    {
                        Arguments = $"{pipeName} {currentProcessId} \"{culture.Name}\" \"{uiCulture.Name}\"",
                        WorkingDirectory = Host._initialWorkingDirectory,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardErrorEncoding = OutputEncoding,
                        StandardOutputEncoding = OutputEncoding
                    },

                    // enables Process.Exited event to be raised:
                    EnableRaisingEvents = true
                };

                try
                {
                    newProcess.Start();
                }
                catch (Exception e)
                {
                    Host.WriteOutputInBackground(
                        isError: true,
                        string.Format(InteractiveHostResources.Failed_to_create_a_remote_process_for_interactive_code_execution, hostPath),
                        e.Message);

                    Host.InteractiveHostProcessCreationFailed?.Invoke(e, TryGetExitCode(newProcess));
                    return null;
                }

                Host.InteractiveHostProcessCreated?.Invoke(newProcess);

                int newProcessId = -1;
                try
                {
                    newProcessId = newProcess.Id;
                }
                catch
                {
                    newProcessId = 0;
                }

                var clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                JsonRpc? jsonRpc = null;

                void ProcessExitedBeforeEstablishingConnection(object sender, EventArgs e)
                {
                    Host.InteractiveHostProcessCreationFailed?.Invoke(null, TryGetExitCode(newProcess));
                    _cancellationSource.Cancel();
                }

                // Connecting the named pipe client would block if the process exits before the connection is established,
                // as the client waits for the server to become available. We signal the cancellation token to abort.
                newProcess.Exited += ProcessExitedBeforeEstablishingConnection;

                InteractiveHostPlatformInfo platformInfo;
                try
                {
                    if (!CheckAlive(newProcess, hostPath))
                    {
                        Host.InteractiveHostProcessCreationFailed?.Invoke(null, TryGetExitCode(newProcess));
                        return null;
                    }

                    await clientStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    jsonRpc = CreateRpc(clientStream, incomingCallTarget: null);

                    platformInfo = (await jsonRpc.InvokeWithCancellationAsync<InteractiveHostPlatformInfo.Data>(
                        nameof(Service.InitializeAsync),
                        new object[] { Host._replServiceProviderType.AssemblyQualifiedName },
                        cancellationToken).ConfigureAwait(false)).Deserialize();
                }
                catch (Exception e)
                {
                    if (CheckAlive(newProcess, hostPath))
                    {
                        RemoteService.InitiateTermination(newProcess, newProcessId);
                    }

                    jsonRpc?.Dispose();

                    Host.InteractiveHostProcessCreationFailed?.Invoke(e, TryGetExitCode(newProcess));
                    return null;
                }
                finally
                {
                    newProcess.Exited -= ProcessExitedBeforeEstablishingConnection;
                }

                return new RemoteService(Host, newProcess, newProcessId, jsonRpc, platformInfo, Options);
            }

            private bool CheckAlive(Process process, string hostPath)
            {
                bool alive = process.IsAlive();
                if (!alive)
                {
                    string errorString = process.StandardError.ReadToEnd();

                    Host.WriteOutputInBackground(
                        isError: true,
                        string.Format(InteractiveHostResources.Failed_to_launch_0_process_exit_code_colon_1_with_output_colon, hostPath, process.ExitCode),
                        errorString);
                }

                return alive;
            }

            private static int? TryGetExitCode(Process process)
            {
                try
                {
                    return process.ExitCode;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
