// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class SerializationRulesDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        // Implement serialization constructors
        internal const string RuleCA2229Id = "CA2229";
        private static LocalizableString s_localizableTitleCA2229 = new LocalizableResourceString(nameof(FxCopRulesResources.ImplementSerializationConstructor), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString s_localizableDescriptionCA2229 = new LocalizableResourceString(nameof(FxCopRulesResources.ImplementSerializationConstructorDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor RuleCA2229 = new DiagnosticDescriptor(RuleCA2229Id,
                                                                         s_localizableTitleCA2229,
                                                                         "{0}",
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: s_localizableDescriptionCA2229,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182343.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        // Mark ISerializable types with SerializableAttribute
        internal const string RuleCA2237Id = "CA2237";
        private static LocalizableString s_localizableTitleCA2237 = new LocalizableResourceString(nameof(FxCopRulesResources.MarkISerializableTypesWithAttribute), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString s_localizableMessageCA2237 = new LocalizableResourceString(nameof(FxCopRulesResources.AddSerializableAttributeToType), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString s_localizableDescriptionCA2237 = new LocalizableResourceString(nameof(FxCopRulesResources.MarkISerializableTypesWithAttributeDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor RuleCA2237 = new DiagnosticDescriptor(RuleCA2237Id,
                                                                         s_localizableTitleCA2237,
                                                                         s_localizableMessageCA2237,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: s_localizableDescriptionCA2237,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182350.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        // Mark all non-serializable fields
        internal const string RuleCA2235Id = "CA2235";
        private static LocalizableString s_localizableTitleCA2235 = new LocalizableResourceString(nameof(FxCopRulesResources.MarkAllNonSerializableFields), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString s_localizableMessageCA2235 = new LocalizableResourceString(nameof(FxCopRulesResources.FieldIsOfNonSerializableType), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString s_localizableDescriptionCA2235 = new LocalizableResourceString(nameof(FxCopRulesResources.MarkAllNonSerializableFieldsDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor RuleCA2235 = new DiagnosticDescriptor(RuleCA2235Id,
                                                                         s_localizableTitleCA2235,
                                                                         s_localizableMessageCA2235,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: s_localizableDescriptionCA2235,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182349.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedDiagnostics = ImmutableArray.Create(RuleCA2229, RuleCA2235, RuleCA2237);

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
                    var iserializableTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.ISerializable");
                    if (iserializableTypeSymbol == null)
                    {
                        return;
                    }

                    var serializationInfoTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.SerializationInfo");
                    if (serializationInfoTypeSymbol == null)
                    {
                        return;
                    }

                    var streamingContextTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.StreamingContext");
                    if (streamingContextTypeSymbol == null)
                    {
                        return;
                    }

                    var serializableAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("System.SerializableAttribute");
                    if (serializableAttributeTypeSymbol == null)
                    {
                        return;
                    }

                    context.RegisterSymbolAction(new Analyzer(iserializableTypeSymbol, serializationInfoTypeSymbol, streamingContextTypeSymbol, serializableAttributeTypeSymbol).AnalyzeSymbol, SymbolKind.NamedType);
                });
        }

        private sealed class Analyzer
        {
            private INamedTypeSymbol _iserializableTypeSymbol;
            private INamedTypeSymbol _serializationInfoTypeSymbol;
            private INamedTypeSymbol _streamingContextTypeSymbol;
            private INamedTypeSymbol _serializableAttributeTypeSymbol;

            public Analyzer(
                INamedTypeSymbol iserializableTypeSymbol,
                INamedTypeSymbol serializationInfoTypeSymbol,
                INamedTypeSymbol streamingContextTypeSymbol,
                INamedTypeSymbol serializableAttributeTypeSymbol)
            {
                _iserializableTypeSymbol = iserializableTypeSymbol;
                _serializationInfoTypeSymbol = serializationInfoTypeSymbol;
                _streamingContextTypeSymbol = streamingContextTypeSymbol;
                _serializableAttributeTypeSymbol = serializableAttributeTypeSymbol;
            }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

                // If the type is public and implements ISerializable
                if (namedTypeSymbol.DeclaredAccessibility == Accessibility.Public && namedTypeSymbol.AllInterfaces.Contains(_iserializableTypeSymbol))
                {
                    if (!IsSerializable(namedTypeSymbol))
                    {
                        // CA2237 : Mark serializable types with the SerializableAttribute
                        if (namedTypeSymbol.BaseType.SpecialType == SpecialType.System_Object ||
                            IsSerializable(namedTypeSymbol.BaseType))
                        {
                            context.ReportDiagnostic(namedTypeSymbol.CreateDiagnostic(RuleCA2237, namedTypeSymbol.Name));
                        }
                    }
                    else
                    {
                        // Look for a serialization constructor.
                        // A serialization constructor takes two params of type SerializationInfo and StreamingContext.
                        var serializationCtor = namedTypeSymbol.Constructors.Where(c => c.Parameters.Count() == 2 &&
                                                                                  c.Parameters[0].Type == _serializationInfoTypeSymbol &&
                                                                                  c.Parameters[1].Type == _streamingContextTypeSymbol).SingleOrDefault();

                        // There is no serialization ctor - issue a diagnostic.
                        if (serializationCtor == null)
                        {
                            context.ReportDiagnostic(namedTypeSymbol.CreateDiagnostic(RuleCA2229, string.Format(FxCopRulesResources.SerializableTypeDoesntHaveCtor, namedTypeSymbol.Name)));
                        }
                        else
                        {
                            // Check the accessibility
                            // The serializationctor should be protected if the class is unsealed and private if the class is sealed.
                            if (namedTypeSymbol.IsSealed && serializationCtor.DeclaredAccessibility != Accessibility.Private)
                            {
                                context.ReportDiagnostic(serializationCtor.CreateDiagnostic(RuleCA2229, string.Format(FxCopRulesResources.SerializationCtorAccessibilityForSealedType, namedTypeSymbol.Name)));
                            }

                            if (!namedTypeSymbol.IsSealed && serializationCtor.DeclaredAccessibility != Accessibility.Protected)
                            {
                                context.ReportDiagnostic(serializationCtor.CreateDiagnostic(RuleCA2229, string.Format(FxCopRulesResources.SerializationCtorAccessibilityForUnSealedType, namedTypeSymbol.Name)));
                            }
                        }
                    }
                }

                // If this is type is marked Serializable check it's fields types' as well
                if (IsSerializable(namedTypeSymbol))
                {
                    var nonSerializableFields = namedTypeSymbol.GetMembers().OfType<IFieldSymbol>().Where(m => !IsSerializable(m.Type));
                    foreach (var field in nonSerializableFields)
                    {
                        if (field.IsImplicitlyDeclared && field.AssociatedSymbol != null)
                        {
                            context.ReportDiagnostic(field.AssociatedSymbol.CreateDiagnostic(RuleCA2235, field.AssociatedSymbol.Name, namedTypeSymbol.Name, field.Type));
                        }
                        else
                        {
                            context.ReportDiagnostic(field.CreateDiagnostic(RuleCA2235, field.Name, namedTypeSymbol.Name, field.Type));
                        }
                    }
                }
            }

            private bool IsSerializable(ITypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol.GetAttributes().Any(a => a.AttributeClass == _serializableAttributeTypeSymbol);
            }
        }
    }
}
