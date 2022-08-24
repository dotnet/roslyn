// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.AddImport;
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
                    @default.Notification, out var value, out var notification))
            {
                if (bool.TryParse(value, out var boolValue))
                {
                    return boolValue
                        ? new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, notification)
                        : new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, notification);
                }

                if (value == "when_on_single_line")
                {
                    return new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, notification);
                }
            }

            return @default;
        }

        private static string GetExpressionBodyPreferenceEditorConfigString(CodeStyleOption2<ExpressionBodyPreference> value, CodeStyleOption2<ExpressionBodyPreference> defaultValue)
        {
            var notificationString = CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue);
            return value.Value switch
            {
                ExpressionBodyPreference.Never => $"false{notificationString}",
                ExpressionBodyPreference.WhenPossible => $"true{notificationString}",
                ExpressionBodyPreference.WhenOnSingleLine => $"when_on_single_line{notificationString}",
                _ => throw new NotSupportedException(),
            };
        }

        public static CodeStyleOption2<AddImportPlacement> ParseUsingDirectivesPlacement(
            string optionString, CodeStyleOption2<AddImportPlacement> @default)
        {
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                    optionString, @default.Notification, out var value, out var notification))
            {
                return value switch
                {
                    "inside_namespace" => new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.InsideNamespace, notification),
                    "outside_namespace" => new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.OutsideNamespace, notification),
                    _ => throw new NotSupportedException(),
                };
            }

            return @default;
        }

        public static string GetUsingDirectivesPlacementEditorConfigString(CodeStyleOption2<AddImportPlacement> value, CodeStyleOption2<AddImportPlacement> defaultValue)
        {
            var notificationString = CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue);
            return value.Value switch
            {
                AddImportPlacement.InsideNamespace => $"inside_namespace{notificationString}",
                AddImportPlacement.OutsideNamespace => $"outside_namespace{notificationString}",
                _ => throw new NotSupportedException(),
            };
        }

        public static CodeStyleOption2<NamespaceDeclarationPreference> ParseNamespaceDeclaration(
            string optionString, CodeStyleOption2<NamespaceDeclarationPreference> @default)
        {
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                    optionString, @default.Notification, out var value, out var notification))
            {
                return value switch
                {
                    "block_scoped" => new(NamespaceDeclarationPreference.BlockScoped, notification),
                    "file_scoped" => new(NamespaceDeclarationPreference.FileScoped, notification),
                    _ => throw new NotSupportedException(),
                };
            }

            return @default;
        }

        public static string GetNamespaceDeclarationEditorConfigString(CodeStyleOption2<NamespaceDeclarationPreference> value, CodeStyleOption2<NamespaceDeclarationPreference> defaultValue)
        {
            var notificationString = CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue);
            return value.Value switch
            {
                NamespaceDeclarationPreference.BlockScoped => $"block_scoped{notificationString}",
                NamespaceDeclarationPreference.FileScoped => $"file_scoped{notificationString}",
                _ => throw new NotSupportedException(),
            };
        }

        private static CodeStyleOption2<PreferBracesPreference> ParsePreferBracesPreference(
            string optionString,
            CodeStyleOption2<PreferBracesPreference> defaultValue)
        {
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                optionString,
                defaultValue.Notification,
                out var value,
                out var notificationOption))
            {
                if (bool.TryParse(value, out var boolValue))
                {
                    return boolValue
                        ? new CodeStyleOption2<PreferBracesPreference>(PreferBracesPreference.Always, notificationOption)
                        : new CodeStyleOption2<PreferBracesPreference>(PreferBracesPreference.None, notificationOption);
                }

                if (value == "when_multiline")
                {
                    return new CodeStyleOption2<PreferBracesPreference>(PreferBracesPreference.WhenMultiline, notificationOption);
                }
            }

            return defaultValue;
        }

        private static string GetPreferBracesPreferenceEditorConfigString(CodeStyleOption2<PreferBracesPreference> value, CodeStyleOption2<PreferBracesPreference> defaultValue)
        {
            var notificationString = CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue);
            return value.Value switch
            {
                PreferBracesPreference.None => $"false{notificationString}",
                PreferBracesPreference.WhenMultiline => $"when_multiline{notificationString}",
                PreferBracesPreference.Always => $"true{notificationString}",
                _ => throw ExceptionUtilities.Unreachable,
            };
        }
    }
}
