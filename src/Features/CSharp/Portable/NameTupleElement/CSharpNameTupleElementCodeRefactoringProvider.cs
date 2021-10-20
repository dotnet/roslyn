// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.NameTupleElement;

namespace Microsoft.CodeAnalysis.CSharp.NameTupleElement
{
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.NameTupleElement), Shared]
    internal class CSharpNameTupleElementCodeRefactoringProvider : AbstractNameTupleElementCodeRefactoringProvider<ArgumentSyntax, TupleExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpNameTupleElementCodeRefactoringProvider()
        {
        }

        protected override ArgumentSyntax WithName(ArgumentSyntax argument, string argumentName)
            => argument.WithNameColon(SyntaxFactory.NameColon(argumentName.ToIdentifierName()));
    }
}
