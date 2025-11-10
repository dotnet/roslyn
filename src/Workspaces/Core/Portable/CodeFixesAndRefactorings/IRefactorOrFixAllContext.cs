// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// Represents a <see cref="FixAllContext"/> or <see cref="RefactorAllContext"/>.
/// </summary>
internal interface IRefactorOrFixAllContext
{
    IRefactorOrFixAllState State { get; }
    IRefactorOrFixProvider Provider { get; }
    CancellationToken CancellationToken { get; }
    IProgress<CodeAnalysisProgress> Progress { get; }

    string GetDefaultTitle();
    IRefactorOrFixAllContext With(
        Optional<(Document? document, Project project)> documentAndProject = default,
        Optional<FixAllScope> scope = default,
        Optional<string?> codeActionEquivalenceKey = default,
        Optional<CancellationToken> cancellationToken = default);
}
