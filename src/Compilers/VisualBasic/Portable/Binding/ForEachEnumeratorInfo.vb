' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Holds all information needed to rewrite a bound for each node.
    ''' </summary>
    Friend NotInheritable Class ForEachEnumeratorInfo

        ''' <summary>
        ''' A bound call to the GetEnumerator method.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly GetEnumerator As BoundExpression

        ''' <summary>
        ''' A bound call to the MoveNext method.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly MoveNext As BoundExpression

        ''' <summary>
        ''' A bound access to the Current property.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Current As BoundExpression

        ''' <summary>
        ''' Element type of the collection.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly ElementType As TypeSymbol

        ''' <summary>
        ''' True is the enumerator needs or may need (in case of IEnumerator) to be disposed.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly NeedToDispose As Boolean

        ''' <summary>
        ''' True if the enumerator is, inherits from or implements IDisposable.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly IsOrInheritsFromOrImplementsIDisposable As Boolean

        ''' <summary>
        ''' The condition that is used to determine whether to call Dispose or not (contains a placeholder).
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly DisposeCondition As BoundExpression

        ''' <summary>
        ''' The conversion of the enumerator to the target type on which Dispose is called 
        ''' (contains a placeholder).
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly DisposeCast As BoundExpression

        ''' <summary>
        ''' The conversion of the return value of the current call to the type of the control variable 
        ''' (contains a placeholder).
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly CurrentConversion As BoundExpression

        ''' <summary>
        ''' Placeholder for the bound enumerator local. 
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly EnumeratorPlaceholder As BoundLValuePlaceholder

        ''' <summary>
        ''' Placeholder for the bound call to the get_Current method.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly CurrentPlaceholder As BoundRValuePlaceholder

        ''' <summary>
        ''' Placeholder for the collection; used only when the collection's type 
        ''' is not an one dimensional array or string.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly CollectionPlaceholder As BoundRValuePlaceholder

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ForEachEnumeratorInfo" /> class.
        ''' </summary>
        ''' <param name="getEnumerator">A bound call to the GetEnumerator method.</param>
        ''' <param name="moveNext">A bound call to the MoveNext method.</param>
        ''' <param name="current">A bound access to the Current property.</param>
        ''' <param name="elementType">An element type.</param>
        ''' <param name="needToDispose">if set to <c>true</c> the enumerator needs to be disposed.</param>
        ''' <param name="isOrInheritsFromOrImplementsIDisposable">if set to <c>true</c> the enumerator is or inherits from or implements IDisposable.</param>
        ''' <param name="disposeCondition">The condition whether to call dispose or not.</param>
        ''' <param name="disposeCast">The conversion of the enumerator to call Dispose on.</param>
        ''' <param name="currentConversion">The conversion from Current return type to the type of the controlVariable.</param>
        ''' <param name="enumeratorPlaceholder">The placeholder for the bound enumerator local.</param>
        ''' <param name="currentPlaceholder">The placeholder for the expression that get's the current value.</param>
        ''' <param name="collectionPlaceholder">The placeholder for the collection expression.</param>
        Public Sub New(
            getEnumerator As BoundExpression,
            moveNext As BoundExpression,
            current As BoundExpression,
            elementType As TypeSymbol,
            needToDispose As Boolean,
            isOrInheritsFromOrImplementsIDisposable As Boolean,
            disposeCondition As BoundExpression,
            disposeCast As BoundExpression,
            currentConversion As BoundExpression,
            enumeratorPlaceholder As BoundLValuePlaceholder,
            currentPlaceholder As BoundRValuePlaceholder,
            collectionPlaceholder As BoundRValuePlaceholder
        )
            Me.GetEnumerator = getEnumerator
            Me.MoveNext = moveNext
            Me.Current = current
            Me.ElementType = elementType
            Me.NeedToDispose = needToDispose
            Me.IsOrInheritsFromOrImplementsIDisposable = isOrInheritsFromOrImplementsIDisposable
            Me.DisposeCondition = disposeCondition
            Me.DisposeCast = disposeCast
            Me.CurrentConversion = currentConversion
            Me.EnumeratorPlaceholder = enumeratorPlaceholder
            Me.CurrentPlaceholder = currentPlaceholder
            Me.CollectionPlaceholder = collectionPlaceholder
        End Sub
    End Class
End Namespace
