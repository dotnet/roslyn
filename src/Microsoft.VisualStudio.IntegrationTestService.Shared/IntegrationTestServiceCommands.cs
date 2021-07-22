// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTestService
{
    using System;
    using System.Collections;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Channels.Ipc;
    using System.Runtime.Serialization.Formatters;
    using Microsoft.VisualStudio.Shell;

    internal sealed class IntegrationTestServiceCommands : IDisposable
    {
        public const int CmdIdStartIntegrationTestService = 0x5201;
        public const int CmdIdStopIntegrationTestService = 0x5204;

        public static readonly Guid GuidIntegrationTestCmdSet = new Guid("F3505B05-AF1E-493A-A5A5-ECEB69C42714");

        private static readonly BinaryServerFormatterSinkProvider DefaultSinkProvider = new BinaryServerFormatterSinkProvider()
        {
            TypeFilterLevel = TypeFilterLevel.Full,
        };

        private readonly Package _package;

        private readonly MenuCommand _startMenuCmd;
        private readonly MenuCommand _stopMenuCmd;

        private IntegrationService _service;
        private IpcChannel _serviceChannel;
        private ObjRef _marshalledService;

        private IntegrationTestServiceCommands(Package package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService menuCommandService)
            {
                var startMenuCmdId = new CommandID(GuidIntegrationTestCmdSet, CmdIdStartIntegrationTestService);
                _startMenuCmd = new MenuCommand(StartServiceCallback, startMenuCmdId)
                {
                    Enabled = true,
                    Visible = true,
                };
                menuCommandService.AddCommand(_startMenuCmd);

                var stopMenuCmdId = new CommandID(GuidIntegrationTestCmdSet, CmdIdStopIntegrationTestService);
                _stopMenuCmd = new MenuCommand(StopServiceCallback, stopMenuCmdId)
                {
                    Enabled = false,
                    Visible = false,
                };
                menuCommandService.AddCommand(_stopMenuCmd);
            }
        }

        public static IntegrationTestServiceCommands Instance
        {
            get; private set;
        }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package)
        {
            Instance = new IntegrationTestServiceCommands(package);
        }

        public void Dispose()
            => StopServiceCallback(this, EventArgs.Empty);

        /// <summary>
        /// Starts the IPC server for the Integration Test service.
        /// </summary>
        private void StartServiceCallback(object sender, EventArgs e)
        {
            if (_startMenuCmd.Enabled)
            {
                _service = new IntegrationService();

                _serviceChannel = new IpcChannel(
                    new Hashtable
                    {
                        { "name", $"Microsoft.VisualStudio.IntegrationTest.ServiceChannel_{Process.GetCurrentProcess().Id}" },
                        { "portName", _service.PortName },
                    },
                    clientSinkProvider: new BinaryClientFormatterSinkProvider(),
                    serverSinkProvider: DefaultSinkProvider);

                var serviceType = typeof(IntegrationService);
                _marshalledService = RemotingServices.Marshal(_service, serviceType.FullName, serviceType);

                _serviceChannel.StartListening(null);
                ChannelServices.RegisterChannel(_serviceChannel, ensureSecurity: true);

                SwapAvailableCommands(_startMenuCmd, _stopMenuCmd);
            }
        }

        /// <summary>Stops the IPC server for the Integration Test service.</summary>
        private void StopServiceCallback(object sender, EventArgs e)
        {
            if (_stopMenuCmd.Enabled)
            {
                if (_serviceChannel != null)
                {
                    if (ChannelServices.RegisteredChannels.Contains(_serviceChannel))
                    {
                        ChannelServices.UnregisterChannel(_serviceChannel);
                    }

                    _serviceChannel.StopListening(null);
                    _serviceChannel = null;
                }

                GC.KeepAlive(_marshalledService);
                _marshalledService = null;
                _service = null;

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
