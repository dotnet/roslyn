' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ReplaceDocCommentTextWithTag

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ReplaceDocCommentTextWithTag
    <Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
    Public Class ReplaceDocCommentTextWithTagTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicReplaceDocCommentTextWithTagCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function TestStartOfKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword [||]Nothing.
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""Nothing""/>.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestStartOfKeywordCapitalized() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword Shared[||].
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""Shared""/>.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestEndOfKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword True[||].
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""True""/>.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestEndOfKeyword_NewLineFollowing() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword MustInherit[||]
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""MustInherit""/>
class C(Of TKey)
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76548")>
        Public Async Function TestEndOfKeyword_XmlCloseTagFollowing() As Task
            Await TestInRegularAndScriptAsync(
"
''' <summary>Testing keyword MustInherit[||]</summary>
class C(Of TKey)
end class",
"
''' <summary>Testing keyword <see langword=""MustInherit""/></summary>
class C(Of TKey)
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76548")>
        Public Async Function TestEndOfKeyword_XmlOpenTagPreceding() As Task
            Await TestInRegularAndScriptAsync(
"
''' <summary>[||]MustInherit is a thing</summary>
class C(Of TKey)
end class",
"
''' <summary>[||]<see langword=""MustInherit""/> is a thing</summary>
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestSelectedKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword [|Async|].
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""Async""/>.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestInsideKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword Aw[||]ait.
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""Await""/>.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestNotInsideKeywordIfNonEmptySpan() As Task
            Await TestMissingAsync(
"
''' TKey must implement the System.IDisposable int[|erf|]ace
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestStartOfFullyQualifiedTypeName_Start() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the [||]System.IDisposable interface.
class C(Of TKey)
end class",
"
''' TKey must implement the <see cref=""System.IDisposable""/> interface.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestStartOfFullyQualifiedTypeName_Mid1() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System[||].IDisposable interface.
class C(Of TKey)
end class",
"
''' TKey must implement the <see cref=""System.IDisposable""/> interface.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestStartOfFullyQualifiedTypeName_Mid2() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System.[||]IDisposable interface.
class C(Of TKey)
end class",
"
''' TKey must implement the <see cref=""System.IDisposable""/> interface.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestStartOfFullyQualifiedTypeName_End() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System.IDisposable[||] interface.
class C(Of TKey)
end class",
"
''' TKey must implement the <see cref=""System.IDisposable""/> interface.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestStartOfFullyQualifiedTypeName_CaseInsensitive() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the [||]system.idisposable interface.
class C(Of TKey)
end class",
"
''' TKey must implement the <see cref=""system.idisposable""/> interface.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestStartOfFullyQualifiedTypeName_Selected() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the [|System.IDisposable|] interface.
class C(Of TKey)
end class",
"
''' TKey must implement the <see cref=""System.IDisposable""/> interface.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestTypeParameterReference() As Task
            Await TestInRegularAndScriptAsync(
"
''' [||]TKey must implement the System.IDisposable interface.
class C(Of TKey)
end class",
"
''' <typeparamref name=""TKey""/> must implement the System.IDisposable interface.
class C(Of TKey)
end class")
        End Function

        <Fact>
        Public Async Function TestCanSeeInnerMethod() As Task
            Await TestInRegularAndScriptAsync(
"
''' Use WriteLine[||] as a Console.WriteLine replacement
class C
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class",
"
''' Use <see cref=""WriteLine""/> as a Console.WriteLine replacement
class C
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNotOnMispelledName() As Task
            Await TestMissingAsync(
"
''' Use WriteLine1[||] as a Console.WriteLine replacement
class C
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMethodTypeParameterSymbol() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    ''' value has type TKey[||] so we don't box primitives.
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class",
"
class C
    ''' value has type <typeparamref name=""TKey""/> so we don't box primitives.
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMethodTypeParameterSymbol_CaseInsensitive() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    ''' value has type TKey[||] so we don't box primitives.
    sub WriteLine(Of tkey)(value as TKey)
    end sub
end class",
"
class C
    ''' value has type <typeparamref name=""TKey""/> so we don't box primitives.
    sub WriteLine(Of tkey)(value as TKey)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMethodTypeParameterSymbol_EmptyBody() As Task
            Await TestInRegularAndScriptAsync(
"
interface I
    ''' value has type TKey[||] so we don't box primitives.
    sub WriteLine(Of TKey)(value as TKey)
end interface",
"
interface I
    ''' value has type <typeparamref name=""TKey""/> so we don't box primitives.
    sub WriteLine(Of TKey)(value as TKey)
end interface")
        End Function

        <Fact>
        Public Async Function TestMethodParameterSymbol() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    ''' value[||] has type TKey so we don't box primitives.
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class",
"
class C
    ''' <paramref name=""value""/> has type TKey so we don't box primitives.
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMethodParameterSymbol_CaseInsensitive() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    ''' value[||] has type TKey so we don't box primitives.
    sub WriteLine(Of TKey)(Value as TKey)
    end sub
end class",
"
class C
    ''' <paramref name=""value""/> has type TKey so we don't box primitives.
    sub WriteLine(Of TKey)(Value as TKey)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMethodParameterSymbol_EmptyBody() As Task
            Await TestInRegularAndScriptAsync(
"
interface I
    ''' value[||] has type TKey so we don't box primitives.
    sub WriteLine(Of TKey)(value as TKey)
end interface",
"
interface I
    ''' <paramref name=""value""/> has type TKey so we don't box primitives.
    sub WriteLine(Of TKey)(value as TKey)
end interface")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/22278")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/31208")>
        Public Async Function TestApplicableKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword interf[||]ace
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""interface""/>
class C(Of TKey)
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22278")>
        Public Async Function TestInXMLAttribute() As Task
            Await TestMissingAsync(
"
''' Testing keyword inside <see langword=""Noth[||]ing"">
class C
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22278")>
        Public Async Function TestInXMLAttribute2() As Task
            Await TestMissingAsync(
"
''' Testing keyword inside <see langword=""Not[||]hing""
class C
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38370")>
        Public Async Function TestMyBase() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword [||]MyBase.
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""MyBase""/>.
class C(Of TKey)
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38370")>
        Public Async Function TestMyClass() As Task
            Await TestInRegularAndScriptAsync(
"
''' Testing keyword [||]MyClass.
class C(Of TKey)
end class",
"
''' Testing keyword <see langword=""MyClass""/>.
class C(Of TKey)
end class")
        End Function
    End Class
End Namespace
