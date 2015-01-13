// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    internal class CA1821DiagnosticAnalyzerRule
    {
        public const string RuleId = "CA1821";
        private static LocalizableString localizableMessageAndTitle = new LocalizableResourceString(nameof(FxCopRulesResources.RemoveEmptyFinalizers), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString localizableDescription = new LocalizableResourceString(nameof(FxCopRulesResources.RemoveEmptyFinalizersDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         localizableMessageAndTitle,
                                                                         localizableMessageAndTitle,
                                                                         FxCopDiagnosticCategory.Performance,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: localizableDescription,
                                                                         helpLink: "http://msdn.microsoft.com/library/bb264476.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);
    }
}