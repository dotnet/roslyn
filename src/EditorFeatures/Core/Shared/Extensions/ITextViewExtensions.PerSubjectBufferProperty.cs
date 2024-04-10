// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static partial class ITextViewExtensions
{
    private class PerSubjectBufferProperty<TProperty, TTextView> where TTextView : ITextView
    {
        private readonly TTextView _textView;
        private readonly Dictionary<ITextBuffer, Dictionary<object, TProperty>> _subjectBufferMap = [];

        // Some other VS components (e.g. Razor) will temporarily disconnect out ITextBuffer from the ITextView.  When listening to 
        // BufferGraph.GraphBuffersChanged, we should allow buffers we previously knew about to be re-attached.
        private readonly ConditionalWeakTable<ITextBuffer, Dictionary<object, TProperty>> _buffersRemovedFromTextViewBufferGraph = new();

        public static bool GetOrCreateValue(
            TTextView textView,
            ITextBuffer subjectBuffer,
            object key,
            Func<TTextView, ITextBuffer, TProperty> valueCreator,
            out TProperty value)
        {
            Contract.ThrowIfTrue(textView.IsClosed);

            var properties = textView.Properties.GetOrCreateSingletonProperty(() => new PerSubjectBufferProperty<TProperty, TTextView>(textView));
            if (!properties.TryGetValue(subjectBuffer, key, out var priorValue))
            {
                // Need to create it.
                value = valueCreator(textView, subjectBuffer);
                properties.Add(subjectBuffer, key, value);
                return true;
            }

            // Already there.
            value = priorValue;
            return false;
        }

        public static bool TryGetValue(
            TTextView textView,
            ITextBuffer subjectBuffer,
            object key,
            [MaybeNullWhen(false)] out TProperty value)
        {
            Contract.ThrowIfTrue(textView.IsClosed);

            var properties = textView.Properties.GetOrCreateSingletonProperty(() => new PerSubjectBufferProperty<TProperty, TTextView>(textView));
            return properties.TryGetValue(subjectBuffer, key, out value);
        }

        public static void AddValue(
            TTextView textView,
            ITextBuffer subjectBuffer,
            object key,
            TProperty value)
        {
            Contract.ThrowIfTrue(textView.IsClosed);

            var properties = textView.Properties.GetOrCreateSingletonProperty(() => new PerSubjectBufferProperty<TProperty, TTextView>(textView));
            properties.Add(subjectBuffer, key, value);
        }

        public static void RemoveValue(TTextView textView, ITextBuffer subjectBuffer, object key)
        {
            if (textView.Properties.TryGetProperty(typeof(PerSubjectBufferProperty<TProperty, TTextView>), out PerSubjectBufferProperty<TProperty, TTextView> properties))
            {
                properties.Remove(subjectBuffer, key);
            }
        }

        private PerSubjectBufferProperty(TTextView textView)
        {
            _textView = textView;

            _textView.Closed += OnTextViewClosed;
            _textView.BufferGraph.GraphBuffersChanged += OnTextViewBufferGraphChanged;
        }

        private void OnTextViewClosed(object? sender, EventArgs e)
        {
            _textView.Closed -= OnTextViewClosed;
            _textView.BufferGraph.GraphBuffersChanged -= OnTextViewBufferGraphChanged;

            _subjectBufferMap.Clear();
            _textView.Properties.RemoveProperty(typeof(PerSubjectBufferProperty<TProperty, TTextView>));
        }

        private void OnTextViewBufferGraphChanged(object? sender, GraphBuffersChangedEventArgs e)
        {
            foreach (var buffer in e.RemovedBuffers)
            {
                if (_subjectBufferMap.TryGetValue(buffer, out var value))
                {
                    _subjectBufferMap.Remove(buffer);
                    _buffersRemovedFromTextViewBufferGraph.Add(buffer, value);
                }
            }

            foreach (var buffer in e.AddedBuffers)
            {
                if (_buffersRemovedFromTextViewBufferGraph.TryGetValue(buffer, out var value))
                {
                    _subjectBufferMap[buffer] = value;
                    _buffersRemovedFromTextViewBufferGraph.Remove(buffer);
                }
            }
        }

        public bool TryGetValue(ITextBuffer subjectBuffer, object key, [MaybeNullWhen(false)] out TProperty value)
        {
            if (_subjectBufferMap.TryGetValue(subjectBuffer, out var bufferMap))
            {
                return bufferMap.TryGetValue(key, out value);
            }

            if (_buffersRemovedFromTextViewBufferGraph.TryGetValue(subjectBuffer, out bufferMap))
            {
                return bufferMap.TryGetValue(key, out value);
            }

            value = default;
            return false;
        }

        public void Add(ITextBuffer subjectBuffer, object key, TProperty value)
        {
            var bufferMap = _subjectBufferMap.GetOrAdd(subjectBuffer, _ => []);
            bufferMap[key] = value;
        }

        public void Remove(ITextBuffer subjectBuffer, object key)
        {
            if (_subjectBufferMap.TryGetValue(subjectBuffer, out var bufferMap))
            {
                bufferMap.Remove(key);
                if (!bufferMap.Any())
                {
                    _subjectBufferMap.Remove(subjectBuffer);
                }
            }

            if (_buffersRemovedFromTextViewBufferGraph.TryGetValue(subjectBuffer, out bufferMap))
            {
                bufferMap.Remove(key);
                if (!bufferMap.Any())
                {
                    _buffersRemovedFromTextViewBufferGraph.Remove(subjectBuffer);
                }
            }
        }
    }
}
