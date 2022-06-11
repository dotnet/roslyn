// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;

namespace IdeBenchmarks.InheritanceMargin
{
    [MemoryDiagnoser]
    [Orderer(methodOrderPolicy: MethodOrderPolicy.Declared)]
    public class InheritanceMarginGlyphBenchmarks
    {
        private const int MemberCount = 500;
        private const int Iterations = 10;
        private const double WidthAndHeightOfGlyph = 18;

        // The test sample file would contains a pair of implemented + implmenting members and the tag for containing interface/class
        private const double HeightOfCanvas = WidthAndHeightOfGlyph * (MemberCount * 2 + 2);

        private readonly UseExportProviderAttribute _useExportProviderAttribute = new();
        private readonly IWpfTextView _mockTextView;

        /// <summary>
        /// The WPF application that is shared by all the benchmarks. This is static because WPF only allows one application per AppDomain.
        /// </summary>
        private static Application? s_wpfApp;
        private ImmutableArray<InheritanceMarginTag> _tags;
        private IThreadingContext _threadingContext;
        private IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private ClassificationTypeMap _classificationTypeMap;
        private IClassificationFormatMap _classificationFormatMap;
        private IUIThreadOperationExecutor _operationExecutor;
        private IAsynchronousOperationListener _listener;

        public InheritanceMarginGlyphBenchmarks()
        {
            _threadingContext = null!;
            _streamingFindUsagesPresenter = null!;
            _classificationTypeMap = null!;
            _classificationFormatMap = null!;
            _operationExecutor = null!;
            _listener = null!;
            var mockTextView = new Mock<IWpfTextView>();
            mockTextView.Setup(textView => textView.ZoomLevel).Returns(100);
            _mockTextView = mockTextView.Object;
        }

        // Note: Make sure this is targeting the first declarated benchmark method so that the test Application so that
        // the test Application could be created.
        [GlobalSetup(Target = nameof(GlyphRefreshBaseline))]
        public Task SetupAsync()
        {
            return SetupWpfApplicaitonAsync();
        }

        // Note: Make sure this is targeting the last declarated benchmark method so that the test Application
        // could be shutdown.
        [GlobalCleanup(Target = nameof(BenchmarkGlyphRefresh))]
        public void Cleanup()
        {
            RunOnUIThread(() => s_wpfApp!.Shutdown());
        }

        [IterationSetup]
        public Task IterationSetupAsync()
        {
            _useExportProviderAttribute.Before(null);
            return PrepareGlyphRequiredDataAsync(CancellationToken.None);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _useExportProviderAttribute.After(null);
        }

        [Benchmark(Baseline = true)]
        public void GlyphRefreshBaseline()
        {
            RunOnUIThread(() =>
            {
                var canvas = (Canvas)s_wpfApp!.MainWindow.Content;
                for (var i = 0; i < Iterations; i++)
                {
                    for (var j = 0; j < _tags.Length; j++)
                    {
                        var tag = _tags[j];
                    }
                }
            });
        }

        [Benchmark]
        public void BenchmarkGlyphRefresh()
        {
            RunOnUIThread(() =>
            {
                var canvas = (Canvas)s_wpfApp!.MainWindow.Content;
                for (var i = 0; i < Iterations; i++)
                {
                    // Add & remove glyphs from the Canvas, which simulates the real refreshing scenanrio when user is scrolling up/down.
                    for (var j = 0; j < _tags.Length; j++)
                    {
                        var tag = _tags[j];
                        var glyph = new InheritanceMarginGlyph(
                            _threadingContext,
                            _streamingFindUsagesPresenter,
                            _classificationTypeMap,
                            _classificationFormatMap,
                            _operationExecutor,
                            tag,
                            _mockTextView,
                            _listener);
                        Canvas.SetTop(glyph, j * WidthAndHeightOfGlyph);
                        canvas.Children.Add(glyph);
                    }

                    canvas.Measure(new Size(WidthAndHeightOfGlyph, HeightOfCanvas));
                    canvas.Children.Clear();
                }
            });
        }

        private async Task PrepareGlyphRequiredDataAsync(CancellationToken cancellationToken)
        {
            var testFile = CreateTestFile();
            using var workspace = TestWorkspace.CreateCSharp(testFile);
            var items = await BenchmarksHelpers.GenerateInheritanceMarginItemsAsync(workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);

            using var _ = Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<InheritanceMarginTag>.GetInstance(out var builder);
            foreach (var grouping in items.GroupBy(i => i.LineNumber))
            {
                builder.Add(new InheritanceMarginTag(workspace, grouping.Key, grouping.ToImmutableArray()));
            }

            _tags = builder.ToImmutableArray();
            var exportProvider = workspace.ExportProvider;
            _threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
            _streamingFindUsagesPresenter = exportProvider.GetExportedValue<IStreamingFindUsagesPresenter>();
            _classificationTypeMap = exportProvider.GetExportedValue<ClassificationTypeMap>();
            var classificationFormatMapService = exportProvider.GetExportedValue<IClassificationFormatMapService>();
            _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("tooltip");
            _operationExecutor = exportProvider.GetExportedValue<IUIThreadOperationExecutor>();
            var listenerProvider = exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();
            _listener = listenerProvider.GetListener(FeatureAttribute.InheritanceMargin);
        }

        private void RunOnUIThread(Action action)
        {
#pragma warning disable VSTHRD001 // Only used for Benchmark purpose
            s_wpfApp!.Dispatcher.Invoke(() =>
#pragma warning restore VSTHRD001
            {
                action?.Invoke();
            });
        }

        private Task SetupWpfApplicaitonAsync()
        {
            if (Application.Current == null)
            {
                var tcs = new TaskCompletionSource<bool>();
                var mainThread = new Thread(() =>
                {
                    s_wpfApp = new Application
                    {
                        MainWindow = new Window(),
                        ShutdownMode = ShutdownMode.OnExplicitShutdown
                    };

                    s_wpfApp.Startup += (sender, args) =>
                    {
                        tcs.SetResult(true);
                    };

                    s_wpfApp.MainWindow.Content = new Canvas()
                    {
                        ClipToBounds = true,
                        Width = WidthAndHeightOfGlyph,
                        Height = HeightOfCanvas
                    };
                    s_wpfApp.Run();
                });

                mainThread.SetApartmentState(ApartmentState.STA);
                mainThread.Start();
                return tcs.Task;
            }

            s_wpfApp = Application.Current;
            return Task.CompletedTask;
        }

        private static string CreateTestFile()
        {
            var builder = new StringBuilder();
            builder.Append(@"using System;");
            builder.Append(Environment.NewLine);
            builder.Append(@"namespace TestNs
{
");
            builder.Append(@"   public interface IBar
    {
");
            for (var i = 0; i < MemberCount; i++)
            {
                builder.Append($"       int Method{i}();");
                builder.Append(Environment.NewLine);
            }

            builder.Append(@"   }
");

            builder.Append(@"       public class Bar : IBar
    {
");
            for (var i = 0; i < MemberCount; i++)
            {
                builder.Append($"       public int Method{i}() => 1");
                builder.Append(Environment.NewLine);
            }

            builder.Append(@"   }
");

            builder.Append('}');
            return builder.ToString();
        }
    }
}
