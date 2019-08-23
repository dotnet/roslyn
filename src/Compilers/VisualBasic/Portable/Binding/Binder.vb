' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A Binder object represents a general location from where binding is happening, and provides
    ''' virtual methods for looking up unqualified names, reporting errors, and also other
    ''' operations that need to know about where binding happened from (accessibility checking,
    ''' etc.) It also contains most of the methods related to general binding of constructs,
    ''' although some large sections are factored into their own classes.
    '''
    ''' Yes, Binder is a big grab bag of features. The reason for this is that binders are threaded
    ''' through essentially ALL binding functions. So, basically Binder has all the features that
    ''' need to be threaded through binding.
    '''
    ''' Binder objects form a linked list and each binder links to its containing binder. Each
    ''' binder only handles operations that it knows how to handles, and passes on other calls to
    ''' its containing binder. This maintains separation of concerns and allows binders to be strung
    ''' together in various configurations to enable different binding scenarios (e.g., debugger
    ''' expression evaluator).
    '''
    ''' In general, binder objects should be constructed via the BinderBuilder class.
    '''
    ''' Binder class has GetBinder methods that return binders for scopes nested into the current
    ''' binder scope. One should not expect to get a binder from the functions unless a syntax that
    ''' originates a scope is passed as the argument. Also, the functions do not cross lambda
    ''' boundaries, if binder's scope contains a lambda expression, binder will not return any
    ''' binders for nodes contained in the lambda body. In order to get them, the lambda must be
    ''' bound to BoundLambda node, which exposes LambdaBinder, which can be asked for binders in the
    ''' lambda body (but it will not descend into nested lambdas). Currently, only
    ''' <see cref="ExecutableCodeBinder"/>, <see cref="MethodBodySemanticModel.IncrementalBinder"/>
    ''' and <see cref="SpeculativeBinder"/> have special implementation of GetBinder functions,
    ''' the rest just delegate to containing binder.
    ''' </summary>
    Friend MustInherit Class Binder

        Private Shared ReadOnly s_noArguments As ImmutableArray(Of BoundExpression) = ImmutableArray(Of BoundExpression).Empty

        Protected ReadOnly m_containingBinder As Binder

        ' Caching these items in the nearest binder is a performance win.
        Private ReadOnly _syntaxTree As SyntaxTree
        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _sourceModule As SourceModuleSymbol
        Private ReadOnly _isEarlyAttributeBinder As Boolean
        Private ReadOnly _ignoreBaseClassesInLookup As Boolean
        Private ReadOnly _basesBeingResolved As BasesBeingResolved

        Protected Sub New(containingBinder As Binder)
            m_containingBinder = containingBinder

            If containingBinder IsNot Nothing Then
                _syntaxTree = containingBinder.SyntaxTree
                _compilation = containingBinder.Compilation
                _sourceModule = containingBinder.SourceModule
                _isEarlyAttributeBinder = containingBinder.IsEarlyAttributeBinder
                _ignoreBaseClassesInLookup = containingBinder.IgnoreBaseClassesInLookup
                _basesBeingResolved = containingBinder.BasesBeingResolved
            End If
        End Sub

        Protected Sub New(containingBinder As Binder, syntaxTree As SyntaxTree)
            Me.New(containingBinder)
            _syntaxTree = syntaxTree
        End Sub

        Protected Sub New(containingBinder As Binder, sourceModule As SourceModuleSymbol, compilation As VisualBasicCompilation)
            Me.New(containingBinder)
            _sourceModule = sourceModule
            _compilation = compilation
        End Sub

        Protected Sub New(containingBinder As Binder, Optional isEarlyAttributeBinder As Boolean? = Nothing, Optional ignoreBaseClassesInLookup As Boolean? = Nothing)
            Me.New(containingBinder)

            If isEarlyAttributeBinder.HasValue Then
                _isEarlyAttributeBinder = isEarlyAttributeBinder.Value
            End If

            If ignoreBaseClassesInLookup.HasValue Then
                _ignoreBaseClassesInLookup = ignoreBaseClassesInLookup.Value
            End If
        End Sub

        Protected Sub New(containingBinder As Binder, basesBeingResolved As BasesBeingResolved)
            Me.New(containingBinder)
            _basesBeingResolved = basesBeingResolved
        End Sub

        Public ReadOnly Property ContainingBinder As Binder
            Get
                Return m_containingBinder
            End Get
        End Property

        ''' <summary>
        ''' If the binding context requires specific binding options, then modify the given
        ''' lookup options accordingly.
        ''' </summary>
        Friend Overridable Function BinderSpecificLookupOptions(options As LookupOptions) As LookupOptions
            Return m_containingBinder.BinderSpecificLookupOptions(options)
        End Function

        Friend ReadOnly Property IgnoresAccessibility As Boolean
            Get
                Return (BinderSpecificLookupOptions(Nothing) And LookupOptions.IgnoreAccessibility) =
                    LookupOptions.IgnoreAccessibility
            End Get
        End Property

        ''' <summary>
        ''' Lookup the given name in the binder and containing binders.
        ''' Returns the result of the lookup. See the definition of LookupResult for details.
        ''' </summary>
        ''' <remarks>
        ''' This method is virtual, but usually there is no need to override it. It
        ''' calls the virtual LookupInSingleBinder, which should be overridden instead,
        ''' for each binder in turn, and merges the results.
        ''' Overriding this method is needed only in limited scenarios, for example for
        ''' a binder that binds query [Into] clause and has implicit qualifier.
        ''' </remarks>
        Public Overridable Sub Lookup(lookupResult As LookupResult,
                          name As String,
                          arity As Integer,
                          options As LookupOptions,
                          <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            Debug.Assert(name IsNot Nothing)

            Dim originalBinder As Binder = Me
            Dim currentBinder As Binder = Me

            Debug.Assert(lookupResult.IsClear)
            options = BinderSpecificLookupOptions(options)

            Dim currentResult As LookupResult = LookupResult.GetInstance()

            Do
                currentResult.Clear()
                currentBinder.LookupInSingleBinder(currentResult, name, arity, options, originalBinder, useSiteDiagnostics)
                lookupResult.MergePrioritized(currentResult)

                If lookupResult.StopFurtherLookup Then
                    currentResult.Free()
                    Return  ' don't need to look further, we have a viable result.
                ElseIf currentResult.IsWrongArity AndAlso TypeOf currentBinder Is ImportAliasesBinder Then
                    ' Since there was a name match among imported aliases, we should not look
                    ' in types and namespaces imported on the same level (file or project).
                    ' We should skip ImportedTypesAndNamespacesMembersBinder and TypesOfImportedNamespacesMembersBinder
                    ' above the currentBinder. Both binders are optional, however either both are present or
                    ' both are absent and the precedence order is the following:
                    '
                    '         <SourceFile or SourceModule binder>
                    '                          |
                    '                          V
                    '       [<TypesOfImportedNamespacesMembersBinder>]
                    '                          |
                    '                          V
                    '       [<ImportedTypesAndNamespacesMembersBinder>]
                    '                          |
                    '                          V
                    '                <ImportAliasesBinder>

                    If TypeOf currentBinder.m_containingBinder Is ImportedTypesAndNamespacesMembersBinder Then
                        currentBinder = currentBinder.m_containingBinder.m_containingBinder
                    End If

                    Debug.Assert(TypeOf currentBinder.m_containingBinder Is SourceFileBinder OrElse
                                 TypeOf currentBinder.m_containingBinder Is SourceModuleBinder)

                ElseIf (options And LookupOptions.IgnoreExtensionMethods) = 0 AndAlso
                   TypeOf currentBinder Is NamedTypeBinder Then
                    ' Only binder of the most nested type can bind to an extension method.
                    options = options Or LookupOptions.IgnoreExtensionMethods
                End If

                ' Continue to containing binders.
                currentBinder = currentBinder.m_containingBinder
            Loop While currentBinder IsNot Nothing

            currentResult.Free()

            ' No good symbols found in any binder. LookupResult has best we found.
            Return
        End Sub

        ''' <summary>
        ''' Lookup in just a single binder, without delegating to containing binder. The original
        ''' binder passed in is used for accessibility checking and so forth.
        ''' </summary>
        Friend Overridable Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                    name As String,
                                                    arity As Integer,
                                                    options As LookupOptions,
                                                    originalBinder As Binder,
                                                    <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            lookupResult.Clear()
        End Sub

        ''' <summary>
        ''' Collect extension methods with the given name that are in scope in this binder.
        ''' The passed in ArrayBuilder must be empty. Extension methods from the same containing type
        ''' must be grouped together.
        ''' </summary>
        Protected Overridable Sub CollectProbableExtensionMethodsInSingleBinder(name As String,
                                                                        methods As ArrayBuilder(Of MethodSymbol),
                                                                        originalBinder As Binder)
            Debug.Assert(methods.Count = 0)
        End Sub

        ''' <summary>
        ''' Lookup all names of extension methods that are available from a single binder, without delegating
        ''' to containing binder. The original binder passed in is used for accessibility checking
        ''' and so forth.
        ''' Names that are available are inserted into "nameSet". This is a hashSet that accumulates
        ''' names, and should be created with the VB identifierComparer.
        ''' </summary>
        Protected Overridable Sub AddExtensionMethodLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                                     options As LookupOptions,
                                                                                     originalBinder As Binder)
            ' overridden in derived binders that introduce names.
        End Sub

        ''' <summary>
        ''' Lookups labels by label names, returns a label or Nothing
        ''' </summary>
        Friend Overridable Function LookupLabelByNameToken(labelName As SyntaxToken) As LabelSymbol
            Return Me.ContainingBinder.LookupLabelByNameToken(labelName)
        End Function

        ' Lookup the names that are available in this binder, given the options.
        ' Names that are available are inserted into "nameSet". This is a hashSet that accumulates
        ' names, and should be created with the VB identifierComparer.
        Public Overridable Sub AddLookupSymbolsInfo(nameSet As LookupSymbolsInfo, options As LookupOptions)
            Debug.Assert(nameSet IsNot Nothing)

            Dim originalBinder As Binder = Me
            Dim currentBinder As Binder = Me

            Do
                currentBinder.AddLookupSymbolsInfoInSingleBinder(nameSet, options, originalBinder)

                ' Only binder of the most nested type can bind to an extension method.
                If (options And LookupOptions.IgnoreExtensionMethods) = 0 AndAlso
                   TypeOf currentBinder Is NamedTypeBinder Then
                    options = options Or LookupOptions.IgnoreExtensionMethods
                End If

                ' Continue to containing binders.
                currentBinder = currentBinder.m_containingBinder
            Loop While currentBinder IsNot Nothing
        End Sub

        ''' <summary>
        ''' Lookup all names that are available from a single binder, without delegating
        ''' to containing binder. The original binder passed in is used for accessibility checking
        ''' and so forth.
        ''' Names that are available are inserted into "nameSet". This is a hashSet that accumulates
        ''' names, and should be created with the VB identifierComparer.
        ''' </summary>
        Friend Overridable Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                  options As LookupOptions,
                                                                  originalBinder As Binder)
            ' overridden in derived binders that introduce names.
        End Sub

        ''' <summary>
        ''' Determine if "sym" is accessible from the location represented by this binder. For protected
        ''' access, use the qualifier type "accessThroughType" if not Nothing (if Nothing just check protected
        ''' access with no qualifier).
        ''' </summary>
        ''' <remarks>
        ''' Overriding methods should consider <see cref="IgnoresAccessibility"/>.
        ''' </remarks>
        Public Overridable Function CheckAccessibility(sym As Symbol,
                                                       <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                       Optional accessThroughType As TypeSymbol = Nothing,
                                                       Optional basesBeingResolved As BasesBeingResolved = Nothing) As AccessCheckResult
            Return m_containingBinder.CheckAccessibility(sym, useSiteDiagnostics, accessThroughType, basesBeingResolved)
        End Function

        ''' <summary>
        ''' Determine if "sym" is accessible from the location represented by this binder. For protected
        ''' access, use the qualifier type "accessThroughType" if not Nothing (if Nothing just check protected
        ''' access with no qualifier).
        ''' </summary>
        Public Function IsAccessible(sym As Symbol,
                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                     Optional accessThroughType As TypeSymbol = Nothing,
                                     Optional basesBeingResolved As BasesBeingResolved = Nothing) As Boolean
            Return CheckAccessibility(sym, useSiteDiagnostics, accessThroughType, basesBeingResolved) = AccessCheckResult.Accessible
        End Function

        ''' <summary>
        ''' Some nodes have special binder's for their contents
        ''' </summary>
        Public Overridable Function GetBinder(node As SyntaxNode) As Binder
            Return m_containingBinder.GetBinder(node)
        End Function

        ''' <summary>
        ''' Some nodes have special binder's for their contents
        ''' </summary>
        Public Overridable Function GetBinder(stmtList As SyntaxList(Of StatementSyntax)) As Binder
            Return m_containingBinder.GetBinder(stmtList)
        End Function

        ''' <summary>
        ''' The member containing the binding context
        ''' </summary>
        Public Overridable ReadOnly Property ContainingMember As Symbol
            Get
                Return m_containingBinder.ContainingMember
            End Get
        End Property

        ''' <summary>
        ''' Additional members associated with the binding context
        ''' Currently, this field is only used for multiple field/property symbols initialized by an AsNew initializer, e.g. "Dim x, y As New Integer" or "WithEvents x, y as New Object"
        ''' </summary>
        Public Overridable ReadOnly Property AdditionalContainingMembers As ImmutableArray(Of Symbol)
            Get
                Return m_containingBinder.AdditionalContainingMembers
            End Get
        End Property

        Friend ReadOnly Property ContainingModule As ModuleSymbol
            Get
                ' If there's a containing member, it either is or has a containing module.
                ' Otherwise, we'll just use the compilation's source module.
                Dim containingMember = Me.ContainingMember
                Return If(TryCast(containingMember, ModuleSymbol), If(containingMember?.ContainingModule, Me.Compilation.SourceModule))
            End Get
        End Property

        ''' <summary>
        ''' Tells whether binding is happening in a query context.
        ''' </summary>
        Public Overridable ReadOnly Property IsInQuery As Boolean
            Get
                Return m_containingBinder.IsInQuery
            End Get
        End Property

        ''' <summary>
        ''' Tells whether binding is happening in a lambda context.
        ''' </summary>
        Friend ReadOnly Property IsInLambda As Boolean
            Get
                Debug.Assert(ContainingMember IsNot Nothing)
                Return ContainingMember.IsLambdaMethod
            End Get
        End Property

        Public Overridable ReadOnly Property ImplicitlyTypedLocalsBeingBound As ConsList(Of LocalSymbol)
            Get
                Return m_containingBinder.ImplicitlyTypedLocalsBeingBound
            End Get
        End Property

        ''' <summary>
        ''' Returns true if the node is in a position where an unbound type
        ''' such as (C(of)) is allowed.
        ''' </summary>
        Public Overridable Function IsUnboundTypeAllowed(syntax As GenericNameSyntax) As Boolean
            Return m_containingBinder.IsUnboundTypeAllowed(syntax)
        End Function

        ''' <summary>
        ''' The type containing the binding context
        ''' </summary>
        Public Overridable ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return m_containingBinder.ContainingType
            End Get
        End Property

        ''' <summary>
        ''' Returns true if the binder is binding top-level script code.
        ''' </summary>
        Friend ReadOnly Property BindingTopLevelScriptCode As Boolean
            Get
                Dim containingMember = Me.ContainingMember
                Select Case containingMember.Kind
                    Case SymbolKind.Method
                        ' global statements
                        Return (DirectCast(containingMember, MethodSymbol)).IsScriptConstructor
                    Case SymbolKind.NamedType
                        ' script variable initializers
                        Return (DirectCast(containingMember, NamedTypeSymbol)).IsScriptClass
                    Case Else
                        Return False
                End Select
            End Get
        End Property

        ''' <summary>
        ''' The namespace or type containing the binding context
        ''' </summary>
        Public Overridable ReadOnly Property ContainingNamespaceOrType As NamespaceOrTypeSymbol
            Get
                Return m_containingBinder.ContainingNamespaceOrType
            End Get
        End Property

        ''' <summary>
        ''' Get the built-in MSCORLIB type identified. If it's not available (an error type), then report the
        ''' error with the given syntax and diagnostic bag. If the node and diagBag are Nothing, then don't report the error (not recommended).
        ''' </summary>
        ''' <param name="typeId">Type to get</param>
        ''' <param name="node">Where to report the error, if any.</param>
        Public Function GetSpecialType(typeId As SpecialType, node As SyntaxNodeOrToken, diagBag As DiagnosticBag) As NamedTypeSymbol
            Dim reportedAnError As Boolean = False
            Return GetSpecialType(SourceModule.ContainingAssembly, typeId, node, diagBag, reportedAnError, suppressUseSiteError:=False)
        End Function

        Public Shared Function GetSpecialType(assembly As AssemblySymbol, typeId As SpecialType, node As SyntaxNodeOrToken, diagBag As DiagnosticBag) As NamedTypeSymbol
            Dim reportedAnError As Boolean = False
            Return GetSpecialType(assembly, typeId, node, diagBag, reportedAnError, suppressUseSiteError:=False)
        End Function

        Public Function GetSpecialType(typeId As SpecialType, node As SyntaxNodeOrToken, diagBag As DiagnosticBag, ByRef reportedAnError As Boolean, suppressUseSiteError As Boolean) As NamedTypeSymbol
            Return GetSpecialType(SourceModule.ContainingAssembly, typeId, node, diagBag, reportedAnError, suppressUseSiteError)
        End Function

        Public Shared Function GetSpecialType(assembly As AssemblySymbol, typeId As SpecialType, node As SyntaxNodeOrToken, diagBag As DiagnosticBag, ByRef reportedAnError As Boolean, suppressUseSiteError As Boolean) As NamedTypeSymbol
            Dim symbol As NamedTypeSymbol = assembly.GetSpecialType(typeId)

            If diagBag IsNot Nothing Then
                Dim info = GetUseSiteErrorForSpecialType(symbol, suppressUseSiteError)
                If info IsNot Nothing Then
                    ReportDiagnostic(diagBag, node, info)
                    reportedAnError = True
                End If
            End If

            Return symbol
        End Function

        Friend Shared Function GetUseSiteErrorForSpecialType(type As TypeSymbol, Optional suppressUseSiteError As Boolean = False) As DiagnosticInfo
            Dim info As DiagnosticInfo = Nothing
            If type.TypeKind = TypeKind.Error AndAlso TypeOf type Is MissingMetadataTypeSymbol.TopLevel Then
                Dim missing = DirectCast(type, MissingMetadataTypeSymbol.TopLevel)
                info = ErrorFactory.ErrorInfo(ERRID.ERR_UndefinedType1, MetadataHelpers.BuildQualifiedName(missing.NamespaceName, missing.Name))
            ElseIf Not suppressUseSiteError Then
                info = type.GetUseSiteErrorInfo()
            End If
            Return info
        End Function

        ''' <summary>
        ''' This is a layer on top of the Compilation version that generates a diagnostic if the well-known
        ''' type isn't found.
        ''' </summary>
        Friend Function GetWellKnownType(type As WellKnownType, syntax As SyntaxNode, diagBag As DiagnosticBag) As NamedTypeSymbol
            Return GetWellKnownType(Me.Compilation, type, syntax, diagBag)
        End Function

        Friend Shared Function GetWellKnownType(compilation As VisualBasicCompilation, type As WellKnownType, syntax As SyntaxNode, diagBag As DiagnosticBag) As NamedTypeSymbol
            Dim typeSymbol As NamedTypeSymbol = compilation.GetWellKnownType(type)
            Debug.Assert(typeSymbol IsNot Nothing)

            Dim useSiteError = GetUseSiteErrorForWellKnownType(typeSymbol)
            If useSiteError IsNot Nothing Then
                ReportDiagnostic(diagBag, syntax, useSiteError)
            End If

            Return typeSymbol
        End Function

        Friend Shared Function GetUseSiteErrorForWellKnownType(type As TypeSymbol) As DiagnosticInfo
            Return type.GetUseSiteErrorInfo()
        End Function

        Private Function GetInternalXmlHelperType(syntax As VisualBasicSyntaxNode, diagBag As DiagnosticBag) As NamedTypeSymbol
            Dim typeSymbol = GetInternalXmlHelperType()

            Dim useSiteError = GetUseSiteErrorForWellKnownType(typeSymbol)
            If useSiteError IsNot Nothing Then
                ReportDiagnostic(diagBag, syntax, useSiteError)
            End If

            Return typeSymbol
        End Function

        Private Function GetInternalXmlHelperType() As NamedTypeSymbol
            Const globalMetadataName = "My.InternalXmlHelper"
            Dim metadataName = globalMetadataName

            Dim rootNamespace = Me.Compilation.Options.RootNamespace
            If Not String.IsNullOrEmpty(rootNamespace) Then
                metadataName = $"{rootNamespace}.{metadataName}"
            End If

            Dim emittedName = MetadataTypeName.FromFullName(metadataName, useCLSCompliantNameArityEncoding:=True)
            Return Me.ContainingModule.LookupTopLevelMetadataType(emittedName)
        End Function

        ''' <summary>
        ''' WARN: Retrieves the symbol but does not check its viability (accessibility, etc).
        ''' </summary>
        Private Function GetInternalXmlHelperValueExtensionProperty() As PropertySymbol
            For Each candidate As Symbol In GetInternalXmlHelperType().GetMembers("Value")
                If Not candidate.IsShared OrElse candidate.Kind <> SymbolKind.Property Then
                    Continue For
                End If

                Dim candidateProperty = DirectCast(candidate, PropertySymbol)
                If candidateProperty.Type.SpecialType <> SpecialType.System_String OrElse
                    candidateProperty.RefCustomModifiers.Length > 0 OrElse
                    candidateProperty.TypeCustomModifiers.Length > 0 OrElse
                    candidateProperty.ParameterCount <> 1 Then

                    Continue For
                End If

                Dim parameter = candidateProperty.Parameters(0)
                If parameter.CustomModifiers.Length > 0 OrElse parameter.RefCustomModifiers.Length > 0 Then
                    Continue For
                End If

                Dim parameterType = parameter.Type
                If parameterType.OriginalDefinition.SpecialType <> SpecialType.System_Collections_Generic_IEnumerable_T OrElse
                        Not TypeSymbol.Equals(DirectCast(parameterType, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics(0), Me.Compilation.GetWellKnownType(WellKnownType.System_Xml_Linq_XElement), TypeCompareKind.ConsiderEverything) Then
                    Continue For
                End If

                ' Only one symbol can match the criteria above, so we don't have to worry about ambiguity.
                Return candidateProperty
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' This is a layer on top of the assembly version that generates a diagnostic if the well-known
        ''' member isn't found.
        ''' </summary>
        Friend Function GetSpecialTypeMember(member As SpecialMember, syntax As SyntaxNode, diagnostics As DiagnosticBag) As Symbol
            Return GetSpecialTypeMember(Me.ContainingMember.ContainingAssembly, member, syntax, diagnostics)
        End Function

        Friend Shared Function GetSpecialTypeMember(assembly As AssemblySymbol, member As SpecialMember, syntax As SyntaxNode, diagnostics As DiagnosticBag) As Symbol
            Dim useSiteError As DiagnosticInfo = Nothing
            Dim specialMemberSymbol As Symbol = GetSpecialTypeMember(assembly, member, useSiteError)

            If useSiteError IsNot Nothing Then
                ReportDiagnostic(diagnostics, syntax, useSiteError)
            End If

            Return specialMemberSymbol
        End Function

        Friend Shared Function GetSpecialTypeMember(assembly As AssemblySymbol, member As SpecialMember, ByRef useSiteError As DiagnosticInfo) As Symbol
            Dim specialMemberSymbol As Symbol = assembly.GetSpecialTypeMember(member)

            If specialMemberSymbol Is Nothing Then
                Dim memberDescriptor As MemberDescriptor = SpecialMembers.GetDescriptor(member)
                useSiteError = ErrorFactory.ErrorInfo(ERRID.ERR_MissingRuntimeHelper, memberDescriptor.DeclaringTypeMetadataName & "." & memberDescriptor.Name)
            Else
                useSiteError = If(specialMemberSymbol.GetUseSiteErrorInfo(), specialMemberSymbol.ContainingType.GetUseSiteErrorInfo())
            End If

            Return specialMemberSymbol
        End Function

        ''' <summary>
        ''' This is a layer on top of the Compilation version that generates a diagnostic if the well-known
        ''' member isn't found.
        ''' </summary>
        Friend Function GetWellKnownTypeMember(member As WellKnownMember, syntax As SyntaxNode, diagBag As DiagnosticBag) As Symbol
            Return GetWellKnownTypeMember(Me.Compilation, member, syntax, diagBag)
        End Function

        Friend Shared Function GetWellKnownTypeMember(compilation As VisualBasicCompilation, member As WellKnownMember, syntax As SyntaxNode, diagBag As DiagnosticBag) As Symbol
            Dim useSiteError As DiagnosticInfo = Nothing
            Dim memberSymbol As Symbol = GetWellKnownTypeMember(compilation, member, useSiteError)

            If useSiteError IsNot Nothing Then
                ReportDiagnostic(diagBag, syntax, useSiteError)
            End If

            Return memberSymbol
        End Function

        Friend Shared Function GetWellKnownTypeMember(compilation As VisualBasicCompilation, member As WellKnownMember, ByRef useSiteError As DiagnosticInfo) As Symbol
            Dim memberSymbol As Symbol = compilation.GetWellKnownTypeMember(member)

            useSiteError = GetUseSiteErrorForWellKnownTypeMember(memberSymbol, member, compilation.Options.EmbedVbCoreRuntime)

            Return memberSymbol
        End Function

        Friend Shared Function GetUseSiteErrorForWellKnownTypeMember(memberSymbol As Symbol, member As WellKnownMember, embedVBRuntimeUsed As Boolean) As DiagnosticInfo
            If memberSymbol Is Nothing Then
                Dim memberDescriptor As MemberDescriptor = WellKnownMembers.GetDescriptor(member)

                Return GetDiagnosticForMissingRuntimeHelper(memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name, embedVBRuntimeUsed)
            Else
                Return If(memberSymbol.GetUseSiteErrorInfo(), memberSymbol.ContainingType.GetUseSiteErrorInfo())
            End If
        End Function

        ''' <summary>
        ''' Get the source module.
        ''' </summary>
        Public ReadOnly Property SourceModule As SourceModuleSymbol
            Get
                Return _sourceModule
            End Get
        End Property

        ''' <summary>
        ''' Get the compilation.
        ''' </summary>
        Public ReadOnly Property Compilation As VisualBasicCompilation
            Get
                Return _compilation
            End Get
        End Property

        ''' <summary>
        ''' Get an error symbol.
        ''' </summary>
        Public Shared Function GetErrorSymbol(
            name As String,
            errorInfo As DiagnosticInfo,
            candidateSymbols As ImmutableArray(Of Symbol),
            resultKind As LookupResultKind,
            Optional reportErrorWhenReferenced As Boolean = False
        ) As ErrorTypeSymbol
            Return New ExtendedErrorTypeSymbol(errorInfo, name, 0, candidateSymbols, resultKind, reportErrorWhenReferenced)
        End Function

        Public Shared Function GetErrorSymbol(name As String, errorInfo As DiagnosticInfo, Optional reportErrorWhenReferenced As Boolean = False) As ErrorTypeSymbol
            Return GetErrorSymbol(name, errorInfo, ImmutableArray(Of Symbol).Empty, LookupResultKind.Empty, reportErrorWhenReferenced)
        End Function

        ''' <summary>
        ''' Get the Location associated with a given TextSpan.
        ''' </summary>
        Public Function GetLocation(span As TextSpan) As Location
            Return Me.SyntaxTree.GetLocation(span)
        End Function

        ''' <summary>
        ''' Get a SyntaxReference associated with a given syntax node.
        ''' </summary>
        Public Overridable Function GetSyntaxReference(node As VisualBasicSyntaxNode) As SyntaxReference
            Return m_containingBinder.GetSyntaxReference(node)
        End Function

        ''' <summary>
        ''' Returns the syntax tree.
        ''' </summary>
        Public ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _syntaxTree
            End Get
        End Property

        ''' <summary>
        ''' Called in member lookup right before going into the base class of a type. Results a set of named types whose
        ''' bases classes are currently in the process of being resolved, so we shouldn't look into their bases
        ''' again to prevent/detect circular references.
        ''' </summary>
        ''' <returns>Nothing if no bases being resolved, otherwise the set of bases being resolved.</returns>
        Public Function BasesBeingResolved() As BasesBeingResolved
            Return _basesBeingResolved
        End Function

        Friend Overridable ReadOnly Property ConstantFieldsInProgress As SymbolsInProgress(Of FieldSymbol)
            Get
                Return m_containingBinder.ConstantFieldsInProgress
            End Get
        End Property

        Friend Overridable ReadOnly Property DefaultParametersInProgress As SymbolsInProgress(Of ParameterSymbol)
            Get
                Return m_containingBinder.DefaultParametersInProgress
            End Get
        End Property

        ''' <summary>
        ''' Called during member lookup before going into the base class of a type. If returns
        ''' true, the base class is ignored. Primarily used for binding Imports.
        ''' </summary>
        Public ReadOnly Property IgnoreBaseClassesInLookup As Boolean
            Get
                Return _ignoreBaseClassesInLookup
            End Get
        End Property

        ''' <summary>
        ''' Current Option Strict mode.
        ''' </summary>
        Public Overridable ReadOnly Property OptionStrict As OptionStrict
            Get
                Return m_containingBinder.OptionStrict
            End Get
        End Property

        ''' <summary>
        ''' True if Option Infer On is in effect. False if Option Infer Off is in effect.
        ''' </summary>
        Public Overridable ReadOnly Property OptionInfer As Boolean
            Get
                Return m_containingBinder.OptionInfer
            End Get
        End Property

        ''' <summary>
        ''' True if Option Explicit On is in effect. False if Option Explicit Off is in effect.
        ''' Note that even if Option Explicit Off is in effect, there are places (field initializers)
        ''' where implicit variable declaration is not permitted. See the ImplicitVariablesDeclarationAllowedHere
        ''' property also.
        ''' </summary>
        Public Overridable ReadOnly Property OptionExplicit As Boolean
            Get
                Return m_containingBinder.OptionExplicit
            End Get
        End Property

        ''' <summary>
        ''' True if Option Compare Text is in effect. False if Option Compare Binary is in effect.
        ''' </summary>
        Public Overridable ReadOnly Property OptionCompareText As Boolean
            Get
                Return m_containingBinder.OptionCompareText
            End Get
        End Property

        ''' <summary>
        ''' True if integer overflow checking is off.
        ''' </summary>
        Public Overridable ReadOnly Property CheckOverflow As Boolean
            Get
                Return m_containingBinder.CheckOverflow
            End Get
        End Property

        ''' <summary>
        ''' True if implicit variable declaration is available within this binder, and the binder
        ''' has already finished binding all possible implicit declarations inside (and is not accepting)
        ''' any more.
        ''' </summary>
        Public Overridable ReadOnly Property AllImplicitVariableDeclarationsAreHandled As Boolean
            Get
                Return m_containingBinder.AllImplicitVariableDeclarationsAreHandled
            End Get
        End Property

        ''' <summary>
        ''' True if implicit variable declaration is allow by the language here. Differs from OptionExplicit
        ''' in that it is only try if this binder is associated with a region that allows implicit variable
        ''' declaration (field initializers and attributes don't, for example).
        ''' </summary>
        Public Overridable ReadOnly Property ImplicitVariableDeclarationAllowed As Boolean
            Get
                Return m_containingBinder.ImplicitVariableDeclarationAllowed
            End Get
        End Property

        ''' <summary>
        ''' Declare an implicit local variable. The type of the local is determined
        ''' by the type character (if any) on the variable.
        ''' </summary>
        Public Overridable Function DeclareImplicitLocalVariable(nameSyntax As IdentifierNameSyntax, diagnostics As DiagnosticBag) As LocalSymbol
            Debug.Assert(Not Me.AllImplicitVariableDeclarationsAreHandled)
            Return m_containingBinder.DeclareImplicitLocalVariable(nameSyntax, diagnostics)
        End Function

        ''' <summary>
        ''' Get all implicitly declared variables that were declared in this method body.
        ''' </summary>
        Public Overridable ReadOnly Property ImplicitlyDeclaredVariables As ImmutableArray(Of LocalSymbol)
            Get
                Return m_containingBinder.ImplicitlyDeclaredVariables
            End Get
        End Property

        ''' <summary>
        ''' Disallow additional local variable declaration and report delayed shadowing diagnostics.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overridable Sub DisallowFurtherImplicitVariableDeclaration(diagnostics As DiagnosticBag)
            m_containingBinder.DisallowFurtherImplicitVariableDeclaration(diagnostics)
        End Sub

#If DEBUG Then
        ' In DEBUG, this method (overridden in ExecutableCodeBinder) checks that identifiers are bound in order,
        ' which ensures that implicit variable declaration will work correctly.
        Public Overridable Sub CheckSimpleNameBindingOrder(node As SimpleNameSyntax)
            m_containingBinder.CheckSimpleNameBindingOrder(node)
        End Sub

        Public Overridable Sub EnableSimpleNameBindingOrderChecks(enable As Boolean)
            m_containingBinder.EnableSimpleNameBindingOrderChecks(enable)
        End Sub

        ' Helper to allow displaying the binder hierarchy in the debugger.
        Friend Function GetAllBinders() As Binder()
            Dim binders = ArrayBuilder(Of Binder).GetInstance()
            Dim binder = Me
            While binder IsNot Nothing
                binders.Add(binder)
                binder = binder.ContainingBinder
            End While
            Return binders.ToArrayAndFree()
        End Function
#End If

        ''' <summary>
        ''' Get the label that a Exit XXX statement should branch to, or Nothing if we are
        ''' not inside a context that would be exited by that kind of statement. The passed in kind
        ''' is the SyntaxKind for the exit statement that would target the label (e.g. SyntaxKind.ExitDoStatement).
        ''' </summary>
        Public Overridable Function GetExitLabel(exitSyntaxKind As SyntaxKind) As LabelSymbol
            Return m_containingBinder.GetExitLabel(exitSyntaxKind)
        End Function

        ''' <summary>
        ''' Get the label that a Continue XXX statement should branch to, or Nothing if we are
        ''' not inside a context that would be exited by that kind of statement. The passed in kind
        ''' is the SyntaxKind for the exit statement that would target the label (e.g. SyntaxKind.ContinueDoStatement).
        ''' </summary>
        Public Overridable Function GetContinueLabel(continueSyntaxKind As SyntaxKind) As LabelSymbol
            Return m_containingBinder.GetContinueLabel(continueSyntaxKind)
        End Function

        ''' <summary>
        ''' Get the label that a Return statement should branch to, or Nothing if we are
        ''' not inside a context that would be exited by that kind of statement. This method
        ''' is equivalent to calling <see cref="GetExitLabel"/> with the appropriate exit
        ''' <see cref="SyntaxKind"/>.
        ''' </summary>
        Public Overridable Function GetReturnLabel() As LabelSymbol
            Return m_containingBinder.GetReturnLabel()
        End Function

        ''' <summary>
        ''' Get the special local symbol with the same name as the enclosing function.
        ''' </summary>
        Public Overridable Function GetLocalForFunctionValue() As LocalSymbol
            Return m_containingBinder.GetLocalForFunctionValue()
        End Function

        ''' <summary>
        ''' Create a diagnostic at a particular syntax node and place it in a diagnostic bag.
        ''' </summary>
        Public Shared Sub ReportDiagnostic(diagBag As DiagnosticBag, syntax As SyntaxNodeOrToken, id As ERRID)
            ReportDiagnostic(diagBag, syntax, ErrorFactory.ErrorInfo(id))
        End Sub

        Public Shared Sub ReportDiagnostic(diagBag As DiagnosticBag, syntax As SyntaxNodeOrToken, id As ERRID, ParamArray args As Object())
            ReportDiagnostic(diagBag, syntax, ErrorFactory.ErrorInfo(id, args))
        End Sub

        Public Shared Sub ReportDiagnostic(diagBag As DiagnosticBag, syntax As SyntaxNodeOrToken, info As DiagnosticInfo)
            Dim diag As New VBDiagnostic(info, syntax.GetLocation())
            ReportDiagnostic(diagBag, diag)
        End Sub

        Public Shared Sub ReportDiagnostic(diagBag As DiagnosticBag, location As Location, id As ERRID)
            ReportDiagnostic(diagBag, location, ErrorFactory.ErrorInfo(id))
        End Sub

        Public Shared Sub ReportDiagnostic(diagBag As DiagnosticBag, location As Location, id As ERRID, ParamArray args As Object())
            ReportDiagnostic(diagBag, location, ErrorFactory.ErrorInfo(id, args))
        End Sub

        Public Shared Sub ReportDiagnostic(diagBag As DiagnosticBag, location As Location, info As DiagnosticInfo)
            Dim diag As New VBDiagnostic(info, location)
            ReportDiagnostic(diagBag, diag)
        End Sub

        Public Shared Sub ReportDiagnostic(diagBag As DiagnosticBag, diag As Diagnostic)
            diagBag.Add(diag)
        End Sub

        ''' <summary>
        ''' Issue an error or warning for a symbol if it is Obsolete. If there is not enough
        ''' information to report diagnostics, then store the symbols so that diagnostics
        ''' can be reported at a later stage.
        ''' </summary>
        Friend Sub ReportDiagnosticsIfObsolete(diagnostics As DiagnosticBag, symbol As Symbol, node As SyntaxNode)
            If Not Me.SuppressObsoleteDiagnostics Then
                ReportDiagnosticsIfObsolete(diagnostics, Me.ContainingMember, symbol, node)
            End If
        End Sub

        Friend Shared Sub ReportDiagnosticsIfObsolete(diagnostics As DiagnosticBag, context As Symbol, symbol As Symbol, node As SyntaxNode)
            Dim kind = ObsoleteAttributeHelpers.GetObsoleteDiagnosticKind(context, symbol)

            Dim info As DiagnosticInfo = Nothing
            Select Case kind
                Case ObsoleteDiagnosticKind.Diagnostic
                    info = ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(symbol)
                Case ObsoleteDiagnosticKind.Lazy, ObsoleteDiagnosticKind.LazyPotentiallySuppressed
                    info = New LazyObsoleteDiagnosticInfo(symbol, context)
            End Select

            If info IsNot Nothing Then
                diagnostics.Add(info, node.GetLocation())
            End If
        End Sub

        Friend Overridable ReadOnly Property SuppressObsoleteDiagnostics As Boolean
            Get
                Return m_containingBinder.SuppressObsoleteDiagnostics
            End Get
        End Property

        ''' <summary>
        ''' Returns the type of construct being bound (BaseTypes, MethodSignature,
        ''' etc.) to allow the Binder to provide different behavior in certain cases.
        ''' Currently, this property is only used by ShouldCheckConstraints.
        ''' </summary>
        Public Overridable ReadOnly Property BindingLocation As BindingLocation
            Get
                Return m_containingBinder.BindingLocation
            End Get
        End Property

        ''' <summary>
        ''' Returns true if the binder is performing early decoding of a
        ''' (well-known) attribute.
        ''' </summary>
        Public ReadOnly Property IsEarlyAttributeBinder As Boolean
            Get
                Return _isEarlyAttributeBinder
            End Get
        End Property

        ''' <summary>
        ''' Return True if type constraints should be checked when binding.
        ''' </summary>
        Friend ReadOnly Property ShouldCheckConstraints As Boolean
            Get
                Select Case Me.BindingLocation
                    Case BindingLocation.BaseTypes,
                         BindingLocation.MethodSignature,
                         BindingLocation.GenericConstraintsClause,
                         BindingLocation.ProjectImportsDeclaration,
                         BindingLocation.SourceFileImportsDeclaration
                        Return False

                    Case Else
                        Return True

                End Select
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the binder, or any containing binder, has xmlns Imports.
        ''' </summary>
        Friend Overridable ReadOnly Property HasImportedXmlNamespaces As Boolean
            Get
                Return m_containingBinder.HasImportedXmlNamespaces
            End Get
        End Property

        ''' <summary>
        ''' Add { prefix, namespace } pairs from the explicitly declared namespaces in the
        ''' XmlElement hierarchy. The order of the pairs is the order the xmlns attributes
        ''' are declared on each element, and from innermost to outermost element.
        ''' </summary>
        Friend Overridable Sub GetInScopeXmlNamespaces(builder As ArrayBuilder(Of KeyValuePair(Of String, String)))
            m_containingBinder.GetInScopeXmlNamespaces(builder)
        End Sub

        Friend Overridable Function LookupXmlNamespace(prefix As String, ignoreXmlNodes As Boolean, <Out()> ByRef [namespace] As String, <Out()> ByRef fromImports As Boolean) As Boolean
            Return m_containingBinder.LookupXmlNamespace(prefix, ignoreXmlNodes, [namespace], fromImports)
        End Function

        ''' <summary>
        ''' This method reports use site errors if a required attribute constructor is missing.
        ''' Some attributes are considered to be optional (e.g. the CompilerGeneratedAttribute). In this case the use site
        ''' errors will be ignored.
        ''' </summary>
        Friend Function ReportUseSiteErrorForSynthesizedAttribute(
            attributeCtor As WellKnownMember,
            syntax As VisualBasicSyntaxNode,
            diagnostics As DiagnosticBag
        ) As Boolean
            Dim useSiteError As DiagnosticInfo = Nothing

            Dim ctor As Symbol = GetWellKnownTypeMember(Me.Compilation, attributeCtor, useSiteError)

            If Not WellKnownMembers.IsSynthesizedAttributeOptional(attributeCtor) Then
                Debug.Assert(diagnostics IsNot Nothing)

                If useSiteError IsNot Nothing Then
                    ReportDiagnostic(diagnostics, syntax, useSiteError)
                    Return True
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' This method reports use site errors if a required attribute constructor is missing.
        ''' Some attributes are considered to be optional (e.g. the CompilerGeneratedAttribute). In this case the use site
        ''' errors will be ignored.
        ''' </summary>
        Friend Shared Function ReportUseSiteErrorForSynthesizedAttribute(
            attributeCtor As WellKnownMember,
            compilation As VisualBasicCompilation,
            location As Location,
            diagnostics As DiagnosticBag
        ) As Boolean
            Dim memberSymbol = compilation.GetWellKnownTypeMember(attributeCtor)
            Dim useSiteError As DiagnosticInfo = GetUseSiteErrorForWellKnownTypeMember(memberSymbol, attributeCtor, compilation.Options.EmbedVbCoreRuntime)

            If Not WellKnownMembers.IsSynthesizedAttributeOptional(attributeCtor) Then
                Debug.Assert(diagnostics IsNot Nothing)

                If useSiteError IsNot Nothing Then
                    diagnostics.Add(New VBDiagnostic(useSiteError, location))
                    Return True
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Returns a placeholder substitute for a With statement placeholder specified or Nothing if not found
        '''
        ''' Note: 'placeholder' is needed to make sure the binder can check that the placeholder is
        ''' associated with the statement.
        ''' </summary>
        Friend Overridable Function GetWithStatementPlaceholderSubstitute(placeholder As BoundValuePlaceholderBase) As BoundExpression
            Return m_containingBinder.GetWithStatementPlaceholderSubstitute(placeholder)
        End Function

        ''' <summary>
        ''' Indicates that this binder is being used to answer SemanticModel questions (i.e. not
        ''' for batch compilation).
        ''' </summary>
        ''' <remarks>
        ''' Imports touched by a binder with this flag set are not consider "used".
        ''' </remarks>
        Public Overridable ReadOnly Property IsSemanticModelBinder As Boolean
            Get
                Return m_containingBinder.IsSemanticModelBinder
            End Get
        End Property

        Public Overridable ReadOnly Property QuickAttributeChecker As QuickAttributeChecker
            Get
                Return m_containingBinder.QuickAttributeChecker
            End Get
        End Property
    End Class

End Namespace
