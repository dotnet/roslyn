// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

public class SyntaxTokenListTests
{
    private static IEnumerable<SyntaxToken> Tokens(SyntaxKind start, int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return SyntaxFactory.Token((SyntaxKind)((int)start + i));
        }
    }

    [Fact]
    public void Add_EmptyList()
    {
        var list = SyntaxTokenList.Empty;

        list = list.Add(SyntaxFactory.Token(SyntaxKind.Arrow));
        list = list.Add(SyntaxFactory.Token(SyntaxKind.OpenAngle));
        list = list.Add(SyntaxFactory.Token(SyntaxKind.LeftParenthesis));
        list = list.Add(SyntaxFactory.Token(SyntaxKind.RightParenthesis));

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void AddRange_EmptyList()
    {
        var list = SyntaxTokenList.Empty;

        list = list.AddRange([
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)]);

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void AddRange_TokenList()
    {
        SyntaxTokenList list1 = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        SyntaxTokenList list2 = [
            SyntaxFactory.Token(SyntaxKind.RightBracket),
            SyntaxFactory.Token(SyntaxKind.LeftBrace),
            SyntaxFactory.Token(SyntaxKind.GreaterThan)];

        var list = list1.AddRange(list2);

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightBracket, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftBrace, token.Kind),
            token => Assert.Equal(SyntaxKind.GreaterThan, token.Kind));
    }

    [Fact]
    public void AddRange_Iterator()
    {
        // This test handles the SyntaxTokenList.AddRange(...) code path
        // where TryGetCount(...) returns false.

        SyntaxTokenList list = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        list = list.AddRange(Tokens(SyntaxKind.AndAssign, 3));

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.AndAssign, token.Kind),
            token => Assert.Equal(SyntaxKind.And, token.Kind),
            token => Assert.Equal(SyntaxKind.DoubleAnd, token.Kind));
    }

    [Fact]
    public void Insert_EmptyList()
    {
        var list = SyntaxTokenList.Empty;

        list = list.Insert(list.Count, SyntaxFactory.Token(SyntaxKind.Arrow));
        list = list.Insert(list.Count, SyntaxFactory.Token(SyntaxKind.OpenAngle));
        list = list.Insert(list.Count, SyntaxFactory.Token(SyntaxKind.LeftParenthesis));
        list = list.Insert(list.Count, SyntaxFactory.Token(SyntaxKind.RightParenthesis));

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void InsertRange_EmptyList()
    {
        var list = SyntaxTokenList.Empty;

        list = list.InsertRange(list.Count, [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)]);

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void Insert_Middle()
    {
        SyntaxTokenList list = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        list = list.Insert(2, SyntaxFactory.Token(SyntaxKind.RightBracket));

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.RightBracket, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void InsertRange_Middle()
    {
        SyntaxTokenList list = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        list = list.InsertRange(2, [
            SyntaxFactory.Token(SyntaxKind.RightBracket),
            SyntaxFactory.Token(SyntaxKind.LeftBrace),
            SyntaxFactory.Token(SyntaxKind.GreaterThan)]);

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.RightBracket, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftBrace, token.Kind),
            token => Assert.Equal(SyntaxKind.GreaterThan, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void InsertRange_TokenList_Middle()
    {
        SyntaxTokenList list1 = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        SyntaxTokenList list2 = [
            SyntaxFactory.Token(SyntaxKind.RightBracket),
            SyntaxFactory.Token(SyntaxKind.LeftBrace),
            SyntaxFactory.Token(SyntaxKind.GreaterThan)];

        var list = list1.InsertRange(2, list2);

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.RightBracket, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftBrace, token.Kind),
            token => Assert.Equal(SyntaxKind.GreaterThan, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void InsertRange_Iterator_Middle()
    {
        // This test handles the SyntaxTokenList.InsertRange(...) code path
        // where TryGetCount(...) returns false.

        SyntaxTokenList list = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        list = list.InsertRange(2, Tokens(SyntaxKind.AndAssign, 3));

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.AndAssign, token.Kind),
            token => Assert.Equal(SyntaxKind.And, token.Kind),
            token => Assert.Equal(SyntaxKind.DoubleAnd, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftParenthesis, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void RemoveAt()
    {
        SyntaxTokenList list = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        var newList = list;

        foreach (var token in list)
        {
            // Compare kinds because the newList[0].Index will be
            // different once tokens are removed.
            Assert.Equal(token.Kind, newList[0].Kind);
            newList = newList.RemoveAt(0);
        }

        Assert.Empty(newList);
    }

    [Fact]
    public void Remove()
    {
        SyntaxTokenList list = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        while (list.Count > 0)
        {
            var token = list[^1];

            list = list.Remove(token);
            Assert.True(list.IndexOf(token.Kind) == -1);
        }

        Assert.Empty(list);
    }

    [Fact]
    public void Replace()
    {
        SyntaxTokenList list1 = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        SyntaxTokenList list2 = [
            SyntaxFactory.Token(SyntaxKind.Minus),
            SyntaxFactory.Token(SyntaxKind.Decrement),
            SyntaxFactory.Token(SyntaxKind.MinusAssign),
            SyntaxFactory.Token(SyntaxKind.NotEqual)];

        var newList = list1;
        Assert.Equal(list1, newList);

        for (var i = 0; i < list1.Count; i++)
        {
            newList = newList.Replace(list1[i], list2[i]);
        }

        for (var i = 0; i < list2.Count; i++)
        {
            Assert.NotEqual(list1[i], newList[i]);
            Assert.Equal(list2[i], newList[i]);
        }
    }

    [Fact]
    public void ReplaceRange()
    {
        SyntaxTokenList list = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        list = list.ReplaceRange(list[2], [
            SyntaxFactory.Token(SyntaxKind.RightBracket),
            SyntaxFactory.Token(SyntaxKind.LeftBrace),
            SyntaxFactory.Token(SyntaxKind.GreaterThan)]);

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.RightBracket, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftBrace, token.Kind),
            token => Assert.Equal(SyntaxKind.GreaterThan, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void ReplaceRange_TokenList()
    {
        SyntaxTokenList list1 = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        SyntaxTokenList list2 = [
            SyntaxFactory.Token(SyntaxKind.RightBracket),
            SyntaxFactory.Token(SyntaxKind.LeftBrace),
            SyntaxFactory.Token(SyntaxKind.GreaterThan)];

        var list = list1.ReplaceRange(list1[2], list2);

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.RightBracket, token.Kind),
            token => Assert.Equal(SyntaxKind.LeftBrace, token.Kind),
            token => Assert.Equal(SyntaxKind.GreaterThan, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }

    [Fact]
    public void ReplaceRange_Iterator()
    {
        // This test handles the SyntaxTokenList.ReplaceRange(...) code path
        // where TryGetCount(...) returns false.

        SyntaxTokenList list = [
            SyntaxFactory.Token(SyntaxKind.Arrow),
            SyntaxFactory.Token(SyntaxKind.OpenAngle),
            SyntaxFactory.Token(SyntaxKind.LeftParenthesis),
            SyntaxFactory.Token(SyntaxKind.RightParenthesis)];

        list = list.ReplaceRange(list[2], Tokens(SyntaxKind.AndAssign, 3));

        Assert.Collection(list,
            token => Assert.Equal(SyntaxKind.Arrow, token.Kind),
            token => Assert.Equal(SyntaxKind.OpenAngle, token.Kind),
            token => Assert.Equal(SyntaxKind.AndAssign, token.Kind),
            token => Assert.Equal(SyntaxKind.And, token.Kind),
            token => Assert.Equal(SyntaxKind.DoubleAnd, token.Kind),
            token => Assert.Equal(SyntaxKind.RightParenthesis, token.Kind));
    }
}
