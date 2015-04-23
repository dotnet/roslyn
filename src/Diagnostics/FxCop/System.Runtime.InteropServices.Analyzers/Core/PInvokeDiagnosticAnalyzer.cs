// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.InteropServices.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class PInvokeDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string CA1401 = "CA1401";
        public const string CA2101 = "CA2101";

        private static LocalizableString s_localizableTitleCA1401 = new LocalizableResourceString(nameof(SystemRuntimeInteropServicesAnalyzersResources.PInvokesShouldNotBeVisible), SystemRuntimeInteropServicesAnalyzersResources.ResourceManager, typeof(SystemRuntimeInteropServicesAnalyzersResources));
        private static LocalizableString s_localizableMessageCA1401 = new LocalizableResourceString(nameof(SystemRuntimeInteropServicesAnalyzersResources.PInvokeMethodShouldNotBeVisible), SystemRuntimeInteropServicesAnalyzersResources.ResourceManager, typeof(SystemRuntimeInteropServicesAnalyzersResources));
        internal static DiagnosticDescriptor RuleCA1401 = new DiagnosticDescriptor(CA1401,
                                                                         s_localizableTitleCA1401,
                                                                         s_localizableMessageCA1401,
                                                                         DiagnosticCategory.Interoperability,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182209.aspx",
                                                                         customTags: WellKnownDiagnosticTags.Telemetry);

        private static LocalizableString s_localizableMessageAndTitleCA2101 = new LocalizableResourceString(nameof(SystemRuntimeInteropServicesAnalyzersResources.SpecifyMarshalingForPInvokeStringArguments), SystemRuntimeInteropServicesAnalyzersResources.ResourceManager, typeof(SystemRuntimeInteropServicesAnalyzersResources));
        private static LocalizableString s_localizableDescriptionCA2101 = new LocalizableResourceString(nameof(SystemRuntimeInteropServicesAnalyzersResources.SpecifyMarshalingForPInvokeStringArgumentsDescription), SystemRuntimeInteropServicesAnalyzersResources.ResourceManager, typeof(SystemRuntimeInteropServicesAnalyzersResources));
        internal static DiagnosticDescriptor RuleCA2101 = new DiagnosticDescriptor(CA2101,
                                                                         s_localizableMessageAndTitleCA2101,
                                                                         s_localizableMessageAndTitleCA2101,
                                                                         DiagnosticCategory.Globalization,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: s_localizableDescriptionCA2101,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182319.aspx",
                                                                         customTags: WellKnownDiagnosticTags.Telemetry);

        private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedDiagnostics = ImmutableArray.Create(RuleCA1401, RuleCA2101);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return s_supportedDiagnostics;
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(
                (context) =>
                {
                    var dllImportType = context.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.DllImportAttribute");
                    if (dllImportType == null)
                    {
                        return;
                    }

                    var marshalAsType = context.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.MarshalAsAttribute");
                    if (marshalAsType == null)
                    {
                        return;
                    }

                    var stringBuilderType = context.Compilation.GetTypeByMetadataName("System.Text.StringBuilder");
                    if (stringBuilderType == null)
                    {
                        return;
                    }

                    var unmanagedType = context.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedType");
                    if (unmanagedType == null)
                    {
                        return;
                    }

                    context.RegisterSymbolAction(new Analyzer(dllImportType, marshalAsType, stringBuilderType, unmanagedType).AnalyzeSymbol, SymbolKind.Method);
                });
        }

        private sealed class Analyzer
        {
            private INamedTypeSymbol _dllImportType;
            private INamedTypeSymbol _marshalAsType;
            private INamedTypeSymbol _stringBuilderType;
            private INamedTypeSymbol _unmanagedType;

            public Analyzer(
                INamedTypeSymbol dllImportType,
                INamedTypeSymbol marshalAsType,
                INamedTypeSymbol stringBuilderType,
                INamedTypeSymbol unmanagedType)
            {
                _dllImportType = dllImportType;
                _marshalAsType = marshalAsType;
                _stringBuilderType = stringBuilderType;
                _unmanagedType = unmanagedType;
            }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var methodSymbol = (IMethodSymbol)context.Symbol;
                if (methodSymbol == null)
                {
                    return;
                }

                var dllImportData = methodSymbol.GetDllImportData();
                if (dllImportData == null)
                {
                    return;
                }

                var dllAttribute = methodSymbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Equals(_dllImportType));
                var defaultLocation = dllAttribute == null ? methodSymbol.Locations.FirstOrDefault() : GetAttributeLocation(dllAttribute);

                // CA1401 - PInvoke methods should not be visible
                if (methodSymbol.DeclaredAccessibility == Accessibility.Public || methodSymbol.DeclaredAccessibility == Accessibility.Protected)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleCA1401, context.Symbol.Locations.First(l => l.IsInSource), methodSymbol.Name));
                }

                // CA2101 - Specify marshalling for PInvoke string arguments
                if (dllImportData.BestFitMapping != false)
                {
                    bool appliedCA2101ToMethod = false;
                    foreach (var parameter in methodSymbol.Parameters)
                    {
                        if (parameter.Type.SpecialType == SpecialType.System_String || parameter.Type.Equals(_stringBuilderType))
                        {
                            var marshalAsAttribute = parameter.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Equals(_marshalAsType));
                            var charSet = marshalAsAttribute == null
                                ? dllImportData.CharacterSet
                                : MarshalingToCharSet(GetParameterMarshaling(marshalAsAttribute));

                            // only unicode marshaling is considered safe
                            if (charSet != CharSet.Unicode)
                            {
                                if (marshalAsAttribute != null)
                                {
                                    // track the diagnostic on the [MarshalAs] attribute
                                    var marshalAsLocation = GetAttributeLocation(marshalAsAttribute);
                                    context.ReportDiagnostic(Diagnostic.Create(RuleCA2101, marshalAsLocation));
                                }
                                else if (!appliedCA2101ToMethod)
                                {
                                    // track the diagnostic on the [DllImport] attribute
                                    appliedCA2101ToMethod = true;
                                    context.ReportDiagnostic(Diagnostic.Create(RuleCA2101, defaultLocation));
                                }
                            }
                        }
                    }

                    // only unicode marshaling is considered safe, but only check this if we haven't already flagged the attribute
                    if (!appliedCA2101ToMethod && dllImportData.CharacterSet != CharSet.Unicode &&
                        (methodSymbol.ReturnType.SpecialType == SpecialType.System_String || methodSymbol.ReturnType.Equals(_stringBuilderType)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RuleCA2101, defaultLocation));
                    }
                }
            }

            private UnmanagedType? GetParameterMarshaling(AttributeData attributeData)
            {
                if (attributeData.ConstructorArguments.Length > 0)
                {
                    var argument = attributeData.ConstructorArguments.First();
                    if (argument.Type.Equals(_unmanagedType))
                    {
                        return (UnmanagedType)argument.Value;
                    }
                    else if (argument.Type.SpecialType == SpecialType.System_Int16)
                    {
                        return (UnmanagedType)((short)argument.Value);
                    }
                }

                return null;
            }

            private static CharSet? MarshalingToCharSet(UnmanagedType? type)
            {
                if (type == null)
                {
                    return null;
                }

                switch (type)
                {
                    case UnmanagedType.AnsiBStr:
                    case UnmanagedType.LPStr:
                    case UnmanagedType.VBByRefStr:
                        return CharSet.Ansi;
                    case UnmanagedType.BStr:
                    case UnmanagedType.LPWStr:
                        return CharSet.Unicode;
                    case UnmanagedType.ByValTStr:
                    case UnmanagedType.LPTStr:
                    case UnmanagedType.TBStr:
                    default:
                        // CharSet.Auto and CharSet.None are not available in the portable
                        // profiles. We are not interested in those values for our analysis and so simply
                        // return null
                        return null;
                }
            }

            private static Location GetAttributeLocation(AttributeData attributeData)
            {
                return attributeData.ApplicationSyntaxReference.SyntaxTree.GetLocation(attributeData.ApplicationSyntaxReference.Span);
            }
        }
    }
}
