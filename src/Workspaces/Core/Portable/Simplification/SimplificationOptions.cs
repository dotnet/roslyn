// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification
{
    /// <summary>
    /// Contains the options that needs to be drilled down to the Simplification Engine
    /// </summary>
    public static class SimplificationOptions
    {
        /// <summary>
        /// This option tells the simplification engine if the Qualified Name should be replaced by Alias
        /// if the user had initially not used the Alias
        /// </summary>
        public static Option<bool> PreferAliasToQualification { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(PreferAliasToQualification), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferAliasToQualification"));

        /// <summary>
        /// This option influences the name reduction of members of a module in VB. If set to true, the 
        /// name reducer will e.g. reduce Namespace.Module.Member to Namespace.Member.
        /// </summary>
        public static Option<bool> PreferOmittingModuleNamesInQualification { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(PreferOmittingModuleNamesInQualification), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferOmittingModuleNamesInQualification"));

        /// <summary>
        /// This option says that if we should simplify the Generic Name which has the type argument inferred
        /// </summary>
        public static Option<bool> PreferImplicitTypeInference { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(PreferImplicitTypeInference), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferImplicitTypeInference"));

        /// <summary>
        /// This option says if we should simplify the Explicit Type in Local Declarations
        /// </summary>
        public static Option<bool> PreferImplicitTypeInLocalDeclaration { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(PreferImplicitTypeInLocalDeclaration), false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferImplicitTypeInLocalDeclaration"));

        /// <summary>
        /// This option says if we should simplify to NonGeneric Name rather than GenericName
        /// </summary>
        public static Option<bool> AllowSimplificationToGenericType { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(AllowSimplificationToGenericType), false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AllowSimplificationToGenericType"));

        /// <summary>
        /// This option says if we should simplify from Derived types to Base types in Static Member Accesses
        /// </summary>
        public static Option<bool> AllowSimplificationToBaseType { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(AllowSimplificationToBaseType), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AllowSimplificationToBaseType"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/> or <see langword="Me"/> in member access expressions.
        /// </summary>
        [Obsolete]
        public static PerLanguageOption<bool> QualifyMemberAccessWithThisOrMe { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyMemberAccessWithThisOrMe), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyMemberAccessWithThisOrMe"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
        /// </summary>
        [Obsolete]
        public static PerLanguageOption<bool> QualifyFieldAccess { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyFieldAccess), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyFieldAccess"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
        /// </summary>
        [Obsolete]
        public static PerLanguageOption<bool> QualifyPropertyAccess{ get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyPropertyAccess), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyPropertyAccess"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
        /// </summary>
        [Obsolete]
        public static PerLanguageOption<bool> QualifyMethodAccess{ get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyMethodAccess), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyMethodAccess"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
        /// </summary>
        [Obsolete]
        public static PerLanguageOption<bool> QualifyEventAccess{ get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyEventAccess), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyEventAccess"));

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
        /// </summary>
        [Obsolete]
        public static PerLanguageOption<bool> PreferIntrinsicPredefinedTypeKeywordInDeclaration { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(PreferIntrinsicPredefinedTypeKeywordInDeclaration), defaultValue: true);

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        [Obsolete]
        public static PerLanguageOption<bool> PreferIntrinsicPredefinedTypeKeywordInMemberAccess { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(PreferIntrinsicPredefinedTypeKeywordInMemberAccess), defaultValue: true);

        private static string _defaultNamingPreferences = $@"
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

        /// <summary>
        /// This option describes the naming rules that should be applied to specified categories of symbols, 
        /// and the level to which those rules should be enforced.
        /// </summary>
        public static PerLanguageOption<string> NamingPreferences { get; } = new PerLanguageOption<string>(nameof(SimplificationOptions), nameof(NamingPreferences), defaultValue: _defaultNamingPreferences,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.NamingPreferences"));
    }
}
