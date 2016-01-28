' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a type or module declared in source. 
    ''' Could be a class, structure, interface, delegate, enum, or module.
    ''' </summary>
    Partial Friend Class SourceNamedTypeSymbol
        Inherits SourceMemberContainerTypeSymbol
        Implements IAttributeTargetSymbol

        ' Type parameters (Nothing if not created yet)
        Private _lazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        ' Attributes on type. Set once after construction. IsNull means not set.  
        Protected m_lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        Private ReadOnly _corTypeId As SpecialType

        Private _lazyDocComment As String
        Private _lazyEnumUnderlyingType As NamedTypeSymbol

        ' Stores symbols for overriding WithEvents properties if we have such
        ' Overriding properties are created when a methods "Handles" is bound and can happen concurrently.
        ' We need this table to ensure that we create each override just once.
        Private _lazyWithEventsOverrides As ConcurrentDictionary(Of PropertySymbol, SynthesizedOverridingWithEventsProperty)

        ' method flags for the synthesized delegate methods
        Friend Const DelegateConstructorMethodFlags As SourceMemberFlags = SourceMemberFlags.MethodKindConstructor
        Friend Const DelegateCommonMethodFlags As SourceMemberFlags = SourceMemberFlags.Overridable

        Private _lazyLexicalSortKey As LexicalSortKey = LexicalSortKey.NotInitialized

        Private _lazyIsExtensibleInterface As ThreeState = ThreeState.Unknown
        Private _lazyIsExplicitDefinitionOfNoPiaLocalType As ThreeState = ThreeState.Unknown

        ''' <summary>
        ''' Information for ComClass specific analysis and metadata generation, created
        ''' once ComClassAttribute is encountered.
        ''' </summary>
        Private _comClassData As ComClassData

        ''' <summary>
        ''' Lazy CoClass type if the attribute is specified. Nothing if not.
        ''' </summary>
        Private _lazyCoClassType As TypeSymbol = ErrorTypeSymbol.UnknownResultType

        ''' <summary>
        ''' In case a cyclic dependency was detected during base type resolution 
        ''' this field stores the diagnostic.
        ''' </summary>
        Protected m_baseCycleDiagnosticInfo As DiagnosticInfo = Nothing


        ' Create the type symbol and associated type parameter symbols. Most information
        ' is deferred until later.
        Friend Sub New(declaration As MergedTypeDeclaration,
                          containingSymbol As NamespaceOrTypeSymbol,
                          containingModule As SourceModuleSymbol)

            MyBase.New(declaration, containingSymbol, containingModule)

            ' check if this is one of the COR library types
            If containingSymbol.Kind = SymbolKind.Namespace AndAlso
               containingSymbol.ContainingAssembly.KeepLookingForDeclaredSpecialTypes AndAlso
               Me.DeclaredAccessibility = Accessibility.Public Then

                Dim emittedName As String = If(Me.GetEmittedNamespaceName(), Me.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))

                Debug.Assert((Arity <> 0) = MangleName)
                emittedName = MetadataHelpers.BuildQualifiedName(emittedName, MetadataName)
                _corTypeId = SpecialTypes.GetTypeFromMetadataName(emittedName)
            Else
                _corTypeId = SpecialType.None
            End If

            If containingSymbol.Kind = SymbolKind.NamedType Then
                ' Nested types are never unified.
                _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.False
            End If
        End Sub

        Public Overrides ReadOnly Property SpecialType As SpecialType
            Get
                Return _corTypeId
            End Get
        End Property

#Region "Completion"
        Protected Overrides Sub GenerateAllDeclarationErrorsImpl(cancellationToken As CancellationToken)
            MyBase.GenerateAllDeclarationErrorsImpl(cancellationToken)

            cancellationToken.ThrowIfCancellationRequested()
            PerformComClassAnalysis()

            cancellationToken.ThrowIfCancellationRequested()
            CheckBaseConstraints()

            cancellationToken.ThrowIfCancellationRequested()
            CheckInterfacesConstraints()
        End Sub
#End Region

#Region "Syntax"

        Friend Function GetTypeIdentifierToken(node As VisualBasicSyntaxNode) As SyntaxToken
            Select Case node.Kind
                Case SyntaxKind.ModuleBlock, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock
                    Return DirectCast(node, TypeBlockSyntax).BlockStatement.Identifier

                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement.Identifier

                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    Return DirectCast(node, DelegateStatementSyntax).Identifier

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            If _lazyDocComment Is Nothing Then
                ' NOTE: replace Nothing with empty comment
                Interlocked.CompareExchange(
                    _lazyDocComment, GetDocumentationCommentForSymbol(Me, preferredCulture, expandIncludes, cancellationToken), Nothing)
            End If
            Return _lazyDocComment
        End Function

        ' Create a LocationSpecificBinder for the type. This is a binder that wraps the
        ' default binder for the type in a binder that will avoid checking constraints,
        ' for cases where constraint checking may result in a recursive binding attempt.
        Private Function CreateLocationSpecificBinderForType(tree As SyntaxTree, location As BindingLocation) As Binder
            Debug.Assert(location <> BindingLocation.None)
            Dim binder As binder = BinderBuilder.CreateBinderForType(ContainingSourceModule, tree, Me)
            Return New LocationSpecificBinder(location, binder)
        End Function
#End Region

#Region "Members"

        Protected Overrides Sub AddDeclaredNonTypeMembers(membersBuilder As SourceMemberContainerTypeSymbol.MembersAndInitializersBuilder, diagnostics As DiagnosticBag)
            Dim accessModifiers As DeclarationModifiers = Nothing
            Dim foundModifiers As DeclarationModifiers

            Dim foundPartial As Boolean = False
            Dim nodeNameIsAlreadyDefined As Boolean = False
            Dim firstNode As VisualBasicSyntaxNode = Nothing
            Dim countMissingPartial = 0

            For Each syntaxRef In SyntaxReferences
                Dim node = syntaxRef.GetVisualBasicSyntax()

                ' Set up a binder for this part of the type.
                Dim binder As binder = BinderBuilder.CreateBinderForType(ContainingSourceModule, syntaxRef.SyntaxTree, Me)

                ' Script and implicit classes are syntactically represented by CompilationUnitSyntax or NamespaceBlockSyntax nodes.
                Dim staticInitializers As ArrayBuilder(Of FieldOrPropertyInitializer) = Nothing
                Dim instanceInitializers As ArrayBuilder(Of FieldOrPropertyInitializer) = Nothing

                foundModifiers = AddMembersInPart(binder,
                                                  node,
                                                  diagnostics,
                                                  accessModifiers,
                                                  membersBuilder,
                                                  staticInitializers,
                                                  instanceInitializers,
                                                  nodeNameIsAlreadyDefined)

                If accessModifiers = Nothing Then
                    accessModifiers = foundModifiers And DeclarationModifiers.AllAccessibilityModifiers
                End If

                If (foundModifiers And DeclarationModifiers.Partial) <> 0 Then
                    If Not foundPartial Then
                        firstNode = node
                        foundPartial = True
                    End If
                Else
                    countMissingPartial += 1
                    If firstNode Is Nothing Then
                        firstNode = node
                    End If
                End If

                ' add the collected initializers for this (partial) type to the collections
                ' and free the array builders
                AddInitializers(membersBuilder.StaticInitializers, staticInitializers)
                AddInitializers(membersBuilder.InstanceInitializers, instanceInitializers)
            Next

            If Not nodeNameIsAlreadyDefined AndAlso countMissingPartial >= 2 Then
                ' Only check partials if no duplicate symbols were found and at least two class declarations are missing the partial keyword.

                For Each syntaxRef In SyntaxReferences
                    ' Report a warning or error for all classes missing the partial modifier
                    CheckDeclarationPart(syntaxRef.SyntaxTree, syntaxRef.GetVisualBasicSyntax(), firstNode, foundPartial, diagnostics)
                Next
            End If
        End Sub

        ' Declare all the non-type members in a single part of this type, and add them to the member list.
        Private Function AddMembersInPart(binder As Binder,
                                          node As VisualBasicSyntaxNode,
                                          diagBag As DiagnosticBag,
                                          accessModifiers As DeclarationModifiers,
                                          members As MembersAndInitializersBuilder,
                                          ByRef staticInitializers As ArrayBuilder(Of FieldOrPropertyInitializer),
                                          ByRef instanceInitializers As ArrayBuilder(Of FieldOrPropertyInitializer),
                                          ByRef nodeNameIsAlreadyDefined As Boolean) As DeclarationModifiers

            ' Check that the node's fully qualified name is not too long and that the type name is unique.
            CheckDeclarationNameAndTypeParameters(node, binder, diagBag, nodeNameIsAlreadyDefined)

            Dim foundModifiers = CheckDeclarationModifiers(node, binder, diagBag, accessModifiers)

            If TypeKind = TypeKind.Delegate Then
                ' add implicit delegate members (invoke, .ctor, begininvoke and endinvoke)
                If members.Members.Count = 0 Then
                    Dim ctor As MethodSymbol = Nothing
                    Dim beginInvoke As MethodSymbol = Nothing
                    Dim endInvoke As MethodSymbol = Nothing
                    Dim invoke As MethodSymbol = Nothing

                    Dim parameters = DirectCast(node, DelegateStatementSyntax).ParameterList
                    SourceDelegateMethodSymbol.MakeDelegateMembers(Me, node, parameters, binder, ctor, beginInvoke, endInvoke, invoke, diagBag)

                    AddSymbolToMembers(ctor, members.Members)

                    ' If this is a winmd compilation begin/endInvoke will be Nothing
                    ' and we shouldn't add them to the symbol
                    If beginInvoke IsNot Nothing Then
                        AddSymbolToMembers(beginInvoke, members.Members)
                    End If
                    If endInvoke IsNot Nothing Then
                        AddSymbolToMembers(endInvoke, members.Members)
                    End If

                    ' Invoke must always be the last member
                    AddSymbolToMembers(invoke, members.Members)
                Else
                    Debug.Assert(members.Members.Count = 4)
                End If

            ElseIf TypeKind = TypeKind.Enum Then
                Dim enumBlock = DirectCast(node, EnumBlockSyntax)
                AddEnumMembers(enumBlock, binder, diagBag, members)
            Else
                Dim typeBlock = DirectCast(node, TypeBlockSyntax)
                For Each memberSyntax In typeBlock.Members
                    AddMember(memberSyntax, binder, diagBag, members, staticInitializers, instanceInitializers, reportAsInvalid:=False)
                Next
            End If

            Return foundModifiers
        End Function

        Private Function CheckDeclarationModifiers(node As VisualBasicSyntaxNode,
                                                   binder As Binder,
                                                   diagBag As DiagnosticBag,
                                                   accessModifiers As DeclarationModifiers) As DeclarationModifiers

            Dim modifiers As SyntaxTokenList = Nothing
            Dim id As SyntaxToken = Nothing
            Dim foundModifiers = DecodeDeclarationModifiers(node, binder, diagBag, modifiers, id)

            If accessModifiers <> Nothing Then
                Dim newModifiers = foundModifiers And DeclarationModifiers.AllAccessibilityModifiers And Not accessModifiers

                ' Specified access '|1' for '|2' does not match the access '|3' specified on one of its other partial types.
                If newModifiers <> 0 Then
                    Binder.ReportDiagnostic(diagBag,
                                            id,
                                            ERRID.ERR_PartialTypeAccessMismatch3,
                                            newModifiers.ToAccessibility().ToDisplay(),
                                            id.ToString(),
                                            accessModifiers.ToAccessibility().ToDisplay())
                End If
            End If

            If Me.IsNotInheritable Then

                ' 'MustInherit' cannot be specified for partial type '|1' because it cannot be combined with 'NotInheritable'
                '  specified for one of its other partial types.
                If (foundModifiers And DeclarationModifiers.MustInherit) <> 0 Then
                    ' Generate error #30926 only if this (partial) declaration does not have both MustInherit and
                    ' NotInheritable (in which case #31408 error must have been generated which should be enough in this
                    ' case). 
                    If (foundModifiers And DeclarationModifiers.NotInheritable) = 0 Then
                        ' Note: in case one partial declaration has both MustInherit & NotInheritable and other partial
                        ' declarations have MustInherit, #31408 will be generated for the first one and #30926 for all
                        ' others with MustInherit
                        Binder.ReportDiagnostic(diagBag, id, ERRID.ERR_PartialTypeBadMustInherit1, id.ToString())
                    End If
                End If

            End If

            Dim containingType = TryCast(Me.ContainingType, SourceNamedTypeSymbol)
            ' IsNested means this is in a Class or Module or Structure
            Dim isNested = containingType IsNot Nothing AndAlso Not containingType.IsNamespace

            If isNested Then

                Select Case containingType.DeclarationKind
                    Case VisualBasic.Symbols.DeclarationKind.Module
                        If (foundModifiers And DeclarationModifiers.InvalidInModule) <> 0 Then
                            binder.ReportModifierError(modifiers, ERRID.ERR_ModuleCantUseTypeSpecifier1, diagBag, InvalidModifiersInModule)
                            foundModifiers = (foundModifiers And (Not DeclarationModifiers.InvalidInModule))
                        End If

                    Case VisualBasic.Symbols.DeclarationKind.Interface
                        If (foundModifiers And DeclarationModifiers.InvalidInInterface) <> 0 Then
                            Dim err As ERRID = ERRID.ERR_None

                            Select Case Me.DeclarationKind
                                Case VisualBasic.Symbols.DeclarationKind.Class
                                    err = ERRID.ERR_BadInterfaceClassSpecifier1
                                Case VisualBasic.Symbols.DeclarationKind.Delegate
                                    err = ERRID.ERR_BadInterfaceDelegateSpecifier1
                                Case VisualBasic.Symbols.DeclarationKind.Structure
                                    err = ERRID.ERR_BadInterfaceStructSpecifier1
                                Case VisualBasic.Symbols.DeclarationKind.Enum
                                    err = ERRID.ERR_BadInterfaceEnumSpecifier1
                                Case VisualBasic.Symbols.DeclarationKind.Interface

                                    ' For whatever reason, Dev10 does not report an error on [Friend] or [Public] modifier on an interface inside an interface.
                                    ' Need to handle this specially
                                    Dim invalidModifiers = DeclarationModifiers.InvalidInInterface And (Not (DeclarationModifiers.Friend Or DeclarationModifiers.Public))

                                    If (foundModifiers And invalidModifiers) <> 0 Then
                                        binder.ReportModifierError(modifiers, ERRID.ERR_BadInterfaceInterfaceSpecifier1, diagBag,
                                                                        SyntaxKind.PrivateKeyword,
                                                                        SyntaxKind.ProtectedKeyword,
                                                                        SyntaxKind.SharedKeyword)

                                        foundModifiers = (foundModifiers And (Not invalidModifiers))
                                    End If
                            End Select

                            If err <> ERRID.ERR_None Then
                                binder.ReportModifierError(modifiers, err, diagBag,
                                                                SyntaxKind.PrivateKeyword,
                                                                SyntaxKind.ProtectedKeyword,
                                                                SyntaxKind.FriendKeyword,
                                                                SyntaxKind.PublicKeyword,
                                                                SyntaxKind.SharedKeyword)

                                foundModifiers = (foundModifiers And (Not DeclarationModifiers.InvalidInInterface))
                            End If
                        End If

                End Select

            Else

                If (foundModifiers And DeclarationModifiers.Private) <> 0 Then
                    Binder.ReportDiagnostic(diagBag, id, ERRID.ERR_PrivateTypeOutsideType)
                End If

                If (foundModifiers And DeclarationModifiers.Shadows) <> 0 Then
                    Binder.ReportDiagnostic(diagBag, id, ERRID.ERR_ShadowingTypeOutsideClass1, id.ToString())
                    foundModifiers = (foundModifiers And (Not DeclarationModifiers.Shadows))
                End If

            End If

            ' Only nested type (not nested in a struct, nested in a class, etc. ) can be Protected.
            If (foundModifiers And DeclarationModifiers.Protected) <> 0 AndAlso
                (Not isNested OrElse containingType.DeclarationKind <> VisualBasic.Symbols.DeclarationKind.Class) Then
                Binder.ReportDiagnostic(diagBag, id, ERRID.ERR_ProtectedTypeOutsideClass)
                foundModifiers = (foundModifiers And (Not DeclarationModifiers.Protected))
            End If

            Return foundModifiers
        End Function

        Private Function DecodeDeclarationModifiers(node As VisualBasicSyntaxNode,
                                            binder As Binder,
                                            diagBag As DiagnosticBag,
                                            ByRef modifiers As SyntaxTokenList,
                                            ByRef id As SyntaxToken) As DeclarationModifiers
            Dim allowableModifiers = SourceMemberFlags.AllAccessibilityModifiers Or SourceMemberFlags.Shadows
            Dim err = ERRID.ERR_None
            Dim typeBlock As TypeBlockSyntax

            Select Case node.Kind
                Case SyntaxKind.ModuleBlock
                    err = ERRID.ERR_BadModuleFlags1
                    allowableModifiers = SourceMemberFlags.AllAccessibilityModifiers Or SourceMemberFlags.Partial
                    typeBlock = DirectCast(node, TypeBlockSyntax)
                    modifiers = typeBlock.BlockStatement.Modifiers
                    id = typeBlock.BlockStatement.Identifier

                Case SyntaxKind.ClassBlock
                    err = ERRID.ERR_BadClassFlags1
                    allowableModifiers = SourceMemberFlags.AllAccessibilityModifiers Or SourceMemberFlags.Shadows Or SourceMemberFlags.MustInherit Or SourceMemberFlags.NotInheritable Or SourceMemberFlags.Partial
                    typeBlock = DirectCast(node, TypeBlockSyntax)
                    modifiers = typeBlock.BlockStatement.Modifiers
                    id = typeBlock.BlockStatement.Identifier

                Case SyntaxKind.StructureBlock
                    err = ERRID.ERR_BadRecordFlags1
                    allowableModifiers = SourceMemberFlags.AllAccessibilityModifiers Or SourceMemberFlags.Shadows Or SourceMemberFlags.Partial
                    typeBlock = DirectCast(node, TypeBlockSyntax)
                    modifiers = typeBlock.BlockStatement.Modifiers
                    id = typeBlock.BlockStatement.Identifier

                Case SyntaxKind.InterfaceBlock
                    err = ERRID.ERR_BadInterfaceFlags1
                    allowableModifiers = SourceMemberFlags.AllAccessibilityModifiers Or SourceMemberFlags.Shadows Or SourceMemberFlags.Partial
                    typeBlock = DirectCast(node, TypeBlockSyntax)
                    modifiers = typeBlock.BlockStatement.Modifiers
                    id = typeBlock.BlockStatement.Identifier

                Case SyntaxKind.EnumBlock
                    err = ERRID.ERR_BadEnumFlags1
                    Dim enumBlock As EnumBlockSyntax = DirectCast(node, EnumBlockSyntax)
                    modifiers = enumBlock.EnumStatement.Modifiers
                    id = enumBlock.EnumStatement.Identifier

                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    err = ERRID.ERR_BadDelegateFlags1
                    modifiers = DirectCast(node, DelegateStatementSyntax).Modifiers
                    id = DirectCast(node, DelegateStatementSyntax).Identifier

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)

            End Select

            If modifiers.Count <> 0 Then
                Dim foundFlags As SourceMemberFlags = binder.DecodeModifiers(modifiers,
                    allowableModifiers,
                    err,
                    Nothing,
                    diagBag).FoundFlags

                Return CType((foundFlags And SourceMemberFlags.DeclarationModifierFlagMask) >> SourceMemberFlags.DeclarationModifierFlagShift, DeclarationModifiers)
            End If

            Return Nothing
        End Function

        Private Sub CheckDeclarationNameAndTypeParameters(node As VisualBasicSyntaxNode,
                                                          binder As Binder,
                                                          diagBag As DiagnosticBag,
                                                          ByRef nodeNameIsAlreadyDeclared As Boolean)

            ' Check that the node's fully qualified name is not too long. Only check declarations that create types.

            Dim id As SyntaxToken = GetTypeIdentifierToken(node)

            binder.DisallowTypeCharacter(id, diagBag)

            Dim _6thArg As Object = Me.ContainingSymbol.ToErrorMessageArgument()

            Dim thisTypeIsEmbedded As Boolean = Me.IsEmbedded

            ' Check name for duplicate type declarations in this container
            Dim container = TryCast(Me.ContainingSymbol, NamespaceOrTypeSymbol)
            If container IsNot Nothing Then

                ' Get all type or namespace symbols with this name. 
                Dim symbols As ImmutableArray(Of Symbol)
                If container.IsNamespace Then
                    symbols = container.GetMembers(Me.Name)
                Else
                    symbols = StaticCast(Of Symbol).From(container.GetTypeMembers(Me.Name))
                End If

                Dim arity As Integer = Me.Arity

                For Each s In symbols
                    If s IsNot Me Then
                        Dim _3rdArg As Object

                        Select Case s.Kind
                            Case SymbolKind.Namespace
                                If arity > 0 Then
                                    Continue For
                                End If

                                _3rdArg = DirectCast(s, NamespaceSymbol).GetKindText()

                            Case SymbolKind.NamedType

                                Dim contender = DirectCast(s, NamedTypeSymbol)

                                If contender.Arity <> arity Then
                                    Continue For
                                End If

                                _3rdArg = contender.GetKindText()

                            Case Else
                                Continue For
                        End Select

                        If s.IsEmbedded Then

                            ' We expect 'this' type not to be an embedded type in this 
                            ' case because otherwise it should be design time bug.
                            Debug.Assert(Not thisTypeIsEmbedded)

                            ' This non-embedded type conflicts with an embedded type or namespace
                            Binder.ReportDiagnostic(diagBag, id, ERRID.ERR_TypeClashesWithVbCoreType4,
                                                    Me.GetKindText(), id.ToString, _3rdArg, s.Name)

                        ElseIf thisTypeIsEmbedded Then
                            ' Embedded type conflicts with non-embedded type or namespace.
                            ' We should ignore non-embedded types in this case, as a proper 
                            ' diagnostic will be reported when the non-embedded type is processed.
                            If s.Kind = SymbolKind.Namespace Then

                                ' But we should report errors on the first namespace locations 
                                Dim errorReported As Boolean = False
                                For Each location In s.Locations
                                    If location.IsInSource AndAlso Not DirectCast(location.SourceTree, VisualBasicSyntaxTree).IsEmbeddedSyntaxTree Then
                                        Binder.ReportDiagnostic(diagBag, location, ERRID.ERR_TypeClashesWithVbCoreType4,
                                                                _3rdArg, s.Name, Me.GetKindText(), id.ToString)
                                        errorReported = True
                                        Exit For
                                    End If
                                Next

                                If errorReported Then
                                    Exit For
                                End If
                            End If
                            Continue For ' continue analysis of the type if no errors were reported

                        Else
                            ' Neither of types is embedded.
                            If (Me.ContainingType Is Nothing OrElse
                                    container.Locations.Length = 1 OrElse
                                    Not (TypeOf container Is SourceMemberContainerTypeSymbol) OrElse
                                    CType(container, SourceMemberContainerTypeSymbol).IsPartial) Then
                                Binder.ReportDiagnostic(diagBag, id, ERRID.ERR_TypeConflict6,
                                                        Me.GetKindText(), id.ToString, _3rdArg, s.Name,
                                                        container.GetKindText(), _6thArg)
                            End If

                        End If

                        nodeNameIsAlreadyDeclared = True
                        Exit For
                    End If
                Next

                If Not nodeNameIsAlreadyDeclared AndAlso container.IsNamespace AndAlso Me.ContainingAssembly.Modules.Length > 1 Then
                    ' Check for collision with types from added modules
                    Dim containingNamespace = DirectCast(container, NamespaceSymbol)
                    Dim mergedAssemblyNamespace = TryCast(Me.ContainingAssembly.GetAssemblyNamespace(containingNamespace), MergedNamespaceSymbol)

                    If mergedAssemblyNamespace IsNot Nothing Then
                        Dim targetQualifiedNamespaceName As String = If(Me.GetEmittedNamespaceName(),
                                                                        containingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))

                        Dim collision As NamedTypeSymbol = Nothing

                        For Each constituent As NamespaceSymbol In mergedAssemblyNamespace.ConstituentNamespaces
                            If constituent Is container Then
                                Continue For
                            End If

                            If collision IsNot Nothing AndAlso collision.ContainingModule.Ordinal < constituent.ContainingModule.Ordinal Then
                                Continue For
                            End If

                            Dim contenders As ImmutableArray(Of NamedTypeSymbol) = constituent.GetTypeMembers(Me.Name, arity)

                            If contenders.Length = 0 Then
                                Continue For
                            End If

                            Dim constituentQualifiedName As String = constituent.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat)

                            For Each namedType In contenders
                                If namedType.DeclaredAccessibility = Accessibility.Public AndAlso namedType.MangleName = Me.MangleName Then
                                    ' Because namespaces are merged case-insensitively,
                                    ' we need to make sure that we have a match for
                                    ' full emitted name of the type.
                                    If String.Equals(Me.Name, namedType.Name, StringComparison.Ordinal) AndAlso
                                       String.Equals(targetQualifiedNamespaceName, If(namedType.GetEmittedNamespaceName(), constituentQualifiedName), StringComparison.Ordinal) Then
                                        collision = namedType
                                        Exit For
                                    End If
                                End If
                            Next
                        Next

                        If collision IsNot Nothing Then
                            Binder.ReportDiagnostic(diagBag, id, ERRID.ERR_CollisionWithPublicTypeInModule, Me, collision.ContainingModule)
                        End If
                    End If
                End If
            End If

            ' Check name against type parameters of immediate container
            Dim containingSourceType = TryCast(container, SourceNamedTypeSymbol)
            If containingSourceType IsNot Nothing AndAlso containingSourceType.TypeParameters.MatchesAnyName(Me.Name) Then
                ' "'|1' has the same name as a type parameter."
                Binder.ReportDiagnostic(diagBag, id, ERRID.ERR_ShadowingGenericParamWithMember1, Me.Name)
            End If

            ' Check the source symbol type parameters for duplicates and shadowing
            CheckForDuplicateTypeParameters(TypeParameters, diagBag)
        End Sub

        Private Sub CheckDeclarationPart(tree As SyntaxTree,
                                            node As VisualBasicSyntaxNode,
                                            firstNode As VisualBasicSyntaxNode,
                                            foundPartial As Boolean,
                                            diagBag As DiagnosticBag)

            ' No error or warning on the first declaration
            If node Is firstNode Then
                Return
            End If

            ' Set up a binder for this part of the type.
            Dim binder As Binder = BinderBuilder.CreateBinderForType(ContainingSourceModule, tree, Me)

            ' all type declarations are treated as possible partial types. Because these type have different base classes 
            ' we need to get the modifiers in different ways.
            ' class, interface, struct and module all are all derived from TypeBlockSyntax.
            ' delegate is derived from MethodBase
            Dim modifiers As SyntaxTokenList = Nothing
            Select Case node.Kind
                Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                    modifiers = DirectCast(node, DelegateStatementSyntax).Modifiers
                Case SyntaxKind.EnumBlock
                    modifiers = DirectCast(node, EnumBlockSyntax).EnumStatement.Modifiers
                Case SyntaxKind.ModuleBlock, SyntaxKind.ClassBlock,
                    SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock
                    modifiers = DirectCast(node, TypeBlockSyntax).BlockStatement.Modifiers
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select

            Dim id As SyntaxToken = Nothing

            ' because this method was called before, we will pass a new (unused) instance of 
            ' diagnostics to avoid duplicate error messages for the same nodes
            Dim unusedDiagnostics = DiagnosticBag.GetInstance()
            Dim foundModifiers = DecodeDeclarationModifiers(node, binder, unusedDiagnostics, modifiers, id)
            unusedDiagnostics.Free()

            If (foundModifiers And DeclarationModifiers.Partial) = 0 Then

                ' Ensure multiple class declarations all have partial.  Report a warning if more than 2 declarations are missing partial.
                ' VB allows one class declaration with partial and one declaration without partial because designer generated code
                ' may not have specified partial. This allows user-code to force it. However, VB does not allow more than one declaration
                ' to not have partial as this would (erroneously) make what would have been a error (duplicate declarations) compile.
                Dim _6thArg As Object = Me.ContainingSymbol.ToErrorMessageArgument()

                Dim identifier As String = GetTypeIdentifierToken(firstNode).ToString

                Dim nodeKindText = Me.GetKindText()

                Binder.ReportDiagnostic(diagBag, id, If(foundPartial, ERRID.WRN_TypeConflictButMerged6, ERRID.ERR_TypeConflict6),
                                             nodeKindText, id.ToString,
                                             nodeKindText, identifier,
                                             Me.ContainingSymbol.GetKindText(),
                                             _6thArg)
            End If

        End Sub

        Private Sub AddEnumMembers(syntax As EnumBlockSyntax,
                                   bodyBinder As Binder,
                                   diagnostics As DiagnosticBag,
                                   members As MembersAndInitializersBuilder)

            Dim valField = New SynthesizedFieldSymbol(
                        Me,
                        Me,
                        Me.EnumUnderlyingType,
                        WellKnownMemberNames.EnumBackingFieldName,
                        accessibility:=Accessibility.Public,
                        isSpecialNameAndRuntimeSpecial:=True)

            AddMember(valField, bodyBinder, members, omitDiagnostics:=False)

            ' The previous enum constant used to calculate subsequent
            ' implicit enum constants. (This is the most recent explicit
            ' enum constant or the first implicit constant if no explicit values.)
            Dim otherSymbol As SourceEnumConstantSymbol = Nothing

            ' Offset from "otherSymbol".
            Dim otherSymbolOffset As Integer = 0

            If syntax.Members.Count = 0 Then
                Binder.ReportDiagnostic(diagnostics, syntax.EnumStatement.Identifier, ERRID.ERR_BadEmptyEnum1, syntax.EnumStatement.Identifier.ValueText)
                Return
            End If

            For Each member In syntax.Members
                If member.Kind <> SyntaxKind.EnumMemberDeclaration Then
                    ' skip invalid syntax 
                    Continue For
                End If

                Dim declaration = DirectCast(member, EnumMemberDeclarationSyntax)
                Dim symbol As SourceEnumConstantSymbol
                Dim valueOpt = declaration.Initializer
                If valueOpt IsNot Nothing Then
                    symbol = SourceEnumConstantSymbol.CreateExplicitValuedConstant(Me, bodyBinder, declaration, diagnostics)
                Else
                    symbol = SourceEnumConstantSymbol.CreateImplicitValuedConstant(Me, bodyBinder, declaration, otherSymbol, otherSymbolOffset, diagnostics)
                End If

                If (valueOpt IsNot Nothing) OrElse (otherSymbol Is Nothing) Then
                    otherSymbol = symbol
                    otherSymbolOffset = 1
                Else
                    otherSymbolOffset = otherSymbolOffset + 1
                End If

                AddMember(symbol, bodyBinder, members, omitDiagnostics:=False)
            Next
        End Sub

#End Region

#Region "Type Parameters (phase 3)"

        Private Structure TypeParameterInfo
            Public Sub New(
                          variance As VarianceKind,
                          constraints As ImmutableArray(Of TypeParameterConstraint))
                Me.Variance = variance
                Me.Constraints = constraints
            End Sub

            Public ReadOnly Variance As VarianceKind
            Public ReadOnly Constraints As ImmutableArray(Of TypeParameterConstraint)

            Public ReadOnly Property Initialized As Boolean
                Get
                    Return Not Me.Constraints.IsDefault
                End Get
            End Property
        End Structure

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                If _lazyTypeParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedInitialize(_lazyTypeParameters, MakeTypeParameters())
                End If

                Return _lazyTypeParameters
            End Get
        End Property

        ''' <summary>
        ''' Bind the constraint declarations for the given type parameter.
        ''' </summary>
        ''' <remarks>
        ''' The caller is expected to handle constraint checking and any caching of results.
        ''' </remarks>
        Friend Sub BindTypeParameterConstraints(
                                       typeParameter As SourceTypeParameterOnTypeSymbol,
                                       <Out()> ByRef variance As VarianceKind,
                                       <Out()> ByRef constraints As ImmutableArray(Of TypeParameterConstraint),
                                       diagnostics As DiagnosticBag)
            Dim unused = GetTypeMembersDictionary()   ' forced nested types to be declared.
            Dim info As TypeParameterInfo = Nothing

            ' Go through all declarations, determining the type parameter information
            ' from each, and updating the type parameter and reporting errors.
            For Each syntaxRef In SyntaxReferences
                Dim tree = syntaxRef.SyntaxTree
                Dim syntaxNode = syntaxRef.GetVisualBasicSyntax()

                Dim allowVariance = False
                Select Case syntaxNode.Kind
                    Case SyntaxKind.InterfaceBlock, SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                        allowVariance = True
                End Select

                Dim typeParameterList = GetTypeParameterListSyntax(syntaxNode)
                CreateTypeParameterInfoInPart(tree, typeParameter, typeParameterList, allowVariance, info, diagnostics)
            Next

            Debug.Assert(info.Initialized)
            variance = info.Variance
            constraints = info.Constraints
        End Sub

        ' Create all the type parameter information from the given declaration.
        Private Sub CreateTypeParameterInfoInPart(tree As SyntaxTree,
                                                         typeParameter As SourceTypeParameterOnTypeSymbol,
                                                         typeParamListSyntax As TypeParameterListSyntax,
                                                         allowVarianceSpecifier As Boolean,
                                                         ByRef info As TypeParameterInfo,
                                                         diagBag As DiagnosticBag)
            Debug.Assert(typeParamListSyntax IsNot Nothing)
            Debug.Assert(typeParamListSyntax.Parameters.Count = Me.Arity) ' If this is false, something is really wrong with the declaration tree.

            ' Set up a binder for this part of the type.
            Dim binder As Binder = CreateLocationSpecificBinderForType(tree, BindingLocation.GenericConstraintsClause)
            Dim typeParamSyntax = typeParamListSyntax.Parameters(typeParameter.Ordinal)

            ' Handle type parameter identifier.
            Dim identSymbol = typeParamSyntax.Identifier
            binder.DisallowTypeCharacter(identSymbol, diagBag, ERRID.ERR_TypeCharOnGenericParam)
            Dim name As String = identSymbol.ValueText

            ' Handle type parameter variance.
            Dim varianceKeyword = typeParamSyntax.VarianceKeyword
            Dim variance As VarianceKind = VarianceKind.None
            If varianceKeyword.Kind <> SyntaxKind.None Then
                If allowVarianceSpecifier Then
                    variance = binder.DecodeVariance(varianceKeyword)
                Else
                    Binder.ReportDiagnostic(diagBag, varianceKeyword, ERRID.ERR_VarianceDisallowedHere)
                End If
            End If

            ' Handle constraints.
            Dim constraints = binder.BindTypeParameterConstraintClause(Me, typeParamSyntax.TypeParameterConstraintClause, diagBag)

            If info.Initialized Then
                If Not IdentifierComparison.Equals(typeParameter.Name, name) Then
                    ' "Type parameter name '{0}' does not match the name '{1}' of the corresponding type parameter defined on one of the other partial types of '{2}'."
                    Binder.ReportDiagnostic(diagBag, identSymbol, ERRID.ERR_PartialTypeTypeParamNameMismatch3, name, typeParameter.Name, Me.Name)
                End If

                If Not HaveSameConstraints(info.Constraints, constraints) Then
                    ' "Constraints for this type parameter do not match the constraints on the corresponding type parameter defined on one of the other partial types of '{0}'."
                    Binder.ReportDiagnostic(diagBag, identSymbol, ERRID.ERR_PartialTypeConstraintMismatch1, Me.Name)
                End If
            Else
                info = New TypeParameterInfo(variance, constraints)
            End If
        End Sub

        Private Shared Function HaveSameConstraints(constraints1 As ImmutableArray(Of TypeParameterConstraint),
                                                    constraints2 As ImmutableArray(Of TypeParameterConstraint)) As Boolean
            Dim n1 = constraints1.Length
            Dim n2 = constraints2.Length
            If n1 <> n2 Then
                Return False
            End If

            If (n1 = 0) AndAlso (n2 = 0) Then
                Return True
            End If

            If GetConstraintKind(constraints1) <> GetConstraintKind(constraints2) Then
                Return False
            End If

            ' Construct a HashSet<T> for one of the sets
            ' to allow O(n) comparison of the two sets.
            Dim constraintTypes1 = New HashSet(Of TypeSymbol)
            For Each constraint In constraints1
                Dim constraintType = constraint.TypeConstraint
                If constraintType IsNot Nothing Then
                    constraintTypes1.Add(constraintType)
                End If
            Next

            For Each constraint In constraints2
                Dim constraintType = constraint.TypeConstraint
                If (constraintType IsNot Nothing) AndAlso Not constraintTypes1.Contains(constraintType) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function GetConstraintKind(constraints As ImmutableArray(Of TypeParameterConstraint)) As TypeParameterConstraintKind
            Dim kind = TypeParameterConstraintKind.None
            For Each constraint In constraints
                kind = kind Or constraint.Kind
            Next
            Return kind
        End Function

        Private Function MakeTypeParameters() As ImmutableArray(Of TypeParameterSymbol)
            Dim n = TypeDeclaration.Arity
            If n = 0 Then
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End If

            Dim typeParameters(0 To n - 1) As TypeParameterSymbol

            For i = 0 To n - 1
                Dim syntaxRefBuilder = ArrayBuilder(Of SyntaxReference).GetInstance()
                Dim name As String = Nothing

                For Each syntaxRef In SyntaxReferences
                    Dim tree = syntaxRef.SyntaxTree
                    Dim syntaxNode = syntaxRef.GetVisualBasicSyntax()
                    Dim typeParamListSyntax = GetTypeParameterListSyntax(syntaxNode).Parameters
                    Debug.Assert(typeParamListSyntax.Count = n)

                    Dim typeParamSyntax = typeParamListSyntax(i)
                    If name Is Nothing Then
                        name = typeParamSyntax.Identifier.ValueText
                    End If
                    syntaxRefBuilder.Add(tree.GetReference(typeParamSyntax))
                Next

                Debug.Assert(name IsNot Nothing)
                Debug.Assert(syntaxRefBuilder.Count > 0)

                typeParameters(i) = New SourceTypeParameterOnTypeSymbol(Me, i, name, syntaxRefBuilder.ToImmutableAndFree())
            Next

            Return typeParameters.AsImmutableOrNull()
        End Function

        Private Shared Function GetTypeParameterListSyntax(syntax As VisualBasicSyntaxNode) As TypeParameterListSyntax
            Select Case syntax.Kind
                Case SyntaxKind.StructureBlock, SyntaxKind.ClassBlock, SyntaxKind.InterfaceBlock
                    Return DirectCast(syntax, TypeBlockSyntax).BlockStatement.TypeParameterList
                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    Return DirectCast(syntax, DelegateStatementSyntax).TypeParameterList
                Case Else
                    Return Nothing
            End Select
        End Function

        Friend Sub CheckForDuplicateTypeParameters(typeParameters As ImmutableArray(Of TypeParameterSymbol),
                                                   diagBag As DiagnosticBag)
            If Not typeParameters.IsDefault Then
                Dim typeParameterSet As New HashSet(Of String)(IdentifierComparison.Comparer)
                ' Check for duplicate type parameters
                For i = 0 To typeParameters.Length - 1
                    Dim s = typeParameters(i)
                    If Not typeParameterSet.Contains(s.Name) Then
                        typeParameterSet.Add(s.Name)

                        If ShadowsTypeParameter(s) Then
                            Binder.ReportDiagnostic(diagBag, s.Locations(0), ERRID.WRN_ShadowingGenericParamWithParam1, s.Name)
                        End If
                    Else
                        Binder.ReportDiagnostic(diagBag, s.Locations(0), ERRID.ERR_DuplicateTypeParamName1, s.Name)
                    End If
                Next
            End If
        End Sub

        Private Function ShadowsTypeParameter(typeParameter As TypeParameterSymbol) As Boolean
            Dim name As String = typeParameter.Name

            Dim containingType As SourceNamedTypeSymbol

            If typeParameter.TypeParameterKind = TypeParameterKind.Method Then
                containingType = Me
            Else
                containingType = TryCast(Me.ContainingType, SourceNamedTypeSymbol)
            End If

            While containingType IsNot Nothing
                If containingType.TypeParameters.MatchesAnyName(name) Then
                    Return True
                End If
                containingType = TryCast(containingType.ContainingType, SourceNamedTypeSymbol)
            End While
            Return False
        End Function

#End Region

#Region "Base Type and Interfaces (phase 4)"

        Private Sub MakeDeclaredBaseInPart(tree As SyntaxTree,
                                           syntaxNode As VisualBasicSyntaxNode,
                                           ByRef baseType As NamedTypeSymbol,
                                           basesBeingResolved As ConsList(Of Symbol),
                                           diagBag As DiagnosticBag)

            ' Set up a binder for this part of the type.
            Dim binder As Binder = CreateLocationSpecificBinderForType(tree, BindingLocation.BaseTypes)

            Select Case syntaxNode.Kind
                Case SyntaxKind.ClassBlock
                    Dim inheritsSyntax = DirectCast(syntaxNode, TypeBlockSyntax).Inherits

                    ' classes may have a base class
                    Dim thisBase As NamedTypeSymbol = ValidateClassBase(inheritsSyntax, baseType, basesBeingResolved, binder, diagBag)
                    If baseType Is Nothing Then
                        baseType = thisBase
                    End If

                Case SyntaxKind.StructureBlock
                    Dim inheritsSyntax = DirectCast(syntaxNode, TypeBlockSyntax).Inherits
                    CheckNoBase(inheritsSyntax, ERRID.ERR_StructCantInherit, diagBag)

                Case SyntaxKind.ModuleBlock
                    Dim inheritsSyntax = DirectCast(syntaxNode, TypeBlockSyntax).Inherits
                    CheckNoBase(inheritsSyntax, ERRID.ERR_ModuleCantInherit, diagBag)
            End Select
        End Sub

        Private Sub MakeDeclaredInterfacesInPart(tree As SyntaxTree,
                                                syntaxNode As VisualBasicSyntaxNode,
                                                interfaces As SetWithInsertionOrder(Of NamedTypeSymbol),
                                                basesBeingResolved As ConsList(Of Symbol),
                                                diagBag As DiagnosticBag)

            ' Set up a binder for this part of the type.
            Dim binder As Binder = CreateLocationSpecificBinderForType(tree, BindingLocation.BaseTypes)

            Select Case syntaxNode.Kind
                Case SyntaxKind.ClassBlock
                    Dim implementsSyntax = DirectCast(syntaxNode, TypeBlockSyntax).Implements
                    ' class may implement interfaces
                    ValidateImplementedInterfaces(implementsSyntax, interfaces, basesBeingResolved, binder, diagBag)

                Case SyntaxKind.StructureBlock
                    Dim implementsSyntax = DirectCast(syntaxNode, TypeBlockSyntax).Implements
                    ' struct may implement interfaces
                    ValidateImplementedInterfaces(implementsSyntax, interfaces, basesBeingResolved, binder, diagBag)

                Case SyntaxKind.InterfaceBlock
                    Dim implementsSyntax = DirectCast(syntaxNode, TypeBlockSyntax).Inherits
                    ' interface may inherit interfaces
                    ValidateInheritedInterfaces(implementsSyntax, interfaces, basesBeingResolved, binder, diagBag)

                Case SyntaxKind.ModuleBlock
                    Dim implementsSyntax = DirectCast(syntaxNode, TypeBlockSyntax).Implements
                    CheckNoBase(implementsSyntax, ERRID.ERR_ModuleCantImplement, diagBag)

            End Select
        End Sub

        ' Check that there are no base declarations in the given list, and report the given error if any are found.
        Private Sub CheckNoBase(Of T As InheritsOrImplementsStatementSyntax)(baseDeclList As SyntaxList(Of T),
                                errId As ERRID,
                                diagBag As DiagnosticBag)
            If baseDeclList.Count > 0 Then
                For Each baseDecl In baseDeclList
                    Binder.ReportDiagnostic(diagBag, baseDecl, errId)
                Next
            End If
        End Sub

        ' Validate the base class declared by a class, diagnosing errors.
        ' If a base class is found already in another partial, it is passed as baseInOtherPartial.
        ' Returns the base class if a good base class was found, otherwise Nothing.
        Private Function ValidateClassBase(inheritsSyntax As SyntaxList(Of InheritsStatementSyntax),
                                           baseInOtherPartial As NamedTypeSymbol,
                                           basesBeingResolved As ConsList(Of Symbol),
                                           binder As Binder,
                                           diagBag As DiagnosticBag) As NamedTypeSymbol

            If inheritsSyntax.Count = 0 Then Return Nothing

            ' Add myself to the set of classes whose bases are being resolved
            If basesBeingResolved Is Nothing Then
                basesBeingResolved = ConsList(Of Symbol).Empty.Prepend(Me)
            Else
                basesBeingResolved = basesBeingResolved.Prepend(Me)
            End If

            binder = New BasesBeingResolvedBinder(binder, basesBeingResolved)

            ' Get the first base class declared, and give errors for multiple base classes
            Dim baseClassSyntax As TypeSyntax = Nothing
            For Each baseDeclaration In inheritsSyntax
                If baseDeclaration.Kind = SyntaxKind.InheritsStatement Then
                    Dim inheritsDeclaration = DirectCast(baseDeclaration, InheritsStatementSyntax)
                    If baseClassSyntax IsNot Nothing OrElse inheritsDeclaration.Types.Count > 1 Then
                        Binder.ReportDiagnostic(diagBag, inheritsDeclaration, ERRID.ERR_MultipleExtends)
                    End If
                    If baseClassSyntax Is Nothing AndAlso inheritsDeclaration.Types.Count > 0 Then
                        baseClassSyntax = inheritsDeclaration.Types(0)
                    End If
                End If
            Next

            If baseClassSyntax Is Nothing Then
                Return Nothing
            End If

            ' Bind the base class.
            Dim baseClassType = binder.BindTypeSyntax(baseClassSyntax, diagBag, suppressUseSiteError:=True, resolvingBaseType:=True)
            If baseClassType Is Nothing Then
                Return Nothing
            End If

            ' Check to make sure the base class is valid.
            Dim diagInfo As DiagnosticInfo = Nothing
            Select Case baseClassType.TypeKind
                Case TypeKind.TypeParameter
                    Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_GenericParamBase2, "Class", Me.Name)
                    Return Nothing

                Case TypeKind.Interface, TypeKind.Enum, TypeKind.Delegate, TypeKind.Structure, TypeKind.Module, TypeKind.Array ' array can't really occur
                    Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InheritsFromNonClass)
                    Return Nothing

                Case TypeKind.Error, TypeKind.Unknown
                    Return DirectCast(baseClassType, NamedTypeSymbol)

                Case TypeKind.Class
                    If IsRestrictedBaseClass(baseClassType.SpecialType) Then
                        Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InheritsFromRestrictedType1, baseClassType)
                        Return Nothing

                    ElseIf DirectCast(baseClassType, NamedTypeSymbol).IsNotInheritable Then
                        Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InheritsFromCantInherit3, Me.Name, baseClassType.Name, baseClassType.GetKindText())
                        Return Nothing
                    End If
            End Select

            ' The same base class can be declared in multiple partials, but not different ones
            If baseInOtherPartial IsNot Nothing Then
                If Not baseClassType.Equals(baseInOtherPartial) Then
                    Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_BaseMismatchForPartialClass3,
                                             baseClassType, Me.Name, baseInOtherPartial)
                    Return Nothing
                End If

            ElseIf Not baseClassType.IsErrorType() Then

                ' Verify that we don't have public classes inheriting from private ones, etc.
                AccessCheck.VerifyAccessExposureOfBaseClassOrInterface(Me, baseClassSyntax, baseClassType, diagBag)
            End If

            Return DirectCast(baseClassType, NamedTypeSymbol)
        End Function

        Private Sub ValidateInheritedInterfaces(baseSyntax As SyntaxList(Of InheritsStatementSyntax),
                                                basesInOtherPartials As SetWithInsertionOrder(Of NamedTypeSymbol),
                                                basesBeingResolved As ConsList(Of Symbol),
                                                binder As Binder,
                                                diagBag As DiagnosticBag)

            If baseSyntax.Count = 0 Then Return

            ' Add myself to the set of classes whose bases are being resolved
            If basesBeingResolved Is Nothing Then
                basesBeingResolved = ConsList(Of Symbol).Empty.Prepend(Me)
            Else
                basesBeingResolved = basesBeingResolved.Prepend(Me)
            End If

            binder = New BasesBeingResolvedBinder(binder, basesBeingResolved)

            ' give errors for multiple base classes
            Dim interfacesInThisPartial As New HashSet(Of NamedTypeSymbol)()

            For Each baseDeclaration In baseSyntax
                Dim types = DirectCast(baseDeclaration, InheritsStatementSyntax).Types

                For Each baseClassSyntax In types
                    Dim typeSymbol = binder.BindTypeSyntax(baseClassSyntax, diagBag, suppressUseSiteError:=True)
                    Dim namedType = TryCast(typeSymbol, NamedTypeSymbol)

                    If namedType IsNot Nothing AndAlso interfacesInThisPartial.Contains(namedType) Then
                        Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_DuplicateInInherits1, typeSymbol)
                    Else
                        If namedType IsNot Nothing Then
                            interfacesInThisPartial.Add(namedType)
                        End If

                        ' Check to make sure the base interfaces are valid.
                        Select Case typeSymbol.TypeKind
                            Case TypeKind.TypeParameter
                                Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_GenericParamBase2, "Interface", Me.Name)
                                Continue For

                            Case TypeKind.Unknown
                                Continue For

                            Case TypeKind.Interface, TypeKind.Error
                                basesInOtherPartials.Add(namedType)

                                If Not typeSymbol.IsErrorType() Then
                                    ' Make sure that we aren't exposing an interface with a restricted type,
                                    ' e.g. a public interface can't inherit from a private interface
                                    AccessCheck.VerifyAccessExposureOfBaseClassOrInterface(Me, baseClassSyntax, typeSymbol, diagBag)
                                End If

                            Case Else
                                Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InheritsFromNonInterface)
                                Continue For
                        End Select
                    End If
                Next
            Next
        End Sub

        Private Sub ValidateImplementedInterfaces(baseSyntax As SyntaxList(Of ImplementsStatementSyntax),
                                                  basesInOtherPartials As SetWithInsertionOrder(Of NamedTypeSymbol),
                                                  basesBeingResolved As ConsList(Of Symbol),
                                                  binder As Binder,
                                                  diagBag As DiagnosticBag)

            If baseSyntax.Count = 0 Then Return

            If basesBeingResolved IsNot Nothing Then
                binder = New BasesBeingResolvedBinder(binder, basesBeingResolved)
            End If

            ' give errors for multiple base classes
            Dim interfacesInThisPartial As New HashSet(Of TypeSymbol)()

            For Each baseDeclaration In baseSyntax
                Dim types = DirectCast(baseDeclaration, ImplementsStatementSyntax).Types
                For Each baseClassSyntax In types
                    Dim typeSymbol = binder.BindTypeSyntax(baseClassSyntax, diagBag, suppressUseSiteError:=True)

                    If interfacesInThisPartial.Contains(typeSymbol) Then
                        Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InterfaceImplementedTwice1, typeSymbol)
                    Else
                        interfacesInThisPartial.Add(typeSymbol)

                        ' Check to make sure the base interfaces are valid.
                        Select Case typeSymbol.TypeKind
                            Case TypeKind.TypeParameter
                                Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_ImplementsGenericParam, "Interface", Me.Name)
                                Continue For

                            Case TypeKind.Unknown
                                Continue For

                            Case TypeKind.Interface, TypeKind.Error
                                basesInOtherPartials.Add(DirectCast(typeSymbol, NamedTypeSymbol))

                            Case Else
                                Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_BadImplementsType)
                                Continue For
                        End Select
                    End If
                Next
            Next
        End Sub

        ' Determines if this type is one of the special types we can't inherit from
        Private Function IsRestrictedBaseClass(type As SpecialType) As Boolean
            Select Case type
                Case SpecialType.System_Array,
                     SpecialType.System_Delegate,
                     SpecialType.System_MulticastDelegate,
                     SpecialType.System_Enum,
                     SpecialType.System_ValueType
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Private Function AsPeOrRetargetingType(potentialBaseType As TypeSymbol) As NamedTypeSymbol
            Dim peType As NamedTypeSymbol = TryCast(potentialBaseType, Symbols.Metadata.PE.PENamedTypeSymbol)
            If peType Is Nothing Then
                peType = TryCast(potentialBaseType, Retargeting.RetargetingNamedTypeSymbol)
            End If

            Return peType
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As NamedTypeSymbol
            ' For types nested in a source type symbol (not in a script class): 
            ' before resolving the base type ensure that enclosing type's base type is already resolved
            Dim containingSourceType = TryCast(ContainingSymbol, SourceNamedTypeSymbol)
            If containingSourceType IsNot Nothing Then
                containingSourceType.GetDeclaredBaseSafe(If(basesBeingResolved, ConsList(Of Symbol).Empty).Prepend(Me))
            End If

            Dim baseType As NamedTypeSymbol = Nothing

            ' Go through all the parts of this type, and declare the information in that part, 
            ' reporting errors appropriately.
            For Each decl In Me.TypeDeclaration.Declarations
                If decl.HasBaseDeclarations Then
                    Dim syntaxRef = decl.SyntaxReference
                    MakeDeclaredBaseInPart(syntaxRef.SyntaxTree, syntaxRef.GetVisualBasicSyntax(), baseType, basesBeingResolved, diagnostics)
                End If
            Next

            Return baseType
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            ' For types nested in a source type symbol (not in a script class): 
            ' before resolving the base type ensure that enclosing type's base type is already resolved
            Dim containingSourceType = TryCast(ContainingSymbol, SourceNamedTypeSymbol)
            If containingSourceType IsNot Nothing AndAlso containingSourceType.IsInterface Then
                containingSourceType.GetDeclaredBaseInterfacesSafe(If(basesBeingResolved, ConsList(Of Symbol).Empty).Prepend(Me))
            End If

            Dim interfaces As New SetWithInsertionOrder(Of NamedTypeSymbol)

            ' Go through all the parts of this type, and declare the information in that part, 
            ' reporting errors appropriately.
            For Each syntaxRef In SyntaxReferences
                MakeDeclaredInterfacesInPart(syntaxRef.SyntaxTree, syntaxRef.GetVisualBasicSyntax(), interfaces, basesBeingResolved, diagnostics)
            Next

            Return interfaces.InInsertionOrder.AsImmutable
        End Function

        Private Function GetInheritsLocation(base As NamedTypeSymbol) As Location
            Return GetInheritsOrImplementsLocation(base, True)
        End Function

        Protected Overrides Function GetInheritsOrImplementsLocation(base As NamedTypeSymbol, getInherits As Boolean) As Location
            Dim backupLocation As Location = Nothing

            For Each part In SyntaxReferences
                Dim typeBlock = DirectCast(part.GetSyntax(), TypeBlockSyntax)
                Dim inhDecl = If(getInherits,
                                   DirectCast(typeBlock.Inherits, IEnumerable(Of InheritsOrImplementsStatementSyntax)),
                                   DirectCast(typeBlock.Implements, IEnumerable(Of InheritsOrImplementsStatementSyntax)))
                Dim binder As Binder = CreateLocationSpecificBinderForType(part.SyntaxTree, BindingLocation.BaseTypes)

                Dim basesBeingResolved = ConsList(Of Symbol).Empty.Prepend(Me)
                binder = New BasesBeingResolvedBinder(binder, basesBeingResolved)

                Dim diag = New DiagnosticBag ' unused

                For Each t In inhDecl
                    If backupLocation Is Nothing Then
                        backupLocation = t.GetLocation()
                    End If

                    Dim types As SeparatedSyntaxList(Of TypeSyntax) =
                        If(getInherits, DirectCast(t, InheritsStatementSyntax).Types, DirectCast(t, ImplementsStatementSyntax).Types)

                    For Each typeSyntax In types
                        Dim bt = binder.BindTypeSyntax(typeSyntax, diag, suppressUseSiteError:=True)
                        If bt = base Then
                            Return typeSyntax.GetLocation()
                        End If
                    Next
                Next
            Next

            ' In recursive or circular cases, the BindTypeSyntax fails to give the same result as the circularity
            ' removing algorithm does. In this case, use the entire Inherits or Implements statement as the location. 
            Return backupLocation
        End Function

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
            Dim compilation As VisualBasicCompilation = Me.DeclaringCompilation

            Dim declaredBase As NamedTypeSymbol = Me.GetDeclaredBase(Nothing)
            If declaredBase IsNot Nothing Then
                Dim diag As DiagnosticInfo = If(m_baseCycleDiagnosticInfo, BaseTypeAnalysis.GetDependenceDiagnosticForBase(Me, declaredBase))
                If diag IsNot Nothing Then
                    Dim location = GetInheritsLocation(declaredBase)
                    ' TODO: if there is a cycle dependency in base type we might want to ignore all 
                    '       other diagnostics collected so far because they may be incorrectly generated 
                    '       because of the cycle -- check and decide if we want to do so
                    'diagnostics.Clear()
                    diagnostics.Add(New VBDiagnostic(diag, location))
                    Return New ExtendedErrorTypeSymbol(diag, False)
                End If
            End If

            Dim declaredOrDefaultBase As NamedTypeSymbol = declaredBase

            ' Get the default base type if none was declared
            If declaredOrDefaultBase Is Nothing AndAlso Me.SpecialType <> Microsoft.CodeAnalysis.SpecialType.System_Object Then

                Select Case TypeKind
                    Case TypeKind.Submission
                        ' check that System.Object is available. 
                        ' Although the submission semantically doesn't have a base class we need to emit one.
                        ReportUseSiteDiagnosticsForBaseType(Me.DeclaringCompilation.GetSpecialType(SpecialType.System_Object), declaredBase, diagnostics)
                        declaredOrDefaultBase = Nothing

                    Case TypeKind.Class
                        declaredOrDefaultBase = GetSpecialType(SpecialType.System_Object)

                    Case TypeKind.Interface
                        declaredOrDefaultBase = Nothing

                    Case TypeKind.Enum
                        declaredOrDefaultBase = GetSpecialType(SpecialType.System_Enum)

                    Case TypeKind.Structure
                        declaredOrDefaultBase = GetSpecialType(SpecialType.System_ValueType)

                    Case TypeKind.Delegate
                        declaredOrDefaultBase = GetSpecialType(SpecialType.System_MulticastDelegate)

                    Case TypeKind.Module
                        declaredOrDefaultBase = GetSpecialType(SpecialType.System_Object)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(TypeKind)

                End Select
            End If

            If declaredOrDefaultBase IsNot Nothing Then
                ReportUseSiteDiagnosticsForBaseType(declaredOrDefaultBase, declaredBase, diagnostics)
            End If

            Return declaredOrDefaultBase
        End Function

        Private Function GetSpecialType(type As SpecialType) As NamedTypeSymbol
            Return ContainingModule.ContainingAssembly.GetSpecialType(type)
        End Function

        Private Sub ReportUseSiteDiagnosticsForBaseType(baseType As NamedTypeSymbol, declaredBase As NamedTypeSymbol, diagnostics As DiagnosticBag)
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            Dim current As NamedTypeSymbol = baseType

            Do
                If current.DeclaringCompilation Is Me.DeclaringCompilation Then
                    Exit Do
                End If

                current.AddUseSiteDiagnostics(useSiteDiagnostics)
                current = current.BaseTypeNoUseSiteDiagnostics
            Loop While current IsNot Nothing

            If Not useSiteDiagnostics.IsNullOrEmpty Then
                Dim location As Location

                If declaredBase Is baseType Then
                    location = GetInheritsLocation(baseType)
                Else
                    Dim syntaxRef = SyntaxReferences.First()
                    Dim syntax = syntaxRef.GetVisualBasicSyntax()

                    ' script, submission and implicit classes have no identifier location:
                    location = If(syntax.Kind = SyntaxKind.CompilationUnit OrElse syntax.Kind = SyntaxKind.NamespaceBlock,
                                  Locations(0),
                                  GetTypeIdentifierToken(syntax).GetLocation())
                End If

                diagnostics.Add(location, useSiteDiagnostics)
            End If
        End Sub

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Dim declaredInterfaces As ImmutableArray(Of NamedTypeSymbol) = GetDeclaredInterfacesNoUseSiteDiagnostics(Nothing)

            Dim isInterface As Boolean = Me.IsInterfaceType()

            Dim result As ArrayBuilder(Of NamedTypeSymbol) = If(isInterface, ArrayBuilder(Of NamedTypeSymbol).GetInstance(), Nothing)

            For Each t In declaredInterfaces
                Dim diag = If(isInterface AndAlso Not t.IsErrorType(), GetDependenceDiagnosticForBase(Me, t), Nothing)

                If diag IsNot Nothing Then
                    Dim location = GetInheritsLocation(t)
                    diagnostics.Add(New VBDiagnostic(diag, location))
                    result.Add(New ExtendedErrorTypeSymbol(diag, False))
                Else
                    ' Error types were reported elsewhere.
                    If Not t.IsErrorType() Then
                        Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                        If t.DeclaringCompilation IsNot Me.DeclaringCompilation Then
                            t.AddUseSiteDiagnostics(useSiteDiagnostics)

                            For Each [interface] In t.AllInterfacesNoUseSiteDiagnostics
                                If [interface].DeclaringCompilation IsNot Me.DeclaringCompilation Then
                                    [interface].AddUseSiteDiagnostics(useSiteDiagnostics)
                                End If
                            Next
                        End If

                        If Not useSiteDiagnostics.IsNullOrEmpty Then
                            Dim location = If(isInterface, GetInheritsLocation(t), GetInheritsOrImplementsLocation(t, getInherits:=False))
                            diagnostics.Add(location, useSiteDiagnostics)
                        End If
                    End If

                    If isInterface Then
                        result.Add(t)
                    End If
                End If
            Next

            Return If(isInterface, result.ToImmutableAndFree, declaredInterfaces)
        End Function

        Friend Overrides Function GetDirectBaseTypeNoUseSiteDiagnostics(basesBeingResolved As ConsList(Of Symbol)) As NamedTypeSymbol
            Debug.Assert(Me.TypeKind <> TypeKind.Interface)

            If TypeKind = TypeKind.Enum Then
                ' Base type has the underlying type instead.
                Return GetSpecialType(SpecialType.System_Enum)
            ElseIf TypeKind = TypeKind.Delegate Then
                ' Base type has the underlying type instead.
                Return GetSpecialType(SpecialType.System_MulticastDelegate)
            Else
                If basesBeingResolved Is Nothing Then
                    Return Me.BaseTypeNoUseSiteDiagnostics
                Else
                    Return GetDeclaredBaseSafe(basesBeingResolved)
                End If
            End If
        End Function

        ''' <summary>
        ''' 'Safe' version of GetDeclaredBase takes into account bases being resolved to make sure 
        ''' we avoid infinite loops in some scenarios. Note that the cycle is being broken not when
        ''' we detect it, but when we detect it on the 'smallest' type of the cycle, this brings stability 
        ''' in multithreaded scenarios while still ensures that we don't loop more than twice.
        ''' </summary>
        Private Function GetDeclaredBaseSafe(basesBeingResolved As ConsList(Of Symbol)) As NamedTypeSymbol
            If m_baseCycleDiagnosticInfo IsNot Nothing Then
                ' We have already detected this type has a cycle and it was chosen 
                ' to be the one which reports the problem and breaks the cycle
                Return Nothing
            End If

            Debug.Assert(basesBeingResolved.Any)
            If Me Is basesBeingResolved.Head Then
                ' This is a little tricky: the head of 'basesBeingResolved' represents the innermost
                ' type whose base is being resolved. That means if we start name lookup with that type
                ' as containing type and if we cannot find the name in its scope we want just to skip base
                ' type search and avoid any errors. We want this to happen only for that innermost type
                ' in base resolution chain. An example:
                '
                '   Class A
                '       Class B
                '           Inherits D      ' Lookup for 'D' starts in scope of 'B', we 
                '           Class C         ' are skipping diving into B's base class here 
                '           End Class       ' to make it possible to find A.D
                '       End Class
                '       Class D
                '       End Class
                '   End Class

                ' NOTE: that it the lookup is not the first indirect one, but B was found earlier 
                '       during lookup process, we still can ignore B's base type because another 
                '       error (B cannot reference itself in its Inherits clause) should be generated
                '       by this time, like in the following example:
                '
                '   Class A
                '       Class B
                '           Inherits A.B.C  ' <- error BC31447: Class 'A.B' cannot 
                '           Class C         '    reference itself in Inherits clause.
                '           End Class     
                '       End Class
                '       Class D
                '       End Class
                '   End Class

                Return Nothing
            End If

            Dim diag As DiagnosticInfo = GetDependenceDiagnosticForBase(Me, basesBeingResolved)
            If diag Is Nothing Then
                Dim declaredBase As NamedTypeSymbol = GetDeclaredBase(basesBeingResolved)

                ' If we detected the cycle while calculating the declared base, return Nothing
                Return If(m_baseCycleDiagnosticInfo Is Nothing, declaredBase, Nothing)
            End If

            Dim prev = Interlocked.CompareExchange(m_baseCycleDiagnosticInfo, diag, Nothing)
            Debug.Assert(prev Is Nothing OrElse prev.GetMessage().Equals(diag.GetMessage()))
            Return Nothing
        End Function

        Friend Overrides Function GetDeclaredBaseInterfacesSafe(basesBeingResolved As ConsList(Of Symbol)) As ImmutableArray(Of NamedTypeSymbol)
            If m_baseCycleDiagnosticInfo IsNot Nothing Then
                ' We have already detected this type has a cycle and it was chosen 
                ' to be the one which reports the problem and breaks the cycle
                Return Nothing
            End If

            Debug.Assert(basesBeingResolved.Any)
            If Me Is basesBeingResolved.Head Then
                Return Nothing
            End If

            Dim diag As DiagnosticInfo = GetDependenceDiagnosticForBase(Me, basesBeingResolved)
            If diag Is Nothing Then
                Dim declaredBases As ImmutableArray(Of NamedTypeSymbol) = GetDeclaredInterfacesNoUseSiteDiagnostics(basesBeingResolved)

                ' If we detected the cycle while calculating the declared base, return Nothing
                Return If(m_baseCycleDiagnosticInfo Is Nothing, declaredBases, ImmutableArray(Of NamedTypeSymbol).Empty)
            End If

            Dim prev = Interlocked.CompareExchange(m_baseCycleDiagnosticInfo, diag, Nothing)
            Debug.Assert(prev Is Nothing OrElse prev.GetMessage().Equals(diag.GetMessage()))
            Return Nothing
        End Function

        ''' <summary>
        ''' Do additional verification of base types the after acyclic base is found. This is
        ''' the chance to generate diagnostics that may require walking bases and as such
        ''' can be performed only after the base has been determined and cycles broken.
        ''' (For instance, checking constraints on Class B(Of T) Inherits A(Of B(Of T)).)
        ''' </summary>
        Private Sub CheckBaseConstraints()
            If (m_lazyState And StateFlags.ReportedBaseClassConstraintsDiagnostics) <> 0 Then
                Return
            End If

            Dim diagnostics As DiagnosticBag = Nothing
            Dim localBase = BaseTypeNoUseSiteDiagnostics
            If localBase IsNot Nothing Then
                ' Check constraints on the first declaration with explicit bases.
                Dim singleDeclaration = FirstDeclarationWithExplicitBases()
                If singleDeclaration IsNot Nothing Then
                    Dim location = singleDeclaration.NameLocation
                    diagnostics = DiagnosticBag.GetInstance()

                    localBase.CheckAllConstraints(location, diagnostics)

                    If IsGenericType Then
                        ' Check that generic type does not derive from System.Attribute. 
                        ' This check must be done here instead of in ValidateClassBase to avoid infinite recursion when there are
                        ' cycles in the inheritance chain. In Dev10/11, the error was reported on the inherited statement, now it 
                        ' is reported on the class statement.
                        Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                        Dim isBaseType As Boolean = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Attribute).IsBaseTypeOf(localBase, useSiteDiagnostics)

                        diagnostics.Add(location, useSiteDiagnostics)

                        If isBaseType Then
                            ' WARNING: in case System_Attribute was not found or has errors, the above check may 
                            '          fail to detect inheritance from System.Attribute, but we assume that in this case 
                            '          another error will be generated anyway
                            Binder.ReportDiagnostic(diagnostics, location, ERRID.ERR_GenericClassCannotInheritAttr)
                        End If
                    End If
                End If
            End If

            m_containingModule.AtomicSetFlagAndStoreDiagnostics(m_lazyState,
                                                                StateFlags.ReportedBaseClassConstraintsDiagnostics,
                                                                0,
                                                                diagnostics,
                                                                CompilationStage.Declare)

            If diagnostics IsNot Nothing Then
                diagnostics.Free()
            End If
        End Sub

        ''' <summary>
        ''' Do additional verification of interfaces after acyclic interfaces are found. This is
        ''' the chance to generate diagnostics that may need to walk interfaces and as such
        ''' can be performed only after the interfaces have been determined and cycles broken.
        ''' (For instance, checking constraints on Class C(Of T) Implements I(Of C(Of T)).)
        ''' </summary>
        Private Sub CheckInterfacesConstraints()
            If (m_lazyState And StateFlags.ReportedInterfacesConstraintsDiagnostics) <> 0 Then
                Return
            End If

            Dim diagnostics As DiagnosticBag = Nothing
            Dim localInterfaces = InterfacesNoUseSiteDiagnostics
            If Not localInterfaces.IsEmpty Then
                ' Check constraints on the first declaration with explicit interfaces.
                Dim singleDeclaration = FirstDeclarationWithExplicitInterfaces()
                If singleDeclaration IsNot Nothing Then
                    Dim location = singleDeclaration.NameLocation
                    diagnostics = DiagnosticBag.GetInstance()
                    For Each [interface] In localInterfaces
                        [interface].CheckAllConstraints(location, diagnostics)
                    Next
                End If
            End If

            If m_containingModule.AtomicSetFlagAndStoreDiagnostics(m_lazyState,
                                                                   StateFlags.ReportedInterfacesConstraintsDiagnostics,
                                                                   0,
                                                                   diagnostics,
                                                                   CompilationStage.Declare) Then
                DeclaringCompilation.SymbolDeclaredEvent(Me)
            End If

            If diagnostics IsNot Nothing Then
                diagnostics.Free()
            End If
        End Sub

        ''' <summary>
        ''' Return the first Class declaration with explicit base classes to use for
        ''' checking base class constraints. Other type declarations (Structures,
        ''' Modules, Interfaces) are ignored since other errors will have been
        ''' reported if those types include bases.
        ''' </summary>
        Private Function FirstDeclarationWithExplicitBases() As SingleTypeDeclaration
            For Each decl In TypeDeclaration.Declarations
                Dim syntaxNode = decl.SyntaxReference.GetVisualBasicSyntax()
                Select Case syntaxNode.Kind
                    Case SyntaxKind.ClassBlock
                        If DirectCast(syntaxNode, TypeBlockSyntax).Inherits.Count > 0 Then
                            Return decl
                        End If
                End Select
            Next
            Return Nothing
        End Function

        ''' <summary>
        ''' Return the first Class, Structure, or Interface declaration with explicit interfaces
        ''' to use for checking interface constraints. Other type declarations (Modules) are
        ''' ignored since other errors will have been reported if those types include interfaces.
        ''' </summary>
        Private Function FirstDeclarationWithExplicitInterfaces() As SingleTypeDeclaration
            For Each decl In TypeDeclaration.Declarations
                Dim syntaxNode = decl.SyntaxReference.GetVisualBasicSyntax()
                Select Case syntaxNode.Kind
                    Case SyntaxKind.ClassBlock, SyntaxKind.StructureBlock
                        If DirectCast(syntaxNode, TypeBlockSyntax).Implements.Count > 0 Then
                            Return decl
                        End If
                    Case SyntaxKind.InterfaceBlock
                        If DirectCast(syntaxNode, TypeBlockSyntax).Inherits.Count > 0 Then
                            Return decl
                        End If
                End Select
            Next
            Return Nothing
        End Function

#End Region

#Region "Enums"

        ''' <summary>
        ''' For enum types, gets the underlying type. Returns null on all other
        ''' kinds of types.
        ''' </summary>
        Public Overrides ReadOnly Property EnumUnderlyingType As NamedTypeSymbol
            Get
                If Not Me.IsEnumType Then
                    Return Nothing
                End If

                Dim underlyingType = Me._lazyEnumUnderlyingType

                If underlyingType Is Nothing Then
                    Dim tempDiags = DiagnosticBag.GetInstance
                    Dim blockRef = SyntaxReferences(0)
                    Dim tree = blockRef.SyntaxTree
                    Dim syntax = DirectCast(blockRef.GetSyntax, EnumBlockSyntax)
                    Dim binder As Binder = BinderBuilder.CreateBinderForType(ContainingSourceModule, tree, Me)
                    underlyingType = BindEnumUnderlyingType(syntax, binder, tempDiags)

                    If Interlocked.CompareExchange(Me._lazyEnumUnderlyingType, underlyingType, Nothing) Is Nothing Then
                        ContainingSourceModule.AddDiagnostics(tempDiags, CompilationStage.Declare)
                    Else
                        Debug.Assert(underlyingType = Me._lazyEnumUnderlyingType)
                        underlyingType = Me._lazyEnumUnderlyingType
                    End If

                    tempDiags.Free()
                End If

                Debug.Assert(underlyingType IsNot Nothing)
                Return underlyingType
            End Get
        End Property

        Private Function BindEnumUnderlyingType(syntax As EnumBlockSyntax,
                                   bodyBinder As Binder,
                                   diagnostics As DiagnosticBag) As NamedTypeSymbol

            Dim underlyingType = syntax.EnumStatement.UnderlyingType

            If underlyingType IsNot Nothing AndAlso Not underlyingType.Type.IsMissing Then
                Dim type = bodyBinder.BindTypeSyntax(underlyingType.Type, diagnostics)
                If type.IsValidEnumUnderlyingType Then
                    Return DirectCast(type, NamedTypeSymbol)
                Else
                    Binder.ReportDiagnostic(diagnostics, underlyingType.Type, ERRID.ERR_InvalidEnumBase)
                End If
            End If

            Return bodyBinder.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32, syntax.EnumStatement.Identifier, diagnostics)
        End Function
#End Region

#Region "Attributes"
        Public ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Type
            End Get
        End Property

        Private Function GetAttributeDeclarations() As ImmutableArray(Of SyntaxList(Of AttributeListSyntax))
            Dim result = TypeDeclaration.GetAttributeDeclarations()

            Debug.Assert(result.Length = 0 OrElse (Not Me.IsScriptClass AndAlso Not Me.IsImplicitClass))  ' Should be handled by above test.

            Return result
        End Function

        Private Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            If m_lazyCustomAttributesBag Is Nothing OrElse Not m_lazyCustomAttributesBag.IsSealed Then
                LoadAndValidateAttributes(OneOrMany.Create(GetAttributeDeclarations()), m_lazyCustomAttributesBag)
            End If

            Debug.Assert(m_lazyCustomAttributesBag.IsSealed)
            Return m_lazyCustomAttributesBag
        End Function

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        Public NotOverridable Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetAttributesBag().Attributes
        End Function

        Private Function GetDecodedWellKnownAttributeData() As CommonTypeWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.m_lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonTypeWellKnownAttributeData)
        End Function

        Friend Overrides ReadOnly Property HasEmbeddedAttribute As Boolean
            Get
                Dim data As TypeEarlyWellKnownAttributeData = GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasEmbeddedAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
            Get
                If _lazyIsExtensibleInterface = ThreeState.Unknown Then
                    _lazyIsExtensibleInterface = DecodeIsExtensibleInterface().ToThreeState()
                End If

                Return _lazyIsExtensibleInterface.Value
            End Get
        End Property

        Private Function DecodeIsExtensibleInterface() As Boolean
            If Me.IsInterfaceType() Then
                Dim data As TypeEarlyWellKnownAttributeData = GetEarlyDecodedWellKnownAttributeData()
                If data IsNot Nothing AndAlso data.HasAttributeForExtensibleInterface Then
                    Return True
                End If

                For Each [interface] In Me.AllInterfacesNoUseSiteDiagnostics
                    If [interface].IsExtensibleInterfaceNoUseSiteDiagnostics Then
                        Return True
                    End If
                Next
            End If

            Return False
        End Function

        ''' <summary>
        ''' Returns data decoded from early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        ''' </summary>
        ''' <remarks>
        ''' Forces binding and decoding of attributes.
        ''' </remarks>
        Private Function GetEarlyDecodedWellKnownAttributeData() As TypeEarlyWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.m_lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.EarlyDecodedWellKnownAttributeData, TypeEarlyWellKnownAttributeData)
        End Function

        Friend Overrides ReadOnly Property IsComImport As Boolean
            Get
                Dim data As TypeEarlyWellKnownAttributeData = GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasComImportAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                If _lazyCoClassType Is ErrorTypeSymbol.UnknownResultType Then
                    If Not Me.IsInterface Then
                        Interlocked.CompareExchange(_lazyCoClassType, Nothing, DirectCast(ErrorTypeSymbol.UnknownResultType, TypeSymbol))
                    Else
                        Dim dummy As CommonTypeWellKnownAttributeData = GetDecodedWellKnownAttributeData()
                        If _lazyCoClassType Is ErrorTypeSymbol.UnknownResultType Then
                            ' if this is still ErrorTypeSymbol.UnknownResultType, interface 
                            ' does not have the attribute applied
                            Interlocked.CompareExchange(_lazyCoClassType, Nothing,
                                                        DirectCast(ErrorTypeSymbol.UnknownResultType, TypeSymbol))
                        End If
                    End If
                End If

                Debug.Assert(_lazyCoClassType IsNot ErrorTypeSymbol.UnknownResultType)
                Debug.Assert(Me.IsInterface OrElse _lazyCoClassType Is Nothing)
                Return _lazyCoClassType
            End Get
        End Property

        Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Dim typeData As CommonTypeWellKnownAttributeData = Me.GetDecodedWellKnownAttributeData()
                Return typeData IsNot Nothing AndAlso typeData.HasWindowsRuntimeImportAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return False
            End Get
        End Property

        Friend ReadOnly Property HasSecurityCriticalAttributes As Boolean
            Get
                Dim typeData As CommonTypeWellKnownAttributeData = Me.GetDecodedWellKnownAttributeData()
                Return typeData IsNot Nothing AndAlso typeData.HasSecurityCriticalAttributes
            End Get
        End Property



        ''' <summary>
        ''' Is System.Runtime.InteropServices.GuidAttribute applied to this type in code.
        ''' </summary>
        Friend Function HasGuidAttribute() As Boolean
            ' So far this information is used only by ComClass feature, therefore, I do not believe
            ' it is worth to intercept this attribute in DecodeWellKnownAttribute and cache the fact of attribute's
            ' presence and the guid value. If we start caching that information, implementation of this function 
            ' should change to take advantage of the cache.
            Return GetAttributes().IndexOfAttribute(Me, AttributeDescription.GuidAttribute) > -1
        End Function

        ''' <summary>
        ''' Is System.Runtime.InteropServices.ClassInterfaceAttribute applied to this type in code.
        ''' </summary>
        Friend Function HasClassInterfaceAttribute() As Boolean
            ' So far this information is used only by ComClass feature, therefore, I do not believe
            ' it is worth to intercept this attribute in DecodeWellKnownAttribute and cache the fact of attribute's
            ' presence and its data. If we start caching that information, implementation of this function 
            ' should change to take advantage of the cache.
            Return GetAttributes().IndexOfAttribute(Me, AttributeDescription.ClassInterfaceAttribute) > -1
        End Function

        ''' <summary>
        ''' Is System.Runtime.InteropServices.ComSourceInterfacesAttribute applied to this type in code.
        ''' </summary>
        Friend Function HasComSourceInterfacesAttribute() As Boolean
            ' So far this information is used only by ComClass feature, therefore, I do not believe
            ' it is worth to intercept this attribute in DecodeWellKnownAttribute and cache the fact of attribute's
            ' presence and the its data. If we start caching that information, implementation of this function 
            ' should change to take advantage of the cache.
            Return GetAttributes().IndexOfAttribute(Me, AttributeDescription.ComSourceInterfacesAttribute) > -1
        End Function

        Friend Overrides Function EarlyDecodeWellKnownAttribute(ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            Debug.Assert(arguments.AttributeType IsNot Nothing)
            Debug.Assert(Not arguments.AttributeType.IsErrorType())
            Dim hasAnyDiagnostics As Boolean = False

            If VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.VisualBasicEmbeddedAttribute) Then
                ' Handle Microsoft.VisualBasic.Embedded attribute
                Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                If Not attrdata.HasErrors Then
                    arguments.GetOrCreateData(Of TypeEarlyWellKnownAttributeData)().HasEmbeddedAttribute = True
                    Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                Else
                    Return Nothing
                End If

            ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ComImportAttribute) Then
                ' Handle ComImportAttribute
                Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                If Not attrdata.HasErrors Then
                    arguments.GetOrCreateData(Of TypeEarlyWellKnownAttributeData)().HasComImportAttribute = True
                    Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                Else
                    Return Nothing
                End If
            ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ConditionalAttribute) Then
                ' Handle ConditionalAttribute
                Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                If Not attrdata.HasErrors Then
                    Dim conditionalSymbol As String = attrdata.GetConstructorArgument(Of String)(0, SpecialType.System_String)
                    arguments.GetOrCreateData(Of TypeEarlyWellKnownAttributeData)().AddConditionalSymbol(conditionalSymbol)
                    Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                Else
                    Return Nothing
                End If
            End If

            Dim boundAttribute As VisualBasicAttributeData = Nothing
            Dim obsoleteData As ObsoleteAttributeData = Nothing

            If EarlyDecodeDeprecatedOrObsoleteAttribute(arguments, boundAttribute, obsoleteData) Then
                If obsoleteData IsNot Nothing Then
                    arguments.GetOrCreateData(Of TypeEarlyWellKnownAttributeData)().ObsoleteAttributeData = obsoleteData
                End If

                Return boundAttribute
            End If

            If VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.AttributeUsageAttribute) Then
                ' Avoid decoding duplicate AttributeUsageAttribute.
                If Not arguments.HasDecodedData OrElse DirectCast(arguments.DecodedData, TypeEarlyWellKnownAttributeData).AttributeUsageInfo.IsNull Then
                    ' Handle AttributeUsageAttribute: If this type is an attribute type then decode the AttributeUsageAttribute, otherwise ignore it.
                    Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                    If Not attrdata.HasErrors Then
                        arguments.GetOrCreateData(Of TypeEarlyWellKnownAttributeData)().AttributeUsageInfo = attrdata.DecodeAttributeUsageAttribute()
                        Debug.Assert(Not DirectCast(arguments.DecodedData, TypeEarlyWellKnownAttributeData).AttributeUsageInfo.IsNull)
                        ' NOTE: Native VB compiler does not validate the AttributeTargets argument to AttributeUsageAttribute, we do the same.
                        Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                    End If
                End If
                Return Nothing
            End If

            If Me.IsInterfaceType() Then
                If VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.InterfaceTypeAttribute) Then
                    Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                    If Not attrdata.HasErrors Then
                        Dim interfaceType As ComInterfaceType = Nothing
                        If attrdata.DecodeInterfaceTypeAttribute(interfaceType) AndAlso
                            (interfaceType And ComInterfaceType.InterfaceIsIDispatch) <> 0 Then

                            arguments.GetOrCreateData(Of TypeEarlyWellKnownAttributeData).HasAttributeForExtensibleInterface = True
                        End If

                        Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                    Else
                        Return Nothing
                    End If

                ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.TypeLibTypeAttribute) Then
                    Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                    If Not attrdata.HasErrors Then
                        Dim flags As Cci.TypeLibTypeFlags = attrdata.DecodeTypeLibTypeAttribute()
                        If (flags And Cci.TypeLibTypeFlags.FNonExtensible) = 0 Then
                            arguments.GetOrCreateData(Of TypeEarlyWellKnownAttributeData).HasAttributeForExtensibleInterface = True
                        End If

                        Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                    Else
                        Return Nothing
                    End If
                End If
            End If

            Return MyBase.EarlyDecodeWellKnownAttribute(arguments)
        End Function

        Friend NotOverridable Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Dim data As CommonTypeEarlyWellKnownAttributeData = Me.GetEarlyDecodedWellKnownAttributeData()
            Return If(data IsNot Nothing, data.ConditionalSymbols, ImmutableArray(Of String).Empty)
        End Function

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ' If there are no attributes then this symbol is not Obsolete.
                If Not GetAttributeDeclarations().Any() Then
                    Return Nothing
                End If

                If m_lazyCustomAttributesBag Is Nothing Then
                    Return ObsoleteAttributeData.Uninitialized
                End If

                If m_lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed Then
                    Dim data = DirectCast(m_lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData, TypeEarlyWellKnownAttributeData)
                    Return If(data IsNot Nothing, data.ObsoleteAttributeData, Nothing)
                Else
                    Return ObsoleteAttributeData.Uninitialized
                End If
            End Get
        End Property

        Friend NotOverridable Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Debug.Assert(Me.IsOrDerivedFromWellKnownClass(WellKnownType.System_Attribute, DeclaringCompilation, Nothing) OrElse Me.SpecialType = Microsoft.CodeAnalysis.SpecialType.System_Object)

            Dim data As TypeEarlyWellKnownAttributeData = Me.GetEarlyDecodedWellKnownAttributeData()
            If data IsNot Nothing AndAlso Not data.AttributeUsageInfo.IsNull Then
                Return data.AttributeUsageInfo
            Else
                Dim baseType = Me.BaseTypeNoUseSiteDiagnostics
                Return If(baseType IsNot Nothing, baseType.GetAttributeUsageInfo(), AttributeUsageInfo.Default)
            End If
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Dim data As CommonTypeWellKnownAttributeData = Me.GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasDeclarativeSecurity
            End Get
        End Property

        Friend NotOverridable Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.GetAttributesBag()
            Dim wellKnownAttributeData = DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonTypeWellKnownAttributeData)
            If wellKnownAttributeData IsNot Nothing Then
                Dim securityData As SecurityWellKnownAttributeData = wellKnownAttributeData.SecurityInformation
                If securityData IsNot Nothing Then
                    Return securityData.GetSecurityAttributes(attributesBag.Attributes)
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of Microsoft.Cci.SecurityAttribute)()
        End Function

        Friend NotOverridable Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)

            Dim attrData = arguments.Attribute
            Debug.Assert(Not attrData.HasErrors)
            Debug.Assert(arguments.SymbolPart = AttributeLocation.None)

            ' If we start caching information about GuidAttribute here, implementation of HasGuidAttribute function should be changed accordingly.
            ' If we start caching information about ClassInterfaceAttribute here, implementation of HasClassInterfaceAttribute function should be changed accordingly.
            ' If we start caching information about ComSourceInterfacesAttribute here, implementation of HasComSourceInterfacesAttribute function should be changed accordingly.
            ' If we start caching information about ComVisibleAttribute here, implementation of GetComVisibleState function should be changed accordingly.

            Dim decoded As Boolean = False

            Select Case Me.TypeKind
                Case TypeKind.Class
                    If attrData.IsTargetAttribute(Me, AttributeDescription.CaseInsensitiveExtensionAttribute) Then
                        arguments.Diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionOnlyAllowedOnModuleSubOrFunction), Me.Locations(0))
                        decoded = True

                    ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.VisualBasicComClassAttribute) Then
                        If Me.IsGenericType Then
                            arguments.Diagnostics.Add(ERRID.ERR_ComClassOnGeneric, Me.Locations(0))
                        Else
                            Interlocked.CompareExchange(_comClassData, New ComClassData(attrData), Nothing)
                        End If

                        decoded = True

                    ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.DefaultEventAttribute) Then
                        If attrData.CommonConstructorArguments.Length = 1 AndAlso attrData.CommonConstructorArguments(0).Kind = TypedConstantKind.Primitive Then
                            Dim eventName = TryCast(attrData.CommonConstructorArguments(0).Value, String)

                            If eventName IsNot Nothing AndAlso eventName.Length > 0 AndAlso Not FindDefaultEvent(eventName) Then
                                arguments.Diagnostics.Add(ERRID.ERR_DefaultEventNotFound1, arguments.AttributeSyntaxOpt.GetLocation(), eventName)
                            End If
                        End If

                        decoded = True

                    End If

                Case TypeKind.Interface
                    If attrData.IsTargetAttribute(Me, AttributeDescription.CoClassAttribute) Then
                        Debug.Assert(Not attrData.CommonConstructorArguments.IsDefault AndAlso attrData.CommonConstructorArguments.Length = 1)
                        Dim argument As TypedConstant = attrData.CommonConstructorArguments(0)

                        Debug.Assert(argument.Kind = TypedConstantKind.Type)
                        Debug.Assert(argument.Type IsNot Nothing)
                        Debug.Assert(argument.Type.Equals(DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type)))

                        ' Note that 'argument.Value' may be Nothing in which case Roslyn will 
                        ' generate an error as if CoClassAttribute attribute was not defined on 
                        ' the interface; this behavior matches Dev11, but we should probably 
                        ' revise it later
                        Interlocked.CompareExchange(Me._lazyCoClassType,
                                                    DirectCast(argument.Value, TypeSymbol),
                                                    DirectCast(ErrorTypeSymbol.UnknownResultType, TypeSymbol))

                        decoded = True
                    End If

                Case TypeKind.Module
                    If ContainingSymbol.Kind = SymbolKind.Namespace AndAlso attrData.IsTargetAttribute(Me, AttributeDescription.CaseInsensitiveExtensionAttribute) Then
                        ' Already have an attribute, no need to add another one.
                        SuppressExtensionAttributeSynthesis()
                        decoded = True

                    ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.VisualBasicComClassAttribute) Then
                        ' Can't apply ComClassAttribute to a Module
                        arguments.Diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_InvalidAttributeUsage2, AttributeDescription.VisualBasicComClassAttribute.Name, Me.Name), Me.Locations(0))
                        decoded = True
                    End If
            End Select

            If Not decoded Then
                If attrData.IsTargetAttribute(Me, AttributeDescription.DefaultMemberAttribute) Then
                    arguments.GetOrCreateData(Of CommonTypeWellKnownAttributeData)().HasDefaultMemberAttribute = True

                    ' Check that the explicit <DefaultMember(...)> argument matches the default property if any.
                    Dim attributeValue = attrData.DecodeDefaultMemberAttribute()
                    Dim defaultProperty = DefaultPropertyName
                    If Not String.IsNullOrEmpty(defaultProperty) AndAlso
                        Not IdentifierComparison.Equals(defaultProperty, attributeValue) Then
                        arguments.Diagnostics.Add(ERRID.ERR_ConflictDefaultPropertyAttribute, Locations(0), Me)
                    End If

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SerializableAttribute) Then
                    arguments.GetOrCreateData(Of CommonTypeWellKnownAttributeData)().HasSerializableAttribute = True

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SpecialNameAttribute) Then
                    arguments.GetOrCreateData(Of CommonTypeWellKnownAttributeData)().HasSpecialNameAttribute = True

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.StructLayoutAttribute) Then
                    Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)

                    Dim defaultAutoLayoutSize = If(Me.TypeKind = TypeKind.Structure, 1, 0)
                    AttributeData.DecodeStructLayoutAttribute(Of CommonTypeWellKnownAttributeData, AttributeSyntax, VisualBasicAttributeData, AttributeLocation)(
                        arguments, Me.DefaultMarshallingCharSet, defaultAutoLayoutSize, MessageProvider.Instance)

                    If Me.IsGenericType Then
                        arguments.Diagnostics.Add(ERRID.ERR_StructLayoutAttributeNotAllowed, arguments.AttributeSyntaxOpt.GetLocation(), Me)
                    End If

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SuppressUnmanagedCodeSecurityAttribute) Then
                    arguments.GetOrCreateData(Of CommonTypeWellKnownAttributeData)().HasSuppressUnmanagedCodeSecurityAttribute = True

                ElseIf attrData.IsSecurityAttribute(Me.DeclaringCompilation) Then
                    attrData.DecodeSecurityAttribute(Of CommonTypeWellKnownAttributeData)(Me, Me.DeclaringCompilation, arguments)

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.ClassInterfaceAttribute) Then
                    attrData.DecodeClassInterfaceAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics)

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.InterfaceTypeAttribute) Then
                    attrData.DecodeInterfaceTypeAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics)

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.GuidAttribute) Then
                    attrData.DecodeGuidAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics)

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.WindowsRuntimeImportAttribute) Then
                    arguments.GetOrCreateData(Of CommonTypeWellKnownAttributeData)().HasWindowsRuntimeImportAttribute = True

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SecurityCriticalAttribute) OrElse
                       attrData.IsTargetAttribute(Me, AttributeDescription.SecuritySafeCriticalAttribute) Then
                    arguments.GetOrCreateData(Of CommonTypeWellKnownAttributeData)().HasSecurityCriticalAttributes = True

                ElseIf _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.Unknown AndAlso
                    attrData.IsTargetAttribute(Me, AttributeDescription.TypeIdentifierAttribute) Then
                    _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.True

                ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.RequiredAttributeAttribute) Then
                    Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)
                    arguments.Diagnostics.Add(ERRID.ERR_CantUseRequiredAttribute, arguments.AttributeSyntaxOpt.GetLocation(), Me)
                End If
            End If

            MyBase.DecodeWellKnownAttribute(arguments)
        End Sub

        Friend Overrides ReadOnly Property IsExplicitDefinitionOfNoPiaLocalType As Boolean
            Get
                If _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.Unknown Then
                    GetAttributes()
                    If _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.Unknown Then
                        _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.False
                    End If
                End If

                Debug.Assert(_lazyIsExplicitDefinitionOfNoPiaLocalType <> ThreeState.Unknown)
                Return _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.True
            End Get
        End Property

        Private Function FindDefaultEvent(eventName As String) As Boolean
            Dim current As NamedTypeSymbol = Me

            Do
                For Each member As Symbol In current.GetMembers(eventName)
                    If member.Kind = SymbolKind.Event AndAlso
                       (member.DeclaredAccessibility = Accessibility.Public OrElse
                        member.DeclaredAccessibility = Accessibility.Friend) Then
                        ' We have a match so the default event is valid.
                        Return True
                    End If
                Next

                current = current.BaseTypeNoUseSiteDiagnostics
            Loop While current IsNot Nothing

            Return False
        End Function

        Friend Overrides Sub PostDecodeWellKnownAttributes(
            boundAttributes As ImmutableArray(Of VisualBasicAttributeData),
            allAttributeSyntaxNodes As ImmutableArray(Of AttributeSyntax),
            diagnostics As DiagnosticBag,
            symbolPart As AttributeLocation,
            decodedData As WellKnownAttributeData)

            Debug.Assert(Not boundAttributes.IsDefault)
            Debug.Assert(Not allAttributeSyntaxNodes.IsDefault)
            Debug.Assert(boundAttributes.Length = allAttributeSyntaxNodes.Length)
            Debug.Assert(symbolPart = AttributeLocation.None)

            ValidateStandardModuleAttribute(diagnostics)

            MyBase.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData)
        End Sub

        Private Sub ValidateStandardModuleAttribute(diagnostics As DiagnosticBag)
            ' If this type is a VB Module, touch the ctor for MS.VB.Globals.StandardModuleAttribute to
            ' produce any diagnostics related to that member and type.

            ' Dev10 reported a special diagnostic ERR_NoStdModuleAttribute if the constructor was missing.
            ' Roslyn now used the more general use site errors, which also reports diagnostics if the type or the constructor
            ' is missing.

            If Me.TypeKind = TypeKind.Module Then
                Dim useSiteError As DiagnosticInfo = Nothing

                Binder.ReportUseSiteErrorForSynthesizedAttribute(WellKnownMember.Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute__ctor,
                                                                 Me.DeclaringCompilation,
                                                                 Locations(0),
                                                                 diagnostics)
            End If
        End Sub

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasSpecialNameAttribute
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasSerializableAttribute
            End Get
        End Property

        Private Function HasInstanceFields() As Boolean
            Dim members = Me.GetMembersUnordered()
            For i = 0 To members.Length - 1
                Dim m = members(i)
                If Not m.IsShared And m.Kind = SymbolKind.Field Then
                    Return True
                End If
            Next

            Return False
        End Function

        Friend NotOverridable Overrides ReadOnly Property Layout As TypeLayout
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                If data IsNot Nothing AndAlso data.HasStructLayoutAttribute Then
                    Return data.Layout
                End If

                If Me.TypeKind = TypeKind.Structure Then
                    ' CLI spec 22.37.16:
                    ' "A ValueType shall have a non-zero size - either by defining at least one field, or by providing a non-zero ClassSize"
                    ' 
                    ' Dev11 compiler sets the value to 1 for structs with no fields and no size specified.
                    ' It does not change the size value if it was explicitly specified to be 0, nor does it report an error.
                    Return New TypeLayout(LayoutKind.Sequential, If(Me.HasInstanceFields(), 0, 1), alignment:=0)
                End If

                Return Nothing
            End Get
        End Property

        Friend ReadOnly Property HasStructLayoutAttribute As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasStructLayoutAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return If((data IsNot Nothing AndAlso data.HasStructLayoutAttribute), data.MarshallingCharSet, DefaultMarshallingCharSet)
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(compilationState as ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            Dim compilation = Me.DeclaringCompilation

            If Not String.IsNullOrEmpty(DefaultPropertyName) AndAlso Not HasDefaultMemberAttribute() Then
                Dim stringType = GetSpecialType(SpecialType.System_String)
                ' NOTE: used from emit, so shouldn't have gotten here if there were errors
                Debug.Assert(stringType.GetUseSiteErrorInfo() Is Nothing)

                AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor,
                    ImmutableArray.Create(
                        New TypedConstant(stringType, TypedConstantKind.Primitive, DefaultPropertyName))))
            End If

            If Me.TypeKind = TypeKind.Module Then
                'TODO check that there's not a user supplied instance already. This attribute is AllowMultiple:=False.

                AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute__ctor))
            End If

            If _comClassData IsNot Nothing Then
                If _comClassData.ClassId IsNot Nothing Then
                    AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_InteropServices_GuidAttribute__ctor,
                        ImmutableArray.Create(
                            New TypedConstant(GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, _comClassData.ClassId))))
                End If

                AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_InteropServices_ClassInterfaceAttribute__ctorClassInterfaceType,
                    ImmutableArray.Create(
                        New TypedConstant(GetSpecialType(SpecialType.System_Int32), TypedConstantKind.Enum, CInt(ClassInterfaceType.None)))))

                Dim eventInterface As NamedTypeSymbol = _comClassData.GetSynthesizedEventInterface()

                If eventInterface IsNot Nothing Then
                    Dim eventInterfaceName As String = eventInterface.Name
                    Dim container1 As NamedTypeSymbol = Me
                    Dim container2 As NamedTypeSymbol = container1.ContainingType

                    While container2 IsNot Nothing
                        eventInterfaceName = container1.Name & "+" & eventInterfaceName

                        container1 = container2
                        container2 = container1.ContainingType
                    End While

                    eventInterfaceName = container1.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat) & "+" & eventInterfaceName

                    AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_InteropServices_ComSourceInterfacesAttribute__ctorString,
                        ImmutableArray.Create(
                            New TypedConstant(GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, eventInterfaceName))))
                End If
            End If
        End Sub

        Private Function HasDefaultMemberAttribute() As Boolean
            Dim attributesBag = GetAttributesBag()
            Dim wellKnownAttributeData = DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonTypeWellKnownAttributeData)
            Return (wellKnownAttributeData IsNot Nothing) AndAlso wellKnownAttributeData.HasDefaultMemberAttribute
        End Function

#End Region

        Friend Function GetOrAddWithEventsOverride(baseProperty As PropertySymbol) As SynthesizedOverridingWithEventsProperty
            Dim overridesDict = Me._lazyWithEventsOverrides
            If overridesDict Is Nothing Then
                Interlocked.CompareExchange(Me._lazyWithEventsOverrides,
                                            New ConcurrentDictionary(Of PropertySymbol, SynthesizedOverridingWithEventsProperty),
                                            Nothing)

                overridesDict = Me._lazyWithEventsOverrides
            End If

            Dim result As SynthesizedOverridingWithEventsProperty = Nothing
            If overridesDict.TryGetValue(baseProperty, result) Then
                Return result
            Else
                ' we need to create a lambda here since we need to close over baseProperty
                ' we will however create a lambda only on a cache miss, hopefully not very often.
                Return overridesDict.GetOrAdd(baseProperty, Function() New SynthesizedOverridingWithEventsProperty(baseProperty, Me))
            End If
        End Function

        Protected Overrides Sub AddEntryPointIfNeeded(membersBuilder As MembersAndInitializersBuilder)
            If Me.TypeKind = TypeKind.Class AndAlso Not Me.IsGenericType Then
                Dim mainTypeName As String = DeclaringCompilation.Options.MainTypeName

                If mainTypeName IsNot Nothing AndAlso
                   IdentifierComparison.EndsWith(mainTypeName, Me.Name) AndAlso
                   IdentifierComparison.Equals(mainTypeName, Me.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat)) Then

                    ' Must derive from Windows.Forms.Form
                    Dim formClass As NamedTypeSymbol = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Windows_Forms_Form)

                    If formClass.IsErrorType() OrElse Not Me.IsOrDerivedFrom(formClass, useSiteDiagnostics:=Nothing) Then
                        Return
                    End If

                    Dim entryPointMethodName As String = WellKnownMemberNames.EntryPointMethodName

                    ' If we already have a child named 'Main', do not add a synthetic one.
                    If membersBuilder.Members.ContainsKey(entryPointMethodName) Then
                        Return
                    End If

                    If GetTypeMembersDictionary().ContainsKey(entryPointMethodName) Then
                        Return
                    End If

                    ' We need to have a constructor that can be called without arguments.
                    Dim symbols As ArrayBuilder(Of Symbol) = Nothing
                    Dim haveSuitableConstructor As Boolean = False

                    If membersBuilder.Members.TryGetValue(WellKnownMemberNames.InstanceConstructorName, symbols) Then
                        For Each method As MethodSymbol In symbols
                            If method.MethodKind = MethodKind.Constructor AndAlso method.ParameterCount = 0 Then
                                haveSuitableConstructor = True
                                Exit For
                            End If
                        Next

                        If Not haveSuitableConstructor Then
                            ' Do the second pass to check for optional parameters, etc., it will require binding parameter modifiers and probably types.
                            For Each method As MethodSymbol In symbols
                                If method.MethodKind = MethodKind.Constructor AndAlso method.CanBeCalledWithNoParameters() Then
                                    haveSuitableConstructor = True
                                    Exit For
                                End If
                            Next
                        End If
                    End If

                    If haveSuitableConstructor Then
                        Dim syntaxRef = SyntaxReferences.First() ' use arbitrary part

                        Dim binder As Binder = BinderBuilder.CreateBinderForType(ContainingSourceModule, syntaxRef.SyntaxTree, Me)
                        Dim entryPoint As New SynthesizedMainTypeEntryPoint(syntaxRef.GetVisualBasicSyntax(), Me)
                        AddMember(entryPoint, binder, membersBuilder, omitDiagnostics:=True)
                    End If
                End If
            End If
        End Sub

    End Class
End Namespace

