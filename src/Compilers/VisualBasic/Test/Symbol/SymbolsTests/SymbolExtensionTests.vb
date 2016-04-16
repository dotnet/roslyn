' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SymbolExtensionTests
        Inherits BasicTestBase

        <Fact>
        Public Sub HasNameQualifier()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Class C
End Class
Namespace N
    Class C
    End Class
    Namespace NA
        Class C
        End Class
        Namespace NB
            Class C
            End Class
        End Namespace
    End Namespace
    Namespace NB
        Class C
        End Class
    End Namespace
End Namespace
Namespace NA
    Class C
    End Class
    Namespace NA
        Class C
        End Class
    End Namespace
    Namespace NB
        Class C
        End Class
    End Namespace
End Namespace
Namespace NB
    Class C
    End Class
End Namespace
    </file>
</compilation>)
            compilation.AssertNoErrors()
            Dim namespaceNames =
                {
                    "",
                    ".",
                    "N",
                    "NA",
                    "NB",
                    "n",
                    "AN",
                    "NAB",
                    "N.",
                    ".NA",
                    ".NB",
                    "N.N",
                    "N.NA",
                    "N.NB",
                    "N..NB",
                    "N.NA.NA",
                    "N.NA.NB",
                    "NA.N",
                    "NA.NA",
                    "NA.NB",
                    "NA.NA.NB",
                    "NA.NB.NB"
                }
            HasNameQualifierCore(namespaceNames, compilation.GetMember(Of NamedTypeSymbol)("C"), "")
            HasNameQualifierCore(namespaceNames, compilation.GetMember(Of NamedTypeSymbol)("N.C"), "N")
            HasNameQualifierCore(namespaceNames, compilation.GetMember(Of NamedTypeSymbol)("N.NA.C"), "N.NA")
            HasNameQualifierCore(namespaceNames, compilation.GetMember(Of NamedTypeSymbol)("N.NA.NB.C"), "N.NA.NB")
            HasNameQualifierCore(namespaceNames, compilation.GetMember(Of NamedTypeSymbol)("NA.C"), "NA")
            HasNameQualifierCore(namespaceNames, compilation.GetMember(Of NamedTypeSymbol)("NA.NA.C"), "NA.NA")
            HasNameQualifierCore(namespaceNames, compilation.GetMember(Of NamedTypeSymbol)("NA.NB.C"), "NA.NB")
            HasNameQualifierCore(namespaceNames, compilation.GetMember(Of NamedTypeSymbol)("NB.C"), "NB")
        End Sub

        Private Shared Sub HasNameQualifierCore(namespaceNames As String(), type As NamedTypeSymbol, expectedName As String)
            Assert.True(Array.IndexOf(namespaceNames, expectedName) >= 0)
            Assert.Null(type.GetEmittedNamespaceName())
            For Each namespaceName In namespaceNames
                Assert.Equal(namespaceName = expectedName, type.HasNameQualifier(namespaceName, StringComparison.Ordinal))
            Next
        End Sub

    End Class

End Namespace
