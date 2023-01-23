// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.NamingStyles;
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
    [DataContract]
    internal sealed class NamingStylePreferences : IEquatable<NamingStylePreferences>
    {
        private const int s_serializationVersion = 5;

        private static readonly string _defaultNamingPreferencesString = $@"
<NamingPreferencesInfo SerializationVersion=""{s_serializationVersion}"">
  <SymbolSpecifications>
    <SymbolSpecification ID=""5c545a62-b14d-460a-88d8-e936c0a39316"" Name=""{CompilerExtensionsResources.Class}"">
      <ApplicableSymbolKindList>
        <TypeKind>Class</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""23d856b4-5089-4405-83ce-749aada99153"" Name=""{CompilerExtensionsResources.Interface}"">
      <ApplicableSymbolKindList>
        <TypeKind>Interface</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""d1796e78-ff66-463f-8576-eb46416060c0"" Name=""{CompilerExtensionsResources.Struct}"">
      <ApplicableSymbolKindList>
        <TypeKind>Struct</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""d8af8dc6-1ade-441d-9947-8946922e198a"" Name=""{CompilerExtensionsResources.Enum}"">
      <ApplicableSymbolKindList>
        <TypeKind>Enum</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""408a3347-b908-4b54-a954-1355e64c1de3"" Name=""{CompilerExtensionsResources.Delegate}"">
      <ApplicableSymbolKindList>
        <TypeKind>Delegate</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""830657f6-e7e5-4830-b328-f109d3b6c165"" Name=""{CompilerExtensionsResources.Event}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Event</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" Name=""{CompilerExtensionsResources.Method}"">
      <ApplicableSymbolKindList>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""af410767-f189-47c6-b140-aeccf1ff242e"" Name=""{CompilerExtensionsResources.Private_Method}"">
      <ApplicableSymbolKindList>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Private</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""8076757e-6a4a-47f1-9b4b-ae8a3284e987"" Name=""{CompilerExtensionsResources.Abstract_Method}"">
      <ApplicableSymbolKindList>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsAbstract</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""16133061-a8e7-4392-92c3-1d93cd54c218"" Name=""{CompilerExtensionsResources.Static_Method}"">
      <ApplicableSymbolKindList>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsStatic</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" Name=""{CompilerExtensionsResources.Property}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Property</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""b24a91ce-3501-4799-b6df-baf044156c83"" Name=""{CompilerExtensionsResources.Public_or_Protected_Field}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Field</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""70af42cb-1741-4027-969c-9edc4877d965"" Name=""{CompilerExtensionsResources.Static_Field}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Field</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsStatic</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""10790aa6-0a0b-432d-a52d-d252ca92302b"" Name=""{CompilerExtensionsResources.Private_or_Internal_Field}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Field</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""ac995be4-88de-4771-9dcc-a456a7c02d89"" Name=""{CompilerExtensionsResources.Private_or_Internal_Static_Field}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Field</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsStatic</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""2c07f5bf-bc81-4c2b-82b4-ae9b3ffd0ba4"" Name=""{CompilerExtensionsResources.Types}"">
      <ApplicableSymbolKindList>
        <TypeKind>Class</TypeKind>
        <TypeKind>Struct</TypeKind>
        <TypeKind>Interface</TypeKind>
        <TypeKind>Enum</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""5f3ddba1-279f-486c-801e-5c097c36dd85"" Name=""{CompilerExtensionsResources.Non_Field_Members}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Property</SymbolKind>
        <SymbolKind>Event</SymbolKind>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
        <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
  </SymbolSpecifications>
  <NamingStyles>
    <NamingStyle ID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" Name=""{CompilerExtensionsResources.Pascal_Case}"" Prefix="""" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
    <NamingStyle ID=""1ecc5eb6-b5fc-49a5-a9f1-a980f3e48c92"" Name=""{CompilerExtensionsResources.Begins_with_I}"" Prefix=""I"" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
  </NamingStyles>
  <NamingRules>
    <SerializableNamingRule SymbolSpecificationID=""23d856b4-5089-4405-83ce-749aada99153"" NamingStyleID=""1ecc5eb6-b5fc-49a5-a9f1-a980f3e48c92"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""2c07f5bf-bc81-4c2b-82b4-ae9b3ffd0ba4"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""5f3ddba1-279f-486c-801e-5c097c36dd85"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />
  </NamingRules>
</NamingPreferencesInfo>
";

        [DataMember(Order = 0)]
        public readonly ImmutableArray<SymbolSpecification> SymbolSpecifications;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<NamingStyle> NamingStyles;

        [DataMember(Order = 2)]
        public readonly ImmutableArray<SerializableNamingRule> NamingRules;

        private readonly Lazy<NamingStyleRules> _lazyRules;

        public NamingStylePreferences(
            ImmutableArray<SymbolSpecification> symbolSpecifications,
            ImmutableArray<NamingStyle> namingStyles,
            ImmutableArray<SerializableNamingRule> namingRules)
        {
            SymbolSpecifications = symbolSpecifications;
            NamingStyles = namingStyles;
            NamingRules = namingRules;

            _lazyRules = new Lazy<NamingStyleRules>(CreateRules, isThreadSafe: true);
        }

        public static NamingStylePreferences Default { get; } = FromXElement(XElement.Parse(DefaultNamingPreferencesString));
        public static NamingStylePreferences Empty { get; } = new(ImmutableArray<SymbolSpecification>.Empty, ImmutableArray<NamingStyle>.Empty, ImmutableArray<SerializableNamingRule>.Empty);

        public static string DefaultNamingPreferencesString => _defaultNamingPreferencesString;

        public bool IsEmpty
            => SymbolSpecifications.IsEmpty && NamingStyles.IsEmpty && NamingRules.IsEmpty;

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
                new XElement("SymbolSpecifications", SymbolSpecifications.Select(s => s.CreateXElement())),
                new XElement("NamingStyles", NamingStyles.Select(n => n.CreateXElement())),
                new XElement("NamingRules", NamingRules.Select(n => n.CreateXElement())));
        }

        internal static NamingStylePreferences FromXElement(XElement element)
        {
            element = GetUpgradedSerializationIfNecessary(element);

            return new NamingStylePreferences(
                element.Element("SymbolSpecifications").Elements(nameof(SymbolSpecification))
                       .Select(SymbolSpecification.FromXElement).ToImmutableArray(),
                element.Element("NamingStyles").Elements(nameof(NamingStyle))
                       .Select(NamingStyle.FromXElement).ToImmutableArray(),
                element.Element("NamingRules").Elements(nameof(SerializableNamingRule))
                       .Select(SerializableNamingRule.FromXElement).ToImmutableArray());
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
