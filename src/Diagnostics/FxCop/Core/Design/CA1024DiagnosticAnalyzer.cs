// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1024: Use properties where appropriate
    /// 
    /// Cause:
    /// A public or protected method has a name that starts with Get, takes no parameters, and returns a value that is not an array.
    /// </summary>
    public abstract class CA1024DiagnosticAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
    {
        internal const string RuleId = "CA1024";
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FxCopRulesResources.UsePropertiesWhereAppropriate), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(FxCopRulesResources.ChangeToAPropertyIfAppropriate), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(FxCopRulesResources.UsePropertiesWhereAppropriateDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableTitle,
                                                                         s_localizableMessage,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: false,
                                                                         description: s_localizableDescription,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182181.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);
        private const string GetHashCodeName = "GetHashCode";
        private const string GetEnumeratorName = "GetEnumerator";

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
                    var methodSymbol = context.OwningSymbol as IMethodSymbol;

                    if (methodSymbol == null ||
                        methodSymbol.ReturnsVoid ||
                        methodSymbol.ReturnType.Kind == SymbolKind.ArrayType ||
                        methodSymbol.Parameters.Length > 0 ||
                        !(methodSymbol.DeclaredAccessibility == Accessibility.Public || methodSymbol.DeclaredAccessibility == Accessibility.Protected) ||
                        methodSymbol.IsAccessorMethod() ||
                        !IsPropertyLikeName(methodSymbol.Name))
                    {
                        return;
                    }

                    // Fxcop has a few additional checks to reduce the noise for this diagnostic:
                    // Ensure that the method is non-generic, non-virtual/override, has no overloads and doesn't have special names: 'GetHashCode' or 'GetEnumerator'.
                    // Also avoid generating this diagnostic if the method body has any invocation expressions.
                    if (methodSymbol.IsGenericMethod ||
                        methodSymbol.IsVirtual ||
                        methodSymbol.IsOverride ||
                        methodSymbol.ContainingType.GetMembers(methodSymbol.Name).Length > 1 ||
                        methodSymbol.Name == GetHashCodeName ||
                        methodSymbol.Name == GetEnumeratorName)
                    {
                        return;
                    }

                    CA1024CodeBlockEndedAnalyzer analyzer = GetCodeBlockEndedAnalyzer();
                    context.RegisterCodeBlockEndAction(analyzer.AnalyzeCodeBlock);
                    context.RegisterSyntaxNodeAction(analyzer.AnalyzeNode, analyzer.SyntaxKindOfInterest);
                });
        }

        protected abstract CA1024CodeBlockEndedAnalyzer GetCodeBlockEndedAnalyzer();

        protected abstract class CA1024CodeBlockEndedAnalyzer
        {
            protected bool suppress;

            protected abstract Location GetDiagnosticLocation(SyntaxNode node);

            public abstract TLanguageKindEnum SyntaxKindOfInterest { get; }

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                // We are analyzing an invocation expression node. This method is suffiently complex to suppress the diagnostic.
                suppress = true;
            }

            public void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
            {
                if (!suppress)
                {
                    context.ReportDiagnostic(GetDiagnosticLocation(context.CodeBlock).CreateDiagnostic(Rule, context.OwningSymbol.Name));
                }
            }
        }

        private static bool IsPropertyLikeName(string methodName)
        {
            return methodName.Length > 3 &&
                methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase) &&
                !char.IsLower(methodName[3]);
        }
    }
}
