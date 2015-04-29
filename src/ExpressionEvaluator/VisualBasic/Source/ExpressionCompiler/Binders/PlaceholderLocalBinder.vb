' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class PlaceholderLocalBinder
        Inherits Binder

        Private ReadOnly _inspectionContext As InspectionContext
        Private ReadOnly _typeNameDecoder As TypeNameDecoder(Of PEModuleSymbol, TypeSymbol)
        Private ReadOnly _containingMethod As MethodSymbol
        Private ReadOnly _implicitDeclarations As Dictionary(Of String, LocalSymbol)

        Friend Sub New(
            inspectionContext As InspectionContext,
            typeNameDecoder As TypeNameDecoder(Of PEModuleSymbol, TypeSymbol),
            containingMethod As MethodSymbol,
            allowImplicitDeclarations As Boolean,
            containingBinder As Binder)

            MyBase.New(containingBinder)
            _inspectionContext = inspectionContext
            _typeNameDecoder = typeNameDecoder
            _containingMethod = containingMethod
            ' TODO (https://github.com/dotnet/roslyn/issues/878): pass comparer.  Until then, there is no need for a comparer,
            ' since we're going to canonicalize all names.
            _implicitDeclarations = If(allowImplicitDeclarations, New Dictionary(Of String, LocalSymbol)(), Nothing)
        End Sub

        Friend Overrides Sub LookupInSingleBinder(
            result As LookupResult,
            name As String,
            arity As Integer,
            options As LookupOptions,
            originalBinder As Binder,
            <[In]> <Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

            If (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly Or LookupOptions.MustNotBeLocalOrParameter)) <> 0 Then
                Return
            End If

            ' TODO (https://github.com/dotnet/roslyn/issues/878): use name
            Dim canonicalName = Canonicalize(name)

            Dim local As LocalSymbol = Nothing
            If _implicitDeclarations IsNot Nothing Then
                _implicitDeclarations.TryGetValue(canonicalName, local)
            End If

            If local Is Nothing Then
                local = LookupPlaceholder(canonicalName)
                If local Is Nothing Then
                    Return
                End If
            End If

            result.SetFrom(CheckViability(local, arity, options, Nothing, useSiteDiagnostics))
        End Sub

        Public Overrides ReadOnly Property ImplicitVariableDeclarationAllowed As Boolean
            Get
                Return _implicitDeclarations IsNot Nothing
            End Get
        End Property

        Public Overrides Function DeclareImplicitLocalVariable(nameSyntax As IdentifierNameSyntax, diagnostics As DiagnosticBag) As LocalSymbol
            Debug.Assert(_implicitDeclarations IsNot Nothing)

            Dim identifier = nameSyntax.Identifier
            Dim typeChar As String = Nothing
            Dim specialType = GetSpecialTypeForTypeCharacter(identifier.GetTypeCharacter(), typeChar)
            Dim type = Compilation.GetSpecialType(If(specialType = SpecialType.None, SpecialType.System_Object, specialType))
            ' TODO (https://github.com/dotnet/roslyn/issues/878): don't canonicalize name
            Dim canonicalName = Canonicalize(identifier.GetIdentifierText())
            Dim local = LocalSymbol.Create(
                _containingMethod,
                Me,
                identifier,
                LocalDeclarationKind.ImplicitVariable,
                type,
                canonicalName)
            _implicitDeclarations.Add(canonicalName, local)
            If local.Name.StartsWith("$", StringComparison.Ordinal) Then
                diagnostics.Add(ERRID.ERR_IllegalChar, identifier.GetLocation())
            End If
            Return local
        End Function

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo, options As LookupOptions, originalBinder As Binder)
            Throw New NotImplementedException()
        End Sub

        Private Function LookupPlaceholder(canonicalName As String) As PlaceholderLocalSymbol
            Debug.Assert(canonicalName = Canonicalize(canonicalName))

            Dim kind = AliasKind.None
            Dim id As String = Nothing
            Dim index = 0
            If Not PseudoVariableUtilities.TryParseVariableName(canonicalName, caseSensitive:=False, kind:=kind, id:=id, index:=index) Then
                Return Nothing
            End If

            Debug.Assert(id = Canonicalize(id)) ' Since we started from a canonical name.

            Dim typeName = PseudoVariableUtilities.GetTypeName(_inspectionContext, kind, id, index)
            If typeName Is Nothing Then
                Return Nothing
            End If

            ' The old API (GetObjectTypeNameById) doesn't return custom type info,
            ' but the new one (GetAliases) will.
            Return CreatePlaceholderLocal(_typeNameDecoder, _containingMethod, New [Alias](kind, id, id, typeName, customTypeInfo:=Nothing))
        End Function

        Friend Shared Function CreatePlaceholderLocal(
            typeNameDecoder As TypeNameDecoder(Of PEModuleSymbol, TypeSymbol),
            containingMethod As MethodSymbol,
            [alias] As [Alias]) As PlaceholderLocalSymbol

            Dim typeName = [alias].Type
            Debug.Assert(typeName.Length > 0)

            Dim type = typeNameDecoder.GetTypeSymbolForSerializedType(typeName)
            Debug.Assert(type IsNot Nothing)

            Dim id = [alias].FullName
            Select Case [alias].Kind
                Case AliasKind.Exception
                    Return New ExceptionLocalSymbol(containingMethod, id, type, ExpressionCompilerConstants.GetExceptionMethodName)
                Case AliasKind.StowedException
                    Return New ExceptionLocalSymbol(containingMethod, id, type, ExpressionCompilerConstants.GetStowedExceptionMethodName)
                Case AliasKind.ReturnValue
                    Dim index As Integer = 0
                    PseudoVariableUtilities.TryParseReturnValueIndex(id, index)
                    Return New ReturnValueLocalSymbol(containingMethod, id, type, index)
                Case AliasKind.ObjectId
                    Return New ObjectIdLocalSymbol(containingMethod, type, id, isReadOnly:=True)
                Case AliasKind.DeclaredLocal
                    Return New ObjectIdLocalSymbol(containingMethod, type, id, isReadOnly:=False)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue([alias].Kind)
            End Select
        End Function

        Private Shared Function Canonicalize(name As String) As String
            Return CaseInsensitiveComparison.ToLower(name)
        End Function

    End Class

End Namespace

