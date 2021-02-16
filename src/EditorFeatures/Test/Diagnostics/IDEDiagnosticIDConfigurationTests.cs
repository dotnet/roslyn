﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.ConfigureSeverityLevel
{
    [UseExportProvider]
    public class IDEDiagnosticIDConfigurationTests
    {
        private static ImmutableArray<(string diagnosticId, ImmutableHashSet<IOption2> codeStyleOptions)> GetIDEDiagnosticIdsAndOptions(
            string languageName)
        {
            const string diagnosticIdPrefix = "IDE";

            var diagnosticIdAndOptions = new List<(string diagnosticId, ImmutableHashSet<IOption2> options)>();
            var uniqueDiagnosticIds = new HashSet<string>();
            foreach (var assembly in MefHostServices.DefaultAssemblies)
            {
                var analyzerReference = new AnalyzerFileReference(assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile);
                foreach (var analyzer in analyzerReference.GetAnalyzers(languageName))
                {
                    foreach (var descriptor in analyzer.SupportedDiagnostics)
                    {
                        var diagnosticId = descriptor.Id;
                        ValidateHelpLinkForDiagnostic(diagnosticId, descriptor.HelpLinkUri);

                        if (!diagnosticId.StartsWith(diagnosticIdPrefix) ||
                            !int.TryParse(diagnosticId.Substring(startIndex: diagnosticIdPrefix.Length), out _))
                        {
                            // Ignore non-IDE diagnostic IDs (such as ENCxxxx diagnostics) and
                            // diagnostic IDs for suggestions, fading, etc. (such as IDExxxxWithSuggestion)
                            continue;
                        }

                        if (!IDEDiagnosticIdToOptionMappingHelper.TryGetMappedOptions(diagnosticId, languageName, out var options))
                        {
                            options = ImmutableHashSet<IOption2>.Empty;
                        }

                        if (uniqueDiagnosticIds.Add(diagnosticId))
                        {
                            diagnosticIdAndOptions.Add((diagnosticId, options));
                        }
                        else
                        {
                            Assert.True(diagnosticIdAndOptions.All(tuple => tuple.diagnosticId != diagnosticId || tuple.options.SetEquals(options)));
                        }
                    }
                }
            }

            diagnosticIdAndOptions.Sort();
            return diagnosticIdAndOptions.ToImmutableArray();
        }

        private static void ValidateHelpLinkForDiagnostic(string diagnosticId, string helpLinkUri)
        {
            if (diagnosticId is "IDE0043" // Intentionally undocumented because it's being removed in favor of CA2241
                or "IDE1007" or "IDE1008" or "RemoveUnnecessaryImportsFixable"
                or "RE0001") // Tracked by https://github.com/dotnet/roslyn/issues/48530
            {
                Assert.True(helpLinkUri == string.Empty, $"Expected empty help link for {diagnosticId}");
                return;
            }

            if (diagnosticId == "IDE0005_gen")
            {
                diagnosticId = "IDE0005";
            }

            if (helpLinkUri != $"https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/{diagnosticId.ToLowerInvariant()}")
            {
                Assert.True(false, $"Invalid help link for {diagnosticId}");
            }
        }

        private static Dictionary<string, string> GetExpectedMap(string expected, out string[] expectedLines)
        {
            expectedLines = expected.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(expectedLines.Length % 2 == 0);
            var expectedMap = new Dictionary<string, string>();
            for (var i = 0; i < expectedLines.Length; i += 2)
            {
                expectedMap.Add(expectedLines[i].Trim(), expectedLines[i + 1].Trim());
            }

            return expectedMap;
        }

        private static void VerifyConfigureSeverityCore(string expected, string languageName)
        {
            using var workspace = new TestWorkspace();
            var optionSet = workspace.Options;

            var diagnosticIdAndOptions = GetIDEDiagnosticIdsAndOptions(languageName);
            var expectedMap = GetExpectedMap(expected, out var expectedLines);

            var baseline = new StringBuilder();
            foreach (var (diagnosticId, options) in diagnosticIdAndOptions)
            {
                var editorConfigString = $"dotnet_diagnostic.{diagnosticId}.severity = %value%";

                // Verify we have an entry for diagnosticId
                var diagnosticIdString = $"# {diagnosticId}";

                if (expectedLines.Length == 0)
                {
                    // Executing test to generate baseline
                    baseline.AppendLine();
                    baseline.AppendLine(diagnosticIdString);
                    baseline.AppendLine(editorConfigString);
                    continue;
                }

                if (!expectedMap.TryGetValue(diagnosticIdString, out var expectedValue))
                {
                    Assert.False(true, $@"Missing entry:

{diagnosticIdString}
{editorConfigString}
");
                }

                // Verify entries match for diagnosticId
                if (expectedValue != editorConfigString)
                {
                    Assert.False(true, $@"Mismatch for '{diagnosticId}'
Expected: {expectedValue}
Actual: {editorConfigString}
");
                }

                expectedMap.Remove(diagnosticIdString);
            }

            if (expectedLines.Length == 0)
            {
                Assert.False(true, $"Test Baseline:{baseline}");
            }

            if (expectedMap.Count > 0)
            {
                var extraEntitiesBuilder = new StringBuilder();
                foreach (var kvp in expectedMap.OrderBy(kvp => kvp.Key))
                {
                    extraEntitiesBuilder.AppendLine();
                    extraEntitiesBuilder.AppendLine(kvp.Key);
                    extraEntitiesBuilder.AppendLine(kvp.Value);
                }

                Assert.False(true, $@"Unexpected entries:{extraEntitiesBuilder.ToString()}");
            }
        }

        [Fact]
        public void CSharp_VerifyIDEDiagnosticSeveritiesAreConfigurable()
        {
            var expected = @"
# IDE0001
dotnet_diagnostic.IDE0001.severity = %value%

# IDE0002
dotnet_diagnostic.IDE0002.severity = %value%

# IDE0003
dotnet_diagnostic.IDE0003.severity = %value%

# IDE0004
dotnet_diagnostic.IDE0004.severity = %value%

# IDE0005
dotnet_diagnostic.IDE0005.severity = %value%

# IDE0007
dotnet_diagnostic.IDE0007.severity = %value%

# IDE0008
dotnet_diagnostic.IDE0008.severity = %value%

# IDE0009
dotnet_diagnostic.IDE0009.severity = %value%

# IDE0010
dotnet_diagnostic.IDE0010.severity = %value%

# IDE0011
dotnet_diagnostic.IDE0011.severity = %value%

# IDE0016
dotnet_diagnostic.IDE0016.severity = %value%

# IDE0017
dotnet_diagnostic.IDE0017.severity = %value%

# IDE0018
dotnet_diagnostic.IDE0018.severity = %value%

# IDE0019
dotnet_diagnostic.IDE0019.severity = %value%

# IDE0020
dotnet_diagnostic.IDE0020.severity = %value%

# IDE0021
dotnet_diagnostic.IDE0021.severity = %value%

# IDE0022
dotnet_diagnostic.IDE0022.severity = %value%

# IDE0023
dotnet_diagnostic.IDE0023.severity = %value%

# IDE0024
dotnet_diagnostic.IDE0024.severity = %value%

# IDE0025
dotnet_diagnostic.IDE0025.severity = %value%

# IDE0026
dotnet_diagnostic.IDE0026.severity = %value%

# IDE0027
dotnet_diagnostic.IDE0027.severity = %value%

# IDE0028
dotnet_diagnostic.IDE0028.severity = %value%

# IDE0029
dotnet_diagnostic.IDE0029.severity = %value%

# IDE0030
dotnet_diagnostic.IDE0030.severity = %value%

# IDE0031
dotnet_diagnostic.IDE0031.severity = %value%

# IDE0032
dotnet_diagnostic.IDE0032.severity = %value%

# IDE0033
dotnet_diagnostic.IDE0033.severity = %value%

# IDE0034
dotnet_diagnostic.IDE0034.severity = %value%

# IDE0035
dotnet_diagnostic.IDE0035.severity = %value%

# IDE0036
dotnet_diagnostic.IDE0036.severity = %value%

# IDE0037
dotnet_diagnostic.IDE0037.severity = %value%

# IDE0038
dotnet_diagnostic.IDE0038.severity = %value%

# IDE0039
dotnet_diagnostic.IDE0039.severity = %value%

# IDE0040
dotnet_diagnostic.IDE0040.severity = %value%

# IDE0041
dotnet_diagnostic.IDE0041.severity = %value%

# IDE0042
dotnet_diagnostic.IDE0042.severity = %value%

# IDE0043
dotnet_diagnostic.IDE0043.severity = %value%

# IDE0044
dotnet_diagnostic.IDE0044.severity = %value%

# IDE0045
dotnet_diagnostic.IDE0045.severity = %value%

# IDE0046
dotnet_diagnostic.IDE0046.severity = %value%

# IDE0047
dotnet_diagnostic.IDE0047.severity = %value%

# IDE0048
dotnet_diagnostic.IDE0048.severity = %value%

# IDE0049
dotnet_diagnostic.IDE0049.severity = %value%

# IDE0050
dotnet_diagnostic.IDE0050.severity = %value%

# IDE0051
dotnet_diagnostic.IDE0051.severity = %value%

# IDE0052
dotnet_diagnostic.IDE0052.severity = %value%

# IDE0053
dotnet_diagnostic.IDE0053.severity = %value%

# IDE0054
dotnet_diagnostic.IDE0054.severity = %value%

# IDE0055
dotnet_diagnostic.IDE0055.severity = %value%

# IDE0056
dotnet_diagnostic.IDE0056.severity = %value%

# IDE0057
dotnet_diagnostic.IDE0057.severity = %value%

# IDE0058
dotnet_diagnostic.IDE0058.severity = %value%

# IDE0059
dotnet_diagnostic.IDE0059.severity = %value%

# IDE0060
dotnet_diagnostic.IDE0060.severity = %value%

# IDE0061
dotnet_diagnostic.IDE0061.severity = %value%

# IDE0062
dotnet_diagnostic.IDE0062.severity = %value%

# IDE0063
dotnet_diagnostic.IDE0063.severity = %value%

# IDE0064
dotnet_diagnostic.IDE0064.severity = %value%

# IDE0065
dotnet_diagnostic.IDE0065.severity = %value%

# IDE0066
dotnet_diagnostic.IDE0066.severity = %value%

# IDE0070
dotnet_diagnostic.IDE0070.severity = %value%

# IDE0071
dotnet_diagnostic.IDE0071.severity = %value%

# IDE0072
dotnet_diagnostic.IDE0072.severity = %value%

# IDE0073
dotnet_diagnostic.IDE0073.severity = %value%

# IDE0074
dotnet_diagnostic.IDE0074.severity = %value%

# IDE0075
dotnet_diagnostic.IDE0075.severity = %value%

# IDE0076
dotnet_diagnostic.IDE0076.severity = %value%

# IDE0077
dotnet_diagnostic.IDE0077.severity = %value%

# IDE0078
dotnet_diagnostic.IDE0078.severity = %value%

# IDE0079
dotnet_diagnostic.IDE0079.severity = %value%

# IDE0080
dotnet_diagnostic.IDE0080.severity = %value%

# IDE0082
dotnet_diagnostic.IDE0082.severity = %value%

# IDE0083
dotnet_diagnostic.IDE0083.severity = %value%

# IDE0090
dotnet_diagnostic.IDE0090.severity = %value%

# IDE0110
dotnet_diagnostic.IDE0110.severity = %value%

# IDE0100
dotnet_diagnostic.IDE0100.severity = %value%

# IDE1005
dotnet_diagnostic.IDE1005.severity = %value%

# IDE1006
dotnet_diagnostic.IDE1006.severity = %value%

# IDE1007
dotnet_diagnostic.IDE1007.severity = %value%

# IDE1008
dotnet_diagnostic.IDE1008.severity = %value%

# IDE0120
dotnet_diagnostic.IDE0120.severity = %value%

# IDE0130
dotnet_diagnostic.IDE0130.severity = %value%

# IDE2000
dotnet_diagnostic.IDE2000.severity = %value%

# IDE2001
dotnet_diagnostic.IDE2001.severity = %value%

# IDE2002
dotnet_diagnostic.IDE2002.severity = %value%

# IDE2003
dotnet_diagnostic.IDE2003.severity = %value%

# IDE2004
dotnet_diagnostic.IDE2004.severity = %value%
";

            VerifyConfigureSeverityCore(expected, LanguageNames.CSharp);
        }

        [Fact]
        public void VisualBasic_VerifyIDEDiagnosticSeveritiesAreConfigurable()
        {
            var expected = @"
# IDE0001
dotnet_diagnostic.IDE0001.severity = %value%

# IDE0002
dotnet_diagnostic.IDE0002.severity = %value%

# IDE0003
dotnet_diagnostic.IDE0003.severity = %value%

# IDE0004
dotnet_diagnostic.IDE0004.severity = %value%

# IDE0005
dotnet_diagnostic.IDE0005.severity = %value%

# IDE0009
dotnet_diagnostic.IDE0009.severity = %value%

# IDE0010
dotnet_diagnostic.IDE0010.severity = %value%

# IDE0017
dotnet_diagnostic.IDE0017.severity = %value%

# IDE0028
dotnet_diagnostic.IDE0028.severity = %value%

# IDE0029
dotnet_diagnostic.IDE0029.severity = %value%

# IDE0030
dotnet_diagnostic.IDE0030.severity = %value%

# IDE0031
dotnet_diagnostic.IDE0031.severity = %value%

# IDE0032
dotnet_diagnostic.IDE0032.severity = %value%

# IDE0033
dotnet_diagnostic.IDE0033.severity = %value%

# IDE0036
dotnet_diagnostic.IDE0036.severity = %value%

# IDE0037
dotnet_diagnostic.IDE0037.severity = %value%

# IDE0040
dotnet_diagnostic.IDE0040.severity = %value%

# IDE0041
dotnet_diagnostic.IDE0041.severity = %value%

# IDE0043
dotnet_diagnostic.IDE0043.severity = %value%

# IDE0044
dotnet_diagnostic.IDE0044.severity = %value%

# IDE0045
dotnet_diagnostic.IDE0045.severity = %value%

# IDE0046
dotnet_diagnostic.IDE0046.severity = %value%

# IDE0047
dotnet_diagnostic.IDE0047.severity = %value%

# IDE0048
dotnet_diagnostic.IDE0048.severity = %value%

# IDE0049
dotnet_diagnostic.IDE0049.severity = %value%

# IDE0050
dotnet_diagnostic.IDE0050.severity = %value%

# IDE0051
dotnet_diagnostic.IDE0051.severity = %value%

# IDE0052
dotnet_diagnostic.IDE0052.severity = %value%

# IDE0054
dotnet_diagnostic.IDE0054.severity = %value%

# IDE0055
dotnet_diagnostic.IDE0055.severity = %value%

# IDE0058
dotnet_diagnostic.IDE0058.severity = %value%

# IDE0059
dotnet_diagnostic.IDE0059.severity = %value%

# IDE0060
dotnet_diagnostic.IDE0060.severity = %value%

# IDE0070
dotnet_diagnostic.IDE0070.severity = %value%

# IDE0071
dotnet_diagnostic.IDE0071.severity = %value%

# IDE0073
dotnet_diagnostic.IDE0073.severity = %value%

# IDE0075
dotnet_diagnostic.IDE0075.severity = %value%

# IDE0076
dotnet_diagnostic.IDE0076.severity = %value%

# IDE0077
dotnet_diagnostic.IDE0077.severity = %value%

# IDE0079
dotnet_diagnostic.IDE0079.severity = %value%

# IDE0081
dotnet_diagnostic.IDE0081.severity = %value%

# IDE0082
dotnet_diagnostic.IDE0082.severity = %value%

# IDE0084
dotnet_diagnostic.IDE0084.severity = %value%

# IDE0100
dotnet_diagnostic.IDE0100.severity = %value%

# IDE1006
dotnet_diagnostic.IDE1006.severity = %value%

# IDE1007
dotnet_diagnostic.IDE1007.severity = %value%

# IDE1008
dotnet_diagnostic.IDE1008.severity = %value%

# IDE0120
dotnet_diagnostic.IDE0120.severity = %value%

# IDE2000
dotnet_diagnostic.IDE2000.severity = %value%

# IDE2003
dotnet_diagnostic.IDE2003.severity = %value%
";
            VerifyConfigureSeverityCore(expected, LanguageNames.VisualBasic);
        }

        private static void VerifyConfigureCodeStyleOptionsCore(string expected, string languageName)
        {
            using var workspace = new TestWorkspace();
            var optionSet = workspace.Options;

            var diagnosticIdAndOptions = GetIDEDiagnosticIdsAndOptions(languageName);
            var expectedMap = GetExpectedMap(expected, out var expectedLines);

            var baseline = new StringBuilder();
            foreach (var (diagnosticId, options) in diagnosticIdAndOptions)
            {
                var hasEditorConfigCodeStyleOptions = false;
                foreach (var option in options.OrderBy(o => o.Name))
                {
                    var editorConfigLocation = option.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();
                    if (editorConfigLocation == null)
                    {
                        continue;
                    }

                    var optionKey = new OptionKey(option, option.IsPerLanguage ? languageName : null);
                    var value = optionSet.GetOption(optionKey);
                    var editorConfigString = editorConfigLocation.GetEditorConfigString(value, optionSet);

                    ProcessDiagnosticIdAndOption(diagnosticId, option, editorConfigString);
                    hasEditorConfigCodeStyleOptions = true;
                }

                if (!hasEditorConfigCodeStyleOptions)
                {
                    ProcessDiagnosticIdAndOption(diagnosticId, optionOpt: null, editorConfigString: "No editorconfig based code style option");
                }
            }

            if (expectedLines.Length == 0)
            {
                Assert.False(true, $"Test Baseline:{baseline}");
            }

            if (expectedMap.Count > 0)
            {
                var extraEntitiesBuilder = new StringBuilder();
                foreach (var kvp in expectedMap.OrderBy(kvp => kvp.Key))
                {
                    extraEntitiesBuilder.AppendLine();
                    extraEntitiesBuilder.AppendLine(kvp.Key);
                    extraEntitiesBuilder.AppendLine(kvp.Value);
                }

                Assert.False(true, $@"Unexpected entries:{extraEntitiesBuilder.ToString()}");
            }

            return;

            // Local functions
            void ProcessDiagnosticIdAndOption(string diagnosticId, IOption optionOpt, string editorConfigString)
            {
                // Verify we have an entry for { diagnosticId, optionName }
                var diagnosticIdString = $"# {diagnosticId}";
                if (optionOpt != null)
                {
                    diagnosticIdString += $", {optionOpt.Name}";
                }

                if (expectedLines.Length == 0)
                {
                    // Executing test to generate baseline
                    baseline.AppendLine();
                    baseline.AppendLine(diagnosticIdString);
                    baseline.AppendLine(editorConfigString);
                    return;
                }

                if (!expectedMap.TryGetValue(diagnosticIdString, out var expectedValue))
                {
                    Assert.False(true, $@"Missing entry:

{diagnosticIdString}
{editorConfigString}
");
                }

                // Verify entries match for diagnosticId
                if (expectedValue != editorConfigString)
                {
                    Assert.False(true, $@"Mismatch for '{diagnosticId}'
Expected: {expectedValue}
Actual: {editorConfigString}
");
                }

                expectedMap.Remove(diagnosticIdString);
            }
        }

        [Fact]
        public void CSharp_VerifyIDECodeStyleOptionsAreConfigurable()
        {
            var expected = @"
# IDE0001
No editorconfig based code style option

# IDE0002
No editorconfig based code style option

# IDE0003, QualifyEventAccess
dotnet_style_qualification_for_event = false

# IDE0003, QualifyFieldAccess
dotnet_style_qualification_for_field = false

# IDE0003, QualifyMethodAccess
dotnet_style_qualification_for_method = false

# IDE0003, QualifyPropertyAccess
dotnet_style_qualification_for_property = false

# IDE0004
No editorconfig based code style option

# IDE0005
No editorconfig based code style option

# IDE0007, VarElsewhere
csharp_style_var_elsewhere = false

# IDE0007, VarForBuiltInTypes
csharp_style_var_for_built_in_types = false

# IDE0007, VarWhenTypeIsApparent
csharp_style_var_when_type_is_apparent = false

# IDE0008, VarElsewhere
csharp_style_var_elsewhere = false

# IDE0008, VarForBuiltInTypes
csharp_style_var_for_built_in_types = false

# IDE0008, VarWhenTypeIsApparent
csharp_style_var_when_type_is_apparent = false

# IDE0009, QualifyEventAccess
dotnet_style_qualification_for_event = false

# IDE0009, QualifyFieldAccess
dotnet_style_qualification_for_field = false

# IDE0009, QualifyMethodAccess
dotnet_style_qualification_for_method = false

# IDE0009, QualifyPropertyAccess
dotnet_style_qualification_for_property = false

# IDE0010
No editorconfig based code style option

# IDE0011, PreferBraces
csharp_prefer_braces = true

# IDE0016, PreferThrowExpression
csharp_style_throw_expression = true

# IDE0017, PreferObjectInitializer
dotnet_style_object_initializer = true

# IDE0018, PreferInlinedVariableDeclaration
csharp_style_inlined_variable_declaration = true

# IDE0019, PreferPatternMatchingOverAsWithNullCheck
csharp_style_pattern_matching_over_as_with_null_check = true

# IDE0020, PreferPatternMatchingOverIsWithCastCheck
csharp_style_pattern_matching_over_is_with_cast_check = true

# IDE0021, PreferExpressionBodiedConstructors
csharp_style_expression_bodied_constructors = false

# IDE0022, PreferExpressionBodiedMethods
csharp_style_expression_bodied_methods = false

# IDE0023, PreferExpressionBodiedOperators
csharp_style_expression_bodied_operators = false

# IDE0024, PreferExpressionBodiedOperators
csharp_style_expression_bodied_operators = false

# IDE0025, PreferExpressionBodiedProperties
csharp_style_expression_bodied_properties = true

# IDE0026, PreferExpressionBodiedIndexers
csharp_style_expression_bodied_indexers = true

# IDE0027, PreferExpressionBodiedAccessors
csharp_style_expression_bodied_accessors = true

# IDE0028, PreferCollectionInitializer
dotnet_style_collection_initializer = true

# IDE0029, PreferCoalesceExpression
dotnet_style_coalesce_expression = true

# IDE0030, PreferCoalesceExpression
dotnet_style_coalesce_expression = true

# IDE0031, PreferNullPropagation
dotnet_style_null_propagation = true

# IDE0032, PreferAutoProperties
dotnet_style_prefer_auto_properties = true

# IDE0033, PreferExplicitTupleNames
dotnet_style_explicit_tuple_names = true

# IDE0034, PreferSimpleDefaultExpression
csharp_prefer_simple_default_expression = true

# IDE0035
No editorconfig based code style option

# IDE0036, PreferredModifierOrder
csharp_preferred_modifier_order = public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async

# IDE0037, PreferInferredTupleNames
dotnet_style_prefer_inferred_tuple_names = true

# IDE0037, PreferInferredAnonymousTypeMemberNames
dotnet_style_prefer_inferred_anonymous_type_member_names = true

# IDE0038, PreferPatternMatchingOverIsWithCastCheck
csharp_style_pattern_matching_over_is_with_cast_check = true

# IDE0039, PreferLocalOverAnonymousFunction
csharp_style_pattern_local_over_anonymous_function = true

# IDE0040, RequireAccessibilityModifiers
dotnet_style_require_accessibility_modifiers = for_non_interface_members

# IDE0041, PreferIsNullCheckOverReferenceEqualityMethod
dotnet_style_prefer_is_null_check_over_reference_equality_method = true

# IDE0042, PreferDeconstructedVariableDeclaration
csharp_style_deconstructed_variable_declaration = true

# IDE0043
No editorconfig based code style option

# IDE0044, PreferReadonly
dotnet_style_readonly_field = true

# IDE0045, PreferConditionalExpressionOverAssignment
dotnet_style_prefer_conditional_expression_over_assignment = true

# IDE0046, PreferConditionalExpressionOverReturn
dotnet_style_prefer_conditional_expression_over_return = true

# IDE0047, ArithmeticBinaryParentheses
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity

# IDE0047, OtherBinaryParentheses
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity

# IDE0047, OtherParentheses
dotnet_style_parentheses_in_other_operators = never_if_unnecessary

# IDE0047, RelationalBinaryParentheses
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity

# IDE0048, ArithmeticBinaryParentheses
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity

# IDE0048, OtherBinaryParentheses
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity

# IDE0048, OtherParentheses
dotnet_style_parentheses_in_other_operators = never_if_unnecessary

# IDE0048, RelationalBinaryParentheses
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity

# IDE0049, PreferIntrinsicPredefinedTypeKeywordInDeclaration
dotnet_style_predefined_type_for_locals_parameters_members = true

# IDE0049, PreferIntrinsicPredefinedTypeKeywordInMemberAccess
dotnet_style_predefined_type_for_member_access = true

# IDE0050
No editorconfig based code style option

# IDE0051
No editorconfig based code style option

# IDE0052
No editorconfig based code style option

# IDE0053, PreferExpressionBodiedLambdas
csharp_style_expression_bodied_lambdas = true

# IDE0054, PreferCompoundAssignment
dotnet_style_prefer_compound_assignment = true

# IDE0055
No editorconfig based code style option

# IDE0056, PreferIndexOperator
csharp_style_prefer_index_operator = true

# IDE0057, PreferRangeOperator
csharp_style_prefer_range_operator = true

# IDE0058, UnusedValueExpressionStatement
csharp_style_unused_value_expression_statement_preference = discard_variable

# IDE0059, UnusedValueAssignment
csharp_style_unused_value_assignment_preference = discard_variable

# IDE0060, UnusedParameters
dotnet_code_quality_unused_parameters = all

# IDE0061, PreferExpressionBodiedLocalFunctions
csharp_style_expression_bodied_local_functions = false

# IDE0062, PreferStaticLocalFunction
csharp_prefer_static_local_function = true

# IDE0063, PreferSimpleUsingStatement
csharp_prefer_simple_using_statement = true

# IDE0064
No editorconfig based code style option

# IDE0065, PreferredUsingDirectivePlacement
csharp_using_directive_placement = outside_namespace

# IDE0066, PreferSwitchExpression
csharp_style_prefer_switch_expression = true

# IDE0070
No editorconfig based code style option

# IDE0071, PreferSimplifiedInterpolation
dotnet_style_prefer_simplified_interpolation = true

# IDE0072
No editorconfig based code style option

# IDE0073, FileHeaderTemplate
file_header_template = unset

# IDE0074, PreferCompoundAssignment
dotnet_style_prefer_compound_assignment = true

# IDE0075, PreferSimplifiedBooleanExpressions
dotnet_style_prefer_simplified_boolean_expressions = true

# IDE0076
No editorconfig based code style option

# IDE0077
No editorconfig based code style option

# IDE0078, PreferPatternMatching
csharp_style_prefer_pattern_matching = true

# IDE0079
No editorconfig based code style option

# IDE0080
No editorconfig based code style option

# IDE0082
No editorconfig based code style option

# IDE0083, PreferNotPattern
csharp_style_prefer_not_pattern = true

# IDE0090, ImplicitObjectCreationWhenTypeIsApparent
csharp_style_implicit_object_creation_when_type_is_apparent = true

# IDE0100
No editorconfig based code style option

# IDE0110
No editorconfig based code style option

# IDE0120
No editorconfig based code style option

# IDE0130, PreferNamespaceAndFolderMatchStructure
dotnet_style_namespace_match_folder = true

# IDE1005, PreferConditionalDelegateCall
csharp_style_conditional_delegate_call = true

# IDE1006
No editorconfig based code style option

# IDE1007
No editorconfig based code style option

# IDE1008
No editorconfig based code style option

# IDE2000, AllowMultipleBlankLines
dotnet_style_allow_multiple_blank_lines_experimental = true

# IDE2001, AllowEmbeddedStatementsOnSameLine
csharp_style_allow_embedded_statements_on_same_line_experimental = true

# IDE2002, AllowBlankLinesBetweenConsecutiveBraces
csharp_style_allow_blank_lines_between_consecutive_braces_experimental = true

# IDE2003, AllowStatementImmediatelyAfterBlock
dotnet_style_allow_statement_immediately_after_block_experimental = true

# IDE2004, AllowBlankLineAfterColonInConstructorInitializer
csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental = true
";

            VerifyConfigureCodeStyleOptionsCore(expected, LanguageNames.CSharp);
        }

        [Fact]
        public void VisualBasic_VerifyIDECodeStyleOptionsAreConfigurable()
        {
            var expected = @"
# IDE0001
No editorconfig based code style option

# IDE0002
No editorconfig based code style option

# IDE0003, QualifyEventAccess
dotnet_style_qualification_for_event = false

# IDE0003, QualifyFieldAccess
dotnet_style_qualification_for_field = false

# IDE0003, QualifyMethodAccess
dotnet_style_qualification_for_method = false

# IDE0003, QualifyPropertyAccess
dotnet_style_qualification_for_property = false

# IDE0004
No editorconfig based code style option

# IDE0005
No editorconfig based code style option

# IDE0009, QualifyEventAccess
dotnet_style_qualification_for_event = false

# IDE0009, QualifyFieldAccess
dotnet_style_qualification_for_field = false

# IDE0009, QualifyMethodAccess
dotnet_style_qualification_for_method = false

# IDE0009, QualifyPropertyAccess
dotnet_style_qualification_for_property = false

# IDE0010
No editorconfig based code style option

# IDE0017, PreferObjectInitializer
dotnet_style_object_initializer = true

# IDE0028, PreferCollectionInitializer
dotnet_style_collection_initializer = true

# IDE0029, PreferCoalesceExpression
dotnet_style_coalesce_expression = true

# IDE0030, PreferCoalesceExpression
dotnet_style_coalesce_expression = true

# IDE0031, PreferNullPropagation
dotnet_style_null_propagation = true

# IDE0032, PreferAutoProperties
dotnet_style_prefer_auto_properties = true

# IDE0033, PreferExplicitTupleNames
dotnet_style_explicit_tuple_names = true

# IDE0036, PreferredModifierOrder
visual_basic_preferred_modifier_order = partial,default,private,protected,public,friend,notoverridable,overridable,mustoverride,overloads,overrides,mustinherit,notinheritable,static,shared,shadows,readonly,writeonly,dim,const,withevents,widening,narrowing,custom,async,iterator

# IDE0037, PreferInferredTupleNames
dotnet_style_prefer_inferred_tuple_names = true

# IDE0037, PreferInferredAnonymousTypeMemberNames
dotnet_style_prefer_inferred_anonymous_type_member_names = true

# IDE0040, RequireAccessibilityModifiers
dotnet_style_require_accessibility_modifiers = for_non_interface_members

# IDE0041, PreferIsNullCheckOverReferenceEqualityMethod
dotnet_style_prefer_is_null_check_over_reference_equality_method = true

# IDE0043
No editorconfig based code style option

# IDE0044, PreferReadonly
dotnet_style_readonly_field = true

# IDE0045, PreferConditionalExpressionOverAssignment
dotnet_style_prefer_conditional_expression_over_assignment = true

# IDE0046, PreferConditionalExpressionOverReturn
dotnet_style_prefer_conditional_expression_over_return = true

# IDE0047, ArithmeticBinaryParentheses
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity

# IDE0047, OtherBinaryParentheses
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity

# IDE0047, OtherParentheses
dotnet_style_parentheses_in_other_operators = never_if_unnecessary

# IDE0047, RelationalBinaryParentheses
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity

# IDE0048, ArithmeticBinaryParentheses
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity

# IDE0048, OtherBinaryParentheses
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity

# IDE0048, OtherParentheses
dotnet_style_parentheses_in_other_operators = never_if_unnecessary

# IDE0048, RelationalBinaryParentheses
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity

# IDE0049, PreferIntrinsicPredefinedTypeKeywordInDeclaration
dotnet_style_predefined_type_for_locals_parameters_members = true

# IDE0049, PreferIntrinsicPredefinedTypeKeywordInMemberAccess
dotnet_style_predefined_type_for_member_access = true

# IDE0050
No editorconfig based code style option

# IDE0051
No editorconfig based code style option

# IDE0052
No editorconfig based code style option

# IDE0054, PreferCompoundAssignment
dotnet_style_prefer_compound_assignment = true

# IDE0055
No editorconfig based code style option

# IDE0058, UnusedValueExpressionStatement
visual_basic_style_unused_value_expression_statement_preference = unused_local_variable

# IDE0059, UnusedValueAssignment
visual_basic_style_unused_value_assignment_preference = unused_local_variable

# IDE0060, UnusedParameters
dotnet_code_quality_unused_parameters = all

# IDE0070
No editorconfig based code style option

# IDE0071, PreferSimplifiedInterpolation
dotnet_style_prefer_simplified_interpolation = true

# IDE0073, FileHeaderTemplate
file_header_template = unset

# IDE0075, PreferSimplifiedBooleanExpressions
dotnet_style_prefer_simplified_boolean_expressions = true

# IDE0076
No editorconfig based code style option

# IDE0077
No editorconfig based code style option

# IDE0079
No editorconfig based code style option

# IDE0081
No editorconfig based code style option

# IDE0082
No editorconfig based code style option

# IDE0084, PreferIsNotExpression
visual_basic_style_prefer_isnot_expression = true

# IDE0100
No editorconfig based code style option

# IDE1006
No editorconfig based code style option

# IDE1007
No editorconfig based code style option

# IDE1008
No editorconfig based code style option

# IDE0120
No editorconfig based code style option

# IDE2000, AllowMultipleBlankLines
dotnet_style_allow_multiple_blank_lines_experimental = true

# IDE2003, AllowStatementImmediatelyAfterBlock
dotnet_style_allow_statement_immediately_after_block_experimental = true
";

            VerifyConfigureCodeStyleOptionsCore(expected, LanguageNames.VisualBasic);
        }
    }
}
