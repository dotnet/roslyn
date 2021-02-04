// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    /// <summary>
    /// Base type of all C#/VB navigation bar items.  Only for use internally to roslyn.
    /// </summary>
    [DataContract]
    internal sealed class RoslynNavigationBarItem
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
        public readonly ImmutableArray<RoslynNavigationBarItem> ChildItems;
        [DataMember(Order = 7)]
        public readonly ImmutableArray<TextSpan> Spans;

        // Set when kind == RoslynNavigationBarItemKind.Symbol

        [DataMember(Order = 8)]
        public readonly SymbolKey NavigationSymbolId;
        [DataMember(Order = 9)]
        public readonly int? NavigationSymbolIndex;

        // Set for all GenerateCode kinds.

        [DataMember(Order = 10)]
        public readonly SymbolKey DestinationTypeSymbolKey;

        // Set for GenerateEventHandler

        [DataMember(Order = 11)]
        public readonly string? ContainerName;
        [DataMember(Order = 12)]
        public readonly SymbolKey EventSymbolKey;

        // Set for GenerateMethod

        [DataMember(Order = 13)]
        public readonly SymbolKey MethodToReplicateSymbolKey;

        public RoslynNavigationBarItem(
            RoslynNavigationBarItemKind kind,
            string text,
            Glyph glyph,
            bool bolded,
            bool grayed,
            int indent,
            ImmutableArray<RoslynNavigationBarItem> childItems,
            ImmutableArray<TextSpan> spans,
            SymbolKey navigationSymbolId,
            int? navigationSymbolIndex,
            SymbolKey destinationTypeSymbolKey,
            string? containerName,
            SymbolKey eventSymbolKey,
            SymbolKey methodToReplicateSymbolKey)
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

        public static RoslynNavigationBarItem ActionlessItem(
            string text,
            Glyph glyph,
            ImmutableArray<TextSpan> spans,
            ImmutableArray<RoslynNavigationBarItem> childItems = default,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            return new(RoslynNavigationBarItemKind.Actionless, text, glyph, bolded, grayed, indent, childItems, spans, default, null, default, null, default, default);
        }

        public static RoslynNavigationBarItem SymbolItem(
            string text,
            Glyph glyph,
            ImmutableArray<TextSpan> spans,
            SymbolKey navigationSymbolId,
            int? navigationSymbolIndex,
            ImmutableArray<RoslynNavigationBarItem> childItems = default,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            return new(RoslynNavigationBarItemKind.Symbol, text, glyph, bolded, grayed, indent, childItems, spans, navigationSymbolId, navigationSymbolIndex, default, null, default, default);
        }

        public static RoslynNavigationBarItem GenerateFinalizer(string text, SymbolKey destinationTypeSymbolKey)
        {
            return new(RoslynNavigationBarItemKind.GenerateFinalizer, text, Glyph.MethodProtected,
                bolded: false, grayed: false, indent: 0, default, default, default, null, destinationTypeSymbolKey, null, default, default);
        }

        public static RoslynNavigationBarItem GenerateEventHandler(string eventName, Glyph glyph, string containerName, SymbolKey eventSymbolKey, SymbolKey destinationTypeSymbolKey)
        {
            return new(RoslynNavigationBarItemKind.GenerateEventHandler, eventName, glyph,
                bolded: false, grayed: false, indent: 0, default, default, default, null, destinationTypeSymbolKey, containerName, eventSymbolKey, default);
        }

        public static RoslynNavigationBarItem GenerateMethod(string text, Glyph glyph, SymbolKey destinationTypeSymbolId, SymbolKey methodToReplicateSymbolId)
        {
            return new(RoslynNavigationBarItemKind.GenerateMethod, text, glyph,
                bolded: false, grayed: false, indent: 0, default, default, default, null, destinationTypeSymbolId, null, default, methodToReplicateSymbolId);
        }

        public static RoslynNavigationBarItem GenerateDefaultConstructor(string text, SymbolKey destinationTypeSymbolKey)
        {
            return new(RoslynNavigationBarItemKind.GenerateDefaultConstructor, text, Glyph.MethodPublic,
                bolded: false, grayed: false, indent: 0, default, default, default, null, destinationTypeSymbolKey, null, default, default);
        }
    }
}
