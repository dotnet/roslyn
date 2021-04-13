' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' This class provides an easy way to combine a MethodSignatureComparer and a PropertySignatureComparer
    ''' to create a unified MemberSignatureComparer (e.g. for use in a HashSet).
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class MemberSignatureComparer
        Implements IEqualityComparer(Of Symbol)

        ''' <summary>
        ''' This instance is used to compare potential WinRT fake members in type projection.
        ''' 
        ''' FIXME(angocke): This is almost certainly wrong. The semantics of WinRT conflict 
        ''' comparison should probably match overload resolution (i.e., we should not add a member
        '''  to lookup that would result in ambiguity), but this is closer to what Dev12 does.
        ''' 
        ''' The real fix here is to establish a spec for how WinRT conflict comparison should be
        ''' performed. Once this is done we should remove these comments.
        ''' </summary>
        Public Shared ReadOnly WinRTComparer As MemberSignatureComparer =
            New MemberSignatureComparer(MethodSignatureComparer.WinRTConflictComparer,
                                        PropertySignatureComparer.WinRTConflictComparer,
                                        EventSignatureComparer.WinRTConflictComparer)

        Private ReadOnly _methodComparer As MethodSignatureComparer
        Private ReadOnly _propertyComparer As PropertySignatureComparer
        Private ReadOnly _eventComparer As EventSignatureComparer

        Private Sub New(methodComparer As MethodSignatureComparer,
                        propertyComparer As PropertySignatureComparer,
                        eventComparer As EventSignatureComparer)
            Me._methodComparer = methodComparer
            Me._propertyComparer = propertyComparer
            Me._eventComparer = eventComparer
        End Sub

        Public Overloads Function Equals(sym1 As Symbol, sym2 As Symbol) As Boolean _
            Implements IEqualityComparer(Of Symbol).Equals

            CheckSymbolKind(sym1)
            CheckSymbolKind(sym2)

            If sym1.Kind <> sym2.Kind Then
                Return False
            End If

            Select Case sym1.Kind
                Case SymbolKind.Method
                    Return _methodComparer.Equals(DirectCast(sym1, MethodSymbol), DirectCast(sym2, MethodSymbol))
                Case SymbolKind.Property
                    Return _propertyComparer.Equals(DirectCast(sym1, PropertySymbol), DirectCast(sym2, PropertySymbol))
                Case SymbolKind.Event
                    Return _eventComparer.Equals(DirectCast(sym1, EventSymbol), DirectCast(sym2, EventSymbol))

                Case Else
                    ' To prevent warning
                    Return False
            End Select
        End Function

        Public Overloads Function GetHashCode(sym As Symbol) As Integer _
            Implements IEqualityComparer(Of Symbol).GetHashCode

            Select Case sym.Kind
                Case SymbolKind.Method
                    Return _methodComparer.GetHashCode(DirectCast(sym, MethodSymbol))
                Case SymbolKind.Property
                    Return _propertyComparer.GetHashCode(DirectCast(sym, PropertySymbol))
                Case SymbolKind.Event
                    Return _eventComparer.GetHashCode(DirectCast(sym, EventSymbol))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(sym.Kind)
            End Select
        End Function

        <Conditional("DEBUG")>
        Private Sub CheckSymbolKind(sym As Symbol)
            Select Case sym.Kind
                Case SymbolKind.Method, SymbolKind.Property, SymbolKind.Event
                    Exit Select
                Case Else
                    Debug.Assert(False, "Unexpected symbol kind: " & sym.Kind)
            End Select
        End Sub
    End Class
End Namespace
