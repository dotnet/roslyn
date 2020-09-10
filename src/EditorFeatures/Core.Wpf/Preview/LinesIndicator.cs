// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
#nullable enable

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    internal sealed class LinesIndicator
    {
        private readonly IAdornmentLayer _layer;
        private readonly IWpfTextView _view;
        private readonly IDifferenceViewer _viewer;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly IEditorFormatMap _editorFormatMap;
        private readonly IClassificationType _lineNumberClassification;
        private readonly LinesIndicatorTextViewCreationListener _factory;

        public LinesIndicator(IWpfTextView view, IDifferenceTextViewModel model, LinesIndicatorTextViewCreationListener factory)
        {
            _layer = view.GetAdornmentLayer(nameof(LinesIndicator));
            _view = view;
            _viewer = model.Viewer;
            _factory = factory;
            _classificationFormatMap = factory.ClassificationFormatMapService.GetClassificationFormatMap(view);
            _editorFormatMap = factory.EditorFormatMapService.GetEditorFormatMap(view);
            _lineNumberClassification = factory.ClassificationTypeRegistryService.GetClassificationType("line number");

            view.Closed += this.OnClosed;
            view.LayoutChanged += this.OnLayoutChanged;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.Closed -= this.OnClosed;
            _view.LayoutChanged -= this.OnLayoutChanged;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (var line in e.NewOrReformattedLines)
            {
                this.CreateVisuals(line);
            }
        }

        private void CreateVisuals(ITextViewLine line)
        {
            if (this.IsSpanAnInsertedElipse(line.Extent))
            {
                var snapshot = _viewer.DifferenceBuffer.CurrentSnapshotDifference;
                if ((snapshot != null) && (snapshot.InlineBufferSnapshot == _view.TextSnapshot))
                {
                    var leftProjectionSnapshot = (IProjectionSnapshot)snapshot.LeftBufferSnapshot;
                    var leftSourceSnapshot = this.GetSourceSnapshot(leftProjectionSnapshot);

                    var leftSnapshotLine = snapshot.MapToSnapshot(line.Extent.Start, leftProjectionSnapshot).GetContainingLine();

                    // Find the first line in the left snapshot after the ellipsis that corresponds to a real line in the source of the left snapshot
                    // (it might be more than one before the ellipsis since empty lines in the left buffer are represented entirely as lines from the inert buffer).
                    var startLine = int.MaxValue;
                    var endLine = int.MaxValue;
                    var skipped = 0;
                    for (var i = leftSnapshotLine.LineNumber + 1; i < leftSnapshotLine.Snapshot.LineCount; ++i)
                    {
                        leftSnapshotLine = leftSnapshotLine.Snapshot.GetLineFromLineNumber(i);
                        var source = _view.BufferGraph.MapDownToSnapshot(leftSnapshotLine.Start, PointTrackingMode.Negative, leftSourceSnapshot, PositionAffinity.Successor);
                        if (source.HasValue)
                        {
                            endLine = source.Value.GetContainingLine().LineNumber;
                            startLine = endLine - skipped;                              // Account for the skipped lines;
                            break;
                        }
                        else
                        {
                            ++skipped;
                        }
                    }

                    // No subsequent line means we are on the last ellipsis (and do not need to do anything)
                    UIElement adornment;
                    if (startLine != int.MaxValue)
                    {
                        // Find the last line in the left source snapshot before the next ellipsis.
                        //  Between ellipsis, there's a 1:1 correspondance between lines in the leftProjectionSnapshot and the leftSourceSnapshot so simply count lines.
                        for (var i = leftSnapshotLine.LineNumber + 1; i < leftSnapshotLine.Snapshot.LineCount; ++i)
                        {
                            var nextExtent = leftSnapshotLine.Snapshot.GetLineFromLineNumber(i).Extent;
                            if (this.IsSpanAnInsertedElipse(nextExtent))
                            {
                                break;
                            }

                            ++endLine;
                        }

                        var properties = _classificationFormatMap.GetTextProperties(_lineNumberClassification);
                        var block = new TextBlock
                        {
                            Text = string.Format(EditorFeaturesWpfResources.Lines_0_to_1, startLine + 1, endLine + 1), // Displayed line numbers are 1-based.
                            FontSize = properties.FontRenderingEmSize,
                            FontFamily = properties.Typeface.FontFamily,
                            Foreground = properties.ForegroundBrush,
                            Background = _view.Background,
                            Height = _view.LineHeight,
                        };

                        var separatorProperties = _editorFormatMap.GetProperties("outlining.verticalrule");
                        var separatorBrush = (separatorProperties[EditorFormatDefinition.ForegroundBrushId] as Brush) ?? Brushes.Gray;
                        var separatorY = Math.Floor(block.Height * 0.5) + 0.5;
                        var separator = new Line()
                        {
                            X1 = 5.0,
                            Y1 = separatorY,
                            X2 = _view.ViewportWidth,
                            Y2 = separatorY,
                            StrokeThickness = 1.0,
                            Stroke = separatorBrush,
                        };

                        var panel = new StackPanel() { Orientation = Orientation.Horizontal };
                        panel.Children.Add(block);
                        panel.Children.Add(separator);

                        adornment = panel;
                    }
                    else
                    {
                        // We are on the last ellipsis and need to hide it.
                        adornment = new Rectangle()
                        {
                            Width = line.Width,
                            Height = line.TextHeight,
                            Fill = _view.Background,
                        };
                    }

                    Canvas.SetTop(adornment, line.TextTop);
                    Canvas.SetLeft(adornment, 0.0);

                    _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, line.Extent, null, adornment, null);
                }
            }
        }

        private bool IsSpanAnInsertedElipse(SnapshotSpan span)
        {
            if ((span.Length == PreviewFactoryService.Ellipsis.Length) && (span.GetText() == PreviewFactoryService.Ellipsis))
            {
                // The span is an ellipsis but we need to verify that it is sourced to the inert buffer.
                while (true)
                {
                    if (span.Snapshot is IProjectionSnapshot projection)
                    {
                        var sources = projection.MapToSourceSnapshots(span);
                        if (sources.Count == 1)
                        {
                            span = sources[0];
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        return span.Snapshot.ContentType == _factory.TextBufferFactoryService.InertContentType;
                    }
                }
            }

            return false;
        }

        private ITextSnapshot GetSourceSnapshot(ITextSnapshot snapshot)
        {
            if (snapshot is IProjectionSnapshot projection)
            {
                foreach (var s in projection.SourceSnapshots)
                {
                    if (s.ContentType != _factory.TextBufferFactoryService.InertContentType)
                    {
                        return this.GetSourceSnapshot(s);
                    }
                }
            }

            return snapshot;
        }
    }
}
