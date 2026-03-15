' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.InlineHints
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    <Trait(Traits.Feature, Traits.Features.InlineHints)>
    Public NotInheritable Class CSharpInlineTypeHintsTests
        Inherits AbstractInlineHintsTests

        Private Async Function VerifyTypeHintsWithOptions(test As XElement, output As XElement, options As InlineTypeHintsOptions) As Task
            Using workspace = EditorTestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(CSharpInlineTypeHintsTests)}.{NameOf(Me.VerifyTypeHintsWithOptions)} creates asynchronous taggers")

                Dim displayOptions = New SymbolDescriptionOptions()

                Dim hostDocument = workspace.Documents.Single()
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tagService = document.GetRequiredLanguageService(Of IInlineTypeHintsService)

                Dim span = If(hostDocument.SelectedSpans.Any(), hostDocument.SelectedSpans.Single(), New TextSpan(0, snapshot.Length))
                Dim typeHints = ArrayBuilder(Of InlineHint).GetInstance()

                Await tagService.AddInlineHintsAsync(
                    document, span, options, displayOptions, displayAllOverride:=False, typeHints, CancellationToken.None)

                Dim producedTags = From hint In typeHints
                                   Select hint.DisplayParts.GetFullText() + ":" + hint.Span.ToString()

                ValidateSpans(hostDocument, producedTags)

                Dim outWorkspace = EditorTestWorkspace.Create(output)
                Dim expectedDocument = outWorkspace.CurrentSolution.GetDocument(outWorkspace.Documents.Single().Id)
                Await ValidateDoubleClick(document, expectedDocument, typeHints)
            End Using
        End Function

        Private Shared Sub ValidateSpans(hostDocument As TestHostDocument, producedTags As IEnumerable(Of String))
            Dim expectedTags As New List(Of String)

            Dim nameAndSpansList = hostDocument.AnnotatedSpans.SelectMany(
                Function(name) name.Value,
                Function(name, span) New With {.Name = name.Key, span})

            For Each nameAndSpan In nameAndSpansList.OrderBy(Function(x) x.span.Start)
                expectedTags.Add(nameAndSpan.Name + ":" + nameAndSpan.span.ToString())
            Next

            AssertEx.Equal(expectedTags, producedTags)
        End Sub

        Private Shared Async Function ValidateDoubleClick(document As Document, expectedDocument As Document, typeHints As ArrayBuilder(Of InlineHint)) As Task
            Dim textChanges = New List(Of TextChange)
            For Each inlineHint In typeHints
                If inlineHint.ReplacementTextChange IsNot Nothing Then
                    textChanges.Add(inlineHint.ReplacementTextChange.Value)
                End If
            Next

            Dim value = Await document.GetTextAsync().ConfigureAwait(False)
            Dim newText = value.WithChanges(textChanges).ToString()
            Dim expectedText = Await expectedDocument.GetTextAsync().ConfigureAwait(False)

            AssertEx.Equal(expectedText.ToString(), newText)
        End Function

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
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
using System.Collections.Immutable;
class A
{
    private static readonly ImmutableHashSet&lt;string?&gt; Hashes = {| ImmutableHashSet&lt;string?&gt;:|}[];
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
using System.Collections.Immutable;
class A
{
    private static readonly ImmutableHashSet&lt;string?&gt; Hashes = [];
}
                    </Document>
                </Project>
            </Workspace>

            Await VerifyTypeHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForTargetTypedNew() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Vector2
{
}

class A
{
    void Main()
    {
        Vector2 foo = new();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim options = New InlineTypeHintsOptions() With
            {
                .EnabledForTypes = True,
                .ForImplicitObjectCreation = True,
                .SuppressForTargetTypedNew = True
            }

            Await VerifyTypeHintsWithOptions(input, input, options)
        End Function

        <WpfFact>
        Public Async Function TestSuppressForTargetTypedCollectionExpression() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
class A
{
    void Main()
    {
        int[] arr = [1, 2, 3];
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim options = New InlineTypeHintsOptions() With
            {
                .EnabledForTypes = True,
                .ForCollectionExpressions = True,
                .SuppressForTargetTypedCollectionExpressions = True
            }

            Await VerifyTypeHintsWithOptions(input, input, options)
        End Function

        <WpfFact>
        Public Async Function TestShowForNonTargetTypedNew() As Task
            Dim input =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Vector2
{
}

class A
{
    void Main()
    {
        var foo = {|Vector2 :|}new();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
class Vector2
{
}

class A
{
    void Main()
    {
        var foo = new Vector2();
    }
}
                    </Document>
                </Project>
            </Workspace>

            Dim options = New InlineTypeHintsOptions() With
            {
                .EnabledForTypes = True,
                .ForImplicitObjectCreation = True,
                .SuppressForTargetTypedNew = True
            }

            Await VerifyTypeHintsWithOptions(input, output, options)
        End Function
    End Class
End Namespace
