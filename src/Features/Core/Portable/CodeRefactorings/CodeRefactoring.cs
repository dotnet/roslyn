// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Represents a set of transformations that can be applied to a piece of code.
/// </summary>
internal class CodeRefactoring
{
    public CodeRefactoringProvider Provider { get; }

    /// <summary>
    /// List of tuples of possible actions that can be used to transform the code the TextSpan within the original document they're applicable to.
    /// </summary>
    /// <remarks>
    /// applicableToSpan should represent a logical section within the original document that the action is 
    /// applicable to. It doesn't have to precisely represent the exact <see cref="TextSpan"/> that will get changed.
    /// </remarks>
    public ImmutableArray<(CodeAction action, TextSpan? applicableToSpan)> CodeActions { get; }

    public FixAllProviderInfo? FixAllProviderInfo { get; }

    public CodeRefactoring(
        CodeRefactoringProvider provider,
        ImmutableArray<(CodeAction, TextSpan?)> actions,
        FixAllProviderInfo? fixAllProviderInfo)
    {
        Provider = provider;
        CodeActions = actions.NullToEmpty();
        FixAllProviderInfo = fixAllProviderInfo;

        if (CodeActions.IsEmpty)
        {
            throw new ArgumentException(FeaturesResources.Actions_can_not_be_empty, nameof(actions));
        }
    }
}
