// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

// https://microsoft.github.io/language-server-protocol/specifications/specification-current/#lifeCycleMessages
partial class Methods
{
    // NOTE: these are sorted/grouped in the order used by the spec

    /// <summary>
    /// Method name for 'initialize'.
    /// <para>
    /// The initialize request is sent as the first request from the client to the server and is used to exchange capabilities.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initialize">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string InitializeName = "initialize";

    /// <summary>
    /// Strongly typed message object for 'initialize'.
    /// </summary>
    public static readonly LspRequest<InitializeParams, InitializeResult> Initialize = new(InitializeName);

    /// <summary>
    /// Method name for 'initialized'.
    /// <para>
    /// The initialized notification is sent from the client to the server after the client received the
    /// result of the initialize request but before the client is sending any other request or notification to the server.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initialized">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string InitializedName = "initialized";

    /// <summary>
    /// Strongly typed message object for 'initialized'.
    /// </summary>
    public static readonly LspNotification<InitializedParams> Initialized = new(InitializedName);

    /// <summary>
    /// Method name for 'client/registerCapability'.
    /// <para>
    /// The client/registerCapability request is sent from the server to the client to register for a new capability on the client side.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#client_registerCapability">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string ClientRegisterCapabilityName = "client/registerCapability";

    /// <summary>
    /// Strongly typed message object for 'client/registerCapability'.
    /// </summary>
    public static readonly LspRequest<RegistrationParams, object> ClientRegisterCapability = new(ClientRegisterCapabilityName);

    /// <summary>
    /// Method name for 'client/unregisterCapability'.
    /// <para>
    /// The client/unregisterCapability request is sent from the server to the client to unregister a previously registered capability.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#client_unregisterCapability">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string ClientUnregisterCapabilityName = "client/unregisterCapability";

    /// <summary>
    /// Strongly typed message object for 'client/unregisterCapability'.
    /// </summary>
    public static readonly LspRequest<UnregistrationParams, object> ClientUnregisterCapability = new(ClientUnregisterCapabilityName);

    /// <summary>
    /// Method name for '$/setTrace' notifications.
    /// <para>
    /// A notification that should be used by the client to modify the trace setting of the server.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#setTrace">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string SetTraceName = "$/setTrace";

    /// <summary>
    /// Strongly typed message object for '$/setTrace'.
    /// </summary>
    public static readonly LspNotification<SetTraceParams> SetTrace = new(SetTraceName);

    /// <summary>
    /// Method name for '$/logTrace' notifications.
    /// <para>
    /// A notification to log the trace of the server’s execution. This must
    /// respect the current trace configuration set by the $/logTrace notification.
    /// </para>
    /// <para>
    /// This should only be used for systematic trace reporting For single debugging messages,
    /// the server should instead send window/logMessage notifications.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#logTrace">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string LogTraceName = "$/logTrace";

    /// <summary>
    /// Strongly typed message object for '$/logTrace'.
    /// </summary>
    public static readonly LspNotification<LogTraceParams> LogTrace = new(LogTraceName);

    /// <summary>
    /// Method name for 'shutdown'.
    /// <para>
    /// The shutdown request is sent from the client to the server. It asks the server to shut
    /// down, but to not exit (otherwise the response might not be delivered correctly to the
    /// client). There is a separate exit notification that asks the server to exit.
    /// <para>
    /// </para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#shutdown">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string ShutdownName = "shutdown";

    /// <summary>
    /// Strongly typed message object for 'shutdown'.
    /// </summary>
    public static readonly LspRequest<object?, object> Shutdown = new(ShutdownName);

    /// <summary>
    /// Method name for 'exit'.
    /// <para>
    /// A notification to ask the server to exit its process.
    /// <para>
    /// </para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#shutdown">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string ExitName = "exit";

    /// <summary>
    /// Strongly typed message object for 'exit'.
    /// </summary>
    public static readonly LspNotification<object?> Exit = new(ExitName);
}
