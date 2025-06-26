// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// Represents a FixAllProvider for code fixes or refactorings. 
/// </summary>
internal interface IFixAllProvider
{
    /// <summary>
    /// Gets the supported scopes for applying multiple occurrences of a code refactoring.
    /// By default, it returns the following scopes:
    /// (a) <see cref="FixAllScope.Document"/>
    /// (b) <see cref="FixAllScope.Project"/> and
    /// (c) <see cref="FixAllScope.Solution"/>
    /// </summary>
    IEnumerable<FixAllScope> GetSupportedFixAllScopes();

    Task<CodeAction?> GetFixAsync(IFixAllContext fixAllContext);

    /// <summary>
    /// The sort of cleanup that should automatically be poerformed for this fix all provider.  By default this is
    /// <see cref="CodeActionCleanup.Default"/>.
    /// </summary>
    CodeActionCleanup Cleanup { get; }
}
