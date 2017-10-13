' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Partial Public Class ParseParametersTest
    Inherits BasicTestBase

    <Fact,
      Trait("Feature", "DefaultOptionalParameter")>
    Public Sub Mixed_Imp_Imp()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Method1( Optional opt0 As Integer, Optional opt1 As Integer )
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact,
 Trait("Feature", "DefaultOptionalParameter")>
    Public Sub Mixed_Imp_Exp()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Method1( Optional opt0 As Integer, Optional opt1 As Integer = Nothing )
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact,
 Trait("Feature", "DefaultOptionalParameter")>
    Public Sub Mixed_Exp_Imp()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Method1( Optional opt0 As Integer = Nothing, Optional opt1 As Integer )
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact,
 Trait("Feature", "DefaultOptionalParameter")>
    Public Sub Mixed_Exp_Exp()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Method1( Optional opt0 As Integer = Nothing, Optional opt1 As Integer = Nothing )
                End Sub
            End Module
        ]]>)
    End Sub
End Class
