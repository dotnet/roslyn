﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditorConfigSettings.Data
{
    internal interface IEditorConfigSettingInfo
    {
        /// <summary>
        /// Gets the editorconfig setting name, returns null when the settings provider does not contain an editorconfig setting.
        /// </summary>
        string? GetSettingName();

        /// <summary>
        /// Gets the description of the editorconfig setting.
        /// </summary>
        string GetDocumentation();

        /// <summary>
        /// Gets the possible values for the editorconfig setting, returns null if there are no possible values or if it couldn't find the setting.
        /// </summary>
        ImmutableArray<string>? GetSettingValues();

        /// <summary>
        /// Gets the description of editorconfig setting values, returns null if the value doesn't have a description.
        /// </summary>
        string? GetValueDocumentation(string value);

        /// <summary>
        /// Returns if the string is a valid value for the setting
        /// </summary>
        bool IsValueValid(string value);

        /// <summary>
        /// Returns true if the setting supports severities definition, returns false otherwise.
        /// </summary>
        bool SupportsSeverities();

        /// <summary>
        /// Returns wether the setting can have multiple values.
        /// </summary>
        bool AllowsMultipleValues();
    }
}
