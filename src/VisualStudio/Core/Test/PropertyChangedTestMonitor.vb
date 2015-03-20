' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq.Expressions
Imports System.ComponentModel

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Class PropertyChangedTestMonitor

        Private ReadOnly _propertyChangedObject As INotifyPropertyChanged
        Private _expectationCountMap As Dictionary(Of String, Integer)
        Private _failures As List(Of String)

        Public Sub New(propertyChangedObject As INotifyPropertyChanged, Optional strict As Boolean = False)
            AddHandler propertyChangedObject.PropertyChanged, AddressOf PropertyChangedHandler
            Me._propertyChangedObject = propertyChangedObject
            Me.strict = strict

            _failures = New List(Of String)
            _expectationCountMap = New Dictionary(Of String, Integer)
        End Sub

        Private Property strict As Boolean

        Public Sub AddExpectation(Of TResult)(expectation As Expression(Of Func(Of TResult)), Optional count As Integer = 1)
            Dim propertyName = DirectCast(expectation.Body, MemberExpression).Member.Name

            If _expectationCountMap.ContainsKey(propertyName) Then
                _expectationCountMap(propertyName) += count
            Else
                _expectationCountMap(propertyName) = count
            End If
        End Sub

        Private Sub PropertyChangedHandler(sender As Object, e As PropertyChangedEventArgs)
            If _expectationCountMap.ContainsKey(e.PropertyName) Then
                _expectationCountMap(e.PropertyName) -= 1
                If _expectationCountMap(e.PropertyName) < 0 AndAlso strict Then
                    _failures.Add(String.Format("Property '{0}' was updated more times than expected.", e.PropertyName))
                End If
            ElseIf strict Then
                _failures.Add(String.Format("Property '{0}' was unexpectedly updated.", e.PropertyName))
            End If
        End Sub

        Public Sub VerifyExpectations()
            For Each failureCase In _expectationCountMap.Where(Function(e) e.Value > 0)
                _failures.Add(String.Format("Property '{0}' was expected to be updated {1} more time(s) than it was", failureCase.Key, failureCase.Value))
            Next

            If _failures.Any() Then
                Assert.False(True, String.Format("The following INotifyPropertyChanged expectations were not met: {0}", Environment.NewLine + String.Join(Environment.NewLine, _failures)))
            End If
        End Sub

        Public Sub Detach()
            RemoveHandler _propertyChangedObject.PropertyChanged, AddressOf PropertyChangedHandler
        End Sub
    End Class
End Namespace
