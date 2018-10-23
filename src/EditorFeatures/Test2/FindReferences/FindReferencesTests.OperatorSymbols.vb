' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WorkItem(539174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539174")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasic_OperatorError1() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpFindReferencesOnUnaryOperatorOverload() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpFindReferencesOnUnaryOperatorOverloadFromDefinition() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpFindReferencesOnBinaryOperatorOverload() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpFindReferencesOnBinaryOperatorOverloadFromDefinition() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicFindReferencesOnUnaryOperatorOverload() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicFindReferencesOnUnaryOperatorOverloadFromDefinition() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicFindReferencesOnBinaryOperatorOverload() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicFindReferencesOnBinaryOperatorOverloadFromDefinition() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(30642, "https://github.com/dotnet/roslyn/issues/30642")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpFindReferencesOnBuiltInOperatorWithUserDefinedEquivalent() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(30642, "https://github.com/dotnet/roslyn/issues/30642")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVisualBasicFindReferencesOnBuiltInOperatorWithUserDefinedEquivalent() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(30642, "https://github.com/dotnet/roslyn/issues/30642")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrossLanguageFindReferencesOnBuiltInOperatorWithUserDefinedEquivalent_FromCSharp() As Task
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
            Await TestAPIAndFeature(input)
        End Function

        <WorkItem(30642, "https://github.com/dotnet/roslyn/issues/30642")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrossLanguageFindReferencesOnBuiltInOperatorWithUserDefinedEquivalent_FromVisualBasic() As Task
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
            Await TestAPIAndFeature(input)
        End Function
    End Class
End Namespace
