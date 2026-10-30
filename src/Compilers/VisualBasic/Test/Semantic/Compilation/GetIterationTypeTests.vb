' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GetIterationTypeTests
        Inherits SemanticModelTestBase

        Private Const Source As String = "
Imports System.Collections
Imports System.Collections.Generic

Class C
    Dim Arr As Integer()
    Dim List As List(Of String)
    Dim Str As String
    Dim NonGeneric As IEnumerable
    Dim Pattern As Pattern
    Dim NotIterable As Integer

    Sub M()
        'pos
    End Sub
End Class

Class Pattern
    Public Function GetEnumerator() As PatternEnumerator
        Return Nothing
    End Function
End Class

Class PatternEnumerator
    Public ReadOnly Property Current As Long
    Public Function MoveNext() As Boolean
        Return False
    End Function
End Class
"

        <Theory>
        <InlineData("Arr", "System.Int32")>
        <InlineData("List", "System.String")>
        <InlineData("Str", "System.Char")>
        <InlineData("NonGeneric", "System.Object")>
        <InlineData("Pattern", "System.Int64")>
        <InlineData("NotIterable", Nothing)>
        Public Sub GetIterationType(member As String, expected As String)
            Dim tree = Parse(Source)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim type = DirectCast(comp.GetTypeByMetadataName("C").GetMembers(member).Single(), IFieldSymbol).Type

            Dim iterationType = model.GetIterationType(FindPositionFromText(tree, "'pos"), type)

            Assert.Equal(expected, iterationType?.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub GetIterationType_NullType()
            Dim tree = Parse(Source)
            Dim model = CreateCompilation(tree).GetSemanticModel(tree)

            Assert.Throws(Of ArgumentNullException)(Function() model.GetIterationType(0, Nothing))
        End Sub
    End Class
End Namespace
