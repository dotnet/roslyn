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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=CSharpFeaturesResources.Make_container_async)

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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=CSharpFeaturesResources.Make_container_async)

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
        Public Async Function AwaitCompletionAddsAsync_AnonymousMethodExpression() As Task
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=String.Empty)

                state.SendTab()
                Assert.Equal("
using System;

public class C
{
    public void F()
    {
        Action<int> a = static delegate(int i) { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SimpleLambdaExpression() As Task
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=String.Empty)

                state.SendTab()
                Assert.Equal("
using System;

public class C
{
    public void F()
    {
        Action<int> b = static a => { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_ParenthesizedLambdaExpression() As Task
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=String.Empty)

                state.SendTab()
                Assert.Equal("
using System;

public class C
{
    public void F()
    {
        Action<int> c = static (a) => { await };
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_NotTask() As Task
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=String.Empty)

                state.SendTab()
                Assert.Equal("
using System.Threading.Tasks;

public class C
{
    public static Task Main()
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
                Await state.AssertSelectedCompletionItem(displayText:="await", isHardSelected:=True, inlineDescription:=CSharpFeaturesResources.Make_container_async)

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
    End Class
End Namespace
