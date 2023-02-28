﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class SemanticColorizerOptionsStorage
    {
        public static readonly Option2<bool> SemanticColorizer = new("dotnet_enable_semantic_colorizer", defaultValue: true);
    }
}
