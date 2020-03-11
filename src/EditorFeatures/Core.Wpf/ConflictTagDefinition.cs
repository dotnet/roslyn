﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging.Tags
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(ConflictTag.TagId)]
    [UserVisible(true)]
    [ExcludeFromCodeCoverage]
    internal class ConflictTagDefinition : MarkerFormatDefinition
    {
        [ImportingConstructor]
        public ConflictTagDefinition()
        {
            this.Border = new Pen(Brushes.Red, thickness: 1.5);
            this.DisplayName = EditorFeaturesResources.Conflict;
            this.ZOrder = 10;
        }
    }
}
