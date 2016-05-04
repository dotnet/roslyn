// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Test.Utilities;

namespace Roslyn.VisualStudio.Test.Setup
{
    internal sealed class IntegrationTestServiceCommands : IDisposable
    {
        public const int CmdIdStartIntegrationTestService = 0x0100;
        public const int CmdIdStopIntegrationTestService = 0x0101;

        public static readonly Guid GrpIdIntegrationTestServiceCommands = new Guid("82A24540-AEBC-4883-A717-5317F0C0DAE9");

        private static readonly string DefaultPortName = string.Format(IntegrationService.PortNameFormatString, Process.GetCurrentProcess().Id);
        private static readonly BinaryServerFormatterSinkProvider DefaultSinkProvider = new BinaryServerFormatterSinkProvider() {
            TypeFilterLevel = TypeFilterLevel.Full
        };

        private readonly Package _package;
        private readonly MenuCommand _startServiceMenuCmd;
        private readonly MenuCommand _stopServiceMenuCmd;

        private IntegrationService _service;
        private IpcServerChannel _serviceChannel;
        private ObjRef _marshalledService;

        private IntegrationTestServiceCommands(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var startServiceMenuCmdId = new CommandID(GrpIdIntegrationTestServiceCommands, CmdIdStartIntegrationTestService);
                _startServiceMenuCmd = new MenuCommand(StartServiceCallback, startServiceMenuCmdId) {
                    Enabled = true,
                    Supported = true,
                    Visible = true
                };
                commandService.AddCommand(_startServiceMenuCmd);

                var stopServiceMenuCmdId = new CommandID(GrpIdIntegrationTestServiceCommands, CmdIdStopIntegrationTestService);
                _stopServiceMenuCmd = new MenuCommand(StopServiceCallback, stopServiceMenuCmdId) {
                    Enabled = false,
                    Supported = true,
                    Visible = false
                };
                commandService.AddCommand(_stopServiceMenuCmd);
            }

        }

        public static IntegrationTestServiceCommands Instance { get; private set; }

        private IServiceProvider ServiceProvider
            => _package;

        public static void Initialize(Package package)
        {
            Instance = new IntegrationTestServiceCommands(package);
        }

        public void Dispose()
        {
            StopServiceCallback(this, EventArgs.Empty);
        }

        /// <summary>Starts the IPC server for the Integration Test service.</summary>
        private void StartServiceCallback(object sender, EventArgs e)
        {
            if (_startServiceMenuCmd.Enabled)
            {
                _service = new IntegrationService();
                _serviceChannel = new IpcServerChannel(null, DefaultPortName, DefaultSinkProvider);

                var serviceType = typeof(IntegrationService);
                _marshalledService = RemotingServices.Marshal(_service, serviceType.FullName, serviceType);

                _serviceChannel.StartListening(null);

                SwapAvailableCommands(_startServiceMenuCmd, _stopServiceMenuCmd);
            }
        }

        /// <summary>Stops the IPC server for the Integration Test service.</summary>
        private void StopServiceCallback(object sender, EventArgs e)
        {
            if (_stopServiceMenuCmd.Enabled)
            {
                _serviceChannel?.StopListening(null);

                _marshalledService = null;
                _serviceChannel = null;
                _service = null;

                SwapAvailableCommands(_stopServiceMenuCmd, _startServiceMenuCmd);
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
