// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    /// <summary>
    /// Contains all information related to Naming Style Preferences.
    /// 1. Symbol Specifications
    /// 2. Name Style
    /// 3. Naming Rule (points to Symbol Specification IDs)
    /// </summary>
    internal sealed class NamingStylePreferences : IEquatable<NamingStylePreferences>, IObjectWritable
    {
        static NamingStylePreferences()
        {
            ObjectBinder.RegisterTypeReader(typeof(NamingStylePreferences), ReadFrom);
        }

        private const int s_serializationVersion = 5;

        public readonly ImmutableArray<SymbolSpecification> SymbolSpecifications;
        public readonly ImmutableArray<NamingStyle> NamingStyles;
        public readonly ImmutableArray<SerializableNamingRule> NamingRules;

        private readonly Lazy<NamingStyleRules> _lazyRules;

        internal NamingStylePreferences(
            ImmutableArray<SymbolSpecification> symbolSpecifications,
            ImmutableArray<NamingStyle> namingStyles,
            ImmutableArray<SerializableNamingRule> namingRules)
        {
            SymbolSpecifications = symbolSpecifications;
            NamingStyles = namingStyles;
            NamingRules = namingRules;

            _lazyRules = new Lazy<NamingStyleRules>(CreateRules, isThreadSafe: true);
        }

        public static NamingStylePreferences Default => FromXElement(XElement.Parse(DefaultNamingPreferencesString));

        public static string DefaultNamingPreferencesString => s_defaultNamingPreferencesString.Value;

        internal NamingStyle GetNamingStyle(Guid namingStyleID)
            => NamingStyles.Single(s => s.ID == namingStyleID);

        internal SymbolSpecification GetSymbolSpecification(Guid symbolSpecificationID)
            => SymbolSpecifications.Single(s => s.ID == symbolSpecificationID);

        public NamingStyleRules Rules => _lazyRules.Value;

        public NamingStyleRules CreateRules()
            => new(NamingRules.Select(r => r.GetRule(this)).ToImmutableArray());

        internal XElement CreateXElement()
        {
            return new XElement("NamingPreferencesInfo",
                new XAttribute("SerializationVersion", s_serializationVersion),
                new XElement(nameof(SymbolSpecifications), SymbolSpecifications.Select(s => s.CreateXElement())),
                new XElement(nameof(NamingStyles), NamingStyles.Select(n => n.CreateXElement())),
                new XElement(nameof(NamingRules), NamingRules.Select(n => n.CreateXElement())));
        }

        internal static NamingStylePreferences FromXElement(XElement element)
        {
            element = GetUpgradedSerializationIfNecessary(element);

            return new NamingStylePreferences(
                element.Element(nameof(SymbolSpecifications)).Elements(nameof(SymbolSpecification))
                       .Select(SymbolSpecification.FromXElement).ToImmutableArray(),
                element.Element(nameof(NamingStyles)).Elements(nameof(NamingStyle))
                       .Select(NamingStyle.FromXElement).ToImmutableArray(),
                element.Element(nameof(NamingRules)).Elements(nameof(SerializableNamingRule))
                       .Select(SerializableNamingRule.FromXElement).ToImmutableArray());
        }

        public bool ShouldReuseInSerialization => false;

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteArray(SymbolSpecifications, (w, v) => v.WriteTo(w));
            writer.WriteArray(NamingStyles, (w, v) => v.WriteTo(w));
            writer.WriteArray(NamingRules, (w, v) => v.WriteTo(w));
        }

        public static NamingStylePreferences ReadFrom(ObjectReader reader)
        {
            return new NamingStylePreferences(
                reader.ReadArray(r => SymbolSpecification.ReadFrom(r)),
                reader.ReadArray(r => NamingStyle.ReadFrom(r)),
                reader.ReadArray(r => SerializableNamingRule.ReadFrom(r)));
        }

        public override bool Equals(object obj)
            => Equals(obj as NamingStylePreferences);

        public bool Equals(NamingStylePreferences other)
        {
            if (other is null)
                return false;

            return SymbolSpecifications.SequenceEqual(other.SymbolSpecifications)
                && NamingStyles.SequenceEqual(other.NamingStyles)
                && NamingRules.SequenceEqual(other.NamingRules);
        }

        public static bool operator ==(NamingStylePreferences left, NamingStylePreferences right)
        {
            if (left is null && right is null)
            {
                return true;
            }
            else if (left is null || right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(NamingStylePreferences left, NamingStylePreferences right)
            => !(left == right);

        public override int GetHashCode()
        {
            return Hash.Combine(Hash.CombineValues(SymbolSpecifications),
                Hash.Combine(Hash.CombineValues(NamingStyles),
                    Hash.CombineValues(NamingRules)));
        }

        private static readonly Lazy<string> s_defaultNamingPreferencesString = new(() => ConstructDefaultNamingPreferencesString());

        private static string ConstructDefaultNamingPreferencesString()
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);
            sb.Append(@$"<NamingPreferencesInfo SerializationVersion=""{s_serializationVersion}"">");
            sb.Append("  <SymbolSpecifications>");
            sb.Append(@$"    <SymbolSpecification ID=""23d856b4-5089-4405-83ce-749aada99153"" Name=""{CompilerExtensionsResources.Interfaces}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <TypeKind>Interface</TypeKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""830657f6-e7e5-4830-b328-f109d3b6c165"" Name=""{CompilerExtensionsResources.Events}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Event</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" Name=""{CompilerExtensionsResources.Methods}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <MethodKind>Ordinary</MethodKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" Name=""{CompilerExtensionsResources.Properties}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Property</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""2c07f5bf-bc81-4c2b-82b4-ae9b3ffd0ba4"" Name=""{CompilerExtensionsResources.Types}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Namespace</SymbolKind>");
            sb.Append("        <TypeKind>Class</TypeKind>");
            sb.Append("        <TypeKind>Struct</TypeKind>");
            sb.Append("        <TypeKind>Interface</TypeKind>");
            sb.Append("        <TypeKind>Enum</TypeKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""998a19f2-94d6-47c0-a54e-af01d881849a"" Name=""{CompilerExtensionsResources.Type_Parameters}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>TypeParameter</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>NotApplicable</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""0c69f9a2-668a-4041-bb76-aee7befd4d81"" Name=""{CompilerExtensionsResources.Public_Fields}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Field</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""69da61da-4234-4526-9dad-a472cd04c352"" Name=""{CompilerExtensionsResources.Private_Fields}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Field</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""ac64fd47-0c2e-46be-909d-5f985cc31857"" Name=""{CompilerExtensionsResources.Private_Static_Fields}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Field</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList>");
            sb.Append("        <ModifierKind>IsStatic</ModifierKind>");
            sb.Append("      </RequiredModifierList>");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""1f26297d-ef0a-4d51-9d46-78f3b3c89f44"" Name=""{CompilerExtensionsResources.Private_Constant_Fields}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Field</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList>");
            sb.Append("        <ModifierKind>IsConst</ModifierKind>");
            sb.Append("      </RequiredModifierList>");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""b40d58d0-902a-4097-b6c7-a0150a3a2415"" Name=""{CompilerExtensionsResources.Local_Variables}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Local</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>NotApplicable</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""07e97ff0-6de9-42e9-9095-1f7fb1e6f16a"" Name=""{CompilerExtensionsResources.Parameters}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Parameter</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>NotApplicable</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""24267880-58f7-4b34-8bc3-2bf801ef6207"" Name=""{CompilerExtensionsResources.Public_Static_Readonly_Fields}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Field</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList>");
            sb.Append("        <ModifierKind>IsReadOnly</ModifierKind>");
            sb.Append("        <ModifierKind>IsStatic</ModifierKind>");
            sb.Append("      </RequiredModifierList>");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""872456da-6683-4b25-b460-2216ebc3b793"" Name=""{CompilerExtensionsResources.Private_Static_Readonly_Fields}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Field</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList>");
            sb.Append("        <ModifierKind>IsReadOnly</ModifierKind>");
            sb.Append("        <ModifierKind>IsStatic</ModifierKind>");
            sb.Append("      </RequiredModifierList>");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""a1ba7f1a-32ec-44c3-83c6-b0719cdec9e9"" Name=""{CompilerExtensionsResources.Local_Functions}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <MethodKind>LocalFunction</MethodKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>Public</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Internal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Private</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>Protected</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>");
            sb.Append("        <AccessibilityKind>NotApplicable</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList />");
            sb.Append("    </SymbolSpecification>");
            sb.Append(@$"    <SymbolSpecification ID=""c7cb6f1a-bd31-49cc-80ca-d66053ef0535"" Name=""{CompilerExtensionsResources.Local_Constants}"">");
            sb.Append("      <ApplicableSymbolKindList>");
            sb.Append("        <SymbolKind>Local</SymbolKind>");
            sb.Append("      </ApplicableSymbolKindList>");
            sb.Append("      <ApplicableAccessibilityList>");
            sb.Append("        <AccessibilityKind>NotApplicable</AccessibilityKind>");
            sb.Append("      </ApplicableAccessibilityList>");
            sb.Append("      <RequiredModifierList>");
            sb.Append("        <ModifierKind>IsConst</ModifierKind>");
            sb.Append("      </RequiredModifierList>");
            sb.Append("    </SymbolSpecification>");
            sb.Append("  </SymbolSpecifications>");
            sb.Append("  <NamingStyles>");
            sb.Append(@$"    <NamingStyle ID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" Name=""{CompilerExtensionsResources.PascalCase}"" Prefix="""" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />");
            sb.Append(@$"    <NamingStyle ID=""1ecc5eb6-b5fc-49a5-a9f1-a980f3e48c92"" Name=""{CompilerExtensionsResources.IPascalCase}"" Prefix=""I"" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />");
            sb.Append(@$"    <NamingStyle ID=""86ca3195-21dd-45cd-a1ce-c514e001b150"" Name=""{CompilerExtensionsResources.TPascalCase}"" Prefix=""T"" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />");
            sb.Append(@$"    <NamingStyle ID=""f39e1169-ce89-492c-9859-d96c3dbf0330"" Name=""{CompilerExtensionsResources._camelCase}"" Prefix=""_"" Suffix="""" WordSeparator="""" CapitalizationScheme=""CamelCase"" />");
            sb.Append(@$"    <NamingStyle ID=""5fc83531-01c4-435b-b215-a8df6f0f0bcc"" Name=""{CompilerExtensionsResources.camelCase}"" Prefix="""" Suffix="""" WordSeparator="""" CapitalizationScheme=""CamelCase"" />");
            sb.Append(@$"    <NamingStyle ID=""ca1f2e07-8f9f-4eb1-8b2c-757b16c9a34c"" Name=""{CompilerExtensionsResources.s_camelCase}"" Prefix=""s_"" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />");
            sb.Append("  </NamingStyles>");
            sb.Append("  <NamingRules>");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""2c07f5bf-bc81-4c2b-82b4-ae9b3ffd0ba4"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""23d856b4-5089-4405-83ce-749aada99153"" NamingStyleID=""1ecc5eb6-b5fc-49a5-a9f1-a980f3e48c92"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""998a19f2-94d6-47c0-a54e-af01d881849a"" NamingStyleID=""86ca3195-21dd-45cd-a1ce-c514e001b150"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""830657f6-e7e5-4830-b328-f109d3b6c165"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""b40d58d0-902a-4097-b6c7-a0150a3a2415"" NamingStyleID=""5fc83531-01c4-435b-b215-a8df6f0f0bcc"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""c7cb6f1a-bd31-49cc-80ca-d66053ef0535"" NamingStyleID=""5fc83531-01c4-435b-b215-a8df6f0f0bcc"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""07e97ff0-6de9-42e9-9095-1f7fb1e6f16a"" NamingStyleID=""5fc83531-01c4-435b-b215-a8df6f0f0bcc"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""0c69f9a2-668a-4041-bb76-aee7befd4d81"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""69da61da-4234-4526-9dad-a472cd04c352"" NamingStyleID=""f39e1169-ce89-492c-9859-d96c3dbf0330"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""ac64fd47-0c2e-46be-909d-5f985cc31857"" NamingStyleID=""ca1f2e07-8f9f-4eb1-8b2c-757b16c9a34c"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""1f26297d-ef0a-4d51-9d46-78f3b3c89f44"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""24267880-58f7-4b34-8bc3-2bf801ef6207"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""872456da-6683-4b25-b460-2216ebc3b793"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append(@"    <SerializableNamingRule SymbolSpecificationID=""a1ba7f1a-32ec-44c3-83c6-b0719cdec9e9"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />");
            sb.Append("  </NamingRules>");
            sb.Append(" </NamingPreferencesInfo>");
            return sb.ToString();
        }

        private static XElement GetUpgradedSerializationIfNecessary(XElement rootElement)
        {
            var serializationVersion = int.Parse(rootElement.Attribute("SerializationVersion").Value);

            if (serializationVersion == 4)
            {
                UpgradeSerialization_4To5(rootElement = new XElement(rootElement));
                serializationVersion = 5;
            }

            // Add future version checks here. If the version is off by more than 1, these upgrades will run in sequence.
            // The next one should check serializationVersion == 5 and update it to 6.
            // It is also important to create a new roaming location in NamingStyleOptions.NamingPreferences
            // so that we never store the new format in an older version.
            Debug.Assert(s_serializationVersion == 5, "After increasing the serialization version, add an upgrade path here.");

            return serializationVersion == s_serializationVersion
                ? rootElement
                : XElement.Parse(DefaultNamingPreferencesString);
        }

        private static void UpgradeSerialization_4To5(XElement rootElement)
        {
            var methodElements = rootElement
                .Descendants()
                .Where(e => e.Name.LocalName == "SymbolKind" && e.Value == "Method").ToList();

            foreach (var element in methodElements)
            {
                element.ReplaceWith(XElement.Parse("<MethodKind>Ordinary</MethodKind>"));
            }
        }
    }
}
