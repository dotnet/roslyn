// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.EditorConfigSettings;
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
        internal static bool DetermineIfSpaceOptionIsSet(string value, SpacingWithinParenthesesOption parenthesesSpacingOption, EditorConfigData<SpacingWithinParenthesesOption> editorConfigData)
            => (from v in value.Split(',').Select(v => v.Trim())
                let option = ConvertToSpacingOption(v, editorConfigData)
                where option.HasValue && option.Value == parenthesesSpacingOption
                select option)
                .Any();

        private static SpacingWithinParenthesesOption? ConvertToSpacingOption(string value, EditorConfigData<SpacingWithinParenthesesOption> editorConfigData)
        {
            var editorConfigOption = editorConfigData.GetValueFromEditorConfigString(value);
            if (editorConfigOption.HasValue)
            {
                return editorConfigOption.Value;
            }

            return null;
        }

        private static string GetSpacingWithParenthesesEditorConfigString(OptionSet optionSet, EditorConfigData<SpacingWithinParenthesesOption> editorConfigData)
        {
            var editorConfigStringBuilder = new List<string>();
            foreach (var kvp in SpacingWithinParenthesisOptionsMap)
            {
                var value = optionSet.GetOption(kvp.Key);
                if (value)
                {
                    var editorConfigString = editorConfigData.GetEditorConfigStringFromValue(kvp.Value);
                    Debug.Assert(editorConfigString != null);
                    editorConfigStringBuilder.Add(editorConfigString!);
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

        internal static bool DetermineIfNewLineOptionIsSet(string value, NewLineOption optionName, EditorConfigData<NewLineOption> editorConfigData)
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
                    let option = ConvertToNewLineOption(v, editorConfigData)
                    where option.HasValue && option.Value == optionName
                    select option)
                    .Any();
        }

        private static NewLineOption? ConvertToNewLineOption(string value, EditorConfigData<NewLineOption> editorConfigData)
        {
            var editorConfigOption = editorConfigData.GetValueFromEditorConfigString(value);
            if (editorConfigOption.HasValue)
            {
                return editorConfigOption.Value;
            }

            if (s_legacyNewLineOptionsEditorConfigMap.TryGetValue(value, out var legacyOption))
            {
                return legacyOption;
            }

            return null;
        }

        private static string GetNewLineOptionEditorConfigString(OptionSet optionSet, EditorConfigData<NewLineOption> editorConfigData)
        {
            var editorConfigStringBuilder = new List<string>(NewLineOptionsMap.Count);
            foreach (var kvp in NewLineOptionsMap)
            {
                var value = optionSet.GetOption(kvp.Key);
                if (value)
                {
                    var editorConfigString = editorConfigData.GetEditorConfigStringFromValue(kvp.Value);
                    Debug.Assert(editorConfigString != null);
                    editorConfigStringBuilder.Add(editorConfigString!);
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
