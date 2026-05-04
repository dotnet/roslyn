// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class CSharpSyntaxFactsServiceTests
{
    private static bool IsQueryKeyword(string markup)
    {
        MarkupTestFile.GetPosition(markup, out var code, out int position);
        var tree = SyntaxFactory.ParseSyntaxTree(code);
        var token = tree.GetRoot().FindToken(position);
        var service = CSharpSyntaxFacts.Instance;

        return service.IsQueryKeyword(token);
    }

    private static string WrapInMethod(string methodBody)
    {
        return $$"""
            class C
            {
                void M() 
                {
                    {{methodBody}}
                }
            }
            """;
    }

    [Fact]
    public void IsQueryKeyword_From()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var result = $$from var1 in collection1
            """)));

    [Fact]
    public void IsQueryKeyword_In()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var result = from var1 $$in collection1
            """)));

    [Fact]
    public void IsQueryKeyword_Where()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var customerOrders = from cust in customers
                                 $$where cust.CustomerID = 1
            """)));

    [Fact]
    public void IsQueryKeyword_Select()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var customerOrders = from cust in customers
                                 from ord in orders
                                 where cust.CustomerID == ord.CustomerID
                                 $$select cust.CompanyName, ord.OrderDate
            """)));

    [Fact]
    public void IsQueryKeyword_GroupBy_Group()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var customersByCountry = from cust in customers
                                     $$group cust by cust.Country
                                     into g
            """)));

    [Fact]
    public void IsQueryKeyword_GroupBy_By()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var customersByCountry = from cust in customers
                                     group cust $$by cust.Country
                                     into g
            """)));

    [Fact]
    public void IsQueryKeyword_Into()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var customersByCountry = from cust in customers
                                     group cust by cust.Country
                                     $$into g
            """)));

    [Fact]
    public void IsQueryKeyword_GroupJoin_Join()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        $$join pet in pets on person equals pet.Owner into gj
                        select new { OwnerName = person.FirstName, Pets = gj };
            """)));

    [Fact]
    public void IsQueryKeyword_GroupJoin_In()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        join pet $$in pets on person equals pet.Owner into gj
                        select new { OwnerName = person.FirstName, Pets = gj };
            """)));

    [Fact]
    public void IsQueryKeyword_GroupJoin_On()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        join pet in pets $$on person equals pet.Owner into gj
                        select new { OwnerName = person.FirstName, Pets = gj };
            """)));

    [Fact]
    public void IsQueryKeyword_GroupJoin_Equals()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        join pet in pets on person $$equals pet.Owner into gj
                        select new { OwnerName = person.FirstName, Pets = gj };
            """)));

    [Fact]
    public void IsQueryKeyword_GroupJoin_Into()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        join pet in pets on person equals pet.Owner $$into gj
                        select new { OwnerName = person.FirstName, Pets = gj };
            """)));

    [Fact]
    public void IsQueryKeyword_Join_Join()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        $$join pet in pets on person equals pet.Owner
                        select new { OwnerName = person.FirstName, PetName = pet.Name };
            """)));

    [Fact]
    public void IsQueryKeyword_Join_In()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        join pet $$in pets on person equals pet.Owner
                        select new { OwnerName = person.FirstName, PetName = pet.Name };
            """)));

    [Fact]
    public void IsQueryKeyword_Join_On()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        join pet in pets $$on person equals pet.Owner
                        select new { OwnerName = person.FirstName, PetName = pet.Name };
            """)));

    [Fact]
    public void IsQueryKeyword_Join_Equals()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var query = from person in people
                        join pet in pets on person $$equals pet.Owner
                        select new { OwnerName = person.FirstName, PetName = pet.Name };
            """)));

    [Fact]
    public void IsQueryKeyword_Let()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var discountedProducts = from prod in products
                                     $$let discount = prod.UnitPrice * 0.1
                                     where discount >= 50
                                     select new { prod.ProductName, prod.UnitPrice, Discount }
            """)));

    [Fact]
    public void IsQueryKeyword_OrderBy_OrderBy()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var titlesDescendingPrice = from book in books
                                        $$orderby book.Price descending, book.Title ascending, book.Author
                                        select new { book.Title, book.Price }
            """)));

    [Fact]
    public void IsQueryKeyword_OrderBy_Descending()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var titlesDescendingPrice = from book in books
                                        orderby book.Price $$descending, book.Title ascending, book.Author
                                        select new { book.Title, book.Price }
            """)));

    [Fact]
    public void IsQueryKeyword_OrderBy_Ascending()
        => Assert.True(IsQueryKeyword(WrapInMethod("""
            var titlesDescendingPrice = from book in books
                                        orderby book.Price descending, book.Title $$ascending, book.Author
                                        select new { book.Title, book.Price }
            """)));

    [Fact]
    public void IsQueryKeyword_Not_ForEach_In()
        => Assert.False(IsQueryKeyword(WrapInMethod("""
            foreach (var i $$in new int[0])
            {
            }
            """)));
}
