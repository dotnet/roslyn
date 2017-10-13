' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Partial Public Class ParseParametersTest
    Inherits BasicTestBase

    <Fact,
     Trait("Bug", "862436"),
     Trait("Feature", "DefaultOptionalParameter"),
     Trait("DefaultOptionalParameter", "Explicit")>
    Public Sub Bug862436()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Method1(Optional ByVal x As Object = Nothing)
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact,
    Trait("CompilerFeature", "DefaultOptionalParameter"),
    Trait("DefaultOptionalParameter", "Implicit")>
    Public Sub Bug862436_Implicit()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Method1(Optional ByVal x As Object)
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact,
    Trait("Feature", "DefaultOptionalParameter"),
    Trait("DefaultOptionalParameter", "Explicit")>
    Public Sub Bug862505()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f1(Optional ByVal c1 As New Object()
                End Function
            End Class
        ]]>,
        <errors>
            <error id="30180"/>
        </errors>)
    End Sub

End Class
