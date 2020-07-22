' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineParameterNameHints
    Public Class CSharpInlineParameterNameHintsTests
        Inherits AbstractInlineParameterNameHintsTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestNoParameterSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod()
    {
        return 5;
    }
    void Main() 
    {
        testMethod();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestOneParameterSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:5|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestTwoParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:5|}, {|y:2|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestNegativeNumberParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:-5|}, {|y:2|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestLiteralNestedCastParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, double y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:(int)(double)(int)5.5|}, {|y:2|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestObjectCreationParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:(int)5.5|}, {|y:new object()|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestCastingANegativeSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:(int)-5.5|}, {|y:new object()|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestNegatingACastSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:-(int)5.5|}, {|y:new object()|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestMissingParameterNameSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int)
    {
        return 5;
    }
    void Main() 
    {
        testMethod();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestDelegateParameter() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
delegate void D(int x);

class C
{
    public static void M1(int i) { }
}

class Test
{
    static void Main()
    {
        D cd1 = new D(C.M1);
        cd1({|x:-1|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestFunctionPointerNoParameter() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true" AllowUnsafe="true">
                    <Document>
unsafe class Example {
    void Example(delegate*&lt;int, void&gt; f) {
        f(42);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestParamsArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    public void UseParams(params int[] list)
    {
    }

    public void Main(string[] args)
    {
        UseParams({|list:1|}, 2, 3, 4, 5, 6); 
    } 
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestAttributesArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System;

[Obsolete({|message:"test"|})]
class Foo
{
        

}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestIncompleteFunctionCall() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int testMethod(int x, object y)
    {
        return x;
    }
    void Main() 
    {
        testMethod({|x:-(int)5.5|},);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function
    End Class
End Namespace
