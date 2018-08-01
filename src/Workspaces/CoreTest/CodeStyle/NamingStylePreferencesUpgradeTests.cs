// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeStyle
{
    public class NamingStylePreferencesUpgradeTests
    {
        private static string ReserializePreferences(string serializedPreferences)
        {
            var preferences = NamingStylePreferences.FromXElement(XElement.Parse(serializedPreferences));
            return preferences.CreateXElement().ToString();
        }

        private static void AssertTrimmedEqual(string expected, string actual)
        {
            Assert.Equal(expected.Trim(), actual.Trim());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public void TestPreserveDefaultPreferences()
        {
            AssertTrimmedEqual(
                NamingStylePreferences.DefaultNamingPreferencesString,
                ReserializePreferences(NamingStylePreferences.DefaultNamingPreferencesString));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public void TestCannotUpgrade3To5()
        {
            var serializedPreferences = @"
<NamingPreferencesInfo SerializationVersion=""3"">
  <SymbolSpecifications>
    <SymbolSpecification ID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" Name=""Method"">
      <ApplicableSymbolKindList>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList />
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" Name=""Property Or Method"">
      <ApplicableSymbolKindList>
        <SymbolKind>Property</SymbolKind>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList />
      <RequiredModifierList />
    </SymbolSpecification>
  </SymbolSpecifications>
  <NamingStyles>
    <NamingStyle ID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" Name=""Test Pascal Case Rule"" Prefix="""" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
  </NamingStyles>
  <NamingRules>
    <SerializableNamingRule SymbolSpecificationID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Error"" />
  </NamingRules>
</NamingPreferencesInfo>";

            AssertTrimmedEqual(
                NamingStylePreferences.DefaultNamingPreferencesString,
                ReserializePreferences(serializedPreferences));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public void TestUpgrade4To5()
        {
            var serializedPreferences = @"
<NamingPreferencesInfo SerializationVersion=""4"">
  <SymbolSpecifications>
    <SymbolSpecification ID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" Name=""Method"">
      <ApplicableSymbolKindList>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList />
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" Name=""Property Or Method"">
      <ApplicableSymbolKindList>
        <SymbolKind>Property</SymbolKind>
        <SymbolKind>Method</SymbolKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList />
      <RequiredModifierList />
    </SymbolSpecification>
  </SymbolSpecifications>
  <NamingStyles>
    <NamingStyle ID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" Name=""Test Pascal Case Rule"" Prefix="""" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
  </NamingStyles>
  <NamingRules>
    <SerializableNamingRule SymbolSpecificationID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Error"" />
  </NamingRules>
</NamingPreferencesInfo>";

            AssertTrimmedEqual(
                serializedPreferences
                    .Replace("SerializationVersion=\"4\"", "SerializationVersion=\"5\"")
                    .Replace("<SymbolKind>Method</SymbolKind>", "<MethodKind>Ordinary</MethodKind>"),
                ReserializePreferences(serializedPreferences));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public void TestPreserveLatestVersion5()
        {
            var serializedPreferences = @"
<NamingPreferencesInfo SerializationVersion=""5"">
  <SymbolSpecifications>
    <SymbolSpecification ID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" Name=""Method"">
      <ApplicableSymbolKindList>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList />
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" Name=""Property Or Method"">
      <ApplicableSymbolKindList>
        <SymbolKind>Property</SymbolKind>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList />
      <RequiredModifierList />
    </SymbolSpecification>
  </SymbolSpecifications>
  <NamingStyles>
    <NamingStyle ID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" Name=""Test Pascal Case Rule"" Prefix="""" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
  </NamingStyles>
  <NamingRules>
    <SerializableNamingRule SymbolSpecificationID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Error"" />
  </NamingRules>
</NamingPreferencesInfo>";

            AssertTrimmedEqual(
                serializedPreferences,
                ReserializePreferences(serializedPreferences));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public void TestCannotDowngradeHigherThanLatestVersion5()
        {
            var serializedPreferences = @"
<NamingPreferencesInfo SerializationVersion=""6"">
  <SymbolSpecifications>
    <SymbolSpecification ID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" Name=""Method"">
      <ApplicableSymbolKindList>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList />
      <RequiredModifierList />
    </SymbolSpecification>
    <SymbolSpecification ID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" Name=""Property Or Method"">
      <ApplicableSymbolKindList>
        <SymbolKind>Property</SymbolKind>
        <MethodKind>Ordinary</MethodKind>
      </ApplicableSymbolKindList>
      <ApplicableAccessibilityList />
      <RequiredModifierList />
    </SymbolSpecification>
  </SymbolSpecifications>
  <NamingStyles>
    <NamingStyle ID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" Name=""Test Pascal Case Rule"" Prefix="""" Suffix="""" WordSeparator="""" CapitalizationScheme=""PascalCase"" />
  </NamingStyles>
  <NamingRules>
    <SerializableNamingRule SymbolSpecificationID=""390caed4-f0a9-42bb-adbb-b44c4a302a22"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Info"" />
    <SerializableNamingRule SymbolSpecificationID=""da6a2919-5aa6-4ad1-a24d-576776ed3974"" NamingStyleID=""87e7c501-9948-4b53-b1eb-a6cbe918feee"" EnforcementLevel=""Error"" />
  </NamingRules>
</NamingPreferencesInfo>";

            AssertTrimmedEqual(
                NamingStylePreferences.DefaultNamingPreferencesString,
                ReserializePreferences(serializedPreferences));
        }
    }
}
