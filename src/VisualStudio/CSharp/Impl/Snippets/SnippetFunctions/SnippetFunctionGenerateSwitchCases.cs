// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets.SnippetFunctions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions
{
    internal sealed class SnippetFunctionGenerateSwitchCases : AbstractSnippetFunctionGenerateSwitchCases
    {
        public SnippetFunctionGenerateSwitchCases(SnippetExpansionClient snippetExpansionClient, ITextView textView, ITextBuffer subjectBuffer, string caseGenerationLocationField, string switchExpressionField)
            : base(snippetExpansionClient, textView, subjectBuffer, caseGenerationLocationField, switchExpressionField)
        {
        }

        protected override string CaseFormat
        {
            get
            {
                return @"case {0}.{1}:
 break;
";
            }
        }

        protected override string DefaultCase
        {
            get
            {
                return @"default:
 break;";
            }
        }

        protected override bool TryGetEnumTypeSymbol(CancellationToken cancellationToken, out ITypeSymbol typeSymbol)
        {
            typeSymbol = null;
            if (!TryGetDocument(out var document))
            {
                return false;
            }

            var surfaceBufferFieldSpan = new VsTextSpan[1];
            if (snippetExpansionClient.ExpansionSession.GetFieldSpan(SwitchExpressionField, surfaceBufferFieldSpan) != VSConstants.S_OK)
            {
                return false;
            }

            if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan[0], out var subjectBufferFieldSpan))
            {
                return false;
            }

            var expressionSpan = subjectBufferFieldSpan.Span.ToTextSpan();

            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var token = syntaxTree.FindTokenOnRightOfPosition(expressionSpan.Start, cancellationToken);
            var expressionNode = token.GetAncestor(n => n.Span == expressionSpan);

            if (expressionNode == null)
            {
                return false;
            }

            var model = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            typeSymbol = model.GetTypeInfo(expressionNode, cancellationToken).Type;

            return typeSymbol != null;
        }

        protected override bool TryGetSimplifiedTypeNameInCaseContext(Document document, string fullyQualifiedTypeName, string firstEnumMemberName, int startPosition, int endPosition, CancellationToken cancellationToken, out string simplifiedTypeName)
        {
            simplifiedTypeName = string.Empty;
            var typeAnnotation = new SyntaxAnnotation();

            var str = "case " + fullyQualifiedTypeName + "." + firstEnumMemberName + ":" + Environment.NewLine + " break;";
            var textChange = new TextChange(new TextSpan(startPosition, endPosition - startPosition), str);
            var typeSpanToAnnotate = new TextSpan(startPosition + "case ".Length, fullyQualifiedTypeName.Length);

            var textWithCaseAdded = document.GetTextSynchronously(cancellationToken).WithChanges(textChange);
            var documentWithCaseAdded = document.WithText(textWithCaseAdded);

            var syntaxRoot = documentWithCaseAdded.GetSyntaxRootSynchronously(cancellationToken);
            var nodeToReplace = syntaxRoot.DescendantNodes().FirstOrDefault(n => n.Span == typeSpanToAnnotate);

            if (nodeToReplace == null)
            {
                return false;
            }

            var updatedRoot = syntaxRoot.ReplaceNode(nodeToReplace, nodeToReplace.WithAdditionalAnnotations(typeAnnotation, Simplifier.Annotation));
            var documentWithAnnotations = documentWithCaseAdded.WithSyntaxRoot(updatedRoot);

            var simplifiedDocument = Simplifier.ReduceAsync(documentWithAnnotations, cancellationToken: cancellationToken).Result;
            simplifiedTypeName = simplifiedDocument.GetSyntaxRootSynchronously(cancellationToken).GetAnnotatedNodesAndTokens(typeAnnotation).Single().ToString();
            return true;
        }
    }
}
