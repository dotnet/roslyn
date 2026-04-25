// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Protocol;

/// <summary>
/// Interface for delegated params that enables sharing of code in RazorCustomMessageTarget
/// </summary>
internal interface IDelegatedParams
{
    TextDocumentIdentifierAndVersion Identifier { get; }
    RazorLanguageKind ProjectedKind { get; }
}
