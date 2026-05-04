// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Completion.Providers;

[DataContract]
internal readonly record struct SerializableImportCompletionItem(
    [property: DataMember(Order = 0)] string SymbolKeyData,
    [property: DataMember(Order = 1)] string Name,
    [property: DataMember(Order = 2)] int Arity,
    [property: DataMember(Order = 3)] Glyph Glyph,
    [property: DataMember(Order = 4)] string ContainingNamespace,
    [property: DataMember(Order = 5)] int AdditionalOverloadCount,
    [property: DataMember(Order = 6)] bool IncludedInTargetTypeCompletion);
