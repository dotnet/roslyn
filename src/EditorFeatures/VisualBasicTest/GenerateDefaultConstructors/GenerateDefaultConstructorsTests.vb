' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyCodeFix = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
        Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
        Microsoft.CodeAnalysis.VisualBasic.GenerateDefaultConstructors.VisualBasicGenerateDefaultConstructorsCodeFixProvider)

Imports VerifyRefactoring = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
        Microsoft.CodeAnalysis.GenerateDefaultConstructors.GenerateDefaultConstructorsCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateDefaultConstructors
    Public Class GenerateDefaultConstructorsTests

        Private Shared Async Function TestRefactoringAsync(source As String, fixedSource As String, Optional index As Integer = 0) As Task
            Await TestRefactoringOnlyAsync(source, fixedSource, index)
            await TestCodeFixMissingAsync(source)
        End Function

        Private Shared Async Function TestRefactoringOnlyAsync(source As String, fixedSource As String, Optional index As Integer = 0) As Task
            Await New VerifyRefactoring.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .CodeActionIndex = index
            }.RunAsync()
        End Function

        Private Shared Async Function TestCodeFixAsync(source As String, fixedSource As String, Optional index As Integer = 0) As Task
            Await New VerifyCodeFix.Test With
            {
                .TestCode = source.Replace("[||]", ""),
                .FixedCode = fixedSource,
                .CodeActionIndex = index
            }.RunAsync()

            Await TestRefactoringMissingAsync(source)
        End Function

        Private Shared Async Function TestRefactoringMissingAsync(source As String) As Task
            Await New VerifyRefactoring.Test With
            {
                .TestCode = source,
                .FixedCode = source
            }.RunAsync()
        End Function

        Private Shared Async Function TestCodeFixMissingAsync(source As String) As Task
            source = source.Replace("[||]", "")
            Await New VerifyCodeFix.Test With
            {
                .TestCode = source,
                .FixedCode = source
            }.RunAsync()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException0() As Task
            Await TestRefactoringAsync(
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
            Await TestRefactoringAsync(
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
            Await TestRefactoringAsync(
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
            Await TestRefactoringAsync(
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
            Await TestRefactoringAsync(
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
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539676")>
        Public Async Function TestNotOfferedOnResolvedBaseClassName() As Task
            Await TestRefactoringAsync(
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
            Await TestRefactoringMissingAsync(
"Class Derived
    Inherits [||]{|BC30002:Base|}
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedOnInheritsStatementForStructures() As Task
            Await TestRefactoringMissingAsync(
"Structure Derived
    {|BC30628:Inherits [||]Base|}
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedForIncorrectlyParentedInheritsStatement() As Task
            Await TestRefactoringMissingAsync(
"{|BC30683:Inherits [||]Goo|}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructor() As Task
            Await TestRefactoringAsync(
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
            Await TestRefactoringAsync(
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
            Await TestRefactoringAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540712")>
        Public Async Function TestEndOfToken() As Task
            Await TestRefactoringAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestFormattingInGenerateDefaultConstructor() As Task
            Await TestRefactoringAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")>
        Public Async Function TestDefaultConstructorGeneration() As Task
            Await TestRefactoringAsync(
<Text>Class C
    Inherits B[||]
    Public Sub New(y As Integer)
        mybase.new(y)
    End Sub
End Class

Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B
    Public Sub {|BC30269:New|}(y As Integer)
        mybase.new(y)
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
            Await TestRefactoringAsync(
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
            Await TestRefactoringAsync(
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
            Await TestCodeFixAsync(
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public [||]Class {|BC30203:|}{|BC30387:|}{|BC30037:;|}{|BC30037:;|}Derived
    Inherits Base

End Class",
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class {|BC30203:|}{|BC30037:;|}{|BC30037:;|}Derived
    Inherits Base

    Public Sub New(a As Integer, Optional b As String = Nothing)
        MyBase.New(a, b)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        <WorkItem("https://github.com/dotnet/Roslyn/issues/6541")>
        Public Async Function TestGenerateInDerivedType1() As Task
            Await TestCodeFixAsync(
"
Public Class Base
    Public Sub New(a As String)

    End Sub
End Class

Public Class [||]{|BC30387:Derived|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        <WorkItem("https://github.com/dotnet/Roslyn/issues/6541")>
        Public Async Function TestGenerateInDerivedType2() As Task
            Await TestCodeFixAsync(
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class [||]{|BC30387:Derived|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/19953")>
        Public Async Function TestNotOnEnum() As Task
            Await TestRefactoringMissingAsync(
"
Public Enum [||]E
    A
End Enum")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25238")>
        Public Async Function TestGenerateConstructorFromFriendConstructor() As Task
            Await TestCodeFixAsync(
<Text>Class {|BC30387:C|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25238")>
        Public Async Function TestGenerateConstructorFromFriendConstructor2() As Task
            Await TestCodeFixAsync(
<Text>MustInherit Class {|BC30387:C|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25238")>
        Public Async Function TestGenerateConstructorFromProtectedConstructor() As Task
            Await TestCodeFixAsync(
<Text>Class {|BC30387:C|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25238")>
        Public Async Function TestGenerateConstructorFromProtectedConstructor2() As Task
            Await TestCodeFixAsync(
<Text>MustInherit Class {|BC30387:C|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25238")>
        Public Async Function TestGenerateConstructorFromProtectedFriendConstructor() As Task
            Await TestCodeFixAsync(
<Text>Class {|BC30387:C|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25238")>
        Public Async Function TestGenerateConstructorFromProtectedFriendConstructor2() As Task
            Await TestCodeFixAsync(
<Text>MustInherit Class {|BC30387:C|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25238")>
        Public Async Function TestGenerateConstructorFromPublicConstructor() As Task
            Await TestCodeFixAsync(
<Text>Class {|BC30387:C|}
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/35208")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25238")>
        Public Async Function TestGenerateConstructorInAbstractClassFromPublicConstructor() As Task
            Await TestCodeFixAsync(
<Text>MustInherit Class {|BC30387:C|}
    Inherits B[||]
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Protected Sub New(x As Integer)
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
