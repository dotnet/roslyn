' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SyntaxEquivalenceTests

        Private Function NewLines(p1 As String) As String
            Return p1.Replace("\n", vbCrLf)
        End Function

        Private Sub VerifyEquivalent(tree1 As SyntaxTree, tree2 As SyntaxTree, topLevel As Boolean)
            Assert.True(SyntaxFactory.AreEquivalent(tree1, tree2, topLevel))
            ' now try as if the second tree were created from scratch.
            Dim tree3 = VisualBasicSyntaxTree.ParseText(tree2.GetText().ToString())
            Assert.True(SyntaxFactory.AreEquivalent(tree1, tree3, topLevel))
        End Sub

        Private Sub VerifyNotEquivalent(tree1 As SyntaxTree, tree2 As SyntaxTree, topLevel As Boolean)
            Assert.False(SyntaxFactory.AreEquivalent(tree1, tree2, topLevel))
            ' now try as if the second tree were created from scratch.
            Dim tree3 = VisualBasicSyntaxTree.ParseText(tree2.GetText().ToString())
            Assert.False(SyntaxFactory.AreEquivalent(tree1, tree3, topLevel))
        End Sub

        <Fact>
        Public Sub TestEmptyTrees()
            Dim text = ""
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text)
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestAddingComment()
            Dim text = ""
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = tree1.WithInsertAt(0, "' foo")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestAddingActivePPDirective()
            Dim text = ""
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = tree1.WithInsertAt(0, NewLines("#if true \n\n#end if"))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestAddingInactivePPDirective()
            Dim text = ""
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = tree1.WithInsertAt(0, NewLines("#if false \n\n#end if"))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingEmpty()
            Dim text = ""
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = tree1.WithInsertAt(0, NewLines("namespace N \n end namespace"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestAddingClass()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n end namespace"))
            Dim tree2 = tree1.WithInsertBefore("end", NewLines("class C \n end class \n"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestRenameOuter()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("N", "N1")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestRenameInner()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n sub Foo() \n dim z = 0 \n end sub \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("z", "y")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestRenameOuterToSamename()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("N", "N")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestRenameInnerToSameName()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n sub Foo() \n dim z = 0 \n end sub \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("z", "z")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestAddingMethod()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n end class \n end namespace"))
            Dim tree2 = tree1.WithInsertBefore("end", NewLines("sub Foo() \n end sub \n"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestAddingLocal()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n sub Foo() \n end sub \n end class \n end namespace"))
            Dim tree2 = tree1.WithInsertBefore("end", NewLines("dim i as Integer \n "))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestRemovingLocal()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n sub Foo() \n dim z = 0 \n end sub \n end class \n end namespace"))
            Dim tree2 = tree1.WithRemoveFirst("dim z = 0")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingConstLocal()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n sub Foo() \n const i = 5 \n end sub \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("5", "6")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingEnumMember()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("enum E \n i = 5 \n end enum"))
            Dim tree2 = tree1.WithReplaceFirst("5", "6")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingAttribute()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n <Obsolete(true)>\nclass C \n const i = 5 \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("true", "false")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingMethodCall()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n sub Foo() \n Console.Write(0) \n end sub \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("Write", "WriteLine")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingUsing()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("Imports System \n namespace N \n class C \n sub Foo() \n Console.Write(0) \n end sub \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("System", "System.Linq")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingBaseType()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n Console.Write(0) \n end sub \n end class"))
            Dim tree2 = tree1.WithInsertBefore("sub", "Inherits B \n")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingMethodType()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n Console.Write(0) \n end sub \n end class"))
            Dim tree2 = tree1.WithReplaceFirst("sub Foo()", "function Foo() as Integer")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestAddComment()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n Console.Write(0) \n end sub \n end class"))
            Dim tree2 = tree1.WithInsertBefore("class", NewLines("' Comment\n"))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestCommentOutCode()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n Console.Write(0) \n end sub \n end class"))
            Dim tree2 = tree1.WithInsertBefore("class", "' ")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestAddDocComment()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n Console.Write(0) \n end sub \n end class"))
            Dim tree2 = tree1.WithInsertBefore("class", NewLines("''' Comment \n"))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestSurroundMethodWithActivePPRegion()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n end sub \n end class"))
            Dim tree2 = tree1.WithReplaceFirst(NewLines("sub Foo() \n end sub \n"), NewLines("\n #if true \n sub Foo() \n end sub \n #end if \n"))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestSurroundMethodWithInactivePPRegion()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n end sub \n end class"))
            Dim tree2 = tree1.WithReplaceFirst(NewLines("sub Foo() \n end sub \n"), NewLines("\n #if false \n sub Foo() \n end sub \n #end if \n"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestSurroundStatementWithActivePPRegion()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n dim i as Integer \n end sub \n end class"))
            Dim tree2 = tree1.WithReplaceFirst(NewLines("dim i as Integer \n"), NewLines("\n #if true \n dim i as Integer \n #end if \n"))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestSurroundStatementWithInactivePPRegion()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n dim i as Integer \n end sub \n end class"))
            Dim tree2 = tree1.WithReplaceFirst(NewLines("dim i as Integer \n"), NewLines("\n #if false \n dim i as Integer \n #end if \n"))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangeWhitespace()
            Dim text = NewLines("class C \n sub Foo() \n dim i as Integer \n end sub \n end class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace(" ", "  "))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestSkippedText()
            Dim text = NewLines("Imports System \n Modle Program \n Sub Main(args As String()) \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Modle ", "Mode "))
            VerifyEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestUpdateInterpolatedString()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n sub Foo() \n Console.Write($""Hello{123:N1}"") \n end sub \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("N1", "N2")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)

            tree2 = tree1.WithReplaceFirst("Hello", "World")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

#Region "Field"

        <Fact>
        Public Sub TestRemovingField1()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n dim i = 5 \n dim j = 6 \n end class \n end namespace"))
            Dim tree2 = tree1.WithRemoveFirst("dim i = 5")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestRemovingField2()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n dim i = 5 \n dim j = 6 \n end class \n end namespace"))
            Dim tree2 = tree1.WithRemoveFirst("dim j = 6")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingFieldInitializer()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n dim i = 5 \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("5", "6")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingFieldAsNewInitializer1()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n dim i As New C(5) \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("5", "6")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingFieldAsNewInitializer2()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n dim i, j As New C(5) \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("5", "6")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingField2()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n dim i = 5, j = 7 \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("7", "8")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            tree2 = tree1.WithReplaceFirst("5", "6")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingConstField()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n const i = 5 \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("5", "6")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestChangingConstField2()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("namespace N \n class C \n const i = 5, j = 7 \n end class \n end namespace"))
            Dim tree2 = tree1.WithReplaceFirst("5", "6")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            tree2 = tree1.WithReplaceFirst("7", "8")
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

#End Region

#Region "Methods"

        <Fact>
        Public Sub TestMethod_Body()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Body(1) \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Body(1)", "Body(2)"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_Modifiers()
            Dim text = NewLines("Imports System \n Module Program \n Friend Sub Main(args As String()) \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Friend", ""))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_ParameterName()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("args", "arg"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_ParameterAttribute()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("args", "<A>args"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_ParameterModifier()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("args", "ByRef args"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_ParameterDefaultValue()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(Optional arg As Integer = 123) \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "456"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_Kind()
            Dim text = NewLines("Imports System \n Module Program \n Function Main(Optional arg As Integer = 123) \n \n End Function \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Function", "Sub"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_ReturnType1()
            Dim text = NewLines("Imports System \n Module Program \n Function Main(Optional arg As Integer = 123) As Integer \n \n End Function \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("As Integer", ""))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_ReturnType2()
            Dim text = NewLines("Imports System \n Module Program \n Function Main(Optional arg As Integer = 123) As C \n \n End Function \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("As C", "As D"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_ReturnTypeCustomAttribute()
            Dim text = NewLines("Imports System \n Module Program \n Function Main(Optional arg As Integer = 123) As <A(1)>C \n \n End Function \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("As <A(1)>C", "As <A(2)>C"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_Handles()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(args As String()) Handles E.Foo \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("E.Foo", "E.Bar"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_Implements()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(args As String()) Implements I.Foo \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("I.Foo", "I.Bar"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_RemoveEnd()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(args As String()) Implements I.Foo \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("End Sub", ""))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_ChangeEndKind()
            Dim text = NewLines("Imports System \n Module Program \n Sub Main(args As String()) Implements I.Foo \n \n End Sub \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("End Sub", "End Function"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestMethod_CommentOutMethodCode()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n Console.Write(0) \n end sub \n end class"))
            Dim tree2 = tree1.WithReplaceFirst("Console.Write(0)", "' Console.Write(0) ")
            VerifyEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub

        <Fact>
        Public Sub TestMethod_CommentOutMethod()
            Dim tree1 = VisualBasicSyntaxTree.ParseText(NewLines("class C \n sub Foo() \n end sub \n end class"))
            Dim tree2 = tree1.WithReplaceFirst(NewLines("sub Foo() \n end sub \n"), NewLines("' sub Foo() \n ' end sub \n"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
        End Sub
#End Region

#Region "Constructor"
        <Fact>
        Public Sub TestConstructor_Body()
            Dim text = NewLines("Imports System \n Class Program \n Sub New(args As String()) \n Body(1) \n End Sub \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Body(1)", "Body(2)"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestConstructor_Initializer()
            Dim text = NewLines("Imports System \n Class Program \n Sub New(args As String()) \n MyBase.New(1) \n End Sub \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("MyBase", "Me"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestConstructor_ParameterDefaultValue()
            Dim text = NewLines("Imports System \n Class Program \n Sub New(Optional arg As Integer = 123) \n \n End Sub \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "456"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub
#End Region

#Region "Operator"
        <Fact>
        Public Sub TestOperator_Body()
            Dim text = NewLines("Imports System \n Class C \n Shared Operator *(a As C, b As C) As Integer \n Return 0 \n End Operator \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Return 0", "Return 1"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestOperator_ParameterName()
            Dim text = NewLines("Imports System \n Class C \n Shared Operator *(a As C, b As C) As Integer \n Return 0 \n End Operator \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("b As C", "c As C"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub
#End Region

#Region "Property"
        <Fact>
        Public Sub TestPropertyAccessor_Attribute1()
            Dim text = NewLines("Imports System \n Class Program \n Property P As Integer \n <A(1)>Get \n End Get \n Set(value As Integer) \n End Set \n End Property \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestPropertyAccessor_Attribute2()
            Dim text = NewLines("Imports System \n Class Program \n Property P As Integer \n Get \n End Get \n <A(1)>Set(value As Integer) \n End Set \n End Property \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestPropertyAccessor_Attribute3()
            Dim text = NewLines("Imports System \n Class Program \n Property P As Integer \n Get \n End Get \n Set(<A(1)>value As Integer) \n End Set \n End Property \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestProperty_Parameters()
            Dim text = NewLines("Imports System \n Class Program \n Property P(a As Integer = 123) \n Get \n End Get \n Set(value As Integer) \n End Set \n End Property \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "345"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestAutoProperty_Initializer1()
            Dim text = NewLines("Imports System \n Class Program \n Property P As Integer = 123 \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "345"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestAutoProperty_Initializer_InvalidSyntax()
            Dim text = NewLines("Imports System \n Class Program \n Property P(a As Integer = 123) As Integer = 1 \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "345"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub
#End Region

#Region "Event"
        <Fact>
        Public Sub TestEventAccessor_Attribute1()
            Dim text = NewLines("Imports System \n Class Program \n Custom Event E As Action \n <A(1)>AddHandler(value As Action) \n End AddHandler \n RemoveHandler(value As Action) \n End RemoveHandler \n RaiseEvent() \n End RaiseEvent \n End Event \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestEventAccessor_Attribute2()
            Dim text = NewLines("Imports System \n Class Program \n Custom Event E As Action \n AddHandler(value As Action) \n End AddHandler \n <A(1)>RemoveHandler(value As Action) \n End RemoveHandler \n RaiseEvent() \n End RaiseEvent \n End Event \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestEventAccessor_Attribute3()
            Dim text = NewLines("Imports System \n Class Program \n Custom Event E As Action \n AddHandler(value As Action) \n End AddHandler \n RemoveHandler(value As Action) \n End RemoveHandler \n <A(1)>RaiseEvent() \n End RaiseEvent \n End Event \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestEventAccessor_Attribute4()
            Dim text = NewLines("Imports System \n Class Program \n Custom Event E As Action \n AddHandler(<A(1)>value As Action) \n End AddHandler \n RemoveHandler(value As Action) \n End RemoveHandler \n RaiseEvent() \n End RaiseEvent \n End Event \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestEventAccessor_Attribute5()
            Dim text = NewLines("Imports System \n Class Program \n Custom Event E As Action \n AddHandler(value As Action) \n End AddHandler \n RemoveHandler(<A(1)>value As Action) \n End RemoveHandler \n RaiseEvent() \n End RaiseEvent \n End Event \n End Class")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub
#End Region

#Region "Declare"
        <Fact>
        Public Sub TestDeclare_Modifier()
            Dim text = NewLines("Imports System \n Module Program \n Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Ansi", "Unicode"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestDeclare_LibName()
            Dim text = NewLines("Imports System \n Module Program \n Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("foo", "foo2"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestDeclare_AliasName()
            Dim text = NewLines("Imports System \n Module Program \n Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("bar", "bar2"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestDeclare_ReturnType()
            Dim text = NewLines("Imports System \n Module Program \n Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Integer", "Boolean"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub

        <Fact>
        Public Sub TestDeclare_Parameter()
            Dim text = NewLines("Imports System \n Module Program \n Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer \n End Module")
            Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("()", "(a As Integer)"))
            VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            VerifyNotEquivalent(tree1, tree2, topLevel:=True)
        End Sub
#End Region
    End Class
End Namespace
