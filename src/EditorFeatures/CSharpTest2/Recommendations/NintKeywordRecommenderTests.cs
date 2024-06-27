﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

public class NintKeywordRecommenderTests : NativeIntegerKeywordRecommenderTests
{
    protected override string KeywordText => "nint";

    private protected override AbstractNativeIntegerKeywordRecommender Recommender => new NintKeywordRecommender();
}
