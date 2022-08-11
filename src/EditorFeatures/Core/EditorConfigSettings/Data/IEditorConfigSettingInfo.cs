// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Options;

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
        ImmutableArray<string>? GetSettingValues(OptionSet optionSet);

        /// <summary>
        /// Returns true if the setting supports severities definition, returns false otherwise.
        /// </summary>
        bool SupportsSeverities();
    }
}
