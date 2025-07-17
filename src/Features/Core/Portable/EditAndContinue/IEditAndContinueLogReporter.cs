// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Exported by the host to provide additional logging for EnC operations.
/// </summary>
internal interface IEditAndContinueLogReporter
{
    void Report(string message, LogMessageSeverity severity);
}
