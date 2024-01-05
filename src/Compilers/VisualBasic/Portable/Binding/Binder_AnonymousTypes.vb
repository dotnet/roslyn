' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' This portion of the binder converts an ExpressionSyntax into a BoundExpression

    Partial Friend Class Binder

        Private Function BindAnonymousObjectCreationExpression(node As AnonymousObjectCreationExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Return AnonymousTypeCreationBinder.BindAnonymousObjectInitializer(Me, node, node.Initializer, node.NewKeyword, diagnostics)
        End Function

        Private Function BindAnonymousObjectCreationExpression(node As VisualBasicSyntaxNode,
                                                               typeDescr As AnonymousTypeDescriptor,
                                                               initExpressions As ImmutableArray(Of BoundExpression),
                                                               diagnostics As BindingDiagnosticBag) As BoundExpression
            '  Check for restricted types.
            For Each field As AnonymousTypeField In typeDescr.Fields
                Dim restrictedType As TypeSymbol = Nothing
                If field.Type.IsRestrictedTypeOrArrayType(restrictedType) Then
                    ReportDiagnostic(diagnostics, field.Location, ERRID.ERR_RestrictedType1, restrictedType)
                End If
            Next

            Return CreateAnonymousObjectCreationExpression(node, typeDescr, initExpressions)
        End Function

        Private Function CreateAnonymousObjectCreationExpression(node As VisualBasicSyntaxNode,
                                                               typeDescr As AnonymousTypeDescriptor,
                                                               initExpressions As ImmutableArray(Of BoundExpression),
                                                               Optional hasErrors As Boolean = False) As BoundExpression
            '  Get or create an anonymous type
            Dim anonymousType As AnonymousTypeManager.AnonymousTypePublicSymbol =
                Me.Compilation.AnonymousTypeManager.ConstructAnonymousTypeSymbol(typeDescr)

            ' get constructor
            Dim constructor As MethodSymbol = anonymousType.InstanceConstructors.First()
            Debug.Assert(constructor IsNot Nothing)
            Debug.Assert(constructor.ParameterCount = initExpressions.Length)

            Return CreateAnonymousObjectCreationExpression(node, anonymousType, initExpressions, hasErrors)
        End Function

        Protected Overridable Function CreateAnonymousObjectCreationExpression(node As VisualBasicSyntaxNode,
                                                                               anonymousType As AnonymousTypeManager.AnonymousTypePublicSymbol,
                                                                               initExpressions As ImmutableArray(Of BoundExpression),
                                                                               Optional hasErrors As Boolean = False) As BoundAnonymousTypeCreationExpression

            ' By default BoundAnonymousTypeCreationExpression is created without 
            ' locals and bound nodes for 'declarations' of properties
            Return New BoundAnonymousTypeCreationExpression(node, Nothing,
                                                            ImmutableArray(Of BoundAnonymousTypePropertyAccess).Empty,
                                                            initExpressions, anonymousType, hasErrors)
        End Function

        ''' <summary>
        ''' Binder to be used for binding New With { ... } expressions. 
        ''' </summary>
        Friend Class AnonymousTypeCreationBinder
            Inherits Binder

            ' NOTE: field descriptors in 'Fields' array are mutable, field types are being assigned 
            '       in successfully processed fields as we go through the field initializers
            Private ReadOnly _fields() As AnonymousTypeField

            ' NOTE: after the binder is initialized, some elements in 'Fields' may remain empty because 
            '       errors in field declaration; '_fieldName2index' map actually stores the list of 
            '       'good' fields into corresponding slots in 'Fields', those slots must not be empty
            Private ReadOnly _fieldName2index As Dictionary(Of String, Integer)

            ' field declaration bound node is created for fields with implicitly 
            ' specified name to provide semantic info on those identifier;
            ' the array builder is being created lazily if needed
            Private _fieldDeclarations As ArrayBuilder(Of BoundAnonymousTypePropertyAccess)

            ' '_locals' field points to an array which holds locals introduced during binding
            ' for each field which is being used in other field initialization;
            ' Thus while binding 'New With { .a = 1, .b = 1 + .a }' a local will be created to 
            ' hold the value of '1' and to be used as '.a = <local_a>' and '.b = 1 + <local_a>'
            ' Note that no local is created for not referenced fields (like '.b' in the example 
            ' above) leaving correspondent slots in '_locals' empty.
            Private ReadOnly _locals() As LocalSymbol

            Private ReadOnly _propertySymbols() As PropertySymbol

            ''' <summary>
            ''' If set, the state of the binder shouldn't be modified by subsequent binding operations,
            ''' which could be performed by SemanticModel in context of this binder.
            ''' </summary>
            Private _freeze As Boolean

            Friend Shared Function BindAnonymousObjectInitializer(containingBinder As Binder,
                                                                  owningSyntax As VisualBasicSyntaxNode,
                                                                  initializerSyntax As ObjectMemberInitializerSyntax,
                                                                  typeLocationToken As SyntaxToken,
                                                                  diagnostics As BindingDiagnosticBag) As BoundExpression

                Dim fieldsCount = initializerSyntax.Initializers.Count

                If fieldsCount = 0 Then
                    ' ERR_AnonymousTypeNeedField must have been reported in Parser
                    Return BadExpression(owningSyntax, ImmutableArray(Of BoundExpression).Empty, ErrorTypeSymbol.UnknownResultType)
                End If

                Return New AnonymousTypeCreationBinder(containingBinder, initializerSyntax, diagnostics).
                            BindInitializersAndCreateBoundNode(owningSyntax, initializerSyntax, diagnostics, typeLocationToken)
            End Function

#Region "Binder Creation and Initial Analysis"

            Private Sub New(containingBinder As Binder,
                            initializerSyntax As ObjectMemberInitializerSyntax,
                            diagnostics As BindingDiagnosticBag)
                MyBase.New(containingBinder)

                Dim objectType As TypeSymbol = GetSpecialType(SpecialType.System_Object, initializerSyntax, diagnostics)

                ' Examine 'initializerSyntax' node and builds the list of anonymous type field declarations
                ' with no type assigned to fields yet (those will be assigned when the binder binds field 
                ' initializers one-by-one). Some diagnostics may be reported as we go...
                Dim initializers = initializerSyntax.Initializers
                Dim initializersCount As Integer = initializers.Count
                Debug.Assert(initializersCount > 0)

                ' Initialize binder fields
                Me._fieldName2index = New Dictionary(Of String, Integer)(initializersCount, CaseInsensitiveComparison.Comparer)
                Me._fields = New AnonymousTypeField(initializersCount - 1) {}
                Me._fieldDeclarations = Nothing
                Me._locals = New LocalSymbol(initializersCount - 1) {}
                Me._propertySymbols = New PropertySymbol(initializersCount - 1) {}

                '  Process field initializers
                For fieldIndex = 0 To initializersCount - 1
                    Dim fieldSyntax As FieldInitializerSyntax = initializers(fieldIndex)

                    Dim fieldName As String = Nothing
                    Dim fieldNode As VisualBasicSyntaxNode = Nothing
                    Dim fieldIsKey As Boolean = False

                    ' get field's name 
                    If fieldSyntax.Kind = SyntaxKind.InferredFieldInitializer Then
                        Dim inferredFieldInitializer = DirectCast(fieldSyntax, InferredFieldInitializerSyntax)

                        Dim fieldNameToken As SyntaxToken = inferredFieldInitializer.Expression.ExtractAnonymousTypeMemberName(Nothing)

                        If fieldNameToken.Kind = SyntaxKind.None Then
                            ' failed to infer field name, create a dummy field descriptor
                            ' NOTE: errors are supposed to be reported by parser
                            fieldName = Nothing
                            fieldNode = inferredFieldInitializer.Expression
                            fieldIsKey = False

                        Else
                            ' field name successfully inferred
                            fieldName = fieldNameToken.ValueText
                            fieldNode = DirectCast(fieldNameToken.Parent, VisualBasicSyntaxNode)
                            fieldIsKey = inferredFieldInitializer.KeyKeyword.Kind = SyntaxKind.KeyKeyword
                        End If

                    Else
                        ' field name is specified implicitly
                        Dim namedFieldInitializer = DirectCast(fieldSyntax, NamedFieldInitializerSyntax)
                        fieldNode = namedFieldInitializer.Name
                        fieldIsKey = namedFieldInitializer.KeyKeyword.Kind = SyntaxKind.KeyKeyword
                        fieldName = namedFieldInitializer.Name.Identifier.ValueText
                    End If

                    '  check type character
                    Dim typeChar As TypeCharacter = ExtractTypeCharacter(fieldNode)
                    If typeChar <> TypeCharacter.None Then
                        ' report the error and proceed to the next field initializer
                        ReportDiagnostic(diagnostics, fieldSyntax, ERRID.ERR_AnonymousTypeDisallowsTypeChar)
                    End If

                    If String.IsNullOrEmpty(fieldName) Then
                        ' since the field does not have name, we generate a pseudo name to be used in template
                        fieldName = "$"c & fieldIndex.ToString()

                    Else
                        ' check the name for duplications (in System.Object and in the list of fields)
                        If objectType.GetMembers(fieldName).Any() OrElse Me._fieldName2index.ContainsKey(fieldName) Then
                            ' report the error 
                            ReportDiagnostic(diagnostics, fieldSyntax, ErrorFactory.ErrorInfo(ERRID.ERR_DuplicateAnonTypeMemberName1, fieldName))
                        End If
                    End If

                    ' build anonymous type field descriptor
                    Me._fields(fieldIndex) = New AnonymousTypeField(fieldName, fieldNode.GetLocation(), fieldIsKey)
                    Me._fieldName2index(fieldName) = fieldIndex ' This might overwrite fields in error-cases
                Next
            End Sub

#End Region

#Region "Binding of anonymous type creation field initializers"

            Private Function BindInitializersAndCreateBoundNode(owningSyntax As VisualBasicSyntaxNode,
                                                                initializerSyntax As ObjectMemberInitializerSyntax,
                                                                diagnostics As BindingDiagnosticBag,
                                                                typeLocationToken As SyntaxToken) As BoundExpression
                Dim fieldsCount As Integer = Me._fields.Length

                ' Try to bind expressions from field initializers one-by-one; after each of the 
                ' expression is bound successfully assign the type of the field in 'fields'.
                Dim boundInitializers(fieldsCount - 1) As BoundExpression

                ' WARNING: Note that SemanticModel.GetDeclaredSymbol for field initializer node relies on 
                '          the fact that the order of properties in anonymous type template corresponds 
                '          1-to-1 to the appropriate filed initializer syntax nodes; This means such 
                '          correspondence must be preserved all the time including erroneous scenarios

                ' NOTE: if one field initializer references another, the binder creates an 
                '       BoundAnonymousTypePropertyAccess node to represent the value of the field, 
                '       if the field referenced is not processed yet an error will be generated
                For index = 0 To fieldsCount - 1
                    Dim initializer As FieldInitializerSyntax = initializerSyntax.Initializers(index)

                    ' to be used if we need to create BoundAnonymousTypePropertyAccess node
                    Dim namedFieldInitializer As NamedFieldInitializerSyntax = Nothing

                    Dim initExpression As ExpressionSyntax = Nothing
                    If initializer.Kind = SyntaxKind.InferredFieldInitializer Then
                        initExpression = DirectCast(initializer, InferredFieldInitializerSyntax).Expression
                    Else
                        namedFieldInitializer = DirectCast(initializer, NamedFieldInitializerSyntax)
                        initExpression = namedFieldInitializer.Expression
                    End If

                    Dim initializerBinder As New AnonymousTypeFieldInitializerBinder(Me, index)

                    Dim boundExpression As BoundExpression = initializerBinder.BindRValue(initExpression, diagnostics)
                    boundExpression = New BoundAnonymousTypeFieldInitializer(initializer, initializerBinder, boundExpression, boundExpression.Type).MakeCompilerGenerated()

                    boundInitializers(index) = boundExpression

                    Dim fieldType As TypeSymbol = boundExpression.Type

                    '  check for restricted type
                    Dim restrictedType As TypeSymbol = Nothing
                    If fieldType.IsRestrictedTypeOrArrayType(restrictedType) Then
                        ReportDiagnostic(diagnostics, initExpression, ERRID.ERR_RestrictedType1, restrictedType)
                    End If

                    ' always assign the type, event if there were errors in binding and/or 
                    ' the type is an error type, we are going to use it for anonymous type fields
                    Me._fields(index).AssignFieldType(fieldType)

                    If namedFieldInitializer IsNot Nothing Then
                        ' create an instance of BoundAnonymousTypePropertyAccess to 
                        ' guarantee semantic info on the identifier

                        If Me._fieldDeclarations Is Nothing Then
                            Me._fieldDeclarations = ArrayBuilder(Of BoundAnonymousTypePropertyAccess).GetInstance()
                        End If

                        Me._fieldDeclarations.Add(
                                        New BoundAnonymousTypePropertyAccess(
                                            namedFieldInitializer.Name,
                                            Me, index, fieldType))
                    End If

                    ' TODO: when Dev10 reports ERR_BadOrCircularInitializerReference (BC36555) ??
                Next

                ' just return a new bound anonymous type creation node
                Dim result As BoundExpression = Me.CreateAnonymousObjectCreationExpression(owningSyntax,
                                                                New AnonymousTypeDescriptor(
                                                                    Me._fields.AsImmutableOrNull(),
                                                                    typeLocationToken.GetLocation(),
                                                                    False),
                                                                boundInitializers.AsImmutableOrNull())

                Me._freeze = True

                Return result
            End Function

            Protected Overrides Function CreateAnonymousObjectCreationExpression(node As VisualBasicSyntaxNode,
                                                                                 anonymousType As AnonymousTypeManager.AnonymousTypePublicSymbol,
                                                                                 initExpressions As ImmutableArray(Of BoundExpression),
                                                                                 Optional hasErrors As Boolean = False) As BoundAnonymousTypeCreationExpression
                ' cache anonymous type property symbols created
                For index = 0 To Me._fields.Length - 1

                    Dim name As String = Me._fields(index).Name

                    ' NOTE: we use the following criteria as an indicator of the fact that 
                    '       the name of the field is not correct, so we don't want to return 
                    '       symbols of such fields to semantic API
                    If name(0) <> "$"c Then
                        Me._propertySymbols(index) = anonymousType.Properties(index)
                    End If
                Next

                ' create a node
                Return New BoundAnonymousTypeCreationExpression(node, Me,
                                                                If(Me._fieldDeclarations Is Nothing,
                                                                   ImmutableArray(Of BoundAnonymousTypePropertyAccess).Empty,
                                                                   Me._fieldDeclarations.ToImmutableAndFree()),
                                                                initExpressions, anonymousType, hasErrors)
            End Function

#End Region

#Region "Accessors for anonymous type creation bound nodes "

            Friend Function GetAnonymousTypePropertySymbol(index As Integer) As PropertySymbol
                Return Me._propertySymbols(index)
            End Function

            Friend Function GetAnonymousTypePropertyLocal(index As Integer) As LocalSymbol
                Return Me._locals(index)
            End Function

            Friend Function TryGetField(name As String, <Out()> ByRef field As AnonymousTypeField, <Out()> ByRef fieldIndex As Integer) As Boolean
                If Me._fieldName2index.TryGetValue(name, fieldIndex) Then
                    field = Me._fields(fieldIndex)
                    Return True
                End If

                field = Nothing
                Return False
            End Function

            Friend Sub RegisterFieldReference(fieldIndex As Integer)
                Debug.Assert(Me._fields(fieldIndex).Type IsNot Nothing)

                If Not _freeze Then
                    ' check if there is already a local symbol created for this field

                    Dim local = Me._locals(fieldIndex)
                    If local Is Nothing Then
                        ' create a local
                        local = New SynthesizedLocal(Me.ContainingMember, Me._fields(fieldIndex).Type, SynthesizedLocalKind.LoweringTemp)
                        Me._locals(fieldIndex) = local
                    End If
                End If
            End Sub
#End Region

        End Class

        ''' <summary>
        ''' Having this binder, which is created for each field initializer within AnonymousObjectCreationExpressionSyntax
        ''' gives us the following advantages:
        '''   - We no longer rely on transient state of AnonymousTypeField objects to detect out of order field references
        '''     within initializers. This way we can be sure that result of binding performed by SemanticModel is consistent
        '''     with result of initial binding of the entire node.
        '''   - AnonymousTypeCreationBinder overrides CreateAnonymousObjectCreationExpression in such a way that it mutates
        '''     its state. That overridden method shouldn't be called while we are binding each initializer (by queries, for example), 
        '''     it should be called only by AnonymousTypeCreationBinder itself after all initializers are bound and we are producing 
        '''     the resulting node. So having an extra binder in between takes care of that.
        ''' </summary>
        Friend Class AnonymousTypeFieldInitializerBinder
            Inherits Binder

            Private ReadOnly _initializerOrdinal As Integer

            Public Sub New(creationBinder As AnonymousTypeCreationBinder, initializerOrdinal As Integer)
                MyBase.New(creationBinder)

                _initializerOrdinal = initializerOrdinal
            End Sub

#Region "Binding of member access with omitted left like '.fieldName'"

            Protected Friend Overrides Function TryBindOmittedLeftForMemberAccess(node As MemberAccessExpressionSyntax,
                                                                                  diagnostics As BindingDiagnosticBag,
                                                                                  accessingBinder As Binder,
                                                                                  <Out> ByRef wholeMemberAccessExpressionBound As Boolean) As BoundExpression
                wholeMemberAccessExpressionBound = True

                Dim creationBinder = DirectCast(ContainingBinder, AnonymousTypeCreationBinder)

                ' filter out parser errors
                If node.ContainsDiagnostics Then
                    Return BadExpression(node, ErrorTypeSymbol.UnknownResultType)
                End If

                Dim nameSyntax As SimpleNameSyntax = node.Name
                Dim name As String = nameSyntax.Identifier.ValueText

                ' 'nameSyntax' points to '.<nameSyntax>' which is supposed to be either 
                ' a property name or a name inherited from System.Object
                Dim fieldIndex As Integer = 0
                Dim field As AnonymousTypeField = Nothing
                If creationBinder.TryGetField(name, field, fieldIndex) Then
                    ' Field is found

                    Dim hasErrors As Boolean = False
                    ' check for type arguments
                    If nameSyntax.Kind = SyntaxKind.GenericName Then
                        ' referencing a field, but with type arguments specified
                        ' NOTE: since we don't have the symbol of the anonymous type's 
                        '       property, we mock property name to be used in this message
                        ' TODO: revise
                        ReportDiagnostic(diagnostics, DirectCast(nameSyntax, GenericNameSyntax).TypeArgumentList,
                                         ERRID.ERR_TypeOrMemberNotGeneric1,
                                         String.Format(
                                             "Public {0}Property {1} As T{2}",
                                             If(field.IsKey, "Readonly ", ""),
                                             name, fieldIndex))
                        hasErrors = True
                    End If

                    ' check if the field referenced is already processed, and is 'good', e.g. has type assigned
                    If fieldIndex >= _initializerOrdinal Then

                        ' referencing a field which is not processed yet or has an error
                        ' report an error and return a bad expression
                        If Not hasErrors Then
                            ' don't report this error if other diagnostics are already reported
                            ReportDiagnostic(diagnostics, node, ERRID.ERR_AnonymousTypePropertyOutOfOrder1, name)
                        End If
                        Return BadExpression(node, ErrorTypeSymbol.UnknownResultType)

                    Else
                        creationBinder.RegisterFieldReference(fieldIndex)

                        If Me.ContainingMember IsNot accessingBinder.ContainingMember Then
                            ReportDiagnostic(diagnostics, node, ERRID.ERR_CannotLiftAnonymousType1, node.Name.Identifier.ValueText)
                            hasErrors = True
                        End If

                        ' return bound anonymous type access
                        Debug.Assert(field.Type IsNot Nothing)
                        Return New BoundAnonymousTypePropertyAccess(node, creationBinder, fieldIndex, field.Type, hasErrors)
                    End If
                Else
                    ' NOTE: Dev10 allows references to methods defined of anonymous type, which boils 
                    '       down to those defined on System.Object AND extension methods defined 
                    '       for System.Object:
                    '
                    '       - In case an instance method of System.Object is being called, the result of 
                    '         Dev10 compilation with throw an exception in runtime
                    '       - In case a shared method of System.Object is being called, like 
                    '         New With {.a = .ReferenceEquals(Nothing, Nothing)}, the call finishes fine
                    '       - The result of calling extension methods depends on method's implementation 
                    '         (Nothing is being passed as the first argument)
                    '
                    '       In Roslyn we disable this functionality which is a breaking change in a sense,
                    '       but really should only affect a very few customers.

                    ' TODO: revise and maybe report a special error message
                End If

                ' NOTE: since we don't have the symbol of the anonymous type, we use 
                '       "<anonymous type>" literal to be consistent with Dev10
                ReportDiagnostic(diagnostics, node, ERRID.ERR_NameNotMemberOfAnonymousType2, name, StringConstants.AnonymousTypeName)
                Return BadExpression(node, ErrorTypeSymbol.UnknownResultType)
            End Function

            Protected Overrides Function TryBindOmittedLeftForDictionaryAccess(node As MemberAccessExpressionSyntax, accessingBinder As Binder, diagnostics As BindingDiagnosticBag) As BoundExpression
                ' NOTE: since we don't have the symbol of the anonymous type, we use 
                '       "<anonymous type>" literal to be consistent with Dev10
                ReportDiagnostic(diagnostics, node, ERRID.ERR_NoDefaultNotExtend1, StringConstants.AnonymousTypeName)
                Return BadExpression(node, ErrorTypeSymbol.UnknownResultType)
            End Function

            Protected Overrides Function TryBindOmittedLeftForConditionalAccess(node As ConditionalAccessExpressionSyntax, accessingBinder As Binder, diagnostics As BindingDiagnosticBag) As BoundExpression
                Return Nothing
            End Function
#End Region

        End Class
    End Class

End Namespace
