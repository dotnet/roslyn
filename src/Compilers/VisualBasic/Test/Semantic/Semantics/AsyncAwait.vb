' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports System.Collections.ObjectModel
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AsyncAwait
        Inherits BasicTestBase

        <Fact()>
        Public Sub Basic()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Threading.Tasks

Module Program
    Function Test1() As Task(Of Integer)
        Return Nothing
    End Function

    Async Sub Test2()
        Await Test1()
        Dim x As Integer = Await Test1()
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact, WorkItem(744146, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/744146")>
        Public Sub DefaultAwaitExpressionInfo()
            Dim awaitInfo As AwaitExpressionInfo = Nothing

            Assert.Null(awaitInfo.GetAwaiterMethod)
            Assert.Null(awaitInfo.IsCompletedProperty)
            Assert.Null(awaitInfo.GetResultMethod)
        End Sub

        <Fact()>
        Public Sub AwaitableType01()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node1 As AwaitExpressionSyntax = CompilationUtils.FindBindingText(Of AwaitExpressionSyntax)(compilation, "a.vb", 1)
            Dim awaitInfo As AwaitExpressionInfo = semanticModel.GetAwaitExpressionInfo(node1)

            Assert.Equal("Function MyTask(Of System.Int32).GetAwaiter() As MyTaskAwaiter(Of System.Int32)", awaitInfo.GetAwaiterMethod.ToTestDisplayString())
            Assert.Equal("ReadOnly Property MyTaskAwaiter(Of System.Int32).IsCompleted As System.Boolean", awaitInfo.IsCompletedProperty.ToTestDisplayString())
            Assert.Equal("Function MyTaskAwaiter(Of System.Int32).GetResult() As System.Int32", awaitInfo.GetResultMethod.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub AwaitableType02()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter(Optional x As Integer = 0) As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function

End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node1 As AwaitExpressionSyntax = CompilationUtils.FindBindingText(Of AwaitExpressionSyntax)(compilation, "a.vb", 1)
            Dim awaitInfo As AwaitExpressionInfo = semanticModel.GetAwaitExpressionInfo(node1)

            Assert.Null(awaitInfo.GetAwaiterMethod)
            Assert.Null(awaitInfo.IsCompletedProperty)
            Assert.Null(awaitInfo.GetResultMethod)
        End Sub

        <Fact()>
        Public Sub AwaitableType03()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Sub GetAwaiter()
    End Sub
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36949: Expression is of type 'MyTask(Of Integer)', which is not awaitable.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType04()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As Object
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function

End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType05()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)

    ReadOnly Property GetAwaiter As MyTaskAwaiter(Of T)
        Get
            Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
        End Get
    End Property
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
        Dim y = (New MyTask(Of Integer)).GetAwaiter
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType06()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)

    Protected Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function

End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36949: Expression is of type 'MyTask(Of Integer)', which is not awaitable.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType07()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)

    Function GetAwaiter(x As Integer) As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function

End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36949: Expression is of type 'MyTask(Of Integer)', which is not awaitable.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType08()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36949: Expression is of type 'MyTask(Of Integer)', which is not awaitable.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType09()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)

    Shared Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return Nothing
    End Function

End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType10()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)

    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return Nothing
    End Function

End Class

Module Program

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30002: Type 'MyTaskAwaiter' is not defined. 
            ' error BC36949: Expression is of type 'MyTask(Of Integer)', which is not awaitable.
            AssertTheseDiagnostics(compilation,
<expected>
BC30002: Type 'MyTaskAwaiter' is not defined.
    Function GetAwaiter() As MyTaskAwaiter(Of T)
                             ~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType11()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program

    <Extension>
    Function GetAwaiter(Of T)(this As MyTask(Of T)) As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = this}
    End Function

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType12()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program

    <Extension>
    Function GetAwaiter(Of T)(this As MyTask(Of T), Optional x As Integer = 0) As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = this}
    End Function

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
            AssertTheseDiagnostics(compilation,
<expected>
BC36930: 'Await' requires that the type 'MyTask(Of Integer)' have a suitable GetAwaiter method.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType13()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)

    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function

End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure


Module Program

    Async Sub Test2()
        Dim o As Object = New MyTask(Of Integer)
        Dim x = Await o
        Await o
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.Custom))

            AssertTheseDiagnostics(compilation,
<expected>
BC42017: Late bound resolution; runtime errors could occur.
        Dim x = Await o
                ~~~~~~~
BC42017: Late bound resolution; runtime errors could occur.
        Await o
        ~~~~~~~
</expected>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.On))

            AssertTheseDiagnostics(compilation,
<expected>
BC30574: Option Strict On disallows late binding.
        Dim x = Await o
                ~~~~~~~
BC30574: Option Strict On disallows late binding.
        Await o
        ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType14()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Module Program

    Sub Test1()
    End Sub

    Async Sub Test2()
        Dim x As Integer = Await Test1()
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30491: Expression does not produce a value.
            AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        Dim x As Integer = Await Test1()
                                 ~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType15()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30456: 'IsCompleted' is not a member of 'MyTaskAwaiter(Of Integer)'.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node1 As AwaitExpressionSyntax = CompilationUtils.FindBindingText(Of AwaitExpressionSyntax)(compilation, "a.vb", 1)
            Dim awaitInfo As AwaitExpressionInfo = semanticModel.GetAwaitExpressionInfo(node1)

            Assert.Equal("Function MyTask(Of System.Int32).GetAwaiter() As MyTaskAwaiter(Of System.Int32)", awaitInfo.GetAwaiterMethod.ToTestDisplayString())
            Assert.Null(awaitInfo.IsCompletedProperty)
            Assert.Equal("Function MyTaskAwaiter(Of System.Int32).GetResult() As System.Int32", awaitInfo.GetResultMethod.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub AwaitableType16()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    Private ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' BC30390: 'MyTaskAwaiter(Of T).Private ReadOnly Property IsCompleted As Boolean' is not accessible in this context because it is 'Private'.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType17()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    Function IsCompleted() As Boolean
        Throw New NotImplementedException()
    End Function

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
        Dim y = (New MyTask(Of Integer)).GetAwaiter().IsCompleted
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType18()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted As Object
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType19()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted(x As Integer) As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30455: Argument not specified for parameter 'x' of 'Public ReadOnly Property IsCompleted(x As Integer) As Boolean'.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType20()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted(Optional x As Integer = 0) As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
        Dim y = (New MyTask(Of Integer)).GetAwaiter().IsCompleted
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType21()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    Shared ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
        Dim y = MyTaskAwaiter(Of Integer).IsCompleted
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' warning BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            ' error BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType22()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As DoesntExist
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30002: Type 'DoesntExist' is not defined.
            AssertTheseDiagnostics(compilation,
<expected>
BC30002: Type 'DoesntExist' is not defined.
    ReadOnly Property IsCompleted() As DoesntExist
                                       ~~~~~~~~~~~
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType23()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30456: 'GetResult' is not a member of 'MyTaskAwaiter(Of Integer)'.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node1 As AwaitExpressionSyntax = CompilationUtils.FindBindingText(Of AwaitExpressionSyntax)(compilation, "a.vb", 1)
            Dim awaitInfo As AwaitExpressionInfo = semanticModel.GetAwaitExpressionInfo(node1)

            Assert.Equal("Function MyTask(Of System.Int32).GetAwaiter() As MyTaskAwaiter(Of System.Int32)", awaitInfo.GetAwaiterMethod.ToTestDisplayString())
            Assert.Equal("ReadOnly Property MyTaskAwaiter(Of System.Int32).IsCompleted As System.Boolean", awaitInfo.IsCompletedProperty.ToTestDisplayString())
            Assert.Null(awaitInfo.GetResultMethod)
        End Sub

        <Fact()>
        Public Sub AwaitableType24()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Private Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30390: 'MyTaskAwaiter(Of T).Private Function GetResult() As T' is not accessible in this context because it is 'Private'.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType25()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult(x As Integer) As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30455: Argument not specified for parameter 'x' of 'Public Function GetResult(x As Integer) As T'.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType26()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult(Optional x As Integer = 0) As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType27()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Shared Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' warning BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            ' error BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType28()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    ReadOnly Property GetResult() As T
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
        Dim Y = (New MyTask(Of Integer)).GetAwaiter().GetResult
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType29()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub
End Structure

Module Program

    <Extension>
    Function GetResult(Of t)(X As MyTaskAwaiter(Of t)) As t
        Throw New NotImplementedException()
    End Function

    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
        Dim Y = (New MyTask(Of Integer)).GetAwaiter().GetResult
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType30()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As DoesntExist
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30002: Type 'DoesntExist' is not defined.
            AssertTheseDiagnostics(compilation,
<expected>
BC30002: Type 'DoesntExist' is not defined.
    Function GetResult() As DoesntExist
                            ~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType31()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Sub GetResult()
        Throw New NotImplementedException()
    End Sub
End Structure

Module Program
    Async Sub Test2()
        Dim x As Integer = Await New MyTask(Of Integer)
        Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30491: Expression does not produce a value.
            AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        Dim x As Integer = Await New MyTask(Of Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType32()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC37056: 'MyTaskAwaiter(Of Integer)' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'.
            AssertTheseDiagnostics(compilation,
<expected>
BC37056: 'MyTaskAwaiter(Of Integer)' does not implement 'INotifyCompletion'.
        Await New MyTask(Of Integer) 'BIND1:"Await New MyTask(Of Integer)"
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node1 As AwaitExpressionSyntax = CompilationUtils.FindBindingText(Of AwaitExpressionSyntax)(compilation, "a.vb", 1)
            Dim awaitInfo As AwaitExpressionInfo = semanticModel.GetAwaitExpressionInfo(node1)

            Assert.Equal("Function MyTask(Of System.Int32).GetAwaiter() As MyTaskAwaiter(Of System.Int32)", awaitInfo.GetAwaiterMethod.ToTestDisplayString())
            Assert.Equal("ReadOnly Property MyTaskAwaiter(Of System.Int32).IsCompleted As System.Boolean", awaitInfo.IsCompletedProperty.ToTestDisplayString())
            Assert.Equal("Function MyTaskAwaiter(Of System.Int32).GetResult() As System.Int32", awaitInfo.GetResultMethod.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub AwaitableType33()
            Dim source =
<compilation name="MissingINotifyCompletion">
    <file name="a.vb">
        <![CDATA[
Imports System

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {Net40.References.mscorlib, Net40.References.MicrosoftVisualBasic}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC31091: Import of type 'INotifyCompletion' from assembly or module 'MissingINotifyCompletion.exe' failed.
        Await New MyTask(Of Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitableType34()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Async Sub Test0()
        Await Task.Delay(1)
    End Sub

    Async Sub Test1()
        Await Test0()
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC37001: 'Test0' does not return a Task and cannot be awaited. Consider changing it to an Async Function.
        Await Test0()
              ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaitableType35()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

#Const defined = True

    <System.Diagnostics.Conditional("defined")>
    Sub GetResult()
        Throw New NotImplementedException()
    End Sub
End Structure

Module Program

    Async Sub Test1()
        Await New MyTask(Of Integer)()
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC37053: 'Await' requires that the return type 'MyTaskAwaiter(Of Integer)' of 'MyTask(Of Integer).GetAwaiter()' have suitable IsCompleted, OnCompleted and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion.
        Await New MyTask(Of Integer)()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AwaiterImplementsINotifyCompletion_Constraint()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Runtime.CompilerServices
Class Awaitable(Of T)
    Friend Function GetAwaiter() As T
        Return Nothing
    End Function
End Class
Interface IA
    ReadOnly Property IsCompleted As Boolean
    Function GetResult() As Object
End Interface
Interface IB
    Inherits IA, INotifyCompletion
End Interface
Class A
    Friend ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
    Friend Function GetResult() As Object
        Return Nothing
    End Function
End Class
Class B
    Inherits A
    Implements INotifyCompletion
    Private Sub OnCompleted(a As System.Action) Implements INotifyCompletion.OnCompleted
    End Sub
End Class
Module M
    Async Sub F(Of T1 As IA, T2 As {IA, INotifyCompletion}, T3 As IB, T4 As {T1, INotifyCompletion}, T5 As T3, T6 As A, T7 As {A, INotifyCompletion}, T8 As B, T9 As {T6, INotifyCompletion}, T10 As T8)()
        Await New Awaitable(Of T1)()
        Await New Awaitable(Of T2)()
        Await New Awaitable(Of T3)()
        Await New Awaitable(Of T4)()
        Await New Awaitable(Of T5)()
        Await New Awaitable(Of T6)()
        Await New Awaitable(Of T7)()
        Await New Awaitable(Of T8)()
        Await New Awaitable(Of T9)()
        Await New Awaitable(Of T10)()
    End Sub
End Module
    ]]>
    </file>
</compilation>
            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929})
            compilation.AssertTheseDiagnostics(
<expected>
BC37056: 'T1' does not implement 'INotifyCompletion'.
        Await New Awaitable(Of T1)()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37056: 'T6' does not implement 'INotifyCompletion'.
        Await New Awaitable(Of T6)()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ''' <summary>
        ''' Should call ICriticalNotifyCompletion.UnsafeOnCompleted
        ''' if the awaiter type implements ICriticalNotifyCompletion.
        ''' </summary>
        <Fact()>
        Public Sub AwaiterImplementsICriticalNotifyCompletion_Constraint()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Class Awaitable(Of T)
    Friend Function GetAwaiter() As T
        Return Nothing
    End Function
End Class
Class A
    Implements INotifyCompletion
    Private Sub OnCompleted(a As Action) Implements INotifyCompletion.OnCompleted
    End Sub
    Friend ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
    Friend Function GetResult() As Object
        Return Nothing
    End Function
End Class
Class B
    Inherits A
    Implements ICriticalNotifyCompletion
    Private Sub UnsafeOnCompleted(a As Action) Implements ICriticalNotifyCompletion.UnsafeOnCompleted
    End Sub
End Class
Module M
    Async Sub F(Of T1 As A, T2 As {A, ICriticalNotifyCompletion}, T3 As B, T4 As T1, T5 As T2, T6 As {T1, ICriticalNotifyCompletion})()
        Await New Awaitable(Of T1)()
        Await New Awaitable(Of T2)()
        Await New Awaitable(Of T3)()
        Await New Awaitable(Of T4)()
        Await New Awaitable(Of T5)()
        Await New Awaitable(Of T6)()
    End Sub
End Module
    ]]>
    </file>
</compilation>
            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929})
            Dim verifier = CompileAndVerify(compilation)
            Dim actualIL = verifier.VisualizeIL("M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6).MoveNext()")
            Dim calls = actualIL.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries).Where(Function(s) s.Contains("OnCompleted")).ToArray()
            Assert.Equal(calls.Length, 6)
            Assert.Equal(calls(0), <![CDATA[    IL_0058:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted(Of SM$T1, M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))(ByRef SM$T1, ByRef M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))"]]>.Value)
            Assert.Equal(calls(1), <![CDATA[    IL_00c7:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted(Of SM$T2, M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))(ByRef SM$T2, ByRef M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))"]]>.Value)
            Assert.Equal(calls(2), <![CDATA[    IL_0136:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted(Of SM$T3, M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))(ByRef SM$T3, ByRef M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))"]]>.Value)
            Assert.Equal(calls(3), <![CDATA[    IL_01a7:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted(Of SM$T4, M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))(ByRef SM$T4, ByRef M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))"]]>.Value)
            Assert.Equal(calls(4), <![CDATA[    IL_0219:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted(Of SM$T5, M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))(ByRef SM$T5, ByRef M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))"]]>.Value)
            Assert.Equal(calls(5), <![CDATA[    IL_028b:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted(Of SM$T6, M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))(ByRef SM$T6, ByRef M.VB$StateMachine_0_F(Of SM$T1, SM$T2, SM$T3, SM$T4, SM$T5, SM$T6))"]]>.Value)
        End Sub

        <Fact()>
        Public Sub Assignment()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Await (New MyTask(Of Integer))=1
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC30205: End of statement expected.
            AssertTheseDiagnostics(compilation,
<expected>
BC30205: End of statement expected.
        Await (New MyTask(Of Integer))=1
                                      ~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitNothing()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Module Program
    Async Sub Test2()
        Await Nothing
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36933: Cannot await Nothing. Consider awaiting 'Task.Yield()' instead.
            AssertTheseDiagnostics(compilation,
<expected>
BC36933: Cannot await Nothing. Consider awaiting 'Task.Yield()' instead.
        Await Nothing
        ~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub AwaitInQuery()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Linq.Enumerable

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As T
        Throw New NotImplementedException()
    End Function
End Structure

Module Program
    Async Sub Test2()
        Dim y = From x In {1} Where x <> Await New MyTask(Of Integer)
        Dim z = From x In {Await New MyTask(Of Integer)} Where x <> 0
        Await New MyTask(Of Integer)
    End Sub

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            ' error BC36929: 'Await' may only be used in a query expression within the first collection expression of the initial 'From' clause or within the collection expression of a 'Join' clause.
            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36929: 'Await' may only be used in a query expression within the first collection expression of the initial 'From' clause or within the collection expression of a 'Join' clause.
        Dim y = From x In {1} Where x <> Await New MyTask(Of Integer)
                                         ~~~~~
]]></expected>)

        End Sub

        <Fact()>
        Public Sub MisplacedAsyncModifier()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Public Module Program1
    Sub Main()
    End Sub

    Async Class Test1
    End Class

    Async Structure Test2
    End Structure

    Async Enum Test3
        X
    End Enum

    Async Interface Test4
    End Interface

    Class Test5
        Private Async Test6 As Object

        Async Property Test7 As Object

        Async Event Test8(x As Object)

        Async Sub Test9(ByRef x As Integer)
            Await Task.Delay(x)
            Dim Async Test10 As Object
        End Sub
    End Class

    Async Delegate Sub Test11()

    Async Declare Sub Test12 Lib "ddd" ()

    Interface I1
        Async Sub Test13()
    End Interface

    Enum E1
        Async Test14
    End Enum

    Structure S1
        Async Sub Test15()
            Await Task.Delay(1)
            Async Test16 As Object
        End Sub
    End Structure

    Async Iterator Function Test17() As Object
    End Function
End Module

Async Module Program2
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30461: Classes cannot be declared 'Async'.
    Async Class Test1
    ~~~~~
BC30395: 'Async' is not valid on a Structure declaration.
    Async Structure Test2
    ~~~~~
BC30396: 'Async' is not valid on an Enum declaration.
    Async Enum Test3
    ~~~~~
BC30397: 'Async' is not valid on an Interface declaration.
    Async Interface Test4
    ~~~~~
BC30235: 'Async' is not valid on a member variable declaration.
        Private Async Test6 As Object
                ~~~~~
BC30639: Properties cannot be declared 'Async'.
        Async Property Test7 As Object
        ~~~~~
BC30243: 'Async' is not valid on an event declaration.
        Async Event Test8(x As Object)
        ~~~~~
BC36926: Async methods cannot have ByRef parameters.
        Async Sub Test9(ByRef x As Integer)
                        ~~~~~~~~~~~~~~~~~~
BC30247: 'Async' is not valid on a local variable declaration.
            Dim Async Test10 As Object
                ~~~~~
BC42024: Unused local variable: 'Test10'.
            Dim Async Test10 As Object
                      ~~~~~~
BC30385: 'Async' is not valid on a Delegate declaration.
    Async Delegate Sub Test11()
    ~~~~~
BC30244: 'Async' is not valid on a Declare.
    Async Declare Sub Test12 Lib "ddd" ()
    ~~~~~
BC30270: 'Async' is not valid on an interface method declaration.
        Async Sub Test13()
        ~~~~~
BC30205: End of statement expected.
        Async Test14
              ~~~~~~
BC30247: 'Async' is not valid on a local variable declaration.
            Async Test16 As Object
            ~~~~~
BC42024: Unused local variable: 'Test16'.
            Async Test16 As Object
                  ~~~~~~
BC36936: 'Async' and 'Iterator' modifiers cannot be used together.
    Async Iterator Function Test17() As Object
    ~~~~~
BC31052: Modules cannot be declared 'Async'.
Async Module Program2
~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub ReferToReturnVariable()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Async Sub Test0()
        Dim x = Test0
        Await Task.Delay(1)
    End Sub

    Async Function Test1() As Task
        Dim x = Test1
        Await Task.Delay(1)
    End Function

    Async Function Test2() As Task(Of Integer)
        Dim x = Test2
        Await Task.Delay(1)
    End Function

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        Dim x = Test0
                ~~~~~
BC36946: The implicit return variable of an Iterator or Async method cannot be accessed.
        Dim x = Test1
                ~~~~~
BC36946: The implicit return variable of an Iterator or Async method cannot be accessed.
        Dim x = Test2
                ~~~~~
BC42105: Function 'Test2' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function
    ~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub ReturnStatements()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Async Sub Test0()
        Await Task.Delay(1)
        Dim x As Integer = 1
        If x = 3 Then
            Return Nothing ' 0
        Else
            Return
        End If
    End Sub

    Async Function Test1() As Task
        Await Task.Delay(1)
        Dim x As Integer = 1
        If x = 3 Then
            Return Nothing ' 1
        ElseIf x = 2 Then
            Return ' 1
        Else
            Return Task.Delay(1)
        End If
    End Function

    Async Function Test2() As Task
        Await Task.Delay(2)
    End Function

    Async Function Test3() As Task(Of Integer)
        Await Task.Delay(3)
        Return 3
    End Function

    Async Function Test4() As Task(Of Integer)
        Await Task.Delay(3)
        Return Test3()
    End Function

    Async Function Test5() As Object
        Await Task.Delay(3)
        Return Nothing ' 5
    End Function

    Async Function Test6() As Task(Of Integer)
        Await Task.Delay(6)
        Return New Guid()
    End Function

    Sub Main()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
            Return Nothing ' 0
            ~~~~~~~~~~~~~~
BC36952: 'Return' statements in this Async method cannot return a value since the return type of the function is 'Task'. Consider changing the function's return type to 'Task(Of T)'.
            Return Nothing ' 1
            ~~~~~~~~~~~~~~
BC36952: 'Return' statements in this Async method cannot return a value since the return type of the function is 'Task'. Consider changing the function's return type to 'Task(Of T)'.
            Return Task.Delay(1)
            ~~~~~~~~~~~~~~~~~~~~
BC37055: Since this is an async method, the return expression must be of type 'Integer' rather than 'Task(Of Integer)'.
        Return Test3()
               ~~~~~~~
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
    Async Function Test5() As Object
                              ~~~~~~
BC30311: Value of type 'Guid' cannot be converted to 'Integer'.
        Return New Guid()
               ~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub Lambdas()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Delegate Function D1() As Object
    Delegate Sub D2(ByRef x As Integer)

    Sub Main()
        Dim x00 = Async Sub()
                      Await Task.Delay(0)
                  End Sub

        Dim x01 = Async Iterator Function() As Object
                      Return Nothing
                  End Function ' 1

        Dim x02 = Async Function() As Object
                      Await Task.Delay(3)
                      Return Nothing ' 5
                  End Function '2

        Dim x03 As D1 = Async Function()
                            Await Task.Delay(3)
                            Return Nothing ' 5
                        End Function '3

        Dim x04 = Async Sub(ByRef x As Integer)
                      Await Task.Delay(0)
                  End Sub

        Dim x05 As D2 = Async Sub(x)
                            Await Task.Delay(0)
                        End Sub

        Dim x06 As D2 = Async Sub(ByRef x)
                            Await Task.Delay(0)
                        End Sub

        Dim x07 = Async Iterator Function() As Object
                      Await Task.Delay(0)
                      Return Nothing
                  End Function ' 4

        Dim x08 = Sub()
                      Await Task.Delay(0) ' x08
                  End Sub

        Dim x09 = Async Sub()
                      Await Task.Delay(0)
                      Dim x10 = Sub()
                                    Await Task.Delay(0) ' x10
                                End Sub
                  End Sub

        Dim x11 = Async Function()
                      Await Task.Delay(0)
                      Dim x As Integer = 0
                      If x = 0 Then
                          Return CByte(1)
                      Else
                          Return 1
                      End If
                  End Function ' 5

        Dim x12 As Func(Of Task(Of Integer)) = x11
        Dim x13 As Func(Of Task(Of Byte)) = x11

        Dim x14 = Async Function() Await New Task(Of Byte)(Function() 1)

        x12 = x14
        x13 = x14

        Dim x15 = Async Function()
                      Await Task.Delay(0)
                  End Function ' 6

        Dim x16 As Func(Of Task) = x15
        Dim x17 As Func(Of Integer) = x15

        Dim x18 = Async Function()
                      Await Task.Delay(0)
                      Return
                  End Function ' 7

        x16 = x18
        x17 = x18

        Dim x19 = Async Function()
                      Await Task.Delay(0)
                      Dim x As Integer = 0
                      If x = 0 Then
                          Return ' x19
                      Else
                          Return 1
                      End If
                  End Function ' 8

        Dim x20 As Func(Of Object) = Async Function()
                                         Await Task.Delay(0)
                                     End Function ' 9

        Dim x21 As Func(Of Object) = Async Function() Await New Task(Of Byte)(Function() 1)

        Dim x22 As Func(Of Object) = Async Function()
                                         Await Task.Delay(0)
                                         Return 1
                                     End Function ' 10

        'Dim x23 As Action = Async Function() ' Expected BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
        '                        Await Task.Delay(0)
        '                    End Function ' 11

        'Dim x24 As Action = Async Function() ' Expected BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
        '                        Await Task.Delay(0)
        '                        Return 1
        '                    End Function ' 12

        Dim x25 As Func(Of Task) = Async Function()
                                       Await Task.Delay(0)
                                   End Function ' 12

        Dim x26 As Func(Of Task) = Async Function() Await New Task(Of Byte)(Function() 1)

        Dim x27 As Func(Of Task) = Async Function()
                                       Await Task.Delay(0)
                                       Return 1
                                   End Function ' 13

        'Dim x28 = Async Overridable Sub()
        '              Await Task.Delay(0)
        '          End Sub

        'Dim x29 = Async Iterator Overridable Sub()
        '              Await Task.Delay(0)
        '          End Sub

        Dim x30 = Overridable Sub()
                  End Sub ' x30

        'Dim x31 = Async Main

    End Sub ' Main

    Async Sub Test()
        Await Task.Delay(0)
        Dim x04 = Sub(ByRef x As Integer)
                  End Sub
    End Sub

    Sub Test2()
        Dim x40 = Async Iterator Function() 1
        Dim x41 = Async Iterator Function()
                      Yield 1
                  End Function ' 14
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36936: 'Async' and 'Iterator' modifiers cannot be used together.
        Dim x01 = Async Iterator Function() As Object
                  ~~~~~
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
        Dim x02 = Async Function() As Object
                                      ~~~~~~
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
        Dim x03 As D1 = Async Function()
                        ~~~~~~~~~~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
        Dim x04 = Async Sub(ByRef x As Integer)
                            ~~~~~~~~~~~~~~~~~~
BC36670: Nested sub does not have a signature that is compatible with delegate 'Delegate Sub Program.D2(ByRef x As Integer)'.
        Dim x05 As D2 = Async Sub(x)
                        ~~~~~~~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
        Dim x06 As D2 = Async Sub(ByRef x)
                                  ~~~~~~~
BC36936: 'Async' and 'Iterator' modifiers cannot be used together.
        Dim x07 = Async Iterator Function() As Object
                  ~~~~~
BC37059: 'Await' can only be used within an Async lambda expression. Consider marking this lambda expression with the 'Async' modifier.
                      Await Task.Delay(0) ' x08
                      ~~~~~
BC30800: Method arguments must be enclosed in parentheses.
                      Await Task.Delay(0) ' x08
                            ~~~~~~~~~~~~~~
BC37059: 'Await' can only be used within an Async lambda expression. Consider marking this lambda expression with the 'Async' modifier.
                                    Await Task.Delay(0) ' x10
                                    ~~~~~
BC30800: Method arguments must be enclosed in parentheses.
                                    Await Task.Delay(0) ' x10
                                          ~~~~~~~~~~~~~~
BC30311: Value of type 'Function <generated method>() As Task(Of Integer)' cannot be converted to 'Func(Of Task(Of Byte))'.
        Dim x13 As Func(Of Task(Of Byte)) = x11
                                            ~~~
BC30311: Value of type 'Function <generated method>() As Task(Of Byte)' cannot be converted to 'Func(Of Task(Of Integer))'.
        x12 = x14
              ~~~
BC30311: Value of type 'Function <generated method>() As Task' cannot be converted to 'Func(Of Integer)'.
        Dim x17 As Func(Of Integer) = x15
                                      ~~~
BC30311: Value of type 'Function <generated method>() As Task' cannot be converted to 'Func(Of Integer)'.
        x17 = x18
              ~~~
BC30654: 'Return' statement in a Function, Get, or Operator must return a value.
                          Return ' x19
                          ~~~~~~
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
        Dim x20 As Func(Of Object) = Async Function()
                                     ~~~~~~~~~~~~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                                     End Function ' 9
                                     ~~~~~~~~~~~~
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
        Dim x21 As Func(Of Object) = Async Function() Await New Task(Of Byte)(Function() 1)
                                     ~~~~~~~~~~~~~~~~
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
        Dim x22 As Func(Of Object) = Async Function()
                                     ~~~~~~~~~~~~~~~~
BC30201: Expression expected.
        Dim x30 = Overridable Sub()
                  ~
BC30429: 'End Sub' must be preceded by a matching 'Sub'.
    End Sub ' Main
    ~~~~~~~
BC36936: 'Async' and 'Iterator' modifiers cannot be used together.
        Dim x40 = Async Iterator Function() 1
                  ~~~~~
BC36947: Single-line lambdas cannot have the 'Iterator' modifier. Use a multiline lambda instead.
        Dim x40 = Async Iterator Function() 1
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36936: 'Async' and 'Iterator' modifiers cannot be used together.
        Dim x41 = Async Iterator Function()
                  ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub LambdaRelaxation()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
        Dim x = CandidateMethod(Async Function()
                                    Await Task.Delay(1)
                                End Function)

    End Sub

    Function CandidateMethod(f As Func(Of Task)) As Object
        System.Console.WriteLine("CandidateMethod(f As Func(Of Task)) As Object")
        Return Nothing
    End Function

    Sub CandidateMethod(f As Func(Of Task(Of Integer)))
    End Sub

    Sub CandidateMethod(f As Func(Of Task(Of Double)))
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
CandidateMethod(f As Func(Of Task)) As Object
]]>).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub MissingTypes()
            Dim source =
<compilation name="MissingTaskTypes">
    <file name="a.vb">
        <![CDATA[
Imports System

Class Program

    Shared Sub Main()
        Dim x = Async Function()
                End Function

        Dim y = Async Function() 1

        Dim z = Async Function()
                    return 1
                End Function
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v20}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC31091: Import of type 'Task' from assembly or module 'MissingTaskTypes.exe' failed.
        Dim x = Async Function()
                ~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Task(Of )' from assembly or module 'MissingTaskTypes.exe' failed.
        Dim y = Async Function() 1
                ~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Task(Of )' from assembly or module 'MissingTaskTypes.exe' failed.
        Dim z = Async Function()
                ~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub IllegalAwait()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
        Await Task.Delay(1)
    End Sub

    Function Main2() As Integer
        Await Task.Delay(2)
        Return 1
    End Function

    Dim x = Await Task.Delay(3)
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC37058: 'Await' can only be used within an Async method. Consider marking this method with the 'Async' modifier and changing its return type to 'Task'.
        Await Task.Delay(1)
        ~~~~~
BC30800: Method arguments must be enclosed in parentheses.
        Await Task.Delay(1)
              ~~~~~~~~~~~~~
BC37057: 'Await' can only be used within an Async method. Consider marking this method with the 'Async' modifier and changing its return type to 'Task(Of Integer)'.
        Await Task.Delay(2)
        ~~~~~
BC30800: Method arguments must be enclosed in parentheses.
        Await Task.Delay(2)
              ~~~~~~~~~~~~~
BC36937: 'Await' can only be used when contained within a method or lambda expression marked with the 'Async' modifier.
    Dim x = Await Task.Delay(3)
            ~~~~~
BC30205: End of statement expected.
    Dim x = Await Task.Delay(3)
                  ~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WRNID_UnobservedAwaitableExpression()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Namespace Windows.Foundation
    public interface IAsyncAction
    End Interface

    public interface IAsyncActionWithProgress(Of T)
    End Interface

    public interface IAsyncOperation(Of T)
    End Interface

    public interface IAsyncOperationWithProgress(Of T, S)
    End Interface
End Namespace

Class MyTask(Of T)
    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T) With {.m_Task = Me}
    End Function
End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)

    ReadOnly Property IsCompleted() As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Sub GetResult()
        Throw New NotImplementedException()
    End Sub
End Structure

Module Program

    Sub Main()
    End Sub

    Interface AsyncAction
        Inherits Windows.Foundation.IAsyncAction
    End Interface

    Function M1() As AsyncAction
        Return Nothing
    End Function

    Interface AsyncActionWithProgress
        Inherits Windows.Foundation.IAsyncActionWithProgress(Of Integer)
    End Interface

    Function M2() As AsyncActionWithProgress
        Return Nothing
    End Function

    Interface AsyncOperation
        Inherits Windows.Foundation.IAsyncOperation(Of Integer)
    End Interface

    Function M3() As AsyncOperation
        Return Nothing
    End Function

    Interface AsyncOperationWithProgress
        Inherits Windows.Foundation.IAsyncOperationWithProgress(Of Integer, Integer)
    End Interface

    Function M4() As AsyncOperationWithProgress
        Return Nothing
    End Function

    Async Sub M5()
        Await Task.Delay(1)
    End Sub

    Async Function M6() As Task
        Await Task.Delay(1)
    End Function

    Async Function M7() As Task(Of Integer)
        Await Task.Delay(1)
        Return 1
    End Function

    Function M8() As Object
        Return Nothing
    End Function

    Function M9() As MyTask(Of Integer)
        Return Nothing
    End Function

    Function M10() As Task
        Return Nothing
    End Function

    Function M11() As Task(Of Integer)
        Return Nothing
    End Function

    Sub Test1()
        Call M1()
        M1() ' 1
        Dim x1 = M1()

        Call M2()
        M2() ' 1
        Dim x2 = M2()

        Call M3()
        M3() ' 1
        Dim x3 = M3()

        Call M4()
        M4() ' 1
        Dim x4 = M4()

        Call M5()
        M5()

        Call M6()
        M6() ' 1
        Dim x6 = M6()

        Call M7()
        M7() ' 1
        Dim x7 = M7()

        Call M8()
        M8()
        Dim x8 = M8()

        Call M9()
        M9()
        Dim x9 = M9()

        Call M10()
        M10()
        Dim x10 = M10()

        Call M11()
        M11()
        Dim x11 = M11()
    End Sub

    Async Sub Test2()
        Await Task.Delay(1)

        Call M1()
        M1() ' 2
        Dim x1 = M1()

        Call M2()
        M2() ' 2
        Dim x2 = M2()

        Call M3()
        M3() ' 2
        Dim x3 = M3()

        Call M4()
        M4() ' 2
        Dim x4 = M4()

        Call M5()
        M5()

        Call M6()
        M6() ' 2
        Dim x6 = M6()

        Call M7()
        M7() ' 2
        Dim x7 = M7()

        Call M8()
        M8()
        Dim x8 = M8()

        Call M9()
        M9() ' 2
        Dim x9 = M9()

        Call M10()
        M10() ' 2
        Dim x10 = M10()

        Call M11()
        M11() ' 2
        Dim x11 = M11()
    End Sub

    Async Function Test3() As Task
        Await Task.Delay(1)

        Call M1()
        M1() ' 3
        Dim x1 = M1()

        Call M2()
        M2() ' 3
        Dim x2 = M2()

        Call M3()
        M3() ' 3
        Dim x3 = M3()

        Call M4()
        M4() ' 3
        Dim x4 = M4()

        Call M5()
        M5()

        Call M6()
        M6() ' 3
        Dim x6 = M6()

        Call M7()
        M7() ' 3
        Dim x7 = M7()

        Call M8()
        M8()
        Dim x8 = M8()

        Call M9()
        M9() ' 3
        Dim x9 = M9()

        Call M10()
        M10() ' 3
        Dim x10 = M10()

        Call M11()
        M11() ' 3
        Dim x11 = M11()
    End Function

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M1() ' 1
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M2() ' 1
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M3() ' 1
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M4() ' 1
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M6() ' 1
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M7() ' 1
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M1() ' 2
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M2() ' 2
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M3() ' 2
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M4() ' 2
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M6() ' 2
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M7() ' 2
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M9() ' 2
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M10() ' 2
        ~~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M11() ' 2
        ~~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M1() ' 3
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M2() ' 3
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M3() ' 3
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M4() ' 3
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M6() ' 3
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M7() ' 3
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M9() ' 3
        ~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M10() ' 3
        ~~~~~
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        M11() ' 3
        ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_LoopControlMustNotAwait()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
    End Sub

    Dim array() As Integer

    Async Sub Test1()
        For x As Integer = Test3(Await Test2()) To 10
        Next

        For array(Test3(Await Test2())) = 0 To 10 ' 1
        Next

        For Each array(Test3(Await Test2())) In {1, 2, 3} ' 1
        Next

        For Each array(Test4(Async Function()
                                 Await Test2()
                             End Function)) In {1, 2, 3} ' 1
        Next
    End Sub

    Async Function Test2() As Task(Of Integer)
        For y As Integer = Test3(Await Test2()) To 10
        Next

        For array(Test3(Await Test2())) = 0 To 10 ' 2
        Next

        For Each array(Test3(Await Test2())) In {1, 2, 3} ' 2
        Next

        For Each array(Test4(Async Function()
                                 Await Test2()
                             End Function)) In {1, 2, 3} ' 2
        Next

        Return 2
    End Function

    Function Test3(x As Integer) As Integer
        Return x
    End Function

    Function Test4(x As Func(Of Task)) As Integer
        Return 0
    End Function
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC37060: Loop control variable cannot include an 'Await'.
        For array(Test3(Await Test2())) = 0 To 10 ' 1
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37060: Loop control variable cannot include an 'Await'.
        For Each array(Test3(Await Test2())) In {1, 2, 3} ' 1
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37060: Loop control variable cannot include an 'Await'.
        For array(Test3(Await Test2())) = 0 To 10 ' 2
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37060: Loop control variable cannot include an 'Await'.
        For Each array(Test3(Await Test2())) In {1, 2, 3} ' 2
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_BadStaticInitializerInResumable()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
    End Sub

    Async Sub Test1()
        Await Task.Delay(1)
        Static x As Integer
        x = 0
        Static y As Integer = 0
        Static a, b As Integer
        a = 0
        b = 1

        Static c As New Integer()
        Static d, e As New Integer()
    End Sub


    Async Function Test2() As Task(Of Integer)
        Await Task.Delay(1)
        Static u As Integer
        u = 0
        Static v As Integer = 0
        Static f, g As Integer
        f = 0
        g = 1

        Static h As New Integer()
        Static i, j As New Integer()

        Return 2
    End Function
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static x As Integer
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static y As Integer = 0
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static a, b As Integer
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static a, b As Integer
                  ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static c As New Integer()
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static d, e As New Integer()
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static d, e As New Integer()
                  ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static u As Integer
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static v As Integer = 0
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static f, g As Integer
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static f, g As Integer
                  ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static h As New Integer()
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static i, j As New Integer()
               ~
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static i, j As New Integer()
                  ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_RestrictedResumableType1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Delegate Sub D1(x As ArgIterator)
    Delegate Sub D2(ByRef x As ArgIterator)
    Delegate Sub D3(ByRef x As ArgIterator())
    Delegate Sub D4(x As ArgIterator())

    Sub Main()
        Dim x = Async Sub(a As ArgIterator)
                    Await Task.Delay(1)
                End Sub

        Dim y = Async Sub(ByRef a As ArgIterator)
                    Await Task.Delay(1)
                End Sub

        Dim z = Async Sub(ByRef a As ArgIterator())
                    Await Task.Delay(1)
                End Sub

        Dim u = Async Sub(a As ArgIterator())
                    Await Task.Delay(1)
                End Sub

        Dim x1 As D1 = Async Sub(a As ArgIterator)
                           Await Task.Delay(1)
                       End Sub

        Dim y1 As D2 = Async Sub(ByRef a As ArgIterator)
                           Await Task.Delay(1)
                       End Sub

        Dim z1 As D3 = Async Sub(ByRef a As ArgIterator())
                           Await Task.Delay(1)
                       End Sub

        Dim u1 As D4 = Async Sub(a As ArgIterator())
                           Await Task.Delay(1)
                       End Sub

        Dim x2 As D1 = Async Sub(a)
                           Await Task.Delay(1)
                       End Sub

        Dim y2 As D2 = Async Sub(ByRef a)
                           Await Task.Delay(1)
                       End Sub

        Dim z2 As D3 = Async Sub(ByRef a)
                           Await Task.Delay(1)
                       End Sub

        Dim u2 As D4 = Async Sub(a)
                           Await Task.Delay(1)
                       End Sub
    End Sub

    Async Sub Test1(x As ArgIterator)
        Await Task.Delay(1)
    End Sub

    Async Sub Test2(ByRef x As ArgIterator)
        Await Task.Delay(1)
    End Sub

    Async Sub Test3(x As ArgIterator())
        Await Task.Delay(1)
    End Sub

    Async Sub Test4(ByRef x As ArgIterator())
        Await Task.Delay(1)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Delegate Sub D2(ByRef x As ArgIterator)
                               ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Delegate Sub D3(ByRef x As ArgIterator())
                               ~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Delegate Sub D4(x As ArgIterator())
                         ~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x = Async Sub(a As ArgIterator)
                               ~~~~~~~~~~~
BC36932: 'ArgIterator' cannot be used as a parameter type for an Iterator or Async method.
        Dim x = Async Sub(a As ArgIterator)
                               ~~~~~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
        Dim y = Async Sub(ByRef a As ArgIterator)
                          ~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim y = Async Sub(ByRef a As ArgIterator)
                                     ~~~~~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
        Dim z = Async Sub(ByRef a As ArgIterator())
                          ~~~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim u = Async Sub(a As ArgIterator())
                               ~~~~~~~~~~~~~
BC36932: 'ArgIterator' cannot be used as a parameter type for an Iterator or Async method.
        Dim x1 As D1 = Async Sub(a As ArgIterator)
                                      ~~~~~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
        Dim y1 As D2 = Async Sub(ByRef a As ArgIterator)
                                 ~~~~~~~~~~~~~~~~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
        Dim z1 As D3 = Async Sub(ByRef a As ArgIterator())
                                 ~~~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim u1 As D4 = Async Sub(a As ArgIterator())
                                      ~~~~~~~~~~~~~
BC36932: 'ArgIterator' cannot be used as a parameter type for an Iterator or Async method.
        Dim x2 As D1 = Async Sub(a)
                                 ~
BC36926: Async methods cannot have ByRef parameters.
        Dim y2 As D2 = Async Sub(ByRef a)
                                 ~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
        Dim z2 As D3 = Async Sub(ByRef a)
                                 ~~~~~~~
BC36932: 'ArgIterator' cannot be used as a parameter type for an Iterator or Async method.
    Async Sub Test1(x As ArgIterator)
                         ~~~~~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
    Async Sub Test2(ByRef x As ArgIterator)
                    ~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Async Sub Test3(x As ArgIterator())
                         ~~~~~~~~~~~~~
BC36926: Async methods cannot have ByRef parameters.
    Async Sub Test4(ByRef x As ArgIterator())
                    ~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_ConstructorAsync_and_ERRID_PartialMethodsMustNotBeAsync1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
    End Sub

    Class Test
        Async Sub New()
        End Sub

        Shared Async Sub New()
        End Sub

        Partial Private Async Sub Part()
        End Sub

        Private Async Sub Part()
            Await Task.Delay(1)
        End Sub
    End Class
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36950: Constructor must not have the 'Async' modifier.
        Async Sub New()
        ~~~~~
BC36950: Constructor must not have the 'Async' modifier.
        Shared Async Sub New()
               ~~~~~
BC36935: 'Part' cannot be declared 'Partial' because it has the 'Async' modifier.
        Partial Private Async Sub Part()
                                  ~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_ResumablesCannotContainOnError()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
    End Sub

    Async Sub Test1()
        Await Task.Delay(1)
        On Error GoTo 0 ' 1
        Resume Next ' 1
    End Sub

    Iterator Function Test2() As System.Collections.Generic.IEnumerable(Of Integer)
        On Error GoTo 0 ' 2
        Resume Next ' 2
    End Function
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36956: 'On Error' and 'Resume' cannot appear inside async or iterator methods.
        On Error GoTo 0 ' 1
        ~~~~~~~~~~~~~~~
BC36956: 'On Error' and 'Resume' cannot appear inside async or iterator methods.
        Resume Next ' 1
        ~~~~~~~~~~~
BC36956: 'On Error' and 'Resume' cannot appear inside async or iterator methods.
        On Error GoTo 0 ' 2
        ~~~~~~~~~~~~~~~
BC36956: 'On Error' and 'Resume' cannot appear inside async or iterator methods.
        Resume Next ' 2
        ~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_ResumableLambdaInExpressionTree()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Linq.Expressions

Module Program

    Sub Main()
        Dim x As Expression(Of Action) = Async Sub() Await Task.Delay(1)

        Dim y As Expression(Of Func(Of System.Collections.Generic.IEnumerable(Of Integer))) = Iterator Function()
                                                                                              End Function
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC37050: Lambdas with the 'Async' or 'Iterator' modifiers cannot be converted to expression trees.
        Dim x As Expression(Of Action) = Async Sub() Await Task.Delay(1)
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37050: Lambdas with the 'Async' or 'Iterator' modifiers cannot be converted to expression trees.
        Dim y As Expression(Of Func(Of System.Collections.Generic.IEnumerable(Of Integer))) = Iterator Function()
                                                                                              ~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_CannotLiftRestrictedTypeResumable1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
    End Sub

    Async Sub Test1()
        Await Task.Delay(1)

        Dim a1 As ArgIterator
        Dim b1 As ArgIterator = Nothing
        Dim c1 As ArgIterator, d1 As ArgIterator = Nothing
        Dim e1 As New ArgIterator(Nothing)
        Dim f1, g1 As New ArgIterator(Nothing)
        Dim h1 = New ArgIterator()

        a1=Nothing
        c1=Nothing
    End Sub

    Iterator Function Test2() As System.Collections.Generic.IEnumerable(Of Integer)
        Dim a2 As ArgIterator
        Dim b2 As ArgIterator = Nothing
        Dim c2 As ArgIterator, d2 As ArgIterator = Nothing
        Dim e2 As New ArgIterator(Nothing)
        Dim f2, g2 As New ArgIterator(Nothing)
        Dim h2 = New ArgIterator()

        a2=Nothing
        c2=Nothing
    End Function
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim a1 As ArgIterator
                  ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim b1 As ArgIterator = Nothing
                  ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim c1 As ArgIterator, d1 As ArgIterator = Nothing
                  ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim c1 As ArgIterator, d1 As ArgIterator = Nothing
                                     ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim e1 As New ArgIterator(Nothing)
                      ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim f1, g1 As New ArgIterator(Nothing)
                          ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim h1 = New ArgIterator()
               ~~~~~~~~~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim a2 As ArgIterator
                  ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim b2 As ArgIterator = Nothing
                  ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim c2 As ArgIterator, d2 As ArgIterator = Nothing
                  ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim c2 As ArgIterator, d2 As ArgIterator = Nothing
                                     ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim e2 As New ArgIterator(Nothing)
                      ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim f2, g2 As New ArgIterator(Nothing)
                          ~~~~~~~~~~~
BC37052: Variable of restricted type 'ArgIterator' cannot be declared in an Async or Iterator method.
        Dim h2 = New ArgIterator()
               ~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_BadAwaitInTryHandler()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
        Dim x = Async Sub()
                    Try
                        Await Test2() ' Try 1
                    Catch ex As Exception When Await Test2() > 1 ' Filter 1
                    Catch
                        Await Test2() ' Catch 1
                    Finally
                        Await Test2() ' Finally 1
                    End Try

                    SyncLock New Lock(Await Test2()) ' SyncLock 1
                        Await Test2() ' SyncLock 1
                    End SyncLock
                End Sub
    End Sub

    Async Sub Test1()
        Try
            Await Test2() ' Try 2
        Catch ex As Exception When Await Test2() > 1 ' Filter 2
        Catch
            Await Test2() ' Catch 2
        Finally
            Await Test2() ' Finally 2
        End Try

        SyncLock New Lock(Await Test2()) ' SyncLock 2
            Await Test2() ' SyncLock 2
        End SyncLock
    End Sub

    Async Function Test2() As Task(Of Integer)
        Await Task.Delay(1)
        Return 1
    End Function

    Class Lock
        Sub New(x As Integer)
        End Sub
    End Class
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
                    Catch ex As Exception When Await Test2() > 1 ' Filter 1
                                               ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
                        Await Test2() ' Catch 1
                        ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
                        Await Test2() ' Finally 1
                        ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
                    SyncLock New Lock(Await Test2()) ' SyncLock 1
                                      ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
                        Await Test2() ' SyncLock 1
                        ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
        Catch ex As Exception When Await Test2() > 1 ' Filter 2
                                   ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
            Await Test2() ' Catch 2
            ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
            Await Test2() ' Finally 2
            ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
        SyncLock New Lock(Await Test2()) ' SyncLock 2
                          ~~~~~~~~~~~~~
BC36943: 'Await' cannot be used inside a 'Catch' statement, a 'Finally' statement, or a 'SyncLock' statement.
            Await Test2() ' SyncLock 2
            ~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WRNID_AsyncLacksAwaits()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
        Dim x = Async Sub()
                End Sub
    End Sub

    Async Sub Test1()
    End Sub

    Async Function Test2() As Task(Of Integer)
        Return 1
    End Function
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
        Dim x = Async Sub()
                      ~~~
BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
    Async Sub Test1()
              ~~~~~
BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
    Async Function Test2() As Task(Of Integer)
                   ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WRNID_UnobservedAwaitableDelegate()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
        Dim x As Action

        x = AddressOf Test1
        x = AddressOf Test2
        x = AddressOf Test3
        x = AddressOf Test4
        x = AddressOf Test5
        x = New Action(AddressOf Test2)
        x = CType(AddressOf Test2, Action)
        x = DirectCast(AddressOf Test2, Action)
        x = TryCast(AddressOf Test2, Action)

        x = Async Sub()
                Await Task.Delay(1)
            End Sub

        x = Async Function()
                Await Task.Delay(1)
                Return 1
            End Function

        x = Async Function() As Task(Of Integer)
                Await Task.Delay(1)
                Return 1
            End Function

        x = Async Function()
                Await Task.Delay(1)
            End Function

        x = Async Function() As Task
                Await Task.Delay(1)
            End Function

        x = New Action(Async Function()
                           Await Task.Delay(1)
                       End Function)

        x = CType(Async Function()
                      Await Task.Delay(1)
                  End Function, Action)

        x = DirectCast(Async Function()
                           Await Task.Delay(1)
                       End Function, Action)

        x = TryCast(Async Function()
                        Await Task.Delay(1)
                    End Function, Action)

        x = Function() As Task(Of Integer)
                Return Nothing
            End Function

        x = Function() As Task
                Return Nothing
            End Function

        Dim y = Async Function()
                    Await Task.Delay(1)
                End Function

        x = y
    End Sub

    Async Sub Test1() Handles z.y
        Await Task.Delay(1)
    End Sub

    Async Function Test2() As Task(Of Integer) Handles z.y
        Await Task.Delay(1)
        Return 1
    End Function

    Async Function Test3() As Task Handles z.y
        Await Task.Delay(1)
    End Function

    WithEvents z As CWithEvents

    Class CWithEvents
        Public Event y As Action
    End Class

    Function Test4() As Task(Of Integer) Handles z.y
        Return Nothing
    End Function

    Function Test5() As Task Handles z.y
        Return Nothing
    End Function

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
        x = AddressOf Test2
            ~~~~~~~~~~~~~~~
BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
        x = AddressOf Test3
            ~~~~~~~~~~~~~~~
BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
        x = Async Function()
            ~~~~~~~~~~~~~~~~~
BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
        x = Async Function() As Task(Of Integer)
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
        x = Async Function()
            ~~~~~~~~~~~~~~~~~
BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
        x = Async Function() As Task
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
    Async Function Test2() As Task(Of Integer) Handles z.y
                                                       ~~~
BC42359: The Task returned from this Async Function will be dropped, and any exceptions in it ignored. Consider changing it to an Async Sub so its exceptions are propagated.
    Async Function Test3() As Task Handles z.y
                                           ~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_SecurityCriticalAsyncInClassOrStruct_1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

<Security.SecurityCritical()>
Module Program

    Sub Main()
    End Sub

    Async Sub Test1()
        Await Task.Delay(1)
    End Sub

    Iterator Function Test2() As Collections.Generic.IEnumerable(Of Integer)
    End Function

    Class C1
        Async Sub Test3()
            Await Task.Delay(1)
        End Sub

        Iterator Function Test4() As Collections.Generic.IEnumerable(Of Integer)
        End Function
    End Class

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC31005: Async and Iterator methods are not allowed in a [Class|Structure|Interface|Module] that has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
    Async Sub Test1()
              ~~~~~
BC31005: Async and Iterator methods are not allowed in a [Class|Structure|Interface|Module] that has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
    Iterator Function Test2() As Collections.Generic.IEnumerable(Of Integer)
                      ~~~~~
BC31005: Async and Iterator methods are not allowed in a [Class|Structure|Interface|Module] that has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
        Async Sub Test3()
                  ~~~~~
BC31005: Async and Iterator methods are not allowed in a [Class|Structure|Interface|Module] that has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
        Iterator Function Test4() As Collections.Generic.IEnumerable(Of Integer)
                          ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_SecurityCriticalAsyncInClassOrStruct_2()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
    End Sub

End Module

<Security.SecuritySafeCritical()>
Class C2
    Private Async Sub Test5()
        Await Task.Delay(1)
    End Sub

    Partial Private Async Sub Test5()
    End Sub

    Iterator Function Test6() As Collections.Generic.IEnumerable(Of Integer)
    End Function
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC31005: Async and Iterator methods are not allowed in a [Class|Structure|Interface|Module] that has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
    Private Async Sub Test5()
                      ~~~~~
BC36935: 'Test5' cannot be declared 'Partial' because it has the 'Async' modifier.
    Partial Private Async Sub Test5()
                              ~~~~~
BC31005: Async and Iterator methods are not allowed in a [Class|Structure|Interface|Module] that has the 'SecurityCritical' or 'SecuritySafeCritical' attribute.
    Iterator Function Test6() As Collections.Generic.IEnumerable(Of Integer)
                      ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_SecurityCriticalAsync()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program
    Sub Main()
    End Sub

    <Security.SecurityCritical()> ' 1
    Private Async Sub Test1()
        Await Task.Delay(1)
    End Sub

    Partial Private Async Sub Test1()
    End Sub

    <Security.SecuritySafeCritical()> ' 2
    Iterator Function Test2() As Collections.Generic.IEnumerable(Of Integer)
    End Function

    Private Async Sub Test3()
        Await Task.Delay(1)
    End Sub

    <Security.SecurityCritical()> ' 3
    Partial Private Sub Test3()
    End Sub

    <Security.SecurityCritical()> ' 4
    Partial Private Async Sub Test4()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC31006: Security attribute 'SecurityCritical' cannot be applied to an Async or Iterator method.
    <Security.SecurityCritical()> ' 1
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36935: 'Test1' cannot be declared 'Partial' because it has the 'Async' modifier.
    Partial Private Async Sub Test1()
                              ~~~~~
BC31006: Security attribute 'SecuritySafeCritical' cannot be applied to an Async or Iterator method.
    <Security.SecuritySafeCritical()> ' 2
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31006: Security attribute 'SecurityCritical' cannot be applied to an Async or Iterator method.
    <Security.SecurityCritical()> ' 3
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36935: 'Test4' cannot be declared 'Partial' because it has the 'Async' modifier.
    Partial Private Async Sub Test4()
                              ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_DllImportOnResumableMethod()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Module Program
    Sub Main()
    End Sub

    <DllImport("Test1")>
    Private Async Sub Test1()
        Await Task.Delay(1)
    End Sub

    Partial Private Async Sub Test1()
    End Sub

    <DllImport("Test2")>
    Iterator Function Test2() As Collections.Generic.IEnumerable(Of Integer)
    End Function

    Private Async Sub Test3()
        Await Task.Delay(1)
    End Sub

    <DllImport("Test3")>
    Partial Private Sub Test3()
    End Sub

    <DllImport("Test4")>
    Partial Private Async Sub Test4()
    End Sub

    <DllImport("Test5")>
    Partial Private Sub Test5()
    End Sub

    Private Sub Test5()
        Return
    End Sub

    <DllImport("Test6")>
    Partial Private Sub Test6()
    End Sub

    Private Sub Test6()
    End Sub

    <DllImport("Test7")>
    Partial Private Sub Test7()
    End Sub

    Private Async Sub Test7()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC37051: 'System.Runtime.InteropServices.DllImportAttribute' cannot be applied to an Async or Iterator method.
    Private Async Sub Test1()
                      ~~~~~
BC36935: 'Test1' cannot be declared 'Partial' because it has the 'Async' modifier.
    Partial Private Async Sub Test1()
                              ~~~~~
BC37051: 'System.Runtime.InteropServices.DllImportAttribute' cannot be applied to an Async or Iterator method.
    Iterator Function Test2() As Collections.Generic.IEnumerable(Of Integer)
                      ~~~~~
BC37051: 'System.Runtime.InteropServices.DllImportAttribute' cannot be applied to an Async or Iterator method.
    Private Async Sub Test3()
                      ~~~~~
BC36935: 'Test4' cannot be declared 'Partial' because it has the 'Async' modifier.
    Partial Private Async Sub Test4()
                              ~~~~~
BC31522: 'System.Runtime.InteropServices.DllImportAttribute' cannot be applied to a Sub, Function, or Operator with a non-empty body.
    <DllImport("Test5")>
     ~~~~~~~~~
BC37051: 'System.Runtime.InteropServices.DllImportAttribute' cannot be applied to an Async or Iterator method.
    Private Async Sub Test7()
                      ~~~~~
BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
    Private Async Sub Test7()
                      ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_SynchronizedAsyncMethod()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
    End Sub

    <MethodImpl(MethodImplOptions.Synchronized)>
    Private Async Sub Test1()
        Await Task.Delay(1)
    End Sub

    Partial Private Async Sub Test1()
    End Sub

    <MethodImpl(MethodImplOptions.Synchronized)>
    Iterator Function Test2() As Collections.Generic.IEnumerable(Of Integer)
    End Function

    Private Async Sub Test3()
        Await Task.Delay(1)
    End Sub

    <MethodImpl(MethodImplOptions.Synchronized)>
    Partial Private Sub Test3()
    End Sub

    <MethodImpl(MethodImplOptions.Synchronized)>
    Partial Private Async Sub Test4()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC37054: 'MethodImplOptions.Synchronized' cannot be applied to an Async method.
    Private Async Sub Test1()
                      ~~~~~
BC36935: 'Test1' cannot be declared 'Partial' because it has the 'Async' modifier.
    Partial Private Async Sub Test1()
                              ~~~~~
BC37054: 'MethodImplOptions.Synchronized' cannot be applied to an Async method.
    Private Async Sub Test3()
                      ~~~~~
BC36935: 'Test4' cannot be declared 'Partial' because it has the 'Async' modifier.
    Partial Private Async Sub Test4()
                              ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ERRID_AsyncSubMain()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program
    Async Sub Main()
        'Await Task.Delay(1)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36934: The 'Main' method cannot be marked 'Async'.
    Async Sub Main()
              ~~~~
BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
    Async Sub Main()
              ~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WRNID_AsyncSubCouldBeFunction_1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
    End Sub
End Module

Module Program1
    Sub Main1()
        ' 1
        Task.Factory.StartNew(Async Sub() ' 1
                                  Await Task.Delay(1)
                              End Sub)

        Task.Factory.StartNew((Async Sub() ' 1
                                   Await Task.Delay(1)
                               End Sub))

        ' 2
        Task.Run(Async Sub() ' 2
                     Await Task.Delay(1)
                 End Sub)

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        Task.Factory.StartNew(Async Sub() ' 1
                              ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        Task.Factory.StartNew((Async Sub() ' 1
                               ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        Task.Run(Async Sub() ' 2
                 ~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WRNID_AsyncSubCouldBeFunction_2()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
    End Sub
End Module

Module Program2
    Async Function f(ByVal i As Integer) As Task
        Dim j As Integer = 2

        Await taskrun(Sub()
                          Dim x = 5
                          Console.WriteLine(x + i + j)
                      End Sub)

        ' 3
        Await taskrun(Async Sub() ' 3
                          Dim x = 6
                          Await Task.Delay(1)
                      End Sub)

        Await taskrun((Async Sub() ' 3
                           Dim x = 6
                           Await Task.Delay(1)
                       End Sub))

        Await taskrun(Async Function()
                          Dim x = 7
                          Await Task.Delay(1)
                      End Function)

        Await taskrun(Async Function()
                          Dim x = 8
                          Await Task.Delay(1)
                      End Function)
    End Function

    Async Function taskrun(ByVal f As Func(Of Task)) As Task
        Dim tt As Task(Of Task) = Task.Factory.StartNew(Of Task)(f)
        Dim t As Task = Await tt
        Await t
    End Function

    Async Function taskrun(ByVal f As Action) As task
        Dim t As Task = Task.Factory.StartNew(f)
        Await t
    End Function

End Module

    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        Await taskrun(Async Sub() ' 3
                      ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        Await taskrun((Async Sub() ' 3
                       ~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WRNID_AsyncSubCouldBeFunction_3()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
    End Sub
End Module

Module Program3

    Sub Main3()
        Dim t = Async Function()
                    Await Task.Yield()
                    Return "hello"
                End Function()

        TaskRun(Sub() Console.WriteLine("expected Action"))

        TaskRun(Sub()
                    Console.WriteLine("expected Action")
                End Sub)

        ' 4
        TaskRun(Async Sub() Await Task.Delay(1)) ' 4

        ' 5
        TaskRun(Async Sub() ' 5
                    Await Task.Delay(1)
                End Sub)

        TaskRun(Function() "expected Func<T>")
        TaskRun(Function()
                    Return "expected Func<T>"
                End Function)

        TaskRun(Async Function()
                    Console.WriteLine("expected Func<Task>")
                    Await Task.Yield()
                End Function)

        TaskRun(Async Function() d(Await t, "expected Func<Task<T>>"))

        TaskRun(Async Function()
                    Await Task.Yield()
                    Return "expected Func<Task<T>>"
                End Function)

    End Sub

    Function d(Of T)(dummy As Object, x As T) As T
        Return x
    End Function


    Sub TaskRun(f As Action)
        Console.WriteLine("TaskRun: Action")
        f()
        Console.WriteLine()
    End Sub

    Sub TaskRun(f As Func(Of Task))
        Console.WriteLine("TaskRun: Func<Task>")
        f().Wait()
        Console.WriteLine()
    End Sub

    Sub TaskRun(Of T)(f As Func(Of T))
        Console.WriteLine("TaskRun: Func<T>")
        Console.WriteLine(f())
        Console.WriteLine()
    End Sub

    Sub TaskRun(Of T)(f As Func(Of Task(Of T)))
        Console.WriteLine("TaskRun: Func<Task<T>>")
        Console.WriteLine(f().Result)
        Console.WriteLine()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        TaskRun(Async Sub() Await Task.Delay(1)) ' 4
                ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        TaskRun(Async Sub() ' 5
                ~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WRNID_AsyncSubCouldBeFunction_4()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
    End Sub
End Module

Module Program4

    Function f(x As action) As Integer
        Return 1
    End Function
    Function f(Of T)(x As Func(Of T)) As Integer
        Return 2
    End Function

    Sub h(x As Action)
    End Sub
    Sub h(x As Func(Of Task))
    End Sub

    Sub Main4()
        ' 6
        f(Async Sub() ' 6
              Await Task.Yield()
          End Sub)

        ' 7
        Console.WriteLine(f(Async Sub() ' 7
                                Await Task.Yield()
                            End Sub))

        ' 8
        h(Async Sub() ' 8
              Await Task.Yield()
          End Sub)

        Dim s = ""
        ' 9
        s.gg(Async Sub() ' 9
                 Await Task.Yield()
             End Sub)

    End Sub

    <Extension()> Sub gg(this As String, x As Action)
    End Sub

    <Extension()> Sub gg(this As String, x As Func(Of Task))
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        f(Async Sub() ' 6
          ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        Console.WriteLine(f(Async Sub() ' 7
                            ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        h(Async Sub() ' 8
          ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        s.gg(Async Sub() ' 9
             ~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WRNID_AsyncSubCouldBeFunction_5()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
    End Sub
End Module

Module Program5

    Async Function Test() As Task

        Await Task.Yield()

        ' 10
        CandidateMethod(Async Sub() ' 10
                            Await Task.Yield()
                        End Sub)
        ' 11
        CandidateMethod(Async Sub() ' 11
                            Await Task.Yield()
                        End Sub)

        ' 12
        CandidateMethod(Async Sub() ' 12
                            Console.WriteLine("fReturningTaskAsyncLambdaWithAwait")
                            Await Task.Yield()
                        End Sub)

        ' 13
        CandidateMethod(Async Sub(i As Object) ' 13
                            Console.WriteLine("fTakingIntReturningTask")
                            Await Task.Yield()
                        End Sub)
    End Function

    Sub CandidateMethod()
        Console.WriteLine("CandidateMethod()")
    End Sub
    Sub CandidateMethod(i As Integer)
        Console.WriteLine("CandidateMethod(integer) passed {0}", i)
    End Sub
    Sub CandidateMethod(d As Double)
        Console.WriteLine("CandidateMethod(double) passed {0}", d)
    End Sub
    Sub CandidateMethod(o As Object)
        Console.WriteLine("CandidateMethod(object) passed {0}", o)
    End Sub
    Sub CandidateMethod(d As System.Delegate)
        Console.WriteLine("CandidateMethod(System.Delegate) passed {0}", d)
    End Sub
    Sub CandidateMethod(a As action)
        a()
        Console.WriteLine("CandidateMethod(Action) passed {0}", a)
    End Sub
    Sub CandidateMethod(f As Func(Of Integer))
        Console.WriteLine("CandidateMethod(Func(Of integer)) func returns {0}", f())
    End Sub
    Sub CandidateMethod(f As Func(Of Double))
        Console.WriteLine("CandidateMethod(Func(Of double)) func returns {0}", f())
    End Sub
    Sub CandidateMethod(f As Func(Of Task))
        f().Wait()
        Console.WriteLine("CandidateMethod(Func(Of Task)) passed {0}", f)
    End Sub
    Sub CandidateMethod(f As Func(Of Task(Of Integer)))
        Console.WriteLine("CandidateMethod(Func(Of Task(Of integer))) task returns {0}", f().Result)
    End Sub
    Sub CandidateMethod(f As Func(Of Task(Of Double)))
        Console.WriteLine("CandidateMethod(Func(Of Task(Of (Of double))) task returns {0}", f().Result)
    End Sub
    Sub CandidateMethod(f As Func(Of Integer, Task))
        f(1).Wait()
        Console.WriteLine("CandidateMethod(Func(Of integer,Task)) passed {0}", f)
    End Sub
    Sub CandidateMethod(f As Func(Of Integer, Task(Of Integer)))
        Console.WriteLine("CandidateMethod(Func(Of integer,Task(Of integer))) task returns {0}", f(1).Result)
    End Sub
    Sub CandidateMethod(f As Func(Of Integer, Task(Of Double)))
        Console.WriteLine("CandidateMethod(Func(Of integer,Task(Of double))) task returns {0}", f(1).Result)
    End Sub
    Sub CandidateMethod(f As Func(Of Task(Of Object)))
        Console.WriteLine("CandidateMethod(f As Func(Of Task(Of object))) task returns {0}", f().Result)
    End Sub
    Sub CandidateMethod(f As Func(Of Task(Of String)))
        Console.WriteLine("CandidateMethod(f As Func(Of Task(Of sting))) task returns {0}", f().Result)
    End Sub
    Sub CandidateMethod(f As Func(Of Task(Of Integer?)))
        Console.WriteLine("CandidateMethod(f As Func(Of Task(Of integer?))) task returns {0}", f().Result)
    End Sub
    Sub CandidateMethod(f As Func(Of Task(Of Double?)))
        Console.WriteLine("CandidateMethod(f As Func(Of Task(Of double?))) task returns {0}", f().Result)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        CandidateMethod(Async Sub() ' 10
                        ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        CandidateMethod(Async Sub() ' 11
                        ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        CandidateMethod(Async Sub() ' 12
                        ~~~~~~~~~~~
BC42357: Some overloads here take an Async Function rather than an Async Sub. Consider either using an Async Function, or casting this Async Sub explicitly to the desired type.
        CandidateMethod(Async Sub(i As Object) ' 13
                        ~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact(), WorkItem(547087, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547087")>
        Public Sub Bug17912_1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Module1
    Sub Main(args As String())
    End Sub

    Async Function Test0() As Task
        Dim x as Await
        Dim y As [Await]
        Await Task.Delay(1)
    End Function

    Async Function await() As Task
        Await Task.Delay(1)
    End Function

    Async Sub Test1(await As Integer) 
        Await Task.Delay(1)
    End Sub

    Async Function Test2(x As Await) As Task
        Await Task.Delay(1)
    End Function

    Async Function Test3() As Await
        Await Task.Delay(1)
    End Function
End Module

Class Await
End Class
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC42024: Unused local variable: 'x'.
        Dim x as Await
            ~
BC30183: Keyword is not valid as an identifier.
        Dim x as Await
                 ~~~~~
BC42024: Unused local variable: 'y'.
        Dim y As [Await]
            ~
BC30183: Keyword is not valid as an identifier.
    Async Function await() As Task
                   ~~~~~
BC30183: Keyword is not valid as an identifier.
    Async Sub Test1(await As Integer) 
                    ~~~~~
BC30183: Keyword is not valid as an identifier.
    Async Function Test2(x As Await) As Task
                              ~~~~~
BC30183: Keyword is not valid as an identifier.
    Async Function Test3() As Await
                              ~~~~~
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
    Async Function Test3() As Await
                              ~~~~~
BC42105: Function 'Test3' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function
    ~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact(), WorkItem(547087, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547087")>
        Public Sub Bug17912_2()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Class Await
End Class

Module Module2
    Sub Main(args As String())
    End Sub

    Async Function [await]() As Task
        Await Task.Delay(1)
    End Function

    Async Sub Test1([await] As Integer)
        Await Task.Delay(1)
    End Sub

    Async Function Test2(x As [Await]) As Task
        Await Task.Delay(1)
    End Function

    Async Function Test3() As [Await]
        Await Task.Delay(1)
    End Function
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
    Async Function Test3() As [Await]
                              ~~~~~~~
BC42105: Function 'Test3' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function
    ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(547087, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547087")>
        Public Sub Bug17912_3()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Module1
    Sub Main(args As String())
        Dim test0 = Async Function() As Task
                        Dim x As Await
                        Dim y As [Await]
                        Await Task.Delay(1)
                    End Function

        Dim test1 = Async Sub (await As Integer) 
                        Await Task.Delay(1)
                    End Sub

        Dim test2 = Async Function (x As Await) As Task
                        Await Task.Delay(1)
                    End Function

        Dim test3 = Async Function() As Await
                        Await Task.Delay(1)
                    End Function ' 1

        Dim test4 = Async Sub (await As Integer) Await Task.Delay(1)

        Dim test5 = Async Sub (await As Integer) Await GetTask()

        Dim test11 = Async Sub([await] As Integer)
                         Await Task.Delay(1)
                     End Sub

        Dim test21 = Async Function(x As [Await]) As Task
                         Await Task.Delay(1)
                     End Function

        Dim test31 = Async Function() As Task(Of [Await])
                         Await Task.Delay(1)
                     End Function ' 2

        Dim test41 = Async Sub([await] As Integer) Await Task.Delay(1)

        Dim test51 = Async Sub([await] As Integer) Await GetTask()

    End Sub


    Function GetTask() As Task(Of Integer)
        Return Nothing
    End Function
End Module

Class Await
End Class
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42024: Unused local variable: 'x'.
                        Dim x As Await
                            ~
BC30183: Keyword is not valid as an identifier.
                        Dim x As Await
                                 ~~~~~
BC42024: Unused local variable: 'y'.
                        Dim y As [Await]
                            ~
BC30183: Keyword is not valid as an identifier.
        Dim test1 = Async Sub (await As Integer) 
                               ~~~~~
BC30183: Keyword is not valid as an identifier.
        Dim test2 = Async Function (x As Await) As Task
                                         ~~~~~
BC30183: Keyword is not valid as an identifier.
        Dim test3 = Async Function() As Await
                                        ~~~~~
BC36945: The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).
        Dim test3 = Async Function() As Await
                                        ~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                    End Function ' 1
                    ~~~~~~~~~~~~
BC30183: Keyword is not valid as an identifier.
        Dim test4 = Async Sub (await As Integer) Await Task.Delay(1)
                               ~~~~~
BC30183: Keyword is not valid as an identifier.
        Dim test5 = Async Sub (await As Integer) Await GetTask()
                               ~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                     End Function ' 2
                     ~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact(), WorkItem(568948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568948")>
        Public Sub Bug568948()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Public Module Module1
        Public Sub Main()
  goo()
 End Sub
        Public Async Sub Goo()
                    Dim AwaitableLambda1 = Async Function()
                                               Await Task.Yield
                                               Return New List(Of Test) From {New Test With {.Async = "test2", .Await = 2}, New Test With {.Async = "test3", .Await = 3}}
                                           End Function
                    Dim Query = From x2 In Await AwaitableLambda1() Select New With {.async = x2, .b = Function(Await)
                                                                                                           Return Await.Await + 1
                                                                                                       End Function.Invoke(x2)}
 
 
'ANOTHER EXAMPLE FAILING WITH SAME ISSUE....
'
'                    Dim AwaitableLambda1 = Async Function()
'                                               Await Task.Yield
'                                               Return New List(Of Test) From {New Test With {.Async = "test2", .Await = 2},                                                                                                                            New Test With {.Async = "test3", .Await = 3}}
'                                           End Function
'                    Dim Query = From x2 In Await AwaitableLambda1() Select New With {.async = x2, .b = Function(Await)
'                                                                                                           Return Await.Await + 1
'                                                                                                       End Function.Invoke(.async)}            
End Sub
    End Module

Public Class Test
    Public Property Await As Integer = 1
    Public Property Async As String = "Test"
End Class

    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, LatestVbReferences, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub AsyncWithObsolete_Errors()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Public Class MyTask(Of T)
    <Obsolete("Do not use!", True)>
    Public Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T)()
    End Function

    Async Function Test() As Task
        Await Me
    End Function
End Class

Public Class MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
    End Sub

    <Obsolete("Do not use!", True)>
    Public Function GetResult() As T
        Return Nothing
    End Function

    <Obsolete("Do not use!", True)>
    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC30668: 'Public Function GetAwaiter() As MyTaskAwaiter(Of T)' is obsolete: 'Do not use!'.
        Await Me
        ~~~~~~~~
BC30668: 'Public Function GetResult() As T' is obsolete: 'Do not use!'.
        Await Me
        ~~~~~~~~
BC30668: 'Public ReadOnly Property IsCompleted As Boolean' is obsolete: 'Do not use!'.
        Await Me
        ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AsyncWithObsolete_Warnings()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Public Class MyTask(Of T)
    <Obsolete("Do not use!", False)>
    Public Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T)()
    End Function

    Async Function Test() As Task
        Await Me
    End Function
End Class

Public Class MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
    End Sub

    <Obsolete("Do not use!", False)>
    Public Function GetResult() As T
        Return Nothing
    End Function

    <Obsolete("Do not use!", False)>
    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC40000: 'Public Function GetAwaiter() As MyTaskAwaiter(Of T)' is obsolete: 'Do not use!'.
        Await Me
        ~~~~~~~~
BC40000: 'Public Function GetResult() As T' is obsolete: 'Do not use!'.
        Await Me
        ~~~~~~~~
BC40000: 'Public ReadOnly Property IsCompleted As Boolean' is obsolete: 'Do not use!'.
        Await Me
        ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AsyncWithObsolete_Interface()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Public Class MyTask(Of T)
    Public Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T)()
    End Function

    Async Function Test() As Task
        Await Me
    End Function
End Class

<Obsolete("Do not use!", True)>
Public Class MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
    End Sub

    Public Function GetResult() As T
        Return Nothing
    End Function

    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC30668: 'MyTaskAwaiter(Of T)' is obsolete: 'Do not use!'.
    Public Function GetAwaiter() As MyTaskAwaiter(Of T)
                                    ~~~~~~~~~~~~~~~~~~~
BC30668: 'MyTaskAwaiter(Of T)' is obsolete: 'Do not use!'.
        Return New MyTaskAwaiter(Of T)()
                   ~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AsyncWithObsolete_InterfaceMethod()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Public Class MyTask(Of T)
    Public Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T)()
    End Function

    Async Function Test() As Task
        Await Me
    End Function
End Class

Public Class MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    <Obsolete("Do not use!", True)>
    Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
    End Sub

    Public Function GetResult() As T
        Return Nothing
    End Function

    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub Legacy_Async_Overload_Change_3()
            Dim source =
<compilation>
    <file name="a.vb">
        <%= SemanticResourceUtil.Async_Overload_Change_3_vb %>
    </file>
</compilation>

            Dim warnings = New Dictionary(Of String, ReportDiagnostic)()
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42356), ReportDiagnostic.Suppress)
            Dim compilation = CreateEmptyCompilationWithReferences(source,
                                    {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929},
                                    TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(New ReadOnlyDictionary(Of String, ReportDiagnostic)(warnings)))

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact(), WorkItem(1066694, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066694")>
        Public Sub Bug1066694()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Module1
    Sub Main()
        System.Console.WriteLine("Non-Async")
        System.Console.WriteLine()
        TestLocal()
        System.Console.WriteLine()
        System.Console.WriteLine("Async")
        System.Console.WriteLine()
        Task.WaitAll(TestLocalAsync())
    End Sub
    Sub TestLocal()
        Dim l = New TestClass("Unchanged")
        l.M1(l, Mutate(l))
        System.Console.WriteLine(l.State)
    End Sub

    Async Function DummyAsync(x As Object) As Task(Of Object)
        Return x
    End Function
    Async Function TestLocalAsync() As Task
        Dim l = New TestClass("Unchanged")
        l.M1(l, Await DummyAsync(Mutate(l)))
        System.Console.WriteLine(l.State)
    End Function
    Function Mutate(ByRef x As TestClass) As Object
        x = New TestClass("Changed")
        Return x
    End Function
End Module
Class TestClass
    Private ReadOnly fld1 As String
    Sub New(val As String)
        fld1 = val
    End Sub
    Function State() As String
        Return fld1
    End Function
    Sub M1(arg1 As TestClass, arg2 As Object)
        System.Console.WriteLine(Me.State)
        System.Console.WriteLine(arg1.State)
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Non-Async

Unchanged
Unchanged
Changed

Async

Unchanged
Unchanged
Changed
]]>)
        End Sub

        <Fact(), WorkItem(1068084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068084")>
        Public Sub Bug1068084()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Threading.Tasks
 
Class Test
    Async Sub F()
        Await Task.Delay(0)
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626}, TestOptions.ReleaseDll)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError' is not defined.
    Async Sub F()
    ~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError' is not defined.
    Async Sub F()
    ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(1021941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021941")>
        Public Sub Bug1021941()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub M1(x As Integer, y As Integer, z As Integer)
    Function M2() As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub M1(x As Integer, y As Integer, z As Integer) Implements IMoveable.M1
        Console.WriteLine("M1 is called for item '{0}'", Me.Name)
    End Sub

    Public Function M2() As Integer Implements IMoveable.M2
        Console.WriteLine("M2 is called for item '{0}'", Me.Name)
        Return 0
    End Function
End Class

Class Program
    Shared Sub Main()
        Dim item = New Item With {.Name = "Goo"}
        Task.WaitAll(Shift(item))
    End Sub

    Shared Async Function Shift(Of T As {Class, IMoveable})(item As T) As Task(Of Integer)
        item.M1(item.M2(), Await DummyAsync(), GetOffset(item))
        Return 0
    End Function

    Shared Async Function DummyAsync() As Task(Of Integer)
        Return 0
    End Function

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        item = DirectCast(DirectCast(New Item With {.Name = "Bar"}, IMoveable), T)
        Return 0
    End Function
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.DebugExe)

            CompileAndVerify(compilation,
            <![CDATA[
M2 is called for item 'Goo'
M1 is called for item 'Goo'
]]>)
        End Sub

        <Fact(), WorkItem(1173166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1173166"), WorkItem(2878, "https://github.com/dotnet/roslyn/issues/2878")>
        Public Sub CompoundAssignment()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Public Class Test
    Private _field As UInteger

    Shared Sub Main()
        Dim t as New Test()
        System.Console.WriteLine(t._field)
        t.EventHandler(-1).Wait()
        System.Console.WriteLine(t._field)
    End Sub

    Private Async Function EventHandler(args As Integer) As System.Threading.Tasks.Task
        Await RunAsync(Async Function()
                                   System.Console.WriteLine(args)
                                   _field += CUInt(1)
                       End Function)
    End Function

    Private Async Function RunAsync(x As System.Func(Of System.Threading.Tasks.Task)) As System.Threading.Tasks.Task
        Await x()
    End Function
End Class
]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.DebugExe)

            Dim expected As Xml.Linq.XCData = <![CDATA[
0
-1
1
]]>
            CompileAndVerify(compilation, expected)

            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expected)
        End Sub

    End Class
End Namespace
