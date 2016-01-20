// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive
{
    public sealed class InteractiveWindowTestHost : IDisposable
    {
        internal readonly IInteractiveWindow Window;
        internal readonly TestInteractiveEvaluator Evaluator;

        private readonly CompositionContainer ExportProvider;

        private static readonly Lazy<AggregateCatalog> _lazyCatalog = new Lazy<AggregateCatalog>(() =>
        {
            var types = new[] { typeof(TestWaitIndicator), typeof(TestInteractiveEvaluator), typeof(InteractiveWindow) }.Concat(GetVisualStudioTypes());
            return new AggregateCatalog(types.Select(t => new AssemblyCatalog(t.Assembly)));
        });

        internal InteractiveWindowTestHost(Action<InteractiveWindow.State> stateChangedHandler = null)
        {
            ExportProvider = new CompositionContainer(
                _lazyCatalog.Value,
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

            var contentTypeRegistryService = ExportProvider.GetExport<IContentTypeRegistryService>().Value;
            Evaluator = new TestInteractiveEvaluator();
            Window = ExportProvider.GetExport<IInteractiveWindowFactoryService>().Value.CreateWindow(Evaluator);
            ((InteractiveWindow)Window).StateChanged += stateChangedHandler;
            Window.InitializeAsync().Wait();
        }

        private static Type[] GetVisualStudioTypes()
        {
            var types = new[]
            {
                // EDITOR

                // Microsoft.VisualStudio.Platform.VSEditor.dll:
                typeof(VisualStudio.Platform.VSEditor.EventArgsHelper),

                // Microsoft.VisualStudio.Text.Logic.dll:
                //   Must include this because several editor options are actually stored as exported information 
                //   on this DLL.  Including most importantly, the tab size information.
                typeof(VisualStudio.Text.Editor.DefaultOptions),

                // Microsoft.VisualStudio.Text.UI.dll:
                //   Include this DLL to get several more EditorOptions including WordWrapStyle.
                typeof(VisualStudio.Text.Editor.WordWrapStyle),

                // Microsoft.VisualStudio.Text.UI.Wpf.dll:
                //   Include this DLL to get more EditorOptions values.
                typeof(VisualStudio.Text.Editor.HighlightCurrentLineOption),

                // BasicUndo.dll:
                //   Include this DLL to satisfy ITextUndoHistoryRegistry
                typeof(BasicUndo.IBasicUndoHistory),

                // Microsoft.VisualStudio.Language.StandardClassification.dll:
                typeof(VisualStudio.Language.StandardClassification.PredefinedClassificationTypeNames)
            };

            return types;
        }

        public void Dispose()
        {
            if (Window != null)
            {
                // close interactive host process:
                Window.Evaluator?.Dispose();

                // dispose buffer:
                Window.Dispose();
            }
        }
    }
}
