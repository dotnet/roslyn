// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

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
    public abstract class CA2214DiagnosticAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
    {
        public const string RuleId = "CA2214";
        private static readonly LocalizableString s_localizableMessageAndTitle = new LocalizableResourceString(nameof(FxCopRulesResources.DoNotCallOverridableMethodsInConstructors), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(FxCopRulesResources.DoNotCallOverridableMethodsInConstructorsDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableMessageAndTitle,
                                                                         s_localizableMessageAndTitle,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: s_localizableDescription,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182331.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCodeBlockStartAction<TLanguageKindEnum>(
                (context) =>
                {
                    if (ShouldOmitThisDiagnostic(context.OwningSymbol, context.SemanticModel.Compilation))
                    {
                        return;
                    }

                    GetCodeBlockEndedAnalyzer(context, context.OwningSymbol as IMethodSymbol);
                });
        }

        protected abstract void GetCodeBlockEndedAnalyzer(CodeBlockStartAnalysisContext<TLanguageKindEnum> context, IMethodSymbol constructorSymbol);

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
    }
}
