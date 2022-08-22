// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Whitespace
{
    internal partial class WhitespaceSettingsValuesDescriptions
    {
        internal ImmutableDictionary<string, string> OperatorPlacementWhenWrappingValuesDescription = new Dictionary<string, string>()
        {
            { "beginning_of_line", EditorFeaturesResources.Operator_Placement_When_Wrapping_Beginning_Of_Line },
            { "end_of_line", EditorFeaturesResources.Operator_Placement_When_Wrapping_End_Of_Line },
        }.ToImmutableDictionary();
    }
}
