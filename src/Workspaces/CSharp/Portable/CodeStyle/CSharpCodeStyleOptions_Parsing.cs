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
            switch (value.Value)
            {
                case ExpressionBodyPreference.Never: return $"false:{notificationString}";
                case ExpressionBodyPreference.WhenPossible: return $"true:{notificationString}";
                case ExpressionBodyPreference.WhenOnSingleLine: return $"when_on_single_line:{notificationString}";
                default:
                    throw new NotSupportedException();
            }
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
                    switch (value)
                    {
                        case "inside_namespace":
                            return new CodeStyleOption<AddImportPlacement>(AddImportPlacement.InsideNamespace, notificationOpt);
                        case "outside_namespace":
                            return new CodeStyleOption<AddImportPlacement>(AddImportPlacement.OutsideNamespace, notificationOpt);
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            return @default;
        }

        public static string GetUsingDirectivesPlacementEditorConfigString(CodeStyleOption<AddImportPlacement> value)
        {
            Debug.Assert(value.Notification != null);

            var notificationString = value.Notification.ToEditorConfigString();
            switch (value.Value)
            {
                case AddImportPlacement.InsideNamespace: return $"inside_namespace:{notificationString}";
                case AddImportPlacement.OutsideNamespace: return $"outside_namespace:{notificationString}";
                default:
                    throw new NotSupportedException();
            }
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
            switch (value.Value)
            {
                case PreferBracesPreference.None:
                    return $"false:{notificationString}";

                case PreferBracesPreference.WhenMultiline:
                    return $"when_multiline:{notificationString}";

                case PreferBracesPreference.Always:
                    return $"true:{notificationString}";

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
