// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedAction
    {
        internal class CaretPositionRestorer : IDisposable
        {
            // Bug 5535: By default the standard editor caret is set to have positive affinity.  This
            // means that if text is added right at the caret then the caret moves to the right and
            // is placed after the added text.  However, we don't want that.  Instead, we want the
            // caret to stay where it started at. So we store the caret position here and
            // restore it afterwards.
            private readonly EventHandler<CaretPositionChangedEventArgs> _caretPositionChangedHandler;
            private readonly IList<Tuple<IWpfTextView, IMappingPoint>> _caretPositions;
            private readonly ITextBuffer _subjectBuffer;
            private readonly ITextBufferAssociatedViewService _visibilityService;

            private bool _caretChanged;

            public CaretPositionRestorer(ITextBuffer subjectBuffer, ITextBufferAssociatedViewService visibilityService)
            {
                Contract.ThrowIfNull(visibilityService);
                _subjectBuffer = subjectBuffer;
                _caretPositionChangedHandler = (s, e) => _caretChanged = true;
                _visibilityService = visibilityService;

                _caretPositions = GetCaretPositions();
            }

            private IList<Tuple<IWpfTextView, IMappingPoint>> GetCaretPositions()
            {
                // Currently, only do this if there's a single view 
                var views = _visibilityService.GetAssociatedTextViews(_subjectBuffer);
                var result = new List<Tuple<IWpfTextView, IMappingPoint>>();

                foreach (var view in views)
                {
                    view.Caret.PositionChanged += _caretPositionChangedHandler;
                    var point = view.GetCaretPoint(_subjectBuffer);
                    if (point != null)
                    {
                        result.Add(Tuple.Create(view, view.BufferGraph.CreateMappingPoint(point.Value, PointTrackingMode.Negative)));
                    }
                }

                return result;
            }

            private void RestoreCaretPositions()
            {
                if (_caretChanged)
                {
                    return;
                }

                foreach (var tuple in _caretPositions)
                {
                    var position = tuple.Item1.GetCaretPoint(_subjectBuffer);
                    if (position != null)
                    {
                        var view = tuple.Item1;

                        if (!view.IsClosed)
                        {
                            view.TryMoveCaretToAndEnsureVisible(position.Value);
                        }
                    }
                }
            }

            public void Dispose()
            {
                RestoreCaretPositions();
                foreach (var tuple in _caretPositions)
                {
                    tuple.Item1.Caret.PositionChanged -= _caretPositionChangedHandler;
                }
            }
        }
    }
}
