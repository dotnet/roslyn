// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal class EditorConfigSettingsValuesDocumentation
    {
        #region Boolean Documentations
        public static readonly Dictionary<string, string> YesOrNoDocumentation = new()
        {
            { "true", CompilerExtensionsResources.Yes },
            { "false", CompilerExtensionsResources.No },
        };

        public static readonly Dictionary<string, string> ThisOrMeDocumentation = new()
        {
            { "true", CompilerExtensionsResources.Prefer_this_or_Me },
            { "false", CompilerExtensionsResources.Do_not_prefer_this_or_Me },
        };

        public static readonly Dictionary<string, string> PreferTypeDocumentation = new()
        {
            { "true", CompilerExtensionsResources.Prefer_predefined_type },
            { "false", CompilerExtensionsResources.Prefer_framework_type },
        };

        public static readonly Dictionary<string, string> PreferVarDocumentation = new()
        {
            { "true", CompilerExtensionsResources.Prefer_var },
            { "false", CompilerExtensionsResources.Prefer_explicit_type },
        };
        #endregion

        #region Strings Documentation
        public static readonly Dictionary<string, string> NewLineDocumentation = new()
        {
            { "lf", CompilerExtensionsResources.Lf },
            { "cr", CompilerExtensionsResources.Cr },
            { "crlf", CompilerExtensionsResources.Crlf },
        };
        #endregion

        #region Enums Documentations
        public static readonly Dictionary<string, string> OperatorPlacementWhenWrappingPreferenceDocumentation = new()
        {
            { "end_of_line", CompilerExtensionsResources.End_of_line },
            { "beginning_of_line", CompilerExtensionsResources.Beginning_of_line },
        };

        public static readonly Dictionary<string, string> UseTabsDocumentation = new()
        {
            { "space", CompilerExtensionsResources.Space },
            { "tab", CompilerExtensionsResources.Tab },
        };

        public static readonly Dictionary<string, string> SpacesIgnoreAroundVariableDeclarationDocumentation = new()
        {
            { "ignore", CompilerExtensionsResources.Ignore },
            { "false", CompilerExtensionsResources.False },
        };

        public static readonly Dictionary<string, string> SpacingWithinParenthesisOptionsDocumentation = new()
        {
            { "expressions", CompilerExtensionsResources.Expressions },
            { "type_casts", CompilerExtensionsResources.Type_casts },
            { "control_flow_statements", CompilerExtensionsResources.Control_flow_statements },
        };

        public static readonly Dictionary<string, string> BinaryOperatorSpacingOptionsDocumentation = new()
        {
            { "ignore", CompilerExtensionsResources.Ignore_binary_operator_spacing },
            { "none", CompilerExtensionsResources.None_binary_operator_spacing },
            { "before_and_after", CompilerExtensionsResources.Before_and_after },
        };

        public static readonly Dictionary<string, string> NewLineOptionsDocumentation = new()
        {
            { "accessors", CompilerExtensionsResources.Accessors },
            { "types", CompilerExtensionsResources.Types },
            { "methods", CompilerExtensionsResources.Methods },
            { "properties", CompilerExtensionsResources.Properties },
            { "indexers", CompilerExtensionsResources.Indexers },
            { "events", CompilerExtensionsResources.Events },
            { "anonymous_methods", CompilerExtensionsResources.Anonymous_methods },
            { "control_blocks", CompilerExtensionsResources.Control_blocks },
            { "anonymous_types", CompilerExtensionsResources.Anonymous_types },
            { "object_collection_array_initializers", CompilerExtensionsResources.Object_collection_array_initializers },
            { "lambdas", CompilerExtensionsResources.Lambdas },
            { "local_functions", CompilerExtensionsResources.Local_functions },
        };

        public static readonly Dictionary<string, string> LabelPositionOptionsDocumentation = new()
        {
            { "flush_left", CompilerExtensionsResources.Flush_left },
            { "no_change", CompilerExtensionsResources.No_change },
            { "one_less_than_current", CompilerExtensionsResources.One_less_than_current },
        };

        public static readonly Dictionary<string, string> AccessibilityModifiersDocumentation = new()
        {
            { "never", CompilerExtensionsResources.Never },
            { "always", CompilerExtensionsResources.Always },
            { "for_non_interface_members", CompilerExtensionsResources.For_non_interface_members },
            { "omit_if_default", CompilerExtensionsResources.Omit_if_default },
        };

        public static readonly Dictionary<string, string> ParenthesesPreferenceDocumentation = new()
        {
            { "always_for_clarity", CompilerExtensionsResources.Always_for_clarity },
            { "never_if_unnecessary", CompilerExtensionsResources.Never_if_unnecessary },
        };

        public static readonly Dictionary<string, string> UnusedParametersPreferenceDocumentation = new()
        {
            { "non_public", CompilerExtensionsResources.Non_public_methods },
            { "all", CompilerExtensionsResources.All_methods },
        };

        public static readonly Dictionary<string, string> AddImportPlacementDocumentation = new()
        {
            { "inside_namespace", CompilerExtensionsResources.Inside_namespace },
            { "outside_namespace", CompilerExtensionsResources.Outside_namespace },
        };

        public static readonly Dictionary<string, string> PreferBracesDocumentation = new()
        {
            { "false", CompilerExtensionsResources.No },
            { "when_multiline", CompilerExtensionsResources.When_on_multiple_lines },
            { "true", CompilerExtensionsResources.Yes },
        };

        public static readonly Dictionary<string, string> NamespaceDeclarationPreferencesDocumentation = new()
        {
            { "block_scoped", CompilerExtensionsResources.Block_scoped },
            { "file_scoped", CompilerExtensionsResources.File_scoped },
        };

        public static readonly Dictionary<string, string> ExpressionBodyPreferenceDocumentation = new()
        {
            { "false", CompilerExtensionsResources.Never },
            { "when_on_single_line", CompilerExtensionsResources.When_on_single_line },
            { "true", CompilerExtensionsResources.When_possible },
        };

        public static readonly Dictionary<string, string> UnusedValuePreferenceDocumentation = new()
        {
            { "discard_variable", CompilerExtensionsResources.Discard },
            { "unused_local_variable", CompilerExtensionsResources.Unused_local },
        };

        public static readonly Dictionary<string, string> DiagnosticSeverityDocumentation = new()
        {
            { "suggestion", CompilerExtensionsResources.Suggestion_severity },
            { "warning", CompilerExtensionsResources.Warning_severity },
            { "silent", CompilerExtensionsResources.Silent_severity },
            { "error", CompilerExtensionsResources.Error_severity },
        };
        #endregion
    }
}
