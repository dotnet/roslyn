' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class SourceMemberFieldSymbol
        Inherits SourceFieldSymbol

        ' The type of the field. Set to Nothing if not computed yet.
        Private _lazyType As TypeSymbol

        Private _lazyMeParameter As ParameterSymbol

        Protected Sub New(container As SourceMemberContainerTypeSymbol,
                          syntaxRef As SyntaxReference,
                          name As String,
                          memberFlags As SourceMemberFlags)

            MyBase.New(container, syntaxRef, name, memberFlags)
        End Sub

        Friend NotOverridable Overrides ReadOnly Property DeclarationSyntax As VisualBasicSyntaxNode
            Get
                Return Syntax.Parent.Parent
            End Get
        End Property

        Friend NotOverridable Overrides Function IsDefinedInSourceTree(tree As SyntaxTree, definedWithinSpan As TextSpan?, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            Return IsDefinedInSourceTree(Me.DeclarationSyntax, tree, definedWithinSpan, cancellationToken)
        End Function

        Friend NotOverridable Overrides ReadOnly Property GetAttributeDeclarations As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            Get
                Return OneOrMany.Create(DirectCast(Syntax.Parent.Parent, FieldDeclarationSyntax).AttributeLists)
            End Get
        End Property

        Friend Overrides ReadOnly Property MeParameter As ParameterSymbol
            Get
                If IsShared Then
                    Return Nothing
                Else
                    If _lazyMeParameter Is Nothing Then
                        Interlocked.CompareExchange(Of ParameterSymbol)(_lazyMeParameter, New MeParameterSymbol(Me), Nothing)
                    End If

                    Return _lazyMeParameter
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                If _lazyType Is Nothing Then
                    Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
                    Dim diagnostics = BindingDiagnosticBag.GetInstance()
                    Dim varType = ComputeType(diagnostics)
                    Debug.Assert(varType IsNot Nothing)
                    sourceModule.AtomicStoreReferenceAndDiagnostics(_lazyType, varType, diagnostics)
                    diagnostics.Free()
                End If

                Return _lazyType
            End Get
        End Property

        Private Function ComputeType(diagBag As BindingDiagnosticBag) As TypeSymbol
            Dim declaredType = GetDeclaredType(diagBag)  ' needed for diagnostic creation in all cases

            If Not HasDeclaredType Then
                Return GetInferredType(ConstantFieldsInProgress.Empty)
            Else
                Return declaredType
            End If
        End Function

        Private Function GetDeclaredType(diagBag As BindingDiagnosticBag) As TypeSymbol
            Dim modifiedIdentifier As ModifiedIdentifierSyntax = DirectCast(Syntax, ModifiedIdentifierSyntax)
            Dim declarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)

            Dim binder As Binder = BinderBuilder.CreateBinderForType(
                DirectCast(Me.ContainingModule, SourceModuleSymbol),
                Me.SyntaxTree,
                ContainingType)

            binder = New LocationSpecificBinder(BindingLocation.FieldType, Me, binder)

            ' Ignore type syntax errors for all but the first field of a given type.
            Dim varType = ComputeFieldType(modifiedIdentifier, binder, diagBag,
                                           isConst:=Me.IsConst,
                                           isWithEvents:=False,
                                           ignoreTypeSyntaxDiagnostics:=(m_memberFlags And SourceMemberFlags.FirstFieldDeclarationOfType) = 0)

            If Not varType.IsErrorType() Then
                If Me.IsConst Then
                    ' Note: The native compiler reports two errors for "Const F As C": ERR_ConstAsNonConstant
                    ' and ERR_ConstantWithNoValue. Rather than report two errors in those cases, we only
                    ' report ERR_ConstantWithNoValue if ERR_ConstAsNonConstant has not been reported.
                    ' In other words, we only report "Constants must have a value" for fields with missing
                    ' initializer if the field type can be "Const".

                    If Not varType.IsValidTypeForConstField() Then
                        ' "Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type."
                        If varType.IsArrayType Then
                            ' arrays get the squiggles under the identifier name
                            binder.ReportDiagnostic(diagBag, modifiedIdentifier.Identifier, ERRID.ERR_ConstAsNonConstant)
                        Else
                            ' other data types get the squiggles under the type part of the as clause 
                            binder.ReportDiagnostic(diagBag, declarator.AsClause.Type, ERRID.ERR_ConstAsNonConstant)
                        End If
                    ElseIf declarator.Initializer Is Nothing Then
                        ' "Constants must have a value."
                        binder.ReportDiagnostic(diagBag, modifiedIdentifier, ERRID.ERR_ConstantWithNoValue)
                    End If

                Else
                    Dim restrictedType As TypeSymbol = Nothing
                    If varType.IsRestrictedTypeOrArrayType(restrictedType) Then
                        binder.ReportDiagnostic(diagBag, declarator.AsClause.Type, ERRID.ERR_RestrictedType1, restrictedType)
                    End If
                End If

                If HasDeclaredType Then
                    Dim errorLocation = SourceSymbolHelpers.GetAsClauseLocation(modifiedIdentifier.Identifier, declarator.AsClause)
                    AccessCheck.VerifyAccessExposureForMemberType(Me, errorLocation, varType, diagBag)
                End If
            End If
            Return varType
        End Function

        ' Helper used for computing the type of a field.
        Private Shared Function ComputeFieldType(modifiedIdentifierSyntax As ModifiedIdentifierSyntax,
                                                 binder As Binder,
                                                 diagnostics As BindingDiagnosticBag,
                                                 isConst As Boolean,
                                                 isWithEvents As Boolean,
                                                 ignoreTypeSyntaxDiagnostics As Boolean) As TypeSymbol
            Dim declarator = DirectCast(modifiedIdentifierSyntax.Parent, VariableDeclaratorSyntax)

            Dim asClauseOpt = declarator.AsClause
            Dim asClauseType As TypeSymbol = Nothing
            Dim initializerSyntax As VisualBasicSyntaxNode = declarator.Initializer

            If asClauseOpt IsNot Nothing Then
                If (asClauseOpt.Kind <> SyntaxKind.AsNewClause OrElse (DirectCast(asClauseOpt, AsNewClauseSyntax).NewExpression.Kind <> SyntaxKind.AnonymousObjectCreationExpression)) Then
                    asClauseType = binder.BindTypeSyntax(asClauseOpt.Type, If(ignoreTypeSyntaxDiagnostics, BindingDiagnosticBag.Discarded, diagnostics))
                End If
                If asClauseOpt.Kind = SyntaxKind.AsNewClause Then
                    initializerSyntax = asClauseOpt
                End If
            End If

            Dim omitFurtherDiagnostics As Boolean = String.IsNullOrEmpty(modifiedIdentifierSyntax.Identifier.ValueText)

            Dim varType As TypeSymbol
            If (asClauseOpt IsNot Nothing AndAlso asClauseOpt.Kind = SyntaxKind.AsNewClause AndAlso
             (DirectCast(asClauseOpt, AsNewClauseSyntax).NewExpression.Kind = SyntaxKind.AnonymousObjectCreationExpression)) Then
                varType = ErrorTypeSymbol.UnknownResultType
            Else
                Dim getErrorInfo As Func(Of DiagnosticInfo) = Nothing

                If Not omitFurtherDiagnostics AndAlso Not (isConst AndAlso binder.OptionInfer) Then

                    If isWithEvents Then
                        'WithEvents' variables must have an 'As' clause.
                        getErrorInfo = ErrorFactory.GetErrorInfo_ERR_WithEventsRequiresClass
                    Else
                        If binder.OptionStrict = OptionStrict.On Then
                            getErrorInfo = ErrorFactory.GetErrorInfo_ERR_StrictDisallowImplicitObject
                        ElseIf binder.OptionStrict = OptionStrict.Custom Then
                            getErrorInfo = ErrorFactory.GetErrorInfo_WRN_ObjectAssumedVar1_WRN_MissingAsClauseinVarDecl
                        End If
                    End If
                End If

                varType = binder.DecodeModifiedIdentifierType(modifiedIdentifierSyntax, asClauseType, asClauseOpt, initializerSyntax,
                                                                  getErrorInfo,
                                                                  diagnostics,
                                                                  VisualBasic.Binder.ModifiedIdentifierTypeDecoderContext.FieldType)
            End If

            Debug.Assert(varType IsNot Nothing)
            Return varType
        End Function

        Friend Shared Function ComputeWithEventsFieldType(propertySymbol As PropertySymbol,
                                                          modifiedIdentifier As ModifiedIdentifierSyntax,
                                                          binder As Binder,
                                                          ignoreTypeSyntaxDiagnostics As Boolean,
                                                          diagnostics As BindingDiagnosticBag) As TypeSymbol

            Dim varType = ComputeFieldType(modifiedIdentifier, binder, diagnostics, isConst:=False, isWithEvents:=True, ignoreTypeSyntaxDiagnostics:=ignoreTypeSyntaxDiagnostics)

            If Not varType.IsErrorType() Then
                Dim declarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
                Dim identifier = modifiedIdentifier.Identifier

                ' it must be either a class or an interface or a type parameter
                ' with class constraints.
                ' and for arrays there is a special error.
                If varType.IsArrayType Then
                    ' 'WithEvents' variables cannot be typed as arrays.
                    binder.ReportDiagnostic(diagnostics, modifiedIdentifier, ERRID.ERR_EventSourceIsArray)

                ElseIf Not (varType.IsClassOrInterfaceType OrElse
                    (varType.Kind = SymbolKind.TypeParameter AndAlso varType.IsReferenceType)) Then

                    If declarator.AsClause IsNot Nothing Then
                        Dim typeSyntax = declarator.AsClause.Type
                        If typeSyntax IsNot Nothing Then
                            ' 'WithEvents' variables can only be typed as classes, interfaces or type parameters with class constraints.
                            binder.ReportDiagnostic(diagnostics, identifier, ERRID.ERR_WithEventsAsStruct)
                        End If
                    End If
                End If

                If declarator.AsClause IsNot Nothing Then
                    Dim errorLocation = SourceSymbolHelpers.GetAsClauseLocation(identifier, declarator.AsClause)
                    AccessCheck.VerifyAccessExposureForMemberType(propertySymbol, errorLocation, varType, diagnostics)
                End If
            End If

            Return varType
        End Function

        ''' <summary>
        ''' Gets the inferred type of this const field from the initialization value.
        ''' </summary>
        ''' <param name="inProgress">Used to detect dependencies between constant field values.</param><returns></returns>
        Friend Overrides Function GetInferredType(inProgress As ConstantFieldsInProgress) As TypeSymbol
            ' there are no inferred types for non const fields, simply return the type in that case
            If HasDeclaredType Then
                Return Type
            End If

            GetConstantValue(inProgress)

            ' if constantType is nothing it means that there was no initializer given and a diagnostic has already been issued.
            ' In this case we'll return System.Object which is the default type for all locals that could not infer a type.
            Dim constantType = GetInferredConstantType(inProgress)
            Debug.Assert(constantType IsNot Nothing OrElse EqualsValueOrAsNewInitOpt Is Nothing)

            If constantType Is Nothing Then
                constantType = ContainingAssembly.GetSpecialType(SpecialType.System_Object)

            Else
                ' Dev11 only allows a restricted set of types for constant fields (see IsValidTypeForConstField()).
                If Not constantType.IsValidTypeForConstField() Then
                    constantType = ContainingAssembly.GetSpecialType(SpecialType.System_Object)
                Else
                    ' Dev11 translates all inferred Enum constant types into their underlying types
                    constantType = constantType.GetEnumUnderlyingTypeOrSelf
                End If
            End If

            Return constantType
        End Function

        Protected Overridable Function GetInferredConstantType(inProgress As ConstantFieldsInProgress) As TypeSymbol
            Return Nothing
        End Function

        ''' <summary>
        ''' A source field with an explicit initializer. In a declaration declaring multiple fields,
        ''' such as "Dim a, b, c = d", this class is used for the first field only. (Other fields in
        ''' the declaration are instances of SourceFieldSymbolSiblingInitializer.)
        ''' </summary>
        Private Class SourceFieldSymbolWithInitializer
            Inherits SourceMemberFieldSymbol

            ' reference to the initialization syntax of this field,
            ' can be an EqualsValue or AsNew syntax node
            Protected ReadOnly _equalsValueOrAsNewInit As SyntaxReference

            Public Sub New(container As SourceMemberContainerTypeSymbol,
                           syntaxRef As SyntaxReference,
                           name As String,
                           memberFlags As SourceMemberFlags,
                           equalsValueOrAsNewInit As SyntaxReference)
                MyBase.New(container, syntaxRef, name, memberFlags)
                Debug.Assert(equalsValueOrAsNewInit IsNot Nothing)
                Debug.Assert(IsConst = TypeOf Me Is SourceConstFieldSymbolWithInitializer)
                _equalsValueOrAsNewInit = equalsValueOrAsNewInit
            End Sub

            Friend NotOverridable Overrides ReadOnly Property EqualsValueOrAsNewInitOpt As VisualBasicSyntaxNode
                Get
                    Return _equalsValueOrAsNewInit.GetVisualBasicSyntax()
                End Get
            End Property
        End Class

        Private NotInheritable Class SourceConstFieldSymbolWithInitializer
            Inherits SourceFieldSymbolWithInitializer

            ''' <summary>
            ''' A tuple consisting of the evaluated constant value and type
            ''' </summary>
            Private _constantTuple As EvaluatedConstant

            Public Sub New(container As SourceMemberContainerTypeSymbol,
                           syntaxRef As SyntaxReference,
                           name As String,
                           memberFlags As SourceMemberFlags,
                           equalsValueOrAsNewInit As SyntaxReference)
                MyBase.New(container, syntaxRef, name, memberFlags, equalsValueOrAsNewInit)
                Debug.Assert(IsConst)
            End Sub

            Protected Overrides Function GetLazyConstantTuple() As EvaluatedConstant
                Return _constantTuple
            End Function

            Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
                Return GetConstantValueImpl(inProgress)
            End Function

            Protected Overrides Function MakeConstantTuple(dependencies As ConstantFieldsInProgress.Dependencies, diagnostics As BindingDiagnosticBag) As EvaluatedConstant
                Return ConstantValueUtils.EvaluateFieldConstant(Me, _equalsValueOrAsNewInit, dependencies, diagnostics)
            End Function

            Protected Overrides Sub SetLazyConstantTuple(constantTuple As EvaluatedConstant, diagnostics As BindingDiagnosticBag)
                Debug.Assert(constantTuple IsNot Nothing)
                Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
                sourceModule.AtomicStoreReferenceAndDiagnostics(_constantTuple, constantTuple, diagnostics)
            End Sub

            Protected Overrides Function GetInferredConstantType(inProgress As ConstantFieldsInProgress) As TypeSymbol
                GetConstantValueImpl(inProgress)

                Dim constantTuple As EvaluatedConstant = GetLazyConstantTuple()
                If constantTuple IsNot Nothing Then
                    Return constantTuple.Type
                End If

                Debug.Assert(Not inProgress.IsEmpty)
                Return New ErrorTypeSymbol()
            End Function
        End Class

        ''' <summary>
        ''' A source field with an explicit initializer. In a declaration declaring multiple
        ''' fields, such as "Dim a, b, c = d", this class is used for the fields other than
        ''' the first. (The first field is an instance of SourceFieldSymbolWithInitializer.)
        ''' An instance of this class holds a reference to the first field in the declaration
        ''' and reuses the bound initializer from that field.
        ''' </summary>
        Private NotInheritable Class SourceFieldSymbolSiblingInitializer
            Inherits SourceMemberFieldSymbol

            ' Sibling field symbol with common initializer (used to
            ' avoid binding constant initializer multiple times).
            Private ReadOnly _sibling As SourceMemberFieldSymbol

            Public Sub New(container As SourceMemberContainerTypeSymbol,
                           syntaxRef As SyntaxReference,
                           name As String,
                           memberFlags As SourceMemberFlags,
                           sibling As SourceMemberFieldSymbol)

                MyBase.New(container, syntaxRef, name, memberFlags)
                _sibling = sibling
            End Sub

            Friend Overrides ReadOnly Property EqualsValueOrAsNewInitOpt As VisualBasicSyntaxNode
                Get
                    Return _sibling.EqualsValueOrAsNewInitOpt
                End Get
            End Property

            Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
                Return _sibling.GetConstantValue(inProgress)
            End Function

            Protected Overrides Function GetInferredConstantType(inProgress As ConstantFieldsInProgress) As TypeSymbol
                Return _sibling.GetInferredConstantType(inProgress)
            End Function
        End Class

        ' Create variable members, with the given, name, declarator, and declaration syntax.
        Friend Shared Sub Create(container As SourceMemberContainerTypeSymbol,
                                 syntax As FieldDeclarationSyntax,
                                 binder As Binder,
                                 members As SourceNamedTypeSymbol.MembersAndInitializersBuilder,
                                 ByRef staticInitializers As ArrayBuilder(Of FieldOrPropertyInitializer),
                                 ByRef instanceInitializers As ArrayBuilder(Of FieldOrPropertyInitializer),
                                 diagBag As BindingDiagnosticBag)

            Debug.Assert(diagBag.AccumulatesDiagnostics)
            ' Decode the flags.

            Dim validFlags = SourceMemberFlags.AllAccessibilityModifiers Or
                            SourceMemberFlags.Dim Or
                            SourceMemberFlags.Const Or
                            SourceMemberFlags.Shadows Or
                            SourceMemberFlags.Shared

            Dim errorId = ERRID.ERR_BadDimFlags1

            If syntax.Modifiers.Any(SyntaxKind.WithEventsKeyword) Then
                validFlags = validFlags Or SourceMemberFlags.WithEvents
                errorId = ERRID.ERR_BadWithEventsFlags1
            Else
                ' only regular fields can be readonly
                validFlags = validFlags Or SourceMemberFlags.ReadOnly
                errorId = ERRID.ERR_BadDimFlags1
            End If

            Dim modifiers = binder.DecodeModifiers(syntax.Modifiers,
                                                validFlags,
                                                errorId,
                                                If(container.IsValueType, Accessibility.Public, Accessibility.Private),
                                                diagBag.DiagnosticBag)

            If container IsNot Nothing Then
                Select Case container.DeclarationKind
                    Case DeclarationKind.Structure
                        If (modifiers.FoundFlags And SourceMemberFlags.Protected) <> 0 Then
                            binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_StructCantUseVarSpecifier1, diagBag.DiagnosticBag, SyntaxKind.ProtectedKeyword)
                            modifiers = New MemberModifiers(modifiers.FoundFlags And Not SourceMemberFlags.Protected,
                                                            (modifiers.ComputedFlags And Not SourceMemberFlags.AccessibilityMask) Or SourceMemberFlags.AccessibilityPrivate)
                        End If

                        If (modifiers.FoundFlags And SourceMemberFlags.WithEvents) <> 0 Then
                            binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_StructCantUseVarSpecifier1, diagBag.DiagnosticBag, SyntaxKind.WithEventsKeyword)
                            modifiers = New MemberModifiers(modifiers.FoundFlags And Not SourceMemberFlags.WithEvents, modifiers.ComputedFlags)
                        End If

                    Case DeclarationKind.Module
                        ' Member variables in module are implicitly Shared, and cannot be explicitly Shared.
                        If (modifiers.FoundFlags And SourceMemberFlags.InvalidInModule) <> 0 Then
                            binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_ModuleCantUseVariableSpecifier1, diagBag.DiagnosticBag,
                                                       SyntaxKind.SharedKeyword,
                                                       SyntaxKind.ProtectedKeyword,
                                                       SyntaxKind.DefaultKeyword,
                                                       SyntaxKind.MustOverrideKeyword,
                                                       SyntaxKind.OverridableKeyword,
                                                       SyntaxKind.ShadowsKeyword,
                                                       SyntaxKind.OverridesKeyword,
                                                       SyntaxKind.NotOverridableKeyword)
                        End If

                        modifiers = New MemberModifiers(modifiers.FoundFlags And Not SourceMemberFlags.InvalidInModule,
                                                        modifiers.ComputedFlags Or SourceMemberFlags.Shared)

                End Select
            End If

            Const FlagsNotCombinableWithConst As SourceMemberFlags = SourceMemberFlags.WithEvents Or SourceMemberFlags.Shared Or SourceMemberFlags.ReadOnly Or SourceMemberFlags.Dim

            ' Const fields cannot be Shared or ReadOnly or WithEvents
            If (modifiers.FoundFlags And SourceMemberFlags.Const) <> 0 AndAlso
                (modifiers.FoundFlags And FlagsNotCombinableWithConst) <> 0 Then

                If (modifiers.FoundFlags And SourceMemberFlags.Shared) <> 0 Then
                    binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_BadConstFlags1, diagBag.DiagnosticBag, SyntaxKind.SharedKeyword)
                End If

                If (modifiers.FoundFlags And SourceMemberFlags.ReadOnly) <> 0 Then
                    binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_BadConstFlags1, diagBag.DiagnosticBag, SyntaxKind.ReadOnlyKeyword)
                End If

                If (modifiers.FoundFlags And SourceMemberFlags.WithEvents) <> 0 Then
                    binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_BadConstFlags1, diagBag.DiagnosticBag, SyntaxKind.WithEventsKeyword)
                End If

                If (modifiers.FoundFlags And SourceMemberFlags.Dim) <> 0 Then
                    binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_BadConstFlags1, diagBag.DiagnosticBag, SyntaxKind.DimKeyword)
                End If

                modifiers = New MemberModifiers(modifiers.FoundFlags And Not FlagsNotCombinableWithConst,
                                                modifiers.ComputedFlags)
            End If

            ' VB spec 9.5: const fields are implicitly shared
            If (modifiers.FoundFlags And SourceMemberFlags.Const) <> 0 AndAlso
                (modifiers.FoundFlags And SourceMemberFlags.Shared) = 0 Then

                modifiers = New MemberModifiers(modifiers.FoundFlags,
                                                modifiers.ComputedFlags Or SourceMemberFlags.Shared)
            End If

            Dim flags = modifiers.AllFlags

            For Each declarator As VariableDeclaratorSyntax In syntax.Declarators
                ' field declarations can only have one name (strict on & off) if they have an initialization
                If declarator.Names.Count > 1 AndAlso declarator.Initializer IsNot Nothing Then
                    binder.ReportDiagnostic(diagBag, declarator, ERRID.ERR_InitWithMultipleDeclarators)
                End If

                Dim asClauseOpt = declarator.AsClause

                Dim initializerOpt = declarator.Initializer
                Dim initializerSyntax As VisualBasicSyntaxNode = Nothing
                If asClauseOpt IsNot Nothing AndAlso asClauseOpt.Kind = SyntaxKind.AsNewClause Then
                    initializerSyntax = asClauseOpt
                Else
                    initializerSyntax = initializerOpt
                End If

                Dim initializerOptRef = If(initializerSyntax Is Nothing, Nothing, binder.GetSyntaxReference(initializerSyntax))

#If Not ALLOW_STRUCT_INST_INITIALIZERS Then
                'instance members of a structure cannot have initializations (const values are implicitly shared)
                If container.TypeKind = TypeKind.Structure AndAlso
                   (flags And SourceMemberFlags.Shared) = 0 Then
                    If initializerOpt IsNot Nothing Then
                        binder.ReportDiagnostic(diagBag,
                                                If(declarator.Names.Count > 0,
                                                   declarator.Names.Last,
                                                   DirectCast(declarator, VisualBasicSyntaxNode)),
                                                ERRID.ERR_InitializerInStruct)
                    ElseIf asClauseOpt IsNot Nothing AndAlso asClauseOpt.Kind = SyntaxKind.AsNewClause Then
                        binder.ReportDiagnostic(diagBag, DirectCast(asClauseOpt, AsNewClauseSyntax).NewExpression.NewKeyword, ERRID.ERR_SharedStructMemberCannotSpecifyNew)
                    End If
                End If
#End If

                Dim nameCount = declarator.Names.Count
                Dim fieldOrWithEventSymbols(nameCount - 1) As Symbol
                ' even if a declarator has multiple names, treat them all as legal and report issues for all of them
                ' this is what Dev10 does.

                For nameIndex = 0 To nameCount - 1
                    Dim perFieldFlags As SourceMemberFlags = flags
                    If nameIndex = 0 Then
                        perFieldFlags = perFieldFlags Or SourceMemberFlags.FirstFieldDeclarationOfType
                    End If

                    Dim modifiedIdentifier = declarator.Names(nameIndex)
                    Dim identifier = modifiedIdentifier.Identifier
                    Dim omitFurtherDiagnostics As Boolean = String.IsNullOrEmpty(modifiedIdentifier.Identifier.ValueText)

                    If (modifiers.FoundFlags And SourceMemberFlags.Const) <> 0 AndAlso
                        identifier.GetTypeCharacter() = TypeCharacter.None AndAlso
                        modifiedIdentifier.Nullable.Node Is Nothing AndAlso
                        modifiedIdentifier.ArrayBounds Is Nothing AndAlso modifiedIdentifier.ArrayRankSpecifiers.IsEmpty Then
                        ' Fields have inferred type if they have no type character and no As clause, ?, array bounds, or only "As Object" (and the Object must
                        ' be the actual token "Object", not a synonym like "System.Object")
                        Dim simpleAsClauseSyntax = TryCast(asClauseOpt, SimpleAsClauseSyntax)
                        If simpleAsClauseSyntax Is Nothing OrElse
                            (simpleAsClauseSyntax.Type.Kind = SyntaxKind.PredefinedType AndAlso DirectCast(simpleAsClauseSyntax.Type, PredefinedTypeSyntax).Keyword.Kind = SyntaxKind.ObjectKeyword) Then
                            perFieldFlags = perFieldFlags Or SourceMemberFlags.InferredFieldType
                        End If
                    End If

                    Dim modifiedIdentifierRef = binder.GetSyntaxReference(modifiedIdentifier)

                    ' regular field or WithEvents property?
                    If (modifiers.FoundFlags And SourceMemberFlags.WithEvents) = 0 Then
                        Dim fieldSymbol As SourceFieldSymbol

                        If initializerOptRef Is Nothing Then
                            fieldSymbol = New SourceMemberFieldSymbol(
                                container,
                                modifiedIdentifierRef,
                                identifier.ValueText,
                                perFieldFlags)

                        ElseIf nameIndex = 0 Then
                            If (perFieldFlags And SourceMemberFlags.Const) <> 0 Then
                                fieldSymbol = New SourceConstFieldSymbolWithInitializer(
                                    container,
                                    modifiedIdentifierRef,
                                    identifier.ValueText,
                                    perFieldFlags,
                                    initializerOptRef)
                            Else
                                fieldSymbol = New SourceFieldSymbolWithInitializer(
                                    container,
                                    modifiedIdentifierRef,
                                    identifier.ValueText,
                                    perFieldFlags,
                                    initializerOptRef)
                            End If
                        Else
                            fieldSymbol = New SourceFieldSymbolSiblingInitializer(
                                container,
                                modifiedIdentifierRef,
                                identifier.ValueText,
                                perFieldFlags,
                                DirectCast(fieldOrWithEventSymbols(0), SourceMemberFieldSymbol))
                        End If

                        fieldOrWithEventSymbols(nameIndex) = fieldSymbol

                        If syntax.AttributeLists.Count = 0 Then
                            fieldSymbol.SetCustomAttributeData(CustomAttributesBag(Of VisualBasicAttributeData).Empty)
                        End If

                        If (modifiedIdentifier.ArrayBounds IsNot Nothing) AndAlso
                                (modifiedIdentifier.ArrayBounds.Arguments.Count > 0) Then

                            If container.IsStructureType AndAlso Not fieldSymbol.IsShared Then
                                ' Arrays declared as structure members cannot be declared with an initial size.
                                binder.ReportDiagnostic(diagBag, modifiedIdentifier, ERRID.ERR_ArrayInitInStruct)

                            Else
                                If initializerOpt Is Nothing Then
                                    ' Array declaration with implicit initializer.
                                    Dim initializer = Function(precedingInitializersLength As Integer)
                                                          Return New FieldOrPropertyInitializer(fieldSymbol, modifiedIdentifierRef, precedingInitializersLength)
                                                      End Function
                                    If fieldSymbol.IsShared Then
                                        SourceNamedTypeSymbol.AddInitializer(staticInitializers, initializer, members.StaticSyntaxLength)
                                    Else
                                        SourceNamedTypeSymbol.AddInitializer(instanceInitializers, initializer, members.InstanceSyntaxLength)
                                    End If
                                Else
                                    ' Array declaration with implicit and explicit initializers.
                                    binder.ReportDiagnostic(diagBag, modifiedIdentifier, ERRID.ERR_InitWithExplicitArraySizes)
                                End If
                            End If

                        End If

                        container.AddMember(fieldSymbol, binder, members, omitFurtherDiagnostics)
                    Else
                        Dim propertySymbol = SourcePropertySymbol.CreateWithEvents(container,
                                                                                   binder,
                                                                                   identifier,
                                                                                   modifiedIdentifierRef,
                                                                                   modifiers,
                                                                                   nameIndex = 0,
                                                                                   diagBag)

                        fieldOrWithEventSymbols(nameIndex) = propertySymbol

                        container.AddMember(propertySymbol, binder, members, omitFurtherDiagnostics)
                        container.AddMember(propertySymbol.GetMethod, binder, members, omitDiagnostics:=False)
                        container.AddMember(propertySymbol.SetMethod, binder, members, omitDiagnostics:=False)
                        container.AddMember(propertySymbol.AssociatedField, binder, members, omitDiagnostics:=False)
                    End If
                Next

                If initializerOptRef IsNot Nothing Then
                    Dim initializer = Function(precedingInitializersLength As Integer)
                                          Return New FieldOrPropertyInitializer(fieldOrWithEventSymbols.AsImmutableOrNull, initializerOptRef, precedingInitializersLength)
                                      End Function

                    ' all symbols are the same regarding the sharedness
                    Dim symbolsAreShared = nameCount > 0 AndAlso fieldOrWithEventSymbols(0).IsShared

                    If symbolsAreShared Then
                        ' const fields are implicitly shared and get into this list.
                        SourceNamedTypeSymbol.AddInitializer(staticInitializers, initializer, members.StaticSyntaxLength)
                    Else
                        SourceNamedTypeSymbol.AddInitializer(instanceInitializers, initializer, members.InstanceSyntaxLength)
                    End If
                End If
            Next
        End Sub
    End Class
End Namespace
