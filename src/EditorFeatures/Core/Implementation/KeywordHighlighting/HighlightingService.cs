// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    [Export(typeof(IHighlightingService))]
    [Shared]
    internal class HighlightingService : IHighlightingService
    {
        private readonly List<Lazy<IHighlighter, LanguageMetadata>> _highlighters;

        [ImportingConstructor]
        public HighlightingService(
            [ImportMany] IEnumerable<Lazy<IHighlighter, LanguageMetadata>> highlighters)
        {
            _highlighters = highlighters.ToList();
        }

        public IEnumerable<TextSpan> GetHighlights(
             SyntaxNode root, int position, CancellationToken cancellationToken)
        {
            try
            {
                return _highlighters.Where(h => h.Metadata.Language == root.Language)
                                   .Select(h => h.Value.GetHighlights(root, position, cancellationToken))
                                   .WhereNotNull()
                                   .Flatten()
                                   .Distinct();
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/763988
                // due to await, we do not get dump with right callstack. this should let
                // us to catch exception at the point where we can get good dump
                // also, change crash to NFW. high lighting failure is not important enough
                // to crash VS
                return SpecializedCollections.EmptyEnumerable<TextSpan>();
            }
        }
    }
}
