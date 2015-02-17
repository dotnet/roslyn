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
            _implicitDeclarations = If(allowImplicitDeclarations, New Dictionary(Of String, LocalSymbol), Nothing)
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

            Dim local As LocalSymbol = Nothing
            If _implicitDeclarations IsNot Nothing Then
                _implicitDeclarations.TryGetValue(name, local)
            End If

            If local Is Nothing Then
                local = LookupPlaceholder(name)
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
            Dim local = LocalSymbol.Create(
                _containingMethod,
                Me,
                identifier,
                LocalDeclarationKind.ImplicitVariable,
                type)
            _implicitDeclarations.Add(local.Name, local)
            If local.Name.StartsWith("$", StringComparison.Ordinal) Then
                diagnostics.Add(ERRID.ERR_IllegalChar, identifier.GetLocation())
            End If
            Return local
        End Function

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo, options As LookupOptions, originalBinder As Binder)
            Throw New NotImplementedException()
        End Sub

        Private Function LookupPlaceholder(name As String) As PlaceholderLocalSymbol
            Dim kind = PseudoVariableKind.None
            Dim id As String = Nothing
            Dim index = 0
            If Not PseudoVariableUtilities.TryParseVariableName(name, caseSensitive:=False, kind:=kind, id:=id, index:=index) Then
                Return Nothing
            End If

            Dim typeName = PseudoVariableUtilities.GetTypeName(_inspectionContext, kind, id, index)
            If typeName Is Nothing Then
                Return Nothing
            End If

            Debug.Assert(typeName.Length > 0)

            Dim type = _typeNameDecoder.GetTypeSymbolForSerializedType(typeName)
            Debug.Assert(type IsNot Nothing)

            Select Case kind
                Case PseudoVariableKind.Exception, PseudoVariableKind.StowedException
                    Return New ExceptionLocalSymbol(_containingMethod, id, type)
                Case PseudoVariableKind.ReturnValue
                    Return New ReturnValueLocalSymbol(_containingMethod, id, type, index)
                Case PseudoVariableKind.ObjectId
                    Return New ObjectIdLocalSymbol(_containingMethod, type, id, isReadOnly:=True)
                Case PseudoVariableKind.DeclaredLocal
                    Return New ObjectIdLocalSymbol(_containingMethod, type, id, isReadOnly:=False)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function

    End Class

End Namespace

