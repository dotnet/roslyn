// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings
{
    /// <summary>
    /// Represents a FixAllProvider for code fixes or refactorings. 
    /// </summary>
    internal interface IFixAllProvider
    {
        IEnumerable<FixAllScope> GetSupportedFixAllScopes();
        Task<CodeAction?> GetFixAsync(IFixAllContext fixAllContext);
    }
}
