// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;

[Export(typeof(EditorFormatDefinition))]
[Name(ActiveStatementTag.TagId)]
[UserVisible(true)]
[ExcludeFromCodeCoverage]
internal sealed class ActiveStatementTagFormatDefinition : MarkerFormatDefinition
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ActiveStatementTagFormatDefinition()
    {
        // TODO (tomat): bug 777271
        // Should we reuse an existing marker for read only regions?
        // MARKER_READONLY
        // { L"Read-Only Region", IDS_MDN_READONLY, MV_COLOR_ALWAYS, LI_NONE, CI_BLACK, RGB(255, 255, 255), FALSE, CI_LIGHTGRAY, RGB(238, 239, 230), TRUE, DrawNoGlyph, MB_INHERIT_FOREGROUND, 0}

        this.BackgroundColor = Colors.Silver;
        this.DisplayName = EditorFeaturesResources.Active_Statement;
        this.ZOrder = -1;
    }
}
