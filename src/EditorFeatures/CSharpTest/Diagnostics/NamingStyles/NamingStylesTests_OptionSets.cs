// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles
{
    public partial class NamingStylesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private IDictionary<OptionKey, object> ClassNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), ClassNamesArePascalCaseOption());

        private IDictionary<OptionKey, object> MethodNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), MethodNamesArePascalCaseOption());

        private IDictionary<OptionKey, object> Options(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>
            {
                { option, value }
            };
            return options;
        }

        private NamingStylePreferences ClassNamesArePascalCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Class)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new MutableNamingStyle()
            {
                CapitalizationScheme = Capitalization.PascalCase,
                Name = "Name",
                Prefix = "",
                Suffix = "",
                WordSeparator = ""
            };
            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };
            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private NamingStylePreferences MethodNamesArePascalCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Method)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new MutableNamingStyle()
            {
                CapitalizationScheme = Capitalization.PascalCase,
                Name = "Name",
                Prefix = "",
                Suffix = "",
                WordSeparator = ""
            };
            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };
            var info = new NamingStylePreferences();
            info.SymbolSpecifications.Add(symbolSpecification);
            info.NamingStyles.Add(namingStyle);
            info.NamingRules.Add(namingRule);

            return info;
        }
    }
}