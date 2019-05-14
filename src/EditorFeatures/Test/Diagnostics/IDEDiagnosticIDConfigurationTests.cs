using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.ConfigureSeverityLevel
{
    [UseExportProvider]
    public class IDEDiagnosticIDConfigurationTests
    {
        private void Verify(string expected, IDiagnosticIdToEditorConfigOptionMappingService service)
        {
            using var workspace = new TestWorkspace();
            var optionSet = workspace.Options;

            var diagnosticIds = new List<string>();
            foreach (var field in typeof(IDEDiagnosticIds).GetFields())
            {
                Assert.Equal(typeof(string), field.FieldType);

                var diagnosticId = field.GetRawConstantValue() as string;
                Assert.True(!string.IsNullOrEmpty(diagnosticId));

                Assert.StartsWith("IDE", diagnosticId);
                diagnosticIds.Add(diagnosticId);
            }

            diagnosticIds.Sort();

            var expectedLines = expected.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(expectedLines.Length % 2 == 0);
            var expectedMap = new Dictionary<string, string>();
            for (int i = 0; i < expectedLines.Length; i += 2)
            {
                expectedMap.Add(expectedLines[i], expectedLines[i + 1]);
            }

            var baseline = new StringBuilder();
            foreach (var diagnosticId in diagnosticIds)
            {
                var editorConfigOption = service.GetMappedEditorConfigOption(diagnosticId);
                var editorConfigLocation = editorConfigOption?.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();
                string editorConfigString;
                if (editorConfigLocation != null)
                {
                    var optionKey = new OptionKey(editorConfigOption, editorConfigOption.IsPerLanguage ? LanguageNames.CSharp : null);
                    var value = optionSet.GetOption(optionKey);
                    editorConfigString = editorConfigLocation.GetEditorConfigString(value, optionSet);
                }
                else
                {
                    editorConfigString = $"dotnet_diagnostic.{diagnosticId}.severity = %value%";
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
        public void CSharp_VerifyIDEDiagnosticsAreConfigurable()
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

# IDE0006
dotnet_diagnostic.IDE0006.severity = %value%

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

# IDE0038
dotnet_diagnostic.IDE0038.severity = %value%

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

# IDE1001
dotnet_diagnostic.IDE1001.severity = %value%

# IDE1002
dotnet_diagnostic.IDE1002.severity = %value%

# IDE1003
dotnet_diagnostic.IDE1003.severity = %value%

# IDE1004
dotnet_diagnostic.IDE1004.severity = %value%

# IDE1005
csharp_style_conditional_delegate_call = true:suggestion

# IDE1006
dotnet_diagnostic.IDE1006.severity = %value%

# IDE1007
dotnet_diagnostic.IDE1007.severity = %value%

# IDE1008
dotnet_diagnostic.IDE1008.severity = %value%
";

            Verify(expected, new CSharpDiagnosticIdToEditorConfigOptionMappingService());
        }

        [Fact]
        public void VisualBasic_VerifyIDEDiagnosticsAreConfigurable()
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

# IDE0006
dotnet_diagnostic.IDE0006.severity = %value%

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
csharp_style_throw_expression = true:suggestion

# IDE0017
dotnet_style_object_initializer = true:suggestion

# IDE0018
csharp_style_inlined_variable_declaration = true:suggestion

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
dotnet_diagnostic.IDE0034.severity = %value%

# IDE0035
dotnet_diagnostic.IDE0035.severity = %value%

# IDE0036
dotnet_diagnostic.IDE0036.severity = %value%

# IDE0037
dotnet_style_prefer_inferred_tuple_names = true:suggestion

# IDE0038
dotnet_diagnostic.IDE0038.severity = %value%

# IDE0039
dotnet_diagnostic.IDE0039.severity = %value%

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
dotnet_diagnostic.IDE0053.severity = %value%

# IDE0054
dotnet_style_prefer_compound_assignment = true:suggestion

# IDE0055
dotnet_diagnostic.IDE0055.severity = %value%

# IDE0056
dotnet_diagnostic.IDE0056.severity = %value%

# IDE0057
dotnet_diagnostic.IDE0057.severity = %value%

# IDE0058
visual_basic_style_unused_value_expression_statement_preference = unused_local_variable:silent

# IDE0059
visual_basic_style_unused_value_assignment_preference = unused_local_variable:suggestion

# IDE0060
dotnet_code_quality_unused_parameters = all:suggestion

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

# IDE1001
dotnet_diagnostic.IDE1001.severity = %value%

# IDE1002
dotnet_diagnostic.IDE1002.severity = %value%

# IDE1003
dotnet_diagnostic.IDE1003.severity = %value%

# IDE1004
dotnet_diagnostic.IDE1004.severity = %value%

# IDE1005
dotnet_diagnostic.IDE1005.severity = %value%

# IDE1006
dotnet_diagnostic.IDE1006.severity = %value%

# IDE1007
dotnet_diagnostic.IDE1007.severity = %value%

# IDE1008
dotnet_diagnostic.IDE1008.severity = %value%
";

            Verify(expected, new VisualBasicDiagnosticIdToEditorConfigOptionMappingService());
        }
    }
}
