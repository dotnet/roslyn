' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestModernExtensionMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
class Program
{
    static void Main(string[] args)
    {
        string s = "Hello";
        s.[|$$ExtensionMethod|]();
    }
}
 
 
public static class MyExtension
{
    extension(string s)
    {
        public int {|Definition:ExtensionMethod|}()
        {
            return s.Length;
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestModernExtensionMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
class Program
{
    static void Main(string[] args)
    {
        string s = "Hello";
        string.[|$$ExtensionMethod|]();
    }
}
 
 
public static class MyExtension
{
    extension(string s)
    {
        public static int {|Definition:ExtensionMethod|}()
        {
            return 0;
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestModernExtensionProperty1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
class Program
{
    static void Main(string[] args)
    {
        string s = "Hello";
        var v = s.[|$$ExtensionProp|];
    }
}
 
 
public static class MyExtension
{
    extension(string s)
    {
        public int {|Definition:ExtensionProp|} => 0;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestModernExtensionMethodParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
 
public static class MyExtension
{
    extension(string $${|Definition:s|})
    {
        public int ExtensionMethod()
        {
            return [|s|].Length;
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestModernExtensionMethodTypeParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
 
public static class MyExtension
{
    extension<$${|Definition:T|}>(string s)
    {
        public int ExtensionMethod([|T|] t)
        {
            return s.Length;
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        Public Async Function FindReferences_ExtensionBlockMethod(kind As TestKind, host As TestHost) As Task
            ' Find references identifies both kinds of calls sites to an extension method:
            ' 1) extension invocation `42.M()`
            ' 2) static implementation method invocation `E.M(42)`
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        42.[|M|]();
        E.[|M|](43);
    }
}

public static class E
{
    extension(int i)
    {
        public void {|Definition:$$M|}() { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        Public Async Function FindReferences_ExtensionBlockMethod_Generic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        42.[|M|]("");
        E.[|M|](43, "");
    }
}

public static class E
{
    extension&lt;T>(T t)
    {
        public void {|Definition:$$M|}&lt;U>(U u) { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        Public Async Function FindReferences_ExtensionBlockMethod_Static(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        int.[|M|]();
        E.[|M|]();
    }
}

public static class E
{
    extension(int)
    {
        public static void {|Definition:$$M|}() { }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        <WpfTheory, CombinatorialData>
        Public Async Function FindReferences_ExtensionBlockProperty(kind As TestKind, host As TestHost) As Task
            ' Find references identifies both kinds of usages of an extension property:
            ' 1) extension access of different kinds (member access like `42.P`, property pattern, object initializer)
            ' 2) static implementation method invocation `E.get_P(42)`/`E.set_P(42, value)`
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        var c = new C();
        _ = c.[|P|];
        c.[|P|] = 1;

        E.[|get_P|](c);
        E.[|set_P|](c, 1);

        _ = c is { [|P|]: 1 };
        _ = new C() { [|P|] = 1 };
    }
}

public static class E
{
    extension(C c)
    {
        public int {|Definition:$$P|} { {|Definition:get|} => i; {|Definition:set|} { } }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        <WpfTheory, CombinatorialData>
        Public Async Function FindReferences_ExtensionBlockProperty_Generic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        var c = new C();
        _ = c.[|P|];
        c.[|P|] = 1;

        E.[|get_P|](c);
        E.[|set_P|](c, 1);

        _ = c is { [|P|]: 1 };
        _ = new C() { [|P|] = 1 };
    }
}

public static class E
{
    extension&lt;T>(T t)
    {
        public int {|Definition:$$P|} { {|Definition:get|} => i; {|Definition:set|} { } }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        <WpfTheory, CombinatorialData>
        Public Async Function FindReferences_ExtensionBlockProperty_FromAccess(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        var c = new C();
        _ = c.[|$$P|];
        c.[|P|] = 1;

        E.[|get_P|](c);
        E.[|set_P|](c, 1);

        _ = c is { [|P|]: 1 };
        _ = new C() { [|P|] = 1 };
    }
}

public static class E
{
    extension(C c)
    {
        public int {|Definition:P|} { {|Definition:get|} => i; {|Definition:set|} { } }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        <WpfTheory, CombinatorialData>
        Public Async Function FindReferences_ExtensionBlockProperty_FromAccess_MultiFile(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        E.[|get_P|](c);
    }
}
        </Document>
        <Document>
class C2
{
    void Test()
    {
        var c = new C();
        _ = c.[|$$P|];
    }
}
        </Document>
        <Document>
class C3
{
    void Test()
    {
        var c = new C();
        c.[|P|] = 1;
    }
}
        </Document>
        <Document>
class C4
{
    void Test()
    {
        var c = new C();
        E.[|get_P|](c);
    }
}
        </Document>
        <Document>
class C5
{
    void Test()
    {
        var c = new C();
        E.[|set_P|](c, 1);
    }
}
        </Document>
        <Document>
class C6
{
    void Test()
    {
        var c = new C();
        _ = c is { [|P|]: 1 };
    }
}
        </Document>
        <Document>
class C7
{
    void Test()
    {
        _ = new C() { [|P|] = 1 };
    }
}
        </Document>
        <Document>
public static class E
{
    extension(C c)
    {
        public int {|Definition:P|} { {|Definition:get|} => i; {|Definition:set|} { } }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        <WpfTheory, CombinatorialData>
        Public Async Function FindReferences_ExtensionBlockProperty_FromImplementationMethodInvocation(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        var c = new C();
        _ = c.[|P|];
        c.[|P|] = 1;

        E.[|$$get_P|](c);
        E.[|set_P|](c, 1);

        _ = c is { [|P|]: 1 };
        _ = new C() { [|P|] = 1 };
    }
}

public static class E
{
    extension(C c)
    {
        public int {|Definition:P|} { {|Definition:get|} => i; {|Definition:set|} { } }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        <WpfTheory, CombinatorialData>
        Public Async Function FindReferences_ExtensionBlockProperty_FromImplementationMethodInvocation_MultiFile(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        E.[|$$get_P|](c);
    }
}
        </Document>
        <Document>
class C2
{
    void Test()
    {
        var c = new C();
        _ = c.[|P|];
    }
}
        </Document>
        <Document>
class C3
{
    void Test()
    {
        var c = new C();
        c.[|P|] = 1;
    }
}
        </Document>
        <Document>
class C4
{
    void Test()
    {
        var c = new C();
        E.[|get_P|](c);
    }
}
        </Document>
        <Document>
class C5
{
    void Test()
    {
        var c = new C();
        E.[|set_P|](c, 1);
    }
}
        </Document>
        <Document>
class C6
{
    void Test()
    {
        var c = new C();
        _ = c is { [|P|]: 1 };
    }
}
        </Document>
        <Document>
class C7
{
    void Test()
    {
        _ = new C() { [|P|] = 1 };
    }
}
        </Document>
        <Document>
public static class E
{
    extension(C c)
    {
        public int {|Definition:P|} { {|Definition:get|} => i; {|Definition:set|} { } }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        <WpfTheory, CombinatorialData>
        Public Async Function FindReferences_ExtensionBlockProperty_Static(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    void Test()
    {
        _ = C.[|P|];
        C.[|P|] = 1;

        E.[|get_P|]();
        E.[|set_P|](1);
    }
}

public static class E
{
    extension(C c)
    {
        public static int {|Definition:$$P|} { {|Definition:get|} => i; {|Definition:set|} { } }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        Public Async Function FindReferences_ExtensionBlockOperator_FromExtensionUsage(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    static void Test(C c1, C c2)
    {
        _ = c1 $$[|+|] c2;
        E.[|op_Addition|](c1, c2);
    }
}

public static class E
{
    extension(C)
    {
        public static C operator {|Definition:+|}(C c1, C c2) => throw null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        Public Async Function FindReferences_ExtensionBlockOperator_FromDisambiguationUsage(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    static void Test(C c1, C c2)
    {
        _ = c1 [|+|] c2;
        E.[|$$op_Addition|](c1, c2);
    }
}

public static class E
{
    extension(C)
    {
        public static C operator {|Definition:+|}(C c1, C c2) => throw null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81507")>
        Public Async Function FindReferences_ExtensionBlockOperator_FromDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class C
{
    static void Test(C c1, C c2)
    {
        _ = c1 [|+|] c2;
        E.[|op_Addition|](c1, c2);
    }
}

public static class E
{
    extension(C)
    {
        public static C operator $${|Definition:+|}(C c1, C c2) => throw null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

    End Class
End Namespace
