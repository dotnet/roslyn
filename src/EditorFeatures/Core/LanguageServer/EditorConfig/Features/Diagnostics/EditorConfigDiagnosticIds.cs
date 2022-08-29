// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.EditorConfig.Features
{
    internal class EditorConfigDiagnosticIds
    {
        public const string IncorrectSettingDefinition = "EC0001";
        public const string SettingNotFound = "EC0002";
        public const string ValueNotDefinedInSetting = "EC0003";
        public const string SeveritiesNotSupported = "EC0004";
        public const string MultipleValuesNotSupported = "EC0005";
        public const string SeverityNotDefined = "EC0007";
        public const string SettingAlreadyDefined = "EC0009";
        public const string ValueAlreadyAssigned = "EC0010";

        public static string GetMessageFromId(string id, string settingName, string settingValue)
        {
            return id switch
            {
                IncorrectSettingDefinition => "Incorrect setting definition",
                SettingNotFound => $"{settingName} is not defined",
                ValueNotDefinedInSetting => $"{settingName} does not support value {settingValue}",
                SeveritiesNotSupported => $"{settingName} does not support severities",
                MultipleValuesNotSupported => $"{settingName} does not support multiple values",
                SeverityNotDefined => "Severity not defined",
                SettingAlreadyDefined => $"{settingName} is already defined",
                ValueAlreadyAssigned => $"{settingValue} is already assigned to {settingName}",
                _ => "Error code not defined",
            };
        }

        public static string GetCategoryFromId(string id)
        {
            return id switch
            {
                IncorrectSettingDefinition => "Syntax",
                SettingNotFound => "Semantic",
                ValueNotDefinedInSetting => "Semantic",
                SeveritiesNotSupported => "Semantic",
                MultipleValuesNotSupported => "Semantic",
                SeverityNotDefined => "Semantic",
                SettingAlreadyDefined => "Semantic",
                ValueAlreadyAssigned => "Semantic",
                _ => "Error code not defined",
            };
        }
    }
}
