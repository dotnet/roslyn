// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// Allows for generic handling of a <see cref="FixAllProvider"/> or <see cref="RefactorAllProvider"/>.
/// </summary>
internal interface IRefactorOrFixAllProvider
{
    Task<CodeAction?> GetCodeActionAsync(IRefactorOrFixAllContext fixAllContext);

    /// <summary>
    /// The sort of cleanup that should automatically be poerformed for this fix all provider.  By default this is
    /// <see cref="CodeActionCleanup.Default"/>.
    /// </summary>
    CodeActionCleanup Cleanup { get; }
}
