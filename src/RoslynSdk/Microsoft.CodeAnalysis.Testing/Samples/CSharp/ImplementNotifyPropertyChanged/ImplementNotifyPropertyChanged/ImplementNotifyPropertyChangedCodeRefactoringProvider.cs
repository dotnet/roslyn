// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace ImplementNotifyPropertyChangedCS
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "ImplementNotifyPropertyChangedCS"), Shared]
    internal partial class ImplementNotifyPropertyChangedCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            Document document = context.Document;
            Microsoft.CodeAnalysis.Text.TextSpan textSpan = context.Span;
            CancellationToken cancellationToken = context.CancellationToken;

            CompilationUnitSyntax root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            SemanticModel model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // if length is 0 then no particular range is selected, so pick the first enclosing member
            if (textSpan.Length == 0)
            {
                MemberDeclarationSyntax decl = root.FindToken(textSpan.Start).Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                if (decl != null)
                {
                    textSpan = decl.FullSpan;
                }
            }

            IEnumerable<ExpandablePropertyInfo> properties = ExpansionChecker.GetExpandableProperties(textSpan, root, model);

            if (properties.Any())
            {
                CodeAction action = CodeAction.Create(
                    "Apply INotifyPropertyChanged pattern",
                    c => ImplementNotifyPropertyChangedAsync(document, root, model, properties, c),
                    equivalenceKey: nameof(ImplementNotifyPropertyChangedCodeRefactoringProvider));

                context.RegisterRefactoring(action);
            }
        }

        private async Task<Document> ImplementNotifyPropertyChangedAsync(Document document, CompilationUnitSyntax root, SemanticModel model, IEnumerable<ExpandablePropertyInfo> properties, CancellationToken cancellationToken)
        {
            document = document.WithSyntaxRoot(CodeGeneration.ImplementINotifyPropertyChanged(root, model, properties, document.Project.Solution.Workspace));
            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            return document;
        }
    }
}
