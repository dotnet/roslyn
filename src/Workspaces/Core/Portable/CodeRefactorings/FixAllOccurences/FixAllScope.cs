// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Indicates scope for "Fix all occurrences" code fixes provided by each <see cref="RefactorAllProvider"/>.
/// </summary>
internal enum RefactorAllScope
{
    /// <summary>
    /// Scope to fix all occurrences of diagnostic(s) in the entire document.
    /// </summary>
    Document,

    /// <summary>
    /// Scope to fix all occurrences of diagnostic(s) in the entire project.
    /// </summary>
    Project,

    /// <summary>
    /// Scope to fix all occurrences of diagnostic(s) in the entire solution.
    /// </summary>
    Solution,

    /// <summary>
    /// Custom scope to fix all occurrences of diagnostic(s). This scope can
    /// be used by custom <see cref="RefactorAllProvider"/>s and custom code fix engines.
    /// </summary>
    Custom,

    /// <summary>
    /// Scope to fix all occurrences of diagnostic(s) in the containing member
    /// relative to the trigger span for the original code fix.
    /// </summary>
    ContainingMember,

    /// <summary>
    /// Scope to fix all occurrences of diagnostic(s) in the containing type
    /// relative to the trigger span for the original code fix.
    /// </summary>
    ContainingType,
}

internal static class RefactorAllScopeExtensions
{
    static RefactorAllScopeExtensions()
    {
#if DEBUG
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
}
