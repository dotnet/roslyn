' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CrossLanguageTest
        Inherits BasicTestBase

        <Fact>
        Public Sub CanAchieveHandledEvents()
            Dim csharpCompilation = CreateCSharpCompilation(<![CDATA[
[assembly:System.CLSCompliant(true)]
public class Sample
{
    public void Create()
    {
    }
}
]]>)

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
            Assert.Equal(1, method.HandledEvents().Length)

        End Sub

    End Class

End Namespace
