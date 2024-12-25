' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseIsNullCheck
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
    Partial Public Class UseIsNullCheckForReferenceEqualsTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(), New VisualBasicUseIsNullCheckForReferenceEqualsCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestIdentifierName() As Task
            Await TestInRegularAndScriptAsync(
"Imports System

class C
    sub M(s as string)
        if ([||]ReferenceEquals(s, Nothing))
            return
        end if
    end sub
end class",
"Imports System

class C
    sub M(s as string)
        if (s Is Nothing)
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestBuiltInType() As Task
            Await TestInRegularAndScriptAsync(
"Imports System

class C
    sub M(s as string)
        if (object.[||]ReferenceEquals(s, Nothing))
            return
        end if
    end sub
end class",
"Imports System

class C
    sub M(s as string)
        if (s Is Nothing)
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNamedType() As Task
            Await TestInRegularAndScriptAsync(
"Imports System

class C
    sub M(s as string)
        if (Object.[||]ReferenceEquals(s, Nothing))
            return
        end if
    end sub
end class",
"Imports System

class C
    sub M(s as string)
        if (s Is Nothing)
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestReversed() As Task
            Await TestInRegularAndScriptAsync(
"Imports System

class C
    sub M(s as string)
        if ([||]ReferenceEquals(Nothing, s))
            return
        end if
    end sub
end class",
"Imports System

class C
    sub M(s as string)
        if (s Is Nothing)
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNegated() As Task
            Await TestInRegularAndScriptAsync(
"Imports System

class C
    sub M(s as string)
        if (not [||]ReferenceEquals(Nothing, s))
            return
        end if
    end sub
end class",
"Imports System

class C
    sub M(s as string)
        if (s IsNot Nothing)
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestFixAll1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System

class C
    sub M(s1 as string, s2 as string)
        if ({|FixAllInDocument:ReferenceEquals|}(s1, Nothing) orelse
            ReferenceEquals(s2, Nothing))
            return
        end if
    end sub
end class",
"Imports System

class C
    sub M(s1 as string, s2 as string)
        if (s1 Is Nothing orelse
            s2 Is Nothing)
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestFixAll2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System

class C
    sub M(s1 as string, s2 as string)
        if (ReferenceEquals(s1, Nothing) orelse
            {|FixAllInDocument:ReferenceEquals|}(s2, Nothing))
            return
        end if
    end sub
end class",
"Imports System

class C
    sub M(s1 as string, s2 as string)
        if (s1 Is Nothing orelse
            s2 Is Nothing)
            return
        end if
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23581")>
        Public Async Function TestValueParameterTypeIsValueConstraintGeneric() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System

class C
    sub M(Of T As Structure)(v as T)
        if ([||]ReferenceEquals(Nothing, v))
            return
        end if
    end sub
end class")
        End Function
    End Class
End Namespace
