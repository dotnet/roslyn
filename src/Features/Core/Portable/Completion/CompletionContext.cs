// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion;

/// <summary>
/// The context presented to a <see cref="CompletionProvider"/> when providing completions.
/// </summary>
public sealed class CompletionContext
{
    private readonly SegmentedList<CompletionItem> _items = [];

    private CompletionItem? _suggestionModeItem;
    private bool _isExclusive;

    internal CompletionProvider Provider { get; }

    /// <summary>
    /// The document that completion was invoked within.
    /// </summary>
    public Document Document { get; }

    /// <summary>
    /// The caret position when completion was triggered.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// By providing this object, we have an opportunity to share requested SyntaxContext among all CompletionProviders
    /// during a completion session to reduce repeat computation.
    /// </summary>
    private SharedSyntaxContextsWithSpeculativeModel? SharedSyntaxContextsWithSpeculativeModel { get; }

    /// <summary>
    /// The span of the syntax element at the caret position.
    /// 
    /// This is the most common value used for <see cref="CompletionItem.Span"/> and will
    /// be automatically assigned to any <see cref="CompletionItem"/> that has no <see cref="CompletionItem.Span"/> specified.
    /// </summary>
    [Obsolete("Not used anymore. Use CompletionListSpan instead.", error: true)]
    public TextSpan DefaultItemSpan { get; }

#pragma warning disable RS0030 // Do not used banned APIs
    /// <summary>
    /// The span of the document the completion list corresponds to.  It will be set initially to
    /// the result of <see cref="CompletionService.GetDefaultCompletionListSpan"/>, but it can
    /// be overwritten during <see cref="CompletionService.GetCompletionsAsync(Document, int, CompletionTrigger, ImmutableHashSet{string}, OptionSet, CancellationToken)"/>.
    /// The purpose of the span is to:
    ///     1. Signify where the completions should be presented.
    ///     2. Designate any existing text in the document that should be used for filtering.
    ///     3. Specify, by default, what portion of the text should be replaced when a completion 
    ///        item is committed.
    /// </summary>
    public TextSpan CompletionListSpan { get; set; }
#pragma warning restore

    /// <summary>
    /// The triggering action that caused completion to be started.
    /// </summary>
    public CompletionTrigger Trigger { get; }

    /// <summary>
    /// The options that completion was started with.
    /// </summary>
    internal CompletionOptions CompletionOptions { get; }

    /// <summary>
    /// The cancellation token to use for this operation.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Set to true if the items added here should be the only items presented to the user.
    /// Expand items should never be exclusive.
    /// </summary>
    public bool IsExclusive
    {
        get
        {
            return _isExclusive && !Provider.IsExpandItemProvider;
        }

        set
        {
            if (value)
                Debug.Assert(!Provider.IsExpandItemProvider);

            _isExclusive = value;
        }
    }

    /// <summary>
    /// The options that completion was started with.
    /// </summary>
    public OptionSet Options { get; }

    /// <summary>
    /// Creates a <see cref="CompletionContext"/> instance.
    /// </summary>
    public CompletionContext(
        CompletionProvider provider,
        Document document,
        int position,
        TextSpan defaultSpan,
        CompletionTrigger trigger,
        OptionSet? options,
        CancellationToken cancellationToken)
        : this(provider ?? throw new ArgumentNullException(nameof(provider)),
               document ?? throw new ArgumentNullException(nameof(document)),
               position,
               sharedSyntaxContextsWithSpeculativeModel: null,
               defaultSpan,
               trigger,
               // Publicly available options do not affect this API.
               CompletionOptions.Default,
               cancellationToken)
    {
#pragma warning disable RS0030 // Do not used banned APIs
        Options = options ?? OptionSet.Empty;
#pragma warning restore
    }

    /// <summary>
    /// Creates a <see cref="CompletionContext"/> instance.
    /// </summary>
    internal CompletionContext(
        CompletionProvider provider,
        Document document,
        int position,
        SharedSyntaxContextsWithSpeculativeModel? sharedSyntaxContextsWithSpeculativeModel,
        TextSpan defaultSpan,
        CompletionTrigger trigger,
        in CompletionOptions options,
        CancellationToken cancellationToken)
    {
        Provider = provider;
        Document = document;
        Position = position;
        CompletionListSpan = defaultSpan;
        Trigger = trigger;
        CompletionOptions = options;
        CancellationToken = cancellationToken;

        SharedSyntaxContextsWithSpeculativeModel = sharedSyntaxContextsWithSpeculativeModel;

#pragma warning disable RS0030 // Do not used banned APIs
        Options = OptionSet.Empty;
#pragma warning restore
    }

    internal IReadOnlyList<CompletionItem> Items => _items;

    public void AddItem(CompletionItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        item = FixItem(item);
        _items.Add(item);
    }

    public void AddItems(IEnumerable<CompletionItem> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    /// <summary>
    /// An optional <see cref="CompletionItem"/> that appears selected in the list presented to the user during suggestion mode.
    /// 
    /// Suggestion mode disables auto-selection of items in the list, giving preference to the text typed by the user unless a specific item is selected manually.
    /// 
    /// Specifying a <see cref="SuggestionModeItem"/> is a request that the completion host operate in suggestion mode.
    /// The item specified determines the text displayed and the description associated with it unless a different item is manually selected.
    /// 
    /// No text is ever inserted when this item is completed, leaving the text the user typed instead.
    /// </summary>
    public CompletionItem? SuggestionModeItem
    {
        get
        {
            return _suggestionModeItem;
        }

        set
        {
            if (value != null)
            {
                value = FixItem(value);
            }

            _suggestionModeItem = value;
        }
    }

    internal Task<SyntaxContext> GetSyntaxContextWithExistingSpeculativeModelAsync(Document document, CancellationToken cancellationToken)
    {
        if (SharedSyntaxContextsWithSpeculativeModel is null)
            return Utilities.CreateSyntaxContextWithExistingSpeculativeModelAsync(document, Position, cancellationToken);

        return SharedSyntaxContextsWithSpeculativeModel.GetSyntaxContextAsync(document, cancellationToken);
    }

    private CompletionItem FixItem(CompletionItem item)
    {
        // remember provider so we can find it again later
        item.ProviderName = Provider.Name;

        item.Span = CompletionListSpan;

        return item;
    }
}
