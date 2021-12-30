' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class LambdaUtilitiesTests
        <Fact>
        Public Sub AreEquivalentIgnoringLambdaBodies1()

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(1)"),
                SyntaxFactory.ParseExpression("F(1)")))

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(1)"),
                SyntaxFactory.ParseExpression("F(2)")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(Function(a) 1)"),
                SyntaxFactory.ParseExpression("F(Function(a) 2)")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(Sub(a) Console.WriteLine(1))"),
                SyntaxFactory.ParseExpression("F(Sub(a) Console.WriteLine(2))")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(Function(a) : Return 1 : End Function)"),
                SyntaxFactory.ParseExpression("F(Function(a) : Return 2 : End Function)")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(Sub(a) : Console.WriteLine(1)) : End Sub)"),
                SyntaxFactory.ParseExpression("F(Sub(a) : Console.WriteLine(2)) : End Sub)")))

            ' RECONSIDER: lambda header is currently considered to be part of the body
            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(Sub(a) : Console.WriteLine(1)) : End Sub)"),
                SyntaxFactory.ParseExpression("F(Sub(b) : Console.WriteLine(1)) : End Sub)")))

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From x In {1,2} Select 1)"),
                SyntaxFactory.ParseExpression("F(From x In {1,2,3} Select 1)")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From x In {1,2} Let a = 1, b = 2 Select a)"),
                SyntaxFactory.ParseExpression("F(From x In {1,2} Let a = 4, b = 3 Select b)")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From x In {1,2}, y in {3,4} Where x > 0 Select 1)"),
                SyntaxFactory.ParseExpression("F(From x In {1,2}, y in {3,4,5} Where x < 0 Select 2)")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From x In {1,2} Join y In {3,4} On F(1) Equals G(1) And F(2) Equals G(2) Select 1)"),
                SyntaxFactory.ParseExpression("F(From x In {1,2} Join y In {3,4} On F(2) Equals G(2) And F(3) Equals G(3) Select 1)")))

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From x In {1,2} Join y In {3,4} On F(1) Equals G(1) Select 1)"),
                SyntaxFactory.ParseExpression("F(From x In {1,2} Join y In {3,4,5} On F(1) Equals G(1) Select 1)")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From x In {1,2} Order By x.f1, x.g1 Descending, x.h1 Ascending Select 1)"),
                SyntaxFactory.ParseExpression("F(From x In {1,2} Order By x.f2, x.g2 Descending, x.h2 Ascending Select 1)")))

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From x In {1,2} Order By x.f, x.g Descending, x.h Ascending Select 1)"),
                SyntaxFactory.ParseExpression("F(From x In {1,2} Order By x.f, x.g Descending, x.h Descending Select 1)")))

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From a In {1} Skip F(1) Select a"),
                SyntaxFactory.ParseExpression("F(From a In {1} Skip F(2) Select a")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From a In {1} Skip While F(1) Select a"),
                SyntaxFactory.ParseExpression("F(From a In {1} Skip While F(2) Select a")))

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From a In {1} Take F(1) Select a"),
                SyntaxFactory.ParseExpression("F(From a In {1} Take F(2) Select a")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(From a In {1} Take While F(1) Select a"),
                SyntaxFactory.ParseExpression("F(From a In {1} Take While F(2) Select a")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("
F(From a In Id({1}, 1)
  Join b In Id({1}, 2)
  Join b2 In Id({1}, 3) On Id(b, 4) Equals Id(b2, 5) And Id(b, 6) Equals Id(b2, 7)
    On Id(b, 8) Equals Id(a, 9) And Id(b, 10) Equals Id(a, 11)
  Group Join c In Id({1}, 12) On Id(c, 13) Equals Id(b, 14) And Id(c, 15) Equals Id(b, 16) Into d1 = Count(Id(1, 17)), e1 = Count(Id(1, 18)))
"),
                SyntaxFactory.ParseExpression("
F(From a In Id({1}, 1)
  Join b In Id({1}, 2)
  Join b2 In Id({1}, 3) On Id(b, 40) Equals Id(b2, 50) And Id(b, 60) Equals Id(b2, 70)
    On Id(b, 80) Equals Id(a, 90) And Id(b, 100) Equals Id(a, 110)
  Group Join c In Id({1}, 12) On Id(c, 130) Equals Id(b, 140) And Id(c, 150) Equals Id(b, 160) Into d1 = Count(Id(1, 170)), e1 = Count(Id(1, 180)))
")))

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("
F(From a In Id({1}, 1)
  Join b2 In Id({1}, 3) On Id(a, 4) Equals Id(b2, 5)
  Group Join c In Id({1}, 12) On Id(c, 13) Equals Id(b, 14) And Id(c, 15) Equals Id(b, 16) Into d1 = Count(Id(1, 17)), e1 = Count(Id(1, 18)))
"),
                SyntaxFactory.ParseExpression("
F(From a In Id({1}, 1)
  Join b2 In Id({1}, 3) On Id(a, 4) Equals Id(b2, 5)
  Group Join c In Id({10000000}, 12) On Id(c, 13) Equals Id(b, 14) And Id(c, 15) Equals Id(b, 16) Into d1 = Count(Id(1, 17)), e1 = Count(Id(1, 18)))
")))

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("
F(From a In {1}
  Aggregate b In {1}, c in {1}
    From d In {1}
    Let h = 1
    Where d > 1
  Into q = Count(b), p = Distinct()
"),
                SyntaxFactory.ParseExpression("
F(From a In {1}
  Aggregate b In {10}, c in {10}
    From d In {10}
    Let h = 10
    Where d > 10
  Into q = Count(b + 1), p = Distinct()
")))
        End Sub
    End Class
End Namespace
