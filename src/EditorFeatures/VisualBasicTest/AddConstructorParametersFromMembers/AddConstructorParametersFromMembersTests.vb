﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddConstructorParametersFromMembers
    Public Class AddConstructorParametersFromMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New AddConstructorParametersFromMembersCodeRefactoringProvider()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <WorkItem(530592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAdd1() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private s As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(i As Integer, s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(530592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAddOptional1() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private s As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(i As Integer, Optional s As String = Nothing)
        Me.i = i
        Me.s = s
    End Sub
End Class", index:=1, title:=String.Format(FeaturesResources.Add_optional_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(530592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAddToConstructorWithMostMatchingParameters1() As Task
            ' behavior change with 33603, now all constructors offered
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String)
        Me.New(i)
        Me.s = s
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String, b As Boolean)
        Me.New(i)
        Me.s = s
        Me.b = b
    End Sub
End Class", index:=1, title:=String.Format(FeaturesResources.Add_to_0, "Program(Integer, String)"))
        End Function

        <WorkItem(530592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAddOptionalToConstructorWithMostMatchingParameters1() As Task
            ' behavior change with 33603, now all constructors offered
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String)
        Me.New(i)
        Me.s = s
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String, Optional b As Boolean = Nothing)
        Me.New(i)
        Me.s = s
        Me.b = b
    End Sub
End Class", index:=3, title:=String.Format(FeaturesResources.Add_to_0, "Program(Integer, String)"))
        End Function

        <WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAddParamtersToConstructorBySelectOneMember() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private i As Integer
    [|Private k As Integer|]
    Private j As Integer
    Public Sub New(i As Integer, j As Integer)
        Me.i = i
        Me.j = j
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private k As Integer
    Private j As Integer
    Public Sub New(i As Integer, j As Integer, k As Integer)
        Me.i = i
        Me.j = j
        Me.k = k
    End Sub
End Class")
        End Function

        <WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestParametersAreStillRightIfMembersAreOutOfOrder() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private k As Integer
    Private j As Integer|]
    Public Sub New(i As Integer, j As Integer)
        Me.i = i
        Me.j = j
    End Sub
End Class",
"Class Program
    [|Private i As Integer
    Private k As Integer
    Private j As Integer|]
    Public Sub New(i As Integer, j As Integer, k As Integer)
        Me.i = i
        Me.j = j
        Me.k = k
    End Sub
End Class")
        End Function

        <WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNormalProperty() As Task
            Await TestInRegularAndScriptAsync(
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New(i As Integer)
    End Sub
End Class",
"
Class Program
    Private i As Integer
    Property Hello As Integer = 1
    Public Sub New(i As Integer, hello As Integer)
        Me.Hello = hello
    End Sub
End Class"
            )
        End Function

        <WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMissingIfFieldsAndPropertyAlreadyExists() As Task
            Await TestMissingAsync(
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New(i As Integer, hello As Integer)
    End Sub
End Class")
        End Function

        <WorkItem(33602, "https://github.com/dotnet/roslyn/issues/33602")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestConstructorWithNoParameters() As Task
            Await TestInRegularAndScriptAsync(
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New()
    End Sub
End Class",
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New(i As Integer, hello As Integer)
        Me.i = i
        Me.Hello = hello
    End Sub
End Class"
)
        End Function

        <WorkItem(33602, "https://github.com/dotnet/roslyn/issues/33602")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestDefaultConstructor() As Task
            Await TestMissingAsync(
"
Class Program
    [|Private i As Integer|]
End Class"
)
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestPartialSelection() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private i As Integer
    Private [|s|] As String
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(i As Integer, s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultiplePartialSelection() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private i As Integer
    Private [|s As String
    Private j|] As Integer
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Private j As Integer
    Public Sub New(i As Integer, s As String, j As Integer)
        Me.i = i
        Me.s = s
        Me.j = j
    End Sub
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultiplePartialSelection2() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private i As Integer
    Private [|s As String
    Private |]j As Integer
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Private j As Integer
    Public Sub New(i As Integer, s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class")
        End Function

        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultipleConstructors_FirstOfThree() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class",
"Class Program
    Private [|l|] As Integer

    Public Sub New(i As Integer, l As Integer)
        Me.i = i
        Me.l = l
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class", index:=0, title:=String.Format(FeaturesResources.Add_to_0, "Program(Integer)"))
        End Function

        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultipleConstructors_SecondOfThree() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class",
"Class Program
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer, l As Integer)
        Me.l = l
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class", index:=1, title:=String.Format(FeaturesResources.Add_to_0, "Program(Integer, Integer)"))
        End Function

        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultipleConstructors_ThirdOfThree() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class",
"Class Program
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer, l As Integer)
        Me.l = l
    End Sub
End Class", index:=2, title:=String.Format(FeaturesResources.Add_to_0, "Program(Integer, Integer, Integer)"))
        End Function

        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultipleConstructors_OneMustBeOptional() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|l|] As Integer

    ' index 0 as required
    ' index 2 as optional
    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    ' index 3 as optional
    Public Sub New(Optional j As Double = Nothing)
    End Sub

    ' index 1 as required
    ' index 4 as optional
    Public Sub New(i As Integer, j As Double)
    End Sub
End Class",
"Class Program
    Private [|l|] As Integer

    ' index 0 as required
    ' index 2 as optional
    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    ' index 3 as optional
    Public Sub New(Optional j As Double = Nothing)
    End Sub

    ' index 1 as required
    ' index 4 as optional
    Public Sub New(i As Integer, j As Double, l As Integer)
        Me.l = l
    End Sub
End Class", index:=1, title:=String.Format(FeaturesResources.Add_to_0, "Program(Integer, Double)"))
        End Function

        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultipleConstructors_OneMustBeOptional2() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|l|] As Integer

    ' index 0, and 2 as optional
    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    ' index 3 as optional
    Public Sub New(Optional j As Double = Nothing)
    End Sub

    ' index 1, and 4 as optional
    Public Sub New(i As Integer, j As Double)
    End Sub
End Class",
"Class Program
    Private [|l|] As Integer

    ' index 0, and 2 as optional
    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    ' index 3 as optional
    Public Sub New(Optional j As Double = Nothing, Optional l As Integer = Nothing)
        Me.l = l
    End Sub

    ' index 1, and 4 as optional
    Public Sub New(i As Integer, j As Double)
    End Sub
End Class", index:=3, title:=String.Format(FeaturesResources.Add_to_0, "Program(Double)"))
        End Function

        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultipleConstructors_AllMustBeOptional1() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|p|] As Integer

    Public Sub New(Optional i As Integer = Nothing)
        Me.i = i
    End Sub

    Public Sub New(j As Double, Optional k As Double = Nothing)
    End Sub

    Public Sub New(l As Integer, m As Integer, Optional n As Integer = Nothing)
    End Sub
End Class",
"Class Program
    Private p As Integer

    Public Sub New(Optional i As Integer = Nothing, Optional p As Integer = Nothing)
        Me.i = i
        Me.p = p
    End Sub

    Public Sub New(j As Double, Optional k As Double = Nothing)
    End Sub

    Public Sub New(l As Integer, m As Integer, Optional n As Integer = Nothing)
    End Sub
End Class", index:=0, title:=String.Format(FeaturesResources.Add_to_0, "Program(Integer)"))
        End Function

        <WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMultipleConstructors_AllMustBeOptional2() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|p|] As Integer

    Public Sub New(Optional i As Integer = Nothing)
        Me.i = i
    End Sub

    Public Sub New(j As Double, Optional k As Double = Nothing)
    End Sub

    Public Sub New(l As Integer, m As Integer, Optional n As Integer = Nothing)
    End Sub
End Class",
"Class Program
    Private p As Integer

    Public Sub New(Optional i As Integer = Nothing)
        Me.i = i
    End Sub

    Public Sub New(j As Double, Optional k As Double = Nothing)
    End Sub

    Public Sub New(l As Integer, m As Integer, Optional n As Integer = Nothing, Optional p As Integer = Nothing)
        Me.p = p
    End Sub
End Class", index:=2, title:=String.Format(FeaturesResources.Add_to_0, "Program(Integer, Integer, Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        public async function TestNonSelection1() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic

class Program
    dim i As Integer
  [||]  dim s As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelection2() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic

class Program
    dim i As Integer
    [||]dim s As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelection3() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim [||]s As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelection4() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s[||] As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelection5() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String [||]

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMultiVar1() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    [||]dim s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, s As String, t As String)
        Me.i = i
        Me.s = s
        Me.t = t
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMultiVar2() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String[||]

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, s As String, t As String)
        Me.i = i
        Me.s = s
        Me.t = t
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMultiVar3() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim [||]s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMultiVar4() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s[||], t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMultiVar5() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, [||]t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, t As String)
        Me.i = i
        Me.t = t
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMultiVar6() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t[||] As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class",
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, t As String)
        Me.i = i
        Me.t = t
    end sub
end class", title:=String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"))
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMissing1() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    [||]
    dim s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
}")
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMissing2() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
{
    dim i As Integer
    d[||]im s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
}")
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMissing3() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim[||] s, t As String

    public sub new(i As Integer)
    {
        Me.i = i
    end sub
}")
        End Function

        <WorkItem(23271, "https://github.com/dotnet/roslyn/issues/23271")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNonSelectionMissing4() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s,[||] t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
}")
        End Function
    End Class
End Namespace
