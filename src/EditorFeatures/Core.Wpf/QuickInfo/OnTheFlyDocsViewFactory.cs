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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Internal.Log;
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
        private readonly IThreadingContext _threadingContext;
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnTheFlyDocsViewFactory(IViewElementFactoryService factoryService, IThreadingContext threadingContext)
        {
            _factoryService = factoryService;
            _threadingContext = threadingContext;
        }

        public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            if (typeof(TView) != typeof(UIElement))
            {
                throw new InvalidOperationException("TView must be UIElement");
            }
            if (model is not OnTheFlyDocsElement onTheFlyDocsElement)
            {
                throw new InvalidOperationException("model must be an OnTheFlyDocsElement");
            }

            Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Showed_Link, KeyValueLogMessage.Create(m =>
            {
                m["SymbolText"] = onTheFlyDocsElement.DescriptionText;
            }, LogLevel.Information));

            return new OnTheFlyDocsView(textView, _factoryService, _threadingContext, onTheFlyDocsElement.Document, onTheFlyDocsElement.Symbol, onTheFlyDocsElement.DescriptionText, onTheFlyDocsElement.CancellationToken) as TView;
        }
    }
}
