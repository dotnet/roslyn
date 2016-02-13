// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface ISyntaxFormattingService : ILanguageService
    {
        IEnumerable<IFormattingRule> GetDefaultFormattingRules();
        Task<IFormattingResult> FormatAsync(SyntaxNode node, IEnumerable<TextSpan> spans, OptionSet options, IEnumerable<IFormattingRule> rules, CancellationToken cancellationToken);
    }
}
