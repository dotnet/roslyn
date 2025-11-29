' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Xunit

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.AddImport.VisualBasicAddImportCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddImport

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
    Public NotInheritable Class AddImportCodeRefactoringTests

        <Fact>
        Public Async Function TestSimpleQualifiedTypeName() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As [||]System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function M() As Task
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InReturnType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As System.Threading.Tasks.[||]Task
        Return Nothing
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function M() As Task
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_GenericType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As [||]System.Collections.Generic.List(Of Integer)
        Return Nothing
    End Function
End Class",
"Imports System.Collections.Generic

Class C
    Function M() As List(Of Integer)
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InParameter() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M(t As [||]System.Threading.Tasks.Task)
    End Sub
End Class",
"Imports System.Threading.Tasks

Class C
    Sub M(t As Task)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InField() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Private _task As [||]System.Threading.Tasks.Task
End Class",
"Imports System.Threading.Tasks

Class C
    Private _task As Task
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InLocalVariable() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M()
        Dim task As [||]System.Threading.Tasks.Task = Nothing
    End Sub
End Class",
"Imports System.Threading.Tasks

Class C
    Sub M()
        Dim task As Task = Nothing
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InNewExpression() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M()
        Dim list = New [||]System.Collections.Generic.List(Of Integer)()
    End Sub
End Class",
"Imports System.Collections.Generic

Class C
    Sub M()
        Dim list = New List(Of Integer)()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InGetType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M()
        Dim t = GetType([||]System.Threading.Tasks.Task)
    End Sub
End Class",
"Imports System.Threading.Tasks

Class C
    Sub M()
        Dim t = GetType(Task)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InBaseType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Inherits [||]System.Exception
End Class",
"Imports System

Class C
    Inherits Exception
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InInterface() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Implements [||]System.IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class",
"Imports System

Class C
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InGenericConstraint() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C(Of T As [||]System.IDisposable)
End Class",
"Imports System

Class C(Of T As IDisposable)
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InCTypeExpression() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M(o As Object)
        Dim e = CType(o, [||]System.Exception)
    End Sub
End Class",
"Imports System

Class C
    Sub M(o As Object)
        Dim e = CType(o, Exception)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InTypeOfExpression() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M(o As Object)
        Dim b = TypeOf o Is [||]System.Exception
    End Sub
End Class",
"Imports System

Class C
    Sub M(o As Object)
        Dim b = TypeOf o Is Exception
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InTryCast() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M(o As Object)
        Dim e = TryCast(o, [||]System.Exception)
    End Sub
End Class",
"Imports System

Class C
    Sub M(o As Object)
        Dim e = TryCast(o, Exception)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_NestedNamespace() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As [||]System.Threading.Tasks.Task(Of Integer)
        Return Nothing
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function M() As Task(Of Integer)
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_InAttribute() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"<[||]System.Obsolete>
Class C
End Class",
"Imports System

<Obsolete>
Class C
End Class")
        End Function

        <Fact>
        Public Async Function TestQualifiedTypeName_NotOfferedWhenImportsAlreadyExists() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Imports System.Threading.Tasks

Class C
    Function M() As [||]System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestStaticMemberAccess() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M()
        [||]System.Console.WriteLine()
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Console.WriteLine()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestStaticMemberAccess_InMiddle() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M()
        System.[||]Console.WriteLine()
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Console.WriteLine()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestStaticMemberAccess_WithExistingImports() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Imports System

Class C
    Sub M()
        [||]System.Console.WriteLine()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGlobalQualifiedName() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As [||]Global.System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function M() As Task
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestAmbiguity_TypeWithSameNameInScope1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class Task
End Class

Class C
    Function M() As [||]System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class",
"Imports System.Threading.Tasks

Class Task
End Class

Class C
    Function M() As System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestAmbiguity_TypeWithSameNameInScope2() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Imports N

Namespace N
    Class Task
    End Class
End Namespace

Class C
    Function M() As [||]System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class

Class D
    Function M() As Task
        Return Nothing
    End Function
End Class",
"Imports System.Threading.Tasks
Imports N

Namespace N
    Class Task
    End Class
End Namespace

Class C
    Function M() As System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class

Class D
    Function M() As N.Task
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestSimplifyAllOccurrences_MultipleUsages() As Task
            Dim test = New VerifyVB.Test()
            test.TestCode =
"Class C
    Function M1() As [||]System.Threading.Tasks.Task
        Return Nothing
    End Function
    Function M2() As System.Threading.Tasks.Task
        Return Nothing
    End Function
    Function M3() As System.Threading.Tasks.Task(Of Integer)
        Return Nothing
    End Function
End Class"
            test.FixedCode =
"Imports System.Threading.Tasks

Class C
    Function M1() As Task
        Return Nothing
    End Function
    Function M2() As Task
        Return Nothing
    End Function
    Function M3() As Task(Of Integer)
        Return Nothing
    End Function
End Class"
            test.CodeActionIndex = 1
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestSimplifyAllOccurrences_MixedUsages() As Task
            Dim test = New VerifyVB.Test()
            test.TestCode =
"Class C
    Function M1() As [||]System.Threading.Tasks.Task
        Return Nothing
    End Function
    Sub M2(t As System.Threading.Tasks.Task)
    End Sub
    Private _field As System.Threading.Tasks.Task(Of String)
End Class"
            test.FixedCode =
"Imports System.Threading.Tasks

Class C
    Function M1() As Task
        Return Nothing
    End Function
    Sub M2(t As Task)
    End Sub
    Private _field As Task(Of String)
End Class"
            test.CodeActionIndex = 1
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestSimplifyAllOccurrences_WithOtherNamespaces() As Task
            Dim test = New VerifyVB.Test()
            test.TestCode =
"Class C
    Function M1() As [||]System.Threading.Tasks.Task
        Return Nothing
    End Function
    Function M2() As System.Collections.Generic.List(Of Integer)
        Return Nothing
    End Function
End Class"
            test.FixedCode =
"Imports System.Threading.Tasks

Class C
    Function M1() As Task
        Return Nothing
    End Function
    Function M2() As System.Collections.Generic.List(Of Integer)
        Return Nothing
    End Function
End Class"
            test.CodeActionIndex = 1
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestSimplifyOnlyCurrentOccurrence() As Task
            Dim test = New VerifyVB.Test()
            test.TestCode =
"Class C
    Function M1() As [||]System.Threading.Tasks.Task
        Return Nothing
    End Function
    Function M2() As System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class"
            test.FixedCode =
"Imports System.Threading.Tasks

Class C
    Function M1() As Task
        Return Nothing
    End Function
    Function M2() As System.Threading.Tasks.Task
        Return Nothing
    End Function
End Class"
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestNotOfferedOnBuiltInType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As [||]Integer
        Return 0
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNotOfferedOnGlobalAloneType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class TopLevel
End Class

Class C
    Function M() As [||]Global.TopLevel
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestGlobalQualifiedName_OnGlobal() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As [||]Global.System.DateTime
        Return Nothing
    End Function
End Class",
"Imports System

Class C
    Function M() As Date
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestGlobalQualifiedName_OnSystem() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As Global.[||]System.DateTime
        Return Nothing
    End Function
End Class",
"Imports System

Class C
    Function M() As Date
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestGlobalQualifiedName_OnTypeName() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As Global.System.[||]DateTime
        Return Nothing
    End Function
End Class",
"Imports System

Class C
    Function M() As Date
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNotOfferedOnMethod_GlobalQualified() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M()
        Global.System.Console.[||]WriteLine()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNotOfferedOnMethod_Qualified() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Sub M()
        System.Console.[||]WriteLine()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNotOfferedOnMethod_Simple() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Imports System

Class C
    Sub M()
        Console.[||]WriteLine()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNotOfferedOnSimpleConsole() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Imports System

Class C
    Sub M()
        [||]Console.WriteLine()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNotOfferedInImportsAliasDirective() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Imports X = [||]System.Console

Class C
    Sub M()
        X.WriteLine()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNestedType_OfferOnOuterType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As [||]NS1.NS2.T1.T2
        Return Nothing
    End Function
End Class",
"Imports NS1.NS2

Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As T1.T2
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNestedType_OfferOnNS1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As [||]NS1.NS2.T1.T2
        Return Nothing
    End Function
End Class",
"Imports NS1.NS2

Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As T1.T2
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNestedType_OfferOnNS2() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As NS1.[||]NS2.T1.T2
        Return Nothing
    End Function
End Class",
"Imports NS1.NS2

Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As T1.T2
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNestedType_OfferOnT1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As NS1.NS2.[||]T1.T2
        Return Nothing
    End Function
End Class",
"Imports NS1.NS2

Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As T1.T2
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNestedType_NotOfferedOnT2() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Namespace NS1.NS2
    Class T1
        Public Class T2
        End Class
    End Class
End Namespace

Class C
    Function M() As NS1.NS2.T1.[||]T2
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNestedGeneric_OuterName_SimplifiesBoth() As Task
            Await VerifyVB.VerifyRefactoringAsync(
"Class C
    Function M() As [||]System.Collections.Generic.List(Of System.Collections.Generic.List(Of Integer))
        Return Nothing
    End Function
End Class",
"Imports System.Collections.Generic

Class C
    Function M() As List(Of List(Of Integer))
        Return Nothing
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNestedGeneric_InnerName_SimplifiesOnlyInner() As Task
            Dim test = New VerifyVB.Test()
            test.TestCode =
"Class C
    Function M() As System.Collections.Generic.List(Of [||]System.Collections.Generic.List(Of Integer))
        Return Nothing
    End Function
End Class"
            test.FixedCode =
"Imports System.Collections.Generic

Class C
    Function M() As System.Collections.Generic.List(Of List(Of Integer))
        Return Nothing
    End Function
End Class"
            test.CodeActionIndex = 0
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestNestedGeneric_InnerName_SimplifyAll_SimplifiesBoth() As Task
            Dim test = New VerifyVB.Test()
            test.TestCode =
"Class C
    Function M() As System.Collections.Generic.List(Of [||]System.Collections.Generic.List(Of Integer))
        Return Nothing
    End Function
End Class"
            test.FixedCode =
"Imports System.Collections.Generic

Class C
    Function M() As List(Of List(Of Integer))
        Return Nothing
    End Function
End Class"
            test.CodeActionIndex = 1
            Await test.RunAsync()
        End Function
    End Class
End Namespace
