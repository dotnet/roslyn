// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

// https://microsoft.github.io/language-server-protocol/specifications/specification-current/#windowFeatures
partial class Methods
{
    /// <summary>
    /// Method name for 'window/showMessage'.
    /// <para>
    /// The show message notification is sent from a server to a client to ask the client to display a particular message in the user interface.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_showMessage">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string WindowShowMessageName = "window/showMessage";

    /// <summary>
    /// Strongly typed message object for 'window/showMessage'.
    /// </summary>
    public static readonly LspNotification<ShowMessageParams> WindowShowMessage = new(WindowShowMessageName);

    /// <summary>
    /// Method name for 'window/showMessageRequest'.
    /// <para>
    /// The show message request is sent from a server to a client to ask the client to display a particular message in the user interface.
    /// </para>
    /// <para>
    /// In addition to the show message notification the request allows to pass actions and to wait for an answer from the client.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_showMessageRequest">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string WindowShowMessageRequestName = "window/showMessageRequest";

    /// <summary>
    /// Strongly typed message object for 'window/showMessageRequest'.
    /// </summary>
    public static readonly LspRequest<ShowMessageRequestParams, MessageActionItem> WindowShowMessageRequest = new(WindowShowMessageRequestName);

    /// <summary>
    /// Method name for 'window/showDocument'.
    /// <para>
    /// The show document request is sent from a server to a client to ask the client to display a particular resource referenced by a URI in the user interface.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_showDocument">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WindowShowDocumentName = "window/showDocument";

    /// <summary>
    /// Strongly typed message object for 'window/showDocument'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<ShowDocumentParams, ShowDocumentResult> WindowShowDocument = new(WindowShowDocumentName);

    /// <summary>
    /// Method name for 'window/logMessage'.
    /// <para>
    /// The log message notification is sent from the server to the client to ask the client to log a particular message.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_logMessage">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string WindowLogMessageName = "window/logMessage";

    /// <summary>
    /// Strongly typed message object for 'window/logMessage'.
    /// </summary>
    public static readonly LspNotification<LogMessageParams> WindowLogMessage = new(WindowLogMessageName);

    /// <summary>
    /// Method name for 'window/workDoneProgress/create'
    /// <para>
    /// Sent from the server to the client to ask the client to create a work done progress.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_workDoneProgress_create">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    public const string WindowWorkDoneProgressCreateName = "window/workDoneProgress/create";

    /// <summary>
    /// Strongly typed message object for 'window/workDoneProgress/create'.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    public static readonly LspRequest<WorkDoneProgressCreateParams, object?> WindowWorkDoneProgressCreate = new(WindowWorkDoneProgressCreateName);

    /// <summary>
    /// Method name for 'window/workDoneProgress/cancel'
    /// <para>
    /// The window/workDoneProgress/cancel notification is sent from the client to the server to cancel a progress
    /// initiated on the server side using the window/workDoneProgress/create.
    /// </para>
    /// <para>
    /// The progress need not be marked as cancellable to be cancelled and a client may cancel a
    /// progress for any number of reasons: in case of error, reloading a workspace etc.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_workDoneProgress_cancel">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    public const string WindowWorkDoneProgressCancelName = "window/workDoneProgress/cancel";

    /// <summary>
    /// Strongly typed message object for 'window/workDoneProgress/cancel'.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    public static readonly LspNotification<WorkDoneProgressCancelParams> WindowWorkDoneProgressCancel = new(WindowWorkDoneProgressCancelName);

    /// <summary>
    /// Method name for 'telemetry/event'.
    /// <para>
    /// The telemetry notification is sent from the server to the client to ask the client to log a telemetry event.
    /// </para>
    /// <para>
    /// The protocol doesn't specify the payload since no interpretation of the data happens in the protocol.
    /// Most clients even don’t handle the event directly but forward them to the extensions owing the
    /// corresponding server issuing the event.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#telemetry_event">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TelemetryEventName = "telemetry/event";

    /// <summary>
    /// Strongly typed message object for 'telemetry/event'.
    /// </summary>
    public static readonly LspNotification<object> TelemetryEvent = new(TelemetryEventName);
}
