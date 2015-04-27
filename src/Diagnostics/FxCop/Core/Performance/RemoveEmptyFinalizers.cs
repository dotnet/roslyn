// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    public abstract class RemoveEmptyFinalizers<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
    {
        public const string RuleId = "CA1821";
        private static LocalizableString s_localizableMessageAndTitle = new LocalizableResourceString(nameof(FxCopRulesResources.RemoveEmptyFinalizers), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(FxCopRulesResources.RemoveEmptyFinalizersDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableMessageAndTitle,
                                                                         s_localizableMessageAndTitle,
                                                                         FxCopDiagnosticCategory.Performance,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: s_localizableDescription,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/bb264476.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCodeBlockStartAction<TLanguageKindEnum>(
                (context) =>
                {
                    var method = context.OwningSymbol as IMethodSymbol;
                    if (method == null)
                    {
                        return;
                    }

                    if (!IsFinalizer(method))
                    {
                        return;
                    }
                    
                    context.RegisterCodeBlockEndAction(codeBlockContext =>
                    {
                        if (IsEmptyFinalizer(codeBlockContext.CodeBlock, context.SemanticModel))
                        {
                            codeBlockContext.ReportDiagnostic(codeBlockContext.OwningSymbol.CreateDiagnostic(Rule));
                        }
                    });
                });
        }

        
        private static bool IsFinalizer(IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.Destructor)
            {
                return true; // for C#
            }

            if (method.Name != "Finalize" || method.Parameters.Length != 0 || !method.ReturnsVoid)
            {
                return false;
            }

            var overridden = method.OverriddenMethod;
            if (overridden == null)
            {
                return false;
            }

            for (var o = overridden.OverriddenMethod; o != null; o = o.OverriddenMethod)
            {
                overridden = o;
            }

            return overridden.ContainingType.SpecialType == SpecialType.System_Object; // it is object.Finalize
        }

        protected abstract bool IsEmptyFinalizer(SyntaxNode node, SemanticModel model);
    }
}
