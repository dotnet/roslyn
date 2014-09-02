// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2214: Do not call overridable methods in constructors
    /// 
    /// Cause: The constructor of an unsealed type calls a virtual method defined in its class.
    /// 
    /// Description: When a virtual method is called, the actual type that executes the method is not selected 
    /// until run time. When a constructor calls a virtual method, it is possible that the constructor for the 
    /// instance that invokes the method has not executed. 
    /// </summary>
    public abstract class CA2214DiagnosticAnalyzer : ICodeBlockNestedAnalyzerFactory
    {
        internal const string RuleId = "CA2214";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.DoNotCallOverridableMethodsInConstructors,
                                                                         FxCopRulesResources.DoNotCallOverridableMethodsInConstructors,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: FxCopRulesResources.DoNotCallOverridableMethodsInConstructorsDescription,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182331.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public IDiagnosticAnalyzer CreateAnalyzerWithinCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            return ShouldOmitThisDiagnostic(ownerSymbol, semanticModel.Compilation) ?
                null :
                GetCodeBlockEndedAnalyzer(ownerSymbol as IMethodSymbol);
        }

        protected abstract IDiagnosticAnalyzer GetCodeBlockEndedAnalyzer(IMethodSymbol constructorSymbol);

        private static bool ShouldOmitThisDiagnostic(ISymbol symbol, Compilation compilation)
        {
            // This diagnostic is only relevant in constructors.
            // TODO: should this apply to instance field initializers for VB?
            var m = symbol as IMethodSymbol;
            if (m == null || m.MethodKind != MethodKind.Constructor)
            {
                return true;
            }

            var containingType = m.ContainingType;
            if (containingType == null)
            {
                return true;
            }

            // special case ASP.NET and WinForms constructors
            INamedTypeSymbol webUiControlType = compilation.GetTypeByMetadataName("System.Web.UI.Control");
            if (containingType.Inherits(webUiControlType))
            {
                return true;
            }

            INamedTypeSymbol windowsFormsControlType = compilation.GetTypeByMetadataName("System.Windows.Forms.Control");
            if (containingType.Inherits(windowsFormsControlType))
            {
                return true;
            }

            return false;
        }

        protected abstract class AbstractSyntaxNodeAnalyzer : IDiagnosticAnalyzer
        {
            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }
        }
    }
}
