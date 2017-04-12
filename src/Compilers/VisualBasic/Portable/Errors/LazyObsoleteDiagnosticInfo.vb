﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class LazyObsoleteDiagnosticInfo
        Inherits DiagnosticInfo

        Private _lazyActualObsoleteDiagnostic As DiagnosticInfo

        Private ReadOnly _symbol As Symbol
        Private ReadOnly _containingSymbol As Symbol

        Friend Sub New(sym As Symbol, containingSymbol As Symbol)
            MyBase.New(VisualBasic.MessageProvider.Instance, ERRID.Unknown)
            Me._symbol = sym
            Me._containingSymbol = containingSymbol
        End Sub

        Friend Overrides Function GetResolvedInfo() As DiagnosticInfo
            If _lazyActualObsoleteDiagnostic Is Nothing Then
                ' A symbol's Obsoleteness may not have been calculated yet if the symbol is coming
                ' from a different compilation's source. In that case, force completion of attributes.
                _symbol.ForceCompleteObsoleteAttribute()

                If _symbol.ObsoleteState = ThreeState.True Then
                    Dim inObsoleteContext = ObsoleteAttributeHelpers.GetObsoleteContextState(_containingSymbol, forceComplete:=True)
                    Debug.Assert(inObsoleteContext <> ThreeState.Unknown)

                    If inObsoleteContext = ThreeState.False Then
                        Dim info As DiagnosticInfo = ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(_symbol)
                        If info IsNot Nothing Then
                            Interlocked.CompareExchange(Me._lazyActualObsoleteDiagnostic, info, Nothing)
                            Return Me._lazyActualObsoleteDiagnostic
                        End If
                    End If
                End If

                ' If this symbol is not obsolete or is in an obsolete context, we don't want to report any diagnostics.
                ' Therefore make this a Void diagnostic.
                Interlocked.CompareExchange(Me._lazyActualObsoleteDiagnostic, ErrorFactory.VoidDiagnosticInfo, Nothing)
            End If

            Return _lazyActualObsoleteDiagnostic
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim asd = TryCast(obj, LazyObsoleteDiagnosticInfo)
            If asd IsNot Nothing Then
                Return Me._symbol = asd._symbol AndAlso
                    Me._containingSymbol = asd._containingSymbol AndAlso
                    MyBase.Equals(obj)
            End If
            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me._symbol,
                Hash.Combine(Me._containingSymbol,
                MyBase.GetHashCode()))
        End Function
    End Class
End Namespace
