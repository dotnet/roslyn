' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    ' Handler the parts of binding for member lookup.
    Partial Friend Class Binder

        Friend Sub LookupMember(lookupResult As LookupResult,
                                container As NamespaceOrTypeSymbol,
                                name As String,
                                arity As Integer,
                                options As LookupOptions,
                                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            Debug.Assert(options.IsValid())

            options = BinderSpecificLookupOptions(options)
            MemberLookup.Lookup(lookupResult, container, name, arity, options, Me, useSiteDiagnostics)
        End Sub

        Friend Sub LookupMember(lookupResult As LookupResult,
                                container As TypeSymbol,
                                name As String,
                                arity As Integer,
                                options As LookupOptions,
                                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            Debug.Assert(options.IsValid())

            options = BinderSpecificLookupOptions(options)
            MemberLookup.Lookup(lookupResult, container, name, arity, options, Me, useSiteDiagnostics)
        End Sub

        Friend Sub LookupMember(lookupResult As LookupResult,
                                container As NamespaceSymbol,
                                name As String,
                                arity As Integer,
                                options As LookupOptions,
                                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            Debug.Assert(options.IsValid())

            options = BinderSpecificLookupOptions(options)
            MemberLookup.Lookup(lookupResult, container, name, arity, options, Me, useSiteDiagnostics)
        End Sub

        Friend Sub LookupMemberImmediate(lookupResult As LookupResult,
                                container As NamespaceSymbol,
                                name As String,
                                arity As Integer,
                                options As LookupOptions,
                                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            Debug.Assert(options.IsValid())

            options = BinderSpecificLookupOptions(options)
            MemberLookup.LookupImmediate(lookupResult, container, name, arity, options, Me, useSiteDiagnostics)
        End Sub

        Friend Sub LookupExtensionMethods(
            lookupResult As LookupResult,
            container As TypeSymbol,
            name As String,
            arity As Integer,
            options As LookupOptions,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        )
            Debug.Assert(options.IsValid())
            Debug.Assert(lookupResult.IsClear)
            options = BinderSpecificLookupOptions(options)
            MemberLookup.LookupForExtensionMethods(lookupResult, container, name, arity, options, Me, useSiteDiagnostics)
        End Sub

        Friend Sub LookupMemberInModules(lookupResult As LookupResult,
                                container As NamespaceSymbol,
                                name As String,
                                arity As Integer,
                                options As LookupOptions,
                                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            Debug.Assert(options.IsValid())

            options = BinderSpecificLookupOptions(options)
            MemberLookup.LookupInModules(lookupResult, container, name, arity, options, Me, useSiteDiagnostics)
        End Sub

        Friend Sub AddMemberLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                     container As NamespaceOrTypeSymbol,
                                     options As LookupOptions)
            Debug.Assert(options.IsValid())

            options = BinderSpecificLookupOptions(options)
            MemberLookup.AddLookupSymbolsInfo(nameSet, container, options, Me)
        End Sub

        ' Validates a symbol to check if it 
        ' a) has the right arity 
        ' b) is accessible. (accessThroughType is passed in for protected access checks)
        ' c) matches the lookup options.
        ' A non-empty SingleLookupResult with the result is returned.
        '
        ' For symbols from outside of this compilation the method also checks 
        ' if the symbol is marked with 'Microsoft.VisualBasic.Embedded' or 'Microsoft.CodeAnalysis.Embedded' attributes.
        '
        ' If arity passed in is -1, no arity checks are done.
        Friend Function CheckViability(sym As Symbol,
                                       arity As Integer,
                                       options As LookupOptions,
                                       accessThroughType As TypeSymbol,
                                       <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As SingleLookupResult
            Debug.Assert(sym IsNot Nothing)

            If Not sym.CanBeReferencedByNameIgnoringIllegalCharacters Then
                Return SingleLookupResult.Empty
            End If

            If (options And LookupOptions.LabelsOnly) <> 0 Then
                ' If LabelsOnly is set then the symbol must be a label otherwise return empty
                If options = LookupOptions.LabelsOnly AndAlso sym.Kind = SymbolKind.Label Then
                    Return SingleLookupResult.Good(sym)
                End If

                ' Mixing LabelsOnly with any other flag returns an empty result
                Return SingleLookupResult.Empty
            End If

            If (options And LookupOptions.MustNotBeReturnValueVariable) <> 0 Then
                '§11.4.4 Simple Name Expressions
                '    If the identifier matches a local variable, the local variable matched is 
                '    the implicit function or Get accessor return local variable, and the expression
                '    is part of an invocation expression, invocation statement, or an AddressOf 
                '    expression, then no match occurs and resolution continues.
                '
                ' LookupOptions.MustNotBeReturnValueVariable is set if "the expression
                ' is part of an invocation expression, invocation statement, or an AddressOf 
                ' expression", and we then skip return value variables.
                ' We'll always bind to the containing method or property instead further on in the lookup process.
                If sym.Kind = SymbolKind.Local AndAlso DirectCast(sym, LocalSymbol).IsFunctionValue Then
                    Return SingleLookupResult.Empty
                End If
            End If

            Dim unwrappedSym = sym
            Dim asAlias = TryCast(sym, AliasSymbol)
            If asAlias IsNot Nothing Then
                unwrappedSym = asAlias.Target
            End If

            ' Check for external symbols marked with 'Microsoft.VisualBasic.Embedded' or 'Microsoft.CodeAnalysis.Embedded' attributes
            If unwrappedSym.ContainingModule IsNot Me.ContainingModule Then
                If unwrappedSym.IsHiddenByVisualBasicEmbeddedAttribute() OrElse unwrappedSym.IsHiddenByCodeAnalysisEmbeddedAttribute() Then
                    Return SingleLookupResult.Empty
                End If
            End If

            If unwrappedSym.Kind = SymbolKind.NamedType AndAlso unwrappedSym.EmbeddedSymbolKind = EmbeddedSymbolKind.EmbeddedAttribute AndAlso
                    Me.SyntaxTree IsNot Nothing AndAlso Me.SyntaxTree.GetEmbeddedKind = EmbeddedSymbolKind.None Then
                ' Only allow direct access to Microsoft.VisualBasic.Embedded attribute
                ' from user code if current compilation embeds Vb Core
                If Not Me.Compilation.Options.EmbedVbCoreRuntime Then
                    Return SingleLookupResult.Empty
                End If
            End If

            ' Do arity checking, unless specifically asked not to.
            ' Only types and namespaces in VB shadow by arity. All other members shadow 
            ' regardless of arity. So, we only check arity on types.
            If arity <> -1 Then
                Select Case sym.Kind
                    Case SymbolKind.NamedType, SymbolKind.ErrorType
                        Dim actualArity As Integer = DirectCast(sym, NamedTypeSymbol).Arity
                        If actualArity <> arity Then
                            Return SingleLookupResult.WrongArity(sym, WrongArityErrid(actualArity, arity))
                        End If

                    Case SymbolKind.TypeParameter, SymbolKind.Namespace
                        If arity <> 0 Then ' type parameters and namespaces are always arity 0
                            Return SingleLookupResult.WrongArity(unwrappedSym, WrongArityErrid(0, arity))
                        End If

                    Case SymbolKind.Alias
                        ' Since raw generics cannot be imported, the import aliases would always refer to
                        ' constructed types when referring to generics. So any other generic arity besides
                        ' -1 or 0 are invalid.
                        If arity <> 0 Then ' aliases are always arity 0, but error refers to the target
                            ' Note, Dev11 doesn't stop lookup in case of arity mismatch for an alias.
                            Return SingleLookupResult.WrongArity(unwrappedSym, WrongArityErrid(0, arity))
                        End If

                    Case SymbolKind.Method
                        ' Unlike types and namespaces, we always stop looking if we find a method with the right name but wrong arity.

                        ' The arity matching rules for methods are customizable for the LookupOptions; when binding expressions 
                        ' we always pass AllMethodsOfAnyArity and allow overload resolution to filter methods. The other flags
                        ' are for binding API scenarios.
                        Dim actualArity As Integer = DirectCast(sym, MethodSymbol).Arity
                        If actualArity <> arity AndAlso
                           Not ((options And LookupOptions.AllMethodsOfAnyArity) <> 0) Then
                            Return SingleLookupResult.WrongArityAndStopLookup(sym, WrongArityErrid(actualArity, arity))
                        End If

                    Case Else
                        ' Unlike types and namespace, we stop looking if we find other symbols with wrong arity.
                        ' All these symbols have arity 0.
                        If arity <> 0 Then
                            Return SingleLookupResult.WrongArityAndStopLookup(sym, WrongArityErrid(0, arity))
                        End If
                End Select
            End If

            If (options And LookupOptions.IgnoreAccessibility) = 0 Then
                Dim accessCheckResult = CheckAccessibility(unwrappedSym, useSiteDiagnostics, If((options And LookupOptions.UseBaseReferenceAccessibility) <> 0, Nothing, accessThroughType))
                ' Check if we are in 'MyBase' resolving mode and we need to ignore 'accessThroughType' to make protected members accessed
                If accessCheckResult <> VisualBasic.AccessCheckResult.Accessible Then
                    Return SingleLookupResult.Inaccessible(sym, GetInaccessibleErrorInfo(sym))
                End If
            End If

            If (options And Global.Microsoft.CodeAnalysis.VisualBasic.LookupOptions.MustNotBeInstance) <> 0 AndAlso sym.IsInstanceMember Then
                Return Global.Microsoft.CodeAnalysis.VisualBasic.SingleLookupResult.MustNotBeInstance(sym, Global.Microsoft.CodeAnalysis.VisualBasic.ERRID.ERR_ObjectReferenceNotSupplied)
            ElseIf (options And Global.Microsoft.CodeAnalysis.VisualBasic.LookupOptions.MustBeInstance) <> 0 AndAlso Not sym.IsInstanceMember Then
                Return Global.Microsoft.CodeAnalysis.VisualBasic.SingleLookupResult.MustBeInstance(sym) ' there is no error message for this 
            End If

            Return SingleLookupResult.Good(sym)
        End Function

        Friend Function GetInaccessibleErrorInfo(sym As Symbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As DiagnosticInfo
            CheckAccessibility(sym, useSiteDiagnostics) ' For diagnostics.
            Return GetInaccessibleErrorInfo(sym)
        End Function

        Friend Function GetInaccessibleErrorInfo(sym As Symbol) As DiagnosticInfo
            Dim unwrappedSym = sym
            Dim asAlias = TryCast(sym, AliasSymbol)
            If asAlias IsNot Nothing Then
                unwrappedSym = asAlias.Target
            ElseIf sym.Kind = SymbolKind.Method Then
                sym = DirectCast(sym, MethodSymbol).ConstructedFrom
            End If

            Dim diagInfo As DiagnosticInfo

            ' for inaccessible members (in e.g. AddressOf expressions, DEV10 shows a ERR_InaccessibleMember3 diagnostic)
            ' TODO maybe this condition needs to be adjusted to be shown in cases of e.g. inaccessible properties
            If unwrappedSym.Kind = SymbolKind.Method AndAlso unwrappedSym.ContainingSymbol IsNot Nothing Then
                diagInfo = New BadSymbolDiagnostic(sym,
                                                   ERRID.ERR_InaccessibleMember3,
                                                   sym.ContainingSymbol.Name,
                                                   sym,
                                                   AccessCheck.GetAccessibilityForErrorMessage(sym, Me.Compilation.Assembly))
            Else
                diagInfo = New BadSymbolDiagnostic(sym,
                                                   ERRID.ERR_InaccessibleSymbol2,
                                                   CustomSymbolDisplayFormatter.QualifiedName(sym),
                                                   AccessCheck.GetAccessibilityForErrorMessage(sym, sym.ContainingAssembly))
            End If

            Debug.Assert(diagInfo.Severity = DiagnosticSeverity.Error)
            Return diagInfo
        End Function

        ''' <summary>
        ''' Used by Add*LookupSymbolsInfo* to determine whether the symbol is of interest.
        ''' Distinguish from <see cref="CheckViability"/>, which performs an analogous task for LookupSymbols*.
        ''' </summary>
        ''' <remarks>
        ''' Does not consider <see cref="Symbol.CanBeReferencedByName"/> - that is left to the caller.
        ''' </remarks>
        Friend Function CanAddLookupSymbolInfo(sym As Symbol,
                                                    options As LookupOptions,
                                                    nameSet As LookupSymbolsInfo,
                                                    accessThroughType As TypeSymbol) As Boolean
            Debug.Assert(sym IsNot Nothing)

            If Not nameSet.CanBeAdded(sym.Name) Then
                Return False
            End If

            Dim singleResult = CheckViability(sym, -1, options, accessThroughType, useSiteDiagnostics:=Nothing)

            If (options And LookupOptions.MethodsOnly) <> 0 AndAlso
               sym.Kind <> SymbolKind.Method Then
                Return False
            End If

            If singleResult.IsGoodOrAmbiguous Then
                ' Its possible there is an error (ambiguity, wrong arity) associated with result.
                ' We still return true here, because binding finds that symbol and doesn't continue.

                ' NOTE: We're going to let the SemanticModel check for symbols that can't be
                ' referenced by name.  That way, it can either filter them or not, depending
                ' on whether a name was passed to LookupSymbols.
                Return True
            End If

            Return False
        End Function

        ' return the error id for mismatched arity.
        Private Shared Function WrongArityErrid(actualArity As Integer, arity As Integer) As ERRID
            If actualArity < arity Then
                If actualArity = 0 Then
                    Return ERRID.ERR_TypeOrMemberNotGeneric1
                Else
                    Return ERRID.ERR_TooManyGenericArguments1
                End If
            Else
                Debug.Assert(actualArity > arity, "arities shouldn't match")
                Return ERRID.ERR_TooFewGenericArguments1
            End If
        End Function

        ''' <summary>
        ''' This class handles binding of members of namespaces and types.
        ''' The key member is Lookup, which handles looking up a name
        ''' in a namespace or type, by name and arity, and produces a 
        ''' lookup result. 
        ''' </summary>
        Private Class MemberLookup
            ''' <summary>
            ''' Lookup a member name in a namespace or type, returning a LookupResult that
            ''' summarizes the results of the lookup. See LookupResult structure for a detailed
            ''' discussing of the meaning of the results. The supplied binder is used for accessibility
            ''' checked and base class suppression.
            ''' </summary>
            Public Shared Sub Lookup(lookupResult As LookupResult,
                                     container As NamespaceOrTypeSymbol,
                                     name As String,
                                     arity As Integer,
                                     options As LookupOptions,
                                     binder As Binder,
                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
                If container.IsNamespace Then
                    Lookup(lookupResult, DirectCast(container, NamespaceSymbol), name, arity, options, binder, useSiteDiagnostics)
                Else
                    Lookup(lookupResult, DirectCast(container, TypeSymbol), name, arity, options, binder, useSiteDiagnostics)
                End If
            End Sub

            ' Lookup all the names available on the given container, that match the given lookup options.
            ' The supplied binder is used for accessibility checking.
            Public Shared Sub AddLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                    container As NamespaceOrTypeSymbol,
                                                    options As LookupOptions,
                                                    binder As Binder)
                If container.IsNamespace Then
                    AddLookupSymbolsInfo(nameSet, DirectCast(container, NamespaceSymbol), options, binder)
                Else
                    AddLookupSymbolsInfo(nameSet, DirectCast(container, TypeSymbol), options, binder)
                End If
            End Sub

            ''' <summary>
            ''' Lookup a member name in a namespace, returning a LookupResult that
            ''' summarizes the results of the lookup. See LookupResult structure for a detailed
            ''' discussing of the meaning of the results. The supplied binder is used for accessibility
            ''' checked and base class suppression.
            ''' </summary>
            Public Shared Sub Lookup(lookupResult As LookupResult,
                                     container As NamespaceSymbol,
                                     name As String,
                                     arity As Integer,
                                     options As LookupOptions,
                                     binder As Binder,
                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

                Debug.Assert(lookupResult.IsClear)

                LookupImmediate(lookupResult, container, name, arity, options, binder, useSiteDiagnostics)

                ' Result in the namespace takes precedence over results in containing modules.
                If lookupResult.StopFurtherLookup Then
                    Return
                End If

                Dim currentResult = LookupResult.GetInstance()

                LookupInModules(currentResult, container, name, arity, options, binder, useSiteDiagnostics)
                lookupResult.MergeAmbiguous(currentResult, s_ambiguousInModuleError)

                currentResult.Free()
            End Sub

            ''' <summary>
            ''' Lookup an immediate (without descending into modules) member name in a namespace, 
            ''' returning a LookupResult that summarizes the results of the lookup. 
            ''' See LookupResult structure for a detailed discussion of the meaning of the results. 
            ''' The supplied binder is used for accessibility checks and base class suppression.
            ''' </summary>
            Public Shared Sub LookupImmediate(lookupResult As LookupResult,
                                     container As NamespaceSymbol,
                                     name As String,
                                     arity As Integer,
                                     options As LookupOptions,
                                     binder As Binder,
                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

                Debug.Assert(lookupResult.IsClear)

                Dim sourceModule = binder.Compilation.SourceModule

                ' Handle a case of being able to refer to System.Int32 through System.Integer.
                ' Same for other intrinsic types with intrinsic name different from emitted name.
                If (options And LookupOptions.AllowIntrinsicAliases) <> 0 AndAlso arity = 0 Then
                    Dim containingNs = container.ContainingNamespace

                    If containingNs IsNot Nothing AndAlso containingNs.IsGlobalNamespace AndAlso CaseInsensitiveComparison.Equals(container.Name, MetadataHelpers.SystemString) Then
                        Dim specialType = GetTypeForIntrinsicAlias(name)

                        If specialType <> SpecialType.None Then
                            Dim candidate = binder.Compilation.GetSpecialType(specialType)

                            ' Intrinsic alias works only if type is available
                            If Not candidate.IsErrorType() Then
                                lookupResult.MergeMembersOfTheSameNamespace(binder.CheckViability(candidate, arity, options, Nothing, useSiteDiagnostics), sourceModule, options)
                            End If
                        End If
                    End If
                End If

#If DEBUG Then
                Dim haveSeenNamespace As Boolean = False
#End If

                For Each sym In container.GetMembers(name)
#If DEBUG Then
                    If sym.Kind = SymbolKind.Namespace Then
                        Debug.Assert(Not haveSeenNamespace, "Expected namespaces to be merged into a single symbol.")
                        haveSeenNamespace = True
                    End If
#End If

                    Dim currentResult As SingleLookupResult = binder.CheckViability(sym, arity, options, Nothing, useSiteDiagnostics)

                    lookupResult.MergeMembersOfTheSameNamespace(currentResult, sourceModule, options)
                Next
            End Sub

            Public Shared Function GetTypeForIntrinsicAlias(possibleAlias As String) As SpecialType
                Dim aliasAsKeyword As SyntaxKind = SyntaxFacts.GetKeywordKind(possibleAlias)

                Select Case aliasAsKeyword
                    Case SyntaxKind.DateKeyword
                        Return SpecialType.System_DateTime
                    Case SyntaxKind.UShortKeyword
                        Return SpecialType.System_UInt16
                    Case SyntaxKind.ShortKeyword
                        Return SpecialType.System_Int16
                    Case SyntaxKind.UIntegerKeyword
                        Return SpecialType.System_UInt32
                    Case SyntaxKind.IntegerKeyword
                        Return SpecialType.System_Int32
                    Case SyntaxKind.ULongKeyword
                        Return SpecialType.System_UInt64
                    Case SyntaxKind.LongKeyword
                        Return SpecialType.System_Int64
                    Case Else
                        Return SpecialType.None
                End Select
            End Function

            ''' <summary>
            ''' Lookup a member name in modules of a namespace, 
            ''' returning a LookupResult that summarizes the results of the lookup. 
            ''' See LookupResult structure for a detailed discussion of the meaning of the results. 
            ''' The supplied binder is used for accessibility checks and base class suppression.
            ''' </summary>
            Public Shared Sub LookupInModules(lookupResult As LookupResult,
                                     container As NamespaceSymbol,
                                     name As String,
                                     arity As Integer,
                                     options As LookupOptions,
                                     binder As Binder,
                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

                Debug.Assert(lookupResult.IsClear)
                Dim firstModule As Boolean = True
                Dim sourceModule = binder.Compilation.SourceModule

                ' NOTE: while looking up the symbol in modules we should ignore base class
                options = options Or LookupOptions.IgnoreExtensionMethods Or LookupOptions.NoBaseClassLookup
                Dim currentResult As LookupResult = Nothing

                ' Next, do a lookup in each contained module and merge the results.
                For Each containedModule As NamedTypeSymbol In container.GetModuleMembers()
                    If firstModule Then
                        Lookup(lookupResult, containedModule, name, arity, options, binder, useSiteDiagnostics)
                        firstModule = False
                    Else
                        If currentResult Is Nothing Then
                            currentResult = LookupResult.GetInstance()
                        Else
                            currentResult.Clear()
                        End If

                        Lookup(currentResult, containedModule, name, arity, options, binder, useSiteDiagnostics)

                        ' Symbols in source take priority over symbols in a referenced assembly.
                        If currentResult.StopFurtherLookup AndAlso currentResult.Symbols.Count > 0 AndAlso
                           lookupResult.StopFurtherLookup AndAlso lookupResult.Symbols.Count > 0 Then

                            Dim currentFromSource = currentResult.Symbols(0).ContainingModule Is sourceModule
                            Dim contenderFromSource = lookupResult.Symbols(0).ContainingModule Is sourceModule

                            If currentFromSource Then
                                If Not contenderFromSource Then
                                    ' current is better
                                    lookupResult.SetFrom(currentResult)
                                    Continue For
                                End If

                            ElseIf contenderFromSource Then
                                ' contender is better
                                Continue For
                            End If
                        End If

                        lookupResult.MergeAmbiguous(currentResult, s_ambiguousInModuleError)
                    End If
                Next

                currentResult?.Free()
            End Sub

            Private Shared Sub AddLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                           container As NamespaceSymbol,
                                           options As LookupOptions,
                                           binder As Binder)
                ' Add names from the namespace
                For Each sym In container.GetMembersUnordered()
                    ' UNDONE: filter by options
                    If binder.CanAddLookupSymbolInfo(sym, options, nameSet, Nothing) Then
                        nameSet.AddSymbol(sym, sym.Name, sym.GetArity())
                    End If
                Next

                ' Next, add names from each contained module.
                For Each containedModule As NamedTypeSymbol In container.GetModuleMembers()
                    AddLookupSymbolsInfo(nameSet, containedModule, options, binder)
                Next
            End Sub

            ' Create a diagnostic for ambiguous names in multiple modules.
            Private Shared ReadOnly s_ambiguousInModuleError As Func(Of ImmutableArray(Of Symbol), AmbiguousSymbolDiagnostic) =
                Function(syms As ImmutableArray(Of Symbol)) As AmbiguousSymbolDiagnostic
                    Dim name As String = syms(0).Name
                    Dim deferredFormattedList As New FormattedSymbolList(syms.Select(Function(sym) sym.ContainingType))

                    Return New AmbiguousSymbolDiagnostic(ERRID.ERR_AmbiguousInModules2, syms, name, deferredFormattedList)
                End Function

            ''' <summary>
            ''' Lookup a member name in a type, returning a LookupResult that
            ''' summarizes the results of the lookup. See LookupResult structure for a detailed
            ''' discussing of the meaning of the results. The supplied binder is used for accessibility
            ''' checked and base class suppression.
            ''' </summary>
            Friend Shared Sub Lookup(lookupResult As LookupResult,
                                      type As TypeSymbol,
                                      name As String,
                                      arity As Integer,
                                      options As LookupOptions,
                                      binder As Binder,
                                      <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
                Debug.Assert(lookupResult.IsClear)

                Select Case type.TypeKind
                    Case TypeKind.Class, TypeKind.Module, TypeKind.Structure, TypeKind.Delegate, TypeKind.Array, TypeKind.Enum
                        LookupInClass(lookupResult, type, name, arity, options, type, binder, useSiteDiagnostics)

                    Case TypeKind.Submission
                        LookupInSubmissions(lookupResult, type, name, arity, options, binder, useSiteDiagnostics)

                    Case TypeKind.Interface
                        LookupInInterface(lookupResult, DirectCast(type, NamedTypeSymbol), name, arity, options, binder, useSiteDiagnostics)

                    Case TypeKind.TypeParameter
                        LookupInTypeParameter(lookupResult, DirectCast(type, TypeParameterSymbol), name, arity, options, binder, useSiteDiagnostics)

                    Case TypeKind.Error
                        ' Error types have no members.
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(type.TypeKind)
                End Select
            End Sub

            Private Shared Sub AddLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                     container As TypeSymbol,
                                                     options As LookupOptions,
                                                     binder As Binder)
                Select Case container.TypeKind
                    Case TypeKind.Class, TypeKind.Structure, TypeKind.Delegate, TypeKind.Array, TypeKind.Enum
                        AddLookupSymbolsInfoInClass(nameSet, container, options, binder)

                    Case TypeKind.Module
                        AddLookupSymbolsInfoInClass(nameSet, container, options Or LookupOptions.NoBaseClassLookup, binder)

                    Case TypeKind.Submission
                        AddLookupSymbolsInfoInSubmissions(nameSet, container, options, binder)

                    Case TypeKind.Interface
                        AddLookupSymbolsInfoInInterface(nameSet, DirectCast(container, NamedTypeSymbol), options, binder)

                    Case TypeKind.TypeParameter
                        AddLookupSymbolsInfoInTypeParameter(nameSet, DirectCast(container, TypeParameterSymbol), options, binder)

                    Case TypeKind.Error
                        ' Error types have no members.
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(container.TypeKind)
                End Select
            End Sub

            ''' <summary>
            ''' Lookup a member name in a module, class, struct, enum, or delegate, returning a LookupResult that
            ''' summarizes the results of the lookup. See LookupResult structure for a detailed
            ''' discussing of the meaning of the results. The supplied binder is used for accessibility
            ''' checks and base class suppression.
            ''' </summary>
            Private Shared Sub LookupInClass(result As LookupResult,
                                             container As TypeSymbol,
                                             name As String,
                                             arity As Integer,
                                             options As LookupOptions,
                                             accessThroughType As TypeSymbol,
                                             binder As Binder,
                                             <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
                Debug.Assert(result.IsClear)

                Dim methodsOnly As Boolean = CheckAndClearMethodsOnlyOption(options)

                ' Lookup proceeds up the base class chain.
                Dim currentType = container
                Dim currentResult = LookupResult.GetInstance()

                Do
                    Dim hitNonoverloadingSymbol As Boolean = False

                    LookupWithoutInheritance(currentResult, currentType, name, arity, options, accessThroughType, binder, useSiteDiagnostics)
                    If result.IsGoodOrAmbiguous AndAlso currentResult.IsGoodOrAmbiguous AndAlso Not LookupResult.CanOverload(result.Symbols(0), currentResult.Symbols(0)) Then
                        ' We hit another good symbol that can't overload this one. That doesn't affect the lookup result, but means we have to stop
                        ' looking for more members. See bug #14078 for example.
                        hitNonoverloadingSymbol = True
                    End If
                    result.MergeOverloadedOrPrioritized(currentResult, True)

                    ' If the type is from a winmd file and implements any of the special WinRT collection
                    ' projections, then we may need to add projected interface members
                    Dim namedType = TryCast(currentType, NamedTypeSymbol)
                    If namedType IsNot Nothing AndAlso namedType.ShouldAddWinRTMembers Then
                        FindWinRTMembers(result,
                                         namedType,
                                         binder,
                                         useSiteDiagnostics,
                                         lookupMembersNotDefaultProperties:=True,
                                         name:=name,
                                         arity:=arity,
                                         options:=options)
                    End If

                    If hitNonoverloadingSymbol Then
                        Exit Do ' still do extension methods.
                    End If

                    If result.StopFurtherLookup Then
                        ' If we found a non-overloadable symbol, we can stop now. Note that even if we find a method without the Overloads
                        ' modifier, we cannot stop because we need to check for extension methods.
                        If result.HasSymbol Then
                            If Not result.Symbols.First.IsOverloadable Then
                                If methodsOnly Then
                                    Exit Do ' Need to look for extension methods.
                                End If

                                currentResult.Free()
                                Return
                            End If
                        End If
                    End If

                    ' Go to base type, unless that would case infinite recursion or the options or the binder
                    ' disallows it.
                    If (options And LookupOptions.NoBaseClassLookup) <> 0 OrElse binder.IgnoreBaseClassesInLookup Then
                        currentType = Nothing
                    Else
                        currentType = currentType.GetDirectBaseTypeWithDefinitionUseSiteDiagnostics(binder.BasesBeingResolved, useSiteDiagnostics)
                    End If

                    If currentType Is Nothing Then
                        Exit Do
                    End If

                    currentResult.Clear()
                Loop

                currentResult.Free()

                ClearLookupResultIfNotMethods(methodsOnly, result)
                LookupForExtensionMethodsIfNeedTo(result, container, name, arity, options, binder, useSiteDiagnostics)
            End Sub

            Public Delegate Sub WinRTLookupDelegate(iface As NamedTypeSymbol,
                                                     binder As Binder,
                                                     result As LookupResult,
                                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

            ''' <summary>
            ''' This function generalizes the idea of producing a set of non-conflicting
            ''' WinRT members of a given type based on the results of some arbitrary lookup
            ''' closure (which produces a LookupResult signifying success as IsGood).
            '''
            ''' A non-conflicting WinRT member lookup looks for all members of projected
            ''' WinRT interfaces which are implemented by a given type, discarding any 
            ''' which have equal signatures.
            ''' 
            ''' If <paramref name="lookupMembersNotDefaultProperties" /> is true then
            ''' this function lookups up members with the given <paramref name="name" />,
            ''' <paramref name="arity" />, and <paramref name="options" />. Otherwise, it looks for default properties.
            ''' </summary>
            Private Shared Sub FindWinRTMembers(result As LookupResult,
                                                type As NamedTypeSymbol,
                                                binder As Binder,
                                                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                lookupMembersNotDefaultProperties As Boolean,
                                                Optional name As String = Nothing,
                                                Optional arity As Integer = -1,
                                                Optional options As LookupOptions = Nothing)
                ' If we have no conflict with existing members, we also have to check
                ' if we have a conflict with other interface members. An example would be
                ' a type which implements both IIterable (IEnumerable) and IMap 
                ' (IDictionary).There are two different GetEnumerator methods from each
                ' interface. Thus, we don't know which method to choose. The solution?
                ' Don't add any GetEnumerator method.

                Dim comparer = MemberSignatureComparer.WinRTComparer

                Dim allMembers = New HashSet(Of Symbol)(comparer)
                Dim conflictingMembers = New HashSet(Of Symbol)(comparer)

                ' Add all viable members from type lookup
                If result.IsGood Then
                    For Each sym In result.Symbols
                        ' Fields can't be present in the HashSet because they can't be compared
                        ' with a MemberSignatureComparer
                        ' TODO: Add field support in the C# and VB member comparers and then
                        ' delete this check
                        If sym.Kind <> SymbolKind.Field Then
                            allMembers.Add(sym)
                        End If
                    Next
                End If

                Dim tmp = LookupResult.GetInstance()

                ' Dev11 searches all declared and undeclared base interfaces
                For Each iface In type.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                    If IsWinRTProjectedInterface(iface, binder.Compilation) Then
                        If lookupMembersNotDefaultProperties Then
                            Debug.Assert(name IsNot Nothing)
                            LookupWithoutInheritance(tmp,
                                                     iface,
                                                     name,
                                                     arity,
                                                     options,
                                                     iface,
                                                     binder,
                                                     useSiteDiagnostics)
                        Else
                            LookupDefaultPropertyInSingleType(tmp,
                                                              iface,
                                                              iface,
                                                              binder,
                                                              useSiteDiagnostics)
                        End If
                        ' only add viable members
                        If tmp.IsGood Then
                            For Each sym In tmp.Symbols
                                If Not allMembers.Add(sym) Then
                                    conflictingMembers.Add(sym)
                                End If
                            Next
                        End If
                        tmp.Clear()
                    End If
                Next

                tmp.Free()
                If result.IsGood Then
                    For Each sym In result.Symbols
                        If sym.Kind <> SymbolKind.Field Then
                            allMembers.Remove(sym)
                            conflictingMembers.Remove(sym)
                        End If
                    Next
                End If

                For Each sym In allMembers
                    If Not conflictingMembers.Contains(sym) Then
                        ' since we only added viable members, every lookupresult should be viable
                        result.MergeOverloadedOrPrioritized(
                            New SingleLookupResult(LookupResultKind.Good, sym, Nothing),
                            checkIfCurrentHasOverloads:=False)
                    End If
                Next
            End Sub

            Private Shared Function IsWinRTProjectedInterface(iFace As NamedTypeSymbol, compilation As VisualBasicCompilation) As Boolean
                Dim idictSymbol = compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IDictionary_KV)
                Dim iroDictSymbol = compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IReadOnlyDictionary_KV)
                Dim iListSymbol = compilation.GetWellKnownType(WellKnownType.System_Collections_IList)
                Dim iCollectionSymbol = compilation.GetWellKnownType(WellKnownType.System_Collections_ICollection)
                Dim inccSymbol = compilation.GetWellKnownType(WellKnownType.System_Collections_Specialized_INotifyCollectionChanged)
                Dim inpcSymbol = compilation.GetWellKnownType(WellKnownType.System_ComponentModel_INotifyPropertyChanged)

                Dim iFaceOriginal = iFace.OriginalDefinition
                Dim iFaceSpecial = iFaceOriginal.SpecialType
                ' Types match the list given in dev11 IMPORTER::GetWindowsRuntimeInterfacesToFake
                Return iFaceSpecial = SpecialType.System_Collections_Generic_IEnumerable_T OrElse
                       iFaceSpecial = SpecialType.System_Collections_Generic_IList_T OrElse
                       iFaceSpecial = SpecialType.System_Collections_Generic_ICollection_T OrElse
                       TypeSymbol.Equals(iFaceOriginal, idictSymbol, TypeCompareKind.ConsiderEverything) OrElse
                       iFaceSpecial = SpecialType.System_Collections_Generic_IReadOnlyList_T OrElse
                       iFaceSpecial = SpecialType.System_Collections_Generic_IReadOnlyCollection_T OrElse
                       TypeSymbol.Equals(iFaceOriginal, iroDictSymbol, TypeCompareKind.ConsiderEverything) OrElse
                       iFaceSpecial = SpecialType.System_Collections_IEnumerable OrElse
                       TypeSymbol.Equals(iFaceOriginal, iListSymbol, TypeCompareKind.ConsiderEverything) OrElse
                       TypeSymbol.Equals(iFaceOriginal, iCollectionSymbol, TypeCompareKind.ConsiderEverything) OrElse
                       TypeSymbol.Equals(iFaceOriginal, inccSymbol, TypeCompareKind.ConsiderEverything) OrElse
                       TypeSymbol.Equals(iFaceOriginal, inpcSymbol, TypeCompareKind.ConsiderEverything)
            End Function


            ''' <summary>
            ''' Lookup a member name in a submission chain.
            ''' </summary>
            ''' <remarks>
            ''' We start with the current submission class and walk the submission chain back to the first submission.
            ''' The search has two phases
            ''' 1) We are looking for any symbol matching the given name, arity, and options. If we don't find any the search is over.
            '''    If we find an overloadable symbol(s) (a method or a property) we start looking for overloads of this kind 
            '''    (lookingForOverloadsOfKind) of symbol in phase 2.
            ''' 2) If a visited submission contains a matching member of a kind different from lookingForOverloadsOfKind we stop 
            '''    looking further. Otherwise, if we find viable overload(s) we add them into the result. Overloads modifier is ignored.
            ''' </remarks>
            Private Shared Sub LookupInSubmissions(result As LookupResult,
                                                   submissionClass As TypeSymbol,
                                                   name As String,
                                                   arity As Integer,
                                                   options As LookupOptions,
                                                   binder As Binder,
                                                   <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
                Debug.Assert(result.IsClear)
                Dim submissionSymbols = LookupResult.GetInstance()
                Dim nonViable = LookupResult.GetInstance()
                Dim lookingForOverloadsOfKind As SymbolKind? = Nothing

                Dim submission = binder.Compilation
                Do
                    submissionSymbols.Clear()

                    If submission.ScriptClass IsNot Nothing Then
                        LookupWithoutInheritance(submissionSymbols, submission.ScriptClass, name, arity, options, submissionClass, binder, useSiteDiagnostics)
                    End If

                    ' TODO (tomat): import aliases

                    If lookingForOverloadsOfKind Is Nothing Then
                        If Not submissionSymbols.IsGoodOrAmbiguous Then
                            ' skip non-viable members, but remember them in case no viable members are found in previous submissions:
                            nonViable.MergePrioritized(submissionSymbols)

                            submission = submission.PreviousSubmission
                            Continue Do
                        End If

                        ' always overload (ignore Overloads modifier):
                        result.MergeOverloadedOrPrioritized(submissionSymbols, checkIfCurrentHasOverloads:=False)

                        Dim first = submissionSymbols.Symbols.First
                        If Not first.IsOverloadable Then
                            Exit Do
                        End If

                        ' we are now looking for any kind of member regardless of the original binding restrictions:
                        options = options And Not LookupOptions.NamespacesOrTypesOnly
                        lookingForOverloadsOfKind = first.Kind
                    Else
                        ' found a member we are not looking for - the overload set is final now
                        If submissionSymbols.HasSymbol AndAlso submissionSymbols.Symbols.First.Kind <> lookingForOverloadsOfKind.Value Then
                            Exit Do
                        End If

                        ' found a viable overload
                        If submissionSymbols.IsGoodOrAmbiguous Then
                            ' merge overloads
                            Debug.Assert(result.Symbols.All(Function(s) s.IsOverloadable))

                            ' always overload (ignore Overloads modifier):
                            result.MergeOverloadedOrPrioritized(submissionSymbols, checkIfCurrentHasOverloads:=False)
                        End If
                    End If

                    submission = submission.PreviousSubmission
                Loop Until submission Is Nothing

                If Not result.HasSymbol Then
                    result.SetFrom(nonViable)
                End If

                ' TODO (tomat): extension methods

                submissionSymbols.Free()
                nonViable.Free()
            End Sub

            Public Shared Sub LookupDefaultProperty(result As LookupResult, container As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
                Select Case container.TypeKind
                    Case TypeKind.Class, TypeKind.Module, TypeKind.Structure
                        LookupDefaultPropertyInClass(result, DirectCast(container, NamedTypeSymbol), binder, useSiteDiagnostics)

                    Case TypeKind.Interface
                        LookupDefaultPropertyInInterface(result, DirectCast(container, NamedTypeSymbol), binder, useSiteDiagnostics)

                    Case TypeKind.TypeParameter
                        LookupDefaultPropertyInTypeParameter(result, DirectCast(container, TypeParameterSymbol), binder, useSiteDiagnostics)

                End Select
            End Sub

            Private Shared Sub LookupDefaultPropertyInClass(
                result As LookupResult,
                type As NamedTypeSymbol,
                binder As Binder,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                Debug.Assert(type.IsClassType OrElse type.IsModuleType OrElse type.IsStructureType OrElse type.IsDelegateType)
                Dim accessThroughType As NamedTypeSymbol = type

                While type IsNot Nothing
                    If LookupDefaultPropertyInSingleType(result, type, accessThroughType, binder, useSiteDiagnostics) Then
                        Return
                    End If

                    ' If this is a WinRT type, we should also look for default properties in the
                    ' implemented projected interfaces
                    If type.ShouldAddWinRTMembers Then
                        FindWinRTMembers(result,
                                         type,
                                         binder,
                                         useSiteDiagnostics,
                                         lookupMembersNotDefaultProperties:=False)
                        If result.IsGood Then
                            Return
                        End If
                    End If

                    type = type.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                End While

            End Sub


            ' See Semantics::LookupDefaultPropertyInInterface.
            Private Shared Sub LookupDefaultPropertyInInterface(
                result As LookupResult,
                [interface] As NamedTypeSymbol,
                binder As Binder,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                Debug.Assert([interface].IsInterfaceType)

                If LookupDefaultPropertyInSingleType(result, [interface], [interface], binder, useSiteDiagnostics) Then
                    Return
                End If

                For Each baseInterface In [interface].InterfacesNoUseSiteDiagnostics
                    baseInterface.OriginalDefinition.AddUseSiteDiagnostics(useSiteDiagnostics)

                    LookupDefaultPropertyInBaseInterface(result, baseInterface, binder, useSiteDiagnostics)
                    If result.HasDiagnostic Then
                        Return
                    End If
                Next
            End Sub

            Private Shared Sub LookupDefaultPropertyInTypeParameter(result As LookupResult, typeParameter As TypeParameterSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
                ' Look up in class constraint.
                Dim constraintClass = typeParameter.GetClassConstraint(useSiteDiagnostics)
                If constraintClass IsNot Nothing Then
                    LookupDefaultPropertyInClass(result, constraintClass, binder, useSiteDiagnostics)
                    If Not result.IsClear Then
                        Return
                    End If
                End If

                ' Look up in interface constraints.
                Dim lookIn As Queue(Of InterfaceInfo) = Nothing
                Dim processed As HashSet(Of InterfaceInfo) = Nothing
                AddInterfaceConstraints(typeParameter, lookIn, processed, useSiteDiagnostics)

                If lookIn IsNot Nothing Then
                    For Each baseInterface In lookIn
                        LookupDefaultPropertyInBaseInterface(result, baseInterface.InterfaceType, binder, useSiteDiagnostics)
                        If result.HasDiagnostic Then
                            Return
                        End If
                    Next
                End If
            End Sub

            ' See Semantics::LookupDefaultPropertyInBaseInterface.
            Private Shared Sub LookupDefaultPropertyInBaseInterface(
                result As LookupResult,
                type As NamedTypeSymbol,
                binder As Binder,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                If type.IsErrorType() Then
                    Return
                End If

                Debug.Assert(type.IsInterfaceType)
                Debug.Assert(Not result.HasDiagnostic)

                Dim tmpResult = LookupResult.GetInstance()
                Try
                    LookupDefaultPropertyInInterface(tmpResult, type, binder, useSiteDiagnostics)

                    If Not tmpResult.HasSymbol Then
                        Return
                    End If

                    If tmpResult.HasDiagnostic OrElse Not result.HasSymbol Then
                        result.SetFrom(tmpResult)
                        Return
                    End If

                    ' At least one member was found on another interface.
                    ' Report an ambiguity error if the two interfaces are distinct.
                    Dim symbolA = result.Symbols(0)
                    Dim symbolB = tmpResult.Symbols(0)

                    If symbolA.ContainingSymbol <> symbolB.ContainingSymbol Then
                        result.MergeAmbiguous(tmpResult, AddressOf GenerateAmbiguousDefaultPropertyDiagnostic)
                    End If
                Finally
                    tmpResult.Free()
                End Try
            End Sub

            ' Return True if a default property is defined on the type.
            Private Shared Function LookupDefaultPropertyInSingleType(
                result As LookupResult,
                type As NamedTypeSymbol,
                accessThroughType As TypeSymbol,
                binder As Binder,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            ) As Boolean
                Dim defaultPropertyName = type.DefaultPropertyName
                If String.IsNullOrEmpty(defaultPropertyName) Then
                    Return False
                End If

                Select Case type.TypeKind
                    Case TypeKind.Class, TypeKind.Module, TypeKind.Structure
                        LookupInClass(
                            result,
                            type,
                            defaultPropertyName,
                            arity:=0,
                            options:=LookupOptions.Default,
                            accessThroughType:=accessThroughType,
                            binder:=binder,
                            useSiteDiagnostics:=useSiteDiagnostics)

                    Case TypeKind.Interface
                        Debug.Assert(accessThroughType Is type)
                        LookupInInterface(
                            result,
                            type,
                            defaultPropertyName,
                            arity:=0,
                            options:=LookupOptions.Default,
                            binder:=binder,
                            useSiteDiagnostics:=useSiteDiagnostics)

                    Case TypeKind.TypeParameter
                        Throw ExceptionUtilities.UnexpectedValue(type.TypeKind)
                End Select

                Return result.HasSymbol
            End Function

            Private Shared Function GenerateAmbiguousDefaultPropertyDiagnostic(symbols As ImmutableArray(Of Symbol)) As AmbiguousSymbolDiagnostic
                Debug.Assert(symbols.Length > 1)

                Dim symbolA = symbols(0)
                Dim containingSymbolA = symbolA.ContainingSymbol

                For i = 1 To symbols.Length - 1
                    Dim symbolB = symbols(i)
                    Dim containingSymbolB = symbolB.ContainingSymbol

                    If containingSymbolA <> containingSymbolB Then
                        ' "Default property access is ambiguous between the inherited interface members '{0}' of interface '{1}' and '{2}' of interface '{3}'."
                        Return New AmbiguousSymbolDiagnostic(ERRID.ERR_DefaultPropertyAmbiguousAcrossInterfaces4, symbols, symbolA, containingSymbolA, symbolB, containingSymbolB)
                    End If
                Next

                ' Expected ambiguous symbols
                Throw ExceptionUtilities.Unreachable
            End Function

            Private Shared Sub LookupForExtensionMethodsIfNeedTo(
                result As LookupResult,
                container As TypeSymbol,
                name As String,
                arity As Integer,
                options As LookupOptions,
                binder As Binder,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                If result.IsGood AndAlso
                    ((options And LookupOptions.EagerlyLookupExtensionMethods) = 0 OrElse
                     result.Symbols(0).Kind <> SymbolKind.Method) Then
                    Return
                End If

                Dim currentResult = LookupResult.GetInstance()
                LookupForExtensionMethods(currentResult, container, name, arity, options, binder, useSiteDiagnostics)
                MergeInternalXmlHelperValueIfNecessary(currentResult, container, name, arity, options, binder, useSiteDiagnostics)
                result.MergeOverloadedOrPrioritized(currentResult, checkIfCurrentHasOverloads:=False)
                currentResult.Free()
            End Sub

            Private Shared Function ShouldLookupExtensionMethods(options As LookupOptions, container As TypeSymbol) As Boolean
                Return options.ShouldLookupExtensionMethods AndAlso
                   Not container.IsObjectType() AndAlso
                   Not container.IsShared AndAlso
                   Not container.IsModuleType()
            End Function

            Public Shared Sub LookupForExtensionMethods(
                lookupResult As LookupResult,
                container As TypeSymbol,
                name As String,
                arity As Integer,
                options As LookupOptions,
                binder As Binder,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                Debug.Assert(lookupResult.IsClear)

                If Not ShouldLookupExtensionMethods(options, container) Then
                    lookupResult.SetFrom(SingleLookupResult.Empty)
                    Return
                End If

                ' Proceed up the chain of binders, collecting extension methods
                Dim originalBinder = binder
                Dim currentBinder = binder

                Dim methods = ArrayBuilder(Of MethodSymbol).GetInstance()
                Dim proximity As Integer = 0

                ' We don't want to process the same methods more than once, but the same extension method 
                ' might be in scope in several different binders. For example, within a type, within
                ' imported the same type, within imported namespace containing the type. 
                ' So, taking into consideration the fact that CollectProbableExtensionMethodsInSingleBinder 
                ' groups methods from the same containing type together, we will keep track of the types and
                ' will process all the methods from the same containing type at once.
                Dim seenContainingTypes As New HashSet(Of NamedTypeSymbol)()

                Do
                    methods.Clear()
                    currentBinder.CollectProbableExtensionMethodsInSingleBinder(name, methods, originalBinder)

                    Dim i As Integer = 0
                    Dim count As Integer = methods.Count

                    While i < count
                        Dim containingType As NamedTypeSymbol = methods(i).ContainingType

                        If seenContainingTypes.Add(containingType) AndAlso
                           ((options And LookupOptions.IgnoreAccessibility) <> 0 OrElse
                            AccessCheck.IsSymbolAccessible(containingType, binder.Compilation.Assembly, useSiteDiagnostics)) Then

                            ' Process all methods from the same type together.
                            Do
                                ' Try to reduce this method and merge with the current result
                                Dim reduced As MethodSymbol = methods(i).ReduceExtensionMethod(container, proximity)

                                If reduced IsNot Nothing Then
                                    lookupResult.MergeOverloadedOrPrioritizedExtensionMethods(binder.CheckViability(reduced, arity, options, reduced.ContainingType, useSiteDiagnostics))
                                End If

                                i += 1
                            Loop While i < count AndAlso containingType Is methods(i).ContainingSymbol
                        Else
                            ' We already processed extension methods from this container before or the whole container is not accessible,
                            ' skip the whole group of methods from this containing type.
                            Do
                                i += 1
                            Loop While i < count AndAlso containingType Is methods(i).ContainingSymbol
                        End If
                    End While

                    ' Continue to containing binders.
                    proximity += 1
                    currentBinder = currentBinder.m_containingBinder
                Loop While currentBinder IsNot Nothing

                methods.Free()
            End Sub

            ''' <summary>
            ''' Include the InternalXmlHelper.Value extension property in the LookupResult
            ''' if the container implements IEnumerable(Of XElement), the name is "Value",
            ''' and the arity is 0.
            ''' </summary>
            Private Shared Sub MergeInternalXmlHelperValueIfNecessary(
                lookupResult As LookupResult,
                container As TypeSymbol,
                name As String,
                arity As Integer,
                options As LookupOptions,
                binder As Binder,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )

                If (arity <> 0) OrElse Not IdentifierComparison.Equals(name, StringConstants.ValueProperty) Then
                    Return
                End If

                Dim compilation = binder.Compilation
                If (options And LookupOptions.NamespacesOrTypesOnly) <> 0 OrElse
                   Not container.IsOrImplementsIEnumerableOfXElement(compilation, useSiteDiagnostics) Then
                    Return
                End If

                Dim symbol = binder.GetInternalXmlHelperValueExtensionProperty()
                Dim singleResult As SingleLookupResult
                If symbol Is Nothing Then
                    ' Match the native compiler which reports ERR_XmlFeaturesNotAvailable in this case.
                    Dim useSiteError = ErrorFactory.ErrorInfo(ERRID.ERR_XmlFeaturesNotAvailable)
                    singleResult = New SingleLookupResult(LookupResultKind.NotReferencable, Binder.GetErrorSymbol(name, useSiteError), useSiteError)
                Else
                    Dim reduced = New ReducedExtensionPropertySymbol(DirectCast(symbol, PropertySymbol))
                    singleResult = binder.CheckViability(reduced, arity, options, reduced.ContainingType, useSiteDiagnostics)
                End If

                lookupResult.MergePrioritized(singleResult)
            End Sub

            Private Shared Sub AddLookupSymbolsInfoOfExtensionMethods(nameSet As LookupSymbolsInfo,
                                                               container As TypeSymbol,
                                                               newInfo As LookupSymbolsInfo,
                                                               binder As Binder)
                Dim lookup = LookupResult.GetInstance()

                For Each name In newInfo.Names
                    lookup.Clear()

                    LookupForExtensionMethods(lookup, container, name, 0,
                                              LookupOptions.AllMethodsOfAnyArity Or LookupOptions.IgnoreAccessibility,
                                              binder, useSiteDiagnostics:=Nothing)

                    If lookup.IsGood Then
                        For Each method As MethodSymbol In lookup.Symbols
                            nameSet.AddSymbol(method, method.Name, method.Arity)
                        Next
                    End If
                Next

                lookup.Free()
            End Sub

            Public Shared Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                   container As TypeSymbol,
                                                                   options As LookupOptions,
                                                                   binder As Binder)
                If Not ShouldLookupExtensionMethods(options, container) Then
                    Return
                End If

                ' We will not reduce extension methods for the purpose of this operation,
                ' they will still be shared methods.
                options = options And (Not Global.Microsoft.CodeAnalysis.VisualBasic.LookupOptions.MustBeInstance)

                ' Proceed up the chain of binders, collecting names of extension methods
                Dim currentBinder As Binder = binder

                Dim newInfo = LookupSymbolsInfo.GetInstance()

                Do
                    currentBinder.AddExtensionMethodLookupSymbolsInfoInSingleBinder(newInfo, options, binder)

                    ' Continue to containing binders.
                    currentBinder = currentBinder.m_containingBinder
                Loop While currentBinder IsNot Nothing

                AddLookupSymbolsInfoOfExtensionMethods(nameSet, container, newInfo, binder)

                newInfo.Free()

                ' Include "Value" for InternalXmlHelper.Value if necessary.
                Dim compilation = binder.Compilation
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                If container.IsOrImplementsIEnumerableOfXElement(compilation, useSiteDiagnostics) AndAlso useSiteDiagnostics.IsNullOrEmpty Then
                    nameSet.AddSymbol(Nothing, StringConstants.ValueProperty, 0)
                End If
            End Sub

            ''' <summary>
            ''' Checks if two interfaces have a base-derived relationship
            ''' </summary>
            Private Shared Function IsDerivedInterface(
                        base As NamedTypeSymbol,
                        derived As NamedTypeSymbol,
                        basesBeingResolved As BasesBeingResolved,
                        <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            ) As Boolean

                Debug.Assert(base.IsInterface)
                Debug.Assert(derived.IsInterface)

                If TypeSymbol.Equals(derived.OriginalDefinition, base.OriginalDefinition, TypeCompareKind.ConsiderEverything) Then
                    Return False
                End If

                ' if we are not resolving bases we can just go through AllInterfaces list
                If basesBeingResolved.InheritsBeingResolvedOpt Is Nothing Then
                    For Each i In derived.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                        If TypeSymbol.Equals(i, base, TypeCompareKind.ConsiderEverything) Then
                            Return True
                        End If
                    Next

                    Return False
                End If

                ' we are resolving bases so should use a private helper that relies only on Declared interfaces
                Return IsDerivedInterface(base, derived, basesBeingResolved, New HashSet(Of Symbol), useSiteDiagnostics)
            End Function

            Private Shared Function IsDerivedInterface(
                        base As NamedTypeSymbol,
                        derived As NamedTypeSymbol,
                        basesBeingResolved As BasesBeingResolved,
                        verified As HashSet(Of Symbol),
                        <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            ) As Boolean

                Debug.Assert(Not TypeSymbol.Equals(base, derived, TypeCompareKind.ConsiderEverything), "should already be verified for equality")
                Debug.Assert(base.IsInterface)
                Debug.Assert(derived.IsInterface)

                verified.Add(derived)

                ' not afraid of cycles here as we will not verify same symbol twice
                Dim interfaces = derived.GetDeclaredInterfacesWithDefinitionUseSiteDiagnostics(basesBeingResolved, useSiteDiagnostics)

                If Not interfaces.IsDefaultOrEmpty Then
                    For Each i In interfaces
                        If TypeSymbol.Equals(i, base, TypeCompareKind.ConsiderEverything) Then
                            Return True
                        End If

                        If verified.Contains(i) Then
                            ' seen this already
                            Continue For
                        End If

                        If IsDerivedInterface(
                            base,
                            i,
                            basesBeingResolved,
                            verified,
                            useSiteDiagnostics) Then

                            Return True
                        End If
                    Next
                End If

                Return False
            End Function

            Private Structure InterfaceInfo
                Implements IEquatable(Of InterfaceInfo)

                Public ReadOnly InterfaceType As NamedTypeSymbol
                Public ReadOnly InComInterfaceContext As Boolean
                Public ReadOnly DescendantDefinitions As ImmutableHashSet(Of NamedTypeSymbol)

                Public Sub New(interfaceType As NamedTypeSymbol, inComInterfaceContext As Boolean, Optional descendantDefinitions As ImmutableHashSet(Of NamedTypeSymbol) = Nothing)
                    Me.InterfaceType = interfaceType
                    Me.InComInterfaceContext = inComInterfaceContext
                    Me.DescendantDefinitions = descendantDefinitions
                End Sub

                Public Overrides Function GetHashCode() As Integer
                    Return Hash.Combine(Me.InterfaceType.GetHashCode(), Me.InComInterfaceContext.GetHashCode())
                End Function

                Public Overloads Overrides Function Equals(obj As Object) As Boolean
                    Return TypeOf obj Is InterfaceInfo AndAlso Equals(DirectCast(obj, InterfaceInfo))
                End Function

                Public Overloads Function Equals(other As InterfaceInfo) As Boolean Implements IEquatable(Of InterfaceInfo).Equals
                    Return Me.InterfaceType.Equals(other.InterfaceType) AndAlso Me.InComInterfaceContext = other.InComInterfaceContext
                End Function
            End Structure

            Private Shared Sub LookupInInterface(lookupResult As LookupResult,
                         container As NamedTypeSymbol,
                         name As String,
                         arity As Integer,
                         options As LookupOptions,
                         binder As Binder,
                         <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                Debug.Assert(lookupResult.IsClear)

                Dim methodsOnly As Boolean = CheckAndClearMethodsOnlyOption(options)

                ' look in these types. Start with container, add more accordingly.
                Dim info As New InterfaceInfo(container, False)

                Dim lookIn As New Queue(Of InterfaceInfo)
                lookIn.Enqueue(info)

                Dim processed As New HashSet(Of InterfaceInfo)
                processed.Add(info)

                LookupInInterfaces(lookupResult, container, lookIn, processed, name, arity, options, binder, methodsOnly, useSiteDiagnostics)

                ' If no viable or ambiguous results, look in Object.
                If Not lookupResult.IsGoodOrAmbiguous AndAlso (options And LookupOptions.NoSystemObjectLookupForInterfaces) = 0 Then
                    Dim currentResult = LookupResult.GetInstance()
                    Dim obj As NamedTypeSymbol = binder.SourceModule.ContainingAssembly.GetSpecialType(SpecialType.System_Object)

                    LookupInClass(currentResult,
                                  obj,
                                  name, arity, options Or LookupOptions.IgnoreExtensionMethods, obj, binder,
                                  useSiteDiagnostics)

                    If currentResult.IsGood Then
                        lookupResult.SetFrom(currentResult)
                    End If

                    currentResult.Free()
                End If

                ClearLookupResultIfNotMethods(methodsOnly, lookupResult)
                LookupForExtensionMethodsIfNeedTo(lookupResult, container, name, arity, options, binder, useSiteDiagnostics)
                Return
            End Sub

            Private Shared Sub LookupInInterfaces(lookupResult As LookupResult,
                         container As TypeSymbol,
                         lookIn As Queue(Of InterfaceInfo),
                         processed As HashSet(Of InterfaceInfo),
                         name As String,
                         arity As Integer,
                         options As LookupOptions,
                         binder As Binder,
                         methodsOnly As Boolean,
                         <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                Debug.Assert(lookupResult.IsClear)

                Dim basesBeingResolved As BasesBeingResolved = binder.BasesBeingResolved()

                Dim isEventsOnlySpecified As Boolean = (options And LookupOptions.EventsOnly) <> 0

                Dim currentResult = LookupResult.GetInstance()
                Do
                    Dim info As InterfaceInfo = lookIn.Dequeue()
                    Debug.Assert(processed.Contains(info))

                    Debug.Assert(currentResult.IsClear)
                    LookupWithoutInheritance(currentResult, info.InterfaceType, name, arity, options, container, binder, useSiteDiagnostics)

                    ' if result does not shadow we will have bases to visit
                    If Not (currentResult.StopFurtherLookup AndAlso AnyShadows(currentResult)) Then
                        If (options And LookupOptions.NoBaseClassLookup) = 0 AndAlso Not binder.IgnoreBaseClassesInLookup Then
                            AddBaseInterfacesToTheSearch(binder, info, lookIn, processed, useSiteDiagnostics)
                        End If
                    End If

                    Dim leaveEventsOnly As Boolean? = Nothing
                    If info.InComInterfaceContext Then
                        leaveEventsOnly = isEventsOnlySpecified
                    End If

                    If lookupResult.IsGood AndAlso currentResult.IsGood Then
                        ' We have _another_ viable result while lookupResult is already viable. Use special interface merging rules.
                        MergeInterfaceLookupResults(lookupResult, currentResult, basesBeingResolved, leaveEventsOnly, useSiteDiagnostics)
                    Else
                        If currentResult.IsGood AndAlso leaveEventsOnly.HasValue Then
                            FilterSymbolsInLookupResult(currentResult, SymbolKind.Event, leaveInsteadOfRemoving:=leaveEventsOnly.Value)
                        End If
                        lookupResult.MergePrioritized(currentResult)
                    End If
                    currentResult.Clear()

                Loop While lookIn.Count <> 0

                currentResult.Free()

                If methodsOnly AndAlso lookupResult.IsGood Then
                    ' We need to filter out non-method symbols from 'currentResult' 
                    ' before merging with 'lookupResult'
                    FilterSymbolsInLookupResult(lookupResult, SymbolKind.Method, leaveInsteadOfRemoving:=True)
                End If

                ' it may look like a Good result, but it may have ambiguities inside
                ' so we need to check that to be sure.
                If lookupResult.IsGood Then
                    Dim ambiguityDiagnostics As AmbiguousSymbolDiagnostic = Nothing
                    Dim symbols As ArrayBuilder(Of Symbol) = lookupResult.Symbols

                    For i As Integer = 0 To symbols.Count - 2
                        Dim interface1 = DirectCast(symbols(i).ContainingType, NamedTypeSymbol)

                        For j As Integer = i + 1 To symbols.Count - 1

                            If Not LookupResult.CanOverload(symbols(i), symbols(j)) Then
                                ' Symbols cannot overload each other.
                                ' If they were from the same interface, LookupWithoutInheritance would make the result ambiguous.
                                ' If they were from interfaces related through inheritance, one of them would shadow another,
                                ' MergeInterfaceLookupResults handles that.
                                ' Therefore, this symbols are from unrelated interfaces.
                                ambiguityDiagnostics = New AmbiguousSymbolDiagnostic(
                                            ERRID.ERR_AmbiguousAcrossInterfaces3,
                                            symbols.ToImmutable,
                                            name,
                                            CustomSymbolDisplayFormatter.DefaultErrorFormat(symbols(i).ContainingType),
                                            CustomSymbolDisplayFormatter.DefaultErrorFormat(symbols(j).ContainingType))

                                GoTo ExitForFor
                            End If
                        Next
                    Next
ExitForFor:

                    If ambiguityDiagnostics IsNot Nothing Then
                        lookupResult.SetFrom(New SingleLookupResult(LookupResultKind.Ambiguous, symbols.First, ambiguityDiagnostics))
                    End If
                End If
            End Sub

            Private Shared Sub FilterSymbolsInLookupResult(result As LookupResult, kind As SymbolKind, leaveInsteadOfRemoving As Boolean)
                Debug.Assert(result.IsGood)

                Dim resultSymbols As ArrayBuilder(Of Symbol) = result.Symbols
                Debug.Assert(resultSymbols.Count > 0)

                Dim i As Integer = 0
                Dim j As Integer = 0
                While j < resultSymbols.Count
                    Dim symbol As Symbol = resultSymbols(j)
                    If (symbol.Kind = kind) = leaveInsteadOfRemoving Then
                        resultSymbols(i) = resultSymbols(j)
                        i += 1
                    End If
                    j += 1
                End While

                resultSymbols.Clip(i)
                If i = 0 Then
                    result.Clear()
                End If
            End Sub

            Private Shared Sub LookupInTypeParameter(lookupResult As LookupResult,
                         typeParameter As TypeParameterSymbol,
                         name As String,
                         arity As Integer,
                         options As LookupOptions,
                         binder As Binder,
                         <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

                Dim methodsOnly = CheckAndClearMethodsOnlyOption(options)
                LookupInTypeParameterNoExtensionMethods(lookupResult, typeParameter, name, arity, options, binder, useSiteDiagnostics)

                ClearLookupResultIfNotMethods(methodsOnly, lookupResult)
                LookupForExtensionMethodsIfNeedTo(lookupResult, typeParameter, name, arity, options, binder, useSiteDiagnostics)
            End Sub

            Private Shared Sub LookupInTypeParameterNoExtensionMethods(result As LookupResult,
                         typeParameter As TypeParameterSymbol,
                         name As String,
                         arity As Integer,
                         options As LookupOptions,
                         binder As Binder,
                         <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
                Debug.Assert((options And LookupOptions.MethodsOnly) = 0)

                options = options Or LookupOptions.IgnoreExtensionMethods

                ' §4.9.2: "the class constraint hides members in interface constraints, which
                ' hide members in System.ValueType (if Structure constraint is specified),
                ' which hides members in Object."

                ' Look up in class constraint.
                Dim constraintClass = typeParameter.GetClassConstraint(useSiteDiagnostics)
                If constraintClass IsNot Nothing Then
                    LookupInClass(result, constraintClass, name, arity, options, constraintClass, binder, useSiteDiagnostics)
                    If result.StopFurtherLookup Then
                        Return
                    End If
                End If

                ' Look up in interface constraints.
                Dim lookIn As Queue(Of InterfaceInfo) = Nothing
                Dim processed As HashSet(Of InterfaceInfo) = Nothing
                AddInterfaceConstraints(typeParameter, lookIn, processed, useSiteDiagnostics)

                If lookIn IsNot Nothing Then
                    ' §4.9.2: "If a member with the same name appears in more than one interface
                    ' constraint the member is unavailable (as in multiple interface inheritance)"
                    Dim interfaceResult = LookupResult.GetInstance()
                    Debug.Assert((options And LookupOptions.MethodsOnly) = 0)
                    LookupInInterfaces(interfaceResult, typeParameter, lookIn, processed, name, arity, options, binder, False, useSiteDiagnostics)
                    result.MergePrioritized(interfaceResult)
                    interfaceResult.Free()
                    If Not result.IsClear Then
                        Return
                    End If
                End If

                ' Look up in System.ValueType or System.Object.
                If constraintClass Is Nothing Then
                    Debug.Assert(result.IsClear)
                    Dim baseType = GetTypeParameterBaseType(typeParameter)
                    LookupInClass(result, baseType, name, arity, options, baseType, binder, useSiteDiagnostics)
                End If
            End Sub

            Private Shared Function CheckAndClearMethodsOnlyOption(ByRef options As LookupOptions) As Boolean
                If (options And LookupOptions.MethodsOnly) <> 0 Then
                    options = CType(options And (Not LookupOptions.MethodsOnly), LookupOptions)
                    Return True
                End If
                Return False
            End Function

            Private Shared Sub ClearLookupResultIfNotMethods(methodsOnly As Boolean, lookupResult As LookupResult)
                If methodsOnly AndAlso
                   lookupResult.HasSymbol AndAlso
                   lookupResult.Symbols(0).Kind <> SymbolKind.Method Then
                    lookupResult.Clear()
                End If
            End Sub

            Private Shared Sub AddInterfaceConstraints(typeParameter As TypeParameterSymbol,
                                                       ByRef allInterfaces As Queue(Of InterfaceInfo),
                                                       ByRef processedInterfaces As HashSet(Of InterfaceInfo),
                                                       <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                For Each constraintType In typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                    Select Case constraintType.TypeKind
                        Case TypeKind.Interface
                            Dim newInfo As New InterfaceInfo(DirectCast(constraintType, NamedTypeSymbol), False)

                            If processedInterfaces Is Nothing OrElse Not processedInterfaces.Contains(newInfo) Then
                                If processedInterfaces Is Nothing Then
                                    allInterfaces = New Queue(Of InterfaceInfo)
                                    processedInterfaces = New HashSet(Of InterfaceInfo)
                                End If

                                allInterfaces.Enqueue(newInfo)
                                processedInterfaces.Add(newInfo)
                            End If

                        Case TypeKind.TypeParameter
                            AddInterfaceConstraints(DirectCast(constraintType, TypeParameterSymbol), allInterfaces, processedInterfaces, useSiteDiagnostics)
                    End Select
                Next
            End Sub

            ''' <summary>
            ''' Merges two lookup results while eliminating symbols that are shadowed.
            ''' Note that the final result may contain unrelated and possibly conflicting symbols as
            ''' this helper is not intended to catch ambiguities.
            ''' </summary>
            ''' <param name="leaveEventsOnly">
            ''' If is not Nothing and False filters out all Event symbols, and if is not Nothing 
            ''' and True filters out all non-Event symbols, nos not have any effect otherwise.
            ''' Is used for special handling of Events inside COM interfaces.
            ''' </param>
            Private Shared Sub MergeInterfaceLookupResults(
                                        knownResult As LookupResult,
                                        newResult As LookupResult,
                                        BasesBeingResolved As BasesBeingResolved,
                                        leaveEventsOnly As Boolean?,
                                        <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )

                Debug.Assert(knownResult.Kind = newResult.Kind)

                Dim knownSymbols As ArrayBuilder(Of Symbol) = knownResult.Symbols
                Dim newSymbols As ArrayBuilder(Of Symbol) = newResult.Symbols
                Dim newSymbolContainer = newSymbols.First().ContainingType

                For i As Integer = 0 To knownSymbols.Count - 1
                    Dim knownSymbol = knownSymbols(i)

                    ' Nothing means that the symbol has been eliminated via shadowing
                    If knownSymbol Is Nothing Then
                        Continue For
                    End If

                    Dim knownSymbolContainer = knownSymbol.ContainingType

                    For j As Integer = 0 To newSymbols.Count - 1
                        Dim newSymbol As Symbol = newSymbols(j)
                        ' Nothing means that the symbol has been eliminated via shadowing
                        If newSymbol Is Nothing Then
                            Continue For
                        End If

                        ' Special-case events in case we are inside COM interface
                        If leaveEventsOnly.HasValue AndAlso (newSymbol.Kind = SymbolKind.Event) <> leaveEventsOnly.Value Then
                            newSymbols(j) = Nothing
                            Continue For
                        End If

                        If knownSymbol = newSymbol Then
                            ' this is the same result as we already have, remove from the new set
                            newSymbols(j) = Nothing
                            Continue For
                        End If

                        ' container of the first new symbol should be container of all others
                        Debug.Assert(TypeSymbol.Equals(newSymbolContainer, newSymbol.ContainingType, TypeCompareKind.ConsiderEverything))

                        ' Are the known and new symbols of the right kinds to overload?
                        Dim cantOverloadEachOther = Not LookupResult.CanOverload(knownSymbol, newSymbol)

                        If IsDerivedInterface(base:=newSymbolContainer,
                                                 derived:=knownSymbolContainer,
                                                 basesBeingResolved:=BasesBeingResolved,
                                                 useSiteDiagnostics:=useSiteDiagnostics) Then

                            ' if currently known is more derived and shadows the new one
                            ' it shadows all the new ones and we are done
                            If IsShadows(knownSymbol) OrElse cantOverloadEachOther Then
                                ' no need to continue with merge. new symbols are all shadowed
                                ' and they cannot shadow anything in the old set
                                Debug.Assert(Not knownSymbols.Any(Function(s) s Is Nothing))
                                newResult.Clear()
                                Return
                            End If

                        ElseIf IsDerivedInterface(base:=knownSymbolContainer,
                                             derived:=newSymbolContainer,
                                             basesBeingResolved:=BasesBeingResolved,
                                             useSiteDiagnostics:=useSiteDiagnostics) Then

                            ' if new is more derived and shadows
                            ' the current one should be dropped
                            ' NOTE that we continue iterating as more known symbols may be "shadowed out" by the current.
                            If IsShadows(newSymbol) OrElse cantOverloadEachOther Then
                                knownSymbols(i) = Nothing

                                ' all following known symbols in the same container are shadowed by the new one
                                ' we can do a quick check and remove them here
                                For k = i + 1 To knownSymbols.Count - 1
                                    Dim otherKnown As Symbol = knownSymbols(k)
                                    If otherKnown IsNot Nothing AndAlso TypeSymbol.Equals(otherKnown.ContainingType, knownSymbolContainer, TypeCompareKind.ConsiderEverything) Then
                                        knownSymbols(k) = Nothing
                                    End If
                                Next
                            End If
                        End If

                        ' we can get here if results are completely unrelated.
                        ' However we do not know if they are conflicting as either one could be "shadowed out" in later iterations.
                        ' for now we let both known and new stay
                    Next
                Next

                CompactAndAppend(knownSymbols, newSymbols)
                newResult.Clear()
            End Sub



            ''' <summary>
            ''' first.Where(t IsNot Nothing).Concat(second.Where(t IsNot Nothing))
            ''' </summary>
            Private Shared Sub CompactAndAppend(first As ArrayBuilder(Of Symbol), second As ArrayBuilder(Of Symbol))
                Dim i As Integer = 0

                ' skip non nulls
                While i < first.Count
                    If first(i) Is Nothing Then
                        Exit While
                    End If

                    i += 1
                End While

                ' compact the rest
                Dim j As Integer = i + 1
                While j < first.Count
                    Dim item As Symbol = first(j)

                    If item IsNot Nothing Then
                        first(i) = item
                        i += 1
                    End If

                    j += 1
                End While

                ' clip to compacted size
                first.Clip(i)

                ' append non nulls from second
                i = 0
                While i < second.Count
                    Dim items As Symbol = second(i)
                    If items IsNot Nothing Then
                        first.Add(items)
                    End If
                    i += 1
                End While
            End Sub

            ''' <summary>
            ''' 
            ''' </summary>
            Private Shared Sub AddBaseInterfacesToTheSearch(binder As Binder,
                               currentInfo As InterfaceInfo,
                               lookIn As Queue(Of InterfaceInfo),
                               processed As HashSet(Of InterfaceInfo),
                               <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

                Dim interfaces As ImmutableArray(Of NamedTypeSymbol) = currentInfo.InterfaceType.GetDirectBaseInterfacesNoUseSiteDiagnostics(binder.BasesBeingResolved)

                If Not interfaces.IsDefaultOrEmpty Then
                    Dim inComInterfaceContext As Boolean = currentInfo.InComInterfaceContext OrElse
                                                           currentInfo.InterfaceType.CoClassType IsNot Nothing

                    Dim descendants As ImmutableHashSet(Of NamedTypeSymbol)

                    If binder.BasesBeingResolved.InheritsBeingResolvedOpt Is Nothing Then
                        descendants = Nothing
                    Else
                        ' We need to watch out for cycles in inheritance chain since they are not broken while bases are being resolved.
                        If currentInfo.DescendantDefinitions Is Nothing Then
                            descendants = ImmutableHashSet.Create(currentInfo.InterfaceType.OriginalDefinition)
                        Else
                            descendants = currentInfo.DescendantDefinitions.Add(currentInfo.InterfaceType.OriginalDefinition)
                        End If
                    End If

                    For Each i In interfaces
                        If descendants IsNot Nothing AndAlso descendants.Contains(i.OriginalDefinition) Then
                            ' About to get in an inheritance cycle
                            Continue For
                        End If

                        i.OriginalDefinition.AddUseSiteDiagnostics(useSiteDiagnostics)

                        Dim newInfo As New InterfaceInfo(i, inComInterfaceContext, descendants)
                        If processed.Add(newInfo) Then
                            lookIn.Enqueue(newInfo)
                        End If
                    Next
                End If
            End Sub

            ''' <summary>
            ''' if any symbol in the list Shadows. This implies that name is not visible through the base.
            ''' </summary>
            Private Shared Function AnyShadows(result As LookupResult) As Boolean
                For Each sym As Symbol In result.Symbols
                    If sym.IsShadows Then
                        Return True
                    End If
                Next

                Return False
            End Function

            ' Find all names in a non-interface type, consider inheritance.
            Private Shared Sub AddLookupSymbolsInfoInClass(nameSet As LookupSymbolsInfo,
                                                  container As TypeSymbol,
                                                  options As LookupOptions,
                                                  binder As Binder)
                ' We need a check for SpecialType.System_Void as its base type is
                ' ValueType but we don't wish to return any members for void type
                If container IsNot Nothing And container.SpecialType = SpecialType.System_Void Then
                    Return
                End If

                ' Lookup proceeds up the base class chain.
                Dim currentType = container
                Do
                    AddLookupSymbolsInfoWithoutInheritance(nameSet, currentType, options, container, binder)

                    ' If the type is from a winmd file and implements any of the special WinRT collection
                    ' projections, then we may need to add projected interface members
                    Dim namedType = TryCast(currentType, NamedTypeSymbol)
                    If namedType IsNot Nothing AndAlso namedType.ShouldAddWinRTMembers Then
                        AddWinRTMembersLookupSymbolsInfo(nameSet, namedType, options, container, binder)
                    End If

                    ' Go to base type, unless that would case infinite recursion or the options or the binder
                    ' disallows it.
                    If (options And LookupOptions.NoBaseClassLookup) <> 0 OrElse binder.IgnoreBaseClassesInLookup Then
                        currentType = Nothing
                    Else
                        currentType = currentType.GetDirectBaseTypeNoUseSiteDiagnostics(binder.BasesBeingResolved)
                    End If
                Loop While currentType IsNot Nothing

                ' Search for extension methods.
                AddExtensionMethodLookupSymbolsInfo(nameSet, container, options, binder)

                ' Special case: if we're in a constructor of a class or structure, then we can call constructors on ourself or our immediate base
                ' (via Me.New or MyClass.New or MyBase.New). We don't have enough info to check the constraints that the constructor must be
                ' the specific tokens Me, MyClass, or MyBase, or that its the first statement in the constructor, so services must do
                ' that check if it wants to show that.
                ' Roslyn Bug 9701.
                Dim containingMethod = TryCast(binder.ContainingMember, MethodSymbol)
                If containingMethod IsNot Nothing AndAlso
                   containingMethod.MethodKind = MethodKind.Constructor AndAlso
                   (container.TypeKind = TypeKind.Class OrElse container.TypeKind = TypeKind.Structure) AndAlso
                   (TypeSymbol.Equals(containingMethod.ContainingType, container, TypeCompareKind.ConsiderEverything) OrElse TypeSymbol.Equals(containingMethod.ContainingType.BaseTypeNoUseSiteDiagnostics, container, TypeCompareKind.ConsiderEverything)) Then
                    nameSet.AddSymbol(Nothing, WellKnownMemberNames.InstanceConstructorName, 0)
                End If
            End Sub

            Private Shared Sub AddLookupSymbolsInfoInSubmissions(nameSet As LookupSymbolsInfo,
                                                                  submissionClass As TypeSymbol,
                                                                  options As LookupOptions,
                                                                  binder As Binder)
                Dim submission = binder.Compilation
                Do
                    ' TODO (tomat): import aliases

                    If submission.ScriptClass IsNot Nothing Then
                        AddLookupSymbolsInfoWithoutInheritance(nameSet, submission.ScriptClass, options, submissionClass, binder)
                    End If

                    submission = submission.PreviousSubmission
                Loop Until submission Is Nothing

                ' TODO (tomat): extension methods
            End Sub

            ' Find all names in an interface type, consider inheritance.
            Private Shared Sub AddLookupSymbolsInfoInInterface(nameSet As LookupSymbolsInfo,
                                                                container As NamedTypeSymbol,
                                                                options As LookupOptions,
                                                                binder As Binder)

                Dim info As New InterfaceInfo(container, False)

                Dim lookIn As New Queue(Of InterfaceInfo)
                lookIn.Enqueue(info)

                Dim processed As New HashSet(Of InterfaceInfo)
                processed.Add(info)

                AddLookupSymbolsInfoInInterfaces(nameSet, container, lookIn, processed, options, binder)

                ' Look in Object.
                AddLookupSymbolsInfoInClass(nameSet,
                              binder.SourceModule.ContainingAssembly.GetSpecialType(SpecialType.System_Object),
                              options Or LookupOptions.IgnoreExtensionMethods, binder)

                ' Search for extension methods.
                AddExtensionMethodLookupSymbolsInfo(nameSet, container, options, binder)
            End Sub

            Private Shared Sub AddLookupSymbolsInfoInInterfaces(nameSet As LookupSymbolsInfo,
                                                                container As TypeSymbol,
                                                                lookIn As Queue(Of InterfaceInfo),
                                                                processed As HashSet(Of InterfaceInfo),
                                                                options As LookupOptions,
                                                                binder As Binder)
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                Do
                    Dim currentType As InterfaceInfo = lookIn.Dequeue

                    AddLookupSymbolsInfoWithoutInheritance(nameSet, currentType.InterfaceType, options, container, binder)

                    ' Go to base type, unless that would case infinite recursion or the options or the binder
                    ' disallows it.
                    If (options And LookupOptions.NoBaseClassLookup) = 0 AndAlso Not binder.IgnoreBaseClassesInLookup Then
                        AddBaseInterfacesToTheSearch(binder, currentType, lookIn, processed, useSiteDiagnostics)
                    End If

                Loop While lookIn.Count <> 0

            End Sub

            Private Shared Sub AddLookupSymbolsInfoInTypeParameter(nameSet As LookupSymbolsInfo,
                                                                    typeParameter As TypeParameterSymbol,
                                                                    options As LookupOptions,
                                                                    binder As Binder)
                If typeParameter.TypeParameterKind = TypeParameterKind.Cref Then
                    Return
                End If

                AddLookupSymbolsInfoInTypeParameterNoExtensionMethods(nameSet, typeParameter, options, binder)

                ' Search for extension methods.
                AddExtensionMethodLookupSymbolsInfo(nameSet, typeParameter, options, binder)
            End Sub

            Private Shared Sub AddLookupSymbolsInfoInTypeParameterNoExtensionMethods(nameSet As LookupSymbolsInfo,
                                                                                      typeParameter As TypeParameterSymbol,
                                                                                      options As LookupOptions,
                                                                                      binder As Binder)
                options = options Or LookupOptions.IgnoreExtensionMethods

                ' Look up in class constraint.
                Dim constraintClass = typeParameter.GetClassConstraint(Nothing)
                If constraintClass IsNot Nothing Then
                    AddLookupSymbolsInfoInClass(nameSet, constraintClass, options, binder)
                End If

                ' Look up in interface constraints.
                Dim lookIn As Queue(Of InterfaceInfo) = Nothing
                Dim processed As HashSet(Of InterfaceInfo) = Nothing
                AddInterfaceConstraints(typeParameter, lookIn, processed, useSiteDiagnostics:=Nothing)

                If lookIn IsNot Nothing Then
                    AddLookupSymbolsInfoInInterfaces(nameSet, typeParameter, lookIn, processed, options, binder)
                End If

                ' Look up in System.ValueType or System.Object.
                If constraintClass Is Nothing Then
                    Dim baseType = GetTypeParameterBaseType(typeParameter)
                    AddLookupSymbolsInfoInClass(nameSet, baseType, options, binder)
                End If
            End Sub

            ''' <summary>
            ''' Lookup a member name in a type without considering inheritance, returning a LookupResult that
            ''' summarizes the results of the lookup. See LookupResult structure for a detailed
            ''' discussing of the meaning of the results.
            ''' </summary>
            Private Shared Sub LookupWithoutInheritance(lookupResult As LookupResult,
                                                       container As TypeSymbol,
                                                       name As String,
                                                       arity As Integer,
                                                       options As LookupOptions,
                                                       accessThroughType As TypeSymbol,
                                                       binder As Binder,
                                                       <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                Dim members As ImmutableArray(Of Symbol) = ImmutableArray(Of Symbol).Empty

                If (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly)) = LookupOptions.NamespacesOrTypesOnly Then
                    ' Only named types have members that are types. Go through all the types in this type and
                    ' validate them. If there's multiple, give an error.
                    If TypeOf container Is NamedTypeSymbol Then
                        members = ImmutableArray(Of Symbol).CastUp(container.GetTypeMembers(name))
                    End If
                ElseIf (options And LookupOptions.LabelsOnly) = 0 Then
                    members = container.GetMembers(name)
                End If

                Debug.Assert(lookupResult.IsClear)

                ' Go through each member of the type, and combine them into a single result. Overloadable members
                ' are combined together, while other duplicates cause an ambiguity error.
                If Not members.IsDefaultOrEmpty Then
                    Dim imported As Boolean = container.ContainingModule IsNot binder.SourceModule

                    For Each sym In members
                        lookupResult.MergeMembersOfTheSameType(binder.CheckViability(sym, arity, options, accessThroughType, useSiteDiagnostics), imported)
                    Next
                End If
            End Sub

            ' Find all names in a type, without considering inheritance.
            Private Shared Sub AddLookupSymbolsInfoWithoutInheritance(nameSet As LookupSymbolsInfo,
                                                                       container As TypeSymbol,
                                                                       options As LookupOptions,
                                                                       accessThroughType As TypeSymbol,
                                                                       binder As Binder)
                ' UNDONE: validate symbols with something that looks like ValidateSymbol.

                If (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly)) = LookupOptions.NamespacesOrTypesOnly Then
                    ' Only named types have members that are types. Go through all the types in this type and
                    ' validate them.
                    If TypeOf container Is NamedTypeSymbol Then
                        For Each sym In container.GetTypeMembersUnordered()
                            If binder.CanAddLookupSymbolInfo(sym, options, nameSet, accessThroughType) Then
                                nameSet.AddSymbol(sym, sym.Name, sym.Arity)
                            End If
                        Next
                    End If
                ElseIf (options And LookupOptions.LabelsOnly) = 0 Then
                    ' Go through each member of the type.
                    For Each sym In container.GetMembersUnordered()
                        If binder.CanAddLookupSymbolInfo(sym, options, nameSet, accessThroughType) Then
                            nameSet.AddSymbol(sym, sym.Name, sym.GetArity())
                        End If
                    Next
                End If
            End Sub

            Private Shared Sub AddWinRTMembersLookupSymbolsInfo(
                nameSet As LookupSymbolsInfo,
                type As NamedTypeSymbol,
                options As LookupOptions,
                accessThroughType As TypeSymbol,
                binder As Binder
            )
                ' Dev11 searches all declared and undeclared base interfaces
                For Each iface In type.AllInterfacesNoUseSiteDiagnostics
                    If IsWinRTProjectedInterface(iface, binder.Compilation) Then
                        AddLookupSymbolsInfoWithoutInheritance(nameSet, iface, options, accessThroughType, binder)
                    End If
                Next
            End Sub

            Private Shared Function GetTypeParameterBaseType(typeParameter As TypeParameterSymbol) As NamedTypeSymbol
                ' The default base type should only be used if there is no explicit class constraint.
                Debug.Assert(typeParameter.GetClassConstraint(Nothing) Is Nothing)
                Return typeParameter.ContainingAssembly.GetSpecialType(If(typeParameter.HasValueTypeConstraint, SpecialType.System_ValueType, SpecialType.System_Object))
            End Function
        End Class
    End Class
End Namespace
