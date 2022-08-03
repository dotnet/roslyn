// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.Whitespace
{
    internal partial class CSharpWhitespaceSettingsProvider
    {
        internal ImmutableDictionary<string, string> SpacingAroundBinaryOperatorValuesDescription = new Dictionary<string, string>()
        {
            { "none", EditorFeaturesResources.Spacing_Around_Binary_Operator_None },
            { "ignore", EditorFeaturesResources.Spacing_Around_Binary_Operator_Ignore },
            { "before_and_after", EditorFeaturesResources.Spacing_Around_Binary_Operator_Before_And_After },
        }.ToImmutableDictionary();

        internal ImmutableDictionary<string, string> LabelPositioningValuesDescription = new Dictionary<string, string>()
        {
            { "flush_left", EditorFeaturesResources.Label_Positioning_Flush_Left },
            { "no_change", EditorFeaturesResources.Label_Positioning_No_Change },
            { "one_less_than_current", EditorFeaturesResources.Label_Positioning_One_Less_Than_Current },
        }.ToImmutableDictionary();
    }
}
