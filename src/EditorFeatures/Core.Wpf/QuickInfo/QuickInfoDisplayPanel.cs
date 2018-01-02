// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal class QuickInfoDisplayPanel : StackPanel
    {
        private ImmutableArray<TextBlockElement> _textBlocks;

        internal TextBlock MainDescription => _textBlocks.FirstOrDefault(tb => tb.Kind == QuickInfoSectionKinds.Description)?.Block;
        internal TextBlock Documentation => _textBlocks.FirstOrDefault(tb => tb.Kind == QuickInfoSectionKinds.DocumentationComments)?.Block;
        internal TextBlock TypeParameterMap => _textBlocks.FirstOrDefault(tb => tb.Kind == QuickInfoSectionKinds.TypeParameters)?.Block;
        internal TextBlock AnonymousTypes => _textBlocks.FirstOrDefault(tb => tb.Kind == QuickInfoSectionKinds.AnonymousTypes)?.Block;
        internal TextBlock UsageText => _textBlocks.FirstOrDefault(tb => tb.Kind == QuickInfoSectionKinds.Usage)?.Block;
        internal TextBlock ExceptionText => _textBlocks.FirstOrDefault(tb => tb.Kind == QuickInfoSectionKinds.Exception)?.Block;

        public QuickInfoDisplayPanel(
            FrameworkElement symbolGlyph,
            FrameworkElement warningGlyph,
            ImmutableArray<TextBlockElement> textBlocks,
            FrameworkElement documentSpan)
        {
            _textBlocks = textBlocks;

            this.Orientation = Orientation.Vertical;

            for (int i = 0; i < _textBlocks.Length; i++)
            {
                var tb = _textBlocks[i];
                if (i == 0)
                {
                    this.Children.Add(AddGlyphs(tb.Block, symbolGlyph, warningGlyph));
                }
                else
                {
                    this.Children.Add(tb.Block);
                }
            }

            if (documentSpan != null)
            {
                this.Children.Add(documentSpan);
            }
        }

        private static FrameworkElement AddGlyphs(TextBlock tb, FrameworkElement symbolGlyph, FrameworkElement warningGlyph)
        {
            var panel = new DockPanel()
            {
                LastChildFill = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            if (symbolGlyph != null)
            {
                symbolGlyph.Margin = new Thickness(1, 1, 3, 1);
                var symbolGlyphBorder = new Border()
                {
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = symbolGlyph
                };

                panel.Children.Add(symbolGlyphBorder);
            }

            tb.Margin = new Thickness(1);
            var mainDescriptionBorder = new Border()
            {
                BorderThickness = new Thickness(0),
                BorderBrush = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                Child = tb
            };

            panel.Children.Add(mainDescriptionBorder);

            if (warningGlyph != null)
            {
                warningGlyph.Margin = new Thickness(1, 1, 3, 1);
                var warningGlyphBorder = new Border()
                {
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Child = warningGlyph
                };

                panel.Children.Add(warningGlyphBorder);
            }

            return panel;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var tb in _textBlocks)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                BuildStringFromInlineCollection(tb.Block.Inlines, sb);
            }

            return sb.ToString();
        }

        private static void BuildStringFromInlineCollection(InlineCollection inlines, StringBuilder sb)
        {
            foreach (var inline in inlines)
            {
                if (inline != null)
                {
                    var inlineText = GetStringFromInline(inline);
                    if (!string.IsNullOrEmpty(inlineText))
                    {
                        sb.Append(inlineText);
                    }
                }
            }
        }

        private static string GetStringFromInline(Inline currentInline)
        {
            if (currentInline is LineBreak lineBreak)
            {
                return Environment.NewLine;
            }

            var run = currentInline as Run;
            if (run == null)
            {
                return null;
            }

            return run.Text;
        }
    }
}
