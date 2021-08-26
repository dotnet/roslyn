' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpCompletionCommandHandlerTests_Await

        Private Shared Function GetTestClassDocument(containerHasAsyncModifier As Boolean, testExpression As String) As XElement
            Return _
<Document>
using System;
using System.Threading.Tasks;

class C
{
    public C Self => this;
    public Task Field = Task.CompletedTask;
    public Task Method() => Task.CompletedTask;
    public Task Property => Task.CompletedTask;
    public Task this[int i] => Task.CompletedTask;
    public Func&lt;Task&gt; Function() => () => Task.CompletedTask;
    public static Task operator +(C left, C right) => Task.CompletedTask;
    public static explicit operator Task(C c) => Task.CompletedTask;
}

static class Program
{
    static Task StaticField = Task.CompletedTask;
    static Task StaticProperty => Task.CompletedTask;
    static Task StaticMethod() => Task.CompletedTask;

    static<%= If(containerHasAsyncModifier, " async", "") %> Task Main(Task parameter)
    {
        var local = Task.CompletedTask;
        var c = new C();

        <%= testExpression %>

        Task LocalFunction() => Task.CompletedTask;
    }
}
</Document>
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_MethodDeclaration() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public static Task Main()
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_LocalFunctionDeclaration() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public static Task Main()
    {
        Task LocalFunc()
        {
            $$
        }
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public static Task Main()
    {
        async Task LocalFunc()
        {
            await
        }
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_AnonymousMethodExpression_Void() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

public class C
{
    public void F()
    {
        Action<int> a = static delegate(int i) { $$ };
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System;

public class C
{
    public void F()
    {
        Action<int> a = static async delegate(int i) { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_AnonymousMethodExpression_Task() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {
        Func<int, Task> a = static delegate(int i) { $$ };
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {
        Func<int, Task> a = static async delegate(int i) { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SimpleLambdaExpression_Void() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

public class C
{
    public void F()
    {
        Action<int> b = static a => { $$ };
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System;

public class C
{
    public void F()
    {
        Action<int> b = static async a => { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SimpleLambdaExpression_Task() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {
        Func<int, Task> b = static a => { $$ };
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {
        Func<int, Task> b = static async a => { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_ParenthesizedLambdaExpression_Void() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

public class C
{
    public void F()
    {
        Action<int> c = static (a) => { $$ };
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System;

public class C
{
    public void F()
    {
        Action<int> c = static async (a) => { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_ParenthesizedLambdaExpression_Task() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {
        Func<int, Task> c = static (a) => { $$ };
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {
        Func<int, Task> c = static async (a) => { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_ParenthesizedLambdaExpression_ExplicitType() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {
        Func<int, Task> c = static Task (a) => { $$ };
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {
        Func<int, Task> c = static async Task (a) => { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionDoesNotAddAsync_NotTask() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public static void Main()
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public static async void Main()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_Trivia() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public /*after public*/ static /*after static*/ Task /*after task*/ Main()
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public /*after public*/ static /*after static*/ async Task /*after task*/ Main()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpression() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        Task.CompletedTask.$$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        await Task.CompletedTask
    }
}
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        await Task.CompletedTask", "")
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpressionAndAsyncModifier() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public static Task Main()
    {
        Task.CompletedTask.$$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        await Task.CompletedTask
    }
}
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        await Task.CompletedTask", "")
            End Using
        End Function

        <WpfTheory>
        <InlineData(' static
            "StaticField.$$",
            "await StaticField")>
        <InlineData(
            "StaticProperty.$$",
            "await StaticProperty")>
        <InlineData(
            "StaticMethod().$$",
            "await StaticMethod()")>
        <InlineData(' parameters, locals and local function
            "parameter.$$",
            "await parameter")>
        <InlineData(
            "LocalFunction().$$",
            "await LocalFunction()")>
        <InlineData(' members
            "c.Field.$$",
            "await c.Field")>
        <InlineData(
            "c.Property.$$",
            "await c.Property")>
        <InlineData(
            "c.Method().$$",
            "await c.Method()")>
        <InlineData(
            "c.Self.Field.$$",
            "await c.Self.Field")>
        <InlineData(
            "c.Self.Property.$$",
            "await c.Self.Property")>
        <InlineData(
            "c.Self.Method().$$",
            "await c.Self.Method()")>
        <InlineData(
            "c.Function()().$$",
            "await c.Function()()")>
        <InlineData(' indexer, operator, conversion
            "c[0].$$",
            "await c[0]")>
        <InlineData(
            "c.Self[0].$$",
            "await c.Self[0]")>
        <InlineData(
            "(c + c).$$",
            "await (c + c)")>
        <InlineData(
            "((Task)c).$$",
            "await ((Task)c)")>
        <InlineData(
            "(c as Task).$$",
            "await (c as Task)")>
        <InlineData(' parenthesized
            "(parameter).$$",
            "await (parameter)")>
        <InlineData(
            "((parameter)).$$",
            "await ((parameter))")>
        <InlineData(
            "(true ? parameter : parameter).$$",
            "await (true ? parameter : parameter)")>
        <InlineData(
            "(null ?? Task.CompletedTask).$$",
            "await (null ?? Task.CompletedTask)")>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpressionForDifferentExpressions(expression As String, committed As String) As Task
            ' place await in front of expression
            Using state = TestStateFactory.CreateCSharpTestState(GetTestClassDocument(containerHasAsyncModifier:=True, expression))
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using

            ' place await in front of expression and make container async
            Using state = TestStateFactory.CreateCSharpTestState(GetTestClassDocument(containerHasAsyncModifier:=False, expression))
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using

            ' ConfigureAwait(false) starts here
            committed += ".ConfigureAwait(false)"
            ' place await in front of expression and append ConfigureAwait(false)
            Using state = TestStateFactory.CreateCSharpTestState(GetTestClassDocument(containerHasAsyncModifier:=True, expression))
                state.SendTypeChars("af")
                Await state.AssertSelectedCompletionItem(displayText:="awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using

            ' place await in front of expression, append ConfigureAwait(false) and make container async
            Using state = TestStateFactory.CreateCSharpTestState(GetTestClassDocument(containerHasAsyncModifier:=False, expression))
                state.SendTypeChars("af")
                Await state.AssertSelectedCompletionItem(displayText:="awaitf", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpressionAndAppendsConfigureAwait() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        Task.CompletedTask.$$
    }
}
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("await", "awaitf")
                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)
                state.SendTypeChars("f")
                Await state.AssertSelectedCompletionItem(displayText:="awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        await Task.CompletedTask.ConfigureAwait(false)
    }
}
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        await Task.CompletedTask.ConfigureAwait(false)", "")
            End Using
        End Function
    End Class
End Namespace
