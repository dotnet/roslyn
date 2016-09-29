' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports System
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' Kinds of Syntax transforms.
''' </summary>
''' <remarks></remarks>
Public Enum TransformKind
    IntTypeToLongType
    TrueToFalse
    FalseToTrue
    ClassToStructure
    StructureToClass
    OrderByAscToOrderByDesc
    OrderByDescToOrderByAsc
    AddAssignmentToAssignment
    DirectCastToTryCast
    TryCastToDirectCast
    InitVariablesToNothing
    ByValParamToByRefParam
    ByRefParamToByValParam
    DoTopTestToDoBottomTest
    DoBottomTestToDoTopTest
    WhileToDoWhileTopTest
    DoWhileTopTestToWhile
    SingleLineIfToMultiLineIf
End Enum

Public Class Transforms
    ''' <summary>
    ''' Performs a syntax transform of the source code which is passed in as a string. The transform to be performed is also passed as an argument
    ''' </summary>
    ''' <param name="sourceText">Text of the source code which is to be transformed</param>
    ''' <param name="transformKind">The kind of Syntax Transform that needs to be performed on the source</param>
    ''' <returns>Transformed source code as a string</returns>
    ''' <remarks></remarks>
    Public Shared Function Transform(sourceText As String, transformKind As TransformKind) As String
        Dim sourceTree = SyntaxFactory.ParseSyntaxTree(sourceText)
        Dim visitor As New TransformVisitor(sourceTree, transformKind)

        Return visitor.Visit(sourceTree.GetRoot()).ToFullString()
    End Function

End Class
