' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Binder
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a parameter symbol defined in source.
    ''' </summary>
    Friend Class SourceComplexParameterSymbol
        Inherits SourceParameterSymbol

        Private ReadOnly _syntaxRef As SyntaxReference
        Private ReadOnly _flags As SourceParameterFlags

        ' m_lazyDefaultValue is not readonly because it is lazily computed
        Private _lazyDefaultValue As ConstantValue
        Private _lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

#If DEBUG Then
        Friend Sub AssertAttributesNotValidatedYet()
            Debug.Assert(_lazyCustomAttributesBag Is Nothing)
        End Sub
#End If
        ''' <summary>
        ''' Symbol to copy bound attributes from, or null if the attributes are not shared among multiple source method symbols.
        ''' </summary>
        ''' <remarks>
        ''' Used for partial method parameters: 
        ''' Implementation parameter always copies its attributes from the corresponding definition parameter.
        ''' Definition is always complex parameter and so it stores the attribute bag.
        ''' </remarks>
        Private ReadOnly Property BoundAttributesSource As SourceParameterSymbol
            Get
                Dim sourceMethod = TryCast(Me.ContainingSymbol, SourceMemberMethodSymbol)
                If sourceMethod Is Nothing Then
                    Return Nothing
                End If

                Dim impl = sourceMethod.SourcePartialDefinition
                If impl Is Nothing Then
                    Return Nothing
                End If

                Return DirectCast(impl.Parameters(Me.Ordinal), SourceParameterSymbol)
            End Get
        End Property

        Friend Overrides ReadOnly Property AttributeDeclarationList As SyntaxList(Of AttributeListSyntax)
            Get
                Return If(Me._syntaxRef Is Nothing, Nothing,
                          DirectCast(Me._syntaxRef.GetSyntax, ParameterSyntax).AttributeLists())
            End Get
        End Property

        Private Function GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            Dim attributes = AttributeDeclarationList

            Dim sourceMethod = TryCast(Me.ContainingSymbol, SourceMemberMethodSymbol)
            If sourceMethod Is Nothing Then
                Return OneOrMany.Create(attributes)
            End If

            Dim otherAttributes As SyntaxList(Of AttributeListSyntax)

            ' combine attributes with the corresponding parameter of a partial implementation:
            Dim otherPart As SourceMemberMethodSymbol = sourceMethod.SourcePartialImplementation
            If otherPart IsNot Nothing Then
                otherAttributes = DirectCast(otherPart.Parameters(Me.Ordinal), SourceParameterSymbol).AttributeDeclarationList
            Else
                otherAttributes = Nothing
            End If

            If attributes.Equals(Nothing) Then
                Return OneOrMany.Create(otherAttributes)
            ElseIf otherAttributes.Equals(Nothing) Then
                Return OneOrMany.Create(attributes)
            Else
                Return OneOrMany.Create(ImmutableArray.Create(attributes, otherAttributes))
            End If
        End Function

        Friend Overrides Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            If _lazyCustomAttributesBag Is Nothing OrElse Not _lazyCustomAttributesBag.IsSealed Then

                Dim copyFrom As SourceParameterSymbol = Me.BoundAttributesSource

                ' prevent infinite recursion:
                Debug.Assert(copyFrom IsNot Me)

                If copyFrom IsNot Nothing Then
                    Dim attributesBag = copyFrom.GetAttributesBag()
                    Interlocked.CompareExchange(_lazyCustomAttributesBag, attributesBag, Nothing)
                Else
                    Dim attributeSyntax = Me.GetAttributeDeclarations()
                    LoadAndValidateAttributes(attributeSyntax, _lazyCustomAttributesBag)
                End If
            End If

            Return _lazyCustomAttributesBag
        End Function

        Friend Overrides Function GetEarlyDecodedWellKnownAttributeData() As ParameterEarlyWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.EarlyDecodedWellKnownAttributeData, ParameterEarlyWellKnownAttributeData)
        End Function

        Friend Overrides Function GetDecodedWellKnownAttributeData() As CommonParameterWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonParameterWellKnownAttributeData)
        End Function

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return ExplicitDefaultConstantValue(SymbolsInProgress(Of ParameterSymbol).Empty) IsNot Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                If _lazyDefaultValue Is ConstantValue.Unset Then
                    Dim diagnostics = BindingDiagnosticBag.GetInstance()
                    If Interlocked.CompareExchange(_lazyDefaultValue, BindDefaultValue(inProgress, diagnostics), ConstantValue.Unset) Is ConstantValue.Unset Then
                        DirectCast(ContainingModule, SourceModuleSymbol).AddDeclarationDiagnostics(diagnostics)
                    End If
                    diagnostics.Free()
                End If

                Return _lazyDefaultValue
            End Get
        End Property

        Private Function BindDefaultValue(inProgress As SymbolsInProgress(Of ParameterSymbol), diagnostics As BindingDiagnosticBag) As ConstantValue

            Dim parameterSyntax = SyntaxNode
            If parameterSyntax Is Nothing Then
                Return Nothing
            End If

            Dim defaultSyntax = parameterSyntax.[Default]
            If defaultSyntax Is Nothing Then
                Return Nothing
            End If

            Dim binder As Binder = BinderBuilder.CreateBinderForParameterDefaultValue(DirectCast(ContainingModule, SourceModuleSymbol),
                                                                                      _syntaxRef.SyntaxTree,
                                                                                      Me,
                                                                                      parameterSyntax)

            ' Before binding the default value, check if we are already in the process of binding it. If so report
            ' an error and return nothing.
            If inProgress.Contains(Me) Then
                Binder.ReportDiagnostic(diagnostics, defaultSyntax.Value, ERRID.ERR_CircularEvaluation1, Me)
                Return Nothing
            End If

            Dim inProgressBinder = New DefaultParametersInProgressBinder(inProgress.Add(Me), binder)

            Dim constValue As ConstantValue = Nothing
            inProgressBinder.BindParameterDefaultValue(Me.Type, defaultSyntax, diagnostics, constValue:=constValue)

            If constValue IsNot Nothing Then
                VerifyParamDefaultValueMatchesAttributeIfAny(constValue, defaultSyntax.Value, diagnostics)
            End If

            Return constValue
        End Function

        Friend ReadOnly Property SyntaxNode As ParameterSyntax
            Get
                Return If(_syntaxRef Is Nothing, Nothing, DirectCast(_syntaxRef.GetSyntax(), ParameterSyntax))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                If IsImplicitlyDeclared Then
                    Return ImmutableArray(Of SyntaxReference).Empty
                ElseIf _syntaxRef IsNot Nothing Then
                    Return GetDeclaringSyntaxReferenceHelper(_syntaxRef)
                Else
                    Return MyBase.DeclaringSyntaxReferences
                End If
            End Get
        End Property

        Friend NotOverridable Overrides Function IsDefinedInSourceTree(tree As SyntaxTree, definedWithinSpan As TextSpan?, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            Return IsDefinedInSourceTree(Me.SyntaxNode, tree, definedWithinSpan, cancellationToken)
        End Function

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return (_flags And SourceParameterFlags.Optional) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                If (_flags And SourceParameterFlags.ParamArray) <> 0 Then
                    Return True
                End If

                Dim attributeSource As SourceParameterSymbol = If(Me.BoundAttributesSource, Me)

                Dim data = attributeSource.GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasParamArrayAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Dim attributeSource As SourceParameterSymbol = If(Me.BoundAttributesSource, Me)

                Dim data = attributeSource.GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasCallerLineNumberAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Dim attributeSource As SourceParameterSymbol = If(Me.BoundAttributesSource, Me)

                Dim data = attributeSource.GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasCallerMemberNameAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Dim attributeSource As SourceParameterSymbol = If(Me.BoundAttributesSource, Me)

                Dim data = attributeSource.GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasCallerFilePathAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
            Get
                Dim attributeSource As SourceParameterSymbol = If(Me.BoundAttributesSource, Me)

                Dim data = attributeSource.GetEarlyDecodedWellKnownAttributeData()
                If data Is Nothing Then
                    Return -1
                End If

                Return data.CallerArgumentExpressionParameterIndex
            End Get
        End Property

        ''' <summary>
        ''' Is parameter explicitly declared ByRef. Can be different from IsByRef only for
        ''' String parameters of Declare methods.
        ''' </summary>
        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return (_flags And SourceParameterFlags.ByRef) <> 0
            End Get
        End Property

        Private Sub New(
            container As Symbol,
            name As String,
            ordinal As Integer,
            type As TypeSymbol,
            location As Location,
            syntaxRef As SyntaxReference,
            flags As SourceParameterFlags,
            defaultValueOpt As ConstantValue
        )
            MyBase.New(container, name, ordinal, type, location)
            _flags = flags
            _lazyDefaultValue = defaultValueOpt
            _syntaxRef = syntaxRef
        End Sub

        ' Create a simple or complex parameter, as necessary.
        Friend Shared Function Create(container As Symbol,
                                 name As String,
                                 ordinal As Integer,
                                 type As TypeSymbol,
                                 location As Location,
                                 syntaxRef As SyntaxReference,
                                 flags As SourceParameterFlags,
                                 defaultValueOpt As ConstantValue) As ParameterSymbol

            ' Note that parameters of partial method declarations should always be complex
            Dim method = TryCast(container, SourceMethodSymbol)

            If flags <> 0 OrElse
                defaultValueOpt IsNot Nothing OrElse
                syntaxRef IsNot Nothing OrElse
                (method IsNot Nothing AndAlso method.IsPartial) Then

                Return New SourceComplexParameterSymbol(container, name, ordinal, type, location, syntaxRef, flags, defaultValueOpt)
            Else
                Return New SourceSimpleParameterSymbol(container, name, ordinal, type, location)
            End If
        End Function

        Friend Overrides Function ChangeOwner(newContainingSymbol As Symbol) As ParameterSymbol
            ' When changing the owner, it is not safe to get the constant value from this parameter symbol (Me.DefaultConstantValue). Default values for source
            ' parameters are computed after all members have been added to the  type. See GenerateAllDeclarationErrors in SourceNamedTypeSymbol.
            ' Asking for the default value earlier than that can lead to infinite recursion. Instead, pass in the m_lazyDefaultValue.  If the value hasn't
            ' been computed yet, the new symbol will compute it.

            Return New SourceComplexParameterSymbol(newContainingSymbol, Me.Name, Me.Ordinal, Me.Type, Me.GetFirstLocation(), _syntaxRef, _flags, _lazyDefaultValue)
        End Function

        ' Create a parameter from syntax.
        Friend Shared Function CreateFromSyntax(container As Symbol,
                                                syntax As ParameterSyntax,
                                                name As String,
                                                flags As SourceParameterFlags,
                                                ordinal As Integer,
                                                binder As Binder,
                                                checkModifier As CheckParameterModifierDelegate,
                                                diagnostics As BindingDiagnosticBag) As ParameterSymbol
            Dim getErrorInfo As Func(Of DiagnosticInfo) = Nothing

            If binder.OptionStrict = OptionStrict.On Then
                getErrorInfo = ErrorFactory.GetErrorInfo_ERR_StrictDisallowsImplicitArgs
            ElseIf binder.OptionStrict = OptionStrict.Custom Then
                getErrorInfo = ErrorFactory.GetErrorInfo_WRN_ObjectAssumedVar1_WRN_MissingAsClauseinVarDecl
            End If

            Dim paramType = binder.DecodeModifiedIdentifierType(syntax.Identifier, syntax.AsClause, Nothing, getErrorInfo, diagnostics, ModifiedIdentifierTypeDecoderContext.ParameterType)

            If (flags And SourceParameterFlags.ParamArray) <> 0 AndAlso paramType.TypeKind <> TypeKind.Error Then
                If paramType.TypeKind <> TypeKind.Array Then
                    ' ParamArray must be of array type.
                    Binder.ReportDiagnostic(diagnostics, syntax.Identifier, ERRID.ERR_ParamArrayNotArray)
                Else
                    Dim paramTypeAsArray = DirectCast(paramType, ArrayTypeSymbol)
                    If Not paramTypeAsArray.IsSZArray Then
                        ' ParamArray type must be rank-1 array.
                        Binder.ReportDiagnostic(diagnostics, syntax.Identifier.Identifier, ERRID.ERR_ParamArrayRank)
                    End If

                    ' 'touch' the constructor in order to generate proper diagnostics
                    binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_ParamArrayAttribute__ctor,
                                                                     syntax,
                                                                     diagnostics)
                End If
            End If

            Dim syntaxRef As SyntaxReference = Nothing

            ' Attributes and default values are computed lazily and need access to the parameter's syntax.
            ' If the parameter syntax includes either of these get a syntax reference and pass it to the parameter symbol.
            If (syntax.AttributeLists.Count <> 0 OrElse syntax.Default IsNot Nothing) Then
                syntaxRef = binder.GetSyntaxReference(syntax)
            End If

            Dim defaultValue As ConstantValue = Nothing

            If (flags And SourceParameterFlags.Optional) <> 0 Then
                ' The default value is computed lazily. If there is default syntax then set the value to ConstantValue.unset to indicate the value needs to
                ' be computed.
                If syntax.Default IsNot Nothing Then
                    defaultValue = ConstantValue.Unset
                End If

                ' Report diagnostic if constructors for datetime and decimal default values are not available
                Select Case paramType.SpecialType
                    Case SpecialType.System_DateTime
                        binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor,
                                                                         syntax.Default,
                                                                         diagnostics)

                    Case SpecialType.System_Decimal
                        binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                                                                        syntax.Default,
                                                                        diagnostics)
                End Select
            End If

            Return Create(container, name, ordinal, paramType, syntax.Identifier.Identifier.GetLocation(), syntaxRef, flags, defaultValue)
        End Function

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Friend Overrides Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), refCustomModifiers As ImmutableArray(Of CustomModifier)) As ParameterSymbol
            If customModifiers.IsEmpty AndAlso refCustomModifiers.IsEmpty Then
                Return New SourceComplexParameterSymbol(Me.ContainingSymbol, Me.Name, Me.Ordinal, type, Me.Location, _syntaxRef, _flags, _lazyDefaultValue)
            End If

            Return New SourceComplexParameterSymbolWithCustomModifiers(Me.ContainingSymbol, Me.Name, Me.Ordinal, type, Me.Location, _syntaxRef, _flags, _lazyDefaultValue, customModifiers, refCustomModifiers)
        End Function

        Private Class SourceComplexParameterSymbolWithCustomModifiers
            Inherits SourceComplexParameterSymbol

            Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)
            Private ReadOnly _refCustomModifiers As ImmutableArray(Of CustomModifier)

            Public Sub New(
                container As Symbol,
                name As String,
                ordinal As Integer,
                type As TypeSymbol,
                location As Location,
                syntaxRef As SyntaxReference,
                flags As SourceParameterFlags,
                defaultValueOpt As ConstantValue,
                customModifiers As ImmutableArray(Of CustomModifier),
                refCustomModifiers As ImmutableArray(Of CustomModifier)
            )
                MyBase.New(container, name, ordinal, type, location, syntaxRef, flags, defaultValueOpt)

                Debug.Assert(Not customModifiers.IsEmpty OrElse Not refCustomModifiers.IsEmpty)
                _customModifiers = customModifiers
                _refCustomModifiers = refCustomModifiers

                Debug.Assert(_refCustomModifiers.IsEmpty OrElse IsByRef)
            End Sub

            Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _customModifiers
                End Get
            End Property

            Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _refCustomModifiers
                End Get
            End Property

            Friend Overrides Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), refCustomModifiers As ImmutableArray(Of CustomModifier)) As ParameterSymbol
                Throw ExceptionUtilities.Unreachable
            End Function
        End Class

    End Class

End Namespace

