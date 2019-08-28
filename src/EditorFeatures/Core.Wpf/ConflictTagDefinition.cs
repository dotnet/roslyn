// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
