﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSAsyncMethodsName1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAsyncMethodsName1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSAsyncMethodsName2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAsyncMethodsName2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSAsyncMethodsName3(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAsyncMethodsName3(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSAsyncDelegatesName1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAsyncDelegatesName1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSAsyncDelegatesName2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAsyncDelegatesName2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSAsyncLambdaName1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVBAsyncLambdaName1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncWithinDelegate(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncVBWithinAnonFunctions(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncWithinLambda(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncFunctionWithAsyncParameters1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncVBFunctionWithAsyncParameters1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncVBFunctionWithAsyncParameters2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncFunctionWithAsyncParameters2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncFunctionWithAsyncParameters3(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncFunctionWithAsyncParameters4(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncCSFunctionWithRecursion(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncCSFunctionWithOverloading1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncVBFunctionWithOverloading1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncVBFunctionWithOverloading2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncCSFunctionWithOverloading2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncCSAsIdentifier(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAsyncVBAsIdentifier(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAwaitCSAsIdentifier(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAwaitVBAsIdentifier(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
