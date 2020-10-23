' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    Public Class CSharpInlineTypeHintsTests
        Inherits AbstractInlineHintsTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestNotOnLocalVariableWithType() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main() 
    {
        int i = 0;
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestOnLocalVariableWithVarType() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main() 
    {
        var {|int :|}i = 0;
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestOnLocalVariableWithVarType_Ephemeral() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main() 
    {
        {|int:var|} i = 0;
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, ephemeral:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestOnDeconstruction() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main() 
    {
        var ({|int :|}i, {|string :|}j) = (0, "");
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestWithForeachVar() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        foreach (var {|string :|}j in args) {}
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestWithForeachVar_Ephemeral() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        foreach ({|string:var|} j in args) {}
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, ephemeral:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestNotWithForeachType() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        foreach (string j in args) {}
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestWithPatternVar() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        if (args is { Length: var {|int :|}goo }) { }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestWithPatternVar_Ephemeral() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        if (args is { Length: {|int:var|} goo }) { }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, ephemeral:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestNotWithPatternType() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        if (args is { Length: int goo }) { }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestWithSimpleLambda() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System.Linq;
class A
{
    void Main(string[] args) 
    {
        args.Where({|string :|}a => a.Length > 0);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestWithParenthesizedLambda() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System.Linq;
class A
{
    void Main(string[] args) 
    {
        args.Where(({|string :|}a) => a.Length > 0);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestNotWithParenthesizedLambdaWithType() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System.Linq;
class A
{
    void Main(string[] args) 
    {
        args.Where((string a) => a.Length > 0);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestWithDeclarationExpression() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        if (int.TryParse("", out var {|int :|}x))
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineHints)>
        Public Async Function TestWithDeclarationExpression_Ephemeral() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        if (int.TryParse("", out {|int:var|} x))
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, ephemeral:=True)
        End Function
    End Class
End Namespace
