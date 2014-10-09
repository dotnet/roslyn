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
using Microsoft.CodeAnalysis.Text;

namespace ImplementNotifyPropertyChangedCS
{
    [ExportCodeRefactoringProvider("ImplementNotifyPropertyChangedCS", LanguageNames.CSharp), Shared]
    internal partial class ImplementNotifyPropertyChangedCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
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

#pragma warning disable RS005
            return properties.Any()
                ? new[] { CodeAction.Create("Apply INotifyPropertyChanged pattern", (c) => ImplementNotifyPropertyChangedAsync(document, root, model, properties, c)) }
                : null;
#pragma warning restore RS005
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
