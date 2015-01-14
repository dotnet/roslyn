' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Causes lookups to assume that the given set of classes are having their 
    ''' bases being resolved, so lookups should not check for base classes.
    ''' </summary>
    Friend NotInheritable Class BasesBeingResolvedBinder
        Inherits Binder

        Public Sub New(containingBinder As Binder, basesBeingResolved As ConsList(Of Symbol))
            MyBase.New(containingBinder, basesBeingResolved)
        End Sub

        Public Overrides Function CheckAccessibility(sym As Symbol,
                                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                     Optional accessThroughType As TypeSymbol = Nothing,
                                                     Optional basesBeingResolved As ConsList(Of Symbol) = Nothing) As AccessCheckResult
            ' Accessibility checking may involve looking at base types. We need to pass this accessibility
            ' checking code any base classes currently being bound so we don't use those.

            ' add basesBeingResolved that was passed in to the ones stored in this binder.
            Dim currentBasesBeingResolved = Me.BasesBeingResolved()
            If basesBeingResolved IsNot Nothing Then
                For Each sym In basesBeingResolved
                    currentBasesBeingResolved = New ConsList(Of Symbol)(sym, currentBasesBeingResolved)
                Next
            End If

            Return m_containingBinder.CheckAccessibility(sym, useSiteDiagnostics, accessThroughType, currentBasesBeingResolved)
        End Function
    End Class

End Namespace
