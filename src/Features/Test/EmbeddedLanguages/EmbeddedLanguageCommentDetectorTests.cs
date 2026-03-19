// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EmbeddedLanguages;

public sealed class EmbeddedLanguageCommentDetectorTests
{
    [Theory]
    [InlineData("// lang=c#")]
    [InlineData("// lang=C#")]
    [InlineData("// Lang=c#")]
    [InlineData("// Lang=C#")]
    [InlineData("// lang=c#-test")]
    [InlineData("// Lang=c#-test")]
    [InlineData("// lang=C#-test")]
    [InlineData("// Lang=C#-test")]
    [InlineData("// Lang=C#-Test")]
    [InlineData("//lang=c#")]
    [InlineData("//lang=c#-test")]
    [InlineData("//language=c#")]
    [InlineData("//language=c#-test")]
    [InlineData("// lang=c#.")]
    [InlineData("// lang=c#-test.")]
    public void TestCSharpDetection_Positive(string commentText)
    {
        var detector = new EmbeddedLanguageCommentDetector([LanguageNames.CSharp, PredefinedEmbeddedLanguageNames.CSharpTest]);
        Assert.True(detector.TryMatch(commentText, out _, out _));
        Assert.True(detector.TryMatch(commentText + " ", out _, out _));

        Assert.True(detector.TryMatch(commentText + " This is C# code", out _, out _));
    }

    [Theory]
    [InlineData("//c#")]
    [InlineData("//lan=c#")]
    [InlineData("//lang c#")]
    [InlineData("// c#")]
    [InlineData("// lan=c#")]
    [InlineData("// lang c#")]
    [InlineData("// lang=c")]
    [InlineData("// lang=csharp")]
    [InlineData("// lang=f#")]
    [InlineData("// lang=#")]
    [InlineData("// lang=.")]
    public void TestCSharpDetection_Negative(string commentText)
    {
        var detector = new EmbeddedLanguageCommentDetector([LanguageNames.CSharp, PredefinedEmbeddedLanguageNames.CSharpTest]);
        Assert.False(detector.TryMatch(commentText, out _, out _));
    }
}
