// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer;

/// <summary>
/// Copilot code analysis service that coordinates triggering Copilot code analysis
/// in the background for a given document.
/// This service caches the computed Copilot suggestion diagnostics by method body to ensure that
/// we do not perform duplicate analysis calls.
/// Additionally, it performs all the option checks and Copilot service availability checks
/// to determine if we should skip analysis or not.
/// </summary>
internal abstract class AbstractCopilotCodeAnalysisService : ICopilotCodeAnalysisService
{
    /// <summary>
    /// Guid for UI context that is set from Copilot when the package is initialized
    /// </summary>
    private const string CopilotHasLoadedGuid = "871c3e1c-e58c-4ce9-b6a7-26600555739a";

    /// <summary>
    /// Guid for UI Context that is set from Copilot when sign in related UI contexts have been set properly. Used to determine when UI context status is final
    /// for a set of operations. When this UI context is not active, the signed in and entitled contexts values may not be correct.
    /// </summary>
    private const string GitHubAccountStatusDetermined = "3049be7e-71ee-4045-a510-f8ee1a967723";

    /// <summary>
    /// Guid for UI context that is set from Copilot when we detect a GitHub account is signed in.
    /// </summary>
    private const string GitHubAccountStatusSignedIn = "ef3ebbb7-511d-472c-ae4b-6af1bb44f378";

    /// <summary>
    /// Guid for UI context that is set from VS Identity Service when we detect that a signed in GitHub account is entitled to access Copilot.
    /// </summary>
    private const string GitHubAccountStatusIsCopilotEntitled = "3DE3FA6E-91B2-46C1-9E9E-DD04975BB890";

    private static readonly UIContext s_copilotHasLoadedUIContext = UIContext.FromUIContextGuid(new Guid(CopilotHasLoadedGuid));
    private static readonly UIContext s_gitHubAccountStatusDeterminedContext = UIContext.FromUIContextGuid(new Guid(GitHubAccountStatusDetermined));
    private static readonly UIContext s_gitHubAccountStatusIsCopilotEntitledUIContext = UIContext.FromUIContextGuid(new Guid(GitHubAccountStatusIsCopilotEntitled));
    private static readonly UIContext s_gitHubAccountStatusSignedInUIContext = UIContext.FromUIContextGuid(new Guid(GitHubAccountStatusSignedIn));

    protected static bool IsCopilotSignedIn
        => s_copilotHasLoadedUIContext.IsActive
        && s_gitHubAccountStatusDeterminedContext.IsActive
        && s_gitHubAccountStatusSignedInUIContext.IsActive
        && s_gitHubAccountStatusIsCopilotEntitledUIContext.IsActive;

    public abstract bool IsCopilotAvailable { get; }

    public abstract Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken);

    public abstract Task<string> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken);

    public abstract Task<bool> IsAnyExclusionAsync(CancellationToken cancellationToken);
}
