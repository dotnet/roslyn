// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace ImplementNotifyPropertyChangedCS
{
    [ExportCodeRefactoringProvider("ImplementNotifyPropertyChangedCS", LanguageNames.CSharp)]
    internal partial class ImplementNotifyPropertyChangedCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var properties = ExpansionChecker.GetExpandableProperties(span, root, model);

            return properties.Any()
                ? new[] { new ImplementNotifyPropertyChangedCodeAction("Apply INotifyPropertyChanged pattern", (c) => ImplementNotifyPropertyChangedAsync(document, root, model, properties, c)) }
                : null;
        }

        private async Task<Document> ImplementNotifyPropertyChangedAsync(Document document, CompilationUnitSyntax root, SemanticModel model, IEnumerable<ExpandablePropertyInfo> properties, CancellationToken cancellationToken)
        {
            document = document.WithSyntaxRoot(CodeGeneration.ImplementINotifyPropertyChanged(root, model, properties, document.Project.Solution.Workspace));
            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            return document;
        }

        private class ImplementNotifyPropertyChangedCodeAction : CodeAction
        {
            private Func<CancellationToken, Task<Document>> createDocument;
            private string title;

            public ImplementNotifyPropertyChangedCodeAction(string title, Func<CancellationToken, Task<Document>> createDocument)
            {
                this.title = title;
                this.createDocument = createDocument;
            }

            public override string Title { get { return title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return this.createDocument(cancellationToken);
            }
        }
    }
}
