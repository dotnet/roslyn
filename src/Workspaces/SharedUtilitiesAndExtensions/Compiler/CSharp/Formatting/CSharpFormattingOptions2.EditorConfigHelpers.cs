// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal static partial class CSharpFormattingOptions2
    {
        internal sealed class EditorConfigHelpers
        {
            // Maps to store mapping between special option kinds and the corresponding editor config string representations.
            public readonly BidirectionalMap<string, SpacingWithinParenthesesOption> SpacingWithinParenthesisOptionsEditorConfigMap =
                new(KeyValuePairUtil.Create("expressions", SpacingWithinParenthesesOption.Expressions),
                    KeyValuePairUtil.Create("type_casts", SpacingWithinParenthesesOption.TypeCasts),
                    KeyValuePairUtil.Create("control_flow_statements", SpacingWithinParenthesesOption.ControlFlowStatements));

            public readonly BidirectionalMap<string, BinaryOperatorSpacingOptions> BinaryOperatorSpacingOptionsEditorConfigMap =
                new(KeyValuePairUtil.Create("ignore", BinaryOperatorSpacingOptions.Ignore),
                    KeyValuePairUtil.Create("none", BinaryOperatorSpacingOptions.Remove),
                    KeyValuePairUtil.Create("before_and_after", BinaryOperatorSpacingOptions.Single));

            public readonly BidirectionalMap<string, LabelPositionOptions> LabelPositionOptionsEditorConfigMap =
                new(KeyValuePairUtil.Create("flush_left", LabelPositionOptions.LeftMost),
                    KeyValuePairUtil.Create("no_change", LabelPositionOptions.NoIndent),
                    KeyValuePairUtil.Create("one_less_than_current", LabelPositionOptions.OneLess));

            public readonly BidirectionalMap<string, NewLineOption> LegacyNewLineOptionsEditorConfigMap =
                new(KeyValuePairUtil.Create("object_collection_array_initalizers", NewLineOption.ObjectCollectionsArrayInitializers));

            public readonly BidirectionalMap<string, NewLineOption> NewLineOptionsEditorConfigMap =
                new(KeyValuePairUtil.Create("accessors", NewLineOption.Accessors),
                    KeyValuePairUtil.Create("types", NewLineOption.Types),
                    KeyValuePairUtil.Create("methods", NewLineOption.Methods),
                    KeyValuePairUtil.Create("properties", NewLineOption.Properties),
                    KeyValuePairUtil.Create("indexers", NewLineOption.Indexers),
                    KeyValuePairUtil.Create("events", NewLineOption.Events),
                    KeyValuePairUtil.Create("anonymous_methods", NewLineOption.AnonymousMethods),
                    KeyValuePairUtil.Create("control_blocks", NewLineOption.ControlBlocks),
                    KeyValuePairUtil.Create("anonymous_types", NewLineOption.AnonymousTypes),
                    KeyValuePairUtil.Create("object_collection_array_initializers", NewLineOption.ObjectCollectionsArrayInitializers),
                    KeyValuePairUtil.Create("lambdas", NewLineOption.Lambdas),
                    KeyValuePairUtil.Create("local_functions", NewLineOption.LocalFunction));

            public readonly Func<LabelPositionOptions, string> GetLabelPositionOptionString;
            public readonly Func<OptionSet, string> GetNewLineOptionString;
            public readonly Func<BinaryOperatorSpacingOptions, string> GetSpacingAroundBinaryOperatorString;
            public readonly Func<OptionSet, string> GetSpacingWithParenthesesString;

            public readonly Func<string, Optional<LabelPositionOptions>> ParseLabelPositioning;
            public readonly Func<string, Optional<BinaryOperatorSpacingOptions>> ParseSpacingAroundBinaryOperator;

            public EditorConfigHelpers()
            {
                GetLabelPositionOptionString = GetLabelPositionStringImpl;
                GetNewLineOptionString = GetNewLineOptionStringImpl;
                GetSpacingAroundBinaryOperatorString = GetSpacingAroundBinaryOperatorStringImpl;
                GetSpacingWithParenthesesString = GetSpacingWithParenthesesStringImpl;

                ParseLabelPositioning = ParseLabelPositioningImpl;
                ParseSpacingAroundBinaryOperator = ParseSpacingAroundBinaryOperatorImpl;
            }

            public readonly Func<string, Optional<bool>> DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet = (string value)
                => value.Trim() == "ignore";

            public readonly Func<bool, string> GetIgnoreSpacesAroundVariableDeclarationString = (bool value)
                => value ? "ignore" : "false";

            private string GetLabelPositionStringImpl(LabelPositionOptions value)
                => LabelPositionOptionsEditorConfigMap.TryGetKey(value, out var key) ? key : "";

            private string GetNewLineOptionStringImpl(OptionSet optionSet)
            {
                var editorConfigStringBuilder = new List<string>(NewLineOptionsMap.Count);

                foreach (var (key, value) in NewLineOptionsMap)
                {
                    if (optionSet.GetOption(key))
                    {
                        Debug.Assert(NewLineOptionsEditorConfigMap.ContainsValue(value));
                        editorConfigStringBuilder.Add(NewLineOptionsEditorConfigMap.GetKeyOrDefault(value)!);
                    }
                }

                if (editorConfigStringBuilder.Count == 0)
                {
                    // No NewLine option set.
                    return "none";
                }
                else if (editorConfigStringBuilder.Count == NewLineOptionsMap.Count)
                {
                    // All NewLine options set.
                    return "all";
                }
                else
                {
                    return string.Join(",", editorConfigStringBuilder.Order());
                }
            }

            private string GetSpacingAroundBinaryOperatorStringImpl(BinaryOperatorSpacingOptions value)
                => BinaryOperatorSpacingOptionsEditorConfigMap.TryGetKey(value, out var key) ? key : "";

            private string GetSpacingWithParenthesesStringImpl(OptionSet optionSet)
            {
                var editorConfigStringBuilder = new List<string>();

                foreach (var (key, value) in SpacingWithinParenthesisOptionsMap)
                {
                    if (optionSet.GetOption(key))
                    {
                        Debug.Assert(SpacingWithinParenthesisOptionsEditorConfigMap.ContainsValue(value));
                        editorConfigStringBuilder.Add(SpacingWithinParenthesisOptionsEditorConfigMap.GetKeyOrDefault(value)!);
                    }
                }

                if (editorConfigStringBuilder.Count == 0)
                {
                    // No spacing within parenthesis option set.
                    return "false";
                }
                else
                {
                    return string.Join(",", editorConfigStringBuilder.Order());
                }
            }

            private Optional<LabelPositionOptions> ParseLabelPositioningImpl(string labelIndentationValue)
                => LabelPositionOptionsEditorConfigMap.TryGetValue(labelIndentationValue.Trim(), out var value)
                    ? value
                    : LabelPositionOptions.NoIndent;

            private Optional<BinaryOperatorSpacingOptions> ParseSpacingAroundBinaryOperatorImpl(string binaryOperatorSpacingValue)
                => BinaryOperatorSpacingOptionsEditorConfigMap.TryGetValue(binaryOperatorSpacingValue.Trim(), out var value)
                    ? value
                    : BinaryOperatorSpacingOptions.Single;

            public Func<string, Optional<bool>> DetermineIfSpaceOptionIsSet(SpacingWithinParenthesesOption parenthesesSpacingOption)
                => value => DetermineIfSpaceOptionIsSet(value, parenthesesSpacingOption);

            public bool DetermineIfSpaceOptionIsSet(string value, SpacingWithinParenthesesOption parenthesesSpacingOption)
            {
                foreach (var part in value.Split(','))
                {
                    if (ConvertToSpacingOption(part.Trim()) is { } option &&
                        option == parenthesesSpacingOption)
                    {
                        return true;
                    }
                }

                return false;

                SpacingWithinParenthesesOption? ConvertToSpacingOption(string value)
                    => SpacingWithinParenthesisOptionsEditorConfigMap.TryGetValue(value, out var option)
                       ? option
                       : null;
            }

            public Func<string, Optional<bool>> DetermineIfNewLineOptionIsSet(NewLineOption newLineOption)
                => value => DetermineIfNewLineOptionIsSet(value, newLineOption);

            public bool DetermineIfNewLineOptionIsSet(string value, NewLineOption newLineOption)
            {
                using var pooledSet = SharedPools.StringHashSet.GetPooledObject();
                var set = pooledSet.Object;

                foreach (var part in value.Split(','))
                {
                    set.Add(part.Trim());
                }

                if (set.Contains("all"))
                {
                    return true;
                }

                if (set.Contains("none"))
                {
                    return false;
                }

                foreach (var part in set)
                {
                    if (ConvertToNewLineOption(part) is { } option &&
                        option == newLineOption)
                    {
                        return true;
                    }
                }

                return false;

                NewLineOption? ConvertToNewLineOption(string value)
                {
                    if (NewLineOptionsEditorConfigMap.TryGetValue(value, out var option))
                    {
                        return option;
                    }

                    if (LegacyNewLineOptionsEditorConfigMap.TryGetValue(value, out var legacyOption))
                    {
                        return legacyOption;
                    }

                    return null;
                }
            }
        }

        internal enum SpacingWithinParenthesesOption
        {
            Expressions,
            TypeCasts,
            ControlFlowStatements
        }

        internal enum NewLineOption
        {
            Types,
            Methods,
            Properties,
            Indexers,
            Events,
            AnonymousMethods,
            ControlBlocks,
            AnonymousTypes,
            ObjectCollectionsArrayInitializers,
            Lambdas,
            LocalFunction,
            Accessors
        }
    }
}
