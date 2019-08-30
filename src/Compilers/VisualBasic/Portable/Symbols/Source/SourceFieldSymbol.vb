' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend MustInherit Class SourceFieldSymbol
        Inherits FieldSymbol
        Implements IAttributeTargetSymbol

        ' Flags associated with the field
        Protected ReadOnly m_memberFlags As SourceMemberFlags

        Private ReadOnly _containingType As SourceMemberContainerTypeSymbol
        Private ReadOnly _name As String

        ' The syntax reference for this field (points to the name of the field)
        Private ReadOnly _syntaxRef As SyntaxReference

        Private _lazyDocComment As String
        Private _lazyExpandedDocComment As String
        Private _lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        ' Set to 1 when the compilation event has been produced
        Private _eventProduced As Integer

        Protected Sub New(container As SourceMemberContainerTypeSymbol,
                          syntaxRef As SyntaxReference,
                          name As String,
                          memberFlags As SourceMemberFlags)

            Debug.Assert(container IsNot Nothing)
            Debug.Assert(syntaxRef IsNot Nothing)
            Debug.Assert(name IsNot Nothing)

            _name = name
            _containingType = container

            _syntaxRef = syntaxRef
            m_memberFlags = memberFlags
        End Sub

        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            MyBase.GenerateDeclarationErrors(cancellationToken)

            Dim unusedType = Me.Type
            GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty)

            ' We want declaration events to be last, after all compilation analysis is done, so we produce them here
            Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
            If Interlocked.CompareExchange(_eventProduced, 1, 0) = 0 AndAlso Not Me.IsImplicitlyDeclared Then
                sourceModule.DeclaringCompilation.SymbolDeclaredEvent(Me)
            End If
        End Sub

        ''' <summary>
        ''' Gets the syntax tree.
        ''' </summary>
        Friend ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _syntaxRef.SyntaxTree
            End Get
        End Property

        Friend ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return _syntaxRef.GetVisualBasicSyntax()
            End Get
        End Property

        Friend MustOverride ReadOnly Property DeclarationSyntax As VisualBasicSyntaxNode

        ''' <summary> 
        ''' Field initializer's declaration syntax node. 
        ''' It can be a EqualsValueSyntax or AsNewClauseSyntax.
        ''' </summary>
        Friend Overridable ReadOnly Property EqualsValueOrAsNewInitOpt As VisualBasicSyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public ReadOnly Property ContainingSourceType As SourceMemberContainerTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            If expandIncludes Then
                Return GetAndCacheDocumentationComment(Me, preferredCulture, expandIncludes, _lazyExpandedDocComment, cancellationToken)
            Else
                Return GetAndCacheDocumentationComment(Me, preferredCulture, expandIncludes, _lazyDocComment, cancellationToken)
            End If
        End Function

        ''' <summary>
        ''' Gets a value indicating whether this instance has declared type. This means not an inferred type.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance has declared type; otherwise, <c>false</c>.
        ''' </value>
        Friend Overrides ReadOnly Property HasDeclaredType As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.InferredFieldType) = 0
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return CType((m_memberFlags And SourceMemberFlags.AccessibilityMask), Accessibility)
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.ReadOnly) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.Const) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Gets the constant value.
        ''' </summary>
        ''' <param name="inProgress">The previously visited const fields; used to detect cycles.</param>
        Friend Overrides Function GetConstantValue(inProgress As SymbolsInProgress(Of FieldSymbol)) As ConstantValue
            Return Nothing
        End Function

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.Shared) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _containingType.AreMembersImplicitlyDeclared
            End Get
        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.Shadows) <> 0
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            ' WARNING: this should not allocate memory!
            Return New LexicalSortKey(_syntaxRef, Me.DeclaringCompilation)
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(Of Location)(GetSymbolLocation(_syntaxRef))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(_syntaxRef)
            End Get
        End Property

        Friend MustOverride ReadOnly Property GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))

        Public ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Field
            End Get
        End Property

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        ''' <remarks>
        ''' NOTE: This method should always be kept as a NotOverridable method.
        ''' If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        ''' </remarks>
        Public NotOverridable Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetAttributesBag().Attributes
        End Function

        Private Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            If _lazyCustomAttributesBag Is Nothing OrElse Not _lazyCustomAttributesBag.IsSealed Then
                LoadAndValidateAttributes(GetAttributeDeclarations(), _lazyCustomAttributesBag)
            End If
            Return _lazyCustomAttributesBag
        End Function

        Private Function GetDecodedWellKnownAttributeData() As CommonFieldWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonFieldWellKnownAttributeData)
        End Function

        ' This should be called at most once after the attributes are bound.  Attributes must be bound after the class
        ' and members are fully declared to avoid infinite recursion.
        Friend Sub SetCustomAttributeData(attributeData As CustomAttributesBag(Of VisualBasicAttributeData))
            Debug.Assert(attributeData IsNot Nothing)
            Debug.Assert(_lazyCustomAttributesBag Is Nothing)

            _lazyCustomAttributesBag = attributeData
        End Sub

        Friend Overrides Sub AddSynthesizedAttributes(compilationState as ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            If Me.IsConst Then
                If Me.GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty) IsNot Nothing Then
                    Dim data = GetDecodedWellKnownAttributeData()
                    If data Is Nothing OrElse data.ConstValue = CodeAnalysis.ConstantValue.Unset Then
                        If Me.Type.SpecialType = SpecialType.System_DateTime Then
                            Dim attributeValue = DirectCast(Me.ConstantValue, DateTime)

                            Dim specialTypeInt64 = Me.ContainingAssembly.GetSpecialType(SpecialType.System_Int64)
                            ' NOTE: used from emit, so shouldn't have gotten here if there were errors
                            Debug.Assert(specialTypeInt64.GetUseSiteErrorInfo() Is Nothing)

                            Dim compilation = Me.DeclaringCompilation

                            AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                            WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor,
                            ImmutableArray.Create(
                                New TypedConstant(specialTypeInt64, TypedConstantKind.Primitive, attributeValue.Ticks))))

                        ElseIf Me.Type.SpecialType = SpecialType.System_Decimal Then
                            Dim attributeValue = DirectCast(Me.ConstantValue, Decimal)

                            Dim compilation = Me.DeclaringCompilation
                            AddSynthesizedAttribute(attributes, compilation.SynthesizeDecimalConstantAttribute(attributeValue))
                        End If
                    End If
                End If
            End If

            If Me.Type.ContainsTupleNames() Then
                AddSynthesizedAttribute(attributes, DeclaringCompilation.SynthesizeTupleNamesAttribute(Type))
            End If
        End Sub

        Friend NotOverridable Overrides Function EarlyDecodeWellKnownAttribute(ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            Debug.Assert(arguments.AttributeType IsNot Nothing)
            Debug.Assert(Not arguments.AttributeType.IsErrorType())

            Dim BoundAttribute As VisualBasicAttributeData = Nothing
            Dim obsoleteData As ObsoleteAttributeData = Nothing

            If EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(arguments, BoundAttribute, obsoleteData) Then
                If obsoleteData IsNot Nothing Then
                    arguments.GetOrCreateData(Of CommonFieldEarlyWellKnownAttributeData)().ObsoleteAttributeData = obsoleteData
                End If

                Return BoundAttribute
            End If

            Return MyBase.EarlyDecodeWellKnownAttribute(arguments)
        End Function

        Friend NotOverridable Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)

            Dim attrData = arguments.Attribute
            Debug.Assert(arguments.SymbolPart = AttributeLocation.None)

            If attrData.IsTargetAttribute(Me, AttributeDescription.TupleElementNamesAttribute) Then
                arguments.Diagnostics.Add(ERRID.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location)
            End If

            If attrData.IsTargetAttribute(Me, AttributeDescription.SpecialNameAttribute) Then
                arguments.GetOrCreateData(Of CommonFieldWellKnownAttributeData)().HasSpecialNameAttribute = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.NonSerializedAttribute) Then

                If Me.ContainingType.IsSerializable Then
                    arguments.GetOrCreateData(Of CommonFieldWellKnownAttributeData)().HasNonSerializedAttribute = True
                Else
                    arguments.Diagnostics.Add(ERRID.ERR_InvalidNonSerializedUsage, arguments.AttributeSyntaxOpt.GetLocation())
                End If

            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.FieldOffsetAttribute) Then
                Dim offset = attrData.CommonConstructorArguments(0).DecodeValue(Of Integer)(SpecialType.System_Int32)
                If offset < 0 Then
                    arguments.Diagnostics.Add(ERRID.ERR_BadAttribute1, arguments.AttributeSyntaxOpt.ArgumentList.Arguments(0).GetLocation(), attrData.AttributeClass)
                    offset = 0
                End If

                arguments.GetOrCreateData(Of CommonFieldWellKnownAttributeData)().SetFieldOffset(offset)

            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.MarshalAsAttribute) Then
                MarshalAsAttributeDecoder(Of CommonFieldWellKnownAttributeData, AttributeSyntax, VisualBasicAttributeData, AttributeLocation).Decode(arguments, AttributeTargets.Field, MessageProvider.Instance)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.DateTimeConstantAttribute) Then
                VerifyConstantValueMatches(attrData.DecodeDateTimeConstantValue(), arguments)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.DecimalConstantAttribute) Then
                VerifyConstantValueMatches(attrData.DecodeDecimalConstantValue(), arguments)
            Else
                MyBase.DecodeWellKnownAttribute(arguments)
            End If
        End Sub

        ''' <summary>
        ''' Verify the constant value matches the default value from any earlier attribute
        ''' (DateTimeConstantAttribute or DecimalConstantAttribute).
        ''' If not, report ERR_FieldHasMultipleDistinctConstantValues.
        ''' </summary>
        Private Sub VerifyConstantValueMatches(attrValue As ConstantValue, ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Dim data = arguments.GetOrCreateData(Of CommonFieldWellKnownAttributeData)()
            Dim constValue As ConstantValue

            If Me.IsConst Then
                If Me.Type.IsDecimalType() OrElse Me.Type.IsDateTimeType() Then
                    constValue = Me.GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty)

                    If constValue IsNot Nothing AndAlso Not constValue.IsBad AndAlso constValue <> attrValue Then
                        arguments.Diagnostics.Add(ERRID.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.GetLocation())
                    End If
                Else
                    arguments.Diagnostics.Add(ERRID.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.GetLocation())
                End If

                If data.ConstValue = CodeAnalysis.ConstantValue.Unset Then
                    data.ConstValue = attrValue
                End If
            Else
                constValue = data.ConstValue

                If constValue <> CodeAnalysis.ConstantValue.Unset Then
                    If constValue <> attrValue Then
                        arguments.Diagnostics.Add(ERRID.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.GetLocation())
                    End If
                Else
                    data.ConstValue = attrValue
                End If
            End If
        End Sub

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                If HasRuntimeSpecialName Then
                    Return True
                End If

                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasSpecialNameAttribute
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return Name = WellKnownMemberNames.EnumBackingFieldName
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasNonSerializedAttribute
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return If(data IsNot Nothing, data.MarshallingInformation, Nothing)
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return If(data IsNot Nothing, data.Offset, Nothing)
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ' If there are no attributes then this symbol is not Obsolete.
                If (Not Me._containingType.AnyMemberHasAttributes) Then
                    Return Nothing
                End If

                Dim lazyCustomAttributesBag = Me._lazyCustomAttributesBag
                If (lazyCustomAttributesBag IsNot Nothing AndAlso lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed) Then
                    Dim data = DirectCast(_lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData, CommonFieldEarlyWellKnownAttributeData)
                    Return If(data IsNot Nothing, data.ObsoleteAttributeData, Nothing)
                End If

                Return ObsoleteAttributeData.Uninitialized
            End Get
        End Property

        ' Given a syntax ref, get the symbol location to return. We return the location of the name
        ' of the method.
        Private Shared Function GetSymbolLocation(syntaxRef As SyntaxReference) As Location
            Dim syntaxNode = syntaxRef.GetSyntax()
            Dim syntaxTree = syntaxRef.SyntaxTree

            Return syntaxTree.GetLocation(GetFieldLocationFromSyntax(DirectCast(syntaxNode, ModifiedIdentifierSyntax).Identifier))
        End Function

        ' Get the location of a field given the syntax for its modified identifier. We use the span of the base part
        ' of the identifier.
        Private Shared Function GetFieldLocationFromSyntax(node As SyntaxToken) As TextSpan
            Return node.Span
        End Function

        ' Given the syntax declaration, and a container, get the field or WithEvents property symbol declared from that syntax.
        ' This is done by lookup up the name from the declaration in the container, handling duplicates and
        ' so forth correctly.
        Friend Shared Function FindFieldOrWithEventsSymbolFromSyntax(variableName As SyntaxToken,
                                                    tree As SyntaxTree,
                                                    container As NamedTypeSymbol) As Symbol
            Dim fieldName As String = variableName.ValueText
            Dim nameSpan As TextSpan = GetFieldLocationFromSyntax(variableName)
            Return container.FindFieldOrProperty(fieldName, nameSpan, tree)
        End Function
    End Class
End Namespace
