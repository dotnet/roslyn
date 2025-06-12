' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    <Trait(Traits.Feature, Traits.Features.InlineHints)>
    Public NotInheritable Class CSharpInlineTypeHintsTests
        Inherits AbstractInlineHintsTests

        <WpfFact>
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

            Await VerifyTypeHints(input, input)
        End Function

        <WpfFact>
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

            Dim output =
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

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
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

            Dim output =
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

            Await VerifyTypeHints(input, output, ephemeral:=True)
        End Function

        <WpfFact>
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

            Dim output =
           <Workspace>
               <Project Language="C#" CommonReferences="true">
                   <Document>
class A
{
    void Main() 
    {
        var (i, j) = (0, "");
    }
}
                    </Document>
               </Project>
           </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestOutVarTuple() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void M(out (int, int) x) => x = default;

    void Main() 
    {
        M(out var {|(int, int) :|}x);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
           <Workspace>
               <Project Language="C#" CommonReferences="true">
                   <Document>
class A
{
    void M(out (int, int) x) => x = default;

    void Main() 
    {
        M(out (int, int) x);
    }
}
                    </Document>
               </Project>
           </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestForEachDeconstruction() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
using System.Collections.Generic;

class A
{
    void Main(IDictionary&lt;int, string&gt; d)
    {
        foreach (var ({|int :|}i, {|string :|}s) in d)
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
           <Workspace>
               <Project Language="C#" CommonReferences="true">
                   <Document>
using System.Collections.Generic;

class A
{
    void Main(IDictionary&lt;int, string&gt; d)
    {
        foreach (var (i, s) in d)
        {
        }
    }
}
                    </Document>
               </Project>
           </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestForEachDeconstruction_NestedTuples() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
using System.Collections.Generic;

class A
{
    void Main(IDictionary&lt;int, (string, float)&gt; d)
    {
        foreach (var ({|int :|}i, {|(string, float) :|}sf) in d)
        {
        }

        foreach (var ({|int :|}i, ({|string :|}s, {|float :|}f)) in d)
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
           <Workspace>
               <Project Language="C#" CommonReferences="true">
                   <Document>
using System.Collections.Generic;

class A
{
    void Main(IDictionary&lt;int, (string, float)&gt; d)
    {
        foreach (var (i, sf) in d)
        {
        }

        foreach (var (i, (s, f)) in d)
        {
        }
    }
}
                    </Document>
               </Project>
           </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
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

            Dim output =
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

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
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

            Dim output =
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

            Await VerifyTypeHints(input, output, ephemeral:=True)
        End Function

        <WpfFact>
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

            Await VerifyTypeHints(input, input)
        End Function

        <WpfFact>
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

            Dim output =
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

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
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

            Dim output =
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
            Await VerifyTypeHints(input, output, ephemeral:=True)
        End Function

        <WpfFact>
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

            Await VerifyTypeHints(input, input)
        End Function

        <WpfFact>
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

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System.Linq;
class A
{
    void Main(string[] args) 
    {
        args.Where(a => a.Length > 0);
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
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

            Dim output =
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

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
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

            Await VerifyTypeHints(input, input)
        End Function

        <WpfFact>
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

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        if (int.TryParse("", out int x))
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
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

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        if (int.TryParse("", out int x))
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output, ephemeral:=True)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48941")>
        Public Async Function TestNotWithStronglyTypedDeclarationExpression() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main(string[] args) 
    {
        if (int.TryParse("", out int x))
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/49657")>
        Public Async Function TestWithImplicitObjectCreation_InMethodArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void M(int i) { }

    void Main(string[] args) 
    {
        M(new{| int:|}())
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void M(int i) { }

    void Main(string[] args) 
    {
        M(new int())
        {
        }
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/49657")>
        Public Async Function TestWithImplicitObjectCreation_FieldInitializer() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int field = new{| int:|}();
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int field = new int();
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/49657")>
        Public Async Function TestWithImplicitObjectCreation_LocalInitializer() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void M()
    {
        int i = new{| int:|}();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void M()
    {
        int i = new int();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/49657")>
        Public Async Function TestWithImplicitObjectCreation_ParameterInitializer() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void M(System.Threading.CancellationToken ct = new{| CancellationToken:|}()) { }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void M(System.Threading.CancellationToken ct = new System.Threading.CancellationToken()) { }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/49657")>
        Public Async Function TestWithImplicitObjectCreation_Return() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int M()
    {
        return new{| int:|}();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int M()
    {
        return new int();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/49657")>
        Public Async Function TestWithImplicitObjectCreation_IfExpression() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int M()
    {
        return true
            ? 1
            : new{| int:|}();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    int M()
    {
        return true
            ? 1
            : new int();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestOnlyProduceTagsWithinSelection() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main() 
    {
        var a = 0;
        [|var {|int :|}b = 0;
        var {|int :|}c = 0;|]
        var d = 0;
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    void Main() 
    {
        var a = 0;
        int b = 0;
        int c = 0;
        var d = 0;
    }
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/72219")>
        Public Async Function TestAlias() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System.Collections.Generic;
using TestFile = (string Path, string Content);

class C
{
    void M()
    {
        var {|List&lt;TestFile&gt; :|}testFiles = GetTestFiles();
    }

    List&lt;TestFile&gt; GetTestFiles() => default;
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
using System.Collections.Generic;
using TestFile = (string Path, string Content);

class C
{
    void M()
    {
        List&lt;TestFile&gt; testFiles = GetTestFiles();
    }

    List&lt;TestFile&gt; GetTestFiles() => default;
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48941")>
        Public Async Function TestNoDoubleClickWithCollectionExpression() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    private static readonly ImmutableHashSet&lt;string?&gt; Hashes = {| ImmutableHashSet&lt;string?&gt;:|}[];
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class A
{
    private static readonly ImmutableHashSet&lt;string?&gt; Hashes = [];
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function
    End Class
End Namespace
