// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Copilot;

/// <summary>
/// Provides options for Copilot features.
/// Created seperately from ICopilotCodeAnalysisService to avoid additiona; assembly load just for checking option values.
/// </summary>
internal interface ICopilotOptionsService : ILanguageService
{
    /// <summary>
    /// Returns true if Copilot on-the-fly docs feature is enabled.
    /// </summary>
    Task<bool> IsOnTheFlyDocsOptionEnabledAsync();

    /// <summary>
    /// Returns true if Copilot generate documentation comment feature is enabled.
    /// </summary>
    Task<bool> IsGenerateDocumentationCommentOptionEnabledAsync();

    /// <summary>
    /// Returns true if Copilot generate method implementation feature is enabled.
    /// </summary>
    Task<bool> IsImplementNotImplementedExceptionEnabledAsync();
}
