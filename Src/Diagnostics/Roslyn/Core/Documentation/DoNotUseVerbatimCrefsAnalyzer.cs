using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.Documentation
{
    public abstract class DoNotUseVerbatimCrefsAnalyzer : DiagnosticAnalyzer
    {
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DoNotUseVerbatimCrefsRuleId,
            RoslynDiagnosticsResources.UseProperCrefTagsDescription,
            RoslynDiagnosticsResources.UseProperCrefTagsMessage,
            "Documentation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        protected void ProcessAttribute(SyntaxNodeAnalysisContext context, SyntaxTokenList textTokens)
        {
            var token = textTokens.First();

            if (token.Span.Length >= 2)
            {
                var text = token.Text;

                if (text[1] == ':')
                {
                    var location = Location.Create(token.SyntaxTree, textTokens.Span);
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location, text.Substring(0, 2)));
                }
            }
        }
    }
}
