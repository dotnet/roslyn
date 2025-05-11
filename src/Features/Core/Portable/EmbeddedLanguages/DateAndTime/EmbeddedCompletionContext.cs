// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime;

internal sealed partial class DateAndTimeEmbeddedCompletionProvider
{
    private readonly struct EmbeddedCompletionContext
    {
        private static readonly DateTime s_exampleDateTime = DateTime.Parse("2009-06-15T13:45:30.1234567");
        private static readonly CultureInfo s_enUsCulture = CultureInfo.GetCultureInfo("en-US");

        private readonly ArrayBuilder<DateAndTimeItem> _items;
        private readonly TextSpan _replacementSpan;

        /// <summary>
        /// The portion of the user string token prior to the section we're replacing.  Used for building the
        /// example format to present.
        /// </summary>
        private readonly string _userFormatPrefix;

        /// <summary>
        /// The portion of the user string token after to the section we're replacing.  Used for building the
        /// example format to present.
        /// </summary>
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

            (_userFormatPrefix, _userFormatSuffix) = GetUserFormatParts(virtualChars, startPosition, context.Position);
        }

        private static (string prefix, string suffix) GetUserFormatParts(
            VirtualCharSequence virtualChars, int startPosition, int endPosition)
        {
            virtualChars = virtualChars.IsDefault ? VirtualCharSequence.Empty : virtualChars;

            using var _1 = PooledStringBuilder.GetInstance(out var prefix);
            using var _2 = PooledStringBuilder.GetInstance(out var suffix);
            foreach (var ch in virtualChars)
            {
                if (ch.Span.End <= startPosition)
                    ch.AppendTo(prefix);
                else if (ch.Span.Start >= endPosition)
                    ch.AppendTo(suffix);
            }

            return (prefix.ToString(), suffix.ToString());
        }

        private void AddExamples(ArrayBuilder<string> examples, bool standard, string displayText)
        {
            var userFormat = _userFormatPrefix + displayText + _userFormatSuffix;

            var primaryCulture = CultureInfo.CurrentCulture;
            var secondaryCulture = s_enUsCulture;
            var hideCulture = primaryCulture.Equals(secondaryCulture);

            AddExample(examples, standard, userFormat, primaryCulture, hideCulture);
            AddExample(examples, standard, userFormat, secondaryCulture, hideCulture);
            AddExample(examples, standard, displayText, primaryCulture, hideCulture);
            AddExample(examples, standard, displayText, secondaryCulture, hideCulture);
        }

        private static void AddExample(
            ArrayBuilder<string> examples, bool standard, string displayText, CultureInfo culture, bool hideCulture)
        {
            // Single letter custom strings need a %, or else they're interpreted as a format
            // standard format string (and will throw a format exception).
            var formatString = !standard && displayText.Length == 1
                ? "%" + displayText
                : displayText;

            if (formatString == "")
                return;

            // Format string may be invalid.  Just don't show anything in that case.
            string formattedDate;
            try
            {
                formattedDate = s_exampleDateTime.ToString(formatString, culture);
            }
            catch (FormatException)
            {
                return;
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            var example = hideCulture
                ? $"   {displayText} → {formattedDate}"
                : $"   {displayText} ({culture}) → {formattedDate}";
            if (!examples.Contains(example))
                examples.Add(example);
        }

        public void AddStandard(string displayText, string suffix, string description, bool isDefault = false)
            => Add(displayText, suffix, description, standard: true, isDefault);

        public void AddCustom(string displayText, string suffix, string description)
            => Add(displayText, suffix, description, standard: false, isDefault: false);

        private void Add(string displayText, string suffix, string description, bool standard, bool isDefault)
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
                    new TextChange(_replacementSpan, displayText)),
                isDefault));
        }
    }
}
