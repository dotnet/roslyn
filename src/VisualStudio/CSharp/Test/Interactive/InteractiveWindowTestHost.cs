// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
