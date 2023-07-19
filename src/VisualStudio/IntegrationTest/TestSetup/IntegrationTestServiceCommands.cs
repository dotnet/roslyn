// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    internal sealed class IntegrationTestServiceCommands : IDisposable
    {
        #region VSCT Identifiers
        public const int menuidIntegrationTestService = 0x5001;

        public const int grpidTestWindowRunTopLevelMenu = 0x0110;
        public const int grpidIntegrationTestService = 0x5101;

        public const int cmdidStartIntegrationTestService = 0x5201;
        public const int cmdidStopIntegrationTestService = 0x5204;

        public static readonly Guid guidTestWindowCmdSet = new Guid("1E198C22-5980-4E7E-92F3-F73168D1FB63");
        #endregion

        private static readonly BinaryServerFormatterSinkProvider DefaultSinkProvider = new BinaryServerFormatterSinkProvider()
        {
            TypeFilterLevel = TypeFilterLevel.Full
        };

        private readonly Package _package;

        private readonly MenuCommand _startMenuCmd;
        private readonly MenuCommand _stopMenuCmd;

        private IntegrationService? _service;
        private IpcServerChannel? _serviceChannel;

#pragma warning disable IDE0052 // Remove unread private members - used to hold the marshalled integration test service
        private ObjRef? _marshalledService;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly IVsRunningDocumentTable _runningDocumentTable;
        private IVsRunningDocTableEvents? s_runningDocTableEventListener;
        private uint s_runningDocTableEventListenerCookie;

        private IntegrationTestServiceCommands(Package package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            var startMenuCmdId = new CommandID(guidTestWindowCmdSet, cmdidStartIntegrationTestService);
            _startMenuCmd = new MenuCommand(StartServiceCallback, startMenuCmdId)
            {
                Enabled = true,
                Visible = true
            };

            var stopMenuCmdId = new CommandID(guidTestWindowCmdSet, cmdidStopIntegrationTestService);
            _stopMenuCmd = new MenuCommand(StopServiceCallback, stopMenuCmdId)
            {
                Enabled = false,
                Visible = false
            };

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService menuCommandService)
            {
                menuCommandService.AddCommand(_startMenuCmd);
                menuCommandService.AddCommand(_stopMenuCmd);
            }

            _runningDocumentTable = (IVsRunningDocumentTable)ServiceProvider.GetService(typeof(SVsRunningDocumentTable));
            Assumes.Present(_runningDocumentTable);
        }

        public static IntegrationTestServiceCommands? Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package)
        {
            Instance = new IntegrationTestServiceCommands(package);
        }

        /// <summary>
        /// Supports deserialization of types passed to APIs injected into the Visual Studio process by
        /// <see cref="IntegrationService.Execute"/>.
        /// </summary>
        private static Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
            => AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.FullName == args.Name);

        public void Dispose()
            => StopServiceCallback(this, EventArgs.Empty);

        /// <summary>
        /// Starts the IPC server for the Integration Test service.
        /// </summary>
        private void StartServiceCallback(object sender, EventArgs e)
        {
            if (_startMenuCmd.Enabled)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;

                TestTraceListener.Install();

                _service = new IntegrationService();

                _serviceChannel = new IpcServerChannel(
                    name: $"Microsoft.VisualStudio.IntegrationTest.ServiceChannel_{Process.GetCurrentProcess().Id}",
                    portName: _service.PortName,
                    sinkProvider: DefaultSinkProvider
                );

                var serviceType = typeof(IntegrationService);
                _marshalledService = RemotingServices.Marshal(_service, serviceType.FullName, serviceType);

                _serviceChannel.StartListening(null);

                // Async initialization is a workaround for deadlock loading ExtensionManagerPackage prior to
                // https://devdiv.visualstudio.com/DevDiv/_git/VSExtensibility/pullrequest/381506
                _ = Task.Run(async () =>
                {
                    var componentModel = (IComponentModel?)await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(false);
                    Assumes.Present(componentModel);

                    var asyncCompletionTracker = componentModel.GetService<AsyncCompletionTracker>();
                    asyncCompletionTracker.StartListening();

#pragma warning disable RS0030 // Do not used banned APIs
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
#pragma warning restore RS0030 // Do not used banned APIs

                    var listenerProvider = componentModel.GetService<IAsynchronousOperationListenerProvider>();
                    s_runningDocTableEventListener = new RunningDocumentTableEventListener(listenerProvider.GetListener(FeatureAttribute.Workspace));
                    ErrorHandler.ThrowOnFailure(_runningDocumentTable.AdviseRunningDocTableEvents(s_runningDocTableEventListener, out s_runningDocTableEventListenerCookie));
                });

                SwapAvailableCommands(_startMenuCmd, _stopMenuCmd);
            }
        }

        /// <summary>Stops the IPC server for the Integration Test service.</summary>
        private void StopServiceCallback(object sender, EventArgs e)
        {
            if (_stopMenuCmd.Enabled)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainAssemblyResolve;

                if (_serviceChannel != null)
                {
                    _serviceChannel.StopListening(null);
                    _serviceChannel = null;
                }

                _marshalledService = null;
                _service = null;

                var componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
                var asyncCompletionTracker = componentModel.GetService<AsyncCompletionTracker>();
                asyncCompletionTracker.StopListening();

                ErrorHandler.ThrowOnFailure(_runningDocumentTable.UnadviseRunningDocTableEvents(s_runningDocTableEventListenerCookie));
                s_runningDocTableEventListener = null;
                s_runningDocTableEventListenerCookie = 0;

                SwapAvailableCommands(_stopMenuCmd, _startMenuCmd);
            }
        }

        private static void SwapAvailableCommands(MenuCommand commandToDisable, MenuCommand commandToEnable)
        {
            commandToDisable.Enabled = false;
            commandToDisable.Visible = false;

            commandToEnable.Enabled = true;
            commandToEnable.Visible = true;
        }

        /// <summary>
        /// This event listener is an adapter to expose asynchronous file save operations to Roslyn via its standard
        /// workspace event waiters.
        /// </summary>
        private sealed class RunningDocumentTableEventListener : IVsRunningDocTableEvents, IVsRunningDocTableEvents7
        {
            private readonly IAsynchronousOperationListener _asynchronousOperationListener;

            public RunningDocumentTableEventListener(IAsynchronousOperationListener asynchronousOperationListener)
            {
                _asynchronousOperationListener = asynchronousOperationListener;
            }

            int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
                => VSConstants.S_OK;

#pragma warning disable CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes). (Signature was corrected in https://devdiv.visualstudio.com/DevDiv/_git/VS/pullrequest/390178)
            IVsTask? IVsRunningDocTableEvents7.OnBeforeSaveAsync(uint cookie, uint flags, IVsTask? saveTask)
#pragma warning restore CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes).
            {
                if (saveTask is not null)
                {
#pragma warning disable RS0030 // Do not used banned APIs
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
#pragma warning restore RS0030 // Do not used banned APIs
                    {
                        // Track asynchronous save operations via Roslyn's Workspace events
                        using var _ = _asynchronousOperationListener.BeginAsyncOperation("OnBeforeSaveAsync");
                        await saveTask;
                    });
                }

                // No additional work for the caller to handle
                return null;
            }

#pragma warning disable CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes). (Signature was corrected in https://devdiv.visualstudio.com/DevDiv/_git/VS/pullrequest/390178)
            IVsTask? IVsRunningDocTableEvents7.OnAfterSaveAsync(uint cookie, uint flags)
#pragma warning restore CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes).
            {
                // No additional work for the caller to handle
                return null;
            }
        }
    }
}
