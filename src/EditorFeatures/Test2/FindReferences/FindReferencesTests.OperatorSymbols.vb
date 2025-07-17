' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539174")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasic_OperatorError1(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnUnaryOperatorOverload(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        A a;
        var x = $$[|-|]a;
    }
    public static A operator {|Definition:-|}(A a) { return a; }}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnUnaryOperatorOverloadFromDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        A a;
        var x = [|-|]a;
    }
    public static A operator {|Definition:$$-|}(A a) { return a; }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnBinaryOperatorOverload_01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        var x = new A() [|$$+|] new A();
    }
    public static A operator {|Definition:+|}(A a, A b) { return a; }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnBinaryOperatorOverload_02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        var x = new A() [|$$>>>|] 1;
    }
    public static A operator {|Definition:>>>|}(A a, int b) { return a; }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnInstanceIncrementOperators(kind As TestKind, host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class A
{
    void Goo()
    {
        var x = new A();
        [|$$<%= op %>|] x;
    }
    public void operator {|Definition:<%= op %>|}() {}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnInstanceIncrementOperators_Checked(kind As TestKind, host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class A
{
    void Goo()
    {
        var x = new A();
        checked
        {
            x [|$$<%= op %>|];
        }
    }
    public void operator checked {|Definition:<%= op %>|}() {}
    public void operator <%= op %>() {}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnInstanceCompoundAssignmentOperators(kind As TestKind, host As TestHost, <CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class A
{
    void Goo()
    {
        var x = new A();
        x [|$$<%= op %>|] 1;
    }
    public void operator {|Definition:<%= op %>|}(int x) {}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnInstanceCompoundAssignmentOperators_Checked(kind As TestKind, host As TestHost, <CombinatorialValues("+=", "-=", "*=", "/=")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class A
{
    void Goo()
    {
        var x = new A();
        checked
        {
            x [|$$<%= op %>|] 1;
        }
    }
    public void operator checked {|Definition:<%= op %>|}(int x) {}
    public void operator <%= op %>(int x) {}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnBinaryOperatorOverloadFromDefinition_01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        var x = new A() [|+|] new A();
    }
    public static A operator {|Definition:$$+|}(A a, A b) { return a; }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnBinaryOperatorOverloadFromDefinition_02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo()
    {
        var x = new A() [|>>>|] 1;
    }
    public static A operator {|Definition:$$>>>|}(A a, int b) { return a; }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnInstanceIncrementOperators_FromDefinition(kind As TestKind, host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class A
{
    void Goo()
    {
        var x = new A();
        [|<%= op %>|] x;
    }
    public void operator {|Definition:$$<%= op %>|}() {}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnInstanceIncrementOperators_FromDefinition_Checked(kind As TestKind, host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class A
{
    void Goo()
    {
        var x = new A();
        checked
        {
            x [|<%= op %>|];
        }
    }
    public void operator checked {|Definition:$$<%= op %>|}() {}
    public void operator <%= op %>() {}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnInstanceCompoundAssignmentOperators_FromDefinition(kind As TestKind, host As TestHost, <CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class A
{
    void Goo()
    {
        var x = new A();
        x [|<%= op %>|] 1;
    }
    public void operator {|Definition:$$<%= op %>|}(int x) {}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnInstanceCompoundAssignmentOperators_FromDefinition_Checked(kind As TestKind, host As TestHost, <CombinatorialValues("+=", "-=", "*=", "/=")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
class A
{
    void Goo()
    {
        var x = new A();
        checked
        {
            x [|<%= op %>|] 1;
        }
    }
    public void operator checked {|Definition:$$<%= op %>|}(int x) {}
    public void operator <%= op %>(int x) {}
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/52654")>
        Public Async Function TestCSharpFindReferencesOnEqualsOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class A
{
    void Goo()
    {
        var x = new A() [|==|] new A();
    }
    public static bool operator {|Definition:$$==|}(A left, A right) => throw new System.NotImplementedException();
    public static bool operator !=(A left, A right) => throw new System.NotImplementedException();
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/52654")>
        Public Async Function TestCSharpFindReferencesOnNotEqualsOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class A
{
    void Goo()
    {
        var x = new A() [|!=|] new A();
    }
    public static bool operator ==(A left, A right) => throw new System.NotImplementedException();
    public static bool operator {|Definition:$$!=|}(A left, A right) => throw new System.NotImplementedException();
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/52654")>
        Public Async Function TestCSharpFindReferencesOnGreaterThanOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class A
{
    void Goo()
    {
        var x = new A() [|>|] new A();
    }
    public static bool operator {|Definition:$$>|}(A left, A right) => throw new System.NotImplementedException();
    public static bool operator <(A left, A right) => throw new System.NotImplementedException();
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/52654")>
        Public Async Function TestCSharpFindReferencesOnLessThanOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class A
{
    void Goo()
    {
        var x = new A() [|<|] new A();
    }
    public static bool operator >(A left, A right) => throw new System.NotImplementedException();
    public static bool operator {|Definition:$$<|}(A left, A right) => throw new System.NotImplementedException();
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/52654")>
        Public Async Function TestCSharpFindReferencesOnGreaterThanOrEqualsOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class A
{
    void Goo()
    {
        var x = new A() [|>=|] new A();
    }
    public static bool operator {|Definition:$$>=|}(A left, A right) => throw new System.NotImplementedException();
    public static bool operator <=(A left, A right) => throw new System.NotImplementedException();
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/52654")>
        Public Async Function TestCSharpFindReferencesOnLessThanOrEqualsOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class A
{
    void Goo()
    {
        var x = new A() [|<=|] new A();
    }
    public static bool operator >=(A left, A right) => throw new System.NotImplementedException();
    public static bool operator {|Definition:$$<=|}(A left, A right) => throw new System.NotImplementedException();
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasicFindReferencesOnUnaryOperatorOverload(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class A
    Public Shared Operator {|Definition:-|}(x As A) As A
        Return x
    End Operator

    Sub Goo()
        Dim a As A
        Dim b = $$[|-|]a
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasicFindReferencesOnUnaryOperatorOverloadFromDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class A
    Public Shared Operator {|Definition:$$-|}(x As A) As A
        Return x
    End Operator

    Sub Goo()
        Dim a As A
        Dim b = [|-|]a
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasicFindReferencesOnBinaryOperatorOverload(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class A
    Public Shared Operator {|Definition:^|}(x As A, y As A) As A
        Return y
    End Operator

    Sub Goo()
        Dim a = New A [|^$$|] New A
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasicFindReferencesOnBinaryOperatorOverloadFromDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class A
    Public Shared Operator {|Definition:$$^|}(x As A, y As A) As A
        Return y
    End Operator

    Sub Goo()
        Dim a = New A [|^|] New A
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/30642")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpFindReferencesOnBuiltInOperatorWithUserDefinedEquivalent(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo(string a, string b, int x, int y)
    {
        var m = a $$[|==|] b;
        var n = a [|==|] b;
        var o = x == y;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/30642")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestVisualBasicFindReferencesOnBuiltInOperatorWithUserDefinedEquivalent(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class A
    sub Goo(a as string, b as string, x as integer, y as integer)
        dim m = a $$[|=|] b
        dim n = a [|=|] b
        dim o = x = y
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/30642")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCrossLanguageFindReferencesOnBuiltInOperatorWithUserDefinedEquivalent_FromCSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo(string a, string b, int x, int y)
    {
        var m = a $$[|==|] b;
        var n = a [|==|] b;
        var o = x == y;
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class A
    sub Goo(a as string, b as string, x as integer, y as integer)
        dim m = a [|=|] b
        dim n = a [|=|] b
        dim o = x = y
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/30642")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCrossLanguageFindReferencesOnBuiltInOperatorWithUserDefinedEquivalent_FromVisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    void Goo(string a, string b, int x, int y)
    {
        var m = a [|==|] b;
        var n = a [|==|] b;
        var o = x == y;
    }
}
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
class A
    sub Goo(a as string, b as string, x as integer, y as integer)
        dim m = a $$[|=|] b
        dim n = a [|=|] b
        dim o = x = y
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOperatorReferenceInGlobalSuppression_01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:A.[|op_Addition|](A,A)~A")]

class A
{
    void Goo()
    {
        var x = new A() [|$$+|] new A();
    }

    public static A operator {|Definition:+|}(A a, A b) { return a; }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestOperatorReferenceInGlobalSuppression_02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:A.[|op_UnsignedRightShift|](A,System.Int32)~A")]

class A
{
    void Goo()
    {
        var x = new A() [|$$>>>|] 1;
    }

    public static A operator {|Definition:>>>|}(A a, int b) { return a; }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpFindOperatorUsedInSourceGeneratedDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class A
{
    public static A operator {|Definition:$$-|}(A a) { return a; }
}
        </Document>
        <DocumentFromSourceGenerator>
class B
{
    void Goo()
    {
        A a;
        var x = [|-|]a;
    }
}
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractConversionOperatorInInterface(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I5<T> where T : I5<T>
{
    abstract static implicit operator {|Definition:i$$nt|}(T x);
}

class C5_1 : I5<C5_1>
{
    public static implicit operator {|Definition:int|}(C5_1 x) => default;
}

class C5_2 : I5<C5_2>
{
    static implicit I5<C5_2>.operator {|Definition:int|}(C5_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractConversionOperatorViaFeature1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I5<T> where T : I5<T>
{
    abstract static implicit operator {|Definition:int|}(T x);
}

class C5_1 : I5<C5_1>
{
    public static implicit operator {|Definition:i$$nt|}(C5_1 x) => default;
}

class C5_2 : I5<C5_2>
{
    static implicit I5<C5_2>.operator int(C5_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractConversionOperatorViaFeature2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I5<T> where T : I5<T>
{
    abstract static implicit operator {|Definition:int|}(T x);
}

class C5_1 : I5<C5_1>
{
    public static implicit operator int(C5_1 x) => default;
}

class C5_2 : I5<C5_2>
{
    static implicit I5<C5_2>.operator {|Definition:i$$nt|}(C5_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractConversionOperatorViaApi1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I5<T> where T : I5<T>
{
    abstract static implicit operator {|Definition:int|}(T x);
}

class C5_1 : I5<C5_1>
{
    public static implicit operator {|Definition:int|}(C5_1 x) => default;
}

class C5_2 : I5<C5_2>
{
    static implicit I5<C5_2>.operator {|Definition:i$$nt|}(C5_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractConversionOperatorViaApi2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I5<T> where T : I5<T>
{
    abstract static implicit operator {|Definition:int|}(T x);
}

class C5_1 : I5<C5_1>
{
    public static implicit operator {|Definition:in$$t|}(C5_1 x) => default;
}

class C5_2 : I5<C5_2>
{
    static implicit I5<C5_2>.operator {|Definition:int|}(C5_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorInInterface_01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:+$$|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static int operator {|Definition:+|}(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator {|Definition:+|}(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorInInterface_02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:>>>$$|}(T x, int y);
}

class C4_1 : I4<C4_1>
{
    public static int operator {|Definition:>>>|}(C4_1 x, int y) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator {|Definition:>>>|}(C4_2 x, int y) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractStaticIncrementOperatorsInInterface(kind As TestKind, host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static T operator {|Definition:<%= op %>$$|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static C4_1 operator {|Definition:<%= op %>|}(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static C4_2 I4<C4_2>.operator {|Definition:<%= op %>|}(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractInstanceIncrementOperatorsInInterface(kind As TestKind, host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract void operator {|Definition:<%= op %>$$|}();
}

class C4_1 : I4<C4_1>
{
    public void operator {|Definition:<%= op %>|}() {}
}

class C4_2 : I4<C4_2>
{
    void I4<C4_2>.operator {|Definition:<%= op %>|}() {}
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractInstanceCompoundAssignmentOperatorsInInterface(kind As TestKind, host As TestHost, <CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract void operator {|Definition:<%= op %>$$|}(int x);
}

class C4_1 : I4<C4_1>
{
    public void operator {|Definition:<%= op %>|}(int x) {}
}

class C4_2 : I4<C4_2>
{
    void I4<C4_2>.operator {|Definition:<%= op %>|}(int x) {}
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorViaApi1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:+|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static int operator {|Definition:$$+|}(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator {|Definition:+|}(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorViaApi2_01(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:+|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static int operator {|Definition:+|}(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator {|Definition:$$+|}(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorViaApi2_02(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:>>>|}(T x, int y);
}

class C4_1 : I4<C4_1>
{
    public static int operator {|Definition:>>>|}(C4_1 x, int y) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator {|Definition:$$>>>|}(C4_2 x, int y) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractStaticIncrementOperatorsInInterface_ViaApi(host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static T operator {|Definition:<%= op %>|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static C4_1 operator {|Definition:<%= op %>|}(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static C4_2 I4<C4_2>.operator {|Definition:<%= op %>$$|}(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractInstanceIncrementOperatorsInInterface_ViaApi(host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract void operator {|Definition:<%= op %>|}();
}

class C4_1 : I4<C4_1>
{
    public void operator {|Definition:<%= op %>|}() {}
}

class C4_2 : I4<C4_2>
{
    void I4<C4_2>.operator {|Definition:<%= op %>$$|}() {}
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractInstanceCompoundAssignmentOperatorsInInterface_ViaApi(host As TestHost, <CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract void operator {|Definition:<%= op %>|}(int x);
}

class C4_1 : I4<C4_1>
{
    public void operator {|Definition:<%= op %>|}(int x) {}
}

class C4_2 : I4<C4_2>
{
    void I4<C4_2>.operator {|Definition:<%= op %>$$|}(int x) {}
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorViaFeature1_01(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:+|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static int operator {|Definition:$$+|}(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator +(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorViaFeature1_02(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:>>>|}(T x, int y);
}

class C4_1 : I4<C4_1>
{
    public static int operator {|Definition:$$>>>|}(C4_1 x, int y) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator >>>(C4_2 x, int y) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractStaticIncrementOperatorsInInterface_ViaFeature_1(host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static T operator {|Definition:<%= op %>|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static C4_1 operator {|Definition:$$<%= op %>|}(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static C4_2 I4<C4_2>.operator <%= op %>(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractInstanceIncrementOperatorsInInterface_ViaFeature_1(host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract void operator {|Definition:<%= op %>|}();
}

class C4_1 : I4<C4_1>
{
    public void operator {|Definition:$$<%= op %>|}() {}
}

class C4_2 : I4<C4_2>
{
    void I4<C4_2>.operator <%= op %>() {}
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractInstanceCompoundAssignmentOperatorsInInterface_ViaFeature_1(host As TestHost, <CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract void operator {|Definition:<%= op %>|}(int x);
}

class C4_1 : I4<C4_1>
{
    public void operator {|Definition:$$<%= op %>|}(int x) {}
}

class C4_2 : I4<C4_2>
{
    void I4<C4_2>.operator <%= op %>(int x) {}
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorViaFeature2_01(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:+|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static int operator +(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator {|Definition:$$+|}(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/60216")>
        Public Async Function TestCSharpFindReferencesOnCheckedAdditionOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    public static C operator +(C x, C y) => throw new System.Exception();
    public static C operator checked {|Definition:$$+|}(C x, C y) => throw new System.Exception();

    void M()
    {
        var a = checked(new C() [|+|] new C());
        var b = unchecked(new C() + new C());
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/60216")>
        Public Async Function TestCSharpFindReferencesOnCheckedDecrementOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    public static C operator --(C x) => throw new System.Exception();
    public static C operator checked {|Definition:$$--|}(C x) => throw new System.Exception();

    void M()
    {
        var c = new C();
        var a = checked(c[|--|]);
        var b = unchecked(c--);
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/60216")>
        Public Async Function TestCSharpFindReferencesOnCheckedDivisionOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    public static C operator /(C x, C y) => throw new System.Exception();
    public static C operator checked {|Definition:$$/|}(C x, C y) => throw new System.Exception();

    void M()
    {
        var a = checked(new C() [|/|] new C());
        var b = unchecked(new C() / new C());
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/60216")>
        Public Async Function TestCSharpFindReferencesOnCheckedIncrementOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    public static C operator ++(C x) => throw new System.Exception();
    public static C operator checked {|Definition:$$++|}(C x) => throw new System.Exception();

    void M()
    {
        var c = new C();
        var a = checked(c[|++|]);
        var b = unchecked(c++);
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/60216")>
        Public Async Function TestCSharpFindReferencesOnCheckedMultiplyOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    public static C operator *(C x, C y) => throw new System.Exception();
    public static C operator checked {|Definition:$$*|}(C x, C y) => throw new System.Exception();

    void M()
    {
        var a = checked(new C() [|*|] new C());
        var b = unchecked(new C() * new C());
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/60216")>
        Public Async Function TestCSharpFindReferencesOnCheckedSubtractionOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    public static C operator -(C x, C y) => throw new System.Exception();
    public static C operator checked {|Definition:$$-|}(C x, C y) => throw new System.Exception();

    void M()
    {
        var a = checked(new C() [|-|] new C());
        var b = unchecked(new C() - new C());
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/60216")>
        Public Async Function TestCSharpFindReferencesOnCheckedUnaryNegationOperator(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    public static C operator -(C x) => throw new System.Exception();
    public static C operator checked {|Definition:$$-|}(C x) => throw new System.Exception();

    void M()
    {
        var c = new C();
        var a = checked([|-|]c);
        var b = unchecked(-c);
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCSharpStaticAbstractOperatorViaFeature2_02(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static int operator {|Definition:>>>|}(T x, int y);
}

class C4_1 : I4<C4_1>
{
    public static int operator >>>(C4_1 x, int y) => default;
}

class C4_2 : I4<C4_2>
{
    static int I4<C4_2>.operator {|Definition:$$>>>|}(C4_2 x, int y) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractStaticIncrementOperatorsInInterface_ViaFeature_2(host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract static T operator {|Definition:<%= op %>|}(T x);
}

class C4_1 : I4<C4_1>
{
    public static C4_1 operator <%= op %>(C4_1 x) => default;
}

class C4_2 : I4<C4_2>
{
    static C4_2 I4<C4_2>.operator {|Definition:$$<%= op %>|}(C4_2 x) => default;
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractInstanceIncrementOperatorsInInterface_ViaFeature_2(host As TestHost, <CombinatorialValues("++", "--")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract void operator {|Definition:<%= op %>|}();
}

class C4_1 : I4<C4_1>
{
    public void operator <%= op %>() {}
}

class C4_2 : I4<C4_2>
{
    void I4<C4_2>.operator {|Definition:$$<%= op %>|}() {}
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/78375"), CombinatorialData>
        Public Async Function TestCSharpAbstractInstanceCompoundAssignmentOperatorsInInterface_ViaFeature_2(host As TestHost, <CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")> op As String) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
            <![CDATA[
interface I4<T> where T : I4<T>
{
    abstract void operator {|Definition:<%= op %>|}(int x);
}

class C4_1 : I4<C4_1>
{
    public void operator <%= op %>(int x) {}
}

class C4_2 : I4<C4_2>
{
    void I4<C4_2>.operator {|Definition:$$<%= op %>|}(int x) {}
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestStreamingFeature(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7311")>
        Public Async Function TestCSharpBitwiseLogicalAndOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
struct Program
{
    bool b;
    static void Main(string[] args)
    {
        Program p1 = new Program();
        Program p2 = new Program();
        if (p1 [|$$&|] p2)
        {
        }
        else if (p1 [|&&|] p2)
        {
        }
    }
    public static Program operator {|Definition:&|}(Program p1, Program p2)
    {
        return new Program() { b = p1.b & p2.b };
    }
    public static bool operator true(Program p)
    {
        return p.b;
    }
    public static bool operator false(Program p)
    {
        return !p.b;
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7311")>
        Public Async Function TestCSharpBitwiseLogicalAndOperator2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
struct Program
{
    bool b;
    static void Main(string[] args)
    {
        Program p1 = new Program();
        Program p2 = new Program();
        if (p1 [|&|] p2)
        {
        }
        else if (p1 [|$$&&|] p2)
        {
        }
    }
    public static Program operator {|Definition:&|}(Program p1, Program p2)
    {
        return new Program() { b = p1.b & p2.b };
    }
    public static bool operator true(Program p)
    {
        return p.b;
    }
    public static bool operator false(Program p)
    {
        return !p.b;
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7311")>
        Public Async Function TestCSharpBitwiseLogicalAndOperator3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
struct Program
{
    bool b;
    static void Main(string[] args)
    {
        Program p1 = new Program();
        Program p2 = new Program();
        if (p1 [|&|] p2)
        {
        }
        else if (p1 [|&&|] p2)
        {
        }
    }
    public static Program operator {|Definition:$$&|}(Program p1, Program p2)
    {
        return new Program() { b = p1.b & p2.b };
    }
    public static bool operator true(Program p)
    {
        return p.b;
    }
    public static bool operator false(Program p)
    {
        return !p.b;
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7311")>
        Public Async Function TestCSharpBitwiseLogicalOrOperator1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
struct Program
{
    bool b;
    static void Main(string[] args)
    {
        Program p1 = new Program();
        Program p2 = new Program();
        if (p1 [|$$||] p2)
        {
        }
        else if (p1 [||||] p2)
        {
        }
    }
    public static Program operator {|Definition:||}(Program p1, Program p2)
    {
        return new Program() { b = p1.b & p2.b };
    }
    public static bool operator true(Program p)
    {
        return p.b;
    }
    public static bool operator false(Program p)
    {
        return !p.b;
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7311")>
        Public Async Function TestCSharpBitwiseLogicalOrOperator2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
struct Program
{
    bool b;
    static void Main(string[] args)
    {
        Program p1 = new Program();
        Program p2 = new Program();
        if (p1 [|||] p2)
        {
        }
        else if (p1 [|$$|||] p2)
        {
        }
    }
    public static Program operator {|Definition:||}(Program p1, Program p2)
    {
        return new Program() { b = p1.b & p2.b };
    }
    public static bool operator true(Program p)
    {
        return p.b;
    }
    public static bool operator false(Program p)
    {
        return !p.b;
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7311")>
        Public Async Function TestCSharpBitwiseLogicalOrOperator3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
struct Program
{
    bool b;
    static void Main(string[] args)
    {
        Program p1 = new Program();
        Program p2 = new Program();
        if (p1 [||||] p2)
        {
        }
        else if (p1 [||||] p2)
        {
        }
    }
    public static Program operator {|Definition:$$||}(Program p1, Program p2)
    {
        return new Program() { b = p1.b & p2.b };
    }
    public static bool operator true(Program p)
    {
        return p.b;
    }
    public static bool operator false(Program p)
    {
        return !p.b;
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
