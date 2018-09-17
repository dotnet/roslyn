' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CrossLanguageTest
        Inherits BasicTestBase

        <Fact>
        Public Sub CanAchieveHandledEvents()
            Dim csharpSource = <![CDATA[
[assembly:System.CLSCompliant(true)]
public class Sample
{
    public void Create()
    {
    }
}
]]>

            Dim csharpCompilation = CreateCSharpCompilation(
                csharpSource,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                assemblyName:="Test",
                referencedAssemblies:={MscorlibRef_v4_0_30316_17626})

            Dim method = csharpCompilation.GetTypeByMetadataName("Sample").GetMembers("Create").OfType(Of IMethodSymbol).SingleOrDefault()

            Assert.NotNull(method)
            Assert.Empty(method.HandledEvents())

            Dim basicCompilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Class Sample
    Public Event Created As EventHandler

    Public Sub Create() Handles Me.Created
    End Sub
End Class
    </file>
</compilation>)

            method = basicCompilation.GetTypeByMetadataName("Sample").GetMembers("Create").OfType(Of IMethodSymbol).SingleOrDefault()
            Assert.NotNull(method)
            Assert.Equal(method.HandledEvents().Length, 1)

        End Sub

    End Class

End Namespace
