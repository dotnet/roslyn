// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    internal sealed partial class SnippetExpansionClient : AbstractSnippetExpansionClient
    {
        public SnippetExpansionClient(Guid languageServiceGuid, ITextView textView, ITextBuffer subjectBuffer, IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(languageServiceGuid, textView, subjectBuffer, editorAdaptersFactoryService)
        {
        }

        /// <returns>The tracking span of the inserted "/**/" if there is an $end$ location, null
        /// otherwise.</returns>
        protected override ITrackingSpan InsertEmptyCommentAndGetEndPositionTrackingSpan()
        {
            VsTextSpan[] endSpanInSurfaceBuffer = new VsTextSpan[1];
            if (ExpansionSession.GetEndSpan(endSpanInSurfaceBuffer) != VSConstants.S_OK)
            {
                return null;
            }

            SnapshotSpan subjectBufferEndSpan;
            if (!TryGetSubjectBufferSpan(endSpanInSurfaceBuffer[0], out subjectBufferEndSpan))
            {
                return null;
            }

            var endPosition = subjectBufferEndSpan.Start.Position;

            string commentString = "/**/";
            SubjectBuffer.Insert(endPosition, commentString);

            var commentSpan = new Span(endPosition, commentString.Length);
            return SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(commentSpan, SpanTrackingMode.EdgeExclusive);
        }

        public override int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
        {
            string snippetFunctionName, param;
            if (!TryGetSnippetFunctionInfo(xmlFunctionNode, out snippetFunctionName, out param))
            {
                pFunc = null;
                return VSConstants.E_INVALIDARG;
            }

            switch (snippetFunctionName)
            {
                case "SimpleTypeName":
                    pFunc = new SnippetFunctionSimpleTypeName(this, TextView, SubjectBuffer, bstrFieldName, param);
                    return VSConstants.S_OK;
                case "ClassName":
                    pFunc = new SnippetFunctionClassName(this, TextView, SubjectBuffer, bstrFieldName);
                    return VSConstants.S_OK;
                case "GenerateSwitchCases":
                    pFunc = new SnippetFunctionGenerateSwitchCases(this, TextView, SubjectBuffer, bstrFieldName, param);
                    return VSConstants.S_OK;
                default:
                    pFunc = null;
                    return VSConstants.E_INVALIDARG;
            }
        }
    }
}