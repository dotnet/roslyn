// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptCodeFixContextExtensions
    {
        public static bool IsBlocking(this CodeFixContext context)
#pragma warning disable CS0612 // Type or member is obsolete
            => ((ITypeScriptCodeFixContext)context).IsBlocking;
#pragma warning restore

        public static bool IsBlocking(this CodeRefactoringContext context)
#pragma warning disable CS0612 // Type or member is obsolete
            => ((ITypeScriptCodeRefactoringContext)context).IsBlocking;
#pragma warning restore
    }
}
