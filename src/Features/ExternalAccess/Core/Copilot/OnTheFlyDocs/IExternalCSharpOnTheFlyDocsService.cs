// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal interface IExternalCSharpOnTheFlyDocsService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
    Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken);
    Task<string> GetOnTheFlyDocsPromptAsync(CopilotOnTheFlyDocsInfoWrapper onTheFlyDocsInfo, CancellationToken cancellationToken);
    Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseAsync(string prompt, CancellationToken cancellationToken);
}
