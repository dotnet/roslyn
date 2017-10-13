' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Partial Public Class ParseParametersTest
    Inherits BasicTestBase

    <Fact,
Trait("Feature", "DefaultOptionalParameter")>
    Public Sub ParmeterIsNullableType_Required()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(ByVal arg As Integer? )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub

    <Fact,
Trait("Feature", "DefaultOptionalParameter")>
    Public Sub ParmeterIsNullableType_EqualsLiteralNothing()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(Optional ByVal arg As Integer? = Nothing )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub

    <Fact,
Trait("Feature", "DefaultOptionalParameter")>
    Public Sub ParmeterIsNullableType_EqualsConstant()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(Optional ByVal arg As Integer? = 0 )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub

    <Fact,
Trait("Feature", "DefaultOptionalParameter")>
    Public Sub ParmeterIsNullableType_Implicit()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f0(Optional ByVal arg As Integer? )
                End Function
            End Class
        ]]>,
        expectedErrors:=Nothing
        )
    End Sub

End Class
