// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ITextViewExtensions
    {
        private class AutoClosingViewProperty<TProperty, TTextView> where TTextView : ITextView
        {
            private readonly TTextView _textView;
            private readonly Dictionary<object, TProperty> _map = new();

            public static bool GetOrCreateValue(
                TTextView textView,
                object key,
                Func<TTextView, TProperty> valueCreator,
                out TProperty value)
            {
                Contract.ThrowIfTrue(textView.IsClosed);

                var properties = textView.Properties.GetOrCreateSingletonProperty(() => new AutoClosingViewProperty<TProperty, TTextView>(textView));
                if (!properties.TryGetValue(key, out var priorValue))
                {
                    // Need to create it.
                    value = valueCreator(textView);
                    properties.Add(key, value);
                    return true;
                }

                // Already there.
                value = priorValue;
                return false;
            }

            public static bool TryGetValue(
                TTextView textView,
                object key,
                [MaybeNullWhen(false)] out TProperty value)
            {
                Contract.ThrowIfTrue(textView.IsClosed);

                var properties = textView.Properties.GetOrCreateSingletonProperty(() => new AutoClosingViewProperty<TProperty, TTextView>(textView));
                return properties.TryGetValue(key, out value);
            }

            public static void AddValue(
                TTextView textView,
                object key,
                TProperty value)
            {
                Contract.ThrowIfTrue(textView.IsClosed);

                var properties = textView.Properties.GetOrCreateSingletonProperty(() => new AutoClosingViewProperty<TProperty, TTextView>(textView));
                properties.Add(key, value);
            }

            public static void RemoveValue(TTextView textView, object key)
            {
                if (textView.Properties.TryGetProperty(typeof(AutoClosingViewProperty<TProperty, TTextView>), out AutoClosingViewProperty<TProperty, TTextView> properties))
                {
                    properties.Remove(key);
                }
            }

            private AutoClosingViewProperty(TTextView textView)
            {
                _textView = textView;
                _textView.Closed += OnTextViewClosed;
            }

            private void OnTextViewClosed(object? sender, EventArgs e)
            {
                _textView.Closed -= OnTextViewClosed;
                _textView.Properties.RemoveProperty(typeof(AutoClosingViewProperty<TProperty, TTextView>));
            }

            public bool TryGetValue(object key, [MaybeNullWhen(false)] out TProperty value)
                => _map.TryGetValue(key, out value);

            public void Add(object key, TProperty value)
                => _map[key] = value;

            public void Remove(object key)
                => _map.Remove(key);
        }
    }
}
