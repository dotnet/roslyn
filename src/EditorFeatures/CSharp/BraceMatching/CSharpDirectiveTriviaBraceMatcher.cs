// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class CSharpDirectiveTriviaBraceMatcher : AbstractDirectiveTriviaBraceMatcher<DirectiveTriviaSyntax,
        IfDirectiveTriviaSyntax, ElifDirectiveTriviaSyntax,
        ElseDirectiveTriviaSyntax, EndIfDirectiveTriviaSyntax,
        RegionDirectiveTriviaSyntax, EndRegionDirectiveTriviaSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpDirectiveTriviaBraceMatcher()
        {
        }

        internal override List<DirectiveTriviaSyntax>? GetMatchingConditionalDirectives(DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
                => directive.GetMatchingConditionalDirectives(cancellationToken)?.ToList();

        internal override DirectiveTriviaSyntax GetMatchingDirective(DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
                => directive.GetMatchingDirective(cancellationToken);

        internal override TextSpan GetSpanForTagging(DirectiveTriviaSyntax directive)
                => TextSpan.FromBounds(directive.HashToken.SpanStart, directive.DirectiveNameToken.Span.End);
    }
}
