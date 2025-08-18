// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;

/// <summary>
/// Async equivalent to <see cref="IVsLanguageServiceBuildErrorReporter2"/>.
/// </summary>
internal interface IAsyncVsLanguageServiceBuildErrorReporter
{
    Task ClearErrorsAsync();

    Task<bool> TryReportErrorAsync(
        string? errorMessage, string errorId, VSTASKPRIORITY taskPriority, int startLine, int startColumn, int endLine, int endColumn, string fileName);
}
