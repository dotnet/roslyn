' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq.Expressions
Imports System.Runtime.CompilerServices

Namespace Global
    Public Class QueryHelper(Of T) : Implements System.Linq.IQueryable(Of T), System.Linq.IQueryProvider
        Public ReadOnly Property Value As T
            Get
                Return Nothing
            End Get
        End Property
        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
            Return DirectCast(New T() {}, IEnumerable(Of T)).GetEnumerator
        End Function
        Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return (New T() {}).GetEnumerator
        End Function
        Public ReadOnly Property ElementType As System.Type Implements System.Linq.IQueryable.ElementType
            Get
                Return GetType(T)
            End Get
        End Property
        Public ReadOnly Property Expression As Expression Implements System.Linq.IQueryable.Expression
            Get
                Return System.Linq.Expressions.Expression.Constant(DirectCast(Nothing, QueryHelper(Of T)), GetType(QueryHelper(Of T)))
            End Get
        End Property
        Public ReadOnly Property Provider As System.Linq.IQueryProvider Implements System.Linq.IQueryable.Provider
            Get
                Return Me
            End Get
        End Property

        Public Function CreateQuery(expression As System.Linq.Expressions.Expression) As System.Linq.IQueryable Implements System.Linq.IQueryProvider.CreateQuery
            Return CreateQuery1(Of T)(expression)
        End Function

        Public Function CreateQuery1(Of TElement)(expression As System.Linq.Expressions.Expression) As System.Linq.IQueryable(Of TElement) Implements System.Linq.IQueryProvider.CreateQuery
            Return New QueryHelper(Of TElement)()
        End Function

        Public Function Execute(expression As System.Linq.Expressions.Expression) As Object Implements System.Linq.IQueryProvider.Execute
            Return Execute1(Of T)(expression)
        End Function

        Public Function Execute1(Of TResult)(expression As System.Linq.Expressions.Expression) As TResult Implements System.Linq.IQueryProvider.Execute
            Return Nothing
        End Function
    End Class

    <Extension()>
    Public Module ExpressionTreeHelpers
        <Extension()>
        Public Function [Select](Of T, S)(ByVal i As QueryHelper(Of T), ByVal func As Expression(Of Func(Of T, S))) As QueryHelper(Of S)
            Console.WriteLine(func.Dump)
            Return New QueryHelper(Of S)()
        End Function

        <Extension()> _
        Public Function [SelectMany](Of TSource, TCollection, TResult)(ByVal source As QueryHelper(Of TSource), ByVal collectionSelector As Expression(Of Func(Of TSource, IEnumerable(Of TCollection))), ByVal resultSelector As Expression(Of Func(Of TSource, TCollection, TResult))) As QueryHelper(Of TResult)
            'Console.WriteLine(collectionSelector.Dump)
            Console.WriteLine(resultSelector.Dump)
            Return New QueryHelper(Of TResult)()
        End Function

        <Extension()> _
        Public Function [Where](Of T)(ByVal source As QueryHelper(Of T), ByVal predicate As Expression(Of Func(Of T, Boolean))) As QueryHelper(Of T)
            Console.WriteLine(predicate.Dump)
            Return source
        End Function

        Public Function GetQueryCollection(Of T)(ParamArray elements() As T) As QueryHelper(Of T)
            Return New QueryHelper(Of T)()
        End Function

        <Extension()> _
        Public Function GroupJoin(Of TOuter, TInner, TKey, TResult)(outer As System.Linq.IQueryable(Of TOuter), inner As System.Linq.IQueryable(Of TInner), outerKeySelector As Expression(Of Func(Of TOuter, TKey)), innerKeySelector As Expression(Of Func(Of TInner, TKey)), resultSelector As Expression(Of Func(Of TOuter, System.Linq.IQueryable(Of TInner), TResult))) As System.Linq.IQueryable(Of TResult)
            Console.WriteLine(outerKeySelector.Dump)
            Console.WriteLine(innerKeySelector.Dump)
            Console.WriteLine(resultSelector.Dump)
            Return New QueryHelper(Of TResult)()
        End Function
    End Module
End Namespace
