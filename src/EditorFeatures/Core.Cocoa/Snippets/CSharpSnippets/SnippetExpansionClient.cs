// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Expansion;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    internal sealed partial class SnippetExpansionClient : AbstractSnippetExpansionClient
    {
        public SnippetExpansionClient(IContentType languageServiceGuid, ITextView textView, ITextBuffer subjectBuffer, IExpansionServiceProvider expansionServiceProvider, IGlobalOptionService globalOptions)
            : base(languageServiceGuid, textView, subjectBuffer, expansionServiceProvider, globalOptions)
        {
        }

        /// <returns>The tracking span of the inserted "/**/" if there is an $end$ location, null
        /// otherwise.</returns>
        protected override ITrackingSpan? InsertEmptyCommentAndGetEndPositionTrackingSpan()
        {
            Contract.ThrowIfNull(ExpansionSession);

            var endSpanInSurfaceBuffer = ExpansionSession.EndSpan;
            if (!TryGetSubjectBufferSpan(endSpanInSurfaceBuffer, out var subjectBufferEndSpan))
            {
                return null;
            }

            var endPosition = subjectBufferEndSpan.End.Position;

            var commentString = "/**/";
            SubjectBuffer.Insert(endPosition, commentString);

            var commentSpan = new Span(endPosition, commentString.Length);
            return SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(commentSpan, SpanTrackingMode.EdgeExclusive);
        }

        public override IExpansionFunction? GetExpansionFunction(XElement xmlFunctionNode, string fieldName)
        {
            if (!TryGetSnippetFunctionInfo(xmlFunctionNode, out var snippetFunctionName, out var param))
            {
                throw new ArgumentException();
            }

            return snippetFunctionName switch
            {
                "SimpleTypeName" => new SnippetFunctionSimpleTypeName(this, SubjectBuffer, fieldName, param),
                "ClassName" => new SnippetFunctionClassName(this, SubjectBuffer, fieldName),
                "GenerateSwitchCases" => new SnippetFunctionGenerateSwitchCases(this, SubjectBuffer, fieldName, param),
                _ => null,
            };
        }
    }
}
