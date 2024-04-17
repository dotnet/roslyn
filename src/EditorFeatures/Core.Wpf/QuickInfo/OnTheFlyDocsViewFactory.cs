// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.IntelliSense.QuickInfo;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    [Export(typeof(IViewElementFactory))]
    [Name("My object converter")]
    [TypeConversion(from: typeof(OnTheFlyDocsElement), to: typeof(UIElement))]
    [Order(Before = "Default object converter")]
    internal class OnTheFlyDocsViewFactory : IViewElementFactory
    {
        private readonly IViewElementFactoryService _factoryService;
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnTheFlyDocsViewFactory(IViewElementFactoryService factoryService)
        {
            _factoryService = factoryService;
        }

        public TView CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            if (typeof(TView) != typeof(OnTheFlyDocsView))
            {
                throw new InvalidOperationException("TView must be OnTheFlyDocsView");
            }

            return new OnTheFlyDocsView(textView, _factoryService) as TView;
        }
    }
}
