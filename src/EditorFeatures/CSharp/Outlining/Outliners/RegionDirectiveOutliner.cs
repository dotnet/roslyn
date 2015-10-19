// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class RegionDirectiveOutliner : AbstractSyntaxNodeOutliner<RegionDirectiveTriviaSyntax>
    {
        private static string GetBannerText(DirectiveTriviaSyntax simpleDirective)
        {
            var kw = simpleDirective.DirectiveNameToken;
            var prefixLength = kw.Span.End - simpleDirective.Span.Start;
            var text = simpleDirective.ToString().Substring(prefixLength).Trim();

            if (text.Length == 0)
            {
                return simpleDirective.HashToken.ToString() + kw.ToString();
            }
            else
            {
                return text;
            }
        }

        protected override void CollectOutliningSpans(
            RegionDirectiveTriviaSyntax regionDirective,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            var match = regionDirective.GetMatchingDirective(cancellationToken);
            if (match != null)
            {
                spans.Add(new OutliningSpan(
                    TextSpan.FromBounds(regionDirective.SpanStart, match.Span.End),
                    GetBannerText(regionDirective),
                    autoCollapse: true,
                    isDefaultCollapsed: true));
            }
        }

        protected override bool SupportedInWorkspaceKind(string kind)
        {
            return kind != WorkspaceKind.MetadataAsSource;
        }
    }
}
