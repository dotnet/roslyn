' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeLens

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeLens
    <Trait(Traits.Feature, Traits.Features.CodeLens)>
    Public Class VisualBasicCodeLensTests
        Inherits AbstractCodeLensTest

        <Fact>
        Public Async Function TestCount() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1">
        <Document FilePath="CurrentDocument.vb"><![CDATA[
Class A
    {|0: Sub B()|}
        C();
    End Sub

    {|2: Sub C()|}
        D();
    End Sub

    {|1: Sub D()|}
        C();
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await RunCountTest(input)
        End Function

        <Fact>
        Public Async Function TestCapping() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1">
        <Document FilePath="CurrentDocument.vb"><![CDATA[
Class A
    {|0: Sub B()|}
        C();
    End Sub

    {|capped1: Sub C()|}
        D();
    End Sub

    {|1: Sub D()|}
        C();
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await RunCountTest(input, 1)
        End Function

        <Fact>
        Public Async Function TestDisplay() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1">
        <Document FilePath="CurrentDocument.vb"><![CDATA[
Class A
    {|0: Sub B()|}
        C();
    End Sub

    {|2: Sub C()|}
        D();
    End Sub

    {|1: Sub D()|}
        C();
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await RunReferenceTest(input)
        End Function

        <Fact>
        Public Async Function TestMethodReferences() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1">
        <Document FilePath="CurrentDocument.vb"><![CDATA[
Class A
    {|0: Sub B()|}
        C();
    End Sub

    {|2: Sub C()|}
        D();
    End Sub

    {|1: Sub D()|}
        C();
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await RunMethodReferenceTest(input)
        End Function

        <Fact>
        Public Async Function TestMethodReferencesWithDocstrings() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1">
        <Document FilePath="CurrentDocument.cs"><![CDATA[
Class A
{
    ''' <summary>
    '''     <see cref="A.C"/>
    ''' </summary>
    {|0: Sub B()|}
        C();
    End Sub

    {|2: Sub C()|}
        D();
    End Sub

    {|1: Sub D()|}
        C();
    End Sub
}
]]>
        </Document>
    </Project>
</Workspace>
            Await RunMethodReferenceTest(input)
        End Function
    End Class
End Namespace
