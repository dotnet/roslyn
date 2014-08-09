// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class SerializationRulesDiagnosticAnalyzer : ICompilationNestedAnalyzerFactory
    {
        // Implement serialization constructors
        internal const string RuleCA2229Id = "CA2229";
        internal static DiagnosticDescriptor RuleCA2229 = new DiagnosticDescriptor(RuleCA2229Id,
                                                                         FxCopRulesResources.ImplementSerializationConstructor,
                                                                         "{0}",
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        // Mark ISerializable types with SerializableAttribute
        internal const string RuleCA2237Id = "CA2237";
        internal static DiagnosticDescriptor RuleCA2237 = new DiagnosticDescriptor(RuleCA2237Id,
                                                                         FxCopRulesResources.MarkISerializableTypesWithAttribute,
                                                                         FxCopRulesResources.AddSerializableAttributeToType,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        // Mark all non-serializable fields
        internal const string RuleCA2235Id = "CA2235";
        internal static DiagnosticDescriptor RuleCA2235 = new DiagnosticDescriptor(RuleCA2235Id,
                                                                         FxCopRulesResources.MarkAllNonSerializableFields,
                                                                         FxCopRulesResources.FieldIsOfNonSerializableType,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        private static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = ImmutableArray.Create(RuleCA2229, RuleCA2235, RuleCA2237);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return supportedDiagnostics;
            }
        }

        public IDiagnosticAnalyzer CreateAnalyzerWithinCompilation(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var iserializableTypeSymbol = compilation.GetTypeByMetadataName("System.Runtime.Serialization.ISerializable");
            if (iserializableTypeSymbol == null)
            {
                return null;
            }

            var serializationInfoTypeSymbol = compilation.GetTypeByMetadataName("System.Runtime.Serialization.SerializationInfo");
            if (serializationInfoTypeSymbol == null)
            {
                return null;
            }

            var streamingContextTypeSymbol = compilation.GetTypeByMetadataName("System.Runtime.Serialization.StreamingContext");
            if (streamingContextTypeSymbol == null)
            {
                return null;
            }

            var serializableAttributeTypeSymbol = compilation.GetTypeByMetadataName("System.SerializableAttribute");
            if (serializableAttributeTypeSymbol == null)
            {
                return null;
            }

            return new Analyzer(iserializableTypeSymbol, serializationInfoTypeSymbol, streamingContextTypeSymbol, serializableAttributeTypeSymbol);
        }

        private sealed class Analyzer : AbstractNamedTypeAnalyzer
        {
            private INamedTypeSymbol iserializableTypeSymbol;
            private INamedTypeSymbol serializationInfoTypeSymbol;
            private INamedTypeSymbol streamingContextTypeSymbol;
            private INamedTypeSymbol serializableAttributeTypeSymbol;

            public Analyzer(
                INamedTypeSymbol iserializableTypeSymbol,
                INamedTypeSymbol serializationInfoTypeSymbol,
                INamedTypeSymbol streamingContextTypeSymbol,
                INamedTypeSymbol serializableAttributeTypeSymbol)
            {
                this.iserializableTypeSymbol = iserializableTypeSymbol;
                this.serializationInfoTypeSymbol = serializationInfoTypeSymbol;
                this.streamingContextTypeSymbol = streamingContextTypeSymbol;
                this.serializableAttributeTypeSymbol = serializableAttributeTypeSymbol;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return supportedDiagnostics;
                }
            }

            public override void AnalyzeSymbol(INamedTypeSymbol namedTypeSymbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                // If the type is public and implements ISerializable
                if (namedTypeSymbol.DeclaredAccessibility == Accessibility.Public && namedTypeSymbol.AllInterfaces.Contains(this.iserializableTypeSymbol))
                {
                    if (!IsSerializable(namedTypeSymbol))
                    {
                        // CA2237 : Mark serializable types with the SerializableAttribute
                        if (namedTypeSymbol.BaseType.SpecialType == SpecialType.System_Object ||
                            IsSerializable(namedTypeSymbol.BaseType))
                        {
                            addDiagnostic(namedTypeSymbol.CreateDiagnostic(RuleCA2237, namedTypeSymbol.Name));
                        }
                    }
                    else
                    {
                        // Look for a serialization constructor.
                        // A serialization constructor takes two params of type SerializationInfo and StreamingContext.
                        var serializationCtor = namedTypeSymbol.Constructors.Where(c => c.Parameters.Count() == 2 &&
                                                                                  c.Parameters[0].Type == this.serializationInfoTypeSymbol &&
                                                                                  c.Parameters[1].Type == this.streamingContextTypeSymbol).SingleOrDefault();

                        // There is no serialization ctor - issue a diagnostic.
                        if (serializationCtor == null)
                        {
                            addDiagnostic(namedTypeSymbol.CreateDiagnostic(RuleCA2229, string.Format(FxCopRulesResources.SerializableTypeDoesntHaveCtor, namedTypeSymbol.Name)));
                        }
                        else
                        {
                            // Check the accessibility
                            // The serializationctor should be protected if the class is unsealed and private if the class is sealed.
                            if (namedTypeSymbol.IsSealed && serializationCtor.DeclaredAccessibility != Accessibility.Private)
                            {
                                addDiagnostic(serializationCtor.CreateDiagnostic(RuleCA2229, string.Format(FxCopRulesResources.SerializationCtorAccessibilityForSealedType, namedTypeSymbol.Name)));
                            }

                            if (!namedTypeSymbol.IsSealed && serializationCtor.DeclaredAccessibility != Accessibility.Protected)
                            {
                                addDiagnostic(serializationCtor.CreateDiagnostic(RuleCA2229, string.Format(FxCopRulesResources.SerializationCtorAccessibilityForUnSealedType, namedTypeSymbol.Name)));
                            }
                        }
                    }
                }

                // If this is type is marked Serializable check it's fields types' as well
                if (IsSerializable(namedTypeSymbol))
                {
                    var nonSerialableFields = namedTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => !IsSerializable(m.Type));
                    foreach (var field in nonSerialableFields)
                    {
                        addDiagnostic(field.CreateDiagnostic(RuleCA2235, field.Name, namedTypeSymbol.Name, field.Type));
                    }
                }
            }

            private bool IsSerializable(ITypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol.GetAttributes().Any(a => a.AttributeClass == this.serializableAttributeTypeSymbol);
            }
        }
    }
}
