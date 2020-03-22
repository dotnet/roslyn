// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.AddDebuggerDisplay;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpAddDebuggerDisplayCodeRefactoringProvider)), Shared]
    internal sealed class CSharpAddDebuggerDisplayCodeRefactoringProvider
        : AbstractAddDebuggerDisplayCodeRefactoringProvider<
            TypeDeclarationSyntax,
            MethodDeclarationSyntax>
    {
        [ImportingConstructor]
        public CSharpAddDebuggerDisplayCodeRefactoringProvider()
        {
        }

        protected override bool CanNameofAccessNonPublicMembersFromAttributeArgument => true;
    }
}
