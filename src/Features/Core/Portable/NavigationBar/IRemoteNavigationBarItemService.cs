// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal interface IRemoteNavigationBarItemService
    {
        ValueTask<ImmutableArray<SerializableNavigationBarItem>> GetItemsAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, bool supportsCodeGeneration, CancellationToken cancellationToken);
    }

    [DataContract]
    internal sealed class SerializableNavigationBarItem
    {
        [DataMember(Order = 0)]
        public readonly RoslynNavigationBarItemKind Kind;

        [DataMember(Order = 1)]
        public readonly string Text;
        [DataMember(Order = 2)]
        public readonly Glyph Glyph;
        [DataMember(Order = 3)]
        public readonly bool Bolded;
        [DataMember(Order = 4)]
        public readonly bool Grayed;
        [DataMember(Order = 5)]
        public readonly int Indent;
        [DataMember(Order = 6)]
        public readonly ImmutableArray<SerializableNavigationBarItem> ChildItems;
        [DataMember(Order = 7)]
        public readonly ImmutableArray<TextSpan> Spans;

        // Set when kind == RoslynNavigationBarItemKind.Symbol

        [DataMember(Order = 8)]
        public readonly SymbolKey? NavigationSymbolId;

        [DataMember(Order = 9)]
        public readonly int NavigationSymbolIndex;

        // Set for all GenerateCode kinds.

        [DataMember(Order = 10)]
        public readonly SymbolKey? DestinationTypeSymbolKey;

        // Set for GenerateEventHandler

        [DataMember(Order = 11)]
        public readonly string? ContainerName;
        [DataMember(Order = 12)]
        public readonly SymbolKey? EventSymbolKey;

        // Set for GenerateMethod

        [DataMember(Order = 13)]
        public readonly SymbolKey? MethodToReplicateSymbolKey;

        private SerializableNavigationBarItem(
            RoslynNavigationBarItemKind kind,
            string text,
            Glyph glyph,
            bool bolded,
            bool grayed,
            int indent,
            ImmutableArray<SerializableNavigationBarItem> childItems,
            ImmutableArray<TextSpan> spans,
            SymbolKey? navigationSymbolId,
            int navigationSymbolIndex,
            SymbolKey? destinationTypeSymbolKey,
            string? containerName,
            SymbolKey? eventSymbolKey,
            SymbolKey? methodToReplicateSymbolKey)
        {
            Kind = kind;
            Text = text;
            Glyph = glyph;
            Spans = spans.NullToEmpty();
            ChildItems = childItems.NullToEmpty();
            Indent = indent;
            Bolded = bolded;
            Grayed = grayed;
            NavigationSymbolId = navigationSymbolId;
            NavigationSymbolIndex = navigationSymbolIndex;
            DestinationTypeSymbolKey = destinationTypeSymbolKey;
            ContainerName = containerName;
            EventSymbolKey = eventSymbolKey;
            MethodToReplicateSymbolKey = methodToReplicateSymbolKey;
        }

        public RoslynNavigationBarItem Rehydrate()
            => this.Kind switch
            {
                RoslynNavigationBarItemKind.Symbol => new RoslynNavigationBarItem.SymbolItem(Text, Glyph, Spans, NavigationSymbolId!.Value, NavigationSymbolIndex, ChildItems.SelectAsArray(i => i.Rehydrate()), Indent, Bolded, Grayed),
                RoslynNavigationBarItemKind.GenerateDefaultConstructor => new RoslynNavigationBarItem.GenerateDefaultConstructor(Text, DestinationTypeSymbolKey!.Value),
                RoslynNavigationBarItemKind.GenerateEventHandler => new RoslynNavigationBarItem.GenerateEventHandler(Text, Glyph, ContainerName!, EventSymbolKey!.Value, DestinationTypeSymbolKey!.Value),
                RoslynNavigationBarItemKind.GenerateFinalizer => new RoslynNavigationBarItem.GenerateFinalizer(Text, DestinationTypeSymbolKey!.Value),
                RoslynNavigationBarItemKind.GenerateMethod => new RoslynNavigationBarItem.GenerateMethod(Text, Glyph, DestinationTypeSymbolKey!.Value, MethodToReplicateSymbolKey!.Value),
                RoslynNavigationBarItemKind.Actionless => new RoslynNavigationBarItem.ActionlessItem(Text, Glyph, Spans, ChildItems.SelectAsArray(v => v.Rehydrate()), Indent, Bolded, Grayed),
                _ => throw ExceptionUtilities.UnexpectedValue(this.Kind),
            };

        public static ImmutableArray<SerializableNavigationBarItem> Dehydrate(ImmutableArray<RoslynNavigationBarItem> values)
            => values.SelectAsArray(v => v.Dehydrate());

        public static SerializableNavigationBarItem ActionlessItem(string text, Glyph glyph, ImmutableArray<TextSpan> spans, ImmutableArray<SerializableNavigationBarItem> childItems = default, int indent = 0, bool bolded = false, bool grayed = false)
            => new(RoslynNavigationBarItemKind.Actionless, text, glyph, bolded, grayed, indent, childItems, spans, null, 0, null, null, null, null);

        public static SerializableNavigationBarItem SymbolItem(string text, Glyph glyph, ImmutableArray<TextSpan> spans, SymbolKey navigationSymbolId, int navigationSymbolIndex, ImmutableArray<SerializableNavigationBarItem> childItems = default, int indent = 0, bool bolded = false, bool grayed = false)
            => new(RoslynNavigationBarItemKind.Symbol, text, glyph, bolded, grayed, indent, childItems, spans, navigationSymbolId, navigationSymbolIndex, null, null, null, null);

        public static SerializableNavigationBarItem GenerateFinalizer(string text, SymbolKey destinationTypeSymbolKey)
            => new(RoslynNavigationBarItemKind.GenerateFinalizer, text, Glyph.MethodProtected, bolded: false, grayed: false, indent: 0, default, default, null, 0, destinationTypeSymbolKey, null, null, null);

        public static SerializableNavigationBarItem GenerateEventHandler(string eventName, Glyph glyph, string containerName, SymbolKey eventSymbolKey, SymbolKey destinationTypeSymbolKey)
            => new(RoslynNavigationBarItemKind.GenerateEventHandler, eventName, glyph, bolded: false, grayed: false, indent: 0, default, default, null, 0, destinationTypeSymbolKey, containerName, eventSymbolKey, null);

        public static SerializableNavigationBarItem GenerateMethod(string text, Glyph glyph, SymbolKey destinationTypeSymbolId, SymbolKey methodToReplicateSymbolId)
            => new(RoslynNavigationBarItemKind.GenerateMethod, text, glyph, bolded: false, grayed: false, indent: 0, default, default, null, 0, destinationTypeSymbolId, null, null, methodToReplicateSymbolId);

        public static SerializableNavigationBarItem GenerateDefaultConstructor(string text, SymbolKey destinationTypeSymbolKey)
            => new(RoslynNavigationBarItemKind.GenerateDefaultConstructor, text, Glyph.MethodPublic, bolded: false, grayed: false, indent: 0, default, default, null, 0, destinationTypeSymbolKey, null, null, null);
    }
}
