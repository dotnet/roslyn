// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public static class LspTestCompositions
{
    public static readonly TestComposition LanguageServerProtocol = FeaturesTestCompositions.Features
        .AddAssemblies(typeof(LanguageServerProtocolResources).Assembly);
}
