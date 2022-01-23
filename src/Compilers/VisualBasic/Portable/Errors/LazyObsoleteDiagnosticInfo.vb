' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

                Dim kind = ObsoleteAttributeHelpers.GetObsoleteDiagnosticKind(_containingSymbol, _symbol, forceComplete:=True)
                Debug.Assert(kind <> ObsoleteDiagnosticKind.Lazy)
                Debug.Assert(kind <> ObsoleteDiagnosticKind.LazyPotentiallySuppressed)

                Dim info = If(kind = ObsoleteDiagnosticKind.Diagnostic,
                    ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(_symbol),
                    Nothing)

                ' If this symbol is not obsolete or is in an obsolete context, we don't want to report any diagnostics.
                ' Therefore make this a Void diagnostic.
                Interlocked.CompareExchange(_lazyActualObsoleteDiagnostic, If(info, ErrorFactory.VoidDiagnosticInfo), Nothing)
            End If

            Return _lazyActualObsoleteDiagnostic
        End Function

    End Class
End Namespace
