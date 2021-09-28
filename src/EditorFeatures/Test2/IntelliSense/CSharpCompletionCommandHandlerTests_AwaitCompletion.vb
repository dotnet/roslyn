' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpCompletionCommandHandlerTests_Await
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
        <WorkItem(56006, "https://github.com/dotnet/roslyn/issues/56006")>
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

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
    End Class
End Namespace
