// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal readonly struct SerializableImportCompletionItem
    {
        public readonly string SymbolKeyData;
        public readonly int Arity;
        public readonly string Name;
        public readonly Glyph Glyph;
        public readonly string ContainingNamespace;

        public SerializableImportCompletionItem(string symbolKeyData, string name, int arity, Glyph glyph, string containingNamespace)
        {
            SymbolKeyData = symbolKeyData;
            Arity = arity;
            Name = name;
            Glyph = glyph;
            ContainingNamespace = containingNamespace;
        }
    }
}
