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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Shell;

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

        private IntegrationService _service;
        private IpcServerChannel _serviceChannel;

#pragma warning disable IDE0052 // Remove unread private members - used to hold the marshalled integration test service
        private ObjRef _marshalledService;
#pragma warning restore IDE0052 // Remove unread private members

        private IntegrationTestServiceCommands(Package package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService menuCommandService)
            {
                var startMenuCmdId = new CommandID(guidTestWindowCmdSet, cmdidStartIntegrationTestService);
                _startMenuCmd = new MenuCommand(StartServiceCallback, startMenuCmdId)
                {
                    Enabled = true,
                    Visible = true
                };
                menuCommandService.AddCommand(_startMenuCmd);

                var stopMenuCmdId = new CommandID(guidTestWindowCmdSet, cmdidStopIntegrationTestService);
                _stopMenuCmd = new MenuCommand(StopServiceCallback, stopMenuCmdId)
                {
                    Enabled = false,
                    Visible = false
                };
                menuCommandService.AddCommand(_stopMenuCmd);
            }
        }

        public static IntegrationTestServiceCommands Instance { get; private set; }

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

                WatsonTraceListener.Install();

                _service = new IntegrationService();

                _serviceChannel = new IpcServerChannel(
                    name: $"Microsoft.VisualStudio.IntegrationTest.ServiceChannel_{Process.GetCurrentProcess().Id}",
                    portName: _service.PortName,
                    sinkProvider: DefaultSinkProvider
                );

                var serviceType = typeof(IntegrationService);
                _marshalledService = RemotingServices.Marshal(_service, serviceType.FullName, serviceType);

                _serviceChannel.StartListening(null);

                var componentModel = ServiceProvider.GetService<SComponentModel, IComponentModel>();
                var asyncCompletionTracker = componentModel.GetService<AsyncCompletionTracker>();
                asyncCompletionTracker.StartListening();

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

                var componentModel = ServiceProvider.GetService<SComponentModel, IComponentModel>();
                var asyncCompletionTracker = componentModel.GetService<AsyncCompletionTracker>();
                asyncCompletionTracker.StopListening();

                SwapAvailableCommands(_stopMenuCmd, _startMenuCmd);
            }
        }

        private void SwapAvailableCommands(MenuCommand commandToDisable, MenuCommand commandToEnable)
        {
            commandToDisable.Enabled = false;
            commandToDisable.Visible = false;

            commandToEnable.Enabled = true;
            commandToEnable.Visible = true;
        }
    }
}
