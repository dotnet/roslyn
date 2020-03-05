// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Inherit this type to provide source code refactorings.
    /// Remember to use <see cref="ExportCodeRefactoringProviderAttribute"/> so the host environment can offer your refactorings in a UI.
    /// </summary>
    public abstract class CodeRefactoringProvider
    {
        /// <summary>
        /// Computes one or more refactorings for the specified <see cref="CodeRefactoringContext"/>.
        /// </summary>
        public abstract Task ComputeRefactoringsAsync(CodeRefactoringContext context);
    }
}
