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
            string optionString, CodeStyleOption2<ExpressionBodyPreference> @default, Func<string, Optional<ExpressionBodyPreference>> editorConfigDataParse)
        {
            // optionString must be similar to true:error or when_on_single_line:suggestion.
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(optionString,
                    @default.Notification, out var value, out var notification))
            {
                var expressionBodyPreference = editorConfigDataParse(value).Value;
                return new CodeStyleOption2<ExpressionBodyPreference>(expressionBodyPreference, notification);
            }

            return @default;
        }

        private static string GetExpressionBodyPreferenceEditorConfigString(CodeStyleOption2<ExpressionBodyPreference> value, CodeStyleOption2<ExpressionBodyPreference> defaultValue, Func<ExpressionBodyPreference, string> editorConfigDataParse)
        {
            var notificationString = CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue);
            var editorConfigString = editorConfigDataParse(value.Value);
            return editorConfigString == "" ? throw new NotSupportedException() : $"{editorConfigString}{notificationString}";
        }

        public static CodeStyleOption2<AddImportPlacement> ParseUsingDirectivesPlacement(
            string optionString, CodeStyleOption2<AddImportPlacement> @default, Func<string, Optional<AddImportPlacement>> editorConfigDataParse)
        {
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                    optionString, @default.Notification, out var value, out var notification))
            {
                var addImportPlacement = editorConfigDataParse(value).Value;
                return new CodeStyleOption2<AddImportPlacement>(addImportPlacement, notification);
            }

            return @default;
        }

        public static string GetUsingDirectivesPlacementEditorConfigString(CodeStyleOption2<AddImportPlacement> value, CodeStyleOption2<AddImportPlacement> defaultValue, Func<AddImportPlacement, string> editorConfigDataParse)
        {
            var notificationString = CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue);
            var editorConfigString = editorConfigDataParse(value.Value);
            return editorConfigString == "" ? throw new NotSupportedException() : $"{editorConfigString}{notificationString}";
        }

        public static CodeStyleOption2<NamespaceDeclarationPreference> ParseNamespaceDeclaration(
            string optionString, CodeStyleOption2<NamespaceDeclarationPreference> @default, Func<string, Optional<NamespaceDeclarationPreference>> editorConfigDataParse)
        {
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                    optionString, @default.Notification, out var value, out var notification))
            {
                var namespaceDeclarationPreference = editorConfigDataParse(value).Value;
                return new CodeStyleOption2<NamespaceDeclarationPreference>(namespaceDeclarationPreference, notification);
            }

            return @default;
        }

        public static string GetNamespaceDeclarationEditorConfigString(CodeStyleOption2<NamespaceDeclarationPreference> value, CodeStyleOption2<NamespaceDeclarationPreference> defaultValue, Func<NamespaceDeclarationPreference, string> editorConfigDataParse)
        {
            var notificationString = CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue);
            var editorConfigString = editorConfigDataParse(value.Value);
            return editorConfigString == "" ? throw new NotSupportedException() : $"{editorConfigString}{notificationString}";
        }

        private static CodeStyleOption2<PreferBracesPreference> ParsePreferBracesPreference(
            string optionString,
            CodeStyleOption2<PreferBracesPreference> defaultValue, Func<string, Optional<PreferBracesPreference>> editorConfigDataParse)
        {
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(optionString,
                    defaultValue.Notification, out var value, out var notification))
            {
                var oreferBracesPreference = editorConfigDataParse(value).Value;
                return new CodeStyleOption2<PreferBracesPreference>(oreferBracesPreference, notification);
            }

            return defaultValue;
        }

        private static string GetPreferBracesPreferenceEditorConfigString(CodeStyleOption2<PreferBracesPreference> value, CodeStyleOption2<PreferBracesPreference> defaultValue, Func<PreferBracesPreference, string> editorConfigDataParse)
        {
            var notificationString = CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue);
            var editorConfigString = editorConfigDataParse(value.Value);
            return editorConfigString == "" ? throw ExceptionUtilities.Unreachable : $"{editorConfigString}{notificationString}";
        }
    }
}
