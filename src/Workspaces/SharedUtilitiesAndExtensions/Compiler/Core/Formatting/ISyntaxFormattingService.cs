// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface ISyntaxFormattingService
#if !CODE_STYLE
        : ILanguageService
#endif
    {
        IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules();
        IFormattingResult Format(SyntaxNode node, IEnumerable<TextSpan> spans, bool shouldUseFormattingSpanCollapse, AnalyzerConfigOptions options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken);
    }
}
