' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCSAsyncMethodsName1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void TestFunction()
    {
        [|OneAsync|]();
    }
    async void TestFunctionAsync()
    {
        await [|OneAsync|]();
    }
    async Task {|Definition:$$OneAsync|}()
    {
        return;
    }
}
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestVBAsyncMethodsName1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Sub TestSub()
        [|OneAsync|]()
    End Sub
    Async Sub TestSubAsync()
        Await [|OneAsync|]()
    End Sub
    Async Function {|Definition:$$OneAsync|}() As Task
        Return
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCSAsyncMethodsName2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{
    void TestFunction()
    {
        [|TwoAsync|]();  
    }
    async void TestFunctionAsync()
    {    
        await [|TwoAsync|](); 
    }
    async Task<int> {|Definition:$$TwoAsync|}()
    {
        return 1;
    }
}
        ]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestVBAsyncMethodsName2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Sub TestSub()
        [|OneAsync|]()
    End Sub
    Async Sub TestSubAsync()
        Await [|OneAsync|]()
    End Sub
    Async Function {|Definition:$$OneAsync|}() As Task(Of Integer)
        Return 1
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCSAsyncMethodsName3()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    void TestFunction()
    {
        [|ThreeAsync|]();
    }
    async void TestFunctionAsync()
    {
        [|ThreeAsync|]();
    }
    async void {|Definition:$$ThreeAsync|}()
    {
    }
}
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestVBAsyncMethodsName3()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Sub TestSub()
        [|OneAsync|]()
    End Sub
    Async Sub TestSubAsync()
        Await [|OneAsync|]()
    End Sub
    Async Sub {|Definition:$$OneAsync|}()
        'do nothing
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCSAsyncDelegatesName1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{
    Func<Task> {|Definition:$$a1|} = async delegate { return; };
    void TestFunction()
    {
        [|a1|]();
    }
    async Task TestFunctionAsync()
    {
        await [|a1|]();
    }
}        ]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestVBAsyncDelegatesName1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Dim {|Definition:$$a1|} As Func(Of Task) = Async Function()
                                  Return
                              End Function
    Sub TestFunction()
        [|a1|]()
    End Sub
    Async Function TestFunctionAsync() As Task
        Await [|a1|]()
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCSAsyncDelegatesName2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{
    Action<Task> {|Definition:$$a1|} = async delegate (Task t) { await t; };
    void TestFunction()
    {
        Task t;
        [|a1|](t);

    }
    async Task TestFunctionAsync()
    {
        Task t;
        [|a1|](t);
    }
}        ]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestVBAsyncDelegatesName2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Dim {|Definition:$$a1|} As Action(Of Task) = Async Sub(ByVal t As Task)
                                    Await t
                                End Sub
    Sub TestFunction()
        Dim t As Task
        [|a1|](t)
    End Sub
    Async Function TestFunctionAsync() As Task
        Dim t As Task
        [|a1|](t)
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCSAsyncLambaName1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{
    Action<Task> {|Definition:$$f1|} = async (t) => { await t; };
    void TestFunction()
    {
        Task t;
        [|f1|](t);
    }
    async void TestFunctionAsync()
    {
        Task t;
        [|f1|](t);
    }
} ]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestVBAsyncLambaName1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Dim {|Definition:$$a1|} As Action(Of Task) = Async Sub(t)
                                    Await t
                                End Sub
    Sub TestFunction()
        Dim t As Task
        [|a1|](t)
    End Sub
    Async Function TestFunctionAsync() As Task
        Dim t As Task
        [|a1|](t)
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncWithinDelegate()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{    
    Action<Task> a1 = async delegate (Task t) { await [|$$Function|](); };
    public static async Task {|Definition:Function|}() { }   
}
class Program
{
    delegate Task mydel();
    async Task FunctionAsync()
    {    
        mydel d = new mydel(Test.[|Function|]);
    }
} ]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncVBWithinAnonFunctions()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Dim a1 As Action(Of Task) = Async Sub(t)
                                    Await [|$$TestFunctionAsync|]()
                                End Sub
    Delegate Function mydel() As Task(Of Integer)
    Async Function {|Definition:TestFunctionAsync|}() As Task(Of Integer)
        Return 1
    End Function
    Async Sub SubAsync()
        Dim d As mydel = New mydel(AddressOf [|TestFunctionAsync|])
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncWithinLamba()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{    
    Action<Task> a1 = async (Task t) => { await [|$$Function|](); };
    public static async Task {|Definition:Function|}() { }   
}
class Program
{
    delegate Task mydel();
    async Task FunctionAsync()
    {    
        mydel d = new mydel(Test.[|Function|]);
    }
} ]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncFunctionWithAsyncParameters1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{    
    async Task CallFunctionAsync()
    {
        await [|OuterFunctionAsync|](await InnerFunctionAsync());
    }
    async Task {|Definition:$$OuterFunctionAsync|}(int x)
    {
        return;
    }
    async Task<int> InnerFunctionAsync()
    {
        return 1;
    }
}        
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncVBFunctionWithAsyncParameters1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Async Sub CallSubAsync()
        Await OuterFunctionAsync(Await [|$$InnerFunctionAsync|]())
    End Sub
    Async Function OuterFunctionAsync(ByVal x As Integer) As Task
        Return
    End Function
    Async Function {|Definition:InnerFunctionAsync|}() As Task(Of Integer)
        Return 1
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncVBFunctionWithAsyncParameters2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Async Sub CallSubAsync()
        Await [|OuterFunctionAsync|](Await InnerFunctionAsync())
    End Sub
    Async Function {|Definition:$$OuterFunctionAsync|}(ByVal x As Integer) As Task
        Return
    End Function
    Async Function InnerFunctionAsync() As Task(Of Integer)
        Return 1
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncFunctionWithAsyncParameters2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{    
    async Task CallFunctionAsync()
    {
        await OuterFunctionAsync(await [|$$InnerFunctionAsync|]());
    }
    async Task OuterFunctionAsync(int x)
    {
        return;
    }
    async Task<int> {|Definition:InnerFunctionAsync|}()
    {
        return 1;
    }
}        
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncFunctionWithAsyncParameters3()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{
    Func<Task<int>> {|Definition:$$f1|} = async delegate {return 1; };
    async void CallFunctionAsync()
    {
        await OuterFunctionAsync(await [|f1|]());
    }
    async Task OuterFunctionAsync(int x)
    {
        return;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncFunctionWithAsyncParameters4()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{
    Func<int,Task<int>> {|Definition:f1|} = async (x) => {return 1; };
    async void CallFunctionAsync()
    {
        await OuterFunctionAsync(await [|$$f1|](1));
    }
    async Task OuterFunctionAsync(int x)
    {
        return;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncCSFunctionWithRecursion()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Test
{
    async Task {|Definition:$$FunctionAsync|}(int x)
    {
        if (x == 1) return;
        await [|FunctionAsync|](--x);
    }
    public void Function(int x)
    {
        [|FunctionAsync|](x);
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncCSFunctionWithOverloading1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Threading.Tasks;
class Test
{
    async Task {|Definition:FunctionAsync|}(int x)
    {
        FunctionAsync("hello");
        await FunctionAsync(await FunctionAsync<string>("hello"));
    }
    async Task FunctionAsync(string x)
    {
        [|FunctionAsync|](3);
        await FunctionAsync<float>(3f);
        await [|$$FunctionAsync|](await FunctionAsync<int>(3));
    }
    async Task<T> FunctionAsync<T>(T x)
    {
        return x;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncVBFunctionWithOverloading1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Threading.Tasks
Class Test
    Async Function {|Definition:FunctionAsync|}(ByVal x As Integer) As Task
        FunctionAsync("Hello")
        Await FunctionAsync(Await FunctionAsync(Of String)("hello"))
    End Function
    Async Function FunctionAsync(ByVal x As String) As Task
        [|FunctionAsync|](3)
        Await FunctionAsync(Of Single)(3.5F)
        Await [|$$FunctionAsync|](Await FunctionAsync(Of Integer)(3))
    End Function
    Async Function FunctionAsync(Of T)(ByVal x As T) As Task(Of T)
        Return x
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncVBFunctionWithOverloading2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Threading.Tasks
Class Test
    Async Function FunctionAsync(ByVal x As Integer) As Task
        FunctionAsync("Hello")
        Await FunctionAsync(Await [|FunctionAsync|](Of String)("hello"))
    End Function
    Async Function FunctionAsync(ByVal x As String) As Task
        FunctionAsync(3)
        Await [|FunctionAsync|](Of Single)(3.5F)
        Await FunctionAsync(Await [|FunctionAsync|](Of Integer)(3))
    End Function
    Async Function {|Definition:$$FunctionAsync|}(Of T)(ByVal x As T) As Task(Of T)
        Return x
    End Function
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncCSFunctionWithOverloading2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System;
using System.Threading.Tasks;
class Test
{
    async Task FunctionAsync(int x)
    {
        FunctionAsync("hello");
        await FunctionAsync(await [|FunctionAsync|]<string>("hello"));
    }
    async Task FunctionAsync(string x)
    {
        FunctionAsync(3);
        await [|FunctionAsync|]<float>(3f);
        await FunctionAsync(await [|$$FunctionAsync|]<int>(3));
    }
    async Task<T> {|Definition:FunctionAsync|}<T>(T x)
    {
        return x;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncCSAsIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    async Task {|Definition:$$async|}() { }
    async void TestFunction() { await [|async|](); }
}
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAsyncVBAsIdentifier()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Async Function {|Definition:$$Async|}() As Task
        'do nothing
    End Function
    Async Sub TestSub()
        Await [|Async|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAwaitCSAsIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Test
{
    async Task {|Definition:await|}(){}
    async void TestAsync(){ await [|$$@await|]();}
}
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAwaitVBAsIdentifier()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class Test
    Function {|Definition:Await|}() As Integer
        Return 1
    End Function
    Async Function [Await](ByVal x As Integer) As Task
        Return
    End Function
    Async Sub TestAsync()
        [|$$[Await]|]()
        Await [Await](1)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
    End Class
End Namespace
