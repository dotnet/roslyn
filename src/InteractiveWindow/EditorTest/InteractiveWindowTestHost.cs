// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using Microsoft.VisualStudio.Language.Intellisense.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class InteractiveWindowTestHost : IDisposable
    {
        private readonly IInteractiveWindow _window;
        private readonly CompositionContainer _exportProvider;

        private static readonly Lazy<AggregateCatalog> _lazyCatalog = new Lazy<AggregateCatalog>(() =>
        {
            var types = new[] { typeof(TestInteractiveEngine), typeof(InteractiveWindow) }.Concat(GetVisualStudioTypes());
            return new AggregateCatalog(types.Select(t => new AssemblyCatalog(t.Assembly)));
        });

        internal InteractiveWindowTestHost(Action<InteractiveWindow.State> stateChangedHandler = null)
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());

            _exportProvider = new CompositionContainer(
                _lazyCatalog.Value,
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

            var contentTypeRegistryService = _exportProvider.GetExport<IContentTypeRegistryService>().Value;
            _window = _exportProvider.GetExport<IInteractiveWindowFactoryService>().Value.CreateWindow(new TestInteractiveEngine(contentTypeRegistryService));
            ((InteractiveWindow)_window).StateChanged += stateChangedHandler;
            _window.InitializeAsync().PumpingWait();
        }

        public CompositionContainer ExportProvider
        {
            get
            {
                return _exportProvider;
            }
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

        internal IInteractiveWindow Window
        {
            get
            {
                return _window;
            }
        }

        public void Dispose()
        {
            if (_window != null)
            {
                // close interactive host process:
                var engine = _window.Evaluator;
                if (engine != null)
                {
                    engine.Dispose();
                }

                // dispose buffer:
                _window.Dispose();
            }
        }
    }
}
