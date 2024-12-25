// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal static partial class CSharpFormattingOptions2
{
    public static int ParseEditorConfigFlags(
        string list,
        Func<string, int> map,
        string? noneToken = null,
        string? allToken = null,
        int allValue = -1)
    {
        var flags = 0;

        var tokens = list.Split(',');
        var hasNoneToken = false;

        foreach (var token in tokens)
        {
            var trimmed = token.Trim();
            if (trimmed == allToken)
            {
                // "all" token has higher priority then "none"
                return allValue;
            }

            if (trimmed == noneToken)
            {
                hasNoneToken = true;
                continue;
            }

            flags |= map(trimmed);
        }

        // if "none" is present all other flags are ignored
        return hasNoneToken ? 0 : flags;
    }

    internal static string ToEditorConfigFlagList(int flags, Func<int, string> map)
    {
        using var _ = PooledStringBuilder.GetInstance(out var builder);

        var flag = 1;
        while (flag <= flags)
        {
            if ((flags & flag) == flag)
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(map(flag));
            }

            flag <<= 1;
        }

        return builder.ToString();
    }

    internal static SpacePlacementWithinParentheses ParseSpacingWithinParenthesesList(string list)
        => (SpacePlacementWithinParentheses)ParseEditorConfigFlags(list, static s => s_spacingWithinParenthesisOptionsEditorConfigMap.TryGetValue(s, out var v) ? (int)v : 0);

    internal static string ToEditorConfigValue(SpacePlacementWithinParentheses value)
        => (value == SpacePlacementWithinParentheses.None) ? "false" :
           ToEditorConfigFlagList((int)value, static v => s_spacingWithinParenthesisOptionsEditorConfigMap[(SpacePlacementWithinParentheses)v]);

    internal static NewLineBeforeOpenBracePlacement ParseNewLineBeforeOpenBracePlacementList(string list)
        => (NewLineBeforeOpenBracePlacement)ParseEditorConfigFlags(
           list,
           static s => s_newLineOptionsEditorConfigMap.TryGetValue(s, out var v) ? (int)v : s_legacyNewLineOptionsEditorConfigMap.TryGetValue(s, out v) ? (int)v : 0,
           noneToken: "none",
           allToken: "all",
           allValue: (int)NewLineBeforeOpenBracePlacement.All);

    internal static string ToEditorConfigValue(NewLineBeforeOpenBracePlacement value)
        => value switch
        {
            NewLineBeforeOpenBracePlacement.None => "none",
            NewLineBeforeOpenBracePlacement.All => "all",
            _ => ToEditorConfigFlagList((int)value, static v => s_newLineOptionsEditorConfigMap[(NewLineBeforeOpenBracePlacement)v])
        };

    internal static BinaryOperatorSpacingOptions ParseEditorConfigSpacingAroundBinaryOperator(string binaryOperatorSpacingValue)
        => s_binaryOperatorSpacingOptionsEditorConfigMap.TryGetValue(binaryOperatorSpacingValue.Trim(), out var value) ? value : BinaryOperatorSpacingOptions.Single;

    private static string GetSpacingAroundBinaryOperatorEditorConfigString(BinaryOperatorSpacingOptions value)
        => s_binaryOperatorSpacingOptionsEditorConfigMap.TryGetKey(value, out var key) ? key : "";

    internal static LabelPositionOptions ParseEditorConfigLabelPositioning(string labelIndentationValue)
        => s_labelPositionOptionsEditorConfigMap.TryGetValue(labelIndentationValue.Trim(), out var value) ? value : LabelPositionOptions.NoIndent;

    private static string GetLabelPositionOptionEditorConfigString(LabelPositionOptions value)
        => s_labelPositionOptionsEditorConfigMap.TryGetKey(value, out var key) ? key : "";

    internal static bool DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(string value)
        => value.Trim() == "ignore";
}
