// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.AnalyzerPowerPack.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AnalyzerPowerPack.Usage
{
    public abstract class CA2200DiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA2200";
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(AnalyzerPowerPackRulesResources.RethrowToPreserveStackDetails), AnalyzerPowerPackRulesResources.ResourceManager, typeof(AnalyzerPowerPackRulesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(AnalyzerPowerPackRulesResources.RethrowException), AnalyzerPowerPackRulesResources.ResourceManager, typeof(AnalyzerPowerPackRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableTitle,
                                                                         s_localizableMessage,
                                                                         AnalyzerPowerPackDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182363.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        protected Diagnostic CreateDiagnostic(SyntaxNode node)
        {
            return node.CreateDiagnostic(Rule);
        }
    }
}
