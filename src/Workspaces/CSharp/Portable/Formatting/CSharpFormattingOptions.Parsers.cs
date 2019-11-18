// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    public static partial class CSharpFormattingOptions
    {
        internal static bool DetermineIfSpaceOptionIsSet(string value, SpacingWithinParenthesesOption parenthesesSpacingOption)
            => (from v in value.Split(',').Select(v => v.Trim())
                let option = ConvertToSpacingOption(v)
                where option.HasValue && option.Value == parenthesesSpacingOption
                select option)
                .Any();

        private static SpacingWithinParenthesesOption? ConvertToSpacingOption(string value)
            => s_spacingWithinParenthesisOptionsEditorConfigMap.TryGetValue(value, out var option)
               ? option
               : (SpacingWithinParenthesesOption?)null;

        private static string GetSpacingWithParenthesesEditorConfigString(OptionSet optionSet)
        {
            var editorConfigStringBuilder = new List<string>();
            foreach (var kvp in SpacingWithinParenthesisOptionsMap)
            {
                var value = optionSet.GetOption(kvp.Key);
                if (value)
                {
                    Debug.Assert(s_spacingWithinParenthesisOptionsEditorConfigMap.ContainsValue(kvp.Value));
                    editorConfigStringBuilder.Add(s_spacingWithinParenthesisOptionsEditorConfigMap.GetKeyOrDefault(kvp.Value));
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

        internal static BinaryOperatorSpacingOptions ParseEditorConfigSpacingAroundBinaryOperator(string binaryOperatorSpacingValue)
            => s_binaryOperatorSpacingOptionsEditorConfigMap.TryGetValue(binaryOperatorSpacingValue.Trim(), out var value) ? value : BinaryOperatorSpacingOptions.Single;

        private static string GetSpacingAroundBinaryOperatorEditorConfigString(BinaryOperatorSpacingOptions value)
            => s_binaryOperatorSpacingOptionsEditorConfigMap.TryGetKey(value, out var key) ? key : null;

        internal static LabelPositionOptions ParseEditorConfigLabelPositioning(string labelIndentationValue)
            => s_labelPositionOptionsEditorConfigMap.TryGetValue(labelIndentationValue.Trim(), out var value) ? value : LabelPositionOptions.NoIndent;

        private static string GetLabelPositionOptionEditorConfigString(LabelPositionOptions value)
            => s_labelPositionOptionsEditorConfigMap.TryGetKey(value, out var key) ? key : null;

        internal static bool DetermineIfNewLineOptionIsSet(string value, NewLineOption optionName)
        {
            var values = value.Split(',').Select(v => v.Trim());

            if (values.Any(s => s == "all"))
            {
                return true;
            }

            if (values.Any(s => s == "none"))
            {
                return false;
            }

            return (from v in values
                    let option = ConvertToNewLineOption(v)
                    where option.HasValue && option.Value == optionName
                    select option)
                    .Any();
        }

        private static NewLineOption? ConvertToNewLineOption(string value)
        {
            if (s_newLineOptionsEditorConfigMap.TryGetValue(value, out var option))
            {
                return option;
            }
            if (s_legacyNewLineOptionsEditorConfigMap.TryGetValue(value, out var legacyOption))
            {
                return legacyOption;
            }
            return null;
        }

        private static string GetNewLineOptionEditorConfigString(OptionSet optionSet)
        {
            var editorConfigStringBuilder = new List<string>(NewLineOptionsMap.Count);
            foreach (var kvp in NewLineOptionsMap)
            {
                var value = optionSet.GetOption(kvp.Key);
                if (value)
                {
                    Debug.Assert(s_newLineOptionsEditorConfigMap.ContainsValue(kvp.Value));
                    editorConfigStringBuilder.Add(s_newLineOptionsEditorConfigMap.GetKeyOrDefault(kvp.Value));
                }
            }

            if (editorConfigStringBuilder.Count == 0)
            {
                // No NewLine option set.
                return "none";
            }
            else if (editorConfigStringBuilder.Count == s_newLineOptionsMapBuilder.Count)
            {
                // All NewLine options set.
                return "all";
            }
            else
            {
                return string.Join(",", editorConfigStringBuilder.Order());
            }
        }

        internal static bool DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(string value)
            => value.Trim() == "ignore";

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
