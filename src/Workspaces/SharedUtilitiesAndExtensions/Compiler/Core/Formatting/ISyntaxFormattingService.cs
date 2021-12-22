// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
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
        IFormattingResult Format(SyntaxNode node, IEnumerable<TextSpan> spans, bool shouldUseFormattingSpanCollapse, AnalyzerConfigOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken);
    }

#if !CODE_STYLE
    internal static class ISyntaxFormattingServiceExtensions
    {
        internal static IFormattingResult GetFormattingResult(this ISyntaxFormattingService service, SyntaxNode node, IEnumerable<TextSpan> spans, OptionSet options, HostWorkspaceServices services, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
        {
            var optionService = services.GetRequiredService<IOptionService>();
            var shouldUseFormattingSpanCollapse = options.GetOption(FormattingBehaviorOptions.AllowDisjointSpanMerging);
            var configOptions = options.AsAnalyzerConfigOptions(optionService, node.Language);

            return service.Format(node, spans, shouldUseFormattingSpanCollapse, configOptions, rules, cancellationToken);
        }
    }
#endif
}
