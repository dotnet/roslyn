// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal static class CodeFixContextExtensions
    {
        /// <summary>
        /// Remove once https://github.com/dotnet/roslyn/issues/63457 is approved
        /// </summary>
        public static bool CanApplyChange(this CodeRefactoringContext context, ApplyChangesKind kind)
        {
            var solution = context.Document.Project.Solution;
#if CODE_STYLE
            return solution.Workspace.CanApplyChange(kind);
#else
            return solution.CanApplyChange(kind);
#endif
        }
    }
}
