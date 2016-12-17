// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles
{
    public partial class NamingStylesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private IDictionary<OptionKey, object> ClassNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), ClassNamesArePascalCaseOptionString());

        private IDictionary<OptionKey, object> MethodNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), MethodNamesArePascalCaseOptionString());

        private IDictionary<OptionKey, object> Options(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>();
            options.Add(option, value);
            return options;
        }

        private string ClassNamesArePascalCaseOptionString()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Class)),
                ImmutableArray<SymbolSpecification.AccessibilityKind>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new MutableNamingStyle();
            namingStyle.CapitalizationScheme = Capitalization.PascalCase;
            namingStyle.Name = "Name";
            namingStyle.Prefix = "";
            namingStyle.Suffix = "";
            namingStyle.WordSeparator = "";


            var namingRule = new SerializableNamingRule();
            namingRule.SymbolSpecificationID = symbolSpecification.ID;
            namingRule.NamingStyleID = namingStyle.ID;
            namingRule.EnforcementLevel = DiagnosticSeverity.Error;

            var info = new SerializableNamingStylePreferencesInfo();
            info.SymbolSpecifications.Add(symbolSpecification);
            info.NamingStyles.Add(namingStyle);
            info.NamingRules.Add(namingRule);

            return info.CreateXElement().ToString();
        }

        private string MethodNamesArePascalCaseOptionString()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Method)),
                ImmutableArray<SymbolSpecification.AccessibilityKind>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new MutableNamingStyle();
            namingStyle.CapitalizationScheme = Capitalization.PascalCase;
            namingStyle.Name = "Name";
            namingStyle.Prefix = "";
            namingStyle.Suffix = "";
            namingStyle.WordSeparator = "";


            var namingRule = new SerializableNamingRule();
            namingRule.SymbolSpecificationID = symbolSpecification.ID;
            namingRule.NamingStyleID = namingStyle.ID;
            namingRule.EnforcementLevel = DiagnosticSeverity.Error;

            var info = new SerializableNamingStylePreferencesInfo();
            info.SymbolSpecifications.Add(symbolSpecification);
            info.NamingStyles.Add(namingStyle);
            info.NamingRules.Add(namingRule);

            return info.CreateXElement().ToString();
        }
    }
}