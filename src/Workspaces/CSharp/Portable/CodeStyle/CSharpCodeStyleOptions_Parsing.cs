// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static partial class CSharpCodeStyleOptions
    {
        public static CodeStyleOption<ExpressionBodyPreference> ParseExpressionBodyPreference(
            string optionString, CodeStyleOption<ExpressionBodyPreference> @default)
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
                            ? new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, notificationOpt)
                            : new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.Never, notificationOpt);
                    }

                    if (value == "when_on_single_line")
                    {
                        return new CodeStyleOption<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, notificationOpt);
                    }
                }
            }

            return @default;
        }

        private static string GetExpressionBodyPreferenceEditorConfigString(CodeStyleOption<ExpressionBodyPreference> value)
        {
            Debug.Assert(value.Notification != null);

            var notificationString = value.Notification.ToEditorConfigString();
            return value.Value switch
            {
                ExpressionBodyPreference.Never => $"false:{notificationString}",
                ExpressionBodyPreference.WhenPossible => $"true:{notificationString}",
                ExpressionBodyPreference.WhenOnSingleLine => $"when_on_single_line:{notificationString}",
                _ => throw new NotSupportedException(),
            };
        }

        public static CodeStyleOption<AddImportPlacement> ParseUsingDirectivesPlacement(
            string optionString, CodeStyleOption<AddImportPlacement> @default)
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
                        "inside_namespace" => new CodeStyleOption<AddImportPlacement>(AddImportPlacement.InsideNamespace, notificationOpt),
                        "outside_namespace" => new CodeStyleOption<AddImportPlacement>(AddImportPlacement.OutsideNamespace, notificationOpt),
                        _ => throw new NotSupportedException(),
                    };
                }
            }

            return @default;
        }

        public static string GetUsingDirectivesPlacementEditorConfigString(CodeStyleOption<AddImportPlacement> value)
        {
            Debug.Assert(value.Notification != null);

            var notificationString = value.Notification.ToEditorConfigString();
            return value.Value switch
            {
                AddImportPlacement.InsideNamespace => $"inside_namespace:{notificationString}",
                AddImportPlacement.OutsideNamespace => $"outside_namespace:{notificationString}",
                _ => throw new NotSupportedException(),
            };
        }

        private static CodeStyleOption<PreferBracesPreference> ParsePreferBracesPreference(
            string optionString,
            CodeStyleOption<PreferBracesPreference> defaultValue)
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
                            ? new CodeStyleOption<PreferBracesPreference>(PreferBracesPreference.Always, notificationOption)
                            : new CodeStyleOption<PreferBracesPreference>(PreferBracesPreference.None, notificationOption);
                    }
                }

                if (value == "when_multiline")
                {
                    return new CodeStyleOption<PreferBracesPreference>(PreferBracesPreference.WhenMultiline, notificationOption);
                }
            }

            return defaultValue;
        }

        private static string GetPreferBracesPreferenceEditorConfigString(CodeStyleOption<PreferBracesPreference> value)
        {
            Debug.Assert(value.Notification != null);

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
