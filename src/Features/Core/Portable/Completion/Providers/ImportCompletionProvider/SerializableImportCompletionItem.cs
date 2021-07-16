// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    [DataContract]
    internal readonly struct SerializableImportCompletionItem
    {
        [DataMember(Order = 0)]
        public readonly string SymbolKeyData;

        [DataMember(Order = 1)]
        public readonly string Name;

        [DataMember(Order = 2)]
        public readonly int Arity;

        [DataMember(Order = 3)]
        public readonly Glyph Glyph;

        [DataMember(Order = 4)]
        public readonly string ContainingNamespace;

        [DataMember(Order = 5)]
        public readonly int AdditionalOverloadCount;

        [DataMember(Order = 6)]
        public readonly bool IncludedInTargetTypeCompletion;

        public SerializableImportCompletionItem(string symbolKeyData, string name, int arity, Glyph glyph, string containingNamespace, int additionalOverloadCount, bool includedInTargetTypeCompletion)
        {
            SymbolKeyData = symbolKeyData;
            Arity = arity;
            Name = name;
            Glyph = glyph;
            ContainingNamespace = containingNamespace;
            AdditionalOverloadCount = additionalOverloadCount;
            IncludedInTargetTypeCompletion = includedInTargetTypeCompletion;
        }
    }
}
