// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.ServiceHub.Framework;
using Nerdbank.Streams;
using StreamJsonRpc;
using static Microsoft.ServiceHub.Framework.ServiceJsonRpcDescriptor;

namespace Microsoft.CodeAnalysis.BrokeredServices;

/// <summary>
/// Descriptors of brokered services not used by Roslyn remoting infrastructure.
/// </summary>
internal static class BrokeredServiceDescriptors
{
    /// <summary>
    /// Descriptors for client services written in TypeScript.
    /// </summary>
    private sealed class ClientServiceDescriptor : ServiceJsonRpcDescriptor
    {
        private const string AsyncSuffix = "Async";

        private static readonly Func<string, string> NameNormalize =
            name => CommonMethodNameTransforms.CamelCase(name.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - AsyncSuffix.Length) : name);

        public ClientServiceDescriptor(ServiceMoniker serviceMoniker, Type? clientInterface = null)
            : base(serviceMoniker, clientInterface, Formatters.MessagePack, MessageDelimiters.BigEndianInt32LengthHeader)
        {
        }

        public ClientServiceDescriptor(ClientServiceDescriptor copyFrom)
            : base(copyFrom)
        {
        }

        protected override ServiceRpcDescriptor Clone()
            => new ClientServiceDescriptor(this);

        protected override JsonRpcConnection CreateConnection(JsonRpc jsonRpc)
        {
            // allow TypeScript to name async methods without "Async" suffix

            var connection = base.CreateConnection(jsonRpc);
            connection.LocalRpcTargetOptions.MethodNameTransform = NameNormalize;
            connection.LocalRpcTargetOptions.EventNameTransform = NameNormalize;
            connection.LocalRpcProxyOptions.MethodNameTransform = NameNormalize;
            connection.LocalRpcProxyOptions.EventNameTransform = NameNormalize;
            return connection;
        }
    }

    internal const string LanguageServerComponentNamespace = "Microsoft.CodeAnalysis";
    internal const string VisualStudioComponentNamespace = "Microsoft.VisualStudio";

    /// <summary>
    /// Services proffered by Language Server process.
    /// </summary>
    internal const string LanguageServerComponentName = "LanguageServer";

    /// <summary>
    /// Services proffered by language client in the Extension Host process.
    /// </summary>
    internal const string LanguageClientComponentName = "LanguageClient";

    /// <summary>
    /// Services proffered by one of the Debugger processes.
    /// </summary>
    internal const string DebuggerComponentName = "Debugger";

    public static readonly ServiceRpcDescriptor SolutionSnapshotProvider = CreateClientServiceDescriptor("SolutionSnapshotProvider", new Version(0, 1));
    public static readonly ServiceRpcDescriptor DebuggerManagedHotReloadService = CreateDebuggerServiceDescriptor("ManagedHotReloadService", new Version(0, 1));
    public static readonly ServiceRpcDescriptor HotReloadLoggerService = CreateDebuggerServiceDescriptor("HotReloadLogger", new Version(0, 1));
    public static readonly ServiceRpcDescriptor HotReloadSessionNotificationService = CreateDebuggerServiceDescriptor("HotReloadSessionNotificationService", new Version(0, 1));
    public static readonly ServiceRpcDescriptor ManagedHotReloadAgentManagerService = CreateDebuggerServiceDescriptor("ManagedHotReloadAgentManagerService", new Version(0, 1));
    public static readonly ServiceRpcDescriptor GenericHotReloadAgentManagerService = CreateDebuggerServiceDescriptor("GenericHotReloadAgentManagerService", new Version(0, 1));
    public static readonly ServiceRpcDescriptor HotReloadOptionService = CreateDebuggerClientServiceDescriptor("HotReloadOptionService", new Version(0, 1));
    public static readonly ServiceRpcDescriptor MauiLaunchCustomizerService = CreateMauiServiceDescriptor("MauiLaunchCustomizerService", new Version(0, 1));
    public static readonly ServiceRpcDescriptor DebuggerSymbolLocatorService =
        CreateDebuggerServiceDescriptor("SymbolLocatorService", new Version(0, 1), new MultiplexingStream.Options { ProtocolMajorVersion = 3 });
    public static readonly ServiceRpcDescriptor DebuggerSourceLinkService =
        CreateDebuggerServiceDescriptor("SourceLinkService", new Version(0, 1), new MultiplexingStream.Options { ProtocolMajorVersion = 3 });

    public static ServiceMoniker CreateMoniker(string namespaceName, string componentName, string serviceName, Version? version)
        => new(namespaceName + "." + componentName + "." + serviceName, version);

    /// <summary>
    /// Descriptor for services proferred by the client extension (implemented in TypeScript).
    /// </summary>
    public static ServiceJsonRpcDescriptor CreateClientServiceDescriptor(string serviceName, Version? version = null)
        => new ClientServiceDescriptor(CreateMoniker(LanguageServerComponentNamespace, LanguageClientComponentName, serviceName, version), clientInterface: null)
           .WithExceptionStrategy(ExceptionProcessing.ISerializable);

    /// <summary>
    /// Descriptor for services proferred by Roslyn server or Visual Studio in-proc (implemented in C#). 
    /// </summary>
    public static ServiceJsonRpcDescriptor CreateServerServiceDescriptor(string serviceName, Version? version = null)
        => CreateDescriptor(CreateMoniker(LanguageServerComponentNamespace, LanguageServerComponentName, serviceName, version));

    /// <summary>
    /// Descriptor for services proferred by the debugger server (implemented in C#). 
    /// </summary>
    public static ServiceJsonRpcDescriptor CreateDebuggerServiceDescriptor(string serviceName, Version? version = null, MultiplexingStream.Options? streamOptions = null)
        => CreateDescriptor(CreateMoniker(VisualStudioComponentNamespace, DebuggerComponentName, serviceName, version), streamOptions);

    /// <summary>
    /// Descriptor for services proferred by the debugger server (implemented in TypeScript).
    /// </summary>
    public static ServiceJsonRpcDescriptor CreateDebuggerClientServiceDescriptor(string serviceName, Version? version = null)
        => new ClientServiceDescriptor(CreateMoniker(VisualStudioComponentNamespace, DebuggerComponentName, serviceName, version), clientInterface: null)
           .WithExceptionStrategy(ExceptionProcessing.ISerializable);

    /// <summary>
    /// Descriptor for services proferred by the MAUI extension (implemented in TypeScript).
    /// </summary>
    public static ServiceJsonRpcDescriptor CreateMauiServiceDescriptor(string serviceName, Version? version)
        => new ServiceJsonRpcDescriptor(CreateMoniker(VisualStudioComponentNamespace, "Maui", serviceName, version), clientInterface: null,
            Formatters.MessagePack, MessageDelimiters.BigEndianInt32LengthHeader,
            new MultiplexingStream.Options { ProtocolMajorVersion = 3 })
           .WithExceptionStrategy(ExceptionProcessing.ISerializable);

    private static ServiceJsonRpcDescriptor CreateDescriptor(ServiceMoniker moniker, MultiplexingStream.Options? streamOptions = null)
    {
        var descriptor = streamOptions is not null
            ? new ServiceJsonRpcDescriptor(moniker, clientInterface: null, Formatters.MessagePack, MessageDelimiters.BigEndianInt32LengthHeader, streamOptions)
            : new ServiceJsonRpcDescriptor(moniker, Formatters.MessagePack, MessageDelimiters.BigEndianInt32LengthHeader);
        return descriptor.WithExceptionStrategy(ExceptionProcessing.ISerializable);
    }

    private static ServiceJsonRpcDescriptor CreateDescriptorWithProtocolVersion(ServiceMoniker moniker, int protocolVersion)
        => new ServiceJsonRpcDescriptor(moniker, clientInterface: null, Formatters.MessagePack, MessageDelimiters.BigEndianInt32LengthHeader, new MultiplexingStream.Options { ProtocolMajorVersion = protocolVersion })
           .WithExceptionStrategy(ExceptionProcessing.ISerializable);

}
