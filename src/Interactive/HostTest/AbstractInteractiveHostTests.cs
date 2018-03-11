﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Interactive;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    public abstract class AbstractInteractiveHostTests : CSharpTestBase
    {
        // Forces xUnit to load dependent assemblies before we launch InteractiveHost.exe process.
        private static readonly Type[] s_testDependencies = new[]
        {
            typeof(InteractiveHost),
            typeof(CSharpCompilation)
        };

        private static readonly FieldInfo s_ipcServerChannelListenerThread = typeof(IpcServerChannel).GetField("_listenerThread", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static string GetInteractiveHostPath()
        {
            return typeof(InteractiveHostEntryPoint).Assembly.Location;
        }

        internal static void DisposeInteractiveHostProcess(InteractiveHost process)
        {
            IpcServerChannel serverChannel = process._ServerChannel;
            process.Dispose();

            var listenerThread = (Thread)s_ipcServerChannelListenerThread.GetValue(serverChannel);
            listenerThread.Join();
        }
    }
}
