// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Defines a <see cref="CodeRefactoringProvider"/> which does not register code refactorings for any source spans.
    /// </summary>
    public sealed class EmptyCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            => Task.FromResult(true);
    }
}
