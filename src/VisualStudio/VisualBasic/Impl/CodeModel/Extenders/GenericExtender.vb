' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
    <ComVisible(True)>
    <ComDefaultInterface(GetType(IVBGenericExtender))>
    Public Class GenericExtender
        Implements IVBGenericExtender

        Friend Shared Function Create(codeType As AbstractCodeType) As IVBGenericExtender
            Dim result = New GenericExtender(codeType)
            Return CType(ComAggregate.CreateAggregatedObject(result), IVBGenericExtender)
        End Function

        Private ReadOnly _codeType As ParentHandle(Of AbstractCodeType)

        Private Sub New(codeType As AbstractCodeType)
            _codeType = New ParentHandle(Of AbstractCodeType)(codeType)
        End Sub

        Private Function GetTypesCount(baseType As Boolean) As Integer
            Dim typeSymbol = CType(_codeType.Value.LookupTypeSymbol(), INamedTypeSymbol)

            If baseType Then
                Select Case typeSymbol.TypeKind
                    Case TypeKind.Class,
                         TypeKind.Module,
                         TypeKind.Structure,
                         TypeKind.Delegate,
                         TypeKind.Enum

                        Return 1

                    Case TypeKind.Interface
                        Return typeSymbol.Interfaces.Length
                End Select
            Else
                Select Case typeSymbol.TypeKind
                    Case TypeKind.Class,
                         TypeKind.Structure

                        Return typeSymbol.Interfaces.Length
                End Select
            End If

            Throw Exceptions.ThrowEInvalidArg()
        End Function

        Private Function GetGenericName(baseType As Boolean, index As Integer) As String
            Dim typeSymbol = CType(_codeType.Value.LookupTypeSymbol(), INamedTypeSymbol)

            If baseType Then
                Select Case typeSymbol.TypeKind
                    Case TypeKind.Class,
                         TypeKind.Module,
                         TypeKind.Structure,
                         TypeKind.Delegate,
                         TypeKind.Enum

                        If index = 0 Then
                            Dim baseTypeSymbol = typeSymbol.BaseType
                            If baseTypeSymbol IsNot Nothing Then
                                Return baseTypeSymbol.ToDisplayString()
                            End If
                        End If

                        Return Nothing

                    Case TypeKind.Interface
                        Return If(index >= 0 AndAlso index < typeSymbol.Interfaces.Length,
                                  typeSymbol.Interfaces(index).ToDisplayString(),
                                  Nothing)
                End Select
            Else
                Select Case typeSymbol.TypeKind
                    Case TypeKind.Class,
                         TypeKind.Structure

                        Return If(index >= 0 AndAlso index < typeSymbol.Interfaces.Length,
                                  typeSymbol.Interfaces(index).ToDisplayString(),
                                  Nothing)

                    Case TypeKind.Delegate,
                         TypeKind.Enum
                        Return Nothing
                End Select
            End If

            Throw Exceptions.ThrowEInvalidArg()
        End Function

        Public ReadOnly Property GetBaseGenericName(index As Integer) As String Implements IVBGenericExtender.GetBaseGenericName
            Get
                ' NOTE: index is 1-based.
                Return GetGenericName(baseType:=True, index:=index - 1)
            End Get
        End Property

        Public ReadOnly Property GetBaseTypesCount As Integer Implements IVBGenericExtender.GetBaseTypesCount
            Get
                Return GetTypesCount(baseType:=True)
            End Get
        End Property

        Public ReadOnly Property GetImplementedTypesCount As Integer Implements IVBGenericExtender.GetImplementedTypesCount
            Get
                Return GetTypesCount(baseType:=False)
            End Get
        End Property

        Public ReadOnly Property GetImplTypeGenericName(index As Integer) As String Implements IVBGenericExtender.GetImplTypeGenericName
            Get
                ' NOTE: index is 1-based.
                Return GetGenericName(baseType:=False, index:=index - 1)
            End Get
        End Property

    End Class
End Namespace
