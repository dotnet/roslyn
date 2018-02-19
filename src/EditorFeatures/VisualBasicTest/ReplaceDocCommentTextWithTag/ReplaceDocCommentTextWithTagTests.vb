﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ReplaceDocCommentTextWithTag

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ReplaceDocCommentTextWithTag
    Public Class ReplaceDocCommentTextWithTagTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(Workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicReplaceDocCommentTextWithTagCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
        Public Async Function TestStartOfKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System.IDisposable [||]interface.
class C(Of TKey)
end class",
"
''' TKey must implement the System.IDisposable <see langword=""interface""/>.
class C(Of TKey)
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
        Public Async Function TestStartOfKeywordCapitalized() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System.IDisposable [||]Interface.
class C(Of TKey)
end class",
"
''' TKey must implement the System.IDisposable <see langword=""Interface""/>.
class C(Of TKey)
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
        Public Async Function TestEndOfKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System.IDisposable interface[||].
class C(Of TKey)
end class",
"
''' TKey must implement the System.IDisposable <see langword=""interface""/>.
class C(Of TKey)
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
        Public Async Function TestEndOfKeyword_NewLineFollowing() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System.IDisposable interface[||]
class C(Of TKey)
end class",
"
''' TKey must implement the System.IDisposable <see langword=""interface""/>
class C(Of TKey)
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
        Public Async Function TestSelectedKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System.IDisposable [|interface|].
class C(Of TKey)
end class",
"
''' TKey must implement the System.IDisposable <see langword=""interface""/>.
class C(Of TKey)
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
        Public Async Function TestInsideKeyword() As Task
            Await TestInRegularAndScriptAsync(
"
''' TKey must implement the System.IDisposable int[||]erface.
class C(Of TKey)
end class",
"
''' TKey must implement the System.IDisposable <see langword=""interface""/>.
class C(Of TKey)
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
        Public Async Function TestNotInsideKeywordIfNonEmptySpan() As Task
            Await TestMissingAsync(
"
''' TKey must implement the System.IDisposable int[|erf|]ace
class C(Of TKey)
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
        Public Async Function TestNotOnMispelledName() As Task
            Await TestMissingAsync(
"
''' Use WriteLine1[||] as a Console.WriteLine replacement
class C
    sub WriteLine(Of TKey)(value as TKey)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)>
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
    End Class
End Namespace
