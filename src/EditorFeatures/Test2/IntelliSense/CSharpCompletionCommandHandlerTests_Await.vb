' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Task local = Task.CompletedTask;
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
        Public Async Function AwaitCompletionAddsAsync_ParenthesizedLambdaExpression_ExpressionBody() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {        
        Task.Run(() => $$);
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {        
        Task.Run(async () => await);
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionDoesNotAddAsync_AsyncParenthesizedLambdaExpression_ExpressionBody() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {        
        Task.Run(async () => $$);
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System;
using System.Threading.Tasks;

public class C
{
    public void F()
    {        
        Task.Run(async () => await);
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionDoesAddAsync_NotTask() As Task
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
            "local.$$",
            "await local")>
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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
                Await state.AssertSelectedCompletionItem(displayText:="awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using
        End Function

        <WpfTheory>
        <InlineData(
            "await Task.Run(async () => Task.CompletedTask.$$",
            "await Task.Run(async () => await Task.CompletedTask$$")>
        <InlineData(
            "await Task.Run(() => Task.CompletedTask.$$",
            "await Task.Run(async () => await Task.CompletedTask$$")>
        <InlineData(
            "await Task.Run(async () => Task.CompletedTask.aw$$",
            "await Task.Run(async () => await Task.CompletedTask$$")>
        <InlineData(
            "await Task.Run(() => Task.CompletedTask.aw$$",
            "await Task.Run(async () => await Task.CompletedTask$$")>
        <InlineData(
            "await Task.Run(async () => someTask.$$",
            "await Task.Run(async () => await someTask$$")>
        <InlineData(
            "await Task.Run(() => someTask.$$",
            "await Task.Run(async () => await someTask$$")>
        <InlineData(
            "await Task.Run(async () => someTask.$$);",
            "await Task.Run(async () => await someTask$$);")>
        <InlineData(
            "await Task.Run(() => someTask.$$);",
            "await Task.Run(async () => await someTask$$);")>
        <InlineData(
            "await Task.Run(async () => someTask.aw$$);",
            "await Task.Run(async () => await someTask$$);")>
        <InlineData(
            "await Task.Run(() => someTask.aw$$);",
            "await Task.Run(async () => await someTask$$);")>
        <InlineData(
            "await Task.Run(async () => {someTask.$$}",
            "await Task.Run(async () => {await someTask$$}")>
        <InlineData(
            "await Task.Run(() => {someTask.$$}",
            "await Task.Run(async () => {await someTask$$}")>
        <InlineData(
            "await Task.Run(async () => {someTask.$$});",
            "await Task.Run(async () => {await someTask$$});")>
        <InlineData(
            "await Task.Run(() => {someTask.$$});",
            "await Task.Run(async () => {await someTask$$});")>
        <InlineData(
            "await Task.Run(async () => someTask.   $$  );",
            "await Task.Run(async () => await someTask$$  );")>
        <InlineData(
            "await Task.Run(() => someTask.   $$  );",
            "await Task.Run(async () => await someTask$$  );")>
        <InlineData(
            "await Task.Run(async () => someTask  .   $$    );",
            "await Task.Run(async () => await someTask  $$    );")>
        <InlineData(
            "await Task.Run(() => someTask  .   $$    );",
            "await Task.Run(async () => await someTask  $$    );")>
        <InlineData(
            "await Task.Run(async () => someTask.$$.);",
            "await Task.Run(async () => await someTask$$.);")>
        <InlineData(
            "await Task.Run(() => someTask.$$.);",
            "await Task.Run(async () => await someTask$$.);")>
        <InlineData(
            "Task.Run(async () => await someTask).$$",
            "await Task.Run(async () => await someTask)$$")>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpressionInLambdas(expression As String, committed As String) As Task

            Dim document As XElement = <Document>
using System.Threading.Tasks;

static class Program
{
    static async Task Main()
    {
        var someTask = Task.CompletedTask;
        <%= expression %>
    }
}
</Document>
            ' Test await completion
            Using state = TestStateFactory.CreateCSharpTestState(document)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("await", "awaitf")
                state.SendTypeChars("a")
                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem("await")

                state.SendTab()
                Dim committedAwait = committed
                Dim committedCursorPosition = committedAwait.IndexOf("$$")
                committedAwait = committedAwait.Replace("$$", "")
                Assert.Equal($"
using System.Threading.Tasks;

static class Program
{{
    static async Task Main()
    {{
        var someTask = Task.CompletedTask;
        {committedAwait}
    }}
}}
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret($"        {committedAwait.Substring(0, committedCursorPosition)}", committedAwait.Substring(committedCursorPosition))
            End Using

            ' Test awaitf completion
            Using state = TestStateFactory.CreateCSharpTestState(document)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("await", "awaitf")
                state.SendTypeChars("a")
                state.SendTypeChars("f")
                Await state.AssertSelectedCompletionItem("awaitf")

                state.SendTab()
                Dim committedAwaitf = committed
                Dim committedCursorPosition = committedAwaitf.IndexOf("$$")
                committedAwaitf = committedAwaitf.Replace("$$", "")
                Dim committedAwaitfBeforeCursor = committedAwaitf.Substring(0, committedCursorPosition)
                Dim committedAwaitfAfterCursor = committedAwaitf.Substring(committedCursorPosition)
                Assert.Equal($"
using System.Threading.Tasks;

static class Program
{{
    static async Task Main()
    {{
        var someTask = Task.CompletedTask;
        {committedAwaitfBeforeCursor}.ConfigureAwait(false){committedAwaitfAfterCursor}
    }}
}}
", state.GetDocumentText())
                ' the expected cursor position is right after the inserted .ConfigureAwait(false)
                Await state.AssertLineTextAroundCaret($"        {committedAwaitfBeforeCursor}.ConfigureAwait(false)", committedAwaitfAfterCursor)
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionOffersAwaitAfterConfigureAwaitInvocation() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        Task.CompletedTask.ConfigureAwait(false).$$
    }
}
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("await")
                Await state.AssertCompletionItemsDoNotContainAny("awaitf")
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

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

        <WpfFact>
        Public Async Function DotAwaitCompletionOffersAwaitBeforeConfigureAwaitInvocation() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        Task.CompletedTask.$$ConfigureAwait(false);
    }
}
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("await", "awaitf")
                state.SendTypeChars("af")
                Await state.AssertSelectedCompletionItem(displayText:="awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        await Task.CompletedTask.ConfigureAwait(false)ConfigureAwait(false);
    }
}
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        await Task.CompletedTask.ConfigureAwait(false)", "ConfigureAwait(false);")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/56006")>
        Public Async Function SyntaxIsLikeLocalFunction() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C
{
    public void M()
    {
        $$ MyFunctionCall();
    }

    public void MyFunctionCall() {}
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()

                Assert.Equal("
public class C
{
    public async void M()
    {
        await MyFunctionCall();
    }

    public void MyFunctionCall() {}
}
", state.GetDocumentText())
            End Using
        End Function
        <WpfFact>
        Public Async Function DotAwaitCompletionInQueryInFirstFromClause() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Linq;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        var arrayTask2 = Task.FromResult(new int[0]);
        var z = from i1 in arrayTask2.$$
                select i1;
    }
}
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("await", "awaitf")
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System.Linq;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        var arrayTask2 = Task.FromResult(new int[0]);
        var z = from i1 in await arrayTask2
                select i1;
    }
}
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        var z = from i1 in await arrayTask2", "")
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionInQueryInFirstFromClauseConfigureAwait() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Linq;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        var arrayTask2 = Task.FromResult(new int[0]);
        var z = from i1 in arrayTask2.$$
                select i1;
    }
}
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("await", "awaitf")
                state.SendTypeChars("af")
                Await state.AssertSelectedCompletionItem(displayText:="awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System.Linq;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        var arrayTask2 = Task.FromResult(new int[0]);
        var z = from i1 in await arrayTask2.ConfigureAwait(false)
                select i1;
    }
}
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        var z = from i1 in await arrayTask2.ConfigureAwait(false)", "")
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionNullForgivingOperatorIsKept() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
#nullable enable

using System.Threading.Tasks;
public class C
{
    public Task? SomeTask => Task.CompletedTask;
    
    public C? Pro => this;
    public C? M() => this;
}

static class Program
{
    public static async Task Main(params string[] args)
    {
        var c =  args[1] == string.Empty ? new C() : null;
        c!.SomeTask!.$$;
    }
}
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("await", "awaitf")
                state.SendTypeChars("af")
                Await state.AssertSelectedCompletionItem(displayText:="awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
#nullable enable

using System.Threading.Tasks;
public class C
{
    public Task? SomeTask => Task.CompletedTask;
    
    public C? Pro => this;
    public C? M() => this;
}

static class Program
{
    public static async Task Main(params string[] args)
    {
        var c =  args[1] == string.Empty ? new C() : null;
        await c!.SomeTask!.ConfigureAwait(false);
    }
}
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        await c!.SomeTask!.ConfigureAwait(false)", ";")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/67952")>
        Public Async Function AwaitCompletion_AfterLocalFunction1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public void Test()
    {
        void Image_ImageOpened()
        {
        }

        awai$$ Goo();
    }
}
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

class C
{
    public async Task Test()
    {
        void Image_ImageOpened()
        {
        }

        await Goo();
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/67952")>
        Public Async Function AwaitCompletion_AfterLocalFunction2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public void Test()
    {
        void Image_ImageOpened()
        {
        }

        $$ Goo();
    }
}
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="await", displayTextSuffix:="")

                state.SendTypeChars("awai")
                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

class C
{
    public async Task Test()
    {
        void Image_ImageOpened()
        {
        }

        await Goo();
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        Public Async Function FixupReturnType_Method_Void() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

public class C
{
    public async Task Main()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        Public Async Function FixupReturnType_LocalFunction_Void() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        void Goo()
        {
            $$
        }
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        async Task Goo()
        {
            await
        }
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        Public Async Function FixupReturnType_ParenthesizedLambda_Void() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        var v = void () =>
        {
            $$
        }
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        var v = async Task () =>
        {
            await
        }
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        Public Async Function FixupReturnType_Method_NonVoid() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public int Main()
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

public class C
{
    public async Task<int> Main()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        Public Async Function FixupReturnType_LocalFunction_NonVoid() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        int Goo()
        {
            $$
        }
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        async Task<int> Goo()
        {
            await
        }
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        Public Async Function FixupReturnType_ParenthesizedLambda_NonVoid() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        var v = int () =>
        {
            $$
        }
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

public class C
{
    public void Main()
    {
        var v = async Task<int> () =>
        {
            await
        }
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        <InlineData("Task")>
        <InlineData("Task<int>")>
        <InlineData("ValueTask")>
        <InlineData("ValueTask<int>")>
        <InlineData("UnknownType")>
        Public Async Function NoFixupReturnType_Method(taskType As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    public Task Main()
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System.Threading.Tasks;

public class C
{
    public async Task Main()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        Public Async Function NoFixupReturnType_SimpleLambda() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
using System.Threading.Tasks;

public class C
{
    public Task Main()
    {
        Func<int, int> v = a =>
        {
            $$
        }
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System;
using System.Threading.Tasks;

public class C
{
    public Task Main()
    {
        Func<int, int> v = async a =>
        {
            await
        }
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55975")>
        Public Async Function NoFixupReturnType_ParenthesizedLambda() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
using System.Threading.Tasks;

public class C
{
    public Task Main()
    {
        Func<int, int> v = (a) =>
        {
            $$
        }
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                AssertEx.Equal("
using System;
using System.Threading.Tasks;

public class C
{
    public Task Main()
    {
        Func<int, int> v = async (a) =>
        {
            await
        }
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/78822")>
        Public Async Function AwaitCompletionAddsAsync_AsyncEnumerableMethodDeclaration1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static IAsyncEnumerable<string> Main()
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static async IAsyncEnumerable<string> Main()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/78822")>
        Public Async Function AwaitCompletionAddsAsync_AsyncEnumerableMethodDeclaration2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static async IAsyncEnumerable<string> Main()
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System.Collections.Generic;
using System.Threading.Tasks;

public class C
{
    public static async IAsyncEnumerable<string> Main()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionDoesNotChangeReturnType_ForEventHandlerMethod() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

public class C
{
    public event EventHandler MyEvent;

    public C()
    {
        MyEvent += OnMyEvent;
    }

    private void OnMyEvent(object sender, EventArgs e)
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System;

public class C
{
    public event EventHandler MyEvent;

    public C()
    {
        MyEvent += OnMyEvent;
    }

    private async void OnMyEvent(object sender, EventArgs e)
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionDoesNotChangeReturnType_ForEventHandlerWithMinusEquals() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

public class C
{
    public event EventHandler MyEvent;

    public void UnregisterHandler()
    {
        MyEvent -= OnMyEvent;
    }

    private void OnMyEvent(object sender, EventArgs e)
    {
        $$
    }
}
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
using System;

public class C
{
    public event EventHandler MyEvent;

    public void UnregisterHandler()
    {
        MyEvent -= OnMyEvent;
    }

    private async void OnMyEvent(object sender, EventArgs e)
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionChangesVoidToTask_ForNonEventHandlerMethod() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

public class C
{
    private void RegularMethod()
    {
        $$
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
    private async Task RegularMethod()
    {
        await
    }
}
", state.GetDocumentText())
            End Using
        End Function
    End Class
End Namespace
