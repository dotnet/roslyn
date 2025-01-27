// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

internal readonly struct PythiaSyntaxFactsServiceWrapper
{
    internal readonly ISyntaxFactsService UnderlyingObject;

    internal PythiaSyntaxFactsServiceWrapper(ISyntaxFactsService underlyingObject)
        => UnderlyingObject = underlyingObject;

    public static PythiaSyntaxFactsServiceWrapper Create(Document document)
        => new(document.GetRequiredLanguageService<ISyntaxFactsService>());
}
