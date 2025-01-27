// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime.LanguageServices;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime;

internal sealed partial class DateAndTimeEmbeddedCompletionProvider(DateAndTimeEmbeddedLanguage language) : EmbeddedLanguageCompletionProvider
{
    private const string StartKey = nameof(StartKey);
    private const string LengthKey = nameof(LengthKey);
    private const string NewTextKey = nameof(NewTextKey);
    private const string DescriptionKey = nameof(DescriptionKey);

    // Always soft-select these completion items.  Also, never filter down.
    private static readonly CompletionItemRules s_rules =
        CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection)
                                   .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, new char[] { }));

    private readonly DateAndTimeEmbeddedLanguage _language = language;

    public override ImmutableHashSet<char> TriggerCharacters { get; } = ['"', ':'];

    public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger)
    {
        if (trigger.Kind is CompletionTriggerKind.Invoke or
            CompletionTriggerKind.InvokeAndCommitIfUnique)
        {
            return true;
        }

        if (trigger.Kind == CompletionTriggerKind.Insertion)
        {
            if (TriggerCharacters.Contains(trigger.Character))
            {
                return true;
            }

            // Only trigger if it's the first character of a sequence
            return char.IsLetter(trigger.Character) &&
                   caretPosition >= 2 &&
                   !char.IsLetter(text[caretPosition - 2]);
        }

        return false;
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        if (!context.CompletionOptions.ProvideDateAndTimeCompletions)
            return;

        if (context.Trigger.Kind is not CompletionTriggerKind.Invoke and
            not CompletionTriggerKind.InvokeAndCommitIfUnique and
            not CompletionTriggerKind.Insertion)
        {
            return;
        }

        var document = context.Document;
        var position = context.Position;
        var cancellationToken = context.CancellationToken;

        var stringTokenOpt = await _language.TryGetDateAndTimeTokenAtPositionAsync(
            document, position, cancellationToken).ConfigureAwait(false);

        if (stringTokenOpt == null)
            return;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var stringToken = stringTokenOpt.Value;

        // If we're not in an interpolation, at least make sure we're within the bounds of the string.
        if (stringToken.RawKind != syntaxFacts.SyntaxKinds.InterpolatedStringTextToken)
        {
            if (position <= stringToken.SpanStart || position >= stringToken.Span.End)
                return;
        }

        // Note: it's acceptable if this fails to convert.  We just won't show the example in that case.
        var virtualChars = _language.Info.VirtualCharService.TryConvertToVirtualChars(stringToken);

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        using var _1 = ArrayBuilder<DateAndTimeItem>.GetInstance(out var items);

        var embeddedContext = new EmbeddedCompletionContext(text, context, virtualChars, items);

        ProvideStandardFormats(embeddedContext);
        ProvideCustomFormats(embeddedContext);
        if (items.Count == 0)
            return;

        using var _2 = ArrayBuilder<KeyValuePair<string, string>>.GetInstance(out var properties);
        foreach (var embeddedItem in items)
        {
            properties.Clear();

            var textChange = embeddedItem.Change.TextChange;

            properties.Add(new(StartKey, textChange.Span.Start.ToString()));
            properties.Add(new(LengthKey, textChange.Span.Length.ToString()));
            properties.Add(new(NewTextKey, textChange.NewText!));
            properties.Add(new(DescriptionKey, embeddedItem.FullDescription));
            properties.Add(new(AbstractAggregateEmbeddedLanguageCompletionProvider.EmbeddedProviderName, Name));

            // Keep everything sorted in the order we just produced the items in.
            var sortText = context.Items.Count.ToString("0000");
            context.AddItem(CompletionItem.CreateInternal(
                displayText: embeddedItem.DisplayText,
                inlineDescription: embeddedItem.InlineDescription,
                sortText: sortText,
                properties: properties.ToImmutable(),
                rules: embeddedItem.IsDefault
                    ? s_rules.WithMatchPriority(MatchPriority.Preselect)
                    : s_rules,
                isComplexTextEdit: context.CompletionListSpan != textChange.Span));
        }

        context.IsExclusive = true;
    }

    private static void ProvideStandardFormats(EmbeddedCompletionContext context)
    {
        context.AddStandard("d", FeaturesResources.short_date, FeaturesResources.short_date_description);
        context.AddStandard("D", FeaturesResources.long_date, FeaturesResources.long_date_description);
        context.AddStandard("f", FeaturesResources.full_short_date_time, FeaturesResources.full_short_date_time_description);
        context.AddStandard("F", FeaturesResources.full_long_date_time, FeaturesResources.full_long_date_time_description);
        context.AddStandard("g", FeaturesResources.general_short_date_time, FeaturesResources.general_short_date_time_description);
        context.AddStandard("G", FeaturesResources.general_long_date_time, FeaturesResources.general_long_date_time_description, isDefault: true); // This is what DateTime.ToString() uses
        context.AddStandard("M", FeaturesResources.month_day, FeaturesResources.month_day_description);
        context.AddStandard("O", FeaturesResources.round_trip_date_time, FeaturesResources.round_trip_date_time_description);
        context.AddStandard("R", FeaturesResources.rfc1123_date_time, FeaturesResources.rfc1123_date_time_description);
        context.AddStandard("s", FeaturesResources.sortable_date_time, FeaturesResources.sortable_date_time_description);
        context.AddStandard("t", FeaturesResources.short_time, FeaturesResources.short_time_description);
        context.AddStandard("T", FeaturesResources.long_time, FeaturesResources.long_time_description);
        context.AddStandard("u", FeaturesResources.universal_sortable_date_time, FeaturesResources.universal_sortable_date_time_description);
        context.AddStandard("U", FeaturesResources.universal_full_date_time, FeaturesResources.universal_full_date_time_description);
        context.AddStandard("Y", FeaturesResources.year_month, FeaturesResources.year_month_description);
    }

    private static void ProvideCustomFormats(EmbeddedCompletionContext context)
    {
        context.AddCustom("d", FeaturesResources.day_of_the_month_1_2_digits, FeaturesResources.day_of_the_month_1_2_digits_description);
        context.AddCustom("dd", FeaturesResources.day_of_the_month_2_digits, FeaturesResources.day_of_the_month_2_digits_description);
        context.AddCustom("ddd", FeaturesResources.day_of_the_week_abbreviated, FeaturesResources.day_of_the_week_abbreviated_description);
        context.AddCustom("dddd", FeaturesResources.day_of_the_week_full, FeaturesResources.day_of_the_week_full_description);

        context.AddCustom("f", FeaturesResources._10ths_of_a_second, FeaturesResources._10ths_of_a_second_description);
        context.AddCustom("ff", FeaturesResources._100ths_of_a_second, FeaturesResources._100ths_of_a_second_description);
        context.AddCustom("fff", FeaturesResources._1000ths_of_a_second, FeaturesResources._1000ths_of_a_second_description);
        context.AddCustom("ffff", FeaturesResources._10000ths_of_a_second, FeaturesResources._10000ths_of_a_second_description);
        context.AddCustom("fffff", FeaturesResources._100000ths_of_a_second, FeaturesResources._100000ths_of_a_second_description);
        context.AddCustom("ffffff", FeaturesResources._1000000ths_of_a_second, FeaturesResources._1000000ths_of_a_second_description);
        context.AddCustom("fffffff", FeaturesResources._10000000ths_of_a_second, FeaturesResources._10000000ths_of_a_second_description);

        context.AddCustom("F", FeaturesResources._10ths_of_a_second_non_zero, FeaturesResources._10ths_of_a_second_non_zero_description);
        context.AddCustom("FF", FeaturesResources._100ths_of_a_second_non_zero, FeaturesResources._100ths_of_a_second_non_zero_description);
        context.AddCustom("FFF", FeaturesResources._1000ths_of_a_second_non_zero, FeaturesResources._1000ths_of_a_second_non_zero_description);
        context.AddCustom("FFFF", FeaturesResources._10000ths_of_a_second_non_zero, FeaturesResources._10000ths_of_a_second_non_zero_description);
        context.AddCustom("FFFFF", FeaturesResources._100000ths_of_a_second_non_zero, FeaturesResources._100000ths_of_a_second_non_zero_description);
        context.AddCustom("FFFFFF", FeaturesResources._1000000ths_of_a_second_non_zero, FeaturesResources._1000000ths_of_a_second_non_zero_description);
        context.AddCustom("FFFFFFF", FeaturesResources._10000000ths_of_a_second_non_zero, FeaturesResources._10000000ths_of_a_second_non_zero_description);

        context.AddCustom("gg", FeaturesResources.period_era, FeaturesResources.period_era_description);

        context.AddCustom("h", FeaturesResources._12_hour_clock_1_2_digits, FeaturesResources._12_hour_clock_1_2_digits_description);
        context.AddCustom("hh", FeaturesResources._12_hour_clock_2_digits, FeaturesResources._12_hour_clock_2_digits_description);

        context.AddCustom("H", FeaturesResources._24_hour_clock_1_2_digits, FeaturesResources._24_hour_clock_1_2_digits_description);
        context.AddCustom("HH", FeaturesResources._24_hour_clock_2_digits, FeaturesResources._24_hour_clock_2_digits_description);

        context.AddCustom("K", FeaturesResources.time_zone, FeaturesResources.time_zone_description);

        context.AddCustom("m", FeaturesResources.minute_1_2_digits, FeaturesResources.minute_1_2_digits_description);
        context.AddCustom("mm", FeaturesResources.minute_2_digits, FeaturesResources.minute_2_digits_description);

        context.AddCustom("M", FeaturesResources.month_1_2_digits, FeaturesResources.month_1_2_digits_description);
        context.AddCustom("MM", FeaturesResources.month_2_digits, FeaturesResources.month_2_digits_description);
        context.AddCustom("MMM", FeaturesResources.month_abbreviated, FeaturesResources.month_abbreviated_description);
        context.AddCustom("MMMM", FeaturesResources.month_full, FeaturesResources.month_full_description);

        context.AddCustom("s", FeaturesResources.second_1_2_digits, FeaturesResources.second_1_2_digits_description);
        context.AddCustom("ss", FeaturesResources.second_2_digits, FeaturesResources.second_2_digits_description);

        context.AddCustom("t", FeaturesResources.AM_PM_abbreviated, FeaturesResources.AM_PM_abbreviated_description);
        context.AddCustom("tt", FeaturesResources.AM_PM_full, FeaturesResources.AM_PM_full_description);

        context.AddCustom("y", FeaturesResources.year_1_2_digits, FeaturesResources.year_1_2_digits_description);
        context.AddCustom("yy", FeaturesResources.year_2_digits, FeaturesResources.year_2_digits_description);
        context.AddCustom("yyy", FeaturesResources.year_3_4_digits, FeaturesResources.year_3_4_digits_description);
        context.AddCustom("yyyy", FeaturesResources.year_4_digits, FeaturesResources.year_4_digits_description);
        context.AddCustom("yyyyy", FeaturesResources.year_5_digits, FeaturesResources.year_5_digits_description);

        context.AddCustom("z", FeaturesResources.utc_hour_offset_1_2_digits, FeaturesResources.utc_hour_offset_1_2_digits_description);
        context.AddCustom("zz", FeaturesResources.utc_hour_offset_2_digits, FeaturesResources.utc_hour_offset_2_digits_description);
        context.AddCustom("zzz", FeaturesResources.utc_hour_and_minute_offset, FeaturesResources.utc_hour_and_minute_offset_description);

        context.AddCustom(":", FeaturesResources.time_separator, FeaturesResources.time_separator_description);
        context.AddCustom("/", FeaturesResources.date_separator, FeaturesResources.date_separator_description);
    }

    public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
        // These values have always been added by us.
        var startString = item.GetProperty(StartKey);
        var lengthString = item.GetProperty(LengthKey);
        var newText = item.GetProperty(NewTextKey);

        Contract.ThrowIfNull(startString);
        Contract.ThrowIfNull(lengthString);
        Contract.ThrowIfNull(newText);

        return Task.FromResult(CompletionChange.Create(
            new TextChange(new TextSpan(int.Parse(startString), int.Parse(lengthString)), newText)));
    }

    public override Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
    {
        if (!item.TryGetProperty(DescriptionKey, out var description))
            return SpecializedTasks.Null<CompletionDescription>();

        return Task.FromResult((CompletionDescription?)CompletionDescription.Create(
            [new TaggedText(TextTags.Text, description)]));
    }
}
