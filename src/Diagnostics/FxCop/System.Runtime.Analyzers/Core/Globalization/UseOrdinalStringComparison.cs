// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

namespace System.Runtime.Analyzers
{
    public abstract class UseOrdinalStringComparisonAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1309";

        private static LocalizableString s_localizableMessageAndTitle = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.StringComparisonShouldBeOrdinalOrOrdinalIgnoreCase), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));
        private static LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.StringComparisonShouldBeOrdinalDescription), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                             s_localizableMessageAndTitle,
                                                                             s_localizableMessageAndTitle,
                                                                             DiagnosticCategory.Globalization,
                                                                             DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: false,
                                                                             description: s_localizableDescription,
                                                                             helpLinkUri: "http://msdn.microsoft.com/library/bb385972.aspx",
                                                                             customTags: WellKnownDiagnosticTags.Telemetry);

        internal const string CompareMethodName = "Compare";
        internal const string EqualsMethodName = "Equals";
        internal const string OrdinalText = "Ordinal";
        internal const string OrdinalIgnoreCaseText = "OrdinalIgnoreCase";
        internal const string StringComparisonTypeName = "System.StringComparison";
        internal const string IgnoreCaseText = "IgnoreCase";

        protected abstract void GetAnalyzer(CompilationStartAnalysisContext context, INamedTypeSymbol stringComparisonType);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(
                (context) =>
                {
                    var stringComparisonType = context.Compilation.GetTypeByMetadataName(StringComparisonTypeName);
                    if (stringComparisonType != null)
                    {
                        GetAnalyzer(context, stringComparisonType);
                    }
                });
        }

        protected abstract class AbstractCodeBlockAnalyzer
        {
            protected INamedTypeSymbol StringComparisonType { get; }

            public AbstractCodeBlockAnalyzer(INamedTypeSymbol stringComparisonType)
            {
                this.StringComparisonType = stringComparisonType;
            }

            protected static bool IsEqualsOrCompare(string methodName)
            {
                return string.Equals(methodName, EqualsMethodName, StringComparison.Ordinal) ||
                    string.Equals(methodName, CompareMethodName, StringComparison.Ordinal);
            }

            protected static bool IsAcceptableOverload(IMethodSymbol methodSymbol, SemanticModel model)
            {
                var stringComparisonType = WellKnownTypes.StringComparison(model.Compilation);
                return methodSymbol.IsStatic
                    ? IsAcceptableStaticOverload(methodSymbol, stringComparisonType)
                    : IsAcceptableInstanceOverload(methodSymbol, stringComparisonType);
            }

            protected static bool IsAcceptableInstanceOverload(IMethodSymbol methodSymbol, INamedTypeSymbol stringComparisonType)
            {
                if (string.Equals(methodSymbol.Name, EqualsMethodName, StringComparison.Ordinal))
                {
                    switch (methodSymbol.Parameters.Length)
                    {
                        case 1:
                            // the instance method .Equals(object) is OK
                            return methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_Object;
                        case 2:
                            return methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
                                methodSymbol.Parameters[1].Type.Equals(stringComparisonType);
                    }
                }

                // all other overloads are unacceptable
                return false;
            }

            protected static bool IsAcceptableStaticOverload(IMethodSymbol methodSymbol, INamedTypeSymbol stringComparisonType)
            {
                if (string.Equals(methodSymbol.Name, CompareMethodName, StringComparison.Ordinal))
                {
                    switch (methodSymbol.Parameters.Length)
                    {
                        case 3:
                            // (string, string, StringComparison) is acceptable
                            return methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
                                methodSymbol.Parameters[1].Type.SpecialType == SpecialType.System_String &&
                                methodSymbol.Parameters[2].Type.Equals(stringComparisonType);
                        case 6:
                            // (string, int, string, int, int, StringComparison) is acceptable
                            return methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
                                methodSymbol.Parameters[1].Type.SpecialType == SpecialType.System_Int32 &&
                                methodSymbol.Parameters[2].Type.SpecialType == SpecialType.System_String &&
                                methodSymbol.Parameters[3].Type.SpecialType == SpecialType.System_Int32 &&
                                methodSymbol.Parameters[4].Type.SpecialType == SpecialType.System_Int32 &&
                                methodSymbol.Parameters[5].Type.Equals(stringComparisonType);
                    }
                }
                else if (string.Equals(methodSymbol.Name, EqualsMethodName, StringComparison.Ordinal))
                {
                    switch (methodSymbol.Parameters.Length)
                    {
                        case 2:
                            // (object, object) is acceptable
                            return methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                                methodSymbol.Parameters[1].Type.SpecialType == SpecialType.System_Object;
                        case 3:
                            // (string, string, StringComparison) is acceptable
                            return methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String &&
                                methodSymbol.Parameters[1].Type.SpecialType == SpecialType.System_String &&
                                methodSymbol.Parameters[2].Type.Equals(stringComparisonType);
                    }
                }

                // all other overloads are unacceptable
                return false;
            }

            protected static bool IsOrdinalOrOrdinalIgnoreCase(string name)
            {
                return string.Compare(name, OrdinalText, StringComparison.Ordinal) == 0 ||
                    string.Compare(name, OrdinalIgnoreCaseText, StringComparison.Ordinal) == 0;
            }
        }
    }
}
