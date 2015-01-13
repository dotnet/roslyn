// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    public abstract class CA2200DiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA2200";
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(FxCopRulesResources.RethrowToPreserveStackDetails), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString localizableMessage = new LocalizableResourceString(nameof(FxCopRulesResources.RethrowException), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         localizableTitle,
                                                                         localizableMessage,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182363.aspx",
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
