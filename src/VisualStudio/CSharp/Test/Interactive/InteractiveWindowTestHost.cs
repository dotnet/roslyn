// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive
{
    public sealed class InteractiveWindowTestHost : IDisposable
    {
        internal readonly IInteractiveWindow Window;
        internal readonly TestInteractiveEvaluator Evaluator;

        private readonly System.ComponentModel.Composition.Hosting.ExportProvider _exportProvider;

        internal static readonly IExportProviderFactory ExportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(
            ExportProviderCache.GetOrCreateAssemblyCatalog(
                new[]
                {
                    typeof(TestWaitIndicator).Assembly,
                    typeof(TestInteractiveEvaluator).Assembly,
                    typeof(IInteractiveWindow).Assembly
                })
                .WithParts(TestExportProvider.GetCSharpAndVisualBasicAssemblyCatalog())
                .WithParts(MinimalTestExportProvider.GetEditorAssemblyCatalog()));

        internal InteractiveWindowTestHost(ExportProvider exportProvider)
        {
            _exportProvider = exportProvider.AsExportProvider();

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
