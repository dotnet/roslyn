' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Public Class FullNameTests : Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub RootComment()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            Dim root = FormatResult("a ' Comment", value)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult(" a ' Comment", value)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("a' Comment", value)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("a  +c '' Comment", value)
            Assert.Equal("(a  +c).F", GetChildren(root).Single().FullName)

            root = FormatResult("a + c' Comment", value)
            Assert.Equal("(a + c).F", GetChildren(root).Single().FullName)

            ' The result provider should never see a value like this in the "real-world"
            root = FormatResult("''a' Comment", value)
            Assert.Equal(".F", GetChildren(root).Single().FullName)
        End Sub

        <Fact>
        Public Sub RootFormatSpecifiers()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            Dim root = FormatResult("a, raw", value) ' simple
            Assert.Equal("a, raw", root.FullName)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("a, raw, ac, h", value) ' multiple specifiers
            Assert.Equal("a, raw, ac, h", root.FullName)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("M(a, b), raw", value) ' non - specifier comma
            Assert.Equal("M(a, b), raw", root.FullName)
            Assert.Equal("(M(a, b)).F", GetChildren(root).Single().FullName) ' parens not required

            root = FormatResult("a, raw1", value) ' alpha - numeric
            Assert.Equal("a, raw1", root.FullName)
            Assert.Equal("a.F", GetChildren(root).Single().FullName)

            root = FormatResult("a, $raw", value) ' other punctuation
            Assert.Equal("a, $raw", root.FullName)
            Assert.Equal("(a, $raw).F", GetChildren(root).Single().FullName) ' Not ideal
        End Sub

        <Fact>
        Public Sub RootParentheses()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            Dim root = FormatResult("a + b", value)
            Assert.Equal("(a + b).F", GetChildren(root).Single().FullName) ' required

            root = FormatResult("new C()", value)
            Assert.Equal("(new C()).F", GetChildren(root).Single().FullName) ' documentation

            root = FormatResult("A.B", value)
            Assert.Equal("A.B.F", GetChildren(root).Single().FullName) ' desirable

            root = FormatResult("Global.A.B", value)
            Assert.Equal("Global.A.B.F", GetChildren(root).Single().FullName) ' desirable
        End Sub

        <Fact>
        Public Sub RootTrailingSemicolons()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            ' The result provider should never see a values like these in the "real-world"

            Dim root = FormatResult("a;", value)
            Assert.Equal("(a;).F", GetChildren(root).Single().FullName)

            root = FormatResult("a + b;", value)
            Assert.Equal("(a + b;).F", GetChildren(root).Single().FullName)

            root = FormatResult(" M( )  ; ", value)
            Assert.Equal("(M( )  ;).F", GetChildren(root).Single().FullName)
        End Sub

        <Fact>
        Public Sub RootMixedExtras()
            Dim source = "
Class C
    Public F As Integer
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(type.Instantiate())

            ' Comment, then format specifier.
            Dim root = FormatResult("a', ac", value)
            Assert.Equal("a", root.FullName)

            ' Format specifier, then comment.
            root = FormatResult("a, ac , raw ', h", value)
            Assert.Equal("a, ac, raw", root.FullName)
        End Sub

        <Fact, WorkItem(1022165)>
        Public Sub Keywords_Root()
            Dim source = "
Class C
    Sub M()
        Dim [Namespace] As Integer = 3
    End Sub
End Class
"
            Dim assembly = GetAssembly(source)
            Dim value = CreateDkmClrValue(3)

            Dim root = FormatResult("[Namespace]", value)
            Verify(root,
                EvalResult("[Namespace]", "3", "Integer", "[Namespace]"))

            value = CreateDkmClrValue(assembly.GetType("C").Instantiate())
            root = FormatResult("Me", value)
            Verify(root,
                EvalResult("Me", "{C}", "C", "Me"))

            ' Verify that keywords aren't escaped by the ResultProvider at the
            ' root level (we would never expect to see "Namespace" passed as a
            ' resultName, but this check verifies that we leave them "as is").
            root = FormatResult("Namespace", CreateDkmClrValue(New Object()))
            Verify(root,
                EvalResult("Namespace", "{Object}", "Object", "Namespace"))
        End Sub

    End Class

End Namespace
