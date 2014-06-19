' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    '''  Structure containing all semantic information about a for each statement.
    ''' </summary>
    Public Structure ForEachStatementInfo

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ForEachStatementInfo" /> structure.
        ''' </summary>
        ''' <param name="getEnumeratorMethod">The GetEnumerator method.</param>
        ''' <param name="moveNextMethod">The MoveNext method.</param>
        ''' <param name="currentProperty">The Current property.</param>
        ''' <param name="disposeMethod">The Dispose method.</param>
        Friend Sub New(getEnumeratorMethod As IMethodSymbol,
                       moveNextMethod As IMethodSymbol,
                       currentProperty As IPropertySymbol,
                       disposeMethod As IMethodSymbol,
                       elementType As ITypeSymbol,
                       elementConversion As Conversion,
                       currentConversion As Conversion)
            Me.GetEnumeratorMethod = getEnumeratorMethod
            Me.MoveNextMethod = moveNextMethod
            Me.CurrentProperty = currentProperty
            Me.DisposeMethod = disposeMethod
            Me.ElementType = elementType
            Me.ElementConversion = elementConversion
            Me.CurrentConversion = currentConversion
        End Sub

        ''' <summary>
        ''' Gets the &quot;GetEnumerator&quot; method.
        ''' </summary>
        Public ReadOnly GetEnumeratorMethod As IMethodSymbol

        ''' <summary>
        ''' Gets the &quot;MoveNext&quot; method.
        ''' </summary>
        Public ReadOnly MoveNextMethod As IMethodSymbol

        ''' <summary>
        ''' Gets the &quot;Current&quot; property.
        ''' </summary>
        Public ReadOnly CurrentProperty As IPropertySymbol

        ''' <summary>
        ''' Gets the &quot;Dispose&quot; method.
        ''' </summary>
        Public ReadOnly DisposeMethod As IMethodSymbol

        ''' <summary>
        ''' The intermediate type to which the output of the <see cref="CurrentProperty"/> is converted
        ''' before being converted to the iteration variable type.
        ''' </summary>
        ''' <remarks>
        ''' As you might hope, for an array, it is the element type of the array.
        ''' </remarks>
        Public ReadOnly ElementType As ITypeSymbol

        ''' <summary>
        ''' The conversion from the <see cref="ElementType"/> to the iteration variable type.
        ''' </summary>
        ''' <remarks>
        ''' May be user-defined.
        ''' </remarks>
        Public ReadOnly ElementConversion As Conversion

        ''' <summary>
        ''' The conversion from the type of the <see cref="CurrentProperty"/> to the <see cref="ElementType"/>.
        ''' </summary>
        ''' <remarks>
        ''' Frequently the identity conversion.
        ''' </remarks>
        Public ReadOnly CurrentConversion As Conversion
    End Structure
End Namespace