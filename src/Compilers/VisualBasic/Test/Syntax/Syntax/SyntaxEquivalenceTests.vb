' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Namespace Syntax.Equivalence

        Public Class SyntaxEquivalenceTests

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
                Dim tree2 = tree1.WithInsertAt(0, "#if true 

#end if")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestAddingInactivePPDirective()
                Dim text = ""
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = tree1.WithInsertAt(0, "#if false 

#end if")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingEmpty()
                Dim text = ""
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = tree1.WithInsertAt(0, "namespace N 
end namespace")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestAddingClass()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
end namespace")
                Dim tree2 = tree1.WithInsertBefore("end", "class C 
end class 
")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestRenameOuter()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
end namespace")
                Dim tree2 = tree1.WithReplaceFirst("N", "N1")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestRenameInner()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
class C 
sub Foo() 
dim z = 0 
end sub 
end class 
end namespace")
                Dim tree2 = tree1.WithReplaceFirst("z", "y")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestRenameOuterToSamename()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
end namespace")
                Dim tree2 = tree1.WithReplaceFirst("N", "N")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestRenameInnerToSameName()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
class C 
sub Foo() 
dim z = 0 
end sub 
end class 
end namespace")
                Dim tree2 = tree1.WithReplaceFirst("z", "z")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestAddingMethod()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
class C
end class 
end namespace")
                Dim tree2 = tree1.WithInsertBefore("end", "sub Foo() 
end sub 
")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestAddingLocal()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
class C 
sub Foo() 
end sub 
end class 
end namespace")
                Dim tree2 = tree1.WithInsertBefore("end", "dim i as Integer 
")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestRemovingLocal()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
class C 
sub Foo() 
dim z = 0 
end sub 
end class 
end namespace")
                Dim tree2 = tree1.WithRemoveFirst("dim z = 0")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingConstLocal()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
class C 
sub Foo() 
const i = 5 
end sub 
end class 
end namespace")
                Dim tree2 = tree1.WithReplaceFirst("5", "6")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingEnumMember()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("enum E 
i = 5 
end enum")
                Dim tree2 = tree1.WithReplaceFirst("5", "6")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingAttribute()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
<Obsolete(true)>
class C 
const i = 5 
end class 
end namespace")
                Dim tree2 = tree1.WithReplaceFirst("true", "false")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingMethodCall()
                Dim tree1 = VisualBasicSyntaxTree.ParseText("namespace N 
class C 
sub Foo() 
Console.Write(0) 
end sub 
end class 
end namespace")
                Dim tree2 = tree1.WithReplaceFirst("Write", "WriteLine")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingUsing()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "Imports System 
namespace N 
class C 
sub Foo() 
Console.Write(0) 
end sub 
end class 
end namespace")
                Dim tree2 = tree1.WithReplaceFirst("System", "System.Linq")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingBaseType()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
Console.Write(0) 
end sub 
end class")
                Dim tree2 = tree1.WithInsertBefore("sub", "Inherits B \n")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingMethodType()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
Console.Write(0) 
end sub 
end class")
                Dim tree2 = tree1.WithReplaceFirst("sub Foo()", "function Foo() as Integer")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestAddComment()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
Console.Write(0) 
end sub 
end class")
                Dim tree2 = tree1.WithInsertBefore("class", "' Comment
")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestCommentOutCode()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
Console.Write(0) 
end sub 
end class")
                Dim tree2 = tree1.WithInsertBefore("class", "' ")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestAddDocComment()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
Console.Write(0) 
end sub 
end class")
                Dim tree2 = tree1.WithInsertBefore("class",
    "''' Comment 
")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestSurroundMethodWithActivePPRegion()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo()
end sub 
end class")
                Dim tree2 = tree1.WithReplaceFirst(
    "sub Foo()
end sub 
",
    "
#if true 
sub Foo() 
end sub 
#end if 
")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestSurroundMethodWithInactivePPRegion()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
end sub 
end class")
                Dim tree2 = tree1.WithReplaceFirst(
    "sub Foo() 
end sub 
",
    "
#if false 
sub Foo() 
end sub 
#end if ")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestSurroundStatementWithActivePPRegion()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
dim i as Integer 
end sub 
end class")
                Dim tree2 = tree1.WithReplaceFirst(
    "dim i as Integer 
", "
#if true 
dim i as Integer 
#end if 
")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestSurroundStatementWithInactivePPRegion()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
dim i as Integer 
end sub 
end class")
                Dim tree2 = tree1.WithReplaceFirst(
    "dim i as Integer 
",
    "
#if false 
dim i as Integer 
#end if 
")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangeWhitespace()
                Dim text =
    "class C 
sub Foo() 
dim i as Integer 
end sub 
end class"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace(" ", "  "))
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestSkippedText()
                Dim text =
    "Imports System 
Modle Program 
Sub Main(args As String()) 

End Sub 
End Module"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Modle ", "Mode "))
                VerifyEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestUpdateInterpolatedString()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
sub Foo() 
Console.Write($""Hello{123:N1}"") 
end sub 
end class 
end namespace
")
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
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
dim i = 5 
dim j = 6 
end class 
end namespace
")
                Dim tree2 = tree1.WithRemoveFirst("dim i = 5")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestRemovingField2()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
dim i = 5 
dim j = 6 
end class 
end namespace
")
                Dim tree2 = tree1.WithRemoveFirst("dim j = 6")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingFieldInitializer()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
dim i = 5 
end class 
end namespace
")
                Dim tree2 = tree1.WithReplaceFirst("5", "6")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingFieldAsNewInitializer1()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
dim i As New C(5) 
end class 
end namespace
")
                Dim tree2 = tree1.WithReplaceFirst("5", "6")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingFieldAsNewInitializer2()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
dim i, j As New C(5) 
end class 
end namespace
")
                Dim tree2 = tree1.WithReplaceFirst("5", "6")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingField2()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
dim i = 5, j = 7 
end class 
end namespace
")
                Dim tree2 = tree1.WithReplaceFirst("7", "8")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                tree2 = tree1.WithReplaceFirst("5", "6")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingConstField()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
const i = 5 
end class 
end namespace
")
                Dim tree2 = tree1.WithReplaceFirst("5", "6")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestChangingConstField2()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "namespace N 
class C 
const i = 5, j = 7 
end class 
end namespace
")
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
                Dim text =
    "Imports System 
Module Program 
Sub Main(args As String()) 
Body(1) 
End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Body(1)", "Body(2)"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_Modifiers()
                Dim text =
    "Imports System 
Module Program 
Friend Sub Main(args As String()) 

End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Friend", ""))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_ParameterName()
                Dim text =
    "Imports System 
Module Program 
Sub Main(args As String()) 

End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("args", "arg"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_ParameterAttribute()
                Dim text =
    "Imports System 
 Module Program 
Sub Main(args As String()) 

 End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("args", "<A>args"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_ParameterModifier()
                Dim text =
    "Imports System 
Module Program 
Sub Main(args As String()) 

End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("args", "ByRef args"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_ParameterDefaultValue()
                Dim text =
    "Imports System 
Module Program 
Sub Main(Optional arg As Integer = 123) 

 End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "456"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_Kind()
                Dim text =
    "Imports System 
Module Program 
Function Main(Optional arg As Integer = 123) 

End Function 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Function", "Sub"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_ReturnType1()
                Dim text =
    "Imports System 
Module Program 
 Function Main(Optional arg As Integer = 123) As Integer 

 End Function 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("As Integer", ""))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_ReturnType2()
                Dim text =
    "Imports System 
 Module Program 
 Function Main(Optional arg As Integer = 123) As C 
 
 End Function 
 End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("As C", "As D"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_ReturnTypeCustomAttribute()
                Dim text =
    "Imports System 
Module Program 
Function Main(Optional arg As Integer = 123) As <A(1)>C 

End Function 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("As <A(1)>C", "As <A(2)>C"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_Handles()
                Dim text =
    "Imports System 
Module Program 
Sub Main(args As String()) Handles E.Foo 
 
End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("E.Foo", "E.Bar"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_Implements()
                Dim text =
    "Imports System 
Module Program 
Sub Main(args As String()) Implements I.Foo 

End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("I.Foo", "I.Bar"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_RemoveEnd()
                Dim text =
    "Imports System 
Module Program 
Sub Main(args As String()) Implements I.Foo 

End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("End Sub", ""))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_ChangeEndKind()
                Dim text =
    "Imports System 
Module Program 
Sub Main(args As String()) Implements I.Foo 

End Sub 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("End Sub", "End Function"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestMethod_CommentOutMethodCode()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
Console.Write(0) 
end sub 
end class
")
                Dim tree2 = tree1.WithReplaceFirst("Console.Write(0)", "' Console.Write(0) ")
                VerifyEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub

            <Fact>
            Public Sub TestMethod_CommentOutMethod()
                Dim tree1 = VisualBasicSyntaxTree.ParseText(
    "class C 
sub Foo() 
end sub 
end class
")
                Dim tree2 = tree1.WithReplaceFirst(
    "sub Foo() 
end sub", "' sub Foo() 
' end sub")
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
            End Sub
#End Region

#Region "Constructor"
            <Fact>
            Public Sub TestConstructor_Body()
                Dim text =
    "Imports System 
Class Program 
Sub New(args As String()) 
Body(1) 
 End Sub 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Body(1)", "Body(2)"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestConstructor_Initializer()
                Dim text =
    "Imports System 
Class Program 
Sub New(args As String()) 
MyBase.New(1) 
End Sub 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("MyBase", "Me"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestConstructor_ParameterDefaultValue()
                Dim text =
    "Imports System 
Class Program 
Sub New(Optional arg As Integer = 123) 

End Sub 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "456"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub
#End Region

#Region "Operator"
            <Fact>
            Public Sub TestOperator_Body()
                Dim text =
    "Imports System 
Class C 
Shared Operator *(a As C, b As C) As Integer 
Return 0 
End Operator 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Return 0", "Return 1"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestOperator_ParameterName()
                Dim text =
    "Imports System 
Class C 
Shared Operator *(a As C, b As C) As Integer 
Return 0 
End Operator 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("b As C", "c As C"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub
#End Region

#Region "Property"
            <Fact>
            Public Sub TestPropertyAccessor_Attribute1()
                Dim text =
    "Imports System 
Class Program 
Property P As Integer 
<A(1)>Get 
End Get 
Set(value As Integer) 
End Set 
End Property 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestPropertyAccessor_Attribute2()
                Dim text =
    "Imports System 
Class Program 
Property P As Integer 
Get 
End Get 
<A(1)>Set(value As Integer) 
End Set 
End Property 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestPropertyAccessor_Attribute3()
                Dim text =
    "Imports System 
Class Program 
Property P As Integer 
Get 
End Get 
Set(<A(1)>value As Integer) 
End Set 
End Property 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestProperty_Parameters()
                Dim text =
    "Imports System 
Class Program 
Property P(a As Integer = 123) 
Get 
End Get 
Set(value As Integer) 
End Set 
End Property 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "345"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestAutoProperty_Initializer1()
                Dim text =
    "Imports System 
Class Program 
Property P As Integer = 123 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "345"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestAutoProperty_Initializer_InvalidSyntax()
                Dim text =
    "Imports System 
Class Program 
Property P(a As Integer = 123) As Integer = 1 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("123", "345"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub
#End Region

#Region "Event"
            <Fact>
            Public Sub TestEventAccessor_Attribute1()
                Dim text =
    "Imports System 
Class Program 
Custom Event E As Action 
<A(1)>AddHandler(value As Action) 
End AddHandler 
RemoveHandler(value As Action) 
End RemoveHandler 
RaiseEvent() 
End RaiseEvent 
End Event 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestEventAccessor_Attribute2()
                Dim text =
    "Imports System 
Class Program 
Custom Event E As Action 
AddHandler(value As Action) 
End AddHandler 
<A(1)>RemoveHandler(value As Action) 
End RemoveHandler 
RaiseEvent() 
End RaiseEvent 
End Event 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestEventAccessor_Attribute3()
                Dim text =
    "Imports System 
Class Program 
Custom Event E As Action 
AddHandler(value As Action) 
End AddHandler 
RemoveHandler(value As Action) 
End RemoveHandler 
<A(1)>RaiseEvent() 
End RaiseEvent 
End Event 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestEventAccessor_Attribute4()
                Dim text =
    "Imports System 
Class Program 
Custom Event E As Action 
AddHandler(<A(1)>value As Action) 
End AddHandler 
RemoveHandler(value As Action) 
End RemoveHandler 
RaiseEvent() 
End RaiseEvent 
End Event 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestEventAccessor_Attribute5()
                Dim text =
    "Imports System 
Class Program 
Custom Event E As Action 
AddHandler(value As Action) 
End AddHandler 
RemoveHandler(<A(1)>value As Action) 
End RemoveHandler 
RaiseEvent() 
End RaiseEvent 
End Event 
End Class
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("<A(1)>", "<A(2)>"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub
#End Region

#Region "Declare"
            <Fact>
            Public Sub TestDeclare_Modifier()
                Dim text =
    "Imports System 
Module Program 
Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Ansi", "Unicode"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestDeclare_LibName()
                Dim text =
    "Imports System 
Module Program 
Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("foo", "foo2"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestDeclare_AliasName()
                Dim text =
    "Imports System 
Module Program 
Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("bar", "bar2"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestDeclare_ReturnType()
                Dim text =
    "Imports System 
Module Program 
Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("Integer", "Boolean"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub

            <Fact>
            Public Sub TestDeclare_Parameter()
                Dim text =
    "Imports System 
Module Program 
Declare Ansi Function Foo Lib ""foo"" Alias ""bar"" () As Integer 
End Module
"
                Dim tree1 = VisualBasicSyntaxTree.ParseText(text)
                Dim tree2 = VisualBasicSyntaxTree.ParseText(text.Replace("()", "(a As Integer)"))
                VerifyNotEquivalent(tree1, tree2, topLevel:=False)
                VerifyNotEquivalent(tree1, tree2, topLevel:=True)
            End Sub
#End Region
        End Class
    End Namespace
End Namespace