// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.EmbeddedLanguages.DateTime;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateTime
{
    internal partial class DateTimeEmbeddedCompletionProvider : CompletionProvider
    {
        private const string StartKey = nameof(StartKey);
        private const string LengthKey = nameof(LengthKey);
        private const string NewTextKey = nameof(NewTextKey);
        private const string NewPositionKey = nameof(NewPositionKey);
        private const string DescriptionKey = nameof(DescriptionKey);

        // Always soft-select these completion items.  Also, never filter down.
        private static readonly CompletionItemRules s_rules =
            CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection)
                                       .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, new char[] { }));

        private readonly DateTimeEmbeddedLanguageFeatures _language;

        public DateTimeEmbeddedCompletionProvider(DateTimeEmbeddedLanguageFeatures language)
        {
            _language = language;
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            if (trigger.Kind == CompletionTriggerKind.Invoke ||
                trigger.Kind == CompletionTriggerKind.InvokeAndCommitIfUnique)
            {
                return true;
            }

            if (trigger.Kind == CompletionTriggerKind.Insertion)
            {
                return char.IsLetter(trigger.Character);
            }

            return false;
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (!context.Options.GetOption(DateTimeOptions.ProvideDateTimeOptionsCompletions, context.Document.Project.Language))
                return;

            if (context.Trigger.Kind != CompletionTriggerKind.Invoke &&
                context.Trigger.Kind != CompletionTriggerKind.InvokeAndCommitIfUnique &&
                context.Trigger.Kind != CompletionTriggerKind.Insertion)
            {
                return;
            }

            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var stringTokenOpt = await _language.TryGetDateTimeStringTokenAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            if (stringTokenOpt == null)
                return;

            var stringToken = stringTokenOpt.Value;
            if (position <= stringToken.SpanStart || position >= stringToken.Span.End)
                return;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<DateTimeItem>.GetInstance(out var items);

            var embeddedContext = new EmbeddedCompletionContext(text, context, items);
            ProvideStandardFormats(embeddedContext);
            ProvideCustomFormats(embeddedContext);
            if (items.Count == 0)
                return;

            foreach (var embeddedItem in items)
            {
                var change = embeddedItem.Change;
                var textChange = change.TextChange;

                var properties = ImmutableDictionary.CreateBuilder<string, string>();
                properties.Add(StartKey, textChange.Span.Start.ToString());
                properties.Add(LengthKey, textChange.Span.Length.ToString());
                properties.Add(NewTextKey, textChange.NewText);
                properties.Add(DescriptionKey, embeddedItem.FullDescription);
                properties.Add(AbstractEmbeddedLanguageCompletionProvider.EmbeddedProviderName, Name);

                if (change.NewPosition != null)
                    properties.Add(NewPositionKey, change.NewPosition.ToString());

                // Keep everything sorted in the order we just produced the items in.
                var sortText = context.Items.Count.ToString("0000");
                context.AddItem(CompletionItem.Create(
                    displayText: embeddedItem.DisplayText,
                    inlineDescription: embeddedItem.InlineDescription,
                    sortText: sortText,
                    properties: properties.ToImmutable(),
                    rules: s_rules));
            }

            context.IsExclusive = true;
        }

        private void ProvideStandardFormats(EmbeddedCompletionContext context)
        {
            context.AddStandard("d", "short date", FeaturesResources.short_date_description);
            context.AddStandard("D", "long date", "blah blah");
            context.AddStandard("f", "full short date/time", "blah blah");
            context.AddStandard("F", "full long date/time", "blah blah");
            context.AddStandard("g", "general short date/time", "blah blah");
            context.AddStandard("G", "general long date/time", "blah blah");
            context.AddStandard("M", "month day", "blah blah");
            context.AddStandard("O", "round-trip date/time", "blah blah");
            context.AddStandard("R", "rfc1123 date/time", "blah blah");
            context.AddStandard("t", "short time", "blah blah");
            context.AddStandard("T", "long time", "blah blah");
            context.AddStandard("u", "universal sortable date/time", "blah blah");
            context.AddStandard("U", "universal full date/time", "blah blah");
            context.AddStandard("Y", "year month", "blah blah");
        }

        private void ProvideCustomFormats(EmbeddedCompletionContext context)
        {
            context.AddCustom("d", "day of the month (1-digit)", "blah blah");
            context.AddCustom("dd", "day of the month (2-digits)", "blah blah");
            context.AddCustom("ddd", "day of the week (abbreviated)", "blah blah");
            context.AddCustom("dddd", "day of the week (full)", "blah blah");

            context.AddCustom("f", "10ths of a second", "blah blah");
            context.AddCustom("ff", "100ths of a second", "blah blah");
            context.AddCustom("fff", "1,000ths of a second", "blah blah");
            context.AddCustom("ffff", "10,000ths of a second", "blah blah");
            context.AddCustom("fffff", "100,000ths of a second", "blah blah");
            context.AddCustom("ffffff", "1,000,000ths of a second", "blah blah");
            context.AddCustom("fffffff", "10,000,000ths of a second", "blah blah");

            context.AddCustom("F", "10ths of a second (non-zero)", "blah blah");
            context.AddCustom("FF", "100ths of a second (non-zero)", "blah blah");
            context.AddCustom("FFF", "1,000th of a second (non-zero)", "blah blah");
            context.AddCustom("FFFF", "10,000ths of a second (non-zero)", "blah blah");
            context.AddCustom("FFFFF", "100,000ths of a second (non-zero)", "blah blah");
            context.AddCustom("FFFFFF", "1,000,000ths of a second (non-zero)", "blah blah");
            context.AddCustom("FFFFFFF", "10,000,000ths of a second (non-zero)", "blah blah");

            context.AddCustom("gg", "period/era", "blah blah");

            context.AddCustom("h", "12 hour clock (1-2 digits)", "blah blah");
            context.AddCustom("hh", "12 hour clock (2-digits)", "blah blah");

            context.AddCustom("H", "24 hour clock (1-digit)", "blah blah");
            context.AddCustom("HH", "24 hour clock (2-digits)", "blah blah");

            context.AddCustom("K", "time zone", "blah blah");

            context.AddCustom("m", "minute (1-2 digits)", "blah blah");
            context.AddCustom("mm", "minute (2 digits)", "blah blah");

            context.AddCustom("M", "month (1-2 digits)", "blah blah");
            context.AddCustom("MM", "month (2 digits)", "blah blah");
            context.AddCustom("MMM", "month (abbreviated)", "blah blah");
            context.AddCustom("MMMM", "month (full)", "blah blah");

            context.AddCustom("s", "second (1-2 digits)", "blah blah");
            context.AddCustom("ss", "second (2 digits)", "blah blah");

            context.AddCustom("t", "AM/PM (abbreviated)", "blah blah");
            context.AddCustom("tt", "AM/PM (full)", "blah blah");

            context.AddCustom("y", "year (1-2 digits)", "blah blah");
            context.AddCustom("yy", "year (2 digits)", "blah blah");
            context.AddCustom("yyy", "year (3-4 digits)", "blah blah");
            context.AddCustom("yyyy", "year (4 digits)", "blah blah");
            context.AddCustom("yyyyy", "year (5 digits)", "blah blah");

            context.AddCustom("z", "utc hour offset (1-2 digits)", "blah blah");
            context.AddCustom("zz", "utc hour offset (2 digits)", "blah blah");
            context.AddCustom("zzz", "utc hour and minute offset", "blah blah");

            context.AddCustom(":", "time separator", "blah blah");
            context.AddCustom("/", "date separator", "blah blah");
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            // These values have always been added by us.
            var startString = item.Properties[StartKey];
            var lengthString = item.Properties[LengthKey];
            var newText = item.Properties[NewTextKey];

            return Task.FromResult(CompletionChange.Create(
                new TextChange(new TextSpan(int.Parse(startString), int.Parse(lengthString)), newText)));
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetValue(DescriptionKey, out var description))
            {
                return SpecializedTasks.Null<CompletionDescription>();
            }

            return Task.FromResult(CompletionDescription.Create(
                ImmutableArray.Create(new TaggedText(TextTags.Text, description))));
        }
    }
}
