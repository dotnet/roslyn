// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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

        public static CodeStyleOption<UsingPlacementPreference> ParseUsingPlacementPreference(
            string optionString, CodeStyleOption<UsingPlacementPreference> @default)
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
                        case "no_preference":
                            return new CodeStyleOption<UsingPlacementPreference>(UsingPlacementPreference.NoPreference, notificationOpt);
                        case "inside_namespace":
                            return new CodeStyleOption<UsingPlacementPreference>(UsingPlacementPreference.InsideNamespace, notificationOpt);
                        case "outside_namespace":
                            return new CodeStyleOption<UsingPlacementPreference>(UsingPlacementPreference.OutsideNamespace, notificationOpt);
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            return @default;
        }

        public static string GetUsingPlacementPreferenceEditorConfigString(CodeStyleOption<UsingPlacementPreference> value)
        {
            Debug.Assert(value.Notification != null);

            var notificationString = value.Notification.ToEditorConfigString();
            switch (value.Value)
            {
                case UsingPlacementPreference.NoPreference: return $"no_preference:{notificationString}";
                case UsingPlacementPreference.InsideNamespace: return $"inside_namespace:{notificationString}";
                case UsingPlacementPreference.OutsideNamespace: return $"outside_namespace:{notificationString}";
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
