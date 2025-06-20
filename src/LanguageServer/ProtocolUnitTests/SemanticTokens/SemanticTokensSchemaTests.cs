// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens;

public class SemanticTokensSchemaTests
{
    [Fact]
    public void TestAllAdditiveClassificationsMappedToTokenModifiers()
    {
        foreach (var additiveClassification in ClassificationTypeNames.AdditiveTypeNames)
        {
            Assert.True(SemanticTokensSchema.AdditiveClassificationTypeToTokenModifier.ContainsKey(additiveClassification), $"Modifier '{additiveClassification}' is not mapped to a {nameof(TokenModifiers)}");
        }
    }
}
