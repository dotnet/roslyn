using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Preview.TestOnly_CompilerDiagnosticAnalyzerProviderService;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.ConfigureSeverityLevel
{
    [UseExportProvider]
    public class IDEDiagnosticIDConfigurationTests
    {
        // Regular expression for .editorconfig code style option entry.
        // For example: "dotnet_style_object_initializer = true:suggestion   # Optional comment"
        private static readonly Regex s_optionBasedEntryPattern = new Regex(@"([\w ]+)=([\w ]+):[ ]*([\w]+)([ ]*[;#].*)?");

        private static ImmutableArray<(string diagnosticId, ImmutableHashSet<IOption> codeStyleOptions)> GetIDEDiagnosticIdsAndOptions(
            string languageName)
        {
            const string diagnosticIdPrefix = "IDE";

            var diagnosticIdAndOptions = new List<(string diagnosticId, ImmutableHashSet<IOption> options)>();
            var uniqueDiagnosticIds = new HashSet<string>();
            foreach (var assembly in MefHostServices.DefaultAssemblies)
            {
                var analyzerReference = new AnalyzerFileReference(assembly.Location, FromFileLoader.Instance);
                foreach (var analyzer in analyzerReference.GetAnalyzers(languageName))
                {
                    foreach (var descriptor in analyzer.SupportedDiagnostics)
                    {
                        var diagnosticId = descriptor.Id;

                        if (!diagnosticId.StartsWith(diagnosticIdPrefix) ||
                            !int.TryParse(diagnosticId.Substring(startIndex: diagnosticIdPrefix.Length), out _))
                        {
                            // Ignore non-IDE diagnostic IDs (such as ENCxxxx diagnostics) and
                            // diagnostic IDs for suggestions, fading, etc. (such as IDExxxxWithSuggestion)
                            continue;
                        }

                        if (!IDEDiagnosticIdToOptionMappingHelper.TryGetMappedOptions(diagnosticId, languageName, out var options))
                        {
                            options = ImmutableHashSet<IOption>.Empty;
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

        private static Dictionary<string, string> GetExpectedMap(string expected, out string[] expectedLines)
        {
            expectedLines = expected.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(expectedLines.Length % 2 == 0);
            var expectedMap = new Dictionary<string, string>();
            for (int i = 0; i < expectedLines.Length; i += 2)
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
                var optionOpt = options.Count == 1 ? options.Single() : null;
                var editorConfigLocation = optionOpt?.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();

                var editorConfigString = $"dotnet_diagnostic.{diagnosticId}.severity = %value%";
                if (editorConfigLocation != null)
                {
                    var optionKey = new OptionKey(optionOpt, optionOpt.IsPerLanguage ? languageName : null);
                    var value = optionSet.GetOption(optionKey);
                    var codeStyleEditorConfigString = editorConfigLocation.GetEditorConfigString(value, optionSet);
                    if (s_optionBasedEntryPattern.IsMatch(codeStyleEditorConfigString))
                    {
                        editorConfigString = codeStyleEditorConfigString;
                    }
                }

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
csharp_prefer_braces = true:silent

# IDE0016
csharp_style_throw_expression = true:suggestion

# IDE0017
dotnet_style_object_initializer = true:suggestion

# IDE0018
csharp_style_inlined_variable_declaration = true:suggestion

# IDE0019
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion

# IDE0020
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion

# IDE0021
csharp_style_expression_bodied_constructors = false:silent

# IDE0022
csharp_style_expression_bodied_methods = false:silent

# IDE0023
csharp_style_expression_bodied_operators = false:silent

# IDE0024
csharp_style_expression_bodied_operators = false:silent

# IDE0025
csharp_style_expression_bodied_properties = true:silent

# IDE0026
csharp_style_expression_bodied_indexers = true:silent

# IDE0027
csharp_style_expression_bodied_accessors = true:silent

# IDE0028
dotnet_style_collection_initializer = true:suggestion

# IDE0029
dotnet_style_coalesce_expression = true:suggestion

# IDE0030
dotnet_style_coalesce_expression = true:suggestion

# IDE0031
dotnet_style_null_propagation = true:suggestion

# IDE0032
dotnet_style_prefer_auto_properties = true:silent

# IDE0033
dotnet_style_explicit_tuple_names = true:suggestion

# IDE0034
csharp_prefer_simple_default_expression = true:suggestion

# IDE0035
dotnet_diagnostic.IDE0035.severity = %value%

# IDE0036
dotnet_diagnostic.IDE0036.severity = %value%

# IDE0037
dotnet_style_prefer_inferred_tuple_names = true:suggestion

# IDE0039
csharp_style_pattern_local_over_anonymous_function = true:suggestion

# IDE0040
dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent

# IDE0041
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion

# IDE0042
csharp_style_deconstructed_variable_declaration = true:suggestion

# IDE0043
dotnet_diagnostic.IDE0043.severity = %value%

# IDE0044
dotnet_style_readonly_field = true:suggestion

# IDE0045
dotnet_style_prefer_conditional_expression_over_assignment = true:silent

# IDE0046
dotnet_style_prefer_conditional_expression_over_return = true:silent

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
csharp_style_expression_bodied_lambdas = true:silent

# IDE0054
dotnet_style_prefer_compound_assignment = true:suggestion

# IDE0055
dotnet_diagnostic.IDE0055.severity = %value%

# IDE0056
csharp_style_prefer_index_operator = true:suggestion

# IDE0057
csharp_style_prefer_range_operator = true:suggestion

# IDE0058
csharp_style_unused_value_expression_statement_preference = discard_variable:silent

# IDE0059
csharp_style_unused_value_assignment_preference = discard_variable:suggestion

# IDE0060
dotnet_code_quality_unused_parameters = all:suggestion

# IDE0061
csharp_style_expression_bodied_local_functions = false:silent

# IDE0062
csharp_prefer_static_local_function = true:suggestion

# IDE0063
csharp_prefer_simple_using_statement = true:suggestion

# IDE0064
dotnet_diagnostic.IDE0064.severity = %value%

# IDE0065
csharp_using_directive_placement = outside_namespace:silent

# IDE0066
csharp_style_prefer_switch_expression = true:suggestion

# IDE0067
dotnet_diagnostic.IDE0067.severity = %value%

# IDE0068
dotnet_diagnostic.IDE0068.severity = %value%

# IDE0069
dotnet_diagnostic.IDE0069.severity = %value%

# IDE1005
csharp_style_conditional_delegate_call = true:suggestion

# IDE1006
dotnet_diagnostic.IDE1006.severity = %value%

# IDE1007
dotnet_diagnostic.IDE1007.severity = %value%

# IDE1008
dotnet_diagnostic.IDE1008.severity = %value%
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
dotnet_style_object_initializer = true:suggestion

# IDE0028
dotnet_style_collection_initializer = true:suggestion

# IDE0029
dotnet_style_coalesce_expression = true:suggestion

# IDE0030
dotnet_style_coalesce_expression = true:suggestion

# IDE0031
dotnet_style_null_propagation = true:suggestion

# IDE0032
dotnet_style_prefer_auto_properties = true:silent

# IDE0033
dotnet_style_explicit_tuple_names = true:suggestion

# IDE0036
dotnet_diagnostic.IDE0036.severity = %value%

# IDE0037
dotnet_style_prefer_inferred_tuple_names = true:suggestion

# IDE0040
dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent

# IDE0041
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion

# IDE0043
dotnet_diagnostic.IDE0043.severity = %value%

# IDE0044
dotnet_style_readonly_field = true:suggestion

# IDE0045
dotnet_style_prefer_conditional_expression_over_assignment = true:silent

# IDE0046
dotnet_style_prefer_conditional_expression_over_return = true:silent

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
dotnet_style_prefer_compound_assignment = true:suggestion

# IDE0055
dotnet_diagnostic.IDE0055.severity = %value%

# IDE0058
visual_basic_style_unused_value_expression_statement_preference = unused_local_variable:silent

# IDE0059
visual_basic_style_unused_value_assignment_preference = unused_local_variable:suggestion

# IDE0060
dotnet_code_quality_unused_parameters = all:suggestion

# IDE0067
dotnet_diagnostic.IDE0067.severity = %value%

# IDE0068
dotnet_diagnostic.IDE0068.severity = %value%

# IDE0069
dotnet_diagnostic.IDE0069.severity = %value%

# IDE1006
dotnet_diagnostic.IDE1006.severity = %value%

# IDE1007
dotnet_diagnostic.IDE1007.severity = %value%

# IDE1008
dotnet_diagnostic.IDE1008.severity = %value%
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
dotnet_style_qualification_for_event = false:silent

# IDE0003, QualifyFieldAccess
dotnet_style_qualification_for_field = false:silent

# IDE0003, QualifyMethodAccess
dotnet_style_qualification_for_method = false:silent

# IDE0003, QualifyPropertyAccess
dotnet_style_qualification_for_property = false:silent

# IDE0004
No editorconfig based code style option

# IDE0005
No editorconfig based code style option

# IDE0007, VarElsewhere
csharp_style_var_elsewhere = false:silent

# IDE0007, VarForBuiltInTypes
csharp_style_var_for_built_in_types = false:silent

# IDE0007, VarWhenTypeIsApparent
csharp_style_var_when_type_is_apparent = false:silent

# IDE0008, VarElsewhere
csharp_style_var_elsewhere = false:silent

# IDE0008, VarForBuiltInTypes
csharp_style_var_for_built_in_types = false:silent

# IDE0008, VarWhenTypeIsApparent
csharp_style_var_when_type_is_apparent = false:silent

# IDE0009, QualifyEventAccess
dotnet_style_qualification_for_event = false:silent

# IDE0009, QualifyFieldAccess
dotnet_style_qualification_for_field = false:silent

# IDE0009, QualifyMethodAccess
dotnet_style_qualification_for_method = false:silent

# IDE0009, QualifyPropertyAccess
dotnet_style_qualification_for_property = false:silent

# IDE0010
No editorconfig based code style option

# IDE0011, PreferBraces
csharp_prefer_braces = true:silent

# IDE0016, PreferThrowExpression
csharp_style_throw_expression = true:suggestion

# IDE0017, PreferObjectInitializer
dotnet_style_object_initializer = true:suggestion

# IDE0018, PreferInlinedVariableDeclaration
csharp_style_inlined_variable_declaration = true:suggestion

# IDE0019, PreferPatternMatchingOverAsWithNullCheck
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion

# IDE0020, PreferPatternMatchingOverIsWithCastCheck
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion

# IDE0021, PreferExpressionBodiedConstructors
csharp_style_expression_bodied_constructors = false:silent

# IDE0022, PreferExpressionBodiedMethods
csharp_style_expression_bodied_methods = false:silent

# IDE0023, PreferExpressionBodiedOperators
csharp_style_expression_bodied_operators = false:silent

# IDE0024, PreferExpressionBodiedOperators
csharp_style_expression_bodied_operators = false:silent

# IDE0025, PreferExpressionBodiedProperties
csharp_style_expression_bodied_properties = true:silent

# IDE0026, PreferExpressionBodiedIndexers
csharp_style_expression_bodied_indexers = true:silent

# IDE0027, PreferExpressionBodiedAccessors
csharp_style_expression_bodied_accessors = true:silent

# IDE0028, PreferCollectionInitializer
dotnet_style_collection_initializer = true:suggestion

# IDE0029, PreferCoalesceExpression
dotnet_style_coalesce_expression = true:suggestion

# IDE0030, PreferCoalesceExpression
dotnet_style_coalesce_expression = true:suggestion

# IDE0031, PreferNullPropagation
dotnet_style_null_propagation = true:suggestion

# IDE0032, PreferAutoProperties
dotnet_style_prefer_auto_properties = true:silent

# IDE0033, PreferExplicitTupleNames
dotnet_style_explicit_tuple_names = true:suggestion

# IDE0034, PreferSimpleDefaultExpression
csharp_prefer_simple_default_expression = true:suggestion

# IDE0035
No editorconfig based code style option

# IDE0036, PreferredModifierOrder
csharp_preferred_modifier_order = public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async

# IDE0037, PreferInferredTupleNames
dotnet_style_prefer_inferred_tuple_names = true:suggestion

# IDE0039, PreferLocalOverAnonymousFunction
csharp_style_pattern_local_over_anonymous_function = true:suggestion

# IDE0040, RequireAccessibilityModifiers
dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent

# IDE0041, PreferIsNullCheckOverReferenceEqualityMethod
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion

# IDE0042, PreferDeconstructedVariableDeclaration
csharp_style_deconstructed_variable_declaration = true:suggestion

# IDE0043
No editorconfig based code style option

# IDE0044, PreferReadonly
dotnet_style_readonly_field = true:suggestion

# IDE0045, PreferConditionalExpressionOverAssignment
dotnet_style_prefer_conditional_expression_over_assignment = true:silent

# IDE0046, PreferConditionalExpressionOverReturn
dotnet_style_prefer_conditional_expression_over_return = true:silent

# IDE0047
No editorconfig based code style option

# IDE0048, ArithmeticBinaryParentheses
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:silent

# IDE0048, OtherBinaryParentheses
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:silent

# IDE0048, OtherParentheses
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:silent

# IDE0048, RelationalBinaryParentheses
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:silent

# IDE0049, PreferIntrinsicPredefinedTypeKeywordInDeclaration
dotnet_style_predefined_type_for_locals_parameters_members = true:silent

# IDE0049, PreferIntrinsicPredefinedTypeKeywordInMemberAccess
dotnet_style_predefined_type_for_member_access = true:silent

# IDE0050
No editorconfig based code style option

# IDE0051
No editorconfig based code style option

# IDE0052
No editorconfig based code style option

# IDE0053, PreferExpressionBodiedLambdas
csharp_style_expression_bodied_lambdas = true:silent

# IDE0054, PreferCompoundAssignment
dotnet_style_prefer_compound_assignment = true:suggestion

# IDE0055
No editorconfig based code style option

# IDE0056, PreferIndexOperator
csharp_style_prefer_index_operator = true:suggestion

# IDE0057, PreferRangeOperator
csharp_style_prefer_range_operator = true:suggestion

# IDE0058, UnusedValueExpressionStatement
csharp_style_unused_value_expression_statement_preference = discard_variable:silent

# IDE0059, UnusedValueAssignment
csharp_style_unused_value_assignment_preference = discard_variable:suggestion

# IDE0060, UnusedParameters
dotnet_code_quality_unused_parameters = all:suggestion

# IDE0061, PreferExpressionBodiedLocalFunctions
csharp_style_expression_bodied_local_functions = false:silent

# IDE0062, PreferStaticLocalFunction
csharp_prefer_static_local_function = true:suggestion

# IDE0063, PreferSimpleUsingStatement
csharp_prefer_simple_using_statement = true:suggestion

# IDE0064
No editorconfig based code style option

# IDE0065, PreferredUsingDirectivePlacement
csharp_using_directive_placement = outside_namespace:silent

# IDE0066, PreferSwitchExpression
csharp_style_prefer_switch_expression = true:suggestion

# IDE0067
No editorconfig based code style option

# IDE0068
No editorconfig based code style option

# IDE0069
No editorconfig based code style option

# IDE1005, PreferConditionalDelegateCall
csharp_style_conditional_delegate_call = true:suggestion

# IDE1006
No editorconfig based code style option

# IDE1007
No editorconfig based code style option

# IDE1008
No editorconfig based code style option
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
dotnet_style_qualification_for_event = false:silent

# IDE0003, QualifyFieldAccess
dotnet_style_qualification_for_field = false:silent

# IDE0003, QualifyMethodAccess
dotnet_style_qualification_for_method = false:silent

# IDE0003, QualifyPropertyAccess
dotnet_style_qualification_for_property = false:silent

# IDE0004
No editorconfig based code style option

# IDE0005
No editorconfig based code style option

# IDE0009, QualifyEventAccess
dotnet_style_qualification_for_event = false:silent

# IDE0009, QualifyFieldAccess
dotnet_style_qualification_for_field = false:silent

# IDE0009, QualifyMethodAccess
dotnet_style_qualification_for_method = false:silent

# IDE0009, QualifyPropertyAccess
dotnet_style_qualification_for_property = false:silent

# IDE0010
No editorconfig based code style option

# IDE0017, PreferObjectInitializer
dotnet_style_object_initializer = true:suggestion

# IDE0028, PreferCollectionInitializer
dotnet_style_collection_initializer = true:suggestion

# IDE0029, PreferCoalesceExpression
dotnet_style_coalesce_expression = true:suggestion

# IDE0030, PreferCoalesceExpression
dotnet_style_coalesce_expression = true:suggestion

# IDE0031, PreferNullPropagation
dotnet_style_null_propagation = true:suggestion

# IDE0032, PreferAutoProperties
dotnet_style_prefer_auto_properties = true:silent

# IDE0033, PreferExplicitTupleNames
dotnet_style_explicit_tuple_names = true:suggestion

# IDE0036, PreferredModifierOrder
visual_basic_preferred_modifier_order = partial,default,private,protected,public,friend,notoverridable,overridable,mustoverride,overloads,overrides,mustinherit,notinheritable,static,shared,shadows,readonly,writeonly,dim,const,withevents,widening,narrowing,custom,async,iterator

# IDE0037, PreferInferredTupleNames
dotnet_style_prefer_inferred_tuple_names = true:suggestion

# IDE0040, RequireAccessibilityModifiers
dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent

# IDE0041, PreferIsNullCheckOverReferenceEqualityMethod
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion

# IDE0043
No editorconfig based code style option

# IDE0044, PreferReadonly
dotnet_style_readonly_field = true:suggestion

# IDE0045, PreferConditionalExpressionOverAssignment
dotnet_style_prefer_conditional_expression_over_assignment = true:silent

# IDE0046, PreferConditionalExpressionOverReturn
dotnet_style_prefer_conditional_expression_over_return = true:silent

# IDE0047
No editorconfig based code style option

# IDE0048, ArithmeticBinaryParentheses
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:silent

# IDE0048, OtherBinaryParentheses
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:silent

# IDE0048, OtherParentheses
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:silent

# IDE0048, RelationalBinaryParentheses
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:silent

# IDE0049, PreferIntrinsicPredefinedTypeKeywordInDeclaration
dotnet_style_predefined_type_for_locals_parameters_members = true:silent

# IDE0049, PreferIntrinsicPredefinedTypeKeywordInMemberAccess
dotnet_style_predefined_type_for_member_access = true:silent

# IDE0050
No editorconfig based code style option

# IDE0051
No editorconfig based code style option

# IDE0052
No editorconfig based code style option

# IDE0054, PreferCompoundAssignment
dotnet_style_prefer_compound_assignment = true:suggestion

# IDE0055
No editorconfig based code style option

# IDE0058, UnusedValueExpressionStatement
visual_basic_style_unused_value_expression_statement_preference = unused_local_variable:silent

# IDE0059, UnusedValueAssignment
visual_basic_style_unused_value_assignment_preference = unused_local_variable:suggestion

# IDE0060, UnusedParameters
dotnet_code_quality_unused_parameters = all:suggestion

# IDE0067
No editorconfig based code style option

# IDE0068
No editorconfig based code style option

# IDE0069
No editorconfig based code style option

# IDE1006
No editorconfig based code style option

# IDE1007
No editorconfig based code style option

# IDE1008
No editorconfig based code style option
";

            VerifyConfigureCodeStyleOptionsCore(expected, LanguageNames.VisualBasic);
        }
    }
}
