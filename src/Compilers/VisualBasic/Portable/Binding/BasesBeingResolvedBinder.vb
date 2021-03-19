' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Public Sub New(containingBinder As Binder, basesBeingResolved As BasesBeingResolved)
            MyBase.New(containingBinder, basesBeingResolved)
        End Sub

        Public Overrides Function CheckAccessibility(sym As Symbol,
                                                     <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
                                                     Optional accessThroughType As TypeSymbol = Nothing,
                                                     Optional basesBeingResolved As BasesBeingResolved = Nothing) As AccessCheckResult
            ' Accessibility checking may involve looking at base types. We need to pass this accessibility
            ' checking code any base classes currently being bound so we don't use those.

            ' add basesBeingResolved that was passed in to the ones stored in this binder.
            Dim currentBasesBeingResolved = Me.BasesBeingResolved()

            For Each inheritsBeingResolved In If(basesBeingResolved.InheritsBeingResolvedOpt, ConsList(Of TypeSymbol).Empty)
                currentBasesBeingResolved = currentBasesBeingResolved.PrependInheritsBeingResolved(inheritsBeingResolved)
            Next
            For Each implementsBeingResolved In If(basesBeingResolved.ImplementsBeingResolvedOpt, ConsList(Of TypeSymbol).Empty)
                currentBasesBeingResolved = currentBasesBeingResolved.PrependImplementsBeingResolved(implementsBeingResolved)
            Next

            Return m_containingBinder.CheckAccessibility(sym, useSiteInfo, accessThroughType, currentBasesBeingResolved)
        End Function
    End Class

    Friend Structure BasesBeingResolved
        Public ReadOnly InheritsBeingResolvedOpt As ConsList(Of TypeSymbol)
        Public ReadOnly ImplementsBeingResolvedOpt As ConsList(Of TypeSymbol)

        Public Shared ReadOnly Property Empty As BasesBeingResolved
            Get
                Return New BasesBeingResolved()
            End Get
        End Property

        Public Sub New(inheritsBeingResolved As ConsList(Of TypeSymbol), implementsBeingResolved As ConsList(Of TypeSymbol))
            Me.InheritsBeingResolvedOpt = inheritsBeingResolved
            Me.ImplementsBeingResolvedOpt = implementsBeingResolved
        End Sub

        Public Function PrependInheritsBeingResolved(symbol As TypeSymbol) As BasesBeingResolved
            Return New BasesBeingResolved(If(InheritsBeingResolvedOpt, ConsList(Of TypeSymbol).Empty).Prepend(symbol), ImplementsBeingResolvedOpt)
        End Function

        Public Function PrependImplementsBeingResolved(symbol As TypeSymbol) As BasesBeingResolved
            Return New BasesBeingResolved(InheritsBeingResolvedOpt, If(ImplementsBeingResolvedOpt, ConsList(Of TypeSymbol).Empty).Prepend(symbol))
        End Function
    End Structure

End Namespace
