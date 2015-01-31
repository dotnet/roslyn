' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(539174)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub VisualBasic_OperatorError1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    Sub Main(args As String())
        Dim b = 5 $$-
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub CSharpFindReferencesOnOperatorOverload()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Foo()
    {
        var x = new A() [|$$+|] new A();
    }
    public static A operator {|Definition:+|}(A a, A b) { return a; }
}
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub VisualBasicFindReferencesOnOperatorOverload()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class A
    Public Shared Operator {|Definition:^|}(x As A, y As A) As A
        Return y
    End Operator

    Sub Foo()
        Dim a = New A [|^$$|] New A
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
    End Class
End Namespace
