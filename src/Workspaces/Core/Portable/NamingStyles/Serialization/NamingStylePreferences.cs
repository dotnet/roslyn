// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    /// <summary>
    /// Contains all information related to Naming Style Preferences.
    /// 1. Symbol Specifications
    /// 2. Name Style
    /// 3. Naming Rule (points to Symbol Specification IDs)
    /// </summary>
    internal class NamingStylePreferences : IEquatable<NamingStylePreferences>
    {
        public List<SymbolSpecification> SymbolSpecifications;
        public List<MutableNamingStyle> NamingStyles;
        public List<SerializableNamingRule> NamingRules;
        private readonly static int s_serializationVersion = 3;

        internal NamingStylePreferences(List<SymbolSpecification> symbolSpecifications, List<MutableNamingStyle> namingStyles, List<SerializableNamingRule> namingRules)
        {
            SymbolSpecifications = symbolSpecifications;
            NamingStyles = namingStyles;
            NamingRules = namingRules;
        }

        internal NamingStylePreferences()
        {
            SymbolSpecifications = new List<SymbolSpecification>();
            NamingStyles = new List<MutableNamingStyle>();
            NamingRules = new List<SerializableNamingRule>();
        }

        public static NamingStylePreferences Default => FromXElement(XElement.Parse(DefaultNamingPreferencesString));

        public static string DefaultNamingPreferencesString => _defaultNamingPreferencesString;

        internal MutableNamingStyle GetNamingStyle(Guid namingStyleID)
        {
            return NamingStyles.Single(s => s.ID == namingStyleID);
        }

        internal SymbolSpecification GetSymbolSpecification(Guid symbolSpecificationID)
        {
            return SymbolSpecifications.Single(s => s.ID == symbolSpecificationID);
        }

        public NamingStyleRules GetNamingStyleRules()
        {
            return new NamingStyleRules(NamingRules.Select(r => r.GetRule(this)).ToImmutableArray());
        }

        internal XElement CreateXElement()
        {
            return new XElement("NamingPreferencesInfo", 
                new XAttribute("SerializationVersion", s_serializationVersion),
                CreateSymbolSpecificationListXElement(),
                CreateNamingStyleListXElement(),
                CreateNamingRuleTreeXElement());
        }

        private XElement CreateNamingRuleTreeXElement()
        {
            var namingRulesElement = new XElement(nameof(NamingRules));

            foreach (var namingRule in NamingRules)
            {
                namingRulesElement.Add(namingRule.CreateXElement());
            }

            return namingRulesElement;
        }

        private XElement CreateNamingStyleListXElement()
        {
            var namingStylesElement = new XElement(nameof(NamingStyles));

            foreach (var namingStyle in NamingStyles)
            {
                namingStylesElement.Add(namingStyle.CreateXElement());
            }

            return namingStylesElement;
        }

        private XElement CreateSymbolSpecificationListXElement()
        {
            var symbolSpecificationsElement = new XElement(nameof(SymbolSpecifications));

            foreach (var symbolSpecification in SymbolSpecifications)
            {
                symbolSpecificationsElement.Add(symbolSpecification.CreateXElement());
            }

            return symbolSpecificationsElement;
        }

        internal static NamingStylePreferences FromXElement(XElement namingPreferencesInfoElement)
        {
            var namingPreferencesInfo = new NamingStylePreferences();

            var serializationVersion = int.Parse(namingPreferencesInfoElement.Attribute("SerializationVersion").Value);
            if (serializationVersion != s_serializationVersion)
            {
                namingPreferencesInfoElement = XElement.Parse(DefaultNamingPreferencesString);
            }

            namingPreferencesInfo.SetSymbolSpecificationListFromXElement(namingPreferencesInfoElement.Element(nameof(SymbolSpecifications)));
            namingPreferencesInfo.SetNamingStyleListFromXElement(namingPreferencesInfoElement.Element(nameof(NamingStyles)));
            namingPreferencesInfo.SetNamingRuleTreeFromXElement(namingPreferencesInfoElement.Element(nameof(NamingRules)));

            return namingPreferencesInfo;
        }

        private void SetSymbolSpecificationListFromXElement(XElement symbolSpecificationsElement)
        {
            foreach (var symbolSpecificationElement in symbolSpecificationsElement.Elements(nameof(SymbolSpecification)))
            {
                SymbolSpecifications.Add(SymbolSpecification.FromXElement(symbolSpecificationElement));
            }
        }

        private void SetNamingStyleListFromXElement(XElement namingStylesElement)
        {
            foreach (var namingStyleElement in namingStylesElement.Elements(nameof(NamingStyle)))
            {
                NamingStyles.Add(new MutableNamingStyle(NamingStyle.FromXElement(namingStyleElement)));
            }
        }

        private void SetNamingRuleTreeFromXElement(XElement namingRulesElement)
        {
            foreach (var namingRuleElement in namingRulesElement.Elements(nameof(SerializableNamingRule)))
            {
                NamingRules.Add(SerializableNamingRule.FromXElement(namingRuleElement));
            }
        }

        public override bool Equals(object obj)
            => Equals(obj as NamingStylePreferences);

        public bool Equals(NamingStylePreferences other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            return CreateXElement().ToString() == other.CreateXElement().ToString();
        }

        public static bool operator ==(NamingStylePreferences left, NamingStylePreferences right)
        {
            var leftIsNull = object.ReferenceEquals(left, null);
            var rightIsNull = object.ReferenceEquals(right, null);
            if (leftIsNull && rightIsNull)
            {
                return true;
            }
            else if(leftIsNull)
            {
                return false;
            }
            else if(rightIsNull)
            {
                return false;
            }
            
            return left.Equals(right);
        }

        public static bool operator !=(NamingStylePreferences left, NamingStylePreferences right)
            => !(left == right);

        public override int GetHashCode()
            => CreateXElement().ToString().GetHashCode();

        private static readonly string _defaultNamingPreferencesString = $@"
<NamingPreferencesInfo SerializationVersion=""3"">
  <SymbolSpecifications>
    <SymbolSpecification ID=""5c545a62-b14d-460a-88d8-e936c0a39316"" Name=""{WorkspacesResources.Class}"">
      <ApplicableSymbolKindList>
        <TypeKind>Class</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""23d856b4-5089-4405-83ce-749aada99153"" Name=""{WorkspacesResources.Interface}"">
      <ApplicableSymbolKindList>
        <TypeKind>Interface</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""d1796e78-ff66-463f-8576-eb46416060c0"" Name=""{WorkspacesResources.Struct}"">
      <ApplicableSymbolKindList>
        <TypeKind>Struct</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""d8af8dc6-1ade-441d-9947-8946922e198a"" Name=""{WorkspacesResources.Enum}"">
      <ApplicableSymbolKindList>
        <TypeKind>Enum</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""408a3347-b908-4b54-a954-1355e64c1de3"" Name=""{WorkspacesResources.Delegate}"">
      <ApplicableSymbolKindList>
        <TypeKind>Delegate</TypeKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""830657f6-e7e5-4830-b328-f109d3b6c165"" Name=""{WorkspacesResources.Event}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Event</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" Name=""{WorkspacesResources.Method}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""af410767-f189-47c6-b140-aeccf1ff242e"" Name=""{WorkspacesResources.Private_Method}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Private</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""8076757e-6a4a-47f1-9b4b-ae8a3284e987"" Name=""{WorkspacesResources.Abstract_Method}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsAbstract</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""16133061-a8e7-4392-92c3-1d93cd54c218"" Name=""{WorkspacesResources.Static_Method}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsStatic</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""03a274df-b686-4a76-9138-96aecb9bd33b"" Name=""{WorkspacesResources.Async_Method}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsAsync</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" Name=""{WorkspacesResources.Property}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Property</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""b24a91ce-3501-4799-b6df-baf044156c83"" Name=""{WorkspacesResources.Public_or_Protected_Field}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Field</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""70af42cb-1741-4027-969c-9edc4877d965"" Name=""{WorkspacesResources.Static_Field}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Field</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsStatic</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""10790aa6-0a0b-432d-a52d-d252ca92302b"" Name=""{WorkspacesResources.Private_or_Internal_Field}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Field</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""ac995be4-88de-4771-9dcc-a456a7c02d89"" Name=""{WorkspacesResources.Private_or_Internal_Static_Field}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Field</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList>
        <ModifierKind>IsStatic</ModifierKind>
      </RequiredModifierList>
    </SymbolSpecification>
    <SymbolSpecification ID=""2c07f5bf-bc81-4c2b-82b4-ae9b3ffd0ba4"" Name=""{WorkspacesResources.Types}"">
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
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""5f3ddba1-279f-486c-801e-5c097c36dd85"" Name=""{WorkspacesResources.Non_Field_Members}"">
      <ApplicableSymbolKindList>
        <SymbolKind>Property</SymbolKind>
        <SymbolKind>Method</SymbolKind>
        <SymbolKind>Event</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList>
        <AccessibilityKind>Public</AccessibilityKind>
        <AccessibilityKind>Internal</AccessibilityKind>
        <AccessibilityKind>Private</AccessibilityKind>
        <AccessibilityKind>Protected</AccessibilityKind>
        <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
      </ApplicableAccessibilityList>
      <RequiredModifierList />
    </SymbolSpecification>
  </SymbolSpecifications>
  <NamingStyles>
    <NamingStyle ID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" Name=""{WorkspacesResources.Pascal_Case}"" Prefix="""" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
    <NamingStyle ID=""308152f2-a334-48b3-8bec-ddee40785feb"" Name=""{WorkspacesResources.Ends_with_Async}"" Prefix="""" Suffix=""Async"" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
    <NamingStyle ID=""1ecc5eb6-b5fc-49a5-a9f1-a980f3e48c92"" Name=""{WorkspacesResources.Begins_with_I}"" Prefix=""I"" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
  </NamingStyles>
  <NamingRules>
    <SerializableNamingRule SymbolSpecificationID=""23d856b4-5089-4405-83ce-749aada99153"" NamingStyleID=""1ecc5eb6-b5fc-49a5-a9f1-a980f3e48c92"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""2c07f5bf-bc81-4c2b-82b4-ae9b3ffd0ba4"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""03a274df-b686-4a76-9138-96aecb9bd33b"" NamingStyleID=""308152f2-a334-48b3-8bec-ddee40785feb"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""5f3ddba1-279f-486c-801e-5c097c36dd85"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />
  </NamingRules>
</NamingPreferencesInfo>
";
    }
}
