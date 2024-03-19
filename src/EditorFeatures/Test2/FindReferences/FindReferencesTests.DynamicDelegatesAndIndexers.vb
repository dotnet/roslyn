' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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
