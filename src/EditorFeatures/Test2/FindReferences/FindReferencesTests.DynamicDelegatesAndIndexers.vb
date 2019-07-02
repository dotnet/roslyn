' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateWithDynamicArgument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
	    delegate void myDelegate(dynamic d);
	    void Goo()
	    {	
            dynamic d = 1;
		    myDelegate {|Definition:del|} = n => { Console.WriteLine(n); };
            [|$$del|](d);
	    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexerWithStaticParameter(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    public int {|Definition:$$this|}[int i] { get { } }   
    public int this[dynamic i] { get { } }
}
class B
{
    public void Goo()
    {
        A a = new A();
        dynamic d = 1;
        var a1 = a[||][1];
        var a2 = a["hello"];
        var a3 = a[||][d];
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexerWithDynamicParameter(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    public int this[int i] { get { } }   
    public int {|Definition:$$this|}[dynamic i] { get { } }
}
class B
{
    public void Goo()
    {
        A a = new A();
        dynamic d = 1;
        var a1 = a[1];
        var a2 = a[||]["hello"];
        var a3 = a[||][d];
    }
}        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
