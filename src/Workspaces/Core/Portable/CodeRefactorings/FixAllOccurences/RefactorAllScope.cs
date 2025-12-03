// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Indicates scope for "Refactor all occurrences" code fixes provided by each <see cref="RefactorAllProvider"/>.
/// </summary>
public enum RefactorAllScope
{
    /// <summary>
    /// Scope to refactor all occurrences of diagnostic(s) in the entire document.
    /// </summary>
    Document,

    /// <summary>
    /// Scope to refactor all occurrences of diagnostic(s) in the entire project.
    /// </summary>
    Project,

    /// <summary>
    /// Scope to refactor all occurrences of diagnostic(s) in the entire solution.
    /// </summary>
    Solution,

    /// <summary>
    /// Custom scope to refactor all occurrences of diagnostic(s). This scope can be used by custom <see
    /// cref="RefactorAllProvider"/>s and custom code refactoring engines.
    /// </summary>
    Custom,

    /// <summary>
    /// Scope to refactor all occurrences of diagnostic(s) in the containing member relative to the trigger span for the
    /// original code refactoring.
    /// </summary>
    ContainingMember,

    /// <summary>
    /// Scope to refactor all occurrences of diagnostic(s) in the containing type relative to the trigger span for the
    /// original code refactoring.
    /// </summary>
    ContainingType,
}

internal static class RefactorAllScopeExtensions
{
    static RefactorAllScopeExtensions()
    {
#if DEBUG
        // Ensures that RefactorAllScope and FixAllScope have the same set of values.

        var refactorFields = typeof(RefactorAllScope)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => (f.Name, Value: (int)f.GetValue(null)!));

        var fixAllFields = typeof(FixAllScope)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => (f.Name, Value: (int)f.GetValue(null)!));

        Contract.ThrowIfFalse(refactorFields.SetEquals(fixAllFields));
#endif
    }

    public static FixAllScope ToFixAllScope(this RefactorAllScope scope)
        => scope switch
        {
            RefactorAllScope.Document => FixAllScope.Document,
            RefactorAllScope.Project => FixAllScope.Project,
            RefactorAllScope.Solution => FixAllScope.Solution,
            RefactorAllScope.Custom => FixAllScope.Custom,
            RefactorAllScope.ContainingMember => FixAllScope.ContainingMember,
            RefactorAllScope.ContainingType => FixAllScope.ContainingType,
            _ => throw ExceptionUtilities.Unreachable(),
        };

    public static RefactorAllScope ToRefactorAllScope(this FixAllScope scope)
        => scope switch
        {
            FixAllScope.Document => RefactorAllScope.Document,
            FixAllScope.Project => RefactorAllScope.Project,
            FixAllScope.Solution => RefactorAllScope.Solution,
            FixAllScope.Custom => RefactorAllScope.Custom,
            FixAllScope.ContainingMember => RefactorAllScope.ContainingMember,
            FixAllScope.ContainingType => RefactorAllScope.ContainingType,
            _ => throw ExceptionUtilities.Unreachable(),
        };
}
