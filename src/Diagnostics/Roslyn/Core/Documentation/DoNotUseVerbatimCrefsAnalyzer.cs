// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseProperCrefTagsTitle), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseProperCrefTagsMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UseProperCrefTagsDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DoNotUseVerbatimCrefsRuleId,
            title: s_localizableTitle,
            messageFormat: s_localizableMessage,
            category: "Documentation",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry,
            description: s_localizableDescription);

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
