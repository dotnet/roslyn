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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
    public class InheritanceMarginGlyphBenchmarks
    {
        private const int MemberCount = 500;
        private const int Iterations = 10;
        private const double WidthAndHeightOfGlyph = 18;

        // The test sample file would contains a pair of implemented + implmenting members and the tag for containing interface/class
        private const double HeightOfCanvas = WidthAndHeightOfGlyph * (MemberCount * 2 + 2);

        private readonly UseExportProviderAttribute _useExportProviderAttribute = new();
        private readonly IWpfTextView _mockTextView;
        private readonly Workspace _workspace;

        private Application _wpfApp;
        private Canvas _canvas;
        private ImmutableArray<InheritanceMarginTag> _tags;
        private IThreadingContext _threadingContext;
        private IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private ClassificationTypeMap _classificationTypeMap;
        private IClassificationFormatMap _classificationFormatMap;
        private IUIThreadOperationExecutor _operationExecutor;
        private IAsynchronousOperationListener _listener;

        public InheritanceMarginGlyphBenchmarks()
        {
            _wpfApp = null!;
            _threadingContext = null!;
            _streamingFindUsagesPresenter = null!;
            _classificationTypeMap = null!;
            _classificationFormatMap = null!;
            _operationExecutor = null!;
            _listener = null!;
            _canvas = null!;
            _workspace = null!;
            var mockTextView = new Mock<IWpfTextView>();
            mockTextView.Setup(textView => textView.ZoomLevel).Returns(100);
            _mockTextView = mockTextView.Object;
        }

        [GlobalSetup]
        public Task SetupAsync()
        {
            // Note: WPF only allows one application per appDomain, and benchmark.net
            // would run all this method for all the Benchmark method within the class.
            return SetupWpfApplicaitonAsync();
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

        [GlobalCleanup]
        public void Cleanup()
        {
            RunOnUIThread(() => _wpfApp.Shutdown());
        }

        [Benchmark]
        public void BenchmarkGlyphRefresh()
        {
            RunOnUIThread(() =>
            {
                for (var i = 0; i < Iterations; i++)
                {
                    // Add & remove glyphs from the Canvas, which simulates the real refreshing scenanrio when user is scrolling up/down.
                    for (var j = 0; j < _tags.Length; j++)
                    {
                        var tag = _tags[j];
                        var glyph = new InheritanceMarginGlyph(
                            _workspace,
                            _threadingContext,
                            _streamingFindUsagesPresenter,
                            _classificationTypeMap,
                            _classificationFormatMap,
                            _operationExecutor,
                            tag,
                            _mockTextView,
                            _listener);
                        Canvas.SetTop(glyph, j * WidthAndHeightOfGlyph);
                        _canvas.Children.Add(glyph);
                    }
                    _canvas.Measure(new Size(WidthAndHeightOfGlyph, HeightOfCanvas));
                    _canvas.Children.Clear();
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
                builder.Add(new InheritanceMarginTag(grouping.Key, grouping.ToImmutableArray()));
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
            _wpfApp.Dispatcher.Invoke(() =>
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
                    _wpfApp = new Application();
                    _wpfApp.MainWindow = new Window();
                    _wpfApp.Startup += (sender, args) =>
                    {
                        tcs.SetResult(true);
                    };

                    _canvas = new Canvas()
                    {
                        ClipToBounds = true,
                        Width = WidthAndHeightOfGlyph,
                        Height = HeightOfCanvas
                    };

                    _wpfApp.MainWindow.Content = _canvas;
                    _wpfApp.Run();
                });

                mainThread.SetApartmentState(ApartmentState.STA);
                mainThread.Start();
                return tcs.Task;
            }

            _wpfApp = Application.Current;
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
