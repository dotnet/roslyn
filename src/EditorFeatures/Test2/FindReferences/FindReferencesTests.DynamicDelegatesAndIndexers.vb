' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateWithDynamicArgument() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
	    delegate void myDelegate(dynamic d);
	    void Foo()
	    {	
            dynamic d = 1;
		    myDelegate {|Definition:del|} = n => { Console.WriteLine(n); };
            [|$$del|](d);
	    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexerWithStaticParameter() As Task
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
    public void Foo()
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexerWithDynamicParameter() As Task
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
    public void Foo()
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
            Await TestAsync(input)
        End Function
    End Class
End Namespace
