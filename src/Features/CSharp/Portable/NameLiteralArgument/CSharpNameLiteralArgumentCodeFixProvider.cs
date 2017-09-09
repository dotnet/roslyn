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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.NameLiteralArgument
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.NameArguments), Shared]
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

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var parameterName = diagnostic.Properties["ParameterName"];
                var node = root.FindNode(diagnostic.Location.SourceSpan);

                SyntaxNode newArgument;
                switch (node)
                {
                    case ArgumentSyntax argument:
                        newArgument = argument.WithoutTrivia()
                            .WithNameColon(SyntaxFactory.NameColon(parameterName)).WithTriviaFrom(argument);
                        break;
                    case AttributeArgumentSyntax argument:
                        newArgument = argument.WithoutTrivia()
                            .WithNameColon(SyntaxFactory.NameColon(parameterName)).WithTriviaFrom(argument);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(node.Kind());
                }

                editor.ReplaceNode(node, newArgument);
            }

            return Task.CompletedTask;
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
