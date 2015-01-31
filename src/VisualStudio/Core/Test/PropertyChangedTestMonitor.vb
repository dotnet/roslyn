' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq.Expressions
Imports System.ComponentModel

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Class PropertyChangedTestMonitor

        ReadOnly propertyChangedObject As INotifyPropertyChanged
        Dim expectationCountMap As Dictionary(Of String, Integer)
        Dim failures As List(Of String)

        Sub New(propertyChangedObject As INotifyPropertyChanged, Optional strict As Boolean = False)
            AddHandler propertyChangedObject.PropertyChanged, AddressOf PropertyChangedHandler
            Me.propertyChangedObject = propertyChangedObject
            Me.strict = strict

            failures = New List(Of String)
            expectationCountMap = New Dictionary(Of String, Integer)
        End Sub

        Private Property strict As Boolean

        Public Sub AddExpectation(Of TResult)(expectation As Expression(Of Func(Of TResult)), Optional count As Integer = 1)
            Dim propertyName = DirectCast(expectation.Body, MemberExpression).Member.Name

            If expectationCountMap.ContainsKey(propertyName) Then
                expectationCountMap(propertyName) += count
            Else
                expectationCountMap(propertyName) = count
            End If
        End Sub

        Private Sub PropertyChangedHandler(sender As Object, e As PropertyChangedEventArgs)
            If expectationCountMap.ContainsKey(e.PropertyName) Then
                expectationCountMap(e.PropertyName) -= 1
                If expectationCountMap(e.PropertyName) < 0 AndAlso strict Then
                    failures.Add(String.Format("Property '{0}' was updated more times than expected.", e.PropertyName))
                End If
            ElseIf strict Then
                failures.Add(String.Format("Property '{0}' was unexpectedly updated.", e.PropertyName))
            End If
        End Sub

        Public Sub VerifyExpectations()
            For Each failureCase In expectationCountMap.Where(Function(e) e.Value > 0)
                failures.Add(String.Format("Property '{0}' was expected to be updated {1} more time(s) than it was", failureCase.Key, failureCase.Value))
            Next

            If failures.Any() Then
                Assert.False(True, String.Format("The following INotifyPropertyChanged expectations were not met: {0}", Environment.NewLine + String.Join(Environment.NewLine, failures)))
            End If
        End Sub

        Public Sub Detach()
            RemoveHandler propertyChangedObject.PropertyChanged, AddressOf PropertyChangedHandler
        End Sub
    End Class
End Namespace
