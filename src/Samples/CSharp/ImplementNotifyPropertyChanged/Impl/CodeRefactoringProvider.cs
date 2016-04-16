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
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // if length is 0 then no particular range is selected, so pick the first enclosing member
            if (textSpan.Length == 0)
            {
                var decl = root.FindToken(textSpan.Start).Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                if (decl != null)
                {
                    textSpan = decl.FullSpan;
                }
            }

            var properties = ExpansionChecker.GetExpandableProperties(textSpan, root, model);

            if (properties.Any())
            {
#pragma warning disable RS0005
                context.RegisterRefactoring(
                   CodeAction.Create("Apply INotifyPropertyChanged pattern", (c) =>
                                     ImplementNotifyPropertyChangedAsync(document, root, model, properties, c)));
#pragma warning restore RS0005
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
