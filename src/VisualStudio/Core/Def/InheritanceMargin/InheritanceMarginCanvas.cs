// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

namespace Microsoft.VisualStudio.LanguageServices.InheritanceMargin
{
    internal class InheritanceMarginCanvas : Canvas
    {
        public event EventHandler<(InheritanceMarginGlyph? glyphAdded, InheritanceMarginGlyph? glyphRemoved)>? OnGlyphsChanged;

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            OnGlyphsChanged?.Invoke(this, (visualAdded as InheritanceMarginGlyph, visualRemoved as InheritanceMarginGlyph));
        }
    }
}
