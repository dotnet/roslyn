' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class PlaceholderLocalBinder
        Inherits Binder

        Private ReadOnly _containingMethod As MethodSymbol
        Private ReadOnly _allowImplicitDeclarations As Boolean
        Private ReadOnly _implicitDeclarations As Dictionary(Of String, LocalSymbol)

        Friend Sub New(
            aliases As ImmutableArray(Of [Alias]),
            containingMethod As MethodSymbol,
            typeNameDecoder As EETypeNameDecoder,
            allowImplicitDeclarations As Boolean,
            containingBinder As Binder)

            MyBase.New(containingBinder)
            _containingMethod = containingMethod
            _allowImplicitDeclarations = allowImplicitDeclarations

            _implicitDeclarations = New Dictionary(Of String, LocalSymbol)(CaseInsensitiveComparison.Comparer)
            For Each [alias] As [Alias] In aliases
                Dim local = PlaceholderLocalSymbol.Create(
                    typeNameDecoder,
                    containingMethod,
                    [alias])
                _implicitDeclarations.Add(local.Name, local)
            Next
        End Sub

        Friend Overrides Sub LookupInSingleBinder(
            result As LookupResult,
            name As String,
            arity As Integer,
            options As LookupOptions,
            originalBinder As Binder,
            <[In]> <Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))

            If (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly Or LookupOptions.MustNotBeLocalOrParameter)) <> 0 Then
                Return
            End If

            Dim local As LocalSymbol = Nothing
            If _implicitDeclarations.TryGetValue(name, local) Then
                result.SetFrom(CheckViability(local, arity, options, Nothing, useSiteInfo))
            End If
        End Sub

        Public Overrides ReadOnly Property ImplicitVariableDeclarationAllowed As Boolean
            Get
                Return _allowImplicitDeclarations
            End Get
        End Property

        Public Overrides Function DeclareImplicitLocalVariable(nameSyntax As IdentifierNameSyntax, diagnostics As BindingDiagnosticBag) As LocalSymbol
            Debug.Assert(_allowImplicitDeclarations)
            Debug.Assert(_implicitDeclarations IsNot Nothing)

            Dim identifier = nameSyntax.Identifier
            Dim typeChar As String = Nothing
            Dim specialType = GetSpecialTypeForTypeCharacter(identifier.GetTypeCharacter(), typeChar)
            Dim type = Compilation.GetSpecialType(If(specialType = SpecialType.None, SpecialType.System_Object, specialType))
            Dim name = identifier.GetIdentifierText()
            Dim local = LocalSymbol.Create(
                _containingMethod,
                Me,
                identifier,
                LocalDeclarationKind.ImplicitVariable,
                type,
                name)
            _implicitDeclarations.Add(name, local)
            If name.StartsWith("$", StringComparison.Ordinal) Then
                diagnostics.Add(ERRID.ERR_IllegalChar, identifier.GetLocation())
            End If
            Return local
        End Function

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo, options As LookupOptions, originalBinder As Binder)
            Throw New NotImplementedException()
        End Sub

    End Class

End Namespace

