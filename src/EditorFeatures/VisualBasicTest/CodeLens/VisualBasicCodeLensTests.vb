' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeLens

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeLens
    Public Class VisualBasicCodeLensTests
        Inherits AbstractCodeLensTest

        <Fact, Trait(Traits.Feature, Traits.Features.CodeLens)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeLens)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeLens)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeLens)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeLens)>
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
