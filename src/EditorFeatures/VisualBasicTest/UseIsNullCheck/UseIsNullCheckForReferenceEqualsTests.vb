' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseIsNullCheck
    Partial Public Class UseIsNullCheckForReferenceEqualsTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(), New VisualBasicUseIsNullCheckForReferenceEqualsCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
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

        <WorkItem(23581, "https://github.com/dotnet/roslyn/issues/23581")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)>
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
