// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Margin.InheritanceChainMargin
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name(nameof(InheritanceChainMarginFactoryProvider))]
    // TODO: Figure out the proper margin order
    // [Order(After = "VsTextMarker")]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class InheritanceChainMarginFactoryProvider : IGlyphFactoryProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InheritanceChainMarginFactoryProvider()
        {
        }

        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new InheritanceChainGlyphFactory();
        }
    }
}
