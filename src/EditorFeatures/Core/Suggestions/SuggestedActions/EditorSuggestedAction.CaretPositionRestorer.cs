// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

internal partial class EditorSuggestedAction
{
    internal sealed class CaretPositionRestorer : IDisposable
    {
        // Bug 5535: By default the standard editor caret is set to have positive affinity.  This
        // means that if text is added right at the caret then the caret moves to the right and
        // is placed after the added text.  However, we don't want that.  Instead, we want the
        // caret to stay where it started at. So we store the caret position here and
        // restore it afterwards.
        private readonly EventHandler<CaretPositionChangedEventArgs> _caretPositionChangedHandler;
        private readonly IList<Tuple<ITextView, IMappingPoint>> _caretPositions;
        private readonly ITextBuffer _subjectBuffer;
        private readonly ITextBufferAssociatedViewService _associatedViewService;

        private bool _caretChanged;

        public CaretPositionRestorer(ITextBuffer subjectBuffer, ITextBufferAssociatedViewService associatedViewService)
        {
            Contract.ThrowIfNull(associatedViewService);
            _subjectBuffer = subjectBuffer;
            _caretPositionChangedHandler = (s, e) => _caretChanged = true;
            _associatedViewService = associatedViewService;

            _caretPositions = GetCaretPositions();
        }

        private IList<Tuple<ITextView, IMappingPoint>> GetCaretPositions()
        {
            // Currently, only do this if there's a single view 
            var views = _associatedViewService.GetAssociatedTextViews(_subjectBuffer);
            var result = new List<Tuple<ITextView, IMappingPoint>>();

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
