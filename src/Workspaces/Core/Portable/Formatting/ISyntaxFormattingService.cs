// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface ISyntaxFormattingService
#if !CODE_STYLE
        : ILanguageService
#endif
    {
        IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules();
        IFormattingResult Format(SyntaxNode node, IEnumerable<TextSpan> spans, OptionSet options, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken);
    }
}
