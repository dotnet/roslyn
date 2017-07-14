' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class PrivateProtected
        Inherits BasicTestBase

        <Fact>
        Public Sub RejectIncimpatibleModifiers()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Public Class Base
    Private Friend Field1 As Integer
    Friend Private Field2 As Integer
    Private Friend Protected Field3 As Integer
    Friend Protected Private Field4 As Integer
End Class
]]>
                        </file>
                    </compilation>,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Friend Field1 As Integer
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Friend Private Field2 As Integer
           ~~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Friend Protected Field3 As Integer
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Friend Protected Private Field4 As Integer
                     ~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AccessibleWhereRequired_01()
            Dim sources = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
Public Class Base
    Private Protected Field1 As Integer
    Protected Private Field2 As Integer
End Class

Public Class Derived
        Inherits Base
    Sub M()
        Field1 = 1
        Field2 = 2
    End Sub
End Class
]]>
                              </file>
                          </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                    sources,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36716: Visual Basic 15.3 does not support Private Protected.
    Private Protected Field1 As Integer
            ~~~~~~~~~
BC36716: Visual Basic 15.3 does not support Private Protected.
    Protected Private Field2 As Integer
              ~~~~~~~
</errors>)

            compilation = CreateCompilationWithMscorlibAndVBRuntime(
                    sources,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

    End Class
End Namespace
