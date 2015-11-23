// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Represents a process that hosts an interactive session.
    /// </summary>
    /// <remarks>
    /// Handles spawning of the host process and communication between the local callers and the remote session.
    /// </remarks>
    internal sealed partial class InteractiveHost : MarshalByRefObject
    {
        private readonly Type _replServiceProviderType;
        private readonly string _hostPath;
        private readonly string _initialWorkingDirectory;

        // adjustable for testing purposes
        private readonly int _millisecondsTimeout;
        private const int MaxAttemptsToCreateProcess = 2;

        private LazyRemoteService _lazyRemoteService;
        private int _remoteServiceInstanceId;

        // Remoting channel to communicate with the remote service.
        private IpcServerChannel _serverChannel;

        private TextWriter _output;
        private TextWriter _errorOutput;

        internal event Action<bool> ProcessStarting;

        public InteractiveHost(
            Type replServiceProviderType,
            string hostPath,
            string workingDirectory,
            int millisecondsTimeout = 5000)
        {
            _millisecondsTimeout = millisecondsTimeout;
            _output = TextWriter.Null;
            _errorOutput = TextWriter.Null;
            _replServiceProviderType = replServiceProviderType;
            _hostPath = hostPath;
            _initialWorkingDirectory = workingDirectory;

            var serverProvider = new BinaryServerFormatterSinkProvider { TypeFilterLevel = TypeFilterLevel.Full };
            _serverChannel = new IpcServerChannel(GenerateUniqueChannelLocalName(), "ReplChannel-" + Guid.NewGuid(), serverProvider);
            ChannelServices.RegisterChannel(_serverChannel, ensureSecurity: false);
        }

        #region Test hooks

        internal event Action<char[], int> OutputReceived;
        internal event Action<char[], int> ErrorOutputReceived;

        internal Process TryGetProcess()
        {
            InitializedRemoteService initializedService;

            return (_lazyRemoteService?.InitializedService != null &&
                    _lazyRemoteService.InitializedService.TryGetValue(out initializedService)) ? initializedService.ServiceOpt.Process : null;
        }

        internal Service TryGetService()
        {
            var initializedService = TryGetOrCreateRemoteServiceAsync().Result;
            return initializedService.ServiceOpt?.Service;
        }

        // Triggered whenever we create a fresh process.
        // The ProcessExited event is not hooked yet.
        internal event Action<Process> InteractiveHostProcessCreated;

        internal IpcServerChannel _ServerChannel
        {
            get { return _serverChannel; }
        }

        internal void Dispose(bool joinThreads)
        {
            Dispose(joinThreads, disposing: true);
        }

        #endregion

        private static string GenerateUniqueChannelLocalName()
        {
            return typeof(InteractiveHost).FullName + Guid.NewGuid();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        private RemoteService TryStartProcess(CultureInfo culture, CancellationToken cancellationToken)
        {
            Process newProcess = null;
            int newProcessId = -1;
            Semaphore semaphore = null;
            try
            {
                int currentProcessId = Process.GetCurrentProcess().Id;

                bool semaphoreCreated;

                string semaphoreName;
                while (true)
                {
                    semaphoreName = "InteractiveHostSemaphore-" + Guid.NewGuid();
                    semaphore = new Semaphore(0, 1, semaphoreName, out semaphoreCreated);

                    if (semaphoreCreated)
                    {
                        break;
                    }

                    semaphore.Close();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var remoteServerPort = "InteractiveHostChannel-" + Guid.NewGuid();

                var processInfo = new ProcessStartInfo(_hostPath);
                processInfo.Arguments = remoteServerPort + " " + semaphoreName + " " + currentProcessId;
                processInfo.WorkingDirectory = _initialWorkingDirectory;
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
                processInfo.StandardErrorEncoding = Encoding.UTF8;
                processInfo.StandardOutputEncoding = Encoding.UTF8;

                newProcess = new Process();
                newProcess.StartInfo = processInfo;

                // enables Process.Exited event to be raised:
                newProcess.EnableRaisingEvents = true;

                newProcess.Start();
                InteractiveHostProcessCreated?.Invoke(newProcess);

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    newProcessId = newProcess.Id;
                }
                catch
                {
                    newProcessId = 0;
                }

                // sync:
                while (!semaphore.WaitOne(_millisecondsTimeout))
                {
                    if (!CheckAlive(newProcess))
                    {
                        return null;
                    }

                    _output.WriteLine(FeaturesResources.AttemptToConnectToProcess, newProcessId);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // instantiate remote service:
                Service newService;
                try
                {
                    newService = (Service)Activator.GetObject(
                        typeof(Service),
                        "ipc://" + remoteServerPort + "/" + Service.ServiceName);

                    cancellationToken.ThrowIfCancellationRequested();

                    newService.Initialize(_replServiceProviderType, culture.Name);
                }
                catch (RemotingException) when (!CheckAlive(newProcess))
                {
                    return null;
                }

                return new RemoteService(this, newProcess, newProcessId, newService);
            }
            catch (OperationCanceledException)
            {
                if (newProcess != null)
                {
                    RemoteService.InitiateTermination(newProcess, newProcessId);
                }

                return null;
            }
            finally
            {
                if (semaphore != null)
                {
                    semaphore.Close();
                }
            }
        }
        private bool CheckAlive(Process process)
        {
            bool alive = process.IsAlive();
            if (!alive)
            {
                _errorOutput.WriteLine(FeaturesResources.FailedToLaunchProcess, _hostPath, process.ExitCode);
                _errorOutput.WriteLine(process.StandardError.ReadToEnd());
            }

            return alive;
        }

        ~InteractiveHost()
        {
            Dispose(joinThreads: false, disposing: false);
        }

        public void Dispose()
        {
            Dispose(joinThreads: false, disposing: true);
        }

        private void Dispose(bool joinThreads, bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
                DisposeChannel();
            }

            if (_lazyRemoteService != null)
            {
                _lazyRemoteService.Dispose(joinThreads);
                _lazyRemoteService = null;
            }
        }

        private void DisposeChannel()
        {
            if (_serverChannel != null)
            {
                ChannelServices.UnregisterChannel(_serverChannel);
                _serverChannel = null;
            }
        }

        public TextWriter Output
        {
            get
            {
                return _output;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var oldOutput = Interlocked.Exchange(ref _output, value);
                oldOutput.Flush();
            }
        }

        public TextWriter ErrorOutput
        {
            get
            {
                return _errorOutput;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var oldOutput = Interlocked.Exchange(ref _errorOutput, value);
                oldOutput.Flush();
            }
        }

        internal void OnOutputReceived(bool error, char[] buffer, int count)
        {
            (error ? ErrorOutputReceived : OutputReceived)?.Invoke(buffer, count);

            var writer = error ? ErrorOutput : Output;
            writer.Write(buffer, 0, count);
        }

        private LazyRemoteService CreateRemoteService(InteractiveHostOptions options, bool skipInitialization)
        {
            return new LazyRemoteService(this, options, Interlocked.Increment(ref _remoteServiceInstanceId), skipInitialization);
        }

        private Task OnProcessExited(Process process)
        {
            ReportProcessExited(process);
            return TryGetOrCreateRemoteServiceAsync();
        }

        private void ReportProcessExited(Process process)
        {
            int? exitCode;
            try
            {
                exitCode = process.HasExited ? process.ExitCode : (int?)null;
            }
            catch
            {
                exitCode = null;
            }

            if (exitCode.HasValue)
            {
                _errorOutput.WriteLine(FeaturesResources.HostingProcessExitedWithExitCode, exitCode.Value);
            }
        }

        private async Task<InitializedRemoteService> TryGetOrCreateRemoteServiceAsync()
        {
            try
            {
                LazyRemoteService currentRemoteService = _lazyRemoteService;

                // disposed or not reset:
                Debug.Assert(currentRemoteService != null);

                for (int attempt = 0; attempt < MaxAttemptsToCreateProcess; attempt++)
                {
                    var initializedService = await currentRemoteService.InitializedService.GetValueAsync(currentRemoteService.CancellationSource.Token).ConfigureAwait(false);
                    if (initializedService.ServiceOpt != null && initializedService.ServiceOpt.Process.IsAlive())
                    {
                        return initializedService;
                    }

                    // Service failed to start or initialize or the process died.
                    var newService = CreateRemoteService(currentRemoteService.Options, skipInitialization: !initializedService.InitializationResult.Success);

                    var previousService = Interlocked.CompareExchange(ref _lazyRemoteService, newService, currentRemoteService);
                    if (previousService == currentRemoteService)
                    {
                        // we replaced the service whose process we know is dead:
                        currentRemoteService.Dispose(joinThreads: false);
                        currentRemoteService = newService;
                    }
                    else
                    {
                        // the process was reset in between our checks, try to use the new service:
                        newService.Dispose(joinThreads: false);
                        currentRemoteService = previousService;
                    }
                }

                _errorOutput.WriteLine(FeaturesResources.UnableToCreateHostingProcess);
            }
            catch (OperationCanceledException)
            {
                // The user reset the process during initialization. 
                // The reset operation will recreate the process.
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }

            return default(InitializedRemoteService);
        }

        private async Task<TResult> Async<TResult>(Action<Service, RemoteAsyncOperation<TResult>> action)
        {
            try
            {
                var initializedService = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
                if (initializedService.ServiceOpt == null)
                {
                    return default(TResult);
                }

                return await new RemoteAsyncOperation<TResult>(initializedService.ServiceOpt).AsyncExecute(action).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static async Task<TResult> Async<TResult>(RemoteService remoteService, Action<Service, RemoteAsyncOperation<TResult>> action)
        {
            try
            {
                return await new RemoteAsyncOperation<TResult>(remoteService).AsyncExecute(action).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        #region Operations

        /// <summary>
        /// Restarts and reinitializes the host process (or starts a new one if it is not running yet).
        /// </summary>
        /// <param name="optionsOpt">The options to initialize the new process with, or null to use the current options (or default options if the process isn't running yet).</param>
        public async Task<RemoteExecutionResult> ResetAsync(InteractiveHostOptions optionsOpt)
        {
            try
            {
                var options = optionsOpt ?? _lazyRemoteService?.Options ?? new InteractiveHostOptions(null, CultureInfo.CurrentUICulture);

                // replace the existing service with a new one:
                var newService = CreateRemoteService(options, skipInitialization: false);

                LazyRemoteService oldService = Interlocked.Exchange(ref _lazyRemoteService, newService);
                if (oldService != null)
                {
                    oldService.Dispose(joinThreads: false);
                }

                var initializedService = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
                if (initializedService.ServiceOpt == null)
                {
                    return default(RemoteExecutionResult);
                }

                return initializedService.InitializationResult;
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Asynchronously executes given code in the remote interactive session.
        /// </summary>
        /// <param name="code">The code to execute.</param>
        /// <remarks>
        /// This method is thread safe but operations are sent to the remote process
        /// asynchronously so tasks should be executed serially if order is important.
        /// </remarks>
        public Task<RemoteExecutionResult> ExecuteAsync(string code)
        {
            Debug.Assert(code != null);
            return Async<RemoteExecutionResult>((service, operation) => service.ExecuteAsync(operation, code));
        }

        /// <summary>
        /// Asynchronously executes given code in the remote interactive session.
        /// </summary>
        /// <param name="path">The file to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <remarks>
        /// This method is thread safe but operations are sent to the remote process
        /// asynchronously so tasks should be executed serially if order is important.
        /// </remarks>
        public Task<RemoteExecutionResult> ExecuteFileAsync(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return Async<RemoteExecutionResult>((service, operation) => service.ExecuteFileAsync(operation, path));
        }

        /// <summary>
        /// Asynchronously adds a reference to the set of available references for next submission.
        /// </summary>
        /// <param name="reference">The reference to add.</param>
        /// <remarks>
        /// This method is thread safe but operations are sent to the remote process
        /// asynchronously so tasks should be executed serially if order is important.
        /// </remarks>
        public Task<bool> AddReferenceAsync(string reference)
        {
            Debug.Assert(reference != null);
            return Async<bool>((service, operation) => service.AddReferenceAsync(operation, reference));
        }

        /// <summary>
        /// Sets the current session's search paths and base directory.
        /// </summary>
        public Task<RemoteExecutionResult> SetPathsAsync(string[] referenceSearchPaths, string[] sourceSearchPaths, string baseDirectory)
        {
            Debug.Assert(referenceSearchPaths != null);
            Debug.Assert(sourceSearchPaths != null);
            Debug.Assert(baseDirectory != null);

            return Async<RemoteExecutionResult>((service, operation) => service.SetPathsAsync(operation, referenceSearchPaths, sourceSearchPaths, baseDirectory));
        }

        #endregion
    }
}
