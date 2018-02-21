// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive
{
    public sealed class InteractiveWindowTestHost : IDisposable
    {
        internal readonly IInteractiveWindow Window;
        internal readonly TestInteractiveEvaluator Evaluator;

        private readonly CompositionContainer _exportProvider;

        private static readonly Lazy<AggregateCatalog> s_lazyCatalog = new Lazy<AggregateCatalog>(() =>
        {
            var assemblies = new[] 
            {
                typeof(TestWaitIndicator).Assembly,
                typeof(TestInteractiveEvaluator).Assembly,
                typeof(IInteractiveWindow).Assembly
            }.Concat(MinimalTestExportProvider.GetEditorAssemblies());
            return new AggregateCatalog(assemblies.Select(a => new AssemblyCatalog(a)));
        });

        internal InteractiveWindowTestHost()
        {
            _exportProvider = new CompositionContainer(
                s_lazyCatalog.Value,
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

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
