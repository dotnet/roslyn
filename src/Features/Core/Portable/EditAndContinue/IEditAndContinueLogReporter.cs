// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Optional workspace service that provides additional logging for EnC operations (e.g. forwarding logs to the
/// debugger's hot reload logger). Hosts that don't support such logging won't provide this service.
/// </summary>
internal interface IEditAndContinueLogReporter : IWorkspaceService
{
    void Report(string message, LogMessageSeverity severity);
}
