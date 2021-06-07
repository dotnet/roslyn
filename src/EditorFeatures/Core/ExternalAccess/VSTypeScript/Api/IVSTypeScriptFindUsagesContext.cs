// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptFindUsagesContext
    {
        /// <summary>
        /// Used for clients that are finding usages to push information about how far along they
        /// are in their search.
        /// </summary>
        IVSTypeScriptStreamingProgressTracker ProgressTracker { get; }

        /// <summary>
        /// Report a message to be displayed to the user.
        /// </summary>
        ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken);

        /// <summary>
        /// Set the title of the window that results are displayed in.
        /// </summary>
        ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken);

        ValueTask OnDefinitionFoundAsync(VSTypeScriptDefinitionItem definition, CancellationToken cancellationToken);
        ValueTask OnReferenceFoundAsync(VSTypeScriptSourceReferenceItem reference, CancellationToken cancellationToken);
    }

    internal interface IVSTypeScriptStreamingProgressTracker
    {
        ValueTask AddItemsAsync(int count, CancellationToken cancellationToken);
        ValueTask ItemCompletedAsync(CancellationToken cancellationToken);
    }

    internal class VSTypeScriptDefinitionItem
    {
        public VSTypeScriptDefinitionItem(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<DocumentSpan> sourceSpans,
            ImmutableArray<TaggedText> nameDisplayParts = default,
            ImmutableDictionary<string, string>? properties = null,
            ImmutableDictionary<string, string>? displayableProperties = null,
            bool displayIfNoReferences = true)
        {
            Tags = tags;
            DisplayParts = displayParts;
            SourceSpans = sourceSpans;
            NameDisplayParts = nameDisplayParts;
            Properties = properties;
            DisplayableProperties = displayableProperties;
            DisplayIfNoReferences = displayIfNoReferences;
        }

        public ImmutableArray<string> Tags { get; }
        public ImmutableArray<TaggedText> DisplayParts { get; }
        public ImmutableArray<DocumentSpan> SourceSpans { get; }
        public ImmutableArray<TaggedText> NameDisplayParts { get; }
        public ImmutableDictionary<string, string>? Properties { get; }
        public ImmutableDictionary<string, string>? DisplayableProperties { get; }
        public bool DisplayIfNoReferences { get; }
    }

    internal class VSTypeScriptSourceReferenceItem
    {
        public VSTypeScriptSourceReferenceItem(
            VSTypeScriptDefinitionItem definition,
            DocumentSpan sourceSpan,
            SymbolUsageInfo symbolUsageInfo)
        {
            Definition = definition;
            SourceSpan = sourceSpan;
            SymbolUsageInfo = symbolUsageInfo;
        }

        public VSTypeScriptDefinitionItem Definition { get; }
        public DocumentSpan SourceSpan { get; }
        public SymbolUsageInfo SymbolUsageInfo { get; }
    }
}
