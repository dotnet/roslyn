// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal partial class NavigableItemFactory
    {
        internal class DeclaredSymbolNavigableItem : INavigableItem
        {
            private readonly DeclaredSymbolInfo _declaredSymbolInfo;

            public Document Document { get; }

            public ImmutableArray<TaggedText> DisplayTaggedParts
                => ImmutableArray.Create(new TaggedText(TextTags.Text, _declaredSymbolInfo.FinalDisplayName));

            public Glyph Glyph => GetGlyph(_declaredSymbolInfo.Kind, _declaredSymbolInfo.Accessibility);

            public TextSpan SourceSpan => _declaredSymbolInfo.Span;

            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;
            public bool DisplayFileLocation => false;

            /// <summary>
            /// DeclaredSymbolInfos always come from some actual declaration in source.  So they're
            /// never implicitly declared.
            /// </summary>
            public bool IsImplicitlyDeclared => false;

            public DeclaredSymbolNavigableItem(
                Document document, DeclaredSymbolInfo declaredSymbolInfo)
            {
                Document = document;
                _declaredSymbolInfo = declaredSymbolInfo;
            }

            private static Glyph GetPublicGlyph(DeclaredSymbolInfoKind kind)
            {
                switch (kind)
                {
                    case DeclaredSymbolInfoKind.Class: return Glyph.ClassPublic;
                    case DeclaredSymbolInfoKind.Constant: return Glyph.ConstantPublic;
                    case DeclaredSymbolInfoKind.Delegate: return Glyph.DelegatePublic;
                    case DeclaredSymbolInfoKind.Enum: return Glyph.EnumPublic;
                    case DeclaredSymbolInfoKind.Event: return Glyph.EventPublic;
                    case DeclaredSymbolInfoKind.ExtensionMethod: return Glyph.ExtensionMethodPublic;
                    case DeclaredSymbolInfoKind.Field: return Glyph.FieldPublic;
                    case DeclaredSymbolInfoKind.Indexer: return Glyph.PropertyPublic;
                    case DeclaredSymbolInfoKind.Interface: return Glyph.InterfacePublic;
                    case DeclaredSymbolInfoKind.Method: return Glyph.MethodPublic;
                    case DeclaredSymbolInfoKind.Module: return Glyph.ModulePublic;
                    case DeclaredSymbolInfoKind.Property: return Glyph.PropertyPublic;
                    case DeclaredSymbolInfoKind.Struct: return Glyph.StructurePublic;
                    default: return Glyph.ClassPublic;
                }
            }

            private static Glyph GetGlyph(DeclaredSymbolInfoKind kind, Accessibility accessibility)
            {
                // EnumMembers have no accessibility.
                if (kind == DeclaredSymbolInfoKind.EnumMember)
                {
                    return Glyph.EnumMember;
                }

                // Glyphs are stored in this order:
                //  ClassPublic,
                //  ClassProtected,
                //  ClassPrivate,
                //  ClassInternal,

                var rawGlyph = GetPublicGlyph(kind);

                switch (accessibility)
                {
                    case Accessibility.Protected: rawGlyph += 1; break;
                    case Accessibility.Private: rawGlyph += 2; break;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.ProtectedAndInternal: rawGlyph += 3; break;
                }

                return rawGlyph;
            }
        }
    }
}