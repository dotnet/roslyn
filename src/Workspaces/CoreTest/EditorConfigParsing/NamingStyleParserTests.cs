// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;
using static Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles.EditorConfigNamingStylesParser;

namespace Microsoft.CodeAnalysis.EditorConfigParsing.NamingStyles.UnitTests;

public sealed class NamingStyleParserTests
{
    [Fact]
    public void TestParseDefaultditorConfig()
    {
        var editorconfig = SourceText.From(DefaultDotNet6EditorConfigText);
        var namingStyles = Parse(editorconfig, null);

        var namingStyleSection = Assert.Single(namingStyles.Sections);
        Assert.Collection(namingStyles.Rules,
            rule0 => Assert.Equal("private_static_readonly_fields_should_be_pascalcase", rule0.RuleName.Value),
            rule1 => Assert.Equal("private_static_fields_should_be_s_camelcase", rule1.RuleName.Value),
            rule2 => Assert.Equal("methods_should_be_pascalcase", rule2.RuleName.Value),
            rule3 => Assert.Equal("interfaces_should_be_ipascalcase", rule3.RuleName.Value),
            rule4 => Assert.Equal("local_constants_should_be_camelcase", rule4.RuleName.Value),
            rule5 => Assert.Equal("public_fields_should_be_pascalcase", rule5.RuleName.Value),
            rule6 => Assert.Equal("parameters_should_be_camelcase", rule6.RuleName.Value),
            rule7 => Assert.Equal("public_constant_fields_should_be_pascalcase", rule7.RuleName.Value),
            rule8 => Assert.Equal("private_fields_should_be__camelcase", rule8.RuleName.Value),
            rule9 => Assert.Equal("local_functions_should_be_pascalcase", rule9.RuleName.Value),
            rule10 => Assert.Equal("type_parameters_should_be_tpascalcase", rule10.RuleName.Value),
            rule11 => Assert.Equal("local_variables_should_be_camelcase", rule11.RuleName.Value),
            rule12 => Assert.Equal("non_field_members_should_be_pascalcase", rule12.RuleName.Value),
            rule13 => Assert.Equal("types_and_namespaces_should_be_pascalcase", rule13.RuleName.Value),
            rule14 => Assert.Equal("enums_should_be_pascalcase", rule14.RuleName.Value),
            rule15 => Assert.Equal("public_static_readonly_fields_should_be_pascalcase", rule15.RuleName.Value),
            rule16 => Assert.Equal("properties_should_be_pascalcase", rule16.RuleName.Value),
            rule17 => Assert.Equal("events_should_be_pascalcase", rule17.RuleName.Value),
            rule18 => Assert.Equal("private_constant_fields_should_be_pascalcase", rule18.RuleName.Value));
    }

    [Fact]
    public void TestParseRoslynEditorConfig()
    {
        var editorconfig = SourceText.From(RoslynEditorConfigText);
        var namingStyles = Parse(editorconfig, null);

        var namingStyleSection = Assert.Single(namingStyles.Sections);
        Assert.Collection(namingStyles.Rules,
            rule0 =>
            {
                Assert.Equal(namingStyleSection, rule0.Section);
                Assert.Equal("non_private_static_fields_should_be_pascal_case", rule0.RuleName.Value);

                Assert.Equal("non_private_static_field_style", rule0.NamingScheme.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(2155, 2260), rule0.NamingScheme.OptionName.Span);
                Assert.Null(rule0.NamingScheme.Prefix.Value);
                Assert.Null(rule0.NamingScheme.Prefix.Span);
                Assert.Null(rule0.NamingScheme.Suffix.Value);
                Assert.Null(rule0.NamingScheme.Suffix.Span);
                Assert.Null(rule0.NamingScheme.WordSeparator.Value);
                Assert.Null(rule0.NamingScheme.WordSeparator.Span);
                Assert.Equal(Capitalization.PascalCase, rule0.NamingScheme.Capitalization.Value);
                Assert.Equal(TextSpan.FromBounds(2562, 2641), rule0.NamingScheme.Capitalization.Span);

                Assert.Equal("non_private_static_fields", rule0.ApplicableSymbolInfo.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(2338, 2481), rule0.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Collection(rule0.ApplicableSymbolInfo.Accessibilities.Value,
                    accessibility => Assert.Equal(Accessibility.Public, accessibility),
                    accessibility => Assert.Equal(Accessibility.Protected, accessibility),
                    accessibility => Assert.Equal(Accessibility.Friend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedOrFriend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedAndFriend, accessibility));
                Assert.Equal(TextSpan.FromBounds(2338, 2481), rule0.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Collection(rule0.ApplicableSymbolInfo.Modifiers.Value,
                    modifier => Assert.Equal(new ModifierKind(Modifiers.Static), modifier));
                Assert.Equal(TextSpan.FromBounds(2483, 2558), rule0.ApplicableSymbolInfo.Modifiers.Span);
                Assert.Collection(rule0.ApplicableSymbolInfo.SymbolKinds.Value,
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Field), symbolKind));
                Assert.Equal(TextSpan.FromBounds(2338, 2481), rule0.ApplicableSymbolInfo.Accessibilities.Span);

                Assert.Equal(ReportDiagnostic.Info, rule0.Severity.Value);
                Assert.Equal(TextSpan.FromBounds(1961, 2049), rule0.Severity.Span);
            },
            rule1 =>
            {
                Assert.Equal(namingStyleSection, rule1.Section);
                Assert.Equal("locals_should_be_camel_case", rule1.RuleName.Value);

                Assert.Equal("camel_case_style", rule1.NamingScheme.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(5078, 5149), rule1.NamingScheme.OptionName.Span);
                Assert.Null(rule1.NamingScheme.Prefix.Value);
                Assert.Null(rule1.NamingScheme.Prefix.Span);
                Assert.Null(rule1.NamingScheme.Suffix.Value);
                Assert.Null(rule1.NamingScheme.Suffix.Span);
                Assert.Null(rule1.NamingScheme.WordSeparator.Value);
                Assert.Null(rule1.NamingScheme.WordSeparator.Span);
                Assert.Equal(Capitalization.CamelCase, rule1.NamingScheme.Capitalization.Value);
                Assert.Equal(TextSpan.FromBounds(5236, 5300), rule1.NamingScheme.Capitalization.Span);

                Assert.Equal("locals_and_parameters", rule1.ApplicableSymbolInfo.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(4998, 5076), rule1.ApplicableSymbolInfo.OptionName.Span);
                Assert.Collection(rule1.ApplicableSymbolInfo.Accessibilities.Value,
                    accessibility => Assert.Equal(Accessibility.NotApplicable, accessibility),
                    accessibility => Assert.Equal(Accessibility.Public, accessibility),
                    accessibility => Assert.Equal(Accessibility.Friend, accessibility),
                    accessibility => Assert.Equal(Accessibility.Private, accessibility),
                    accessibility => Assert.Equal(Accessibility.Protected, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedAndFriend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedOrFriend, accessibility));
                Assert.Null(rule1.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Empty(rule1.ApplicableSymbolInfo.Modifiers.Value);
                Assert.Null(rule1.ApplicableSymbolInfo.Modifiers.Span);
                Assert.Collection(rule1.ApplicableSymbolInfo.SymbolKinds.Value,
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Parameter), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Local), symbolKind));
                Assert.Equal(TextSpan.FromBounds(5153, 5232), rule1.ApplicableSymbolInfo.SymbolKinds.Span);

                Assert.Equal(ReportDiagnostic.Info, rule1.Severity.Value);
                Assert.Equal(TextSpan.FromBounds(4928, 4996), rule1.Severity.Span);
            },
            rule2 =>
            {
                Assert.Equal(namingStyleSection, rule2.Section);
                Assert.Equal("members_should_be_pascal_case", rule2.RuleName.Value);

                Assert.Equal("pascal_case_style", rule2.NamingScheme.OptionName.Value);
                Assert.Equal("pascal_case_style", rule2.NamingScheme.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(5925, 5999), rule2.NamingScheme.OptionName.Span);
                Assert.Null(rule2.NamingScheme.Prefix.Value);
                Assert.Null(rule2.NamingScheme.Prefix.Span);
                Assert.Null(rule2.NamingScheme.Suffix.Value);
                Assert.Null(rule2.NamingScheme.Suffix.Span);
                Assert.Null(rule2.NamingScheme.WordSeparator.Value);
                Assert.Null(rule2.NamingScheme.WordSeparator.Span);
                Assert.Equal(Capitalization.PascalCase, rule2.NamingScheme.Capitalization.Value);
                Assert.Equal(TextSpan.FromBounds(6061, 6127), rule2.NamingScheme.Capitalization.Span);

                Assert.Equal("all_members", rule2.ApplicableSymbolInfo.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(5853, 5923), rule2.ApplicableSymbolInfo.OptionName.Span);
                Assert.Collection(rule2.ApplicableSymbolInfo.Accessibilities.Value,
                    accessibility => Assert.Equal(Accessibility.NotApplicable, accessibility),
                    accessibility => Assert.Equal(Accessibility.Public, accessibility),
                    accessibility => Assert.Equal(Accessibility.Friend, accessibility),
                    accessibility => Assert.Equal(Accessibility.Private, accessibility),
                    accessibility => Assert.Equal(Accessibility.Protected, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedAndFriend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedOrFriend, accessibility));
                Assert.Null(rule2.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Empty(rule2.ApplicableSymbolInfo.Modifiers.Value);
                Assert.Null(rule2.ApplicableSymbolInfo.Modifiers.Span);
                Assert.Collection(rule2.ApplicableSymbolInfo.SymbolKinds.Value,
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Namespace), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(TypeKind.Class), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(TypeKind.Struct), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(TypeKind.Interface), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(TypeKind.Enum), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Property), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(MethodKind.Ordinary), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(MethodKind.LocalFunction), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Field), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Event), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(TypeKind.Delegate), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Parameter), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.TypeParameter), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Local), symbolKind));
                Assert.Equal(TextSpan.FromBounds(6003, 6057), rule2.ApplicableSymbolInfo.SymbolKinds.Span);

                Assert.Equal(ReportDiagnostic.Info, rule2.Severity.Value);
                Assert.Equal(TextSpan.FromBounds(5781, 5851), rule2.Severity.Span);
            },
            rule3 =>
            {
                Assert.Equal(namingStyleSection, rule3.Section);
                Assert.Equal("non_private_readonly_fields_should_be_pascal_case", rule3.RuleName.Value);

                Assert.Equal("non_private_readonly_field_style", rule3.NamingScheme.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(2891, 3000), rule3.NamingScheme.OptionName.Span);
                Assert.Null(rule3.NamingScheme.Prefix.Value);
                Assert.Null(rule3.NamingScheme.Prefix.Span);
                Assert.Null(rule3.NamingScheme.Suffix.Value);
                Assert.Null(rule3.NamingScheme.Suffix.Span);
                Assert.Null(rule3.NamingScheme.WordSeparator.Value);
                Assert.Null(rule3.NamingScheme.WordSeparator.Span);
                Assert.Equal(Capitalization.PascalCase, rule3.NamingScheme.Capitalization.Value);
                Assert.Equal(TextSpan.FromBounds(3310, 3391), rule3.NamingScheme.Capitalization.Span);

                Assert.Equal("non_private_readonly_fields", rule3.ApplicableSymbolInfo.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(2783, 2889), rule3.ApplicableSymbolInfo.OptionName.Span);
                Assert.Collection(rule3.ApplicableSymbolInfo.Accessibilities.Value,
                    accessibility => Assert.Equal(Accessibility.Public, accessibility),
                    accessibility => Assert.Equal(Accessibility.Protected, accessibility),
                    accessibility => Assert.Equal(Accessibility.Friend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedOrFriend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedAndFriend, accessibility));
                Assert.Equal(TextSpan.FromBounds(3080, 3225), rule3.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Collection(rule3.ApplicableSymbolInfo.Modifiers.Value,
                    modifier => Assert.Equal(new ModifierKind(Modifiers.ReadOnly), modifier));
                Assert.Equal(TextSpan.FromBounds(3227, 3306), rule3.ApplicableSymbolInfo.Modifiers.Span);
                Assert.Collection(rule3.ApplicableSymbolInfo.SymbolKinds.Value,
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Field), symbolKind));
                Assert.Equal(TextSpan.FromBounds(3004, 3078), rule3.ApplicableSymbolInfo.SymbolKinds.Span);

                Assert.Equal(ReportDiagnostic.Info, rule3.Severity.Value);
                Assert.Equal(TextSpan.FromBounds(2691, 2781), rule3.Severity.Span);
            },
            rule4 =>
            {
                Assert.Equal(namingStyleSection, rule4.Section);
                Assert.Equal("local_functions_should_be_pascal_case", rule4.RuleName.Value);

                Assert.Equal("local_function_style", rule4.NamingScheme.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(5502, 5587), rule4.NamingScheme.OptionName.Span);
                Assert.Null(rule4.NamingScheme.Prefix.Value);
                Assert.Null(rule4.NamingScheme.Prefix.Span);
                Assert.Null(rule4.NamingScheme.Suffix.Value);
                Assert.Null(rule4.NamingScheme.Suffix.Span);
                Assert.Null(rule4.NamingScheme.WordSeparator.Value);
                Assert.Null(rule4.NamingScheme.WordSeparator.Span);
                Assert.Equal(Capitalization.PascalCase, rule4.NamingScheme.Capitalization.Value);
                Assert.Equal(TextSpan.FromBounds(5666, 5735), rule4.NamingScheme.Capitalization.Span);

                Assert.Equal("local_functions", rule4.ApplicableSymbolInfo.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(5418, 5500), rule4.ApplicableSymbolInfo.OptionName.Span);
                Assert.Collection(rule4.ApplicableSymbolInfo.Accessibilities.Value,
                    accessibility => Assert.Equal(Accessibility.NotApplicable, accessibility),
                    accessibility => Assert.Equal(Accessibility.Public, accessibility),
                    accessibility => Assert.Equal(Accessibility.Friend, accessibility),
                    accessibility => Assert.Equal(Accessibility.Private, accessibility),
                    accessibility => Assert.Equal(Accessibility.Protected, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedAndFriend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedOrFriend, accessibility));
                Assert.Null(rule4.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Empty(rule4.ApplicableSymbolInfo.Modifiers.Value);
                Assert.Null(rule4.ApplicableSymbolInfo.Modifiers.Span);
                Assert.Collection(rule4.ApplicableSymbolInfo.SymbolKinds.Value,
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(MethodKind.LocalFunction), symbolKind));
                Assert.Equal(TextSpan.FromBounds(5591, 5662), rule4.ApplicableSymbolInfo.SymbolKinds.Span);

                Assert.Equal(ReportDiagnostic.Info, rule4.Severity.Value);
                Assert.Equal(TextSpan.FromBounds(5338, 5416), rule4.Severity.Span);
            },
            rule5 =>
            {
                Assert.Equal(namingStyleSection, rule5.Section);
                Assert.Equal("constants_should_be_pascal_case", rule5.RuleName.Value);

                Assert.Equal("constant_style", rule5.NamingScheme.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(3569, 3642), rule5.NamingScheme.OptionName.Span);
                Assert.Null(rule5.NamingScheme.Prefix.Value);
                Assert.Null(rule5.NamingScheme.Prefix.Span);
                Assert.Null(rule5.NamingScheme.Suffix.Value);
                Assert.Null(rule5.NamingScheme.Suffix.Span);
                Assert.Null(rule5.NamingScheme.WordSeparator.Value);
                Assert.Null(rule5.NamingScheme.WordSeparator.Span);
                Assert.Equal(Capitalization.PascalCase, rule5.NamingScheme.Capitalization.Value);
                Assert.Equal(TextSpan.FromBounds(3773, 3836), rule5.NamingScheme.Capitalization.Span);

                Assert.Equal("constants", rule5.ApplicableSymbolInfo.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(3497, 3567), rule5.ApplicableSymbolInfo.OptionName.Span);
                Assert.Collection(rule5.ApplicableSymbolInfo.Accessibilities.Value,
                    accessibility => Assert.Equal(Accessibility.NotApplicable, accessibility),
                    accessibility => Assert.Equal(Accessibility.Public, accessibility),
                    accessibility => Assert.Equal(Accessibility.Friend, accessibility),
                    accessibility => Assert.Equal(Accessibility.Private, accessibility),
                    accessibility => Assert.Equal(Accessibility.Protected, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedAndFriend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedOrFriend, accessibility));
                Assert.Null(rule5.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Collection(rule5.ApplicableSymbolInfo.Modifiers.Value,
                    modifier => Assert.Equal(new ModifierKind(Modifiers.Const), modifier));
                Assert.Equal(TextSpan.FromBounds(3711, 3769), rule5.ApplicableSymbolInfo.Modifiers.Span);
                Assert.Collection(rule5.ApplicableSymbolInfo.SymbolKinds.Value,
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Field), symbolKind),
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Local), symbolKind));
                Assert.Equal(TextSpan.FromBounds(3646, 3709), rule5.ApplicableSymbolInfo.SymbolKinds.Span);

                Assert.Equal(ReportDiagnostic.Info, rule5.Severity.Value);
                Assert.Equal(TextSpan.FromBounds(3423, 3495), rule5.Severity.Span);
            },
            rule6 =>
            {
                Assert.Equal(namingStyleSection, rule6.Section);
                Assert.Equal("instance_fields_should_be_camel_case", rule6.RuleName.Value);

                Assert.Equal("instance_field_style", rule6.NamingScheme.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(4601, 4685), rule6.NamingScheme.OptionName.Span);
                Assert.Equal("_", rule6.NamingScheme.Prefix.Value);
                Assert.Equal(TextSpan.FromBounds(4825, 4885), rule6.NamingScheme.Prefix.Span);
                Assert.Null(rule6.NamingScheme.Suffix.Value);
                Assert.Null(rule6.NamingScheme.Suffix.Span);
                Assert.Null(rule6.NamingScheme.WordSeparator.Value);
                Assert.Null(rule6.NamingScheme.WordSeparator.Span);
                Assert.Equal(Capitalization.CamelCase, rule6.NamingScheme.Capitalization.Value);
                Assert.Equal(TextSpan.FromBounds(4755, 4823), rule6.NamingScheme.Capitalization.Span);

                Assert.Equal("instance_fields", rule6.ApplicableSymbolInfo.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(4518, 4599), rule6.ApplicableSymbolInfo.OptionName.Span);
                Assert.Collection(rule6.ApplicableSymbolInfo.Accessibilities.Value,
                    accessibility => Assert.Equal(Accessibility.NotApplicable, accessibility),
                    accessibility => Assert.Equal(Accessibility.Public, accessibility),
                    accessibility => Assert.Equal(Accessibility.Friend, accessibility),
                    accessibility => Assert.Equal(Accessibility.Private, accessibility),
                    accessibility => Assert.Equal(Accessibility.Protected, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedAndFriend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedOrFriend, accessibility));
                Assert.Null(rule6.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Empty(rule6.ApplicableSymbolInfo.Modifiers.Value);
                Assert.Null(rule6.ApplicableSymbolInfo.Modifiers.Span);
                Assert.Collection(rule6.ApplicableSymbolInfo.SymbolKinds.Value,
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Field), symbolKind));
                Assert.Equal(TextSpan.FromBounds(4689, 4751), rule6.ApplicableSymbolInfo.SymbolKinds.Span);

                Assert.Equal(ReportDiagnostic.Info, rule6.Severity.Value);
                Assert.Equal(TextSpan.FromBounds(4439, 4516), rule6.Severity.Span);
            },
            rule7 =>
            {
                Assert.Equal(namingStyleSection, rule7.Section);
                Assert.Equal("static_fields_should_be_camel_case", rule7.RuleName.Value);

                Assert.Equal("static_field_style", rule7.NamingScheme.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(4045, 4125), rule7.NamingScheme.OptionName.Span);
                Assert.Equal("s_", rule7.NamingScheme.Prefix.Value);
                Assert.Equal(TextSpan.FromBounds(4326, 4385), rule7.NamingScheme.Prefix.Span);
                Assert.Null(rule7.NamingScheme.Suffix.Value);
                Assert.Null(rule7.NamingScheme.Suffix.Span);
                Assert.Null(rule7.NamingScheme.WordSeparator.Value);
                Assert.Null(rule7.NamingScheme.WordSeparator.Span);
                Assert.Equal(Capitalization.CamelCase, rule7.NamingScheme.Capitalization.Value);
                Assert.Equal(TextSpan.FromBounds(4258, 4324), rule7.NamingScheme.Capitalization.Span);

                Assert.Equal("static_fields", rule7.ApplicableSymbolInfo.OptionName.Value);
                Assert.Equal(TextSpan.FromBounds(3966, 4043), rule7.ApplicableSymbolInfo.OptionName.Span);
                Assert.Collection(rule7.ApplicableSymbolInfo.Accessibilities.Value,
                    accessibility => Assert.Equal(Accessibility.NotApplicable, accessibility),
                    accessibility => Assert.Equal(Accessibility.Public, accessibility),
                    accessibility => Assert.Equal(Accessibility.Friend, accessibility),
                    accessibility => Assert.Equal(Accessibility.Private, accessibility),
                    accessibility => Assert.Equal(Accessibility.Protected, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedAndFriend, accessibility),
                    accessibility => Assert.Equal(Accessibility.ProtectedOrFriend, accessibility));
                Assert.Null(rule7.ApplicableSymbolInfo.Accessibilities.Span);
                Assert.Collection(rule7.ApplicableSymbolInfo.Modifiers.Value,
                    modifier => Assert.Equal(new ModifierKind(Modifiers.Static), modifier));
                Assert.Equal(TextSpan.FromBounds(4191, 4254), rule7.ApplicableSymbolInfo.Modifiers.Span);
                Assert.Collection(rule7.ApplicableSymbolInfo.SymbolKinds.Value,
                    symbolKind => Assert.Equal(new SymbolKindOrTypeKind(SymbolKind.Field), symbolKind));
                Assert.Equal(TextSpan.FromBounds(4129, 4189), rule7.ApplicableSymbolInfo.SymbolKinds.Span);

                Assert.Equal(ReportDiagnostic.Info, rule7.Severity.Value);
                Assert.Equal(TextSpan.FromBounds(3889, 3964), rule7.Severity.Span);
            });
    }

    private const string DefaultDotNet6EditorConfigText =
        """
        root = true

        # All files
        [*]
        indent_style = space

        # Xml files
        [*.xml]
        indent_size = 2

        # C# files
        [*.cs]

        #### Core EditorConfig Options ####

        # Indentation and spacing
        indent_size = 4
        tab_width = 4

        # New line preferences
        end_of_line = crlf
        insert_final_newline = false

        #### .NET Coding Conventions ####
        [*.{cs,vb}]

        # Organize usings
        dotnet_separate_import_directive_groups = true
        dotnet_sort_system_directives_first = true
        file_header_template = unset

        # this. and Me. preferences
        dotnet_style_qualification_for_event = false:silent
        dotnet_style_qualification_for_field = false:silent
        dotnet_style_qualification_for_method = false:silent
        dotnet_style_qualification_for_property = false:silent

        # Language keywords vs BCL types preferences
        dotnet_style_predefined_type_for_locals_parameters_members = true:silent
        dotnet_style_predefined_type_for_member_access = true:silent

        # Parentheses preferences
        dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:silent
        dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:silent
        dotnet_style_parentheses_in_other_operators = never_if_unnecessary:silent
        dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:silent

        # Modifier preferences
        dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent

        # Expression-level preferences
        dotnet_style_coalesce_expression = true:suggestion
        dotnet_style_collection_initializer = true:suggestion
        dotnet_style_explicit_tuple_names = true:suggestion
        dotnet_style_null_propagation = true:suggestion
        dotnet_style_object_initializer = true:suggestion
        dotnet_style_operator_placement_when_wrapping = beginning_of_line
        dotnet_style_prefer_auto_properties = true:suggestion
        dotnet_style_prefer_compound_assignment = true:suggestion
        dotnet_style_prefer_conditional_expression_over_assignment = true:suggestion
        dotnet_style_prefer_conditional_expression_over_return = true:suggestion
        dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
        dotnet_style_prefer_inferred_tuple_names = true:suggestion
        dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion
        dotnet_style_prefer_simplified_boolean_expressions = true:suggestion
        dotnet_style_prefer_simplified_interpolation = true:suggestion

        # Field preferences
        dotnet_style_readonly_field = true:warning

        # Parameter preferences
        dotnet_code_quality_unused_parameters = all:suggestion

        # Suppression preferences
        dotnet_remove_unnecessary_suppression_exclusions = none

        #### C# Coding Conventions ####
        [*.cs]

        # var preferences
        csharp_style_var_elsewhere = false:silent
        csharp_style_var_for_built_in_types = false:silent
        csharp_style_var_when_type_is_apparent = false:silent

        # Expression-bodied members
        csharp_style_expression_bodied_accessors = true:silent
        csharp_style_expression_bodied_constructors = false:silent
        csharp_style_expression_bodied_indexers = true:silent
        csharp_style_expression_bodied_lambdas = true:suggestion
        csharp_style_expression_bodied_local_functions = false:silent
        csharp_style_expression_bodied_methods = false:silent
        csharp_style_expression_bodied_operators = false:silent
        csharp_style_expression_bodied_properties = true:silent

        # Pattern matching preferences
        csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
        csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
        csharp_style_prefer_not_pattern = true:suggestion
        csharp_style_prefer_pattern_matching = true:silent
        csharp_style_prefer_switch_expression = true:suggestion

        # Null-checking preferences
        csharp_style_conditional_delegate_call = true:suggestion

        # Modifier preferences
        csharp_prefer_static_local_function = true:warning
        csharp_preferred_modifier_order = public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async:silent

        # Code-block preferences
        csharp_prefer_braces = true:silent
        csharp_prefer_simple_using_statement = true:suggestion

        # Expression-level preferences
        csharp_prefer_simple_default_expression = true:suggestion
        csharp_style_deconstructed_variable_declaration = true:suggestion
        csharp_style_inlined_variable_declaration = true:suggestion
        csharp_style_pattern_local_over_anonymous_function = true:suggestion
        csharp_style_prefer_index_operator = true:suggestion
        csharp_style_prefer_range_operator = true:suggestion
        csharp_style_throw_expression = true:suggestion
        csharp_style_unused_value_assignment_preference = discard_variable:suggestion
        csharp_style_unused_value_expression_statement_preference = discard_variable:silent

        # 'using' directive preferences
        csharp_using_directive_placement = outside_namespace:silent

        #### C# Formatting Rules ####

        # New line preferences
        csharp_new_line_before_catch = true
        csharp_new_line_before_else = true
        csharp_new_line_before_finally = true
        csharp_new_line_before_members_in_anonymous_types = true
        csharp_new_line_before_members_in_object_initializers = true
        csharp_new_line_before_open_brace = all
        csharp_new_line_between_query_expression_clauses = true

        # Indentation preferences
        csharp_indent_block_contents = true
        csharp_indent_braces = false
        csharp_indent_case_contents = true
        csharp_indent_case_contents_when_block = true
        csharp_indent_labels = one_less_than_current
        csharp_indent_switch_labels = true

        # Space preferences
        csharp_space_after_cast = false
        csharp_space_after_colon_in_inheritance_clause = true
        csharp_space_after_comma = true
        csharp_space_after_dot = false
        csharp_space_after_keywords_in_control_flow_statements = true
        csharp_space_after_semicolon_in_for_statement = true
        csharp_space_around_binary_operators = before_and_after
        csharp_space_around_declaration_statements = false
        csharp_space_before_colon_in_inheritance_clause = true
        csharp_space_before_comma = false
        csharp_space_before_dot = false
        csharp_space_before_open_square_brackets = false
        csharp_space_before_semicolon_in_for_statement = false
        csharp_space_between_empty_square_brackets = false
        csharp_space_between_method_call_empty_parameter_list_parentheses = false
        csharp_space_between_method_call_name_and_opening_parenthesis = false
        csharp_space_between_method_call_parameter_list_parentheses = false
        csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
        csharp_space_between_method_declaration_name_and_open_parenthesis = false
        csharp_space_between_method_declaration_parameter_list_parentheses = false
        csharp_space_between_parentheses = false
        csharp_space_between_square_brackets = false

        # Wrapping preferences
        csharp_preserve_single_line_blocks = true
        csharp_preserve_single_line_statements = true

        #### Naming styles ####
        [*.{cs,vb}]

        # Naming rules

        dotnet_naming_rule.types_and_namespaces_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.types_and_namespaces_should_be_pascalcase.symbols = types_and_namespaces
        dotnet_naming_rule.types_and_namespaces_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.interfaces_should_be_ipascalcase.severity = suggestion
        dotnet_naming_rule.interfaces_should_be_ipascalcase.symbols = interfaces
        dotnet_naming_rule.interfaces_should_be_ipascalcase.style = ipascalcase

        dotnet_naming_rule.type_parameters_should_be_tpascalcase.severity = suggestion
        dotnet_naming_rule.type_parameters_should_be_tpascalcase.symbols = type_parameters
        dotnet_naming_rule.type_parameters_should_be_tpascalcase.style = tpascalcase

        dotnet_naming_rule.methods_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.methods_should_be_pascalcase.symbols = methods
        dotnet_naming_rule.methods_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.properties_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.properties_should_be_pascalcase.symbols = properties
        dotnet_naming_rule.properties_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.events_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.events_should_be_pascalcase.symbols = events
        dotnet_naming_rule.events_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.local_variables_should_be_camelcase.severity = suggestion
        dotnet_naming_rule.local_variables_should_be_camelcase.symbols = local_variables
        dotnet_naming_rule.local_variables_should_be_camelcase.style = camelcase

        dotnet_naming_rule.local_constants_should_be_camelcase.severity = suggestion
        dotnet_naming_rule.local_constants_should_be_camelcase.symbols = local_constants
        dotnet_naming_rule.local_constants_should_be_camelcase.style = camelcase

        dotnet_naming_rule.parameters_should_be_camelcase.severity = suggestion
        dotnet_naming_rule.parameters_should_be_camelcase.symbols = parameters
        dotnet_naming_rule.parameters_should_be_camelcase.style = camelcase

        dotnet_naming_rule.public_fields_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.public_fields_should_be_pascalcase.symbols = public_fields
        dotnet_naming_rule.public_fields_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.private_fields_should_be__camelcase.severity = suggestion
        dotnet_naming_rule.private_fields_should_be__camelcase.symbols = private_fields
        dotnet_naming_rule.private_fields_should_be__camelcase.style = _camelcase

        dotnet_naming_rule.private_static_fields_should_be_s_camelcase.severity = suggestion
        dotnet_naming_rule.private_static_fields_should_be_s_camelcase.symbols = private_static_fields
        dotnet_naming_rule.private_static_fields_should_be_s_camelcase.style = s_camelcase

        dotnet_naming_rule.public_constant_fields_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.public_constant_fields_should_be_pascalcase.symbols = public_constant_fields
        dotnet_naming_rule.public_constant_fields_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.private_constant_fields_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.private_constant_fields_should_be_pascalcase.symbols = private_constant_fields
        dotnet_naming_rule.private_constant_fields_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.public_static_readonly_fields_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.public_static_readonly_fields_should_be_pascalcase.symbols = public_static_readonly_fields
        dotnet_naming_rule.public_static_readonly_fields_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.private_static_readonly_fields_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.private_static_readonly_fields_should_be_pascalcase.symbols = private_static_readonly_fields
        dotnet_naming_rule.private_static_readonly_fields_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.enums_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.enums_should_be_pascalcase.symbols = enums
        dotnet_naming_rule.enums_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.local_functions_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.local_functions_should_be_pascalcase.symbols = local_functions
        dotnet_naming_rule.local_functions_should_be_pascalcase.style = pascalcase

        dotnet_naming_rule.non_field_members_should_be_pascalcase.severity = suggestion
        dotnet_naming_rule.non_field_members_should_be_pascalcase.symbols = non_field_members
        dotnet_naming_rule.non_field_members_should_be_pascalcase.style = pascalcase

        # Symbol specifications

        dotnet_naming_symbols.interfaces.applicable_kinds = interface
        dotnet_naming_symbols.interfaces.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
        dotnet_naming_symbols.interfaces.required_modifiers = 

        dotnet_naming_symbols.enums.applicable_kinds = enum
        dotnet_naming_symbols.enums.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
        dotnet_naming_symbols.enums.required_modifiers = 

        dotnet_naming_symbols.events.applicable_kinds = event
        dotnet_naming_symbols.events.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
        dotnet_naming_symbols.events.required_modifiers = 

        dotnet_naming_symbols.methods.applicable_kinds = method
        dotnet_naming_symbols.methods.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
        dotnet_naming_symbols.methods.required_modifiers = 

        dotnet_naming_symbols.properties.applicable_kinds = property
        dotnet_naming_symbols.properties.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
        dotnet_naming_symbols.properties.required_modifiers = 

        dotnet_naming_symbols.public_fields.applicable_kinds = field
        dotnet_naming_symbols.public_fields.applicable_accessibilities = public, internal
        dotnet_naming_symbols.public_fields.required_modifiers = 

        dotnet_naming_symbols.private_fields.applicable_kinds = field
        dotnet_naming_symbols.private_fields.applicable_accessibilities = private, protected, protected_internal, private_protected
        dotnet_naming_symbols.private_fields.required_modifiers = 

        dotnet_naming_symbols.private_static_fields.applicable_kinds = field
        dotnet_naming_symbols.private_static_fields.applicable_accessibilities = private, protected, protected_internal, private_protected
        dotnet_naming_symbols.private_static_fields.required_modifiers = static

        dotnet_naming_symbols.types_and_namespaces.applicable_kinds = namespace, class, struct, interface, enum
        dotnet_naming_symbols.types_and_namespaces.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
        dotnet_naming_symbols.types_and_namespaces.required_modifiers = 

        dotnet_naming_symbols.non_field_members.applicable_kinds = property, event, method
        dotnet_naming_symbols.non_field_members.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
        dotnet_naming_symbols.non_field_members.required_modifiers = 

        dotnet_naming_symbols.type_parameters.applicable_kinds = namespace
        dotnet_naming_symbols.type_parameters.applicable_accessibilities = *
        dotnet_naming_symbols.type_parameters.required_modifiers = 

        dotnet_naming_symbols.private_constant_fields.applicable_kinds = field
        dotnet_naming_symbols.private_constant_fields.applicable_accessibilities = private, protected, protected_internal, private_protected
        dotnet_naming_symbols.private_constant_fields.required_modifiers = const

        dotnet_naming_symbols.local_variables.applicable_kinds = local
        dotnet_naming_symbols.local_variables.applicable_accessibilities = local
        dotnet_naming_symbols.local_variables.required_modifiers = 

        dotnet_naming_symbols.local_constants.applicable_kinds = local
        dotnet_naming_symbols.local_constants.applicable_accessibilities = local
        dotnet_naming_symbols.local_constants.required_modifiers = const

        dotnet_naming_symbols.parameters.applicable_kinds = parameter
        dotnet_naming_symbols.parameters.applicable_accessibilities = *
        dotnet_naming_symbols.parameters.required_modifiers = 

        dotnet_naming_symbols.public_constant_fields.applicable_kinds = field
        dotnet_naming_symbols.public_constant_fields.applicable_accessibilities = public, internal
        dotnet_naming_symbols.public_constant_fields.required_modifiers = const

        dotnet_naming_symbols.public_static_readonly_fields.applicable_kinds = field
        dotnet_naming_symbols.public_static_readonly_fields.applicable_accessibilities = public, internal
        dotnet_naming_symbols.public_static_readonly_fields.required_modifiers = readonly, static

        dotnet_naming_symbols.private_static_readonly_fields.applicable_kinds = field
        dotnet_naming_symbols.private_static_readonly_fields.applicable_accessibilities = private, protected, protected_internal, private_protected
        dotnet_naming_symbols.private_static_readonly_fields.required_modifiers = readonly, static

        dotnet_naming_symbols.local_functions.applicable_kinds = local_function
        dotnet_naming_symbols.local_functions.applicable_accessibilities = *
        dotnet_naming_symbols.local_functions.required_modifiers = 

        # Naming styles

        dotnet_naming_style.pascalcase.required_prefix = 
        dotnet_naming_style.pascalcase.required_suffix = 
        dotnet_naming_style.pascalcase.word_separator = 
        dotnet_naming_style.pascalcase.capitalization = pascal_case

        dotnet_naming_style.ipascalcase.required_prefix = I
        dotnet_naming_style.ipascalcase.required_suffix = 
        dotnet_naming_style.ipascalcase.word_separator = 
        dotnet_naming_style.ipascalcase.capitalization = pascal_case

        dotnet_naming_style.tpascalcase.required_prefix = T
        dotnet_naming_style.tpascalcase.required_suffix = 
        dotnet_naming_style.tpascalcase.word_separator = 
        dotnet_naming_style.tpascalcase.capitalization = pascal_case

        dotnet_naming_style._camelcase.required_prefix = _
        dotnet_naming_style._camelcase.required_suffix = 
        dotnet_naming_style._camelcase.word_separator = 
        dotnet_naming_style._camelcase.capitalization = camel_case

        dotnet_naming_style.camelcase.required_prefix = 
        dotnet_naming_style.camelcase.required_suffix = 
        dotnet_naming_style.camelcase.word_separator = 
        dotnet_naming_style.camelcase.capitalization = camel_case

        dotnet_naming_style.s_camelcase.required_prefix = s_
        dotnet_naming_style.s_camelcase.required_suffix = 
        dotnet_naming_style.s_camelcase.word_separator = 
        dotnet_naming_style.s_camelcase.capitalization = camel_case
        """;

    private const string RoslynEditorConfigText =
        """
        # EditorConfig is awesome: https://EditorConfig.org

        # top-most EditorConfig file
        root = true

        # Don't use tabs for indentation.
        [*]
        indent_style = space
        # (Please don't specify an indent_size here; that has too many unintended consequences.)

        # Code files
        [*.{cs,csx,vb,vbx}]
        indent_size = 4
        insert_final_newline = true
        charset = utf-8-bom

        # XML project files
        [*.{csproj,vbproj,vcxproj,vcxproj.filters,proj,projitems,shproj}]
        indent_size = 2

        # XML config files
        [*.{props,targets,ruleset,config,nuspec,resx,vsixmanifest,vsct}]
        indent_size = 2

        # JSON files
        [*.json]
        indent_size = 2

        # Powershell files
        [*.ps1]
        indent_size = 2

        # Shell script files
        [*.sh]
        end_of_line = lf
        indent_size = 2

        # Dotnet code style settings:
        [*.{cs,vb}]

        # IDE0055: Fix formatting
        dotnet_diagnostic.IDE0055.severity = warning

        # Sort using and Import directives with System.* appearing first
        dotnet_sort_system_directives_first = true
        dotnet_separate_import_directive_groups = false
        # Avoid "this." and "Me." if not necessary
        dotnet_style_qualification_for_field = false:refactoring
        dotnet_style_qualification_for_property = false:refactoring
        dotnet_style_qualification_for_method = false:refactoring
        dotnet_style_qualification_for_event = false:refactoring

        # Use language keywords instead of framework type names for type references
        dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
        dotnet_style_predefined_type_for_member_access = true:suggestion

        # Suggest more modern language features when available
        dotnet_style_object_initializer = true:suggestion
        dotnet_style_collection_initializer = true:suggestion
        dotnet_style_coalesce_expression = true:suggestion
        dotnet_style_null_propagation = true:suggestion
        dotnet_style_explicit_tuple_names = true:suggestion

        # Whitespace options
        dotnet_style_allow_multiple_blank_lines_experimental = false

        # Non-private static fields are PascalCase
        dotnet_naming_rule.non_private_static_fields_should_be_pascal_case.severity = suggestion
        dotnet_naming_rule.non_private_static_fields_should_be_pascal_case.symbols = non_private_static_fields
        dotnet_naming_rule.non_private_static_fields_should_be_pascal_case.style = non_private_static_field_style

        dotnet_naming_symbols.non_private_static_fields.applicable_kinds = field
        dotnet_naming_symbols.non_private_static_fields.applicable_accessibilities = public, protected, internal, protected_internal, private_protected
        dotnet_naming_symbols.non_private_static_fields.required_modifiers = static

        dotnet_naming_style.non_private_static_field_style.capitalization = pascal_case

        # Non-private readonly fields are PascalCase
        dotnet_naming_rule.non_private_readonly_fields_should_be_pascal_case.severity = suggestion
        dotnet_naming_rule.non_private_readonly_fields_should_be_pascal_case.symbols = non_private_readonly_fields
        dotnet_naming_rule.non_private_readonly_fields_should_be_pascal_case.style = non_private_readonly_field_style

        dotnet_naming_symbols.non_private_readonly_fields.applicable_kinds = field
        dotnet_naming_symbols.non_private_readonly_fields.applicable_accessibilities = public, protected, internal, protected_internal, private_protected
        dotnet_naming_symbols.non_private_readonly_fields.required_modifiers = readonly

        dotnet_naming_style.non_private_readonly_field_style.capitalization = pascal_case

        # Constants are PascalCase
        dotnet_naming_rule.constants_should_be_pascal_case.severity = suggestion
        dotnet_naming_rule.constants_should_be_pascal_case.symbols = constants
        dotnet_naming_rule.constants_should_be_pascal_case.style = constant_style

        dotnet_naming_symbols.constants.applicable_kinds = field, local
        dotnet_naming_symbols.constants.required_modifiers = const

        dotnet_naming_style.constant_style.capitalization = pascal_case

        # Static fields are camelCase and start with s_
        dotnet_naming_rule.static_fields_should_be_camel_case.severity = suggestion
        dotnet_naming_rule.static_fields_should_be_camel_case.symbols = static_fields
        dotnet_naming_rule.static_fields_should_be_camel_case.style = static_field_style

        dotnet_naming_symbols.static_fields.applicable_kinds = field
        dotnet_naming_symbols.static_fields.required_modifiers = static

        dotnet_naming_style.static_field_style.capitalization = camel_case
        dotnet_naming_style.static_field_style.required_prefix = s_

        # Instance fields are camelCase and start with _
        dotnet_naming_rule.instance_fields_should_be_camel_case.severity = suggestion
        dotnet_naming_rule.instance_fields_should_be_camel_case.symbols = instance_fields
        dotnet_naming_rule.instance_fields_should_be_camel_case.style = instance_field_style

        dotnet_naming_symbols.instance_fields.applicable_kinds = field

        dotnet_naming_style.instance_field_style.capitalization = camel_case
        dotnet_naming_style.instance_field_style.required_prefix = _

        # Locals and parameters are camelCase
        dotnet_naming_rule.locals_should_be_camel_case.severity = suggestion
        dotnet_naming_rule.locals_should_be_camel_case.symbols = locals_and_parameters
        dotnet_naming_rule.locals_should_be_camel_case.style = camel_case_style

        dotnet_naming_symbols.locals_and_parameters.applicable_kinds = parameter, local

        dotnet_naming_style.camel_case_style.capitalization = camel_case

        # Local functions are PascalCase
        dotnet_naming_rule.local_functions_should_be_pascal_case.severity = suggestion
        dotnet_naming_rule.local_functions_should_be_pascal_case.symbols = local_functions
        dotnet_naming_rule.local_functions_should_be_pascal_case.style = local_function_style

        dotnet_naming_symbols.local_functions.applicable_kinds = local_function

        dotnet_naming_style.local_function_style.capitalization = pascal_case

        # By default, name items with PascalCase
        dotnet_naming_rule.members_should_be_pascal_case.severity = suggestion
        dotnet_naming_rule.members_should_be_pascal_case.symbols = all_members
        dotnet_naming_rule.members_should_be_pascal_case.style = pascal_case_style

        dotnet_naming_symbols.all_members.applicable_kinds = *

        dotnet_naming_style.pascal_case_style.capitalization = pascal_case

        # error RS2008: Enable analyzer release tracking for the analyzer project containing rule '{0}'
        dotnet_diagnostic.RS2008.severity = none

        # IDE0073: File header
        dotnet_diagnostic.IDE0073.severity = warning
        file_header_template = Licensed to the .NET Foundation under one or more agreements.\nThe.NET Foundation licenses this file to you under the MIT license.\nSee the LICENSE file in the project root for more information.

        # IDE0035: Remove unreachable code
        dotnet_diagnostic.IDE0035.severity = warning

        # IDE0036: Order modifiers
        dotnet_diagnostic.IDE0036.severity = warning

        # IDE0043: Format string contains invalid placeholder
        dotnet_diagnostic.IDE0043.severity = warning

        # IDE0044: Make field readonly
        dotnet_diagnostic.IDE0044.severity = warning

        # RS0016: Only enable if API files are present
        dotnet_public_api_analyzer.require_api_files = true

        # CSharp code style settings:
        [*.cs]
        # Newline settings
        csharp_new_line_before_open_brace = all
        csharp_new_line_before_else = true
        csharp_new_line_before_catch = true
        csharp_new_line_before_finally = true
        csharp_new_line_before_members_in_object_initializers = true
        csharp_new_line_before_members_in_anonymous_types = true
        csharp_new_line_between_query_expression_clauses = true

        # Indentation preferences
        csharp_indent_block_contents = true
        csharp_indent_braces = false
        csharp_indent_case_contents = true
        csharp_indent_case_contents_when_block = true
        csharp_indent_switch_labels = true
        csharp_indent_labels = flush_left

        # Whitespace options
        csharp_style_allow_embedded_statements_on_same_line_experimental = false
        csharp_style_allow_blank_lines_between_consecutive_braces_experimental = false
        csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental = false

        # Prefer "var" everywhere
        csharp_style_var_for_built_in_types = true:suggestion
        csharp_style_var_when_type_is_apparent = true:suggestion
        csharp_style_var_elsewhere = true:suggestion

        # Prefer method-like constructs to have a block body
        csharp_style_expression_bodied_methods = false:none
        csharp_style_expression_bodied_constructors = false:none
        csharp_style_expression_bodied_operators = false:none

        # Prefer property-like constructs to have an expression-body
        csharp_style_expression_bodied_properties = true:none
        csharp_style_expression_bodied_indexers = true:none
        csharp_style_expression_bodied_accessors = true:none

        # Suggest more modern language features when available
        csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
        csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
        csharp_style_inlined_variable_declaration = true:suggestion
        csharp_style_throw_expression = true:suggestion
        csharp_style_conditional_delegate_call = true:suggestion

        # Space preferences
        csharp_space_after_cast = false
        csharp_space_after_colon_in_inheritance_clause = true
        csharp_space_after_comma = true
        csharp_space_after_dot = false
        csharp_space_after_keywords_in_control_flow_statements = true
        csharp_space_after_semicolon_in_for_statement = true
        csharp_space_around_binary_operators = before_and_after
        csharp_space_around_declaration_statements = false
        csharp_space_before_colon_in_inheritance_clause = true
        csharp_space_before_comma = false
        csharp_space_before_dot = false
        csharp_space_before_open_square_brackets = false
        csharp_space_before_semicolon_in_for_statement = false
        csharp_space_between_empty_square_brackets = false
        csharp_space_between_method_call_empty_parameter_list_parentheses = false
        csharp_space_between_method_call_name_and_opening_parenthesis = false
        csharp_space_between_method_call_parameter_list_parentheses = false
        csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
        csharp_space_between_method_declaration_name_and_open_parenthesis = false
        csharp_space_between_method_declaration_parameter_list_parentheses = false
        csharp_space_between_parentheses = false
        csharp_space_between_square_brackets = false

        # Blocks are allowed
        csharp_prefer_braces = true:silent
        csharp_preserve_single_line_blocks = true
        csharp_preserve_single_line_statements = true

        # Currently only enabled for C# due to crash in VB analyzer.  VB can be enabled once
        # https://github.com/dotnet/roslyn/pull/54259 has been published.
        dotnet_style_allow_statement_immediately_after_block_experimental = false

        [src / CodeStyle/**.{cs,vb}]
        # warning RS0005: Do not use generic CodeAction.Create to create CodeAction
        dotnet_diagnostic.RS0005.severity = none

        [src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio}/**/*.{ cs,vb}]

        # IDE0011: Add braces
        csharp_prefer_braces = when_multiline:warning
        # NOTE: We need the below severity entry for Add Braces due to https://github.com/dotnet/roslyn/issues/44201
        dotnet_diagnostic.IDE0011.severity = warning

        # IDE0040: Add accessibility modifiers
        dotnet_diagnostic.IDE0040.severity = warning

        # CONSIDER: Are IDE0051 and IDE0052 too noisy to be warnings for IDE editing scenarios? Should they be made build-only warnings?
        # IDE0051: Remove unused private member
        dotnet_diagnostic.IDE0051.severity = warning

        # IDE0052: Remove unread private member
        dotnet_diagnostic.IDE0052.severity = warning

        # IDE0059: Unnecessary assignment to a value
        dotnet_diagnostic.IDE0059.severity = warning

        # IDE0060: Remove unused parameter
        dotnet_diagnostic.IDE0060.severity = warning

        # CA1012: Abstract types should not have public constructors
        dotnet_diagnostic.CA1012.severity = warning

        # CA1822: Make member static
        dotnet_diagnostic.CA1822.severity = warning

        # Prefer "var" everywhere
        dotnet_diagnostic.IDE0007.severity = warning
        csharp_style_var_for_built_in_types = true:warning
        csharp_style_var_when_type_is_apparent = true:warning
        csharp_style_var_elsewhere = true:warning

        # dotnet_style_allow_multiple_blank_lines_experimental
        dotnet_diagnostic.IDE2000.severity = warning

        # csharp_style_allow_embedded_statements_on_same_line_experimental
        dotnet_diagnostic.IDE2001.severity = warning

        # csharp_style_allow_blank_lines_between_consecutive_braces_experimental
        dotnet_diagnostic.IDE2002.severity = warning

        # dotnet_style_allow_statement_immediately_after_block_experimental
        dotnet_diagnostic.IDE2003.severity = warning

        # csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental
        dotnet_diagnostic.IDE2004.severity = warning

        [src /{ VisualStudio}/**/*.{cs,vb
        }]
        # CA1822: Make member static
        # Not enforced as a build 'warning' for 'VisualStudio' layer due to large number of false positives from https://github.com/dotnet/roslyn-analyzers/issues/3857 and https://github.com/dotnet/roslyn-analyzers/issues/3858
        # Additionally, there is a risk of accidentally breaking an internal API that partners rely on though IVT.
        dotnet_diagnostic.CA1822.severity = suggestion
        """;
}
