// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.SpellCheck;

namespace Microsoft.CodeAnalysis.Remote.Razor.SpellCheck;

[Export(typeof(ISpellCheckService)), Shared]
[method: ImportingConstructor]
internal sealed class OOPSpellCheckService(
    ICSharpSpellCheckRangeProvider csharpSpellCheckService,
    IDocumentMappingService documentMappingService)
    : SpellCheckService(csharpSpellCheckService, documentMappingService)
{
}
