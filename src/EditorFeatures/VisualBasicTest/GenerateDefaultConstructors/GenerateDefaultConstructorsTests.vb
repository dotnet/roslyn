' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateDefaultConstructors

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateDefaultConstructors
    Public Class GenerateDefaultConstructorsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New GenerateDefaultConstructorsCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException0() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception

    Public Sub New()
    End Sub

    Sub Main(args As String())
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Sub Main(args As String())
    End Sub
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub

    Sub Main(args As String())
    End Sub
End Class",
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.Serialization

Class Program
    Inherits Exception

    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        MyBase.New(info, context)
    End Sub

    Sub Main(args As String())
    End Sub
End Class",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.Serialization

Class Program
    Inherits Exception

    Public Sub New()
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub

    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        MyBase.New(info, context)
    End Sub

    Sub Main(args As String())
    End Sub
End Class",
index:=4)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem(539676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539676")>
        Public Async Function TestNotOfferedOnResolvedBaseClassName() As Task
            Await TestInRegularAndScript1Async(
"Class Base
End Class
Class Derived
    Inherits B[||]ase
End Class",
"Class Base
End Class
Class Derived
    Inherits Base

    Public Sub New()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedOnUnresolvedBaseClassName() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Derived
    Inherits [||]Base
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedOnInheritsStatementForStructures() As Task
            Await TestMissingInRegularAndScriptAsync(
"Structure Derived
    Inherits [||]Base
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedForIncorrectlyParentedInheritsStatement() As Task
            Await TestMissingInRegularAndScriptAsync(
"Inherits [||]Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructor() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Public Sub New()
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.Serialization

Class Program
    Inherits Exception
    Public Sub New()
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub

    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        MyBase.New(info, context)
    End Sub

    Sub Main(args As String())
    End Sub
End Class",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructorMissing1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception

    Public Sub New()
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructorMissing2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception

    Public Sub New()
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=2)
        End Function

        <WorkItem(540712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540712")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestEndOfToken() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception[||]
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception

    Public Sub New()
    End Sub

    Sub Main(args As String())
    End Sub
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestFormattingInGenerateDefaultConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exce[||]ption
    Public Sub New()
    End Sub
    Sub Main(args As String())
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception
    Public Sub New()
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Sub Main(args As String())
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestDefaultConstructorGeneration() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
    Public Sub New(y As Integer)
    End Sub
End Class

Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B
    Public Sub New(y As Integer)
    End Sub

    Friend Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/15005"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestFixAll() As Task
            Await TestInRegularAndScriptAsync(
<Text>
Class C
    Inherits [||]B

    Public Sub New(y As Boolean)
    End Sub
End Class

Class B
    Friend Sub New(x As Integer)
    End Sub

    Protected Sub New(x As String)
    End Sub

    Public Sub New(x As Boolean)
    End Sub

    Public Sub New(x As Long)
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>
Class C
    Inherits B

    Friend Sub New(x As Integer)
        MyBase.New(x)
    End Sub

    Protected Sub New(x As String)
        MyBase.New(x)
    End Sub

    Public Sub New(x As Long)
        MyBase.New(x)
    End Sub

    Public Sub New(y As Boolean)
    End Sub
End Class

Class B
    Friend Sub New(x As Integer)
    End Sub

    Protected Sub New(x As String)
    End Sub

    Public Sub New(x As Boolean)
    End Sub

    Public Sub New(x As Long)
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
index:=2)
            Throw New Exception() ' (Skip:="https://github.com/dotnet/roslyn/issues/15005")
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/15005"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestFixAll_WithTuples() As Task
            Await TestInRegularAndScriptAsync(
<Text>
Class C
    Inherits [||]B

    Public Sub New(y As (Boolean, Boolean))
    End Sub
End Class

Class B
    Friend Sub New(x As (Integer, Integer))
    End Sub

    Protected Sub New(x As (String, String))
    End Sub

    Public Sub New(x As (Boolean, Boolean))
    End Sub

    Public Sub New(x As (Long, Long))
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>
Class C
    Inherits B

    Friend Sub New(x As (Integer, Integer))
        MyBase.New(x)
    End Sub

    Protected Sub New(x As (String, String))
        MyBase.New(x)
    End Sub

    Public Sub New(x As (Long, Long))
        MyBase.New(x)
    End Sub

    Public Sub New(y As (Boolean, Boolean))
    End Sub
End Class

Class B
    Friend Sub New(x As (Integer, Integer))
    End Sub

    Protected Sub New(x As (String, String))
    End Sub

    Public Sub New(x As (Boolean, Boolean))
    End Sub

    Public Sub New(x As (Long, Long))
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInDerivedType_InvalidClassStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public [||]Class ;;Derived
    Inherits Base

End Class",
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class ;;Derived
    Inherits Base

    Public Sub New(a As Integer, Optional b As String = Nothing)
        MyBase.New(a, b)
    End Sub
End Class")
        End Function

        <WorkItem(6541, "https://github.com/dotnet/Roslyn/issues/6541")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInDerivedType1() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class Base
    Public Sub New(a As String)

    End Sub
End Class

Public Class [||]Derived
    Inherits Base

End Class",
"
Public Class Base
    Public Sub New(a As String)

    End Sub
End Class

Public Class Derived
    Inherits Base

    Public Sub New(a As String)
        MyBase.New(a)
    End Sub
End Class")
        End Function

        <WorkItem(6541, "https://github.com/dotnet/Roslyn/issues/6541")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInDerivedType2() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class [||]Derived
    Inherits Base

End Class",
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class Derived
    Inherits Base

    Public Sub New(a As Integer, Optional b As String = Nothing)
        MyBase.New(a, b)
    End Sub
End Class")
        End Function

        <WorkItem(19953, "https://github.com/dotnet/roslyn/issues/19953")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestNotOnEnum() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Enum [||]E
End Enum")
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromFriendConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
End Class

MustInherit Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromFriendConstructor2() As Task
            Await TestInRegularAndScriptAsync(
<Text>MustInherit Class C
    Inherits B[||]
End Class

MustInherit Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Friend Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromProtectedConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
End Class

MustInherit Class B
    Protected Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Protected Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromProtectedConstructor2() As Task
            Await TestInRegularAndScriptAsync(
<Text>MustInherit Class C
    Inherits B[||]
End Class

MustInherit Class B
    Protected Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Protected Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Protected Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromProtectedFriendConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
End Class

MustInherit Class B
    Protected Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Protected Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromProtectedFriendConstructor2() As Task
            Await TestInRegularAndScriptAsync(
<Text>MustInherit Class C
    Inherits B[||]
End Class

MustInherit Class B
    Protected Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Protected Friend Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Protected Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromPublicConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromPublicConstructor2() As Task
            Await TestInRegularAndScriptAsync(
<Text>MustInherit Class C
    Inherits B[||]
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function
    End Class
End Namespace
