// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateTime
{
    internal partial class DateTimeEmbeddedCompletionProvider
    {
        private readonly struct EmbeddedCompletionContext
        {
            private readonly ArrayBuilder<DateTimeItem> _items;
            private readonly TextSpan _replacementSpan;

            public EmbeddedCompletionContext(
                SourceText text,
                CompletionContext context,
                ArrayBuilder<DateTimeItem> items)
            {
                _items = items;

                var startPosition = context.Position;
                while (char.IsLetter(text[startPosition - 1]))
                {
                    startPosition--;
                }

                _replacementSpan = TextSpan.FromBounds(startPosition, context.Position);
            }

            private static System.DateTime s_exampleDateTime = System.DateTime.Parse("2009-06-15T13:45:30.1234567Z");
            private static CultureInfo s_enUsCulture = CultureInfo.GetCultureInfo("en-US");
            private static CultureInfo s_primaryCulture = CultureInfo.CurrentCulture;
            private static CultureInfo s_secondaryCulture = s_primaryCulture.Equals(s_enUsCulture) ? null : s_enUsCulture;

            public void AddStandard(string displayText, string suffix, string description)
                => Add(displayText, suffix, description, standard: true);

            public void AddCustom(string displayText, string suffix, string description)
                => Add(displayText, suffix, description, standard: false);

            private void Add(string displayText, string suffix, string description, bool standard)
            {
                using var _ = PooledStringBuilder.GetInstance(out var descriptionBuilder);

                // Single letter custom strings need a %, or else they're interpreted as a format
                // standard format string (and will throw a format exception).
                var formatString = !standard && displayText.Length == 1
                    ? "%" + displayText
                    : displayText;
                descriptionBuilder.AppendLine(@$"{s_primaryCulture.Name}: {s_exampleDateTime.ToString(formatString)}");
                if (s_secondaryCulture != null)
                    descriptionBuilder.AppendLine(@$"{s_secondaryCulture.Name}: {s_exampleDateTime.ToString(formatString)}");

                descriptionBuilder.AppendLine();
                descriptionBuilder.Append(description);

                _items.Add(new DateTimeItem(
                    displayText, suffix, descriptionBuilder.ToString(),
                    CompletionChange.Create(
                        new TextChange(_replacementSpan, displayText))));
            }
        }
    }
}
