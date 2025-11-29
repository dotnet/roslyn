// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddImport;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddImport), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpAddImportCodeRefactoringProvider()
    : AbstractAddImportCodeRefactoringProvider<
        ExpressionSyntax,
        MemberAccessExpressionSyntax,
        NameSyntax,
        SimpleNameSyntax,
        QualifiedNameSyntax,
        AliasQualifiedNameSyntax,
        UsingDirectiveSyntax>(CSharpSyntaxFacts.Instance)
{
    protected override string AddImportTitle => CSharpFeaturesResources.Add_using_for_0;
    protected override string AddImportAndSimplifyAllOccurrencesTitle => CSharpFeaturesResources.Add_using_for_0_and_simplify_all_occurrences;
}
