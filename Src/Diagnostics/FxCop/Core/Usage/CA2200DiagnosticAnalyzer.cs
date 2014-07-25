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
    public abstract class CA2200DiagnosticAnalyzer : IDiagnosticAnalyzer
    {
        internal const string RuleId = "CA2200";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.RethrowToPreserveStackDetails,
                                                                         FxCopRulesResources.RethrowException,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
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
