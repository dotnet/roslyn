// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Compilers;
using Microsoft.CodeAnalysis.Compilers.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Services.Editor.Implementation.GenerateMember.GenerateFieldOrProperty;
using Microsoft.CodeAnalysis.Services.Shared.CodeGeneration;
using Microsoft.CodeAnalysis.Services.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Services.Editor.CSharp.CodeActions.GenerateFieldOrProperty
{
    [Order(Before = PredefinedCodeActionProviderNames.GenerateType)]
    [ExportSyntaxNodeCodeIssueProvider(PredefinedCodeActionProviderNames.GenerateFieldOrProperty, LanguageNames.CSharp,
        typeof(SimpleNameSyntax),
        typeof(PropertyDeclarationSyntax))]
    internal partial class GenerateFieldOrPropertyCodeIssueProvider : AbstractCSharpCodeIssueProvider
    {
        [ImportingConstructor]
        public GenerateFieldOrPropertyCodeIssueProvider(
            ILanguageServiceProviderFactory languageServiceProviderFactory,
            ICodeDefinitionFactory codeDefinitionFactory)
            : base(languageServiceProviderFactory, codeDefinitionFactory)
        {
        }

        protected override CodeIssue GetIssue(
            IDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            // NOTE(DustinCa): Not supported in REPL for now.
            if (document.GetSyntaxTree(cancellationToken).Options.Kind == SourceCodeKind.Interactive)
            {
                return null;
            }

            var service = document.GetLanguageService<IGenerateFieldOrPropertyService>();
            var result = service.GenerateFieldOrProperty(document, node, cancellationToken);
            if (!result.ContainsChanges)
            {
                return null;
            }

            return result.GetCodeIssue(cancellationToken);
        }
    }
}
