// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class QuickInfoDisplayPanel : StackPanel
    {
        internal TextBlock MainDescription { get; }
        internal TextBlock Documentation { get; }
        internal TextBlock TypeParameterMap { get; }
        internal TextBlock AnonymousTypes { get; }
        internal TextBlock UsageText { get; }
        internal TextBlock ExceptionText { get; }

        public QuickInfoDisplayPanel(
            FrameworkElement symbolGlyph,
            FrameworkElement warningGlyph,
            FrameworkElement mainDescription,
            FrameworkElement documentation,
            FrameworkElement typeParameterMap,
            FrameworkElement anonymousTypes,
            FrameworkElement usageText,
            FrameworkElement exceptionText)
        {
            this.MainDescription = (TextBlock)mainDescription;
            this.Documentation = (TextBlock)documentation;
            this.TypeParameterMap = (TextBlock)typeParameterMap;
            this.AnonymousTypes = (TextBlock)anonymousTypes;
            this.UsageText = (TextBlock)usageText;
            this.ExceptionText = (TextBlock)exceptionText;

            this.Orientation = Orientation.Vertical;

            Border symbolGlyphBorder = null;
            if (symbolGlyph != null)
            {
                symbolGlyph.Margin = new Thickness(1, 1, 3, 1);
                symbolGlyphBorder = new Border()
                {
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = symbolGlyph
                };
            }

            mainDescription.Margin = new Thickness(1);
            var mainDescriptionBorder = new Border()
            {
                BorderThickness = new Thickness(0),
                BorderBrush = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                Child = mainDescription
            };

            var symbolGlyphAndMainDescriptionDock = new DockPanel()
            {
                LastChildFill = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            if (symbolGlyphBorder != null)
            {
                symbolGlyphAndMainDescriptionDock.Children.Add(symbolGlyphBorder);
            }

            symbolGlyphAndMainDescriptionDock.Children.Add(mainDescriptionBorder);

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

                symbolGlyphAndMainDescriptionDock.Children.Add(warningGlyphBorder);
            }

            this.Children.Add(symbolGlyphAndMainDescriptionDock);
            this.Children.Add(documentation);
            this.Children.Add(usageText);
            this.Children.Add(typeParameterMap);
            this.Children.Add(anonymousTypes);
            this.Children.Add(exceptionText);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            BuildStringFromInlineCollection(this.MainDescription.Inlines, sb);

            if (this.Documentation.Inlines.Count > 0)
            {
                sb.AppendLine();
                BuildStringFromInlineCollection(this.Documentation.Inlines, sb);
            }

            if (this.TypeParameterMap.Inlines.Count > 0)
            {
                sb.AppendLine();
                BuildStringFromInlineCollection(this.TypeParameterMap.Inlines, sb);
            }

            if (this.AnonymousTypes.Inlines.Count > 0)
            {
                sb.AppendLine();
                BuildStringFromInlineCollection(this.AnonymousTypes.Inlines, sb);
            }

            if (this.UsageText.Inlines.Count > 0)
            {
                sb.AppendLine();
                BuildStringFromInlineCollection(this.UsageText.Inlines, sb);
            }

            if (this.ExceptionText.Inlines.Count > 0)
            {
                sb.AppendLine();
                BuildStringFromInlineCollection(this.ExceptionText.Inlines, sb);
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
            var lineBreak = currentInline as LineBreak;
            if (lineBreak != null)
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
