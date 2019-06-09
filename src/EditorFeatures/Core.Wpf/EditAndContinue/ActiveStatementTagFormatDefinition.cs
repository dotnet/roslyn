// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(ActiveStatementTag.TagId)]
    [UserVisible(true)]
    [ExcludeFromCodeCoverage]
    internal sealed class ActiveStatementTagFormatDefinition : MarkerFormatDefinition
    {
        [ImportingConstructor]
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
}
