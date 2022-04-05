' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class RefFieldTests
        Inherits BasicTestBase

        <Fact>
        Public Sub RefField()
            Dim sourceA =
"public ref struct S<T>
{
    private ref T F;
    public S(ref T t) { F = ref t; }
}"
            Dim compA = CreateCSharpCompilation(GetUniqueName(), sourceA, parseOptions:=New CSharp.CSharpParseOptions(languageVersion:=CSharp.LanguageVersion.Preview))
            compA.VerifyDiagnostics()
            Dim refA = compA.EmitToImageReference()

            Dim sourceB =
"Class Program
    Public Sub Main()
        Dim s = New S(Of Integer)()
    End Sub
End Class"

            Dim compB = CreateCompilation(sourceB, references:={refA})
            compB.AssertTheseDiagnostics(<expected>
BC30668: 'S(Of Integer)' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'.
        Dim s = New S(Of Integer)()
                    ~~~~~~~~~~~~~
</expected>)
        End Sub

    End Class

End Namespace
