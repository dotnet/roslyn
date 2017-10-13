' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Partial Public Class ParseParametersTest
    Inherits BasicTestBase

    <Fact,
Trait("CompilerFeature", "DefaultOptionalParameter"),
Trait("DefaultOptionalParameter", "Required")>
    Public Sub ParmeterIsClassType_Required()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(ByVal arg As String )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub

    <Fact,
Trait("CompilerFeature", "DefaultOptionalParameter"),
Trait("DefaultOptionalParameter", "Explicit")>
    Public Sub ParmeterIsClassType_EqualsLiteralNothing()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(Optional ByVal arg As String = Nothing )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub

    <Fact,
Trait("CompilerFeature", "DefaultOptionalParameter"),
Trait("DefaultOptionalParameter", "Explicit")>
    Public Sub ParmeterIsClassType_EqualsStringLiteral()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(Optional ByVal arg As String = "" )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub
    <Fact,
Trait("CompilerFeature", "DefaultOptionalParameter"),
Trait("DefaultOptionalParameter", "Explicit")>
    Public Sub ParmeterIsClassType_EqualsConstant()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(Optional ByVal arg As String = String.Empty )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub
    <Fact,
Trait("CompilerFeature", "DefaultOptionalParameter"),
Trait("DefaultOptionalParameter", "Implicit")>
    Public Sub ParmeterIsClassType_Implicit()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(Optional ByVal arg As String )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub

End Class
