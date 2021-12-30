' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
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
    ''' A source module binder provides the context associated with a source module.
    ''' </summary>
    Friend Class SourceModuleBinder
        Inherits Binder

        Private ReadOnly _sourceModule As SourceModuleSymbol

        Public Sub New(containingBinder As Binder, sourceModule As SourceModuleSymbol)
            MyBase.New(containingBinder, sourceModule, sourceModule.ContainingSourceAssembly.DeclaringCompilation)
            _sourceModule = sourceModule
        End Sub

        Public Overrides Function CheckAccessibility(sym As Symbol,
                                                     <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
                                                     Optional accessThroughType As TypeSymbol = Nothing,
                                                     Optional basesBeingResolved As BasesBeingResolved = Nothing) As AccessCheckResult
            Return If(IgnoresAccessibility,
                AccessCheckResult.Accessible,
                AccessCheck.CheckSymbolAccessibility(sym, _sourceModule.ContainingSourceAssembly, useSiteInfo, basesBeingResolved))  ' accessThroughType doesn't matter at assembly level.
        End Function

        Public Overrides ReadOnly Property OptionStrict As OptionStrict
            Get
                Return _sourceModule.Options.OptionStrict
            End Get
        End Property

        Public Overrides ReadOnly Property OptionInfer As Boolean
            Get
                Return _sourceModule.Options.OptionInfer
            End Get
        End Property

        Public Overrides ReadOnly Property OptionExplicit As Boolean
            Get
                Return _sourceModule.Options.OptionExplicit
            End Get
        End Property

        Public Overrides ReadOnly Property OptionCompareText As Boolean
            Get
                Return _sourceModule.Options.OptionCompareText
            End Get
        End Property

        Public Overrides ReadOnly Property CheckOverflow As Boolean
            Get
                Return _sourceModule.Options.CheckOverflow
            End Get
        End Property

        Public Overrides ReadOnly Property QuickAttributeChecker As QuickAttributeChecker
            Get
                Return _sourceModule.QuickAttributeChecker
            End Get
        End Property
    End Class

End Namespace
