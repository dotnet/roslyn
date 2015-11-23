' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestDelegateWithDynamicArgument()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestIndexerWithStaticParameter()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestIndexerWithDynamicParameter()
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
            Test(input)
        End Sub
    End Class
End Namespace
