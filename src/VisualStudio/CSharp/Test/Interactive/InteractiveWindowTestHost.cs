// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive
{
    public sealed class InteractiveWindowTestHost : IDisposable
    {
        internal readonly IInteractiveWindow Window;
        internal readonly TestInteractiveEvaluator Evaluator;

        private readonly System.ComponentModel.Composition.Hosting.ExportProvider _exportProvider;

        private static readonly Lazy<ComposableCatalog> s_lazyCatalog = new Lazy<ComposableCatalog>(() =>
        {
            var assemblies = new[] { typeof(TestWaitIndicator).Assembly, typeof(TestInteractiveEvaluator).Assembly, typeof(IInteractiveWindow).Assembly }
                .Concat(MinimalTestExportProvider.GetEditorAssemblies());
            return MinimalTestExportProvider.CreateAssemblyCatalog(assemblies);
        });


        // Provide an export of ILoggingServiceInternal to work around https://devdiv.visualstudio.com/DevDiv/_workitems/edit/570290
        [Export(typeof(ILoggingServiceInternal))]
        private sealed class HACK_LoggingProvider : ILoggingServiceInternal
        {
            public void AdjustCounter(string key, string name, int delta = 1)
            {
            }

            public void PostCounters()
            {
            }

            public void PostEvent(string key, params object[] namesAndProperties)
            {
            }

            public void PostEvent(string key, IReadOnlyList<object> namesAndProperties)
            {
            }

            public void PostEvent(TelemetryEventType eventType, string eventName, TelemetryResult result = TelemetryResult.Success, params (string name, object property)[] namesAndProperties)
            {
            }

            public void PostEvent(TelemetryEventType eventType, string eventName, TelemetryResult result, IReadOnlyList<(string name, object property)> namesAndProperties)
            {
            }

            public void PostFault(string eventName, string description, Exception exceptionObject, string additionalErrorInfo, bool? isIncludedInWatsonSample)
            {
            }
        }

        internal InteractiveWindowTestHost()
        {
            _exportProvider = MinimalTestExportProvider.CreateExportProvider(s_lazyCatalog.Value).AsExportProvider();

            var contentTypeRegistryService = _exportProvider.GetExport<IContentTypeRegistryService>().Value;
            Evaluator = new TestInteractiveEvaluator();
            Window = _exportProvider.GetExport<IInteractiveWindowFactoryService>().Value.CreateWindow(Evaluator);
            Window.InitializeAsync().Wait();
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
