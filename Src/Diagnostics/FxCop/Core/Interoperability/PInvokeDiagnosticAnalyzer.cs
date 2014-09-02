// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Interoperability
{
    [DiagnosticAnalyzer]
    public sealed class PInvokeDiagnosticAnalyzer : ICompilationNestedAnalyzerFactory
    {
        public const string CA1401 = "CA1401";
        public const string CA2101 = "CA2101";
        internal static DiagnosticDescriptor RuleCA1401 = new DiagnosticDescriptor(CA1401,
                                                                         FxCopRulesResources.PInvokesShouldNotBeVisible,
                                                                         FxCopRulesResources.PInvokeMethodShouldNotBeVisible,
                                                                         FxCopDiagnosticCategory.Interoperability,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182209.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        internal static DiagnosticDescriptor RuleCA2101 = new DiagnosticDescriptor(CA2101,
                                                                         FxCopRulesResources.SpecifyMarshalingForPInvokeStringArguments,
                                                                         FxCopRulesResources.SpecifyMarshalingForPInvokeStringArguments,
                                                                         FxCopDiagnosticCategory.Globalization,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: FxCopRulesResources.SpecifyMarshalingForPInvokeStringArgumentsDescription,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182319.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        private static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = ImmutableArray.Create(RuleCA1401, RuleCA2101);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return supportedDiagnostics;
            }
        }

        public IDiagnosticAnalyzer CreateAnalyzerWithinCompilation(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var dllImportType = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.DllImportAttribute");
            if (dllImportType == null)
            {
                return null;
            }

            var marshalAsType = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.MarshalAsAttribute");
            if (marshalAsType == null)
            {
                return null;
            }

            var stringBuilderType = compilation.GetTypeByMetadataName("System.Text.StringBuilder");
            if (stringBuilderType == null)
            {
                return null;
            }

            var unmanagedType = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedType");
            if (unmanagedType == null)
            {
                return null;
            }

            return new Analyzer(dllImportType, marshalAsType, stringBuilderType, unmanagedType);
        }

        private sealed class Analyzer : ISymbolAnalyzer
        {
            private INamedTypeSymbol dllImportType;
            private INamedTypeSymbol marshalAsType;
            private INamedTypeSymbol stringBuilderType;
            private INamedTypeSymbol unmanagedType;

            public Analyzer(
                INamedTypeSymbol dllImportType,
                INamedTypeSymbol marshalAsType,
                INamedTypeSymbol stringBuilderType,
                INamedTypeSymbol unmanagedType)
            {
                this.dllImportType = dllImportType;
                this.marshalAsType = marshalAsType;
                this.stringBuilderType = stringBuilderType;
                this.unmanagedType = unmanagedType;
            }

            public ImmutableArray<SymbolKind> SymbolKindsOfInterest
            {
                get
                {
                    return ImmutableArray.Create(SymbolKind.Method);
                }
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return supportedDiagnostics;
                }
            }

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                var methodSymbol = (IMethodSymbol)symbol;
                if (methodSymbol == null)
                {
                    return;
                }

                var dllImportData = methodSymbol.GetDllImportData();
                if (dllImportData == null)
                {
                    return;
                }

                var dllAttribute = methodSymbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Equals(this.dllImportType));
                var defaultLocation = dllAttribute == null ? methodSymbol.Locations.FirstOrDefault() : GetAttributeLocation(dllAttribute);

                // CA1401 - PInvoke methods should not be visible
                if (methodSymbol.DeclaredAccessibility == Accessibility.Public || methodSymbol.DeclaredAccessibility == Accessibility.Protected)
                {
                    addDiagnostic(symbol.CreateDiagnostic(RuleCA1401, methodSymbol.Name));
                }

                // CA2101 - Specify marshalling for PInvoke string arguments
                if (dllImportData.BestFitMapping != false)
                {
                    bool appliedCA2101ToMethod = false;
                    foreach (var parameter in methodSymbol.Parameters)
                    {
                        if (parameter.Type.SpecialType == SpecialType.System_String || parameter.Type.Equals(this.stringBuilderType))
                        {
                            var marshalAsAttribute = parameter.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Equals(this.marshalAsType));
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
                                    addDiagnostic(marshalAsLocation.CreateDiagnostic(RuleCA2101));
                                }
                                else if (!appliedCA2101ToMethod)
                                {
                                    // track the diagnostic on the [DllImport] attribute
                                    appliedCA2101ToMethod = true;
                                    addDiagnostic(defaultLocation.CreateDiagnostic(RuleCA2101));
                                }
                            }
                        }
                    }

                    // only unicode marshaling is considered safe, but only check this if we haven't already flagged the attribute
                    if (!appliedCA2101ToMethod && dllImportData.CharacterSet != CharSet.Unicode &&
                        (methodSymbol.ReturnType.SpecialType == SpecialType.System_String || methodSymbol.ReturnType.Equals(this.stringBuilderType)))
                    {
                        addDiagnostic(defaultLocation.CreateDiagnostic(RuleCA2101));
                    }
                }
            }

            private UnmanagedType? GetParameterMarshaling(AttributeData attributeData)
            {
                if (attributeData.ConstructorArguments.Length > 0)
                {
                    var argument = attributeData.ConstructorArguments.First();
                    if (argument.Type.Equals(this.unmanagedType))
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
                        return CharSet.Auto;
                    default:
                        return CharSet.None;
                }
            }

            private static Location GetAttributeLocation(AttributeData attributeData)
            {
                return attributeData.ApplicationSyntaxReference.SyntaxTree.GetLocation(attributeData.ApplicationSyntaxReference.Span);
            }
        }
    }
}
