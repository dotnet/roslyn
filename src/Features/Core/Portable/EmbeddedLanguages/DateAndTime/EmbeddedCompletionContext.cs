// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime
{
    internal partial class DateAndTimeEmbeddedCompletionProvider
    {
        private readonly struct EmbeddedCompletionContext
        {
            private static DateTime s_exampleDateTime = DateTime.Parse("2009-06-15T13:45:30.1234567Z");
            private static CultureInfo s_enUsCulture = CultureInfo.GetCultureInfo("en-US");
            private static CultureInfo s_primaryCulture = CultureInfo.CurrentCulture;
            private static CultureInfo s_secondaryCulture = s_enUsCulture;

            private readonly ArrayBuilder<DateAndTimeItem> _items;
            private readonly TextSpan _replacementSpan;

            private readonly string _userFormatPrefix;
            private readonly string _userFormatSuffix;

            public EmbeddedCompletionContext(
                SourceText text,
                CompletionContext context,
                VirtualCharSequence virtualChars,
                ArrayBuilder<DateAndTimeItem> items)
            {
                _items = items;

                var startPosition = context.Position;
                while (char.IsLetter(text[startPosition - 1]))
                {
                    startPosition--;
                }

                _replacementSpan = TextSpan.FromBounds(startPosition, context.Position);

                virtualChars = virtualChars.IsDefault ? VirtualCharSequence.Empty : virtualChars;

                using var _1 = PooledStringBuilder.GetInstance(out var prefix);
                using var _2 = PooledStringBuilder.GetInstance(out var suffix);
                foreach (var ch in virtualChars)
                {
                    if (ch.Span.End <= startPosition)
                        ch.AppendTo(prefix);
                    else if (ch.Span.Start >= context.Position)
                        ch.AppendTo(suffix);
                }

                _userFormatPrefix = prefix.ToString();
                _userFormatSuffix = suffix.ToString();
            }

            private void AddExamples(ArrayBuilder<string> examples, bool standard, string displayText)
            {
                var userFormat = _userFormatPrefix + displayText + _userFormatSuffix;
                TryAddExample(examples, standard, userFormat, s_primaryCulture);
                TryAddExample(examples, standard, userFormat, s_secondaryCulture);

                AddExample(examples, standard, displayText, s_primaryCulture);
                AddExample(examples, standard, displayText, s_secondaryCulture);
            }

            private void TryAddExample(ArrayBuilder<string> examples, bool standard, string displayText, CultureInfo culture)
            {
                try
                {
                    AddExample(examples, standard, displayText, culture);
                }
                catch (FormatException)
                {
                    return;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return;
                }
            }

            private void AddExample(ArrayBuilder<string> examples, bool standard, string displayText, CultureInfo culture)
            {
                // Single letter custom strings need a %, or else they're interpreted as a format
                // standard format string (and will throw a format exception).
                var formatString = !standard && displayText.Length == 1
                    ? "%" + displayText
                    : displayText;

                if (formatString == "")
                    return;

                var formattedDate = s_exampleDateTime.ToString(formatString);
                var example = s_primaryCulture.Equals(s_secondaryCulture)
                    ? $"   {displayText} → {formattedDate}"
                    : $"   {displayText} ({culture}) → {formattedDate}";
                if (!examples.Contains(example))
                    examples.Add(example);
            }

            public void AddStandard(string displayText, string suffix, string description)
                => Add(displayText, suffix, description, standard: true);

            public void AddCustom(string displayText, string suffix, string description)
                => Add(displayText, suffix, description, standard: false);

            private void Add(string displayText, string suffix, string description, bool standard)
            {
                using var _1 = PooledStringBuilder.GetInstance(out var descriptionBuilder);
                using var _2 = ArrayBuilder<string>.GetInstance(out var examples);

                AddExamples(examples, standard, displayText);

                descriptionBuilder.AppendLine(
                    examples.Count == 1 ? FeaturesResources.Example : FeaturesResources.Examples);
                foreach (var example in examples)
                    descriptionBuilder.AppendLine(example);

                descriptionBuilder.AppendLine();
                descriptionBuilder.Append(description);

                _items.Add(new DateAndTimeItem(
                    displayText, suffix, descriptionBuilder.ToString(),
                    CompletionChange.Create(
                        new TextChange(_replacementSpan, displayText))));
            }
        }
    }
}
