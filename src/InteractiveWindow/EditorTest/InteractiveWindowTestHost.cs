// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public sealed class InteractiveWindowTestHost : IDisposable
    {
        internal readonly IInteractiveWindow Window;
        internal readonly CompositionContainer ExportProvider;
        internal readonly TestInteractiveEngine Evaluator;

        private static readonly Lazy<AggregateCatalog> s_lazyCatalog = new Lazy<AggregateCatalog>(() =>
        {
            var types = new[] { typeof(TestInteractiveEngine), typeof(InteractiveWindow) }.Concat(GetVisualStudioTypes());
            return new AggregateCatalog(types.Select(t => new AssemblyCatalog(t.Assembly)));
        });

        internal InteractiveWindowTestHost(Action<InteractiveWindow.State> stateChangedHandler = null)
        {
            ExportProvider = new CompositionContainer(
                s_lazyCatalog.Value,
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

            var contentTypeRegistryService = ExportProvider.GetExport<IContentTypeRegistryService>().Value;
            Evaluator = new TestInteractiveEngine(contentTypeRegistryService);
            Window = ExportProvider.GetExport<IInteractiveWindowFactoryService>().Value.CreateWindow(Evaluator);
            ((InteractiveWindow)Window).StateChanged += stateChangedHandler;
            Window.InitializeAsync().Wait();
        }

        public static Type[] GetVisualStudioTypes()
        {
            var types = new[]
            {
                // EDITOR

                // Microsoft.VisualStudio.Platform.VSEditor.dll:
                typeof(Microsoft.VisualStudio.Platform.VSEditor.EventArgsHelper),

                // Microsoft.VisualStudio.Text.Logic.dll:
                //   Must include this because several editor options are actually stored as exported information 
                //   on this DLL.  Including most importantly, the tab size information.
                typeof(Microsoft.VisualStudio.Text.Editor.DefaultOptions),

                // Microsoft.VisualStudio.Text.UI.dll:
                //   Include this DLL to get several more EditorOptions including WordWrapStyle.
                typeof(Microsoft.VisualStudio.Text.Editor.WordWrapStyle),

                // Microsoft.VisualStudio.Text.UI.Wpf.dll:
                //   Include this DLL to get more EditorOptions values.
                typeof(Microsoft.VisualStudio.Text.Editor.HighlightCurrentLineOption),

                // BasicUndo.dll:
                //   Include this DLL to satisfy ITextUndoHistoryRegistry
                typeof(BasicUndo.IBasicUndoHistory),

                // Microsoft.VisualStudio.Language.StandardClassification.dll:
                typeof(Microsoft.VisualStudio.Language.StandardClassification.PredefinedClassificationTypeNames)
            };

            return types;
        }

        public void Dispose()
        {
            if (Window != null)
            {
                // close interactive host process:
                var engine = Window.Evaluator;
                if (engine != null)
                {
                    engine.Dispose();
                }

                // dispose buffer:
                Window.Dispose();
            }
        }
    }
}
