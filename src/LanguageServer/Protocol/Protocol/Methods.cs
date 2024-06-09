// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Class which contains the string values for all common language protocol methods.
    /// </summary>
    internal static partial class Methods
    {
        // NOTE: these are sorted/grouped in the order used by the spec

        /// <summary>
        /// Method name for '$/progress' notifications.
        /// </summary>
        public const string ProgressNotificationName = "$/progress";

        /// <summary>
        /// Name of the progress token in the request.
        /// </summary>
        public const string PartialResultTokenName = "partialResultToken";

        /// <summary>
        /// Name of the work done token in the request.
        /// </summary>
        public const string WorkDoneTokenName = "workDoneToken";

        /// <summary>
        /// Name of the progress token in the $/progress notification.
        /// </summary>
        public const string ProgressNotificationTokenName = "token";

        /// <summary>
        /// Method name for 'window/logMessage'.
        /// </summary>
        public const string WindowLogMessageName = "window/logMessage";

        /// <summary>
        /// Method name for 'window/showMessage'.
        /// </summary>
        public const string WindowShowMessageName = "window/showMessage";

        /// <summary>
        /// Method name for 'window/showMessageRequest'.
        /// </summary>
        public const string WindowShowMessageRequestName = "window/showMessageRequest";

        /// <summary>
        /// Method name for 'workspace/applyEdit'.
        /// </summary>
        public const string WorkspaceApplyEditName = "workspace/applyEdit";

        /// <summary>
        /// Method name for 'workspace/executeCommand'.
        /// </summary>
        public const string WorkspaceExecuteCommandName = "workspace/executeCommand";

        /// <summary>
        /// Method name for 'workspace/didChangeWatchedFiles'.
        /// </summary>
        public const string WorkspaceDidChangeWatchedFilesName = "workspace/didChangeWatchedFiles";

        /// <summary>
        /// Method name for 'telemetry/event'.
        /// </summary>
        public const string TelemetryEventName = "telemetry/event";

        /// <summary>
        /// Strongly typed message object for 'window/logMessage'.
        /// </summary>
        public static readonly LspNotification<LogMessageParams> WindowLogMessage = new LspNotification<LogMessageParams>(WindowLogMessageName);

        /// <summary>
        /// Strongly typed message object for 'window/showMessage'.
        /// </summary>
        public static readonly LspNotification<ShowMessageParams> WindowShowMessage = new LspNotification<ShowMessageParams>(WindowShowMessageName);

        /// <summary>
        /// Strongly typed message object for 'window/showMessageRequest'.
        /// </summary>
        public static readonly LspRequest<ShowMessageRequestParams, MessageActionItem> WindowShowMessageRequest = new LspRequest<ShowMessageRequestParams, MessageActionItem>(WindowShowMessageRequestName);

        /// <summary>
        /// Strongly typed message object for 'workspace/applyEdit'.
        /// </summary>
        public static readonly LspRequest<ApplyWorkspaceEditParams, ApplyWorkspaceEditResponse> WorkspaceApplyEdit = new LspRequest<ApplyWorkspaceEditParams, ApplyWorkspaceEditResponse>(WorkspaceApplyEditName);

        /// <summary>
        /// Strongly typed message object for 'workspace/executeCommand'.
        /// </summary>
        public static readonly LspRequest<ExecuteCommandParams, object?> WorkspaceExecuteCommand = new LspRequest<ExecuteCommandParams, object?>(WorkspaceExecuteCommandName);

        /// <summary>
        /// Strongly typed message object for 'workspace/didChangeWatchedFiles'.
        /// </summary>
        public static readonly LspNotification<DidChangeWatchedFilesParams> WorkspaceDidChangeWatchedFiles = new LspNotification<DidChangeWatchedFilesParams>(WorkspaceDidChangeWatchedFilesName);

        /// <summary>
        /// Strongly typed message object for 'telemetry/event'.
        /// </summary>
        public static readonly LspNotification<object> TelemetryEvent = new LspNotification<object>(TelemetryEventName);
    }
}
