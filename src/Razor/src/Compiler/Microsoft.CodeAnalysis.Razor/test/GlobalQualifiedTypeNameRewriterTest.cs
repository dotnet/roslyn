// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class GlobalQualifiedTypeNameRewriterTest
{
    [Theory]
    [InlineData("String", "global::String")]
    [InlineData("System.String", "global::System.String")]
    [InlineData("TItem2", "TItem2")]
    [InlineData("System.Collections.Generic.List<System.String>", "global::System.Collections.Generic.List<global::System.String>")]
    [InlineData("System.Collections.Generic.Dictionary<System.String, TItem1>", "global::System.Collections.Generic.Dictionary<global::System.String, TItem1>")]
    [InlineData("System.Collections.TItem3.Dictionary<System.String, TItem1>", "global::System.Collections.TItem3.Dictionary<global::System.String, TItem1>")]
    [InlineData("System.Collections.TItem3.TItem1<System.String, TItem1>", "global::System.Collections.TItem3.TItem1<global::System.String, TItem1>")]
    [InlineData("M.RenderFragment<(N.MyClass I1, N.MyStruct I2, TItem1 P)>", "global::M.RenderFragment<(global::N.MyClass I1, global::N.MyStruct I2, TItem1 P)>")]
    [InlineData("global::System.String", "global::System.String")]
    [InlineData("Alias::System.String", "Alias::System.String")]
    [InlineData("global::System.Collections.Generic.List<System.String>", "global::System.Collections.Generic.List<global::System.String>")]
    [InlineData("Alias::Outer<System.String>.Inner<TItem1>", "Alias::Outer<global::System.String>.Inner<TItem1>")]
    [InlineData("N.Outer<System.String>.Inner<M.List<TItem1>>", "global::N.Outer<global::System.String>.Inner<global::M.List<TItem1>>")]
    [InlineData("System.String?[,][]", "global::System.String?[,][]")]
    [InlineData("(System.String Text, TItem1 Item)", "(global::System.String Text, TItem1 Item)")]
    [InlineData("List<System.String>", "List<global::System.String>")]
    [InlineData("TItem1.Member", "global::TItem1.Member")]
    [InlineData("@namespace.@class", "global::@namespace.@class")]
    [InlineData(" /* leading */ System /* left */ . /* right */ String /* trailing */ ", " /* leading */ global::System /* left */ . /* right */ String /* trailing */ ")]
    [InlineData(" /* leading */ N.Outer< /* argument */ System.String /* end */ , TItem1>.Inner /* trailing */ ", " /* leading */ global::N.Outer< /* argument */ global::System.String /* end */ , TItem1>.Inner /* trailing */ ")]
    [InlineData("global /* alias */ :: /* name */ System.String", "global /* alias */ :: /* name */ System.String")]
    [InlineData("System.", "global::System.")]
    [InlineData("N.List<System.String", "global::N.List<global::System.String")]
    [InlineData("N.List<>", "global::N.List<>")]
    [InlineData("N.List<System.String,>", "global::N.List<global::System.String,>")]

    // This case is interesting because we know TITem2 to be a generic type parameter,
    // and we know that this will never be valid, which is why we don't bother rewriting.
    [InlineData("TItem2<System.String, TItem1>", "TItem2<global::System.String, TItem1>")]
    public void GlobalQualifiedTypeNameRewriter_CanQualifyNames(string original, string expected)
    {
        // Arrange
        var visitor = new GlobalQualifiedTypeNameRewriter(new[] { "TItem1", "TItem2", "TItem3" });

        // Act
        var actual = visitor.Rewrite(original);

        // Assert
        Assert.Equal(expected, actual.ToString());
    }

    [Theory]
    [InlineData("", "global::")]
    [InlineData("global::", "global::")]
    [InlineData("Alias::", "Alias::")]
    public void GlobalQualifiedTypeNameRewriter_CanQualifyLongNames(string prefix, string expectedPrefix)
    {
        var namespaceName = string.Join(".", Enumerable.Repeat("N", 128));
        var rewriter = new GlobalQualifiedTypeNameRewriter(new[] { "TItem1" });

        var actual = rewriter.Rewrite(prefix + namespaceName + ".Outer<System.String>.Inner<TItem1>");

        Assert.Equal(expectedPrefix + namespaceName + ".Outer<global::System.String>.Inner<TItem1>", actual);
    }
}
