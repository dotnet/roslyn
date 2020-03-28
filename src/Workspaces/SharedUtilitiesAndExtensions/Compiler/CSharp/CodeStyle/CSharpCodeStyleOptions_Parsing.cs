// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static partial class CSharpCodeStyleOptions
    {
        public static CodeStyleOption2<ExpressionBodyPreference> ParseExpressionBodyPreference(
            string optionString, CodeStyleOption2<ExpressionBodyPreference> @default)
        {
            // optionString must be similar to true:error or when_on_single_line:suggestion.
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(optionString,
                    out var value, out var notificationOpt))
            {
                // A notification value must be provided.
                if (notificationOpt != null)
                {
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return boolValue
                            ? new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, notificationOpt)
                            : new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, notificationOpt);
                    }

                    if (value == "when_on_single_line")
                    {
                        return new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, notificationOpt);
                    }
                }
            }

            return @default;
        }

        private static string GetExpressionBodyPreferenceEditorConfigString(CodeStyleOption2<ExpressionBodyPreference> value)
        {
            var notificationString = value.Notification.ToEditorConfigString();
            return value.Value switch
            {
                ExpressionBodyPreference.Never => $"false:{notificationString}",
                ExpressionBodyPreference.WhenPossible => $"true:{notificationString}",
                ExpressionBodyPreference.WhenOnSingleLine => $"when_on_single_line:{notificationString}",
                _ => throw new NotSupportedException(),
            };
        }

        public static CodeStyleOption2<AddImportPlacement> ParseUsingDirectivesPlacement(
            string optionString, CodeStyleOption2<AddImportPlacement> @default)
        {
            // optionString must be similar to outside_namespace:error or inside_namespace:suggestion.
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                optionString, out var value, out var notificationOpt))
            {
                // A notification value must be provided.
                if (notificationOpt != null)
                {
                    return value switch
                    {
                        "inside_namespace" => new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.InsideNamespace, notificationOpt),
                        "outside_namespace" => new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.OutsideNamespace, notificationOpt),
                        _ => throw new NotSupportedException(),
                    };
                }
            }

            return @default;
        }

        public static string GetUsingDirectivesPlacementEditorConfigString(CodeStyleOption2<AddImportPlacement> value)
        {
            var notificationString = value.Notification.ToEditorConfigString();
            return value.Value switch
            {
                AddImportPlacement.InsideNamespace => $"inside_namespace:{notificationString}",
                AddImportPlacement.OutsideNamespace => $"outside_namespace:{notificationString}",
                _ => throw new NotSupportedException(),
            };
        }

        private static CodeStyleOption2<PreferBracesPreference> ParsePreferBracesPreference(
            string optionString,
            CodeStyleOption2<PreferBracesPreference> defaultValue)
        {
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                optionString,
                out var value,
                out var notificationOption))
            {
                if (notificationOption != null)
                {
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return boolValue
                            ? new CodeStyleOption2<PreferBracesPreference>(PreferBracesPreference.Always, notificationOption)
                            : new CodeStyleOption2<PreferBracesPreference>(PreferBracesPreference.None, notificationOption);
                    }
                }

                if (value == "when_multiline")
                {
                    return new CodeStyleOption2<PreferBracesPreference>(PreferBracesPreference.WhenMultiline, notificationOption);
                }
            }

            return defaultValue;
        }

        private static string GetPreferBracesPreferenceEditorConfigString(CodeStyleOption2<PreferBracesPreference> value)
        {
            var notificationString = value.Notification.ToEditorConfigString();
            return value.Value switch
            {
                PreferBracesPreference.None => $"false:{notificationString}",
                PreferBracesPreference.WhenMultiline => $"when_multiline:{notificationString}",
                PreferBracesPreference.Always => $"true:{notificationString}",
                _ => throw ExceptionUtilities.Unreachable,
            };
        }
    }
}
