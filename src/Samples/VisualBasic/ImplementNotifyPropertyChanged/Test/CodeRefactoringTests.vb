' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Roslyn.Samples.ImplementNotifyPropertyChangedVB
Imports Roslyn.UnitTestFramework
Imports Xunit

Namespace ImplementNotifyPropertyChanged.UnitTests

    Public Class CodeRefactoringTests
        Inherits CodeRefactoringProviderTestFixture

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
        Protected Overrides Function CreateCodeRefactoringProvider() As CodeRefactoringProvider
            Return New ImplementNotifyPropertyChangedCodeRefactoringProvider()
        End Function

        <Fact()>
        Public Sub TestNoActionOnBrokenReadOnlyProperty()
            Dim code As String = <text>
Class C
    Property [|P|] As Integer
        Get
    End Property
End Class</text>.Value
            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoActionOnMethod()
            Dim code As String = <text>
Public Class C
    Public Function [|Foo|]() As Integer
    End Function
End Class
</text>.Value
            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoActionOnReadOnlyProperty()
            Dim code As String = <text>
Class C
    Property [|P|] As Integer
        Get
            Return 5
        End Get
    End Property
End Class</text>.Value
            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestRefactoringOnAutoProperty()
            Dim code As String = <text>Class C
    Property [|P|] As Integer
End Class</text>.Value

            Dim expected As String = <text>Imports System.Collections.Generic
Imports System.ComponentModel

Class C
    Implements INotifyPropertyChanged

    Private _p As Integer

    Property P As Integer
        Get
            Return _p
        End Get

        Set(value As Integer)
            SetProperty(_p, value, "P")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub
End Class
</text>.Value
            Test(code, expected)
        End Sub

        <Fact()>
        Public Sub TestRefactoringOnExpandedProperty1()
            Dim code As String = <text>Class C
    Private _p As Integer

    Property [|P|] As Integer
        Get
            return _p
        End Get
        Set
            _p = value
        End Set
    End Property
End Class</text>.Value

            Dim expected As String = <text>Imports System.Collections.Generic
Imports System.ComponentModel

Class C
    Implements INotifyPropertyChanged

    Private _p As Integer

    Property P As Integer
        Get
            Return _p
        End Get

        Set
            SetProperty(_p, value, "P")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub
End Class
</text>.Value
            Test(code, expected)
        End Sub

        <Fact()>
        Public Sub TestRefactoringOnExpandedProperty2()
            Dim code As String = <text>Class C
    Private _p As Integer

    Property [|P|] As Integer
        Get
            return _p
        End Get
        Set
            If _p &lt;&gt; value Then
                _p = value
            End If
        End Set
    End Property
End Class</text>.Value

            Dim expected As String = <text>Imports System.Collections.Generic
Imports System.ComponentModel

Class C
    Implements INotifyPropertyChanged

    Private _p As Integer

    Property P As Integer
        Get
            Return _p
        End Get

        Set
            SetProperty(_p, value, "P")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub
End Class
</text>.Value
            Test(code, expected)
        End Sub

        <Fact()>
        Public Sub TestRefactoringOnExpandedProperty3()
            Dim code As String = <text>Class C
    Private _p As Integer

    Property [|P|] As Integer
        Get
            return _p
        End Get
        Set
            If _p &lt;&gt; value Then _p = value
        End Set
    End Property
End Class</text>.Value

            Dim expected As String = <text>Imports System.Collections.Generic
Imports System.ComponentModel

Class C
    Implements INotifyPropertyChanged

    Private _p As Integer

    Property P As Integer
        Get
            Return _p
        End Get

        Set
            SetProperty(_p, value, "P")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub
End Class
</text>.Value
            Test(code, expected)
        End Sub

        <Fact()>
        Public Sub TestRefactoringInStruct()
            Dim code As String = <text>Structure S
    Property [|P|] As Integer
End Structure</text>.Value

            Dim expected As String = <text>Imports System.Collections.Generic
Imports System.ComponentModel

Structure S
    Implements INotifyPropertyChanged

    Private _p As Integer

    Property P As Integer
        Get
            Return _p
        End Get

        Set(value As Integer)
            SetProperty(_p, value, "P")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub
End Structure
</text>.Value
            Test(code, expected)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringOnBrokenReadWriteProperty()
            Dim code As String = <text>Class C
    Property [|P|] As Integer
        Get
        End Get
        Set(value As Integer)
    End Property
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringOnReadWritePropertyWithNoBackingField()
            Dim code As String = <text>Class C
    Property [|P|] As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class</text>.Value
            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringOnRecursivePropertyWithEmptySetBody()
            Dim code As String = <text>Class C
    Property [|P|] As Integer
        Get
            Return P
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringOnPropertyWithBackingFieldOnlyUsedInGetter()
            Dim code As String = <text>Class C
    Dim f As Integer
    Property [|P|] As Integer
        Get
            Return f
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringWhenSetterComparesAgainstExpressionOtherThanValue()
            Dim code As String = <text>Class C
    Private _p as Integer

    Property [|P|] As Integer
        Get
            return _p
        End Get
        Set
            If _p &lt;&gt; 3 Then _p = value
        End Set
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringWhenSetterImproprelyStructured1()
            Dim code As String = <text>Class C
    Private f as Integer

    Property [|P|] As Integer
        Get
            Return f
        End Get
        Set
            If f &lt;&gt; value Then
                Return
            End If
        End Set
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringWhenSetterImproprelyStructured2()
            Dim code As String = <text>Class C
    Private f as Integer
    Private f2 as Integer

    Property [|P|] As Integer
        Get
            Return f
        End Get
        Set
            If f &lt;&gt; value Then
                f2 = value
            End If
        End Set
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringWhenSetterImproprelyStructured3()
            Dim code As String = <text>Class C
    Private f as Integer

    Property [|P|] As Integer
        Get
            Return f
        End Get
        Set
            If f = value Then
                Return
            End If
            f = value
        End Set
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringWhenSetterImproprelyStructured4()
            Dim code As String = <text>Class C
    Private f as Integer

    Property [|P|] As Integer
        Get
            Return f
        End Get
        Set
            If f &lt;&gt; value Then Return
            f = value
        End Set
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringWhenSetterImproprelyStructured5()
            Dim code As String = <text>Class C
    Private f as Integer

    Property [|P|] As Integer
        Get
            Return f
        End Get
        Set
            If f = value Then Return
            f = 1
        End Set
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringWhenSetterImproprelyStructured6()
            Dim code As String = <text>Class C
    Private _p as Integer

    Property [|P|] As Integer
        Get
            Return f
        End Get
        Set
            If _p = value Then Return
            _p = value
        End Set
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestNoRefactoringOnAutoPropWithNoType()
            Dim code As String = <text>Class C
    Property [|P|]
End Class</text>.Value

            TestNoActions(code)
        End Sub

        <Fact()>
        Public Sub TestRefactoringOnClassThatImplementsINotifyPropertyChanged()
            Dim code As String = <text>Class C
    Implements INotifyPropertyChanged
    Property [|P|] As Integer
End Class</text>.Value

            Dim expected As String = <text>Imports System.Collections.Generic
Imports System.ComponentModel

Class C
    Implements INotifyPropertyChanged
    Implements INotifyPropertyChanged

    Private _p As Integer

    Property P As Integer
        Get
            Return _p
        End Get

        Set(value As Integer)
            SetProperty(_p, value, "P")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub
End Class
</text>.Value

            Test(code, expected)
        End Sub

        <Fact()>
        Public Sub TestRefactoringOnClassThatImplementsINotifyPropertyChangedAndHasPropertyChangedEvent()
            Dim code As String = <text>Imports System.ComponentModel

Class C
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Property [|P|] As Integer
End Class</text>.Value

            Dim expected As String = <text>Imports System.ComponentModel
Imports System.Collections.Generic

Class C
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Private _p As Integer

    Property P As Integer
        Get
            Return _p
        End Get

        Set(value As Integer)
            SetProperty(_p, value, "P")
        End Set
    End Property

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub
End Class
</text>.Value

            Test(code, expected)
        End Sub

        <Fact()>
        Public Sub TestRefactoringOnClassThatImplementsINotifyPropertyChangedAndHasPropertyChangedEventAndHasSetPropertyMethod()
            Dim code As String = <text>Imports System.ComponentModel

Class C
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value

            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub

    Property [|P|] As Integer
End Class</text>.Value

            Dim expected As String = <text>Imports System.ComponentModel
Imports System.Collections.Generic

Class C
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub

    Private _p As Integer

    Property P As Integer
        Get
            Return _p
        End Get

        Set(value As Integer)
            SetProperty(_p, value, "P")
        End Set
    End Property
End Class
</text>.Value

            Test(code, expected)
        End Sub
    End Class
End Namespace
