// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.NameLiteralArgument
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed class CSharpNameLiteralArgumentCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.NameLiteralArgumentDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var argument = (ArgumentSyntax)root.FindNode(diagnostic.Location.SourceSpan);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var parameterName = GetParameterName(argument, semanticModel);
                var newArgument = SyntaxFactory.Argument(SyntaxFactory.NameColon(parameterName), default, argument.Expression);
                editor.ReplaceNode(argument, newArgument);
            }
        }

        private string GetParameterName(ArgumentSyntax argument, SemanticModel semanticModel)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments;
            switch (argument.Parent.Parent)
            {
                case InvocationExpressionSyntax invocation:
                    arguments = invocation.ArgumentList.Arguments;
                    break;
                case ObjectCreationExpressionSyntax creation:
                    arguments = creation.ArgumentList.Arguments;
                    break;
                case ConstructorInitializerSyntax creation:
                    arguments = creation.ArgumentList.Arguments;
                    break;
                case ElementAccessExpressionSyntax access:
                    arguments = access.ArgumentList.Arguments;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(argument.Parent.Parent.Kind());
            }

            int index = arguments.IndexOf(argument);
            var parameters = semanticModel.GetSymbolInfo(argument.Parent.Parent).Symbol.GetParameters();
            return parameters[index].Name;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Name_literal_argument, createChangedDocument, FeaturesResources.Name_literal_argument)
            {
            }
        }
    }
}
