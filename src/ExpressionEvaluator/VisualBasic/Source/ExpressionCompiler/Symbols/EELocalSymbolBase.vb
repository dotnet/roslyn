' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend Module LocalSymbolExtensions
        <Extension>
        Friend Function ToOtherMethod(local As LocalSymbol, method As MethodSymbol, typeMap As TypeSubstitution) As LocalSymbol
            Dim l = TryCast(local, EELocalSymbolBase)
            If l IsNot Nothing Then
                Return l.ToOtherMethod(method, typeMap)
            End If
            Dim type = typeMap.SubstituteType(local.Type)
            Return New EELocalSymbol(method, local.Locations, local.Name, -1, local.DeclarationKind, type, local.IsByRef, local.IsPinned, local.CanScheduleToStack)
        End Function
    End Module

    Friend MustInherit Class EELocalSymbolBase
        Inherits LocalSymbol

        Friend Shared ReadOnly NoLocations As ImmutableArray(Of Location) = ImmutableArray.Create(NoLocation.Singleton)

        Friend Sub New(container As Symbol, type As TypeSymbol)
            MyBase.New(container, type)
        End Sub

        Friend Overrides ReadOnly Property IsImportedFromMetadata As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function GetDeclaratorSyntax() As SyntaxNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend MustOverride Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As EELocalSymbolBase

        Friend NotOverridable Overrides ReadOnly Property SynthesizedKind As SynthesizedLocalKind
            Get
                Return SynthesizedLocalKind.UserDefined
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsFunctionValue As Boolean
            Get
                Return DeclarationKind = LocalDeclarationKind.FunctionValue
            End Get
        End Property

        Friend NotOverridable Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            Dim localType As TypeSymbol = Me.Type

            Dim info As UseSiteInfo(Of AssemblySymbol) = DeriveUseSiteInfoFromType(localType)
            If info.DiagnosticInfo IsNot Nothing Then
                Return info
            End If

            If Me.ContainingModule.HasUnifiedReferences Then
                ' If the member is in an assembly with unified references,
                ' we check if its definition depends on a type from a unified reference.
                Dim unificationCheckedTypes As HashSet(Of TypeSymbol) = Nothing
                Return New UseSiteInfo(Of AssemblySymbol)(localType.GetUnificationUseSiteDiagnosticRecursive(Me, unificationCheckedTypes))
            End If

            Return Nothing
        End Function

    End Class
End Namespace

