' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    '''  Structure containing all semantic information about a for each statement.
    ''' </summary>
    Public Structure ForEachStatementInfo

        ''' <summary>
        ''' Gets the &quot;GetEnumerator&quot; method.
        ''' </summary>
        Public ReadOnly Property GetEnumeratorMethod As IMethodSymbol

        ''' <summary>
        ''' Gets the &quot;MoveNext&quot; method.
        ''' </summary>
        Public ReadOnly Property MoveNextMethod As IMethodSymbol

        ''' <summary>
        ''' Gets the &quot;Current&quot; property.
        ''' </summary>
        Public ReadOnly Property CurrentProperty As IPropertySymbol

        ''' <summary>
        ''' Gets the &quot;Dispose&quot; method.
        ''' </summary>
        Public ReadOnly Property DisposeMethod As IMethodSymbol

        ''' <summary>
        ''' The intermediate type to which the output of the <see cref="CurrentProperty"/> is converted
        ''' before being converted to the iteration variable type.
        ''' </summary>
        ''' <remarks>
        ''' As you might hope, for an array, it is the element type of the array.
        ''' </remarks>
        Public ReadOnly Property ElementType As ITypeSymbol

        ''' <summary>
        ''' The conversion from the <see cref="ElementType"/> to the iteration variable type.
        ''' </summary>
        ''' <remarks>
        ''' May be user-defined.
        ''' </remarks>
        Public ReadOnly Property ElementConversion As Conversion

        ''' <summary>
        ''' The conversion from the type of the <see cref="CurrentProperty"/> to the <see cref="ElementType"/>.
        ''' </summary>
        ''' <remarks>
        ''' Frequently the identity conversion.
        ''' </remarks>
        Public ReadOnly Property CurrentConversion As Conversion

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
    End Structure
End Namespace
