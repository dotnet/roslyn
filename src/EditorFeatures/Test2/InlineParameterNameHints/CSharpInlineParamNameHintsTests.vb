' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineParameterNameHints
    Public Class CSharpInlineParamNameHintsTests
        Inherits AbstractInlineParamNameHintsTests

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
        $$testMethod({|x:5|});
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
        $$testMethod({|x:5|}, {|y:2|});
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
        $$testMethod({|x:-5|}, {|y:2|});
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
        $$testMethod({|x:(int)(double)(int)5.5|}, {|y:2|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestObjectParametersSimpleCase() As Task
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
        $$testMethod({|x:(int)5.5|}, {|y:new object()|});
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
        $$testMethod({|x:(int)-5.5|}, {|y:new object()|});
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
        $$testMethod({|x:-(int)5.5|}, {|y:new object()|});
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function
    End Class
End Namespace
