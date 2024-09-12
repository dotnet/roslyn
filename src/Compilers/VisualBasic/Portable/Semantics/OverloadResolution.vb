' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind
Imports ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class OverloadResolution

        Private Sub New()
            Throw ExceptionUtilities.Unreachable
        End Sub

        ''' <summary>
        ''' Information about a candidate from a group.
        ''' Will have different implementation for methods, extension methods and properties.
        ''' </summary>
        ''' <remarks></remarks>
        Public MustInherit Class Candidate

            Public MustOverride ReadOnly Property UnderlyingSymbol As Symbol

            Friend MustOverride Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As Candidate

            ''' <summary>
            ''' Whether the method is used as extension method vs. called as a static method.
            ''' </summary>
            Public Overridable ReadOnly Property IsExtensionMethod As Boolean
                Get
                    Return False
                End Get
            End Property

            ''' <summary>
            ''' Whether the method is used as an operator.
            ''' </summary>
            Public Overridable ReadOnly Property IsOperator As Boolean
                Get
                    Return False
                End Get
            End Property

            ''' <summary>
            ''' Whether the method is used in a lifted to nullable form.
            ''' </summary>
            Public Overridable ReadOnly Property IsLifted As Boolean
                Get
                    Return False
                End Get
            End Property

            ''' <summary>
            ''' Precedence level for an extension method.
            ''' </summary>
            Public Overridable ReadOnly Property PrecedenceLevel As Integer
                Get
                    Return 0
                End Get
            End Property

            ''' <summary>
            ''' Extension method type parameters that were fixed during currying, if any.
            ''' If none were fixed, BitArray.Null should be returned.
            ''' </summary>
            Public Overridable ReadOnly Property FixedTypeParameters As BitVector
                Get
                    Return BitVector.Null
                End Get
            End Property

            Public MustOverride ReadOnly Property IsGeneric As Boolean
            Public MustOverride ReadOnly Property ParameterCount As Integer
            Public MustOverride Function Parameters(index As Integer) As ParameterSymbol
            Public MustOverride ReadOnly Property ReturnType As TypeSymbol

            Public MustOverride ReadOnly Property Arity As Integer
            Public MustOverride ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)

            Friend Sub GetAllParameterCounts(
                ByRef requiredCount As Integer,
                ByRef maxCount As Integer,
                ByRef hasParamArray As Boolean
            )
                maxCount = Me.ParameterCount
                hasParamArray = False
                requiredCount = -1

                Dim last = maxCount - 1

                For i As Integer = 0 To last Step 1
                    Dim param As ParameterSymbol = Me.Parameters(i)

                    If i = last AndAlso param.IsParamArray Then
                        hasParamArray = True
                    ElseIf Not param.IsOptional Then
                        requiredCount = i
                    End If
                Next

                requiredCount += 1
            End Sub

            Friend Function TryGetNamedParamIndex(name As String, ByRef index As Integer) As Boolean
                For i As Integer = 0 To Me.ParameterCount - 1 Step 1
                    Dim param As ParameterSymbol = Me.Parameters(i)

                    If IdentifierComparison.Equals(name, param.Name) Then
                        index = i
                        Return True
                    End If
                Next

                index = -1
                Return False
            End Function

            ''' <summary>
            ''' Receiver type for extension method. Otherwise, containing type.
            ''' </summary>
            Public MustOverride ReadOnly Property ReceiverType As TypeSymbol

            ''' <summary>
            ''' For extension methods, the type of the fist parameter in method's definition (i.e. before type parameters are substituted).
            ''' Otherwise, same as the ReceiverType.
            ''' </summary>
            Public MustOverride ReadOnly Property ReceiverTypeDefinition As TypeSymbol

            Friend MustOverride Function IsOverriddenBy(otherSymbol As Symbol) As Boolean
        End Class

        ''' <summary>
        ''' Implementation for an ordinary method (based on usage).
        ''' </summary>
        Public Class MethodCandidate
            Inherits Candidate

            Protected ReadOnly m_Method As MethodSymbol

            Public Sub New(method As MethodSymbol)
                Debug.Assert(method IsNot Nothing)
                Debug.Assert(method.ReducedFrom Is Nothing OrElse Me.IsExtensionMethod)
                m_Method = method
            End Sub

            Friend Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As Candidate
                Return New MethodCandidate(m_Method.Construct(typeArguments))
            End Function

            Public Overrides ReadOnly Property IsGeneric As Boolean
                Get
                    Return m_Method.IsGenericMethod
                End Get
            End Property

            Public Overrides ReadOnly Property ParameterCount As Integer
                Get
                    Return m_Method.ParameterCount
                End Get
            End Property

            Public Overrides Function Parameters(index As Integer) As ParameterSymbol
                Return m_Method.Parameters(index)
            End Function

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return m_Method.ReturnType
                End Get
            End Property

            Public Overrides ReadOnly Property ReceiverType As TypeSymbol
                Get
                    Return m_Method.ContainingType
                End Get
            End Property

            Public Overrides ReadOnly Property ReceiverTypeDefinition As TypeSymbol
                Get
                    Return m_Method.ContainingType
                End Get
            End Property

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return m_Method.Arity
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return m_Method.TypeParameters
                End Get
            End Property

            Public Overrides ReadOnly Property UnderlyingSymbol As Symbol
                Get
                    Return m_Method
                End Get
            End Property

            Friend Overrides Function IsOverriddenBy(otherSymbol As Symbol) As Boolean
                Dim definition As MethodSymbol = m_Method.OriginalDefinition

                If definition.IsOverridable OrElse definition.IsOverrides OrElse definition.IsMustOverride Then
                    Dim otherMethod As MethodSymbol = DirectCast(otherSymbol, MethodSymbol).OverriddenMethod

                    While otherMethod IsNot Nothing
                        If otherMethod.OriginalDefinition.Equals(definition) Then
                            Return True
                        End If

                        otherMethod = otherMethod.OverriddenMethod
                    End While
                End If

                Return False
            End Function
        End Class

        ''' <summary>
        ''' Implementation for an extension method, i.e. it is used as an extension method.
        ''' </summary>
        Public NotInheritable Class ExtensionMethodCandidate
            Inherits MethodCandidate

            Private _fixedTypeParameters As BitVector

            Public Sub New(method As MethodSymbol)
                Me.New(method, GetFixedTypeParameters(method))
            End Sub

            ' TODO: Consider building this bitmap lazily, on demand.
            Private Shared Function GetFixedTypeParameters(method As MethodSymbol) As BitVector
                If method.FixedTypeParameters.Length > 0 Then
                    Dim fixedTypeParameters = BitVector.Create(method.ReducedFrom.Arity)

                    For Each fixed As KeyValuePair(Of TypeParameterSymbol, TypeSymbol) In method.FixedTypeParameters
                        fixedTypeParameters(fixed.Key.Ordinal) = True
                    Next

                    Return fixedTypeParameters
                End If

                Return Nothing
            End Function

            Private Sub New(method As MethodSymbol, fixedTypeParameters As BitVector)
                MyBase.New(method)

                Debug.Assert(method.ReducedFrom IsNot Nothing)
                _fixedTypeParameters = fixedTypeParameters
            End Sub

            Public Overrides ReadOnly Property IsExtensionMethod As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property PrecedenceLevel As Integer
                Get
                    Return m_Method.Proximity
                End Get
            End Property

            Public Overrides ReadOnly Property FixedTypeParameters As BitVector
                Get
                    Return _fixedTypeParameters
                End Get
            End Property

            Friend Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As Candidate
                Return New ExtensionMethodCandidate(m_Method.Construct(typeArguments), _fixedTypeParameters)
            End Function

            Public Overrides ReadOnly Property ReceiverType As TypeSymbol
                Get
                    Return m_Method.ReceiverType
                End Get
            End Property

            Public Overrides ReadOnly Property ReceiverTypeDefinition As TypeSymbol
                Get
                    Return m_Method.ReducedFrom.Parameters(0).Type
                End Get
            End Property

            Friend Overrides Function IsOverriddenBy(otherSymbol As Symbol) As Boolean
                Return False ' Extension methods never override/overridden
            End Function
        End Class

        ''' <summary>
        ''' Implementation for an operator
        ''' </summary>
        Public Class OperatorCandidate
            Inherits MethodCandidate

            Public Sub New(method As MethodSymbol)
                MyBase.New(method)
            End Sub

            Public NotOverridable Overrides ReadOnly Property IsOperator As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Implementation for a lifted operator.
        ''' </summary>
        Public Class LiftedOperatorCandidate
            Inherits OperatorCandidate

            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
            Private ReadOnly _returnType As TypeSymbol

            Public Sub New(method As MethodSymbol, parameters As ImmutableArray(Of ParameterSymbol), returnType As TypeSymbol)
                MyBase.New(method)
                Debug.Assert(parameters.Length = method.ParameterCount)
                _parameters = parameters
                _returnType = returnType
            End Sub

            Public Overrides ReadOnly Property ParameterCount As Integer
                Get
                    Return _parameters.Length
                End Get
            End Property

            Public Overrides Function Parameters(index As Integer) As ParameterSymbol
                Return _parameters(index)
            End Function

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return _returnType
                End Get
            End Property

            Public Overrides ReadOnly Property IsLifted As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Implementation for a property.
        ''' </summary>
        Public NotInheritable Class PropertyCandidate
            Inherits Candidate

            Private ReadOnly _property As PropertySymbol

            Public Sub New([property] As PropertySymbol)
                Debug.Assert([property] IsNot Nothing)
                _property = [property]
            End Sub

            Friend Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As Candidate
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides ReadOnly Property IsGeneric As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property ParameterCount As Integer
                Get
                    Return _property.Parameters.Length
                End Get
            End Property

            Public Overrides Function Parameters(index As Integer) As ParameterSymbol
                Return _property.Parameters(index)
            End Function

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return _property.Type
                End Get
            End Property

            Public Overrides ReadOnly Property ReceiverType As TypeSymbol
                Get
                    Return _property.ContainingType
                End Get
            End Property

            Public Overrides ReadOnly Property ReceiverTypeDefinition As TypeSymbol
                Get
                    Return _property.ContainingType
                End Get
            End Property

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return 0
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return ImmutableArray(Of TypeParameterSymbol).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property UnderlyingSymbol As Symbol
                Get
                    Return _property
                End Get
            End Property

            Friend Overrides Function IsOverriddenBy(otherSymbol As Symbol) As Boolean
                Dim definition As PropertySymbol = _property.OriginalDefinition

                If definition.IsOverridable OrElse definition.IsOverrides OrElse definition.IsMustOverride Then
                    Dim otherProperty As PropertySymbol = DirectCast(otherSymbol, PropertySymbol).OverriddenProperty

                    While otherProperty IsNot Nothing
                        If otherProperty.OriginalDefinition.Equals(definition) Then
                            Return True
                        End If

                        otherProperty = otherProperty.OverriddenProperty
                    End While
                End If

                Return False
            End Function
        End Class

        Private Const s_stateSize = 8   ' bit size of the following enum
        Public Enum CandidateAnalysisResultState As Byte
            Applicable

            ' All following states are to indicate inapplicability
            HasUnsupportedMetadata
            HasUseSiteError
            Ambiguous
            BadGenericArity
            ArgumentCountMismatch
            TypeInferenceFailed
            ArgumentMismatch
            GenericConstraintsViolated
            RequiresNarrowing
            RequiresNarrowingNotFromObject
            ExtensionMethodVsInstanceMethod
            Shadowed
            LessApplicable
            ExtensionMethodVsLateBinding

            Count
        End Enum

        <Flags()>
        Private Enum SmallFieldMask As Integer
            State = (1 << s_stateSize) - 1

            IsExpandedParamArrayForm = 1 << (s_stateSize + 0)

            InferenceLevelShift = (s_stateSize + 1)
            InferenceLevelMask = 3 << InferenceLevelShift ' 2 bits are used

            ArgumentMatchingDone = 1 << (s_stateSize + 3)
            RequiresNarrowingConversion = 1 << (s_stateSize + 4)
            RequiresNarrowingNotFromObject = 1 << (s_stateSize + 5)
            RequiresNarrowingNotFromNumericConstant = 1 << (s_stateSize + 6)

            ' Must be equal to ConversionKind.DelegateRelaxationLevelMask
            ' Compile time "asserts" below enforce it by reporting a compilation error in case of a violation.
            ' I am not using the form of
            '     DelegateRelaxationLevelMask = ConversionKind.DelegateRelaxationLevelMask
            ' to make it easier to reason about bits used relative to other values in this enum.
            DelegateRelaxationLevelMask = 7 << (s_stateSize + 7) ' 3 bits used!

            SomeInferenceFailed = 1 << (s_stateSize + 10)
            AllFailedInferenceIsDueToObject = 1 << (s_stateSize + 11)

            InferenceErrorReasonsShift = (s_stateSize + 12)
            InferenceErrorReasonsMask = 3 << InferenceErrorReasonsShift

            IgnoreExtensionMethods = 1 << (s_stateSize + 14)

            IllegalInAttribute = 1 << (s_stateSize + 15)
        End Enum

#If DEBUG Then
        ' Compile time asserts.
#Disable Warning IDE0051 ' Remove unused private members
        Private Const s_delegateRelaxationLevelMask_AssertZero = SmallFieldMask.DelegateRelaxationLevelMask - ConversionKind.DelegateRelaxationLevelMask
        Private ReadOnly _delegateRelaxationLevelMask_Assert1(s_delegateRelaxationLevelMask_AssertZero) As Boolean
        Private ReadOnly _delegateRelaxationLevelMask_Assert2(-s_delegateRelaxationLevelMask_AssertZero) As Boolean

        Private Const s_inferenceLevelMask_AssertZero = CByte((SmallFieldMask.InferenceLevelMask >> SmallFieldMask.InferenceLevelShift) <> ((TypeArgumentInference.InferenceLevel.Invalid << 1) - 1))
        Private ReadOnly _inferenceLevelMask_Assert1(s_inferenceLevelMask_AssertZero) As Boolean
        Private ReadOnly _inferenceLevelMask_Assert2(-s_inferenceLevelMask_AssertZero) As Boolean
#Enable Warning IDE0051 ' Remove unused private members
#End If
        Public Structure OptionalArgument
            Public ReadOnly DefaultValue As BoundExpression
            Public ReadOnly Conversion As KeyValuePair(Of ConversionKind, MethodSymbol)
            Public ReadOnly Dependencies As ImmutableArray(Of AssemblySymbol)

            Public Sub New(value As BoundExpression, conversion As KeyValuePair(Of ConversionKind, MethodSymbol), dependencies As ImmutableArray(Of AssemblySymbol))
                Me.DefaultValue = value
                Me.Conversion = conversion
                Me.Dependencies = dependencies.NullToEmpty()
            End Sub
        End Structure

        Public Structure CandidateAnalysisResult

            Public ReadOnly Property IsExpandedParamArrayForm As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.IsExpandedParamArrayForm) <> 0
                End Get
            End Property

            Public Sub SetIsExpandedParamArrayForm()
                _smallFields = _smallFields Or SmallFieldMask.IsExpandedParamArrayForm
            End Sub

            Public ReadOnly Property InferenceLevel As TypeArgumentInference.InferenceLevel
                Get
                    Return CType((_smallFields And SmallFieldMask.InferenceLevelMask) >> SmallFieldMask.InferenceLevelShift, TypeArgumentInference.InferenceLevel)
                End Get
            End Property

            Public Sub SetInferenceLevel(level As TypeArgumentInference.InferenceLevel)
                Dim value As Integer = CInt(level) << SmallFieldMask.InferenceLevelShift
                Debug.Assert((value And SmallFieldMask.InferenceLevelMask) = value)

                _smallFields = (_smallFields And (Not SmallFieldMask.InferenceLevelMask)) Or (value And SmallFieldMask.InferenceLevelMask)
            End Sub

            Public ReadOnly Property ArgumentMatchingDone As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.ArgumentMatchingDone) <> 0
                End Get
            End Property

            Public Sub SetArgumentMatchingDone()
                _smallFields = _smallFields Or SmallFieldMask.ArgumentMatchingDone
            End Sub

            Public ReadOnly Property RequiresNarrowingConversion As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.RequiresNarrowingConversion) <> 0
                End Get
            End Property

            Public Sub SetRequiresNarrowingConversion()
                _smallFields = _smallFields Or SmallFieldMask.RequiresNarrowingConversion
            End Sub

            Public ReadOnly Property RequiresNarrowingNotFromObject As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.RequiresNarrowingNotFromObject) <> 0
                End Get
            End Property

            Public Sub SetRequiresNarrowingNotFromObject()
                _smallFields = _smallFields Or SmallFieldMask.RequiresNarrowingNotFromObject
            End Sub

            Public ReadOnly Property RequiresNarrowingNotFromNumericConstant As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.RequiresNarrowingNotFromNumericConstant) <> 0
                End Get
            End Property

            Public Sub SetRequiresNarrowingNotFromNumericConstant()
                Debug.Assert(RequiresNarrowingConversion)
                IgnoreExtensionMethods = False

                _smallFields = _smallFields Or SmallFieldMask.RequiresNarrowingNotFromNumericConstant
            End Sub

            ''' <summary>
            ''' Only bits specific to delegate relaxation level are returned.
            ''' </summary>
            Public ReadOnly Property MaxDelegateRelaxationLevel As ConversionKind
                Get
                    Return CType(_smallFields And SmallFieldMask.DelegateRelaxationLevelMask, ConversionKind)
                End Get
            End Property

            Public Sub RegisterDelegateRelaxationLevel(conversionKind As ConversionKind)
                Dim relaxationLevel As Integer = (conversionKind And SmallFieldMask.DelegateRelaxationLevelMask)

                If relaxationLevel > (_smallFields And SmallFieldMask.DelegateRelaxationLevelMask) Then
                    Debug.Assert(relaxationLevel <= ConversionKind.DelegateRelaxationLevelNarrowing)

                    If relaxationLevel = ConversionKind.DelegateRelaxationLevelNarrowing Then
                        IgnoreExtensionMethods = False
                    End If

                    _smallFields = (_smallFields And (Not SmallFieldMask.DelegateRelaxationLevelMask)) Or relaxationLevel
                End If
            End Sub

            Public Sub SetSomeInferenceFailed()
                _smallFields = _smallFields Or SmallFieldMask.SomeInferenceFailed
            End Sub

            Public ReadOnly Property SomeInferenceFailed As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.SomeInferenceFailed) <> 0
                End Get
            End Property

            Public Sub SetIllegalInAttribute()
                _smallFields = _smallFields Or SmallFieldMask.IllegalInAttribute
            End Sub

            Public ReadOnly Property IsIllegalInAttribute As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.IllegalInAttribute) <> 0
                End Get
            End Property

            Public Sub SetAllFailedInferenceIsDueToObject()
                _smallFields = _smallFields Or SmallFieldMask.AllFailedInferenceIsDueToObject
            End Sub

            Public ReadOnly Property AllFailedInferenceIsDueToObject As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.AllFailedInferenceIsDueToObject) <> 0
                End Get
            End Property

            Public Sub SetInferenceErrorReasons(reasons As InferenceErrorReasons)
                Dim value As Integer = CInt(reasons) << SmallFieldMask.InferenceErrorReasonsShift
                Debug.Assert((value And SmallFieldMask.InferenceErrorReasonsMask) = value)

                _smallFields = (_smallFields And (Not SmallFieldMask.InferenceErrorReasonsMask)) Or (value And SmallFieldMask.InferenceErrorReasonsMask)
            End Sub

            Public ReadOnly Property InferenceErrorReasons As InferenceErrorReasons
                Get
                    Return CType((_smallFields And SmallFieldMask.InferenceErrorReasonsMask) >> SmallFieldMask.InferenceErrorReasonsShift, InferenceErrorReasons)
                End Get
            End Property

            Public Property State As CandidateAnalysisResultState
                Get
                    Return CType(_smallFields And SmallFieldMask.State, CandidateAnalysisResultState)
                End Get
                Set(value As CandidateAnalysisResultState)
                    Debug.Assert((value And (Not SmallFieldMask.State)) = 0)

                    Dim newFields = _smallFields And (Not SmallFieldMask.State)
                    newFields = newFields Or value
                    _smallFields = newFields
                End Set
            End Property

            Public Property IgnoreExtensionMethods As Boolean
                Get
                    Return (_smallFields And SmallFieldMask.IgnoreExtensionMethods) <> 0
                End Get
                Set(value As Boolean)
                    If value Then
                        _smallFields = _smallFields Or SmallFieldMask.IgnoreExtensionMethods
                    Else
                        _smallFields = _smallFields And (Not SmallFieldMask.IgnoreExtensionMethods)
                    End If
                End Set
            End Property

            Private _smallFields As Integer

            Public Candidate As Candidate
            Public ExpandedParamArrayArgumentsUsed As Integer
            Public EquallyApplicableCandidatesBucket As Integer

            ' When this is null, it means that arguments map to parameters sequentially
            Public ArgsToParamsOpt As ImmutableArray(Of Integer)

            ' When these are null, it means that all conversions are identity conversions
            Public ConversionsOpt As ImmutableArray(Of KeyValuePair(Of ConversionKind, MethodSymbol))
            Public ConversionsBackOpt As ImmutableArray(Of KeyValuePair(Of ConversionKind, MethodSymbol))

            ' When this is null, it means that there aren't any optional arguments
            ' This array is indexed by parameter index, not the argument index.
            Public OptionalArguments As ImmutableArray(Of OptionalArgument)

            Public ReadOnly Property UsedOptionalParameterDefaultValue As Boolean
                Get
                    Return Not OptionalArguments.IsDefault
                End Get
            End Property

            Public NotInferredTypeArguments As BitVector

            Public TypeArgumentInferenceDiagnosticsOpt As ImmutableBindingDiagnostic(Of AssemblySymbol)

            Public Sub New(candidate As Candidate, state As CandidateAnalysisResultState)
                Me.Candidate = candidate
                Me.State = state
            End Sub

            Public Sub New(candidate As Candidate)
                Me.Candidate = candidate
                Me.State = CandidateAnalysisResultState.Applicable
            End Sub
        End Structure

        ' Represents a simple overload resolution result
        Friend Structure OverloadResolutionResult
            Private ReadOnly _bestResult As CandidateAnalysisResult?
            Private ReadOnly _allResults As ImmutableArray(Of CandidateAnalysisResult)
            Private ReadOnly _resolutionIsLateBound As Boolean
            Private ReadOnly _remainingCandidatesRequireNarrowingConversion As Boolean
            Public ReadOnly AsyncLambdaSubToFunctionMismatch As ImmutableArray(Of BoundExpression)

            ' Create an overload resolution result from a full set of results.
            Public Sub New(allResults As ImmutableArray(Of CandidateAnalysisResult), resolutionIsLateBound As Boolean,
                           remainingCandidatesRequireNarrowingConversion As Boolean,
                           asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression))
                Me._allResults = allResults
                Me._resolutionIsLateBound = resolutionIsLateBound
                Me._remainingCandidatesRequireNarrowingConversion = remainingCandidatesRequireNarrowingConversion
                Me.AsyncLambdaSubToFunctionMismatch = If(asyncLambdaSubToFunctionMismatch Is Nothing,
                                                         ImmutableArray(Of BoundExpression).Empty,
                                                         asyncLambdaSubToFunctionMismatch.ToArray().AsImmutableOrNull())

                If Not resolutionIsLateBound Then
                    Me._bestResult = GetBestResult(allResults)
                End If
            End Sub

            Public ReadOnly Property Candidates As ImmutableArray(Of CandidateAnalysisResult)
                Get
                    Return _allResults
                End Get
            End Property

            ' Returns the best method. Note that if overload resolution succeeded, the set of conversion kinds will NOT be returned.
            Public ReadOnly Property BestResult As CandidateAnalysisResult?
                Get
                    Return _bestResult
                End Get
            End Property

            Public ReadOnly Property ResolutionIsLateBound As Boolean
                Get
                    Return _resolutionIsLateBound
                End Get
            End Property

            ''' <summary>
            ''' This might simplify error reporting. If not, consider getting rid of this property.
            ''' </summary>
            Public ReadOnly Property RemainingCandidatesRequireNarrowingConversion As Boolean
                Get
                    Return _remainingCandidatesRequireNarrowingConversion
                End Get
            End Property

            Private Shared Function GetBestResult(allResults As ImmutableArray(Of CandidateAnalysisResult)) As CandidateAnalysisResult?
                Dim best As CandidateAnalysisResult? = Nothing
                Dim i As Integer = 0

                While i < allResults.Length
                    Dim current = allResults(i)

                    If current.State = CandidateAnalysisResultState.Applicable Then
                        If best IsNot Nothing Then
                            Return Nothing
                        End If

                        best = current
                    End If

                    i = i + 1
                End While

                Return best
            End Function

        End Structure

        ''' <summary>
        ''' Perform overload resolution on the given method or property group, with the given arguments and names.
        ''' The names can be null if no names were supplied to any arguments.
        ''' </summary>
        Public Shared Function MethodOrPropertyInvocationOverloadResolution(
            group As BoundMethodOrPropertyGroup,
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            binder As Binder,
            callerInfoOpt As SyntaxNode,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
            Optional includeEliminatedCandidates As Boolean = False,
            Optional forceExpandedForm As Boolean = False
        ) As OverloadResolutionResult

            If group.Kind = BoundKind.MethodGroup Then
                Dim methodGroup = DirectCast(group, BoundMethodGroup)
                Return MethodInvocationOverloadResolution(
                    methodGroup,
                    arguments,
                    argumentNames,
                    binder,
                    callerInfoOpt,
                    useSiteInfo,
                    includeEliminatedCandidates,
                    forceExpandedForm:=forceExpandedForm)
            Else
                Dim propertyGroup = DirectCast(group, BoundPropertyGroup)
                Return PropertyInvocationOverloadResolution(
                    propertyGroup,
                    arguments,
                    argumentNames,
                    binder,
                    callerInfoOpt,
                    useSiteInfo,
                    includeEliminatedCandidates)
            End If

        End Function

        ''' <summary>
        ''' Perform overload resolution on the given method group, with the given arguments.
        ''' </summary>
        Public Shared Function QueryOperatorInvocationOverloadResolution(
            methodGroup As BoundMethodGroup,
            arguments As ImmutableArray(Of BoundExpression),
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
            Optional includeEliminatedCandidates As Boolean = False
        ) As OverloadResolutionResult
            Return MethodInvocationOverloadResolution(
                        methodGroup,
                        arguments,
                        Nothing,
                        binder,
                        callerInfoOpt:=Nothing,
                        useSiteInfo:=useSiteInfo,
                        includeEliminatedCandidates:=includeEliminatedCandidates,
                        lateBindingIsAllowed:=False,
                        isQueryOperatorInvocation:=True)

        End Function

        ''' <summary>
        ''' Perform overload resolution on the given method group, with the given arguments and names.
        ''' The names can be null if no names were supplied to any arguments.
        ''' </summary>
        Public Shared Function MethodInvocationOverloadResolution(
            methodGroup As BoundMethodGroup,
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            binder As Binder,
            callerInfoOpt As SyntaxNode,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
            Optional includeEliminatedCandidates As Boolean = False,
            Optional delegateReturnType As TypeSymbol = Nothing,
            Optional delegateReturnTypeReferenceBoundNode As BoundNode = Nothing,
            Optional lateBindingIsAllowed As Boolean = True,
            Optional isQueryOperatorInvocation As Boolean = False,
            Optional forceExpandedForm As Boolean = False
        ) As OverloadResolutionResult
            Debug.Assert(methodGroup.ResultKind = LookupResultKind.Good OrElse methodGroup.ResultKind = LookupResultKind.Inaccessible)

            Dim typeArguments = If(methodGroup.TypeArgumentsOpt IsNot Nothing, methodGroup.TypeArgumentsOpt.Arguments, ImmutableArray(Of TypeSymbol).Empty)

            ' To simplify code later
            If typeArguments.IsDefault Then
                typeArguments = ImmutableArray(Of TypeSymbol).Empty
            End If

            If arguments.IsDefault Then
                arguments = ImmutableArray(Of BoundExpression).Empty
            End If

            Dim candidates = ArrayBuilder(Of CandidateAnalysisResult).GetInstance()

            Dim instanceCandidates As ArrayBuilder(Of Candidate) = ArrayBuilder(Of Candidate).GetInstance()
            Dim curriedCandidates As ArrayBuilder(Of Candidate) = ArrayBuilder(Of Candidate).GetInstance()

            Dim methods As ImmutableArray(Of MethodSymbol) = methodGroup.Methods

            If Not methods.IsDefault Then
                ' Create MethodCandidates for ordinary methods and ExtensionMethodCandidates
                ' for curried methods, separating them.
                For Each method As MethodSymbol In methods
                    If method.ReducedFrom Is Nothing Then
                        instanceCandidates.Add(New MethodCandidate(method))
                    Else
                        curriedCandidates.Add(New ExtensionMethodCandidate(method))
                    End If
                Next
            End If

            Dim asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression) = Nothing

            Dim applicableNarrowingCandidateCount As Integer = 0
            Dim applicableInstanceCandidateCount As Integer = 0

            ' First collect instance methods.
            If instanceCandidates.Count > 0 Then

                CollectOverloadedCandidates(
                    binder, candidates, instanceCandidates, typeArguments,
                    arguments, argumentNames, delegateReturnType, delegateReturnTypeReferenceBoundNode,
                    includeEliminatedCandidates, isQueryOperatorInvocation, forceExpandedForm, asyncLambdaSubToFunctionMismatch,
                    useSiteInfo)

                applicableInstanceCandidateCount = EliminateNotApplicableToArguments(methodGroup, candidates, arguments, argumentNames, binder,
                                                                                     applicableNarrowingCandidateCount, asyncLambdaSubToFunctionMismatch,
                                                                                     callerInfoOpt,
                                                                                     forceExpandedForm,
                                                                                     useSiteInfo)
            End If

            instanceCandidates.Free()
            instanceCandidates = Nothing

            ' Now add extension methods if they should be considered.
            Dim addedExtensionMethods As Boolean = False

            If ShouldConsiderExtensionMethods(candidates) Then
                ' Request additional extension methods, if any available.
                If methodGroup.ResultKind = LookupResultKind.Good Then
                    methods = methodGroup.AdditionalExtensionMethods(useSiteInfo)

                    For Each method As MethodSymbol In methods
                        curriedCandidates.Add(New ExtensionMethodCandidate(method))
                    Next
                End If

                If curriedCandidates.Count > 0 Then
                    addedExtensionMethods = True

                    CollectOverloadedCandidates(
                        binder, candidates, curriedCandidates, typeArguments,
                        arguments, argumentNames, delegateReturnType, delegateReturnTypeReferenceBoundNode,
                        includeEliminatedCandidates, isQueryOperatorInvocation, forceExpandedForm, asyncLambdaSubToFunctionMismatch,
                        useSiteInfo)
                End If
            End If

            curriedCandidates.Free()

            Dim result As OverloadResolutionResult
            If applicableInstanceCandidateCount = 0 AndAlso Not addedExtensionMethods Then
                result = ReportOverloadResolutionFailedOrLateBound(candidates, applicableInstanceCandidateCount, lateBindingIsAllowed AndAlso binder.OptionStrict <> OptionStrict.On, asyncLambdaSubToFunctionMismatch)
            Else
                result = ResolveOverloading(methodGroup, candidates, arguments, argumentNames, delegateReturnType, lateBindingIsAllowed, binder, asyncLambdaSubToFunctionMismatch, callerInfoOpt, forceExpandedForm,
                                            useSiteInfo)
            End If

            candidates.Free()
            Return result
        End Function

        Private Shared Function ReportOverloadResolutionFailedOrLateBound(candidates As ArrayBuilder(Of CandidateAnalysisResult),
                                                                   applicableNarrowingCandidateCount As Integer,
                                                                   lateBindingIsAllowed As Boolean,
                                                                   asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression)) As OverloadResolutionResult
            Dim isLateBound As Boolean = False

            If lateBindingIsAllowed Then
                For Each candidate In candidates
                    If candidate.State = CandidateAnalysisResultState.TypeInferenceFailed Then
                        If candidate.AllFailedInferenceIsDueToObject AndAlso Not candidate.Candidate.IsExtensionMethod Then
                            isLateBound = True
                            Exit For
                        End If
                    End If
                Next
            End If

            Return New OverloadResolutionResult(candidates.ToImmutable, isLateBound, applicableNarrowingCandidateCount > 0, asyncLambdaSubToFunctionMismatch)
        End Function

        ''' <summary>
        ''' Perform overload resolution on the given array of property symbols.
        ''' </summary>
        Public Shared Function PropertyInvocationOverloadResolution(
            propertyGroup As BoundPropertyGroup,
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            binder As Binder,
            callerInfoOpt As SyntaxNode,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
            Optional includeEliminatedCandidates As Boolean = False
        ) As OverloadResolutionResult
            Debug.Assert(propertyGroup.ResultKind = LookupResultKind.Good OrElse propertyGroup.ResultKind = LookupResultKind.Inaccessible)

            Dim properties As ImmutableArray(Of PropertySymbol) = propertyGroup.Properties
            ' To simplify code later
            If arguments.IsDefault Then
                arguments = ImmutableArray(Of BoundExpression).Empty
            End If

            Dim results = ArrayBuilder(Of CandidateAnalysisResult).GetInstance()
            Dim candidates = ArrayBuilder(Of Candidate).GetInstance(properties.Length - 1)

            For i As Integer = 0 To properties.Length - 1 Step 1
                candidates.Add(New PropertyCandidate(properties(i)))
            Next

            Dim asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression) = Nothing

            CollectOverloadedCandidates(binder, results, candidates, ImmutableArray(Of TypeSymbol).Empty,
                                        arguments, argumentNames, Nothing, Nothing, includeEliminatedCandidates,
                                        isQueryOperatorInvocation:=False, forceExpandedForm:=False, asyncLambdaSubToFunctionMismatch:=asyncLambdaSubToFunctionMismatch,
                                        useSiteInfo:=useSiteInfo)
            Debug.Assert(asyncLambdaSubToFunctionMismatch Is Nothing)
            candidates.Free()

            Dim result = ResolveOverloading(propertyGroup, results, arguments, argumentNames, delegateReturnType:=Nothing, lateBindingIsAllowed:=True, binder:=binder,
                                            asyncLambdaSubToFunctionMismatch:=asyncLambdaSubToFunctionMismatch, callerInfoOpt:=callerInfoOpt, forceExpandedForm:=False,
                                            useSiteInfo:=useSiteInfo)
            results.Free()

            Return result
        End Function

        ''' <summary>
        ''' Given instance method candidates gone through applicability analysis,
        ''' figure out if we should consider extension methods, if any.
        ''' </summary>
        Private Shared Function ShouldConsiderExtensionMethods(
            candidates As ArrayBuilder(Of CandidateAnalysisResult)
        ) As Boolean

            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim candidate = candidates(i)

                Debug.Assert(Not candidate.Candidate.IsExtensionMethod)

                If candidate.IgnoreExtensionMethods Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function ResolveOverloading(
            methodOrPropertyGroup As BoundMethodOrPropertyGroup,
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            delegateReturnType As TypeSymbol,
            lateBindingIsAllowed As Boolean,
            binder As Binder,
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            callerInfoOpt As SyntaxNode,
            forceExpandedForm As Boolean,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As OverloadResolutionResult

            Debug.Assert(argumentNames.IsDefault OrElse argumentNames.Length = arguments.Length)

            Dim applicableCandidates As Integer
            Dim resolutionIsLateBound As Boolean = False
            Dim narrowingCandidatesRemainInTheSet As Boolean = False
            Dim applicableNarrowingCandidates As Integer = 0

            'TODO: Where does this fit?
            'Semantics::ResolveOverloading
            '// See if type inference failed for all candidates and it failed from
            '// Object. For this scenario, in non-strict mode, treat the call
            '// as latebound.

            '§11.8.1 Overloaded Method Resolution

            '2.	Next, eliminate all members from the set that are inaccessible or not applicable to the argument list.
            ' Note, similar to Dev10 compiler this process will eliminate candidates requiring narrowing conversions
            ' if strict semantics is used, exception are candidates that require narrowing only from numeric constants.
            applicableCandidates = EliminateNotApplicableToArguments(methodOrPropertyGroup, candidates, arguments, argumentNames, binder,
                                                                     applicableNarrowingCandidates, asyncLambdaSubToFunctionMismatch,
                                                                     callerInfoOpt,
                                                                     forceExpandedForm,
                                                                     useSiteInfo)
            If applicableCandidates < 2 Then
                narrowingCandidatesRemainInTheSet = (applicableNarrowingCandidates > 0)
                GoTo ResolutionComplete
            End If

            ' §11.8.1 Overloaded Method Resolution.
            ' 7.8.	If one or more arguments are AddressOf or lambda expressions, and all of the corresponding
            '         delegate types in M match exactly, but not all do in N, eliminate N from the set.
            ' 7.9.	If one or more arguments are AddressOf or lambda expressions, and all of the corresponding
            '         delegate types in M are widening conversions, but not all are in N, eliminate N from the set.
            '
            ' The spec implies that this rule is applied to the set of most applicable candidate as one of the tie breaking rules.
            ' However, doing it there wouldn't have any effect because all candidates in the set of most applicable candidates
            ' are equally applicable, therefore, have the same types for corresponding parameters. Thus all the candidates
            ' have exactly the same delegate relaxation level and none would be eliminated.
            ' Dev10 applies this rule much earlier, even before eliminating narrowing candidates, and it does it across the board.
            ' I am going to do the same.
            applicableCandidates = ShadowBasedOnDelegateRelaxation(candidates, applicableNarrowingCandidates)
            If applicableCandidates < 2 Then
                narrowingCandidatesRemainInTheSet = (applicableNarrowingCandidates > 0)
                GoTo ResolutionComplete
            End If

            ' §11.8.1 Overloaded Method Resolution.
            '7.7.	If M and N both required type inference to produce type arguments, and M did not
            '       require determining the dominant type for any of its type arguments (i.e. each the
            '       type arguments inferred to a single type), but N did, eliminate N from the set.
            ' Despite what the spec says, this rule is applied after shadowing based on delegate relaxation
            ' level, however it needs other tie breaking rules applied to equally applicable candidates prior
            ' to figuring out the minimal inference level to use as the filter.
            ShadowBasedOnInferenceLevel(candidates, arguments, Not argumentNames.IsDefault, delegateReturnType, binder,
                                        applicableCandidates, applicableNarrowingCandidates, useSiteInfo)
            If applicableCandidates < 2 Then
                narrowingCandidatesRemainInTheSet = (applicableNarrowingCandidates > 0)
                GoTo ResolutionComplete
            End If

            '3.	Next, eliminate all members from the set that require narrowing conversions
            '   to be applicable to the argument list, except for the case where the argument
            '   expression type is Object.
            '4.	Next, eliminate all remaining members from the set that require narrowing coercions
            '   to be applicable to the argument list. If the set is empty, the type containing the
            '   method group is not an interface, and strict semantics are not being used, the
            '   invocation target expression is reclassified as a late-bound method access.
            '   Otherwise, the normal rules apply.
            If applicableCandidates = applicableNarrowingCandidates Then

                ' All remaining candidates are narrowing, deal with them.
                narrowingCandidatesRemainInTheSet = True
                applicableCandidates = AnalyzeNarrowingCandidates(candidates, arguments, delegateReturnType,
                                                                  lateBindingIsAllowed AndAlso binder.OptionStrict <> OptionStrict.On, binder,
                                                                  resolutionIsLateBound,
                                                                  useSiteInfo)
            Else
                If applicableNarrowingCandidates > 0 Then
                    Debug.Assert(applicableNarrowingCandidates < applicableCandidates)

                    applicableCandidates = EliminateNarrowingCandidates(candidates)
                    Debug.Assert(applicableCandidates > 0)
                    If applicableCandidates < 2 Then
                        GoTo ResolutionComplete
                    End If
                End If

                '5.	Next, if any instance methods remain in the set,
                '   eliminate all extension methods from the set.
                ' !!! I don't think we need to do this explicitly. ResolveMethodOverloading doesn't add
                ' !!! extension methods in the list if we need to remove them here.
                'applicableCandidates = EliminateExtensionMethodsInPresenceOfInstanceMethods(candidates)
                'If applicableCandidates < 2 Then
                '    GoTo ResolutionComplete
                'End If

                '6.	Next, if, given any two members of the set, M and N, M is more applicable than N
                '   to the argument list, eliminate N from the set. If more than one member remains
                '   in the set and the remaining members are not equally applicable to the argument
                '   list, a compile-time error results.
                '7.	Otherwise, given any two members of the set, M and N, apply the following tie-breaking rules, in order.
                applicableCandidates = EliminateLessApplicableToTheArguments(candidates, arguments, delegateReturnType,
                                                                             False, ' appliedTieBreakingRules
                                                                             binder, useSiteInfo)
            End If

ResolutionComplete:
            If Not resolutionIsLateBound AndAlso applicableCandidates = 0 Then
                Return ReportOverloadResolutionFailedOrLateBound(candidates, applicableCandidates, lateBindingIsAllowed AndAlso binder.OptionStrict <> OptionStrict.On, asyncLambdaSubToFunctionMismatch)
            End If

            Return New OverloadResolutionResult(candidates.ToImmutable(), resolutionIsLateBound, narrowingCandidatesRemainInTheSet, asyncLambdaSubToFunctionMismatch)
        End Function

        Private Shared Function EliminateNarrowingCandidates(
            candidates As ArrayBuilder(Of CandidateAnalysisResult)
        ) As Integer

            Dim applicableCandidates As Integer = 0

            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim current As CandidateAnalysisResult = candidates(i)

                If current.State = CandidateAnalysisResultState.Applicable Then
                    If current.RequiresNarrowingConversion Then
                        current.State = CandidateAnalysisResultState.RequiresNarrowing
                        candidates(i) = current
                    Else
                        applicableCandidates += 1
                    End If
                End If
            Next

            Return applicableCandidates
        End Function

        ''' <summary>
        ''' §11.8.1 Overloaded Method Resolution
        '''      6.	Next, if, given any two members of the set, M and N, M is more applicable than N
        '''         to the argument list, eliminate N from the set. If more than one member remains
        '''         in the set and the remaining members are not equally applicable to the argument
        '''         list, a compile-time error results.
        '''      7.	Otherwise, given any two members of the set, M and N, apply the following tie-breaking rules, in order.
        '''
        ''' Returns amount of applicable candidates left.
        '''
        ''' Note that less applicable candidates are going to be eliminated if and only if there are most applicable
        ''' candidates.
        ''' </summary>
        Private Shared Function EliminateLessApplicableToTheArguments(
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            delegateReturnType As TypeSymbol,
            appliedTieBreakingRules As Boolean,
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
            Optional mostApplicableMustNarrowOnlyFromNumericConstants As Boolean = False
        ) As Integer
            Dim applicableCandidates As Integer

            Dim indexesOfEqualMostApplicableCandidates As ArrayBuilder(Of Integer) = ArrayBuilder(Of Integer).GetInstance()

            If FastFindMostApplicableCandidates(candidates, arguments, indexesOfEqualMostApplicableCandidates, binder, useSiteInfo) AndAlso
               (mostApplicableMustNarrowOnlyFromNumericConstants = False OrElse
                candidates(indexesOfEqualMostApplicableCandidates(0)).RequiresNarrowingNotFromNumericConstant = False OrElse
                indexesOfEqualMostApplicableCandidates.Count = CountApplicableCandidates(candidates)) Then

                ' We have most applicable candidates.

                ' Mark those that lost applicability comparison.
                ' Applicable candidates with indexes before the first value in indexesOfEqualMostApplicableCandidates,
                ' after the last value in indexesOfEqualMostApplicableCandidates and in between consecutive values in
                ' indexesOfEqualMostApplicableCandidates are less applicable.

                Debug.Assert(indexesOfEqualMostApplicableCandidates.Count > 0)
                Dim nextMostApplicable As Integer = 0 ' and index into indexesOfEqualMostApplicableCandidates
                Dim indexOfNextMostApplicable As Integer = indexesOfEqualMostApplicableCandidates(nextMostApplicable)

                For i As Integer = 0 To candidates.Count - 1 Step 1
                    If i = indexOfNextMostApplicable Then
                        nextMostApplicable += 1

                        If nextMostApplicable < indexesOfEqualMostApplicableCandidates.Count Then
                            indexOfNextMostApplicable = indexesOfEqualMostApplicableCandidates(nextMostApplicable)
                        Else
                            indexOfNextMostApplicable = candidates.Count
                        End If

                        Continue For
                    End If

                    Dim contender As CandidateAnalysisResult = candidates(i)

                    If contender.State <> CandidateAnalysisResultState.Applicable Then
                        Continue For
                    End If

                    contender.State = CandidateAnalysisResultState.LessApplicable
                    candidates(i) = contender
                Next

                ' Apply tie-breaking rules
                If Not appliedTieBreakingRules Then
                    applicableCandidates = ApplyTieBreakingRules(candidates, indexesOfEqualMostApplicableCandidates, arguments, delegateReturnType, binder, useSiteInfo)
                Else
                    applicableCandidates = indexesOfEqualMostApplicableCandidates.Count
                End If

            ElseIf Not appliedTieBreakingRules Then
                ' Overload resolution failed, we couldn't find most applicable candidates.
                ' We still need to apply shadowing rules to the sets of equally applicable candidates,
                ' this will provide better error reporting experience. As we are doing this, we will redo
                ' applicability comparisons that we've done earlier in FastFindMostApplicableCandidates, but we are willing to
                ' pay the price for erroneous code.
                applicableCandidates = ApplyTieBreakingRulesToEquallyApplicableCandidates(candidates, arguments, delegateReturnType, binder, useSiteInfo)

            Else
                applicableCandidates = CountApplicableCandidates(candidates)
            End If

            indexesOfEqualMostApplicableCandidates.Free()
            Return applicableCandidates
        End Function

        Private Shared Function CountApplicableCandidates(candidates As ArrayBuilder(Of CandidateAnalysisResult)) As Integer
            Dim applicableCandidates As Integer = 0

            For i As Integer = 0 To candidates.Count - 1 Step 1
                If candidates(i).State <> CandidateAnalysisResultState.Applicable Then
                    Continue For
                End If

                applicableCandidates += 1
            Next

            Return applicableCandidates
        End Function

        ''' <summary>
        ''' Returns amount of applicable candidates left.
        ''' </summary>
        Private Shared Function ApplyTieBreakingRulesToEquallyApplicableCandidates(
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            delegateReturnType As TypeSymbol,
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Integer

            ' First, let's break all remaining candidates into buckets of equally applicable candidates
            Dim buckets = GroupEquallyApplicableCandidates(candidates, arguments, binder)

            Debug.Assert(buckets.Count > 0)

            Dim applicableCandidates As Integer = 0

            ' Apply tie-breaking rules
            For i As Integer = 0 To buckets.Count - 1 Step 1
                applicableCandidates += ApplyTieBreakingRules(candidates, buckets(i), arguments, delegateReturnType, binder, useSiteInfo)
            Next

            ' Release memory we no longer need.
            For i As Integer = 0 To buckets.Count - 1 Step 1
                buckets(i).Free()
            Next

            buckets.Free()

            Return applicableCandidates
        End Function

        ''' <summary>
        ''' Returns True if there are most applicable candidates.
        '''
        ''' indexesOfMostApplicableCandidates will contain indexes of equally applicable candidates, which are most applicable
        ''' by comparison to the other (non-equal) candidates. The indexes will be in ascending order.
        ''' </summary>
        Private Shared Function FastFindMostApplicableCandidates(
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            indexesOfMostApplicableCandidates As ArrayBuilder(Of Integer),
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Boolean
            Dim mightBeTheMostApplicableIndex As Integer = -1
            Dim mightBeTheMostApplicable As CandidateAnalysisResult = Nothing

            indexesOfMostApplicableCandidates.Clear()

            ' Use linear complexity algorithm to find the first most applicable candidate.
            ' We are saying "the first" because there could be a number of candidates equally applicable
            ' by comparison to the one we find, their indexes are collected in indexesOfMostApplicableCandidates.
            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim contender As CandidateAnalysisResult = candidates(i)

                If contender.State <> CandidateAnalysisResultState.Applicable Then
                    Continue For
                End If

                If mightBeTheMostApplicableIndex = -1 Then
                    mightBeTheMostApplicableIndex = i
                    mightBeTheMostApplicable = contender
                    indexesOfMostApplicableCandidates.Add(i)
                Else
                    Dim cmp As ApplicabilityComparisonResult = CompareApplicabilityToTheArguments(mightBeTheMostApplicable, contender, arguments, binder, useSiteInfo)

                    If cmp = ApplicabilityComparisonResult.RightIsMoreApplicable Then
                        mightBeTheMostApplicableIndex = i
                        mightBeTheMostApplicable = contender
                        indexesOfMostApplicableCandidates.Clear()
                        indexesOfMostApplicableCandidates.Add(i)

                    ElseIf cmp = ApplicabilityComparisonResult.Undefined Then
                        mightBeTheMostApplicableIndex = -1
                        indexesOfMostApplicableCandidates.Clear()

                    ElseIf cmp = ApplicabilityComparisonResult.EquallyApplicable Then
                        indexesOfMostApplicableCandidates.Add(i)
                    Else
                        Debug.Assert(cmp = ApplicabilityComparisonResult.LeftIsMoreApplicable)
                    End If
                End If
            Next

            For i As Integer = 0 To mightBeTheMostApplicableIndex - 1 Step 1
                Dim contender As CandidateAnalysisResult = candidates(i)

                If contender.State <> CandidateAnalysisResultState.Applicable Then
                    Continue For
                End If

                Dim cmp As ApplicabilityComparisonResult = CompareApplicabilityToTheArguments(mightBeTheMostApplicable, contender, arguments, binder, useSiteInfo)

                If cmp = ApplicabilityComparisonResult.RightIsMoreApplicable OrElse
                   cmp = ApplicabilityComparisonResult.Undefined OrElse
                   cmp = ApplicabilityComparisonResult.EquallyApplicable Then
                    ' We do this for equal applicability too because this contender was dropped during the first loop, so,
                    ' if we continue, the mightBeTheMostApplicable candidate will be definitely dropped too.
                    mightBeTheMostApplicableIndex = -1
                    Exit For
                Else
                    Debug.Assert(cmp = ApplicabilityComparisonResult.LeftIsMoreApplicable)
                End If
            Next

            Return (mightBeTheMostApplicableIndex > -1)
        End Function

        ''' <summary>
        ''' §11.8.1 Overloaded Method Resolution
        '''      7.	Otherwise, given any two members of the set, M and N, apply the following tie-breaking rules, in order.
        ''' </summary>
        Private Shared Function ApplyTieBreakingRules(
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            bucket As ArrayBuilder(Of Integer),
            arguments As ImmutableArray(Of BoundExpression),
            delegateReturnType As TypeSymbol,
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Integer
            Dim leftWins As Boolean
            Dim rightWins As Boolean
            Dim applicableCandidates As Integer = bucket.Count

            For i = 0 To bucket.Count - 1 Step 1
                Dim left = candidates(bucket(i))

                If left.State <> CandidateAnalysisResultState.Applicable Then
                    Continue For
                End If

                For j = i + 1 To bucket.Count - 1 Step 1
                    Dim right = candidates(bucket(j))

                    If right.State <> CandidateAnalysisResultState.Applicable Then
                        Continue For
                    End If

                    If ShadowBasedOnTieBreakingRules(left, right, arguments, delegateReturnType, leftWins, rightWins, binder, useSiteInfo) Then
                        Debug.Assert(Not (leftWins AndAlso rightWins))

                        If leftWins Then
                            right.State = CandidateAnalysisResultState.Shadowed
                            candidates(bucket(j)) = right
                            applicableCandidates -= 1
                        Else
                            Debug.Assert(rightWins)
                            left.State = CandidateAnalysisResultState.Shadowed
                            candidates(bucket(i)) = left
                            applicableCandidates -= 1
                            Exit For
                        End If
                    End If
                Next
            Next

            Debug.Assert(applicableCandidates >= 0)
            Return applicableCandidates
        End Function

        ''' <summary>
        ''' §11.8.1 Overloaded Method Resolution
        '''      7.	Otherwise, given any two members of the set, M and N, apply the following tie-breaking rules, in order.
        ''' </summary>
        Private Shared Function ShadowBasedOnTieBreakingRules(
            left As CandidateAnalysisResult,
            right As CandidateAnalysisResult,
            arguments As ImmutableArray(Of BoundExpression),
            delegateReturnType As TypeSymbol,
            ByRef leftWins As Boolean,
            ByRef rightWins As Boolean,
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Boolean

            ' Let's apply various shadowing and tie-breaking rules
            ' from section 7 of §11.8.1 Overloaded Method Resolution.

            leftWins = False
            rightWins = False

            '•	If M has fewer parameters from an expanded paramarray than N, eliminate N from the set.
            If ShadowBasedOnParamArrayUsage(left, right, leftWins, rightWins) Then
                Return True
            End If

            '7.1.	If M is defined in a more derived type than N, eliminate N from the set.
            '       This rule also applies to the types that extension methods are defined on.
            '7.2.	If M and N are extension methods and the target type of M is a class or
            '       structure and the target type of N is an interface, eliminate N from the set.
            If ShadowBasedOnReceiverType(left, right, leftWins, rightWins, useSiteInfo) Then
                Return True  ' I believe we can get here only in presence of named arguments and optional parameters. Otherwise, CombineCandidates takes care of this shadowing.
            End If

            '7.3.	If M and N are extension methods and the target type of M has fewer type
            '       parameters than the target type of N, eliminate N from the set.
            '       !!! Note that spec talks about "fewer type parameters", but it is not really about count.
            '       !!! It is about one refers to a type parameter and the other one doesn't.
            If ShadowBasedOnExtensionMethodTargetTypeGenericity(left, right, leftWins, rightWins) Then
                Return True ' I believe we can get here only in presence of named arguments and optional parameters. Otherwise, CombineCandidates takes care of this shadowing.
            End If

            '7.4.	If M is less generic than N, eliminate N from the set.
            If ShadowBasedOnGenericity(left, right, leftWins, rightWins, arguments, binder) Then
                Return True
            End If

            '7.5.	If M is not an extension method and N is, eliminate N from the set.
            '7.6.	If M and N are extension methods and M was found before N, eliminate N from the set.
            If ShadowBasedOnExtensionVsInstanceAndPrecedence(left, right, leftWins, rightWins) Then
                Return True
            End If

            '7.7.	If M and N both required type inference to produce type arguments, and M did not
            '       require determining the dominant type for any of its type arguments (i.e. each the
            '       type arguments inferred to a single type), but N did, eliminate N from the set.
            ' The spec is incorrect, this shadowing doesn't belong here, it is applied across the board
            ' after these tie breaking rules. For more information, see comment in ResolveOverloading.

            '7.8.	If one or more arguments are AddressOf or lambda expressions, and all of the corresponding delegate types in M match exactly, but not all do in N, eliminate N from the set.
            '7.9.	If one or more arguments are AddressOf or lambda expressions, and all of the corresponding delegate types in M are widening conversions, but not all are in N, eliminate N from the set.
            ' The spec is incorrect, this shadowing doesn't belong here, it is applied much earlier.
            ' For more information, see comment in ResolveOverloading.

            ' 7.9.	If M did not use any optional parameter defaults in place of explicit
            '       arguments, but N did, then eliminate N from the set.
            '
            ' !!!WARNING!!! The index (7.9) is based on "VB11 spec [draft 3]" version of documentation rather
            ' than Dev10 documentation.
            If ShadowBasedOnOptionalParametersDefaultsUsed(left, right, leftWins, rightWins) Then
                Return True
            End If

            '7.10.	If the overload resolution is being done to resolve the target of a delegate-creation expression from an AddressOf expression and M is a function, while N is a subroutine, eliminate N from the set.
            If ShadowBasedOnSubOrFunction(left, right, delegateReturnType, leftWins, rightWins) Then
                Return True
            End If

            ' 7.10.	Before type arguments have been substituted, if M has greater depth of
            '       genericity (Section 11.8.1.3) than N, then eliminate N from the set.
            '
            ' !!!WARNING!!! The index (7.10) is based on "VB11 spec [draft 3]" version of documentation
            ' rather than Dev10 documentation.
            '
            ' NOTE: Dev11 puts this analysis in a second phase with the first phase
            '       performing analysis of { $11.8.1:6 + 7.9/7.10/7.11/7.8 }, see comments in
            '       OverloadResolution.cpp: bool Semantics::AreProceduresEquallySpecific(...)
            '
            '       Placing this analysis here seems to be more natural than
            '       matching Dev11 implementation
            If ShadowBasedOnDepthOfGenericity(left, right, leftWins, rightWins, arguments, binder) Then
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        '''    7.10.	If the overload resolution is being done to resolve the target of a
        '''             delegate-creation expression from an AddressOf expression and M is a
        '''             function, while N is a subroutine, eliminate N from the set.
        ''' </summary>
        Private Shared Function ShadowBasedOnSubOrFunction(
            left As CandidateAnalysisResult, right As CandidateAnalysisResult,
            delegateReturnType As TypeSymbol,
            ByRef leftWins As Boolean, ByRef rightWins As Boolean
        ) As Boolean

            ' !!! Actually, the spec isn't accurate here. If the target delegate is a Sub, we prefer a Sub. !!!
            ' !!! If the target delegate is a Function, we prefer a Function.                               !!!

            If delegateReturnType Is Nothing Then
                Return False
            End If

            Dim leftReturnsVoid As Boolean = left.Candidate.ReturnType.IsVoidType()
            Dim rightReturnsVoid As Boolean = right.Candidate.ReturnType.IsVoidType()

            If leftReturnsVoid = rightReturnsVoid Then
                Return False
            End If

            If delegateReturnType.IsVoidType() = leftReturnsVoid Then
                leftWins = True
                Return True
            End If

            Debug.Assert(delegateReturnType.IsVoidType() = rightReturnsVoid)
            rightWins = True
            Return True
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        ''' 7.8.	If one or more arguments are AddressOf or lambda expressions, and all of the corresponding
        '''         delegate types in M match exactly, but not all do in N, eliminate N from the set.
        ''' 7.9.	If one or more arguments are AddressOf or lambda expressions, and all of the corresponding
        '''         delegate types in M are widening conversions, but not all are in N, eliminate N from the set.
        ''' </summary>
        Private Shared Function ShadowBasedOnDelegateRelaxation(
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            ByRef applicableNarrowingCandidates As Integer
        ) As Integer

            ' Find the minimal MaxDelegateRelaxationLevel
            Dim minMaxRelaxation As ConversionKind = ConversionKind.DelegateRelaxationLevelInvalid

            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim current As CandidateAnalysisResult = candidates(i)

                If current.State = CandidateAnalysisResultState.Applicable Then
                    Dim relaxation As ConversionKind = current.MaxDelegateRelaxationLevel

                    If relaxation < minMaxRelaxation Then
                        minMaxRelaxation = relaxation
                    End If
                End If
            Next

            ' Now eliminate all candidates with relaxation level bigger than the minimal.
            Dim applicableCandidates As Integer = 0
            applicableNarrowingCandidates = 0

            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim current As CandidateAnalysisResult = candidates(i)

                If current.State <> CandidateAnalysisResultState.Applicable Then
                    Continue For
                End If

                Dim relaxation As ConversionKind = current.MaxDelegateRelaxationLevel

                If relaxation > minMaxRelaxation Then
                    current.State = CandidateAnalysisResultState.Shadowed
                    candidates(i) = current
                Else
                    applicableCandidates += 1

                    If current.RequiresNarrowingConversion Then
                        applicableNarrowingCandidates += 1
                    End If
                End If
            Next

            Return applicableCandidates
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        ''' 7.9.	If M did not use any optional parameter defaults in place of explicit
        '''         arguments, but N did, then eliminate N from the set.
        '''
        ''' !!!WARNING!!! The index (7.9) is based on "VB11 spec [draft 3]" version of documentation rather
        ''' than Dev10 documentation.
        ''' TODO: Update indexes of other overload method resolution rules
        ''' </summary>
        Private Shared Function ShadowBasedOnOptionalParametersDefaultsUsed(
            left As CandidateAnalysisResult, right As CandidateAnalysisResult,
            ByRef leftWins As Boolean, ByRef rightWins As Boolean
        ) As Boolean

            Dim leftUsesOptionalParameterDefaults As Boolean = left.UsedOptionalParameterDefaultValue

            If leftUsesOptionalParameterDefaults = right.UsedOptionalParameterDefaultValue Then
                Return False ' No winner
            End If

            If Not leftUsesOptionalParameterDefaults Then
                leftWins = True
            Else
                rightWins = True
            End If
            Return True
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        ''' 7.7.  If M and N both required type inference to produce type arguments, and M did not
        '''       require determining the dominant type for any of its type arguments (i.e. each the
        '''       type arguments inferred to a single type), but N did, eliminate N from the set.
        ''' </summary>
        Private Shared Sub ShadowBasedOnInferenceLevel(
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            haveNamedArguments As Boolean,
            delegateReturnType As TypeSymbol,
            binder As Binder,
            ByRef applicableCandidates As Integer,
            ByRef applicableNarrowingCandidates As Integer,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )
            Debug.Assert(Not haveNamedArguments OrElse Not candidates(0).Candidate.IsOperator)

            ' See if there are candidates with different InferenceLevel
            Dim haveDifferentInferenceLevel As Boolean = False
            Dim theOnlyInferenceLevel As TypeArgumentInference.InferenceLevel = CType(Byte.MaxValue, TypeArgumentInference.InferenceLevel)

            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim current As CandidateAnalysisResult = candidates(i)

                If current.State = CandidateAnalysisResultState.Applicable Then
                    Dim inferenceLevel As TypeArgumentInference.InferenceLevel = current.InferenceLevel

                    If theOnlyInferenceLevel = Byte.MaxValue Then
                        theOnlyInferenceLevel = inferenceLevel

                    ElseIf inferenceLevel <> theOnlyInferenceLevel Then
                        haveDifferentInferenceLevel = True
                        Exit For
                    End If
                End If
            Next

            If Not haveDifferentInferenceLevel Then
                ' Nothing to do.
                Return
            End If

            ' Native compiler used to have a bug where CombineCandidates was applying shadowing in presence of named arguments
            ' before figuring out whether candidates are applicable. We fixed that. However, in cases when candidates were applicable
            ' after all, that shadowing had impact on the shadowing based on the inference level by affecting minimal inference level.
            ' To compensate, we will perform the CombineCandidates-style shadowing here. Note that we cannot simply call
            ' ApplyTieBreakingRulesToEquallyApplicableCandidates to do this because shadowing performed by CombineCandidates is more
            ' constrained.
            If haveNamedArguments Then
                Debug.Assert(Not candidates(0).Candidate.IsOperator)

                Dim indexesOfApplicableCandidates = ArrayBuilder(Of Integer).GetInstance(applicableCandidates)

                For i As Integer = 0 To candidates.Count - 1 Step 1
                    If candidates(i).State = CandidateAnalysisResultState.Applicable Then
                        indexesOfApplicableCandidates.Add(i)
                    End If
                Next

                Debug.Assert(indexesOfApplicableCandidates.Count = applicableCandidates)

                ' Sort indexes by inference level
                indexesOfApplicableCandidates.Sort(New InferenceLevelComparer(candidates))

#If DEBUG Then
                Dim level As TypeArgumentInference.InferenceLevel = TypeArgumentInference.InferenceLevel.None
                For Each index As Integer In indexesOfApplicableCandidates
                    Debug.Assert(level <= candidates(index).InferenceLevel)
                    level = candidates(index).InferenceLevel
                Next
#End If

                ' In order of sorted indexes, apply constrained shadowing rules looking for the first one survived.
                ' This will be sufficient to calculate "correct" minimal inference level. We don't have to apply
                ' shadowing to each pair of candidates.
                For i As Integer = 0 To indexesOfApplicableCandidates.Count - 2
                    Dim left As CandidateAnalysisResult = candidates(indexesOfApplicableCandidates(i))

                    If left.State <> CandidateAnalysisResultState.Applicable Then
                        Continue For
                    End If

                    For j As Integer = i + 1 To indexesOfApplicableCandidates.Count - 1
                        Dim right As CandidateAnalysisResult = candidates(indexesOfApplicableCandidates(j))

                        If right.State <> CandidateAnalysisResultState.Applicable Then
                            Continue For
                        End If

                        ' Shadowing is applied only to candidates that have the same types for corresponding parameters
                        ' in virtual signatures
                        Dim equallyApplicable As Boolean = True
                        For k = 0 To arguments.Length - 1 Step 1

                            Dim leftParamType As TypeSymbol = GetParameterTypeFromVirtualSignature(left, left.ArgsToParamsOpt(k))
                            Dim rightParamType As TypeSymbol = GetParameterTypeFromVirtualSignature(right, right.ArgsToParamsOpt(k))

                            If Not leftParamType.IsSameTypeIgnoringAll(rightParamType) Then
                                ' Signatures are different, shadowing rules do not apply
                                equallyApplicable = False
                                Exit For
                            End If
                        Next

                        If Not equallyApplicable Then
                            Continue For
                        End If

                        Dim signatureMatch As Boolean = True

                        ' Compare complete signature, with no regard to arguments
                        If left.Candidate.ParameterCount <> right.Candidate.ParameterCount Then
                            signatureMatch = False
                        Else
                            For k As Integer = 0 To left.Candidate.ParameterCount - 1 Step 1

                                Dim leftType As TypeSymbol = left.Candidate.Parameters(k).Type
                                Dim rightType As TypeSymbol = right.Candidate.Parameters(k).Type

                                If Not leftType.IsSameTypeIgnoringAll(rightType) Then
                                    signatureMatch = False
                                    Exit For
                                End If
                            Next
                        End If

                        Dim leftWins As Boolean = False
                        Dim rightWins As Boolean = False

                        If (Not signatureMatch AndAlso ShadowBasedOnParamArrayUsage(left, right, leftWins, rightWins)) OrElse
                           ShadowBasedOnReceiverType(left, right, leftWins, rightWins, useSiteInfo) OrElse
                           ShadowBasedOnExtensionMethodTargetTypeGenericity(left, right, leftWins, rightWins) Then
                            Debug.Assert(leftWins Xor rightWins)
                            If leftWins Then
                                right.State = CandidateAnalysisResultState.Shadowed
                                candidates(indexesOfApplicableCandidates(j)) = right
                            ElseIf rightWins Then
                                left.State = CandidateAnalysisResultState.Shadowed
                                candidates(indexesOfApplicableCandidates(i)) = left
                                Exit For ' advance to the next left
                            End If
                        End If
                    Next

                    If left.State = CandidateAnalysisResultState.Applicable Then
                        ' left has survived
                        Exit For
                    End If
                Next
            End If

            ' Find the minimal InferenceLevel
            Dim minInferenceLevel = TypeArgumentInference.InferenceLevel.Invalid
            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim current As CandidateAnalysisResult = candidates(i)

                If current.State = CandidateAnalysisResultState.Applicable Then
                    Dim inferenceLevel As TypeArgumentInference.InferenceLevel = current.InferenceLevel

                    If inferenceLevel < minInferenceLevel Then
                        minInferenceLevel = inferenceLevel
                    End If
                End If
            Next

            ' Now eliminate all candidates with inference level bigger than the minimal.
            applicableCandidates = 0
            applicableNarrowingCandidates = 0

            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim current As CandidateAnalysisResult = candidates(i)

                If current.State <> CandidateAnalysisResultState.Applicable Then
                    Continue For
                End If

                Dim inferenceLevel As TypeArgumentInference.InferenceLevel = current.InferenceLevel

                If inferenceLevel > minInferenceLevel Then
                    current.State = CandidateAnalysisResultState.Shadowed
                    candidates(i) = current
                Else
                    applicableCandidates += 1

                    If current.RequiresNarrowingConversion Then
                        applicableNarrowingCandidates += 1
                    End If
                End If
            Next

            ' Done.
        End Sub

        Private Class InferenceLevelComparer
            Implements IComparer(Of Integer)

            Private ReadOnly _candidates As ArrayBuilder(Of CandidateAnalysisResult)

            Public Sub New(candidates As ArrayBuilder(Of CandidateAnalysisResult))
                _candidates = candidates
            End Sub

            Public Function Compare(indexX As Integer, indexY As Integer) As Integer Implements IComparer(Of Integer).Compare
                Return CInt(_candidates(indexX).InferenceLevel).CompareTo(_candidates(indexY).InferenceLevel)
            End Function
        End Class

        ''' <summary>
        ''' §11.8.1.1 Applicability
        ''' </summary>
        Private Shared Function CompareApplicabilityToTheArguments(
            ByRef left As CandidateAnalysisResult,
            ByRef right As CandidateAnalysisResult,
            arguments As ImmutableArray(Of BoundExpression),
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As ApplicabilityComparisonResult

            ' §11.8.1.1 Applicability
            'A member M is considered more applicable than N if their signatures are different and at least one
            'parameter type in M is more applicable than a parameter type in N, and no parameter type in N is more
            'applicable than a parameter type in M.

            Dim equallyApplicable As Boolean = True
            Dim leftHasMoreApplicableParameterType As Boolean = False
            Dim rightHasMoreApplicableParameterType As Boolean = False

            Dim leftParamIndex As Integer = 0
            Dim rightParamIndex As Integer = 0

            For i = 0 To arguments.Length - 1 Step 1

                Dim leftParamType As TypeSymbol

                Debug.Assert(left.ArgsToParamsOpt.IsDefault = right.ArgsToParamsOpt.IsDefault)

                If left.ArgsToParamsOpt.IsDefault Then
                    leftParamType = GetParameterTypeFromVirtualSignature(left, leftParamIndex)
                    AdvanceParameterInVirtualSignature(left, leftParamIndex)
                Else
                    leftParamType = GetParameterTypeFromVirtualSignature(left, left.ArgsToParamsOpt(i))
                End If

                Dim rightParamType As TypeSymbol

                If right.ArgsToParamsOpt.IsDefault Then
                    rightParamType = GetParameterTypeFromVirtualSignature(right, rightParamIndex)
                    AdvanceParameterInVirtualSignature(right, rightParamIndex)
                Else
                    rightParamType = GetParameterTypeFromVirtualSignature(right, right.ArgsToParamsOpt(i))
                End If

                ' Parameters matching omitted arguments do not participate.
                If arguments(i).Kind = BoundKind.OmittedArgument Then
                    Continue For
                End If

                Dim cmp = CompareParameterTypeApplicability(leftParamType, rightParamType, arguments(i), binder, useSiteInfo)

                If cmp = ApplicabilityComparisonResult.LeftIsMoreApplicable Then
                    leftHasMoreApplicableParameterType = True

                    If rightHasMoreApplicableParameterType Then
                        Return ApplicabilityComparisonResult.Undefined ' Neither is more applicable
                    End If

                    equallyApplicable = False

                ElseIf cmp = ApplicabilityComparisonResult.RightIsMoreApplicable Then
                    rightHasMoreApplicableParameterType = True

                    If leftHasMoreApplicableParameterType Then
                        Return ApplicabilityComparisonResult.Undefined ' Neither is more applicable
                    End If

                    equallyApplicable = False

                ElseIf cmp = ApplicabilityComparisonResult.Undefined Then
                    equallyApplicable = False

                Else
                    Debug.Assert(cmp = ApplicabilityComparisonResult.EquallyApplicable)
                End If
            Next

            Debug.Assert(Not (leftHasMoreApplicableParameterType AndAlso rightHasMoreApplicableParameterType))

            If leftHasMoreApplicableParameterType Then
                Return ApplicabilityComparisonResult.LeftIsMoreApplicable
            End If

            If rightHasMoreApplicableParameterType Then
                Return ApplicabilityComparisonResult.RightIsMoreApplicable
            End If

            Return If(equallyApplicable, ApplicabilityComparisonResult.EquallyApplicable, ApplicabilityComparisonResult.Undefined)
        End Function

        Private Enum ApplicabilityComparisonResult
            Undefined
            EquallyApplicable
            LeftIsMoreApplicable
            RightIsMoreApplicable
        End Enum

        ''' <summary>
        ''' §11.8.1.1 Applicability
        ''' </summary>
        Private Shared Function CompareParameterTypeApplicability(
            left As TypeSymbol,
            right As TypeSymbol,
            argument As BoundExpression,
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As ApplicabilityComparisonResult
            Debug.Assert(argument Is Nothing OrElse argument.Kind <> BoundKind.OmittedArgument)

            ' §11.8.1.1 Applicability
            'Given a pair of parameters Mj and Nj that matches an argument Aj,
            'the type of Mj is considered more applicable than the type of Nj if one of the following conditions is true:

            Dim leftToRightConversion = Conversions.ClassifyConversion(left, right, useSiteInfo)

            '1.	Mj and Nj have identical types, or
            ' !!! Does this rule make sense? Not implementing it for now.
            If Conversions.IsIdentityConversion(leftToRightConversion.Key) Then
                Return ApplicabilityComparisonResult.EquallyApplicable
            End If

            '2.	There exists a widening conversion from the type of Mj to the type Nj, or
            If Conversions.IsWideningConversion(leftToRightConversion.Key) Then

                ' !!! For user defined conversions that widen in both directions there is a tie-breaking rule
                ' !!! not mentioned in the spec. The type that matches argument's type is more applicable.
                ' !!! Otherwise neither is more applicable.
                If Conversions.IsWideningConversion(Conversions.ClassifyConversion(right, left, useSiteInfo).Key) Then
                    GoTo BreakTheTie
                End If

                ' !!! Spec makes it look like rule #3 is a separate rule applied after the second, but this isn't the case
                ' !!! because enumerated type widens to its underlying type, however, if argument is a zero literal,
                ' !!! underlying type should win.
                ' !!! Also, based on Dev10 implementation, Mj doesn't have to be a numeric type, it is enough if it is not
                ' !!! an enumerated type.
                '3.	Aj is the literal 0, Mj is a numeric type and Nj is an enumerated type, or
                If argument IsNot Nothing AndAlso argument.IsIntegerZeroLiteral AndAlso
                   left.TypeKind = TypeKind.Enum AndAlso right.TypeKind <> TypeKind.Enum Then
                    Return ApplicabilityComparisonResult.RightIsMoreApplicable
                End If

                Return ApplicabilityComparisonResult.LeftIsMoreApplicable
            End If

            If Conversions.IsWideningConversion(Conversions.ClassifyConversion(right, left, useSiteInfo).Key) Then

                ' !!! Spec makes it look like rule #3 is a separate rule applied after the second, but this isn't the case
                ' !!! because enumerated type widens to its underlying type, however, if argument is a zero literal,
                ' !!! underlying type should win.
                ' !!! Also, based on Dev10 implementation, Mj doesn't have to be a numeric type, it is enough if it is not
                ' !!! an enumerated type.
                '3.	Aj is the literal 0, Mj is a numeric type and Nj is an enumerated type, or
                If argument IsNot Nothing AndAlso argument.IsIntegerZeroLiteral AndAlso
                   right.TypeKind = TypeKind.Enum AndAlso left.TypeKind <> TypeKind.Enum Then
                    Return ApplicabilityComparisonResult.LeftIsMoreApplicable
                End If

                Return ApplicabilityComparisonResult.RightIsMoreApplicable
            End If

            ''3.	Aj is the literal 0, Mj is a numeric type and Nj is an enumerated type, or
            'If argument IsNot Nothing AndAlso argument.IsIntegerZeroLiteral Then
            '    If left.IsNumericType() Then
            '        If right.TypeKind = TypeKind.Enum Then
            '            leftIsMoreApplicable = True
            '            Return
            '        End If
            '    ElseIf right.IsNumericType() Then
            '        If left.TypeKind = TypeKind.Enum Then
            '            rightIsMoreApplicable = True
            '            Return
            '        End If
            '    End If
            'End If

            '4.	Mj is Byte and Nj is SByte, or
            '5.	Mj is Short and Nj is UShort, or
            '6.	Mj is Integer and Nj is UInteger, or
            '7.	Mj is Long and Nj is ULong.
            '!!! Plus rules not mentioned in the spec
            If left.IsNumericType() AndAlso right.IsNumericType() Then
                Dim leftSpecialType = left.SpecialType
                Dim rightSpecialType = right.SpecialType

                If leftSpecialType = SpecialType.System_Byte AndAlso rightSpecialType = SpecialType.System_SByte Then
                    Return ApplicabilityComparisonResult.LeftIsMoreApplicable
                End If

                If leftSpecialType = SpecialType.System_SByte AndAlso rightSpecialType = SpecialType.System_Byte Then
                    Return ApplicabilityComparisonResult.RightIsMoreApplicable
                End If

                ' This comparison depends on the ordering of the SpecialType enum. There is a unit-test that verifies the ordering.
                If leftSpecialType < rightSpecialType Then
                    Return ApplicabilityComparisonResult.LeftIsMoreApplicable
                Else
                    Debug.Assert(rightSpecialType < leftSpecialType)
                    Return ApplicabilityComparisonResult.RightIsMoreApplicable
                End If
            End If

            '8.	Mj and Nj are delegate function types and the return type of Mj is more specific than the return type of Nj.
            '   If Aj is classified as a lambda method, and Mj or Nj is System.Linq.Expressions.Expression(Of T), then the
            '   type argument of the type (assuming it is a delegate type) is substituted for the type being compared.

            If argument IsNot Nothing Then
                Dim leftIsExpressionTree As Boolean, rightIsExpressionTree As Boolean
                Dim leftDelegateType As NamedTypeSymbol = left.DelegateOrExpressionDelegate(binder, leftIsExpressionTree)
                Dim rightDelegateType As NamedTypeSymbol = right.DelegateOrExpressionDelegate(binder, rightIsExpressionTree)

                ' Native compiler will only compare D1 and D2 for Expression(Of D1) and D2 if the argument is a lambda. It will compare
                ' Expression(Of D1) and Expression (Of D2) regardless of the argument.
                If leftDelegateType IsNot Nothing AndAlso rightDelegateType IsNot Nothing AndAlso
                    ((leftIsExpressionTree = rightIsExpressionTree) OrElse argument.IsAnyLambda()) Then

                    Dim leftInvoke As MethodSymbol = leftDelegateType.DelegateInvokeMethod
                    Dim rightInvoke As MethodSymbol = rightDelegateType.DelegateInvokeMethod

                    If leftInvoke IsNot Nothing AndAlso Not leftInvoke.IsSub AndAlso rightInvoke IsNot Nothing AndAlso Not rightInvoke.IsSub Then
                        Dim newArgument As BoundExpression = Nothing

                        ' TODO: Should probably handle GroupTypeInferenceLambda too.
                        If argument.Kind = BoundKind.QueryLambda Then
                            newArgument = DirectCast(argument, BoundQueryLambda).Expression
                        End If

                        Return CompareParameterTypeApplicability(leftInvoke.ReturnType, rightInvoke.ReturnType, newArgument, binder, useSiteInfo)
                    End If
                End If
            End If

BreakTheTie:
            ' !!! There is a tie-breaking rule not mentioned in the spec. The type that matches argument's type is more applicable.
            ' !!! Otherwise neither is more applicable.
            If argument IsNot Nothing Then
                Dim argType As TypeSymbol = If(argument.Kind <> BoundKind.ArrayLiteral, argument.Type, DirectCast(argument, BoundArrayLiteral).InferredType)

                If argType IsNot Nothing Then
                    If left.IsSameTypeIgnoringAll(argType) Then
                        Return ApplicabilityComparisonResult.LeftIsMoreApplicable
                    End If

                    If right.IsSameTypeIgnoringAll(argType) Then
                        Return ApplicabilityComparisonResult.RightIsMoreApplicable
                    End If
                End If
            End If

            ' Neither is more applicable
            Return ApplicabilityComparisonResult.Undefined
        End Function

        ''' <summary>
        ''' This method groups equally applicable (§11.8.1.1 Applicability) candidates into buckets.
        '''
        ''' Returns an ArrayBuilder of buckets. Each bucket is represented by an ArrayBuilder(Of Integer),
        ''' which contains indexes of equally applicable candidates from input parameter 'candidates'.
        ''' </summary>
        Private Shared Function GroupEquallyApplicableCandidates(
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            binder As Binder
        ) As ArrayBuilder(Of ArrayBuilder(Of Integer))

            Dim buckets = ArrayBuilder(Of ArrayBuilder(Of Integer)).GetInstance()
            Dim i As Integer
            Dim j As Integer

            ' §11.8.1.1 Applicability
            ' A member M is considered equally applicable as N if their signatures are the same or
            ' if each parameter type in M is the same as the corresponding parameter type in N.

            For i = 0 To candidates.Count - 1 Step 1

                Dim left As CandidateAnalysisResult = candidates(i)

                If left.State <> CandidateAnalysisResultState.Applicable OrElse
                   left.EquallyApplicableCandidatesBucket > 0 Then
                    Continue For
                End If

                left.EquallyApplicableCandidatesBucket = buckets.Count + 1
                candidates(i) = left

                Dim b = ArrayBuilder(Of Integer).GetInstance()
                b.Add(i)
                buckets.Add(b)

                For j = i + 1 To candidates.Count - 1 Step 1

                    Dim right As CandidateAnalysisResult = candidates(j)

                    If right.State <> CandidateAnalysisResultState.Applicable OrElse
                       right.EquallyApplicableCandidatesBucket > 0 OrElse
                       right.Candidate Is left.Candidate Then
                        Continue For
                    End If

                    If CandidatesAreEquallyApplicableToArguments(left, right, arguments, binder) Then
                        right.EquallyApplicableCandidatesBucket = left.EquallyApplicableCandidatesBucket
                        candidates(j) = right
                        b.Add(j)
                    End If

                Next
            Next

            Return buckets
        End Function

        Private Shared Function CandidatesAreEquallyApplicableToArguments(
            ByRef left As CandidateAnalysisResult,
            ByRef right As CandidateAnalysisResult,
            arguments As ImmutableArray(Of BoundExpression),
            binder As Binder
        ) As Boolean
            ' §11.8.1.1 Applicability
            ' A member M is considered equally applicable as N if their signatures are the same or
            ' if each parameter type in M is the same as the corresponding parameter type in N.

            ' Compare types of corresponding parameters
            Dim k As Integer
            Dim leftParamIndex As Integer = 0
            Dim rightParamIndex As Integer = 0

            For k = 0 To arguments.Length - 1 Step 1
                Dim leftParamType As TypeSymbol

                Debug.Assert(left.ArgsToParamsOpt.IsDefault = right.ArgsToParamsOpt.IsDefault)

                If left.ArgsToParamsOpt.IsDefault Then
                    leftParamType = GetParameterTypeFromVirtualSignature(left, leftParamIndex)
                    AdvanceParameterInVirtualSignature(left, leftParamIndex)
                Else
                    leftParamType = GetParameterTypeFromVirtualSignature(left, left.ArgsToParamsOpt(k))
                End If

                Dim rightParamType As TypeSymbol

                If right.ArgsToParamsOpt.IsDefault Then
                    rightParamType = GetParameterTypeFromVirtualSignature(right, rightParamIndex)
                    AdvanceParameterInVirtualSignature(right, rightParamIndex)
                Else
                    rightParamType = GetParameterTypeFromVirtualSignature(right, right.ArgsToParamsOpt(k))
                End If

                ' Parameters matching omitted arguments do not participate.
                If arguments(k).Kind <> BoundKind.OmittedArgument AndAlso
                   Not ParametersAreEquallyApplicableToArgument(leftParamType, rightParamType, arguments(k), binder) Then
                    ' Signatures are different
                    Exit For
                End If
            Next

            Return k >= arguments.Length
        End Function

        Private Shared Function ParametersAreEquallyApplicableToArgument(
            leftParamType As TypeSymbol,
            rightParamType As TypeSymbol,
            argument As BoundExpression,
            binder As Binder
        ) As Boolean
            Debug.Assert(argument Is Nothing OrElse argument.Kind <> BoundKind.OmittedArgument)

            If Not leftParamType.IsSameTypeIgnoringAll(rightParamType) Then
                If argument IsNot Nothing Then
                    Dim leftIsExpressionTree As Boolean, rightIsExpressionTree As Boolean
                    Dim leftDelegateType As NamedTypeSymbol = leftParamType.DelegateOrExpressionDelegate(binder, leftIsExpressionTree)
                    Dim rightDelegateType As NamedTypeSymbol = rightParamType.DelegateOrExpressionDelegate(binder, rightIsExpressionTree)

                    ' Native compiler will only compare D1 and D2 for Expression(Of D1) and D2 if the argument is a lambda. It will compare
                    ' Expression(Of D1) and Expression (Of D2) regardless of the argument.
                    If leftDelegateType IsNot Nothing AndAlso rightDelegateType IsNot Nothing AndAlso
                        ((leftIsExpressionTree = rightIsExpressionTree) OrElse argument.IsAnyLambda()) Then

                        Dim leftInvoke As MethodSymbol = leftDelegateType.DelegateInvokeMethod
                        Dim rightInvoke As MethodSymbol = rightDelegateType.DelegateInvokeMethod

                        If leftInvoke IsNot Nothing AndAlso Not leftInvoke.IsSub AndAlso rightInvoke IsNot Nothing AndAlso Not rightInvoke.IsSub Then
                            Dim newArgument As BoundExpression = Nothing

                            ' TODO: Should probably handle GroupTypeInferenceLambda too.
                            If argument.Kind = BoundKind.QueryLambda Then
                                newArgument = DirectCast(argument, BoundQueryLambda).Expression
                            End If

                            Return ParametersAreEquallyApplicableToArgument(leftInvoke.ReturnType, rightInvoke.ReturnType, newArgument, binder)
                        End If
                    End If
                End If

                ' Signatures are different
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' §11.8.1 Overloaded Method Resolution
        '''      3.	Next, eliminate all members from the set that require narrowing conversions
        '''         to be applicable to the argument list, except for the case where the argument
        '''         expression type is Object.
        '''      4.	Next, eliminate all remaining members from the set that require narrowing coercions
        '''         to be applicable to the argument list. If the set is empty, the type containing the
        '''         method group is not an interface, and strict semantics are not being used, the
        '''         invocation target expression is reclassified as a late-bound method access.
        '''         Otherwise, the normal rules apply.
        '''
        ''' Returns amount of applicable candidates left.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function AnalyzeNarrowingCandidates(
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            delegateReturnType As TypeSymbol,
            lateBindingIsAllowed As Boolean,
            binder As Binder,
            ByRef resolutionIsLateBound As Boolean,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Integer
            Dim applicableCandidates As Integer = 0
            Dim appliedTieBreakingRules As Boolean = False

            ' Look through the candidate set for lifted operators that require narrowing conversions whose
            ' source operators also require narrowing conversions. In that case, we only want to keep one method in
            ' the set. If the source operator requires nullables to be unwrapped, then we discard it and keep the lifted operator.
            ' If it does not, then we discard the lifted operator and keep the source operator. This will prevent the presence of
            ' lifted operators from causing overload resolution conflicts where there otherwise wouldn't be one. However,
            ' if the source operator only requires narrowing conversions from numeric literals, then we keep both in the set,
            ' because the conversion in that case is not really narrowing.
            If candidates(0).Candidate.IsOperator Then
                ' As an optimization, we can rely on the fact that lifted operator, if added, immediately
                ' follows source operator.
                Dim i As Integer
                For i = 0 To candidates.Count - 2 Step 1

                    Dim current As CandidateAnalysisResult = candidates(i)
                    Debug.Assert(current.Candidate.IsOperator)

                    If current.State = CandidateAnalysisResultState.Applicable AndAlso
                       Not current.Candidate.IsLifted AndAlso
                       current.RequiresNarrowingNotFromNumericConstant Then

                        Dim contender As CandidateAnalysisResult = candidates(i + 1)
                        Debug.Assert(contender.Candidate.IsOperator)

                        If contender.State = CandidateAnalysisResultState.Applicable AndAlso
                           contender.Candidate.IsLifted AndAlso
                           current.Candidate.UnderlyingSymbol Is contender.Candidate.UnderlyingSymbol Then
                            Exit For
                        End If
                    End If
                Next

                If i < candidates.Count - 1 Then
                    ' [i]  is the index of the first "interesting" pair of source/lifted operators.

                    If Not appliedTieBreakingRules Then
                        ' Apply shadowing rules, Dev10 compiler does that for narrowing candidates too.
                        applicableCandidates = ApplyTieBreakingRulesToEquallyApplicableCandidates(candidates, arguments, delegateReturnType, binder, useSiteInfo)
                        appliedTieBreakingRules = True
                        Debug.Assert(applicableCandidates > 1) ' source and lifted operators are not equally applicable.
                    End If

                    ' Let's do the elimination pass now.
                    For i = i To candidates.Count - 2 Step 1

                        Dim current As CandidateAnalysisResult = candidates(i)
                        Debug.Assert(current.Candidate.IsOperator)

                        If current.State = CandidateAnalysisResultState.Applicable AndAlso
                           Not current.Candidate.IsLifted AndAlso
                           current.RequiresNarrowingNotFromNumericConstant Then

                            Dim contender As CandidateAnalysisResult = candidates(i + 1)
                            Debug.Assert(contender.Candidate.IsOperator)

                            If contender.State = CandidateAnalysisResultState.Applicable AndAlso
                               contender.Candidate.IsLifted AndAlso
                               current.Candidate.UnderlyingSymbol Is contender.Candidate.UnderlyingSymbol Then

                                For j As Integer = 0 To arguments.Length - 1
                                    Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = current.ConversionsOpt(j)

                                    If Conversions.IsNarrowingConversion(conv.Key) Then

                                        Dim lost As Boolean = False

                                        If (conv.Key And ConversionKind.UserDefined) = 0 Then
                                            If IsUnwrappingNullable(conv.Key, arguments(j).Type, current.Candidate.Parameters(j).Type) Then
                                                lost = True
                                            End If
                                        Else
                                            ' Lifted user-defined conversions don't unwrap nullables, they are marked with Nullable bit.
                                            If (conv.Key And ConversionKind.Nullable) = 0 Then
                                                If IsUnwrappingNullable(arguments(j).Type, conv.Value.Parameters(0).Type, useSiteInfo) Then
                                                    lost = True
                                                ElseIf IsUnwrappingNullable(conv.Value.ReturnType, current.Candidate.Parameters(j).Type, useSiteInfo) Then
                                                    lost = True
                                                End If
                                            End If
                                        End If

                                        If lost Then
                                            ' unwrapping nullable, current lost
                                            current.State = CandidateAnalysisResultState.Shadowed
                                            candidates(i) = current
                                            i = i + 1
                                            GoTo Next_i
                                        End If
                                    End If
                                Next

                                ' contender lost
                                contender.State = CandidateAnalysisResultState.Shadowed
                                candidates(i + 1) = contender
                                i = i + 1
                                GoTo Next_i
                            End If
                        End If

Next_i:
                    Next
                End If
            End If

            If lateBindingIsAllowed Then
                ' Are there all narrowing from object candidates?
                Dim haveAllNarrowingFromObject As Boolean = HaveNarrowingOnlyFromObjectCandidates(candidates)

                If haveAllNarrowingFromObject AndAlso Not appliedTieBreakingRules Then
                    ' Apply shadowing rules, Dev10 compiler does that for narrowing candidates too.
                    applicableCandidates = ApplyTieBreakingRulesToEquallyApplicableCandidates(candidates, arguments, delegateReturnType, binder, useSiteInfo)
                    appliedTieBreakingRules = True

                    If applicableCandidates < 2 Then
                        Return applicableCandidates
                    End If

                    haveAllNarrowingFromObject = HaveNarrowingOnlyFromObjectCandidates(candidates)
                End If

                If haveAllNarrowingFromObject Then
                    ' Get rid of candidates that require narrowing from something other than Object
                    applicableCandidates = 0

                    For i As Integer = 0 To candidates.Count - 1 Step 1

                        Dim current As CandidateAnalysisResult = candidates(i)

                        If current.State = CandidateAnalysisResultState.Applicable Then
                            If (current.RequiresNarrowingNotFromObject OrElse current.Candidate.IsExtensionMethod) Then
                                current.State = CandidateAnalysisResultState.ExtensionMethodVsLateBinding
                                candidates(i) = current
                            Else
                                applicableCandidates += 1
                            End If
                        End If
                    Next

                    Debug.Assert(applicableCandidates > 0)

                    If applicableCandidates > 1 Then
                        resolutionIsLateBound = True
                    End If

                    Return applicableCandidates
                End If
            End If

            ' Although all candidates narrow, there may be a best choice when factoring in narrowing of numeric constants.
            ' Note that EliminateLessApplicableToTheArguments applies shadowing rules, Dev10 compiler does that for narrowing candidates too.
            applicableCandidates = EliminateLessApplicableToTheArguments(candidates, arguments, delegateReturnType, appliedTieBreakingRules, binder,
                                                                         mostApplicableMustNarrowOnlyFromNumericConstants:=True, useSiteInfo:=useSiteInfo)

            ' If we ended up with 2 applicable candidates, make sure it is not the same method in
            ' ParamArray expanded and non-expanded form. The non-expanded form should win in this case.
            If applicableCandidates = 2 Then
                For i As Integer = 0 To candidates.Count - 1 Step 1
                    Dim first As CandidateAnalysisResult = candidates(i)

                    If first.State = CandidateAnalysisResultState.Applicable Then
                        For j As Integer = i + 1 To candidates.Count - 1 Step 1
                            Dim second As CandidateAnalysisResult = candidates(j)

                            If second.State = CandidateAnalysisResultState.Applicable Then
                                If first.Candidate.UnderlyingSymbol.Equals(second.Candidate.UnderlyingSymbol) Then
                                    Dim firstWins As Boolean = False
                                    Dim secondWins As Boolean = False

                                    If ShadowBasedOnParamArrayUsage(first, second, firstWins, secondWins) Then
                                        If firstWins Then
                                            second.State = CandidateAnalysisResultState.Shadowed
                                            candidates(j) = second
                                            applicableCandidates = 1
                                        ElseIf secondWins Then
                                            first.State = CandidateAnalysisResultState.Shadowed
                                            candidates(i) = first
                                            applicableCandidates = 1
                                        End If

                                        Debug.Assert(applicableCandidates = 1)
                                    End If
                                End If

                                GoTo Done
                            End If
                        Next

                        Debug.Assert(False, "Should not reach this line.")
                    End If
                Next
            End If

Done:
            Return applicableCandidates
        End Function

        Private Shared Function IsUnwrappingNullable(
            conv As ConversionKind,
            sourceType As TypeSymbol,
            targetType As TypeSymbol
        ) As Boolean
            Debug.Assert((conv And ConversionKind.UserDefined) = 0)
            Return (conv And ConversionKind.Nullable) <> 0 AndAlso
                   sourceType IsNot Nothing AndAlso
                   sourceType.IsNullableType() AndAlso
                   Not targetType.IsNullableType()
        End Function

        Private Shared Function IsUnwrappingNullable(
            sourceType As TypeSymbol,
            targetType As TypeSymbol,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Boolean
            Return sourceType IsNot Nothing AndAlso
                   IsUnwrappingNullable(Conversions.ClassifyPredefinedConversion(sourceType, targetType, useSiteInfo), sourceType, targetType)
        End Function

        Private Shared Function HaveNarrowingOnlyFromObjectCandidates(
            candidates As ArrayBuilder(Of CandidateAnalysisResult)
        ) As Boolean
            Dim haveAllNarrowingFromObject As Boolean = False

            For i As Integer = 0 To candidates.Count - 1 Step 1
                Dim current As CandidateAnalysisResult = candidates(i)

                If current.State = CandidateAnalysisResultState.Applicable AndAlso
                   Not current.RequiresNarrowingNotFromObject AndAlso
                   Not current.Candidate.IsExtensionMethod Then
                    haveAllNarrowingFromObject = True
                    Exit For
                End If
            Next

            Return haveAllNarrowingFromObject
        End Function

        ''' <summary>
        ''' §11.8.1 Overloaded Method Resolution
        '''     2.	Next, eliminate all members from the set that are inaccessible or not applicable to the argument list.
        '''
        ''' Note, similar to Dev10 compiler this process will eliminate candidates requiring narrowing conversions
        ''' if strict semantics is used, exception are candidates that require narrowing only from numeric constants.
        '''
        ''' Returns amount of applicable candidates left.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function EliminateNotApplicableToArguments(
            methodOrPropertyGroup As BoundMethodOrPropertyGroup,
            candidates As ArrayBuilder(Of CandidateAnalysisResult),
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            binder As Binder,
            <Out()> ByRef applicableNarrowingCandidates As Integer,
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            callerInfoOpt As SyntaxNode,
            forceExpandedForm As Boolean,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Integer
            Dim applicableCandidates As Integer = 0
            Dim illegalInAttribute As Integer = 0
            applicableNarrowingCandidates = 0

            ' Filter out inapplicable candidates
            For i As Integer = 0 To candidates.Count - 1 Step 1

                Dim current As CandidateAnalysisResult = candidates(i)

                If current.State <> CandidateAnalysisResultState.Applicable Then
                    Continue For
                End If

                If Not current.ArgumentMatchingDone Then
                    MatchArguments(methodOrPropertyGroup, current, arguments, argumentNames, binder, asyncLambdaSubToFunctionMismatch, callerInfoOpt, forceExpandedForm, useSiteInfo)
                    current.SetArgumentMatchingDone()
                    candidates(i) = current
                End If

                If current.State = CandidateAnalysisResultState.Applicable Then
                    applicableCandidates += 1

                    If current.RequiresNarrowingConversion Then
                        applicableNarrowingCandidates += 1
                    End If

                    If current.IsIllegalInAttribute Then
                        illegalInAttribute += 1
                    End If
                End If
            Next

            ' Filter out candidates with IsIllegalInAttribute if there are other applicable candidates
            If illegalInAttribute > 0 AndAlso applicableCandidates > illegalInAttribute Then
                For i As Integer = 0 To candidates.Count - 1 Step 1

                    Dim current As CandidateAnalysisResult = candidates(i)

                    If current.State = CandidateAnalysisResultState.Applicable AndAlso current.IsIllegalInAttribute Then
                        applicableCandidates -= 1

                        If current.RequiresNarrowingConversion Then
                            applicableNarrowingCandidates -= 1
                        End If

                        current.State = CandidateAnalysisResultState.ArgumentMismatch
                        candidates(i) = current
                    End If
                Next

                Debug.Assert(applicableCandidates > 0)
            End If

            Return applicableCandidates
        End Function

        ''' <summary>
        ''' Figure out corresponding arguments for parameters §11.8.2 Applicable Methods.
        '''
        ''' Note, this function mutates the candidate structure.
        '''
        ''' If non-Nothing ArrayBuilders are returned through parameterToArgumentMap and paramArrayItems
        ''' parameters, the caller is responsible fo returning them into the pool.
        '''
        ''' Assumptions:
        '''    1) This function is never called for a candidate that should be rejected due to parameter count.
        '''    2) Omitted arguments [ Call Goo(a, , b) ] are represented by OmittedArgumentExpression node in the arguments array.
        '''    3) Omitted argument never has name.
        '''    4) argumentNames contains Nothing for all positional arguments.
        '''
        ''' !!! Should keep this function in sync with Binder.PassArguments, which uses data this function populates.              !!!
        ''' !!! Should keep this function in sync with Binder.ReportOverloadResolutionFailureForASingleCandidate.                  !!!
        ''' !!! Everything we flag as an error here, Binder.ReportOverloadResolutionFailureForASingleCandidate should detect as well. !!!
        ''' </summary>
        Private Shared Sub BuildParameterToArgumentMap(
            ByRef candidate As CandidateAnalysisResult,
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            ByRef parameterToArgumentMap As ArrayBuilder(Of Integer),
            ByRef paramArrayItems As ArrayBuilder(Of Integer)
        )
            Debug.Assert(Not arguments.IsDefault)
            Debug.Assert(argumentNames.IsDefault OrElse (argumentNames.Length > 0 AndAlso argumentNames.Length = arguments.Length))
            Debug.Assert(Not candidate.ArgumentMatchingDone)
            Debug.Assert(candidate.State = CandidateAnalysisResultState.Applicable)

            parameterToArgumentMap = ArrayBuilder(Of Integer).GetInstance(candidate.Candidate.ParameterCount, -1)

            Dim argsToParams As ArrayBuilder(Of Integer) = Nothing

            If Not argumentNames.IsDefault Then
                argsToParams = ArrayBuilder(Of Integer).GetInstance(arguments.Length, -1)
            End If

            paramArrayItems = Nothing

            If candidate.IsExpandedParamArrayForm Then
                paramArrayItems = ArrayBuilder(Of Integer).GetInstance()
            End If

            '§11.8.2 Applicable Methods
            '1.	First, match each positional argument in order to the list of method parameters.
            'If there are more positional arguments than parameters and the last parameter is not a paramarray, the method is not applicable.
            'Otherwise, the paramarray parameter is expanded with parameters of the paramarray element type to match the number of positional arguments.
            'If a positional argument is omitted, the method is not applicable.
            ' !!! Not sure about the last sentence: "If a positional argument is omitted, the method is not applicable."
            ' !!! Dev10 allows omitting positional argument as long as the corresponding parameter is optional.

            Dim positionalArguments As Integer = 0
            Dim paramIndex = 0

            For i As Integer = 0 To arguments.Length - 1 Step 1
                If Not argumentNames.IsDefault AndAlso argumentNames(i) IsNot Nothing Then
                    ' A named argument

                    If Not candidate.Candidate.TryGetNamedParamIndex(argumentNames(i), paramIndex) Then
                        ' ERRID_NamedParamNotFound1
                        ' ERRID_NamedParamNotFound2
                        candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                        GoTo Bailout
                    End If

                    If paramIndex <> i Then
                        ' all remaining arguments must be named
                        Exit For
                    End If

                    If paramIndex = candidate.Candidate.ParameterCount - 1 AndAlso
                    candidate.Candidate.Parameters(paramIndex).IsParamArray Then
                        ' ERRID_NamedParamArrayArgument
                        candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                        GoTo Bailout
                    End If

                    Debug.Assert(parameterToArgumentMap(paramIndex) = -1)
                End If

                positionalArguments += 1

                If argsToParams IsNot Nothing Then
                    argsToParams(i) = paramIndex
                End If

                If arguments(i).Kind = BoundKind.OmittedArgument Then

                    If paramIndex = candidate.Candidate.ParameterCount - 1 AndAlso
                       candidate.Candidate.Parameters(paramIndex).IsParamArray Then
                        ' Omitted ParamArray argument at the call site
                        ' ERRID_OmittedParamArrayArgument
                        candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                        GoTo Bailout
                    Else
                        parameterToArgumentMap(paramIndex) = i
                        paramIndex += 1
                    End If

                ElseIf (candidate.IsExpandedParamArrayForm AndAlso
                    paramIndex = candidate.Candidate.ParameterCount - 1) Then

                    paramArrayItems.Add(i)
                Else
                    parameterToArgumentMap(paramIndex) = i
                    paramIndex += 1
                End If
            Next

            '§11.8.2 Applicable Methods
            '2.	Next, match each named argument to a parameter with the given name.
            'If one of the named arguments fails to match, matches a paramarray parameter,
            'or matches an argument already matched with another positional or named argument,
            'the method is not applicable.
            For i As Integer = positionalArguments To arguments.Length - 1 Step 1

                Debug.Assert(argumentNames(i) Is Nothing OrElse argumentNames(i).Length > 0)

                If argumentNames(i) Is Nothing Then
                    ' Unnamed argument follows named arguments, parser should have detected an error.
                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                    GoTo Bailout
                    'Continue For
                End If

                If Not candidate.Candidate.TryGetNamedParamIndex(argumentNames(i), paramIndex) Then
                    ' ERRID_NamedParamNotFound1
                    ' ERRID_NamedParamNotFound2
                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                    GoTo Bailout
                    'Continue For
                End If

                If argsToParams IsNot Nothing Then
                    argsToParams(i) = paramIndex
                End If

                If paramIndex = candidate.Candidate.ParameterCount - 1 AndAlso
                    candidate.Candidate.Parameters(paramIndex).IsParamArray Then
                    ' ERRID_NamedParamArrayArgument
                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                    GoTo Bailout
                    'Continue For
                End If

                If parameterToArgumentMap(paramIndex) <> -1 Then
                    ' ERRID_NamedArgUsedTwice1
                    ' ERRID_NamedArgUsedTwice2
                    ' ERRID_NamedArgUsedTwice3
                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                    GoTo Bailout
                    'Continue For
                End If

                ' It is an error for a named argument to specify
                ' a value for an explicitly omitted positional argument.
                If paramIndex < positionalArguments Then
                    'ERRID_NamedArgAlsoOmitted1
                    'ERRID_NamedArgAlsoOmitted2
                    'ERRID_NamedArgAlsoOmitted3
                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                    GoTo Bailout
                    'Continue For
                End If

                parameterToArgumentMap(paramIndex) = i
            Next

            If argsToParams IsNot Nothing Then
                candidate.ArgsToParamsOpt = argsToParams.ToImmutableAndFree()
                argsToParams = Nothing
            End If

Bailout:
            If argsToParams IsNot Nothing Then
                argsToParams.Free()
                argsToParams = Nothing
            End If

        End Sub

        ''' <summary>
        ''' Match candidate's parameters to arguments §11.8.2 Applicable Methods.
        '''
        ''' Note, similar to Dev10 compiler this process will eliminate candidate requiring narrowing conversions
        ''' if strict semantics is used, exception are candidates that require narrowing only from numeric constants.
        '''
        ''' Assumptions:
        '''    1) This function is never called for a candidate that should be rejected due to parameter count.
        '''    2) Omitted arguments [ Call Goo(a, , b) ] are represented by OmittedArgumentExpression node in the arguments array.
        '''    3) Omitted argument never has name.
        '''    4) argumentNames contains Nothing for all positional arguments.
        '''
        ''' !!! Should keep this function in sync with Binder.PassArguments, which uses data this function populates.              !!!
        ''' !!! Should keep this function in sync with Binder.ReportOverloadResolutionFailureForASingleCandidate.                  !!!
        ''' !!! Should keep this function in sync with InferenceGraph.PopulateGraph.                                               !!!
        ''' !!! Everything we flag as an error here, Binder.ReportOverloadResolutionFailureForASingleCandidate should detect as well. !!!
        ''' </summary>
        Private Shared Sub MatchArguments(
            methodOrPropertyGroup As BoundMethodOrPropertyGroup,
            ByRef candidate As CandidateAnalysisResult,
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            binder As Binder,
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            callerInfoOpt As SyntaxNode,
            forceExpandedForm As Boolean,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )
            Debug.Assert(Not arguments.IsDefault)
            Debug.Assert(argumentNames.IsDefault OrElse (argumentNames.Length > 0 AndAlso argumentNames.Length = arguments.Length))
            Debug.Assert(Not candidate.ArgumentMatchingDone)
            Debug.Assert(candidate.State = CandidateAnalysisResultState.Applicable)
            Debug.Assert(Not candidate.Candidate.UnderlyingSymbol.IsReducedExtensionMethod() OrElse methodOrPropertyGroup.ReceiverOpt IsNot Nothing OrElse TypeOf methodOrPropertyGroup.SyntaxTree Is DummySyntaxTree)

            Dim parameterToArgumentMap As ArrayBuilder(Of Integer) = Nothing
            Dim paramArrayItems As ArrayBuilder(Of Integer) = Nothing
            Dim conversionKinds As KeyValuePair(Of ConversionKind, MethodSymbol)() = Nothing
            Dim conversionBackKinds As KeyValuePair(Of ConversionKind, MethodSymbol)() = Nothing
            Dim optionalArguments As OptionalArgument() = Nothing
            Dim defaultValueDiagnostics As BindingDiagnosticBag = Nothing

            BuildParameterToArgumentMap(candidate, arguments, argumentNames, parameterToArgumentMap, paramArrayItems)

            If candidate.State <> CandidateAnalysisResultState.Applicable Then
                Debug.Assert(Not candidate.IgnoreExtensionMethods)
                GoTo Bailout
            End If

            ' At this point we will set IgnoreExtensionMethods to true and will
            ' clear it when appropriate because not every failure should allow
            ' us to consider extension methods.
            If Not candidate.Candidate.IsExtensionMethod Then
                candidate.IgnoreExtensionMethods = True
            End If

            '§11.8.2 Applicable Methods
            'The type arguments, if any, must satisfy the constraints, if any, on the matching type parameters.
            Dim candidateSymbol = candidate.Candidate.UnderlyingSymbol
            If candidateSymbol.Kind = SymbolKind.Method Then
                Dim method = DirectCast(candidateSymbol, MethodSymbol)
                If method.IsGenericMethod Then
                    Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
                    Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing
                    Dim satisfiedConstraints = method.CheckConstraints(diagnosticsBuilder, useSiteDiagnosticsBuilder, template:=useSiteInfo)
                    diagnosticsBuilder.Free()

                    If useSiteDiagnosticsBuilder IsNot Nothing AndAlso useSiteDiagnosticsBuilder.Count > 0 Then
                        For Each diag In useSiteDiagnosticsBuilder
                            useSiteInfo.Add(diag.UseSiteInfo)
                        Next
                    End If

                    If Not satisfiedConstraints Then
                        ' Do not clear IgnoreExtensionMethods flag if constraints are violated.
                        candidate.State = CandidateAnalysisResultState.GenericConstraintsViolated
                        GoTo Bailout
                    End If
                End If
            End If

            ' Traverse the parameters, converting corresponding arguments
            ' as appropriate.

            Dim argIndex As Integer
            Dim candidateIsAProperty As Boolean = (candidate.Candidate.UnderlyingSymbol.Kind = SymbolKind.Property)

            For paramIndex = 0 To candidate.Candidate.ParameterCount - 1 Step 1
                If candidate.State <> CandidateAnalysisResultState.Applicable AndAlso
                   Not candidate.IgnoreExtensionMethods Then
                    ' There is no reason to continue. We will not learn anything new.
                    GoTo Bailout
                End If

                Dim param As ParameterSymbol = candidate.Candidate.Parameters(paramIndex)
                Dim isByRef As Boolean = param.IsByRef
                Dim targetType As TypeSymbol = param.Type

                If param.IsParamArray AndAlso paramIndex = candidate.Candidate.ParameterCount - 1 Then

                    If targetType.Kind <> SymbolKind.ArrayType Then
                        ' ERRID_ParamArrayWrongType
                        candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                        candidate.IgnoreExtensionMethods = False
                        GoTo Bailout
                        'Continue For
                    End If

                    If Not candidate.IsExpandedParamArrayForm Then

                        argIndex = parameterToArgumentMap(paramIndex)
                        Dim paramArrayArgument = If(argIndex = -1, Nothing, arguments(argIndex))

                        Debug.Assert(paramArrayArgument Is Nothing OrElse paramArrayArgument.Kind <> BoundKind.OmittedArgument)

                        '§11.8.2 Applicable Methods
                        'If the conversion from the type of the argument expression to the paramarray type is narrowing,
                        'then the method is only applicable in its expanded form.
                        '!!! However, there is an exception to that rule - narrowing conversion from semantical Nothing literal is Ok. !!!
                        Dim arrayConversion As KeyValuePair(Of ConversionKind, MethodSymbol) = Nothing

                        If Not (paramArrayArgument IsNot Nothing AndAlso
                                Not paramArrayArgument.HasErrors AndAlso CanPassToParamArray(paramArrayArgument, targetType, arrayConversion, binder, useSiteInfo)) Then
                            ' It doesn't look like native compiler reports any errors in this case.
                            ' Probably due to assumption that either errors were already reported for bad argument expression or
                            ' we will report errors for expanded version of the same candidate.
                            candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                            candidate.IgnoreExtensionMethods = False
                            GoTo Bailout
                            'Continue For

                        ElseIf Conversions.IsNarrowingConversion(arrayConversion.Key) Then

                            ' We can get here only for Object with constant value == Nothing.
                            Debug.Assert(paramArrayArgument.IsNothingLiteral())

                            ' Unlike for other arguments, Dev10 doesn't make a note of this narrowing.
                            ' However, should this narrowing cause a conversion error, the error must be noted.
                            If binder.OptionStrict = OptionStrict.On Then
                                candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                                ' Note, this doesn't clear IgnoreExtensionMethods flag.
                                Continue For
                            End If
                        Else
                            Debug.Assert(Conversions.IsWideningConversion(arrayConversion.Key))
                        End If

                        ' Since CanPassToParamArray succeeded, there is no need to check conversions for this argument again
                        If Not Conversions.IsIdentityConversion(arrayConversion.Key) Then
                            If conversionKinds Is Nothing Then
                                conversionKinds = New KeyValuePair(Of ConversionKind, MethodSymbol)(arguments.Length - 1) {}
                                For i As Integer = 0 To conversionKinds.Length - 1
                                    conversionKinds(i) = Conversions.Identity
                                Next
                            End If

                            conversionKinds(argIndex) = arrayConversion
                        End If
                    Else
                        Debug.Assert(candidate.IsExpandedParamArrayForm)

                        '§11.8.2 Applicable Methods
                        ' If the argument expression is the literal Nothing, then the method is only applicable in its unexpanded form.
                        ' Note, that explicitly converted NOTHING is treated the same way by Dev10.
                        ' But, for the purpose of interpolated string lowering the method is applicable even if the argument expression
                        ' is the literal Nothing. This is because for interpolated string lowering we want to always call methods
                        ' in their expanded form. E.g. $"{Nothing}" should be lowered to String.Format("{0}", New Object() {Nothing}) not
                        ' String.Format("{0}", CType(Nothing, Object())).
                        If paramArrayItems.Count = 1 AndAlso arguments(paramArrayItems(0)).IsNothingLiteral() AndAlso Not forceExpandedForm Then
                            candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                            candidate.IgnoreExtensionMethods = False
                            GoTo Bailout
                            'Continue For
                        End If

                        ' Otherwise, for a ParamArray parameter, all the matching arguments are passed
                        ' ByVal as instances of the element type of the ParamArray.
                        ' Perform the conversions to the element type of the ParamArray here.
                        Dim arrayType = DirectCast(targetType, ArrayTypeSymbol)

                        If Not arrayType.IsSZArray Then
                            ' ERRID_ParamArrayWrongType
                            candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                            candidate.IgnoreExtensionMethods = False
                            GoTo Bailout
                            'Continue For
                        End If

                        targetType = arrayType.ElementType

                        If targetType.Kind = SymbolKind.ErrorType Then
                            candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                            ' Note, IgnoreExtensionMethods is not cleared.
                            Continue For
                        End If

                        For j As Integer = 0 To paramArrayItems.Count - 1 Step 1
                            Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = Nothing

                            If arguments(paramArrayItems(j)).HasErrors Then ' UNDONE: should HasErrors really always cause argument mismatch [petergo, 3/9/2011]
                                candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                                candidate.IgnoreExtensionMethods = False
                                GoTo Bailout
                                'Continue For
                            End If

                            If Not MatchArgumentToByValParameter(methodOrPropertyGroup, candidate, arguments(paramArrayItems(j)), targetType, binder, conv, asyncLambdaSubToFunctionMismatch, useSiteInfo) Then
                                ' Note, IgnoreExtensionMethods is not cleared here, MatchArgumentToByValParameter makes required changes.
                                Continue For
                            End If

                            ' typically all conversions in otherwise acceptable candidate are identity conversions
                            If Not Conversions.IsIdentityConversion(conv.Key) Then
                                If conversionKinds Is Nothing Then
                                    conversionKinds = New KeyValuePair(Of ConversionKind, MethodSymbol)(arguments.Length - 1) {}
                                    For i As Integer = 0 To conversionKinds.Length - 1
                                        conversionKinds(i) = Conversions.Identity
                                    Next
                                End If

                                conversionKinds(paramArrayItems(j)) = conv
                            End If
                        Next
                    End If

                    Continue For
                End If

                argIndex = parameterToArgumentMap(paramIndex)
                Dim argument = If(argIndex = -1, Nothing, arguments(argIndex))
                Dim defaultArgument As BoundExpression = Nothing

                If argument Is Nothing OrElse argument.Kind = BoundKind.OmittedArgument Then

                    ' Deal with Optional arguments.
                    If defaultValueDiagnostics Is Nothing Then
                        defaultValueDiagnostics = BindingDiagnosticBag.GetInstance()
                    Else
                        defaultValueDiagnostics.Clear()
                    End If

                    Dim receiverOpt As BoundExpression = Nothing
                    If candidateSymbol.IsReducedExtensionMethod() Then
                        receiverOpt = methodOrPropertyGroup.ReceiverOpt
                    End If

                    defaultArgument = binder.GetArgumentForParameterDefaultValue(param, If(argument, methodOrPropertyGroup).Syntax, defaultValueDiagnostics, callerInfoOpt, parameterToArgumentMap, arguments, receiverOpt)

                    If defaultArgument IsNot Nothing AndAlso Not defaultValueDiagnostics.HasAnyErrors Then
                        Debug.Assert(Not defaultValueDiagnostics.DiagnosticBag.AsEnumerable().Any())
                        ' Mark these as compiler generated so they are ignored by later phases. For example,
                        ' these bound nodes will mess up the incremental binder cache, because they use the
                        ' the same syntax node as the method identifier from the invocation / AddressOf if they
                        ' are not marked.
                        defaultArgument.SetWasCompilerGenerated()
                        argument = defaultArgument
                    Else
                        candidate.State = CandidateAnalysisResultState.ArgumentMismatch

                        'Note, IgnoreExtensionMethods flag should not be cleared due to a badness of default value.
                        candidate.IgnoreExtensionMethods = False
                        GoTo Bailout
                    End If
                End If

                If targetType.Kind = SymbolKind.ErrorType Then
                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                    ' Note, IgnoreExtensionMethods is not cleared.
                    Continue For
                End If

                If argument.HasErrors Then ' UNDONE: should HasErrors really always cause argument mismatch [petergo, 3/9/2011]
                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                    candidate.IgnoreExtensionMethods = False
                    GoTo Bailout
                End If

                Dim conversion As KeyValuePair(Of ConversionKind, MethodSymbol) = Nothing
                Dim conversionBack As KeyValuePair(Of ConversionKind, MethodSymbol) = Nothing

                Debug.Assert(Not isByRef OrElse param.IsExplicitByRef OrElse targetType.IsStringType())

                ' Arguments for properties are always passed with ByVal semantics. Even if
                ' parameter in metadata is defined ByRef, we always pass corresponding argument
                ' through a temp without copy-back.
                ' Non-string arguments for implicitly ByRef string parameters of Declare functions
                ' are passed through a temp without copy-back.
                If isByRef AndAlso Not candidateIsAProperty AndAlso defaultArgument Is Nothing AndAlso
                   (param.IsExplicitByRef OrElse (argument.Type IsNot Nothing AndAlso argument.Type.IsStringType())) Then
                    MatchArgumentToByRefParameter(methodOrPropertyGroup, candidate, argument, targetType, binder, conversion, conversionBack, asyncLambdaSubToFunctionMismatch, useSiteInfo)
                Else
                    conversionBack = Conversions.Identity
                    MatchArgumentToByValParameter(methodOrPropertyGroup, candidate, argument, targetType, binder, conversion, asyncLambdaSubToFunctionMismatch, useSiteInfo, defaultArgument IsNot Nothing)
                End If

                ' typically all conversions in otherwise acceptable candidate are identity conversions
                If Not Conversions.IsIdentityConversion(conversion.Key) Then
                    If conversionKinds Is Nothing Then
                        conversionKinds = New KeyValuePair(Of ConversionKind, MethodSymbol)(arguments.Length - 1) {}
                        For i As Integer = 0 To conversionKinds.Length - 1
                            conversionKinds(i) = Conversions.Identity
                        Next
                    End If

                    ' If this is not a default argument then store the conversion in the conversionKinds.
                    ' For default arguments the conversion is stored below.
                    If defaultArgument Is Nothing Then
                        conversionKinds(argIndex) = conversion
                    End If
                End If

                ' If this is a default argument then add it to the candidate result default arguments.
                ' Note these arguments are stored by parameter index. Default arguments are missing so they
                ' may not have an argument index.
                If defaultArgument IsNot Nothing Then
                    If optionalArguments Is Nothing Then
                        optionalArguments = New OptionalArgument(candidate.Candidate.ParameterCount - 1) {}
                    End If
                    optionalArguments(paramIndex) = New OptionalArgument(defaultArgument, conversion, defaultValueDiagnostics.DependenciesBag.ToImmutableArray())
                End If

                If Not Conversions.IsIdentityConversion(conversionBack.Key) Then
                    If conversionBackKinds Is Nothing Then
                        ' There should never be a copy back conversion with a default argument.
                        Debug.Assert(defaultArgument Is Nothing)
                        conversionBackKinds = New KeyValuePair(Of ConversionKind, MethodSymbol)(arguments.Length - 1) {}
                        For i As Integer = 0 To conversionBackKinds.Length - 1
                            conversionBackKinds(i) = Conversions.Identity
                        Next
                    End If

                    conversionBackKinds(argIndex) = conversionBack
                End If
            Next

Bailout:
            If defaultValueDiagnostics IsNot Nothing Then
                defaultValueDiagnostics.Free()
            End If

            If paramArrayItems IsNot Nothing Then
                paramArrayItems.Free()
            End If

            If conversionKinds IsNot Nothing Then
                candidate.ConversionsOpt = conversionKinds.AsImmutableOrNull()
            End If

            If conversionBackKinds IsNot Nothing Then
                candidate.ConversionsBackOpt = conversionBackKinds.AsImmutableOrNull()
            End If

            If optionalArguments IsNot Nothing Then
                candidate.OptionalArguments = optionalArguments.AsImmutableOrNull()
            End If

            If parameterToArgumentMap IsNot Nothing Then
                parameterToArgumentMap.Free()
            End If

        End Sub

        ''' <summary>
        ''' Should be in sync with Binder.ReportByRefConversionErrors.
        ''' </summary>
        Private Shared Sub MatchArgumentToByRefParameter(
            methodOrPropertyGroup As BoundMethodOrPropertyGroup,
            ByRef candidate As CandidateAnalysisResult,
            argument As BoundExpression,
            targetType As TypeSymbol,
            binder As Binder,
            <Out()> ByRef outConversionKind As KeyValuePair(Of ConversionKind, MethodSymbol),
            <Out()> ByRef outConversionBackKind As KeyValuePair(Of ConversionKind, MethodSymbol),
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )

            If argument.IsSupportingAssignment() Then

                If argument.IsLValue() AndAlso targetType.IsSameTypeIgnoringAll(argument.Type) Then
                    outConversionKind = Conversions.Identity
                    outConversionBackKind = Conversions.Identity

                Else
                    outConversionBackKind = Conversions.Identity

                    If MatchArgumentToByValParameter(methodOrPropertyGroup, candidate, argument, targetType, binder, outConversionKind, asyncLambdaSubToFunctionMismatch, useSiteInfo) Then

                        ' Check copy back conversion
                        Dim copyBackType = argument.GetTypeOfAssignmentTarget()
                        Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(targetType, copyBackType, useSiteInfo)
                        outConversionBackKind = conv

                        If Conversions.NoConversion(conv.Key) Then
                            candidate.State = CandidateAnalysisResultState.ArgumentMismatch ' Possible only with user-defined conversions, I think.
                            candidate.IgnoreExtensionMethods = False
                        Else
                            If Conversions.IsNarrowingConversion(conv.Key) Then

                                ' Similar to Dev10 compiler, we will eliminate candidate requiring narrowing conversions
                                ' if strict semantics is used, exception are candidates that require narrowing only from
                                ' numeric(Constants.
                                candidate.SetRequiresNarrowingConversion()

                                Debug.Assert((conv.Key And ConversionKind.InvolvesNarrowingFromNumericConstant) = 0)
                                candidate.SetRequiresNarrowingNotFromNumericConstant()

                                If binder.OptionStrict = OptionStrict.On Then
                                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                                    Return
                                End If

                                If targetType.SpecialType <> SpecialType.System_Object Then
                                    candidate.SetRequiresNarrowingNotFromObject()
                                End If
                            End If

                            candidate.RegisterDelegateRelaxationLevel(conv.Key)
                        End If
                    End If

                End If

            Else
                ' No copy back needed

                ' If we are inside a lambda in a constructor and are passing ByRef a non-LValue field, which
                ' would be an LValue field, if it were referred to in the constructor outside of a lambda,
                ' we need to report an error because the operation will result in a simulated pass by
                ' ref (through a temp, without a copy back), which might be not the intent.
                If binder.Report_ERRID_ReadOnlyInClosure(argument) Then
                    candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                    ' Note, we do not change IgnoreExtensionMethods flag here.
                End If

                outConversionBackKind = Conversions.Identity
                MatchArgumentToByValParameter(methodOrPropertyGroup, candidate, argument, targetType, binder, outConversionKind, asyncLambdaSubToFunctionMismatch, useSiteInfo)
            End If

        End Sub

        ''' <summary>
        ''' Should be in sync with Binder.ReportByValConversionErrors.
        ''' </summary>
        Private Shared Function MatchArgumentToByValParameter(
            methodOrPropertyGroup As BoundMethodOrPropertyGroup,
            ByRef candidate As CandidateAnalysisResult,
            argument As BoundExpression,
            targetType As TypeSymbol,
            binder As Binder,
            <Out()> ByRef outConversionKind As KeyValuePair(Of ConversionKind, MethodSymbol),
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
            Optional isDefaultValueArgument As Boolean = False
        ) As Boolean

            outConversionKind = Nothing 'VBConversions.NoConversion

            ' TODO: Do we need to do more thorough check for error types here, i.e. dig into generics,
            ' arrays, etc., detect types from unreferenced assemblies, ... ?
            If targetType.IsErrorType() Then
                candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                ' Note, IgnoreExtensionMethods is not cleared.
                Return False
            End If

            Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(argument, targetType, binder, useSiteInfo)

            outConversionKind = conv

            If Conversions.NoConversion(conv.Key) Then
                candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                candidate.IgnoreExtensionMethods = False

                If (conv.Key And (ConversionKind.DelegateRelaxationLevelMask Or ConversionKind.Lambda)) = (ConversionKind.DelegateRelaxationLevelInvalid Or ConversionKind.Lambda) Then
                    ' Dig through parenthesized
                    Dim underlying As BoundExpression = argument

                    While underlying.Kind = BoundKind.Parenthesized AndAlso underlying.Type Is Nothing
                        underlying = DirectCast(underlying, BoundParenthesized).Expression
                    End While

                    Dim unbound = If(underlying.Kind = BoundKind.UnboundLambda, DirectCast(underlying, UnboundLambda), Nothing)

                    If unbound IsNot Nothing AndAlso Not unbound.IsFunctionLambda AndAlso
                       (unbound.Flags And SourceMemberFlags.Async) <> 0 AndAlso
                       targetType.IsDelegateType Then
                        Dim delegateInvoke As MethodSymbol = DirectCast(targetType, NamedTypeSymbol).DelegateInvokeMethod
                        Debug.Assert(delegateInvoke IsNot Nothing)

                        If delegateInvoke IsNot Nothing Then
                            Dim bound As BoundLambda = unbound.GetBoundLambda(New UnboundLambda.TargetSignature(delegateInvoke))
                            Debug.Assert(bound IsNot Nothing)

                            If bound IsNot Nothing AndAlso (bound.MethodConversionKind And MethodConversionKind.AllErrorReasons) = MethodConversionKind.Error_SubToFunction AndAlso
                               (Not bound.Diagnostics.Diagnostics.HasAnyErrors) Then
                                If asyncLambdaSubToFunctionMismatch Is Nothing Then
                                    asyncLambdaSubToFunctionMismatch = New HashSet(Of BoundExpression)(ReferenceEqualityComparer.Instance)
                                End If

                                asyncLambdaSubToFunctionMismatch.Add(unbound)
                            End If
                        End If
                    End If
                End If

                Return False
            End If

            ' Characteristics of conversion applied to a default value for an optional parameter shouldn't be used to disambiguate
            ' between two candidates.

            If Conversions.IsNarrowingConversion(conv.Key) Then

                ' Similar to Dev10 compiler, we will eliminate candidate requiring narrowing conversions
                ' if strict semantics is used, exception are candidates that require narrowing only from
                ' numeric constants.
                If Not isDefaultValueArgument Then
                    candidate.SetRequiresNarrowingConversion()
                End If

                If (conv.Key And ConversionKind.InvolvesNarrowingFromNumericConstant) = 0 Then
                    If Not isDefaultValueArgument Then
                        candidate.SetRequiresNarrowingNotFromNumericConstant()
                    End If

                    If binder.OptionStrict = OptionStrict.On Then
                        candidate.State = CandidateAnalysisResultState.ArgumentMismatch
                        Return False
                    End If
                End If

                Dim argumentType = argument.Type

                If argumentType Is Nothing OrElse
                   argumentType.SpecialType <> SpecialType.System_Object Then
                    If Not isDefaultValueArgument Then
                        candidate.SetRequiresNarrowingNotFromObject()
                    End If
                End If

            ElseIf (conv.Key And ConversionKind.InvolvesNarrowingFromNumericConstant) <> 0 Then
                ' Dev10 overload resolution treats conversions that involve narrowing from numeric constant type
                ' as narrowing.
                If Not isDefaultValueArgument Then
                    candidate.SetRequiresNarrowingConversion()
                    candidate.SetRequiresNarrowingNotFromObject()
                End If
            End If

            If Not isDefaultValueArgument Then
                candidate.RegisterDelegateRelaxationLevel(conv.Key)
            End If

            ' If we are in attribute context, keep track of candidates that will result in illegal arguments.
            ' They should be dismissed in favor of other applicable candidates.
            If binder.BindingLocation = BindingLocation.Attribute AndAlso
               Not candidate.IsIllegalInAttribute AndAlso
               Not methodOrPropertyGroup.WasCompilerGenerated AndAlso
               methodOrPropertyGroup.Kind = BoundKind.MethodGroup AndAlso
               IsWithinAppliedAttributeName(methodOrPropertyGroup.Syntax) AndAlso
               DirectCast(candidate.Candidate.UnderlyingSymbol, MethodSymbol).MethodKind = MethodKind.Constructor AndAlso
               binder.Compilation.GetWellKnownType(WellKnownType.System_Attribute).IsBaseTypeOf(candidate.Candidate.UnderlyingSymbol.ContainingType, useSiteInfo) Then

                Debug.Assert(Not argument.HasErrors)
                Dim passedExpression As BoundExpression = binder.PassArgumentByVal(argument, conv, targetType, BindingDiagnosticBag.Discarded)

                If Not passedExpression.IsConstant Then ' Trying to match native compiler behavior in Semantics::IsValidAttributeConstant
                    Dim visitor As New Binder.AttributeExpressionVisitor(binder, passedExpression.HasErrors)
                    visitor.VisitExpression(passedExpression, BindingDiagnosticBag.Discarded)

                    If visitor.HasErrors Then
                        candidate.SetIllegalInAttribute()
                    End If
                End If
            End If

            Return True
        End Function

        Private Shared Function IsWithinAppliedAttributeName(syntax As SyntaxNode) As Boolean
            Dim parent As SyntaxNode = syntax.Parent

            While parent IsNot Nothing
                If parent.Kind = SyntaxKind.Attribute Then
                    Return DirectCast(parent, AttributeSyntax).Name.Span.Contains(syntax.Position)
                ElseIf TypeOf parent Is ExpressionSyntax OrElse TypeOf parent Is StatementSyntax Then
                    Exit While
                End If

                parent = parent.Parent
            End While

            Return False
        End Function

        Public Shared Function CanPassToParamArray(
            expression As BoundExpression,
            targetType As TypeSymbol,
            <Out()> ByRef outConvKind As KeyValuePair(Of ConversionKind, MethodSymbol),
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Boolean
            '§11.8.2 Applicable Methods
            'If the conversion from the type of the argument expression to the paramarray type is narrowing,
            'then the method is only applicable in its expanded form.
            outConvKind = Conversions.ClassifyConversion(expression, targetType, binder, useSiteInfo)

            ' Note, user-defined conversions are acceptable here.
            If Conversions.IsWideningConversion(outConvKind.Key) Then
                Return True
            End If

            ' Dev10 allows explicitly converted NOTHING as an argument for a ParamArray parameter,
            ' even if conversion to the array type is narrowing.
            If IsNothingLiteral(expression) Then
                Debug.Assert(Conversions.IsNarrowingConversion(outConvKind.Key))
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Performs an initial pass through the group of candidates and does
        ''' the following in the process.
        ''' 1) Eliminates candidates based on the number of supplied arguments and number of supplied generic type arguments.
        ''' 2) Adds additional entries for expanded ParamArray forms when applicable.
        ''' 3) Infers method's generic type arguments if needed.
        ''' 4) Substitutes method's generic type arguments.
        ''' 5) Eliminates candidates based on shadowing by signature.
        '''    This partially takes care of §11.8.1 Overloaded Method Resolution, section 7.1.
        '''      If M is defined in a more derived type than N, eliminate N from the set.
        ''' 6) Eliminates candidates with identical virtual signatures by applying various shadowing and
        '''    tie-breaking rules from §11.8.1 Overloaded Method Resolution, section 7.0
        '''     • If M has fewer parameters from an expanded paramarray than N, eliminate N from the set.
        ''' 7) Takes care of unsupported overloading within the same type for instance methods/properties.
        '''
        ''' Assumptions:
        ''' 1) Shadowing by name has been already applied.
        ''' 2) group can include extension methods.
        ''' 3) group contains original definitions, i.e. method type arguments have not been substituted yet.
        '''    Exception are extension methods with type parameters substituted based on receiver type rather
        '''    than based on type arguments supplied at the call site.
        ''' 4) group contains only accessible candidates.
        ''' 5) group doesn't contain members involved into unsupported overloading, i.e. differ by casing or custom modifiers only.
        ''' 6) group does not contain duplicates.
        ''' 7) All elements of arguments array are Not Nothing, omitted arguments are represented by OmittedArgumentExpression node.
        ''' </summary>
        ''' <remarks>
        ''' This method is destructive to content of the [group] parameter.
        ''' </remarks>
        Private Shared Sub CollectOverloadedCandidates(
            binder As Binder,
            results As ArrayBuilder(Of CandidateAnalysisResult),
            group As ArrayBuilder(Of Candidate),
            typeArguments As ImmutableArray(Of TypeSymbol),
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            delegateReturnType As TypeSymbol,
            delegateReturnTypeReferenceBoundNode As BoundNode,
            includeEliminatedCandidates As Boolean,
            isQueryOperatorInvocation As Boolean,
            forceExpandedForm As Boolean,
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )
            Debug.Assert(results IsNot Nothing)
            Debug.Assert(argumentNames.IsDefault OrElse (argumentNames.Length > 0 AndAlso argumentNames.Length = arguments.Length))

            Dim quickInfo = ArrayBuilder(Of QuickApplicabilityInfo).GetInstance()
            Dim sourceModule As ModuleSymbol = binder.SourceModule

            For i As Integer = 0 To group.Count - 1

                If group(i) Is Nothing Then
                    Continue For
                End If

                Dim info As QuickApplicabilityInfo = DoQuickApplicabilityCheck(group(i), typeArguments, arguments, isQueryOperatorInvocation, forceExpandedForm, useSiteInfo)

                If info.Candidate Is Nothing Then
                    Continue For
                End If

                If info.Candidate.UnderlyingSymbol.ContainingModule Is sourceModule OrElse
                   info.Candidate.IsExtensionMethod Then
                    CollectOverloadedCandidate(results, info, typeArguments, arguments, argumentNames,
                                               delegateReturnType, delegateReturnTypeReferenceBoundNode,
                                               includeEliminatedCandidates, binder, asyncLambdaSubToFunctionMismatch,
                                               useSiteInfo)
                    Continue For
                End If

                ' Deal with VB-illegal overloading in imported types.
                ' We are trying to avoid doing signature comparison as much as possible and limit them to
                ' cases when at least one candidate is applicable based on the quick applicability check.
                ' Similar code exists in overriding checks in OverrideHidingHelper.RemoveMembersWithConflictingAccessibility.

                Dim container As Symbol = info.Candidate.UnderlyingSymbol.ContainingSymbol

                ' If there are more candidates from this type, collect all of them in quickInfo array,
                ' but keep the applicable candidates at the beginning
                quickInfo.Clear()
                quickInfo.Add(info)
                Dim applicableCount As Integer = If(info.State = CandidateAnalysisResultState.Applicable, 1, 0)

                For j As Integer = i + 1 To group.Count - 1
                    If group(j) Is Nothing OrElse
                       group(j).IsExtensionMethod Then ' VS2013 ignores illegal overloading for extension methods
                        Continue For
                    End If

                    If container = group(j).UnderlyingSymbol.ContainingSymbol Then
                        info = DoQuickApplicabilityCheck(group(j), typeArguments, arguments, isQueryOperatorInvocation, forceExpandedForm, useSiteInfo)
                        group(j) = Nothing

                        If info.Candidate Is Nothing Then
                            Continue For
                        End If

                        ' Keep applicable candidates at the beginning.
                        If info.State <> CandidateAnalysisResultState.Applicable Then
                            quickInfo.Add(info)
                        ElseIf applicableCount = quickInfo.Count Then
                            quickInfo.Add(info)
                            applicableCount += 1
                        Else
                            quickInfo.Add(quickInfo(applicableCount))
                            quickInfo(applicableCount) = info
                            applicableCount += 1
                        End If
                    End If
                Next

                ' Now see if any candidates are ambiguous or lose against other candidates in the quickInfo array.
                ' This loop is destructive to the content of the quickInfo, some applicable candidates could be replaced with
                ' a "better" candidate, even though that candidate is not applicable, "losers" are deleted, etc.
                For k As Integer = 0 To If(applicableCount > 0 OrElse Not includeEliminatedCandidates, applicableCount, quickInfo.Count) - 1
                    info = quickInfo(k)

                    If info.Candidate Is Nothing OrElse info.State = CandidateAnalysisResultState.Ambiguous Then
                        Continue For
                    End If

#If DEBUG Then
                    Dim isExtensionMethod As Boolean = info.Candidate.IsExtensionMethod
#End If
                    Dim firstSymbol As Symbol = info.Candidate.UnderlyingSymbol.OriginalDefinition
                    If firstSymbol.IsReducedExtensionMethod() Then
                        firstSymbol = DirectCast(firstSymbol, MethodSymbol).ReducedFrom
                    End If

                    For l As Integer = k + 1 To quickInfo.Count - 1
                        Dim info2 As QuickApplicabilityInfo = quickInfo(l)

                        If info2.Candidate Is Nothing OrElse info2.State = CandidateAnalysisResultState.Ambiguous Then
                            Continue For
                        End If

#If DEBUG Then
                        Debug.Assert(isExtensionMethod = info2.Candidate.IsExtensionMethod)
#End If
                        Dim secondSymbol As Symbol = info2.Candidate.UnderlyingSymbol.OriginalDefinition
                        If secondSymbol.IsReducedExtensionMethod() Then
                            secondSymbol = DirectCast(secondSymbol, MethodSymbol).ReducedFrom
                        End If

                        ' The following check should be similar to the one performed by SourceNamedTypeSymbol.CheckForOverloadsErrors
                        ' However, we explicitly ignore custom modifiers here, since this part is NYI for SourceNamedTypeSymbol.
                        Const significantDifferences As SymbolComparisonResults = SymbolComparisonResults.AllMismatches And
                                                                                  Not SymbolComparisonResults.MismatchesForConflictingMethods

                        Dim comparisonResults As SymbolComparisonResults = OverrideHidingHelper.DetailedSignatureCompare(
                            firstSymbol,
                            secondSymbol,
                            significantDifferences,
                            significantDifferences)

                        ' Signature must be considered equal following VB rules.
                        If comparisonResults = 0 Then
                            Dim accessibilityCmp As Integer = LookupResult.CompareAccessibilityOfSymbolsConflictingInSameContainer(firstSymbol, secondSymbol)

                            If accessibilityCmp > 0 Then
                                ' first wins
                                quickInfo(l) = Nothing

                            ElseIf accessibilityCmp < 0 Then
                                ' second wins
                                quickInfo(k) = info2
                                quickInfo(l) = Nothing
                                firstSymbol = secondSymbol
                                info = info2
                            Else
                                Debug.Assert(accessibilityCmp = 0)
                                info = New QuickApplicabilityInfo(info.Candidate, CandidateAnalysisResultState.Ambiguous)
                                quickInfo(k) = info
                                quickInfo(l) = New QuickApplicabilityInfo(info2.Candidate, CandidateAnalysisResultState.Ambiguous)
                            End If
                        End If
                    Next

                    If info.State <> CandidateAnalysisResultState.Ambiguous Then
                        CollectOverloadedCandidate(results, info, typeArguments, arguments, argumentNames,
                                                   delegateReturnType, delegateReturnTypeReferenceBoundNode,
                                                   includeEliminatedCandidates, binder, asyncLambdaSubToFunctionMismatch,
                                                   useSiteInfo)
                    ElseIf includeEliminatedCandidates Then
                        CollectOverloadedCandidate(results, info, typeArguments, arguments, argumentNames,
                                                   delegateReturnType, delegateReturnTypeReferenceBoundNode,
                                                   includeEliminatedCandidates, binder, asyncLambdaSubToFunctionMismatch,
                                                   useSiteInfo)

                        For l As Integer = k + 1 To quickInfo.Count - 1
                            Dim info2 As QuickApplicabilityInfo = quickInfo(l)

                            If info2.Candidate IsNot Nothing AndAlso info2.State = CandidateAnalysisResultState.Ambiguous Then
                                quickInfo(l) = Nothing
                                CollectOverloadedCandidate(results, info2, typeArguments, arguments, argumentNames,
                                                           delegateReturnType, delegateReturnTypeReferenceBoundNode,
                                                           includeEliminatedCandidates, binder, asyncLambdaSubToFunctionMismatch,
                                                           useSiteInfo)
                            End If
                        Next
                    End If
                Next
            Next

            quickInfo.Free()

#If DEBUG Then
            group.Clear()
#End If
        End Sub

        Private Structure QuickApplicabilityInfo
            Public ReadOnly Candidate As Candidate
            Public ReadOnly State As CandidateAnalysisResultState
            Public ReadOnly AppliesToNormalForm As Boolean
            Public ReadOnly AppliesToParamArrayForm As Boolean

            Public Sub New(
                candidate As Candidate,
                state As CandidateAnalysisResultState,
                Optional appliesToNormalForm As Boolean = True,
                Optional appliesToParamArrayForm As Boolean = True
            )
                Debug.Assert(candidate IsNot Nothing)
                Debug.Assert(appliesToNormalForm OrElse appliesToParamArrayForm)
                Me.Candidate = candidate
                Me.State = state
                Me.AppliesToNormalForm = appliesToNormalForm
                Me.AppliesToParamArrayForm = appliesToParamArrayForm
            End Sub
        End Structure

        Private Shared Function DoQuickApplicabilityCheck(
            candidate As Candidate,
            typeArguments As ImmutableArray(Of TypeSymbol),
            arguments As ImmutableArray(Of BoundExpression),
            isQueryOperatorInvocation As Boolean,
            forceExpandedForm As Boolean,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As QuickApplicabilityInfo
            If isQueryOperatorInvocation AndAlso DirectCast(candidate.UnderlyingSymbol, MethodSymbol).IsSub Then
                ' Subs are never considered as candidates for Query Operators, but method group might have subs in it.
                Return Nothing
            End If

            If candidate.UnderlyingSymbol.HasUnsupportedMetadata Then
                Return New QuickApplicabilityInfo(candidate, CandidateAnalysisResultState.HasUnsupportedMetadata)
            End If

            ' If type arguments have been supplied, eliminate procedures that don't have an
            ' appropriate number of type parameters.

            '§11.8.2 Applicable Methods
            'Section 4.
            ' If type arguments have been specified, they are matched against the type parameter list.
            ' If the two lists do not have the same number of elements, the method is not applicable,
            ' unless the type argument list is empty.
            If typeArguments.Length > 0 AndAlso candidate.Arity <> typeArguments.Length Then
                Return New QuickApplicabilityInfo(candidate, CandidateAnalysisResultState.BadGenericArity)
            End If

            ' Eliminate procedures that cannot accept the number of supplied arguments.
            Dim requiredCount As Integer
            Dim maxCount As Integer
            Dim hasParamArray As Boolean

            candidate.GetAllParameterCounts(requiredCount, maxCount, hasParamArray)

            '§11.8.2 Applicable Methods
            'If there are more positional arguments than parameters and the last parameter is not a paramarray,
            'the method is not applicable. Otherwise, the paramarray parameter is expanded with parameters of
            'the paramarray element type to match the number of positional arguments. If a single argument expression
            'matches a paramarray parameter and the type of the argument expression is convertible to both the type of
            'the paramarray parameter and the paramarray element type, the method is applicable in both its expanded
            'and unexpanded forms, with two exceptions. If the conversion from the type of the argument expression to
            'the paramarray type is narrowing, then the method is only applicable in its expanded form. If the argument
            'expression is the literal Nothing, then the method is only applicable in its unexpanded form.
            If isQueryOperatorInvocation Then
                ' Query operators require exact match for argument count.
                If arguments.Length <> maxCount Then
                    Return New QuickApplicabilityInfo(candidate, CandidateAnalysisResultState.ArgumentCountMismatch, True, False)
                End If
            ElseIf arguments.Length < requiredCount OrElse
               (Not hasParamArray AndAlso arguments.Length > maxCount) Then
                Return New QuickApplicabilityInfo(candidate, CandidateAnalysisResultState.ArgumentCountMismatch, Not hasParamArray, hasParamArray)
            End If

            Dim candidateUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = candidate.UnderlyingSymbol.GetUseSiteInfo()

            useSiteInfo.Add(candidateUseSiteInfo)
            If candidateUseSiteInfo.DiagnosticInfo IsNot Nothing Then
                Return New QuickApplicabilityInfo(candidate, CandidateAnalysisResultState.HasUseSiteError)
            End If

            ' A method with a paramarray can be considered in two forms: in an
            ' expanded form or in an unexpanded form (i.e. as if the paramarray
            ' decoration was not specified). It can apply in both forms, as
            ' in the case of passing Object() to ParamArray x As Object() (because
            ' Object() converts to both Object() and Object).

            ' Does the method apply in its unexpanded form? This can only happen if
            ' either there is no paramarray or if the argument count matches exactly
            ' (if it's less, then the paramarray is expanded to nothing, if it's more,
            ' it's expanded to one or more parameters).
            Dim applicableInNormalForm As Boolean = False
            Dim applicableInParamArrayForm As Boolean = False

            If Not hasParamArray OrElse (arguments.Length = maxCount AndAlso Not forceExpandedForm) Then
                applicableInNormalForm = True
            End If

            ' How about it's expanded form? It always applies if there's a paramarray.
            If hasParamArray AndAlso Not isQueryOperatorInvocation Then
                applicableInParamArrayForm = True
            End If

            Return New QuickApplicabilityInfo(candidate, CandidateAnalysisResultState.Applicable, applicableInNormalForm, applicableInParamArrayForm)
        End Function

        Private Shared Sub CollectOverloadedCandidate(
            results As ArrayBuilder(Of CandidateAnalysisResult),
            candidate As QuickApplicabilityInfo,
            typeArguments As ImmutableArray(Of TypeSymbol),
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            delegateReturnType As TypeSymbol,
            delegateReturnTypeReferenceBoundNode As BoundNode,
            includeEliminatedCandidates As Boolean,
            binder As Binder,
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )
            Select Case candidate.State
                Case CandidateAnalysisResultState.HasUnsupportedMetadata
                    If includeEliminatedCandidates Then
                        results.Add(New CandidateAnalysisResult(candidate.Candidate, CandidateAnalysisResultState.HasUnsupportedMetadata))
                    End If

                Case CandidateAnalysisResultState.HasUseSiteError
                    If includeEliminatedCandidates Then
                        results.Add(New CandidateAnalysisResult(candidate.Candidate, CandidateAnalysisResultState.HasUseSiteError))
                    End If

                Case CandidateAnalysisResultState.BadGenericArity
                    If includeEliminatedCandidates Then
                        results.Add(New CandidateAnalysisResult(candidate.Candidate, CandidateAnalysisResultState.BadGenericArity))
                    End If

                Case CandidateAnalysisResultState.ArgumentCountMismatch
                    Debug.Assert(candidate.AppliesToNormalForm <> candidate.AppliesToParamArrayForm)
                    If includeEliminatedCandidates Then

                        Dim candidateAnalysis As New CandidateAnalysisResult(ConstructIfNeedTo(candidate.Candidate, typeArguments), CandidateAnalysisResultState.ArgumentCountMismatch)

                        If candidate.AppliesToParamArrayForm Then
                            candidateAnalysis.SetIsExpandedParamArrayForm()
                        End If

                        results.Add(candidateAnalysis)
                    End If

                Case CandidateAnalysisResultState.Applicable

                    Dim candidateAnalysis As CandidateAnalysisResult

                    If typeArguments.Length > 0 Then
                        candidateAnalysis = New CandidateAnalysisResult(candidate.Candidate.Construct(typeArguments))
                    Else
                        candidateAnalysis = New CandidateAnalysisResult(candidate.Candidate)
                    End If

#If DEBUG Then
                    Dim triedToAddSomething As Boolean = False
#End If

                    If candidate.AppliesToNormalForm Then
#If DEBUG Then
                        triedToAddSomething = True
#End If
                        InferTypeArgumentsIfNeedToAndCombineWithExistingCandidates(results, candidateAnalysis, typeArguments,
                                                                                   arguments, argumentNames,
                                                                                   delegateReturnType, delegateReturnTypeReferenceBoundNode,
                                                                                   binder, asyncLambdaSubToFunctionMismatch,
                                                                                   useSiteInfo)
                    End If

                    ' How about it's expanded form? It always applies if there's a paramarray.
                    If candidate.AppliesToParamArrayForm Then

#If DEBUG Then
                        triedToAddSomething = True
#End If

                        candidateAnalysis.SetIsExpandedParamArrayForm()
                        candidateAnalysis.ExpandedParamArrayArgumentsUsed = Math.Max(arguments.Length - candidate.Candidate.ParameterCount + 1, 0)
                        InferTypeArgumentsIfNeedToAndCombineWithExistingCandidates(results, candidateAnalysis, typeArguments,
                                                                                   arguments, argumentNames,
                                                                                   delegateReturnType, delegateReturnTypeReferenceBoundNode,
                                                                                   binder, asyncLambdaSubToFunctionMismatch,
                                                                                   useSiteInfo)
                    End If

#If DEBUG Then
                    Debug.Assert(triedToAddSomething)
#End If

                Case CandidateAnalysisResultState.Ambiguous
                    If includeEliminatedCandidates Then
                        results.Add(New CandidateAnalysisResult(candidate.Candidate, CandidateAnalysisResultState.Ambiguous))
                    End If

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(candidate.State)
            End Select
        End Sub

        Private Shared Sub InferTypeArgumentsIfNeedToAndCombineWithExistingCandidates(
            results As ArrayBuilder(Of CandidateAnalysisResult),
            newCandidate As CandidateAnalysisResult,
            typeArguments As ImmutableArray(Of TypeSymbol),
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            delegateReturnType As TypeSymbol,
            delegateReturnTypeReferenceBoundNode As BoundNode,
            binder As Binder,
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )

            If typeArguments.Length = 0 AndAlso newCandidate.Candidate.Arity > 0 Then

                '§11.8.2 Applicable Methods
                'Section 4.
                'If the type argument list is empty, type inferencing is used to try and infer the type argument list.
                'If type inferencing fails, the method is not applicable. Otherwise, the type arguments are filled
                'in the place of the type parameters in the signature.

                If Not InferTypeArguments(newCandidate, arguments, argumentNames, delegateReturnType, delegateReturnTypeReferenceBoundNode,
                                          asyncLambdaSubToFunctionMismatch, binder, useSiteInfo) Then
                    Debug.Assert(newCandidate.State <> CandidateAnalysisResultState.Applicable)
                    results.Add(newCandidate)
                    Return
                End If
            End If

            CombineCandidates(results, newCandidate, arguments.Length, argumentNames, useSiteInfo)
        End Sub

        ''' <summary>
        ''' Combine new candidate with the list of existing candidates, applying various shadowing and
        ''' tie-breaking rules. New candidate may or may not be added to the result, some
        ''' existing candidates may be removed from the result.
        ''' </summary>
        Private Shared Sub CombineCandidates(
            results As ArrayBuilder(Of CandidateAnalysisResult),
            newCandidate As CandidateAnalysisResult,
            argumentCount As Integer,
            argumentNames As ImmutableArray(Of String),
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )
            Debug.Assert(newCandidate.State = CandidateAnalysisResultState.Applicable)

            Dim operatorResolution As Boolean = newCandidate.Candidate.IsOperator

            Debug.Assert(newCandidate.Candidate.ParameterCount >= argumentCount OrElse newCandidate.IsExpandedParamArrayForm)
            Debug.Assert(argumentNames.IsDefault OrElse argumentNames.Length > 0)
            Debug.Assert(Not operatorResolution OrElse argumentNames.IsDefault)

            Dim i As Integer = 0

            While i < results.Count
                Dim existingCandidate As CandidateAnalysisResult = results(i)

                ' Skip over some eliminated candidates, which we will be unable to match signature against.
                If existingCandidate.State = CandidateAnalysisResultState.ArgumentCountMismatch OrElse
                   existingCandidate.State = CandidateAnalysisResultState.BadGenericArity OrElse
                   existingCandidate.State = CandidateAnalysisResultState.Ambiguous Then
                    GoTo ContinueCandidatesLoop
                End If

                ' Candidate can't hide another form of itself
                If existingCandidate.Candidate Is newCandidate.Candidate Then
                    Debug.Assert(Not operatorResolution)
                    GoTo ContinueCandidatesLoop
                End If

                Dim existingWins As Boolean = False
                Dim newWins As Boolean = False

                ' An overriding method hides the methods it overrides.
                ' In particular, this rule takes care of bug VSWhidbey #385900. Where type argument inference fails
                ' for an overriding method due to named argument name mismatch, but succeeds for the overridden method
                ' from base (the overridden method uses parameter name matching the named argument name). At the end,
                ' however, the overriding method is called, even though it doesn't have parameter with matching name.
                ' Also helps with methods overridden by restricted types (TypedReference, etc.), ShadowBasedOnReceiverType
                ' doesn't do the job for them because it relies on Conversions.ClassifyDirectCastConversion, which
                ' disallows boxing conversion for restricted types.
                If Not operatorResolution AndAlso ShadowBasedOnOverriding(existingCandidate, newCandidate, existingWins, newWins) Then
                    GoTo DeterminedTheWinner
                End If

                If existingCandidate.State = CandidateAnalysisResultState.TypeInferenceFailed OrElse existingCandidate.SomeInferenceFailed OrElse
                   existingCandidate.State = CandidateAnalysisResultState.HasUseSiteError OrElse
                   existingCandidate.State = CandidateAnalysisResultState.HasUnsupportedMetadata Then
                    ' Won't be able to match signature.
                    GoTo ContinueCandidatesLoop
                End If

                ' It looks like the following code is applying some tie-breaking rules from section 7 of
                ' §11.8.1 Overloaded Method Resolution, but not all of them and even skips ParamArrays tie-breaking
                ' rule in some scenarios. I couldn't find an explanation of this behavior in the spec and
                ' simply tried to keep this code close to Dev10.

                ' Spec says that the tie-breaking rules should be applied only for members equally applicable to the argument list.
                ' [§11.8.1.1 Applicability] defines equally applicable members as follows:
                ' A member M is considered equally applicable as N if
                ' 1) their signatures are the same or
                ' 2) if each parameter type in M is the same as the corresponding parameter type in N.

                ' We can always check if signature is the same, but we cannot check the second condition in presence
                ' of named arguments because for them we don't know yet which parameter in M corresponds to which
                ' parameter in N.

                Debug.Assert(existingCandidate.Candidate.ParameterCount >= argumentCount OrElse existingCandidate.IsExpandedParamArrayForm)

                If argumentNames.IsDefault Then
                    Dim existingParamIndex As Integer = 0
                    Dim newParamIndex As Integer = 0

                    'CONSIDER: Can we somehow merge this with the complete signature comparison?
                    For j As Integer = 0 To argumentCount - 1 Step 1

                        Dim existingType As TypeSymbol = GetParameterTypeFromVirtualSignature(existingCandidate, existingParamIndex)
                        Dim newType As TypeSymbol = GetParameterTypeFromVirtualSignature(newCandidate, newParamIndex)

                        If Not existingType.IsSameTypeIgnoringAll(newType) Then
                            ' Signatures are different, shadowing rules do not apply
                            GoTo ContinueCandidatesLoop
                        End If

                        ' Advance to the next parameter in the existing candidate,
                        ' unless we are on the expanded ParamArray parameter.
                        AdvanceParameterInVirtualSignature(existingCandidate, existingParamIndex)

                        ' Advance to the next parameter in the new candidate,
                        ' unless we are on the expanded ParamArray parameter.
                        AdvanceParameterInVirtualSignature(newCandidate, newParamIndex)
                    Next
                Else
                    Debug.Assert(Not operatorResolution)
                End If

                Dim signatureMatch As Boolean = True

                ' Compare complete signature, with no regard to arguments
                If existingCandidate.Candidate.ParameterCount <> newCandidate.Candidate.ParameterCount Then
                    Debug.Assert(Not operatorResolution)
                    signatureMatch = False
                ElseIf operatorResolution Then
                    Debug.Assert(argumentCount = existingCandidate.Candidate.ParameterCount)
                    Debug.Assert(signatureMatch)

                    ' Not lifted operators are preferred over lifted.
                    If existingCandidate.Candidate.IsLifted Then
                        If Not newCandidate.Candidate.IsLifted Then
                            newWins = True
                            GoTo DeterminedTheWinner
                        End If
                    ElseIf newCandidate.Candidate.IsLifted Then
                        Debug.Assert(Not existingCandidate.Candidate.IsLifted)
                        existingWins = True
                        GoTo DeterminedTheWinner
                    End If
                Else
                    For j As Integer = 0 To existingCandidate.Candidate.ParameterCount - 1 Step 1

                        Dim existingType As TypeSymbol = existingCandidate.Candidate.Parameters(j).Type
                        Dim newType As TypeSymbol = newCandidate.Candidate.Parameters(j).Type

                        If Not existingType.IsSameTypeIgnoringAll(newType) Then
                            signatureMatch = False
                            Exit For
                        End If
                    Next
                End If

                If Not argumentNames.IsDefault AndAlso Not signatureMatch Then
                    ' Signatures are different, shadowing rules do not apply
                    GoTo ContinueCandidatesLoop
                End If

                If Not signatureMatch Then
                    Debug.Assert(argumentNames.IsDefault)

                    ' If we have gotten to this point it means that the 2 procedures have equal specificity,
                    ' but signatures that do not match exactly (after generic substitution). This
                    ' implies that we are dealing with differences in shape due to param arrays
                    ' or optional arguments.
                    ' So we look and see if one procedure maps fewer arguments to the
                    ' param array than the other. The one using more, is then shadowed by the one using less.

                    '•	If M has fewer parameters from an expanded paramarray than N, eliminate N from the set.
                    If ShadowBasedOnParamArrayUsage(existingCandidate, newCandidate, existingWins, newWins) Then
                        GoTo DeterminedTheWinner
                    End If

                Else
                    ' The signatures of the two methods match (after generic parameter substitution).
                    ' This means that param array shadowing doesn't come into play.
                    ' !!! Why? Where is this mentioned in the spec?
                End If

                Debug.Assert(argumentNames.IsDefault OrElse signatureMatch)

                ' In presence of named arguments, the following shadowing rules
                ' cannot be applied if any candidate is extension method because
                ' full signature match doesn't guarantee equal applicability (in presence of named arguments)
                ' and instance methods hide by signature regardless applicability rules do not apply to extension methods.
                If argumentNames.IsDefault OrElse
                   Not (existingCandidate.Candidate.IsExtensionMethod OrElse newCandidate.Candidate.IsExtensionMethod) Then

                    '7.1.	If M is defined in a more derived type than N, eliminate N from the set.
                    '       This rule also applies to the types that extension methods are defined on.
                    '7.2.	If M and N are extension methods and the target type of M is a class or
                    '       structure and the target type of N is an interface, eliminate N from the set.
                    If ShadowBasedOnReceiverType(existingCandidate, newCandidate, existingWins, newWins, useSiteInfo) Then
                        GoTo DeterminedTheWinner
                    End If

                    '7.3.	If M and N are extension methods and the target type of M has fewer type
                    '       parameters than the target type of N, eliminate N from the set.
                    '       !!! Note that spec talks about "fewer type parameters", but it is not really about count.
                    '       !!! It is about one refers to a type parameter and the other one doesn't.
                    If ShadowBasedOnExtensionMethodTargetTypeGenericity(existingCandidate, newCandidate, existingWins, newWins) Then
                        GoTo DeterminedTheWinner
                    End If
                End If

DeterminedTheWinner:
                Debug.Assert(Not existingWins OrElse Not newWins) ' Both cannot win!

                If newWins Then
                    ' Remove existing
                    results.RemoveAt(i)

                    ' We should continue the loop because at least with
                    ' extension methods in the picture, there could be other
                    ' winners and losers in the results.
                    ' Since we removed the element, we should bypass index increment.
                    Continue While

                ElseIf existingWins Then
                    ' New candidate lost, shouldn't add it.
                    Return
                End If

ContinueCandidatesLoop:
                i += 1
            End While

            results.Add(newCandidate)
        End Sub

        Private Shared Function ShadowBasedOnOverriding(
            existingCandidate As CandidateAnalysisResult, newCandidate As CandidateAnalysisResult,
            ByRef existingWins As Boolean, ByRef newWins As Boolean
        ) As Boolean
            Dim existingSymbol As Symbol = existingCandidate.Candidate.UnderlyingSymbol
            Dim newSymbol As Symbol = newCandidate.Candidate.UnderlyingSymbol
            Dim existingType As NamedTypeSymbol = existingSymbol.ContainingType
            Dim newType As NamedTypeSymbol = newSymbol.ContainingType

            ' Optimization: We will rely on ShadowBasedOnReceiverType to give us the
            '               same effect later on for cases when existingCandidate is
            '               applicable and neither candidate is from restricted type.
            Dim existingIsApplicable As Boolean = (existingCandidate.State = CandidateAnalysisResultState.Applicable)

            If existingIsApplicable AndAlso Not existingType.IsRestrictedType() AndAlso Not newType.IsRestrictedType() Then
                Return False
            End If

            ' Optimization: symbols from the same type can't override each other.
            ' ShadowBasedOnReceiverType
            If existingType.OriginalDefinition IsNot newType.OriginalDefinition Then
                If newCandidate.Candidate.IsOverriddenBy(existingSymbol) Then
                    existingWins = True
                    Return True

                ElseIf existingIsApplicable AndAlso existingCandidate.Candidate.IsOverriddenBy(newSymbol) Then
                    newWins = True
                    Return True
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        '''    7.5.	If M is not an extension method and N is, eliminate N from the set.
        '''    7.6.	If M and N are extension methods and M was found before N, eliminate N from the set.
        ''' </summary>
        Private Shared Function ShadowBasedOnExtensionVsInstanceAndPrecedence(
            left As CandidateAnalysisResult, right As CandidateAnalysisResult,
            ByRef leftWins As Boolean, ByRef rightWins As Boolean
        ) As Boolean
            If left.Candidate.IsExtensionMethod Then
                If Not right.Candidate.IsExtensionMethod Then
                    '7.5.
                    rightWins = True
                    Return True

                Else
                    ' Both are extensions
                    If left.Candidate.PrecedenceLevel < right.Candidate.PrecedenceLevel Then
                        '7.6.
                        leftWins = True
                        Return True
                    ElseIf left.Candidate.PrecedenceLevel > right.Candidate.PrecedenceLevel Then
                        '7.6.
                        rightWins = True
                        Return True
                    End If
                End If

            ElseIf right.Candidate.IsExtensionMethod Then
                '7.5.
                leftWins = True
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        '''    7.4.	If M is less generic than N, eliminate N from the set.
        ''' </summary>
        Private Shared Function ShadowBasedOnGenericity(
            left As CandidateAnalysisResult, right As CandidateAnalysisResult,
            ByRef leftWins As Boolean, ByRef rightWins As Boolean,
            arguments As ImmutableArray(Of BoundExpression),
            binder As Binder
        ) As Boolean

            ' §11.8.1.2 Genericity
            ' A member M is determined to be less generic than a member N as follows:
            '
            ' 1. If, for each pair of matching parameters Mj and Nj, Mj is less or equally generic than Nj
            '    with respect to type parameters on the method, and at least one Mj is less generic with
            '    respect to type parameters on the method.
            ' 2. Otherwise, if for each pair of matching parameters Mj and Nj, Mj is less or equally generic
            '    than Nj with respect to type parameters on the type, and at least one Mj is less generic with
            '    respect to type parameters on the type, then M is less generic than N.
            '
            ' A parameter M is considered to be equally generic to a parameter N if their types Mt and Nt
            ' both refer to type parameters or both don't refer to type parameters. M is considered to be less
            ' generic than N if Mt does not refer to a type parameter and Nt does.
            '
            ' Extension method type parameters that were fixed during currying are considered type parameters on the type,
            ' not type parameters on the method.

            ' At the beginning we will track both method and type type parameters.
            Dim track As TypeParameterKind = TypeParameterKind.Both

            If Not (left.Candidate.IsGeneric OrElse right.Candidate.IsGeneric) Then
                track = track And (Not TypeParameterKind.Method)
            End If

            If Not ((left.Candidate.UnderlyingSymbol.ContainingType.IsOrInGenericType() OrElse
                     (left.Candidate.IsExtensionMethod AndAlso Not left.Candidate.FixedTypeParameters.IsNull)) OrElse
                    (right.Candidate.UnderlyingSymbol.ContainingType.IsOrInGenericType() OrElse
                     (right.Candidate.IsExtensionMethod AndAlso Not right.Candidate.FixedTypeParameters.IsNull))) Then
                track = track And (Not TypeParameterKind.Type)
            End If

            If track = TypeParameterKind.None Then
                Return False ' There is no winner.
            End If

#If DEBUG Then
            Dim saveTrack = track
#End If

            Dim leftHasLeastGenericParameterAgainstMethod As Boolean = False
            Dim leftHasLeastGenericParameterAgainstType As Boolean = False

            Dim rightHasLeastGenericParameterAgainstMethod As Boolean = False
            Dim rightHasLeastGenericParameterAgainstType As Boolean = False

            Dim leftParamIndex As Integer = 0
            Dim rightParamIndex As Integer = 0

            For i = 0 To arguments.Length - 1 Step 1

                Dim leftParamType As TypeSymbol
                Dim leftParamTypeForGenericityCheck As TypeSymbol = Nothing

                Debug.Assert(left.ArgsToParamsOpt.IsDefault = right.ArgsToParamsOpt.IsDefault)

                If left.ArgsToParamsOpt.IsDefault Then
                    leftParamType = GetParameterTypeFromVirtualSignature(left, leftParamIndex, leftParamTypeForGenericityCheck)
                    AdvanceParameterInVirtualSignature(left, leftParamIndex)
                Else
                    leftParamType = GetParameterTypeFromVirtualSignature(left, left.ArgsToParamsOpt(i), leftParamTypeForGenericityCheck)
                End If

                Dim rightParamType As TypeSymbol
                Dim rightParamTypeForGenericityCheck As TypeSymbol = Nothing

                If right.ArgsToParamsOpt.IsDefault Then
                    rightParamType = GetParameterTypeFromVirtualSignature(right, rightParamIndex, rightParamTypeForGenericityCheck)
                    AdvanceParameterInVirtualSignature(right, rightParamIndex)
                Else
                    rightParamType = GetParameterTypeFromVirtualSignature(right, right.ArgsToParamsOpt(i), rightParamTypeForGenericityCheck)
                End If

                ' Parameters matching omitted arguments do not participate.
                If arguments(i).Kind = BoundKind.OmittedArgument Then
                    Continue For
                End If

                If SignatureMismatchForThePurposeOfShadowingBasedOnGenericity(leftParamType, rightParamType, arguments(i), binder) Then
                    Return False
                End If

                Dim leftRefersTo As TypeParameterKind = DetectReferencesToGenericParameters(leftParamTypeForGenericityCheck, track, left.Candidate.FixedTypeParameters)
                Dim rightRefersTo As TypeParameterKind = DetectReferencesToGenericParameters(rightParamTypeForGenericityCheck, track, right.Candidate.FixedTypeParameters)

                ' Still looking for less generic with respect to type parameters on the method.
                If (track And TypeParameterKind.Method) <> 0 Then
                    If (leftRefersTo And TypeParameterKind.Method) = 0 Then
                        If (rightRefersTo And TypeParameterKind.Method) <> 0 Then
                            leftHasLeastGenericParameterAgainstMethod = True
                        End If
                    ElseIf (rightRefersTo And TypeParameterKind.Method) = 0 Then
                        rightHasLeastGenericParameterAgainstMethod = True
                    End If

                    ' If both won at least once, neither candidate is less generic with respect to type parameters on the method.
                    ' Stop checking for this.
                    If leftHasLeastGenericParameterAgainstMethod AndAlso rightHasLeastGenericParameterAgainstMethod Then
                        track = track And (Not TypeParameterKind.Method)
                    End If
                End If

                ' Still looking for less generic with respect to type parameters on the type.
                If (track And TypeParameterKind.Type) <> 0 Then
                    If (leftRefersTo And TypeParameterKind.Type) = 0 Then
                        If (rightRefersTo And TypeParameterKind.Type) <> 0 Then
                            leftHasLeastGenericParameterAgainstType = True
                        End If
                    ElseIf (rightRefersTo And TypeParameterKind.Type) = 0 Then
                        rightHasLeastGenericParameterAgainstType = True
                    End If

                    ' If both won at least once, neither candidate is less generic with respect to type parameters on the type.
                    ' Stop checking for this.
                    If leftHasLeastGenericParameterAgainstType AndAlso rightHasLeastGenericParameterAgainstType Then
                        track = track And (Not TypeParameterKind.Type)
                    End If
                End If

                ' Are we still looking for a winner?
                If track = TypeParameterKind.None Then
#If DEBUG Then
                    Debug.Assert((saveTrack And TypeParameterKind.Method) = 0 OrElse (leftHasLeastGenericParameterAgainstMethod AndAlso rightHasLeastGenericParameterAgainstMethod))
                    Debug.Assert((saveTrack And TypeParameterKind.Type) = 0 OrElse (leftHasLeastGenericParameterAgainstType AndAlso rightHasLeastGenericParameterAgainstType))
#End If
                    Return False ' There is no winner.
                End If
            Next

            If leftHasLeastGenericParameterAgainstMethod Then
                If Not rightHasLeastGenericParameterAgainstMethod Then
                    leftWins = True
                    Return True
                End If
            ElseIf rightHasLeastGenericParameterAgainstMethod Then
                rightWins = True
                Return True
            End If

            If leftHasLeastGenericParameterAgainstType Then
                If Not rightHasLeastGenericParameterAgainstType Then
                    leftWins = True
                    Return True
                End If
            ElseIf rightHasLeastGenericParameterAgainstType Then
                rightWins = True
                Return True
            End If

            Return False
        End Function

        Private Shared Function SignatureMismatchForThePurposeOfShadowingBasedOnGenericity(
            leftParamType As TypeSymbol,
            rightParamType As TypeSymbol,
            argument As BoundExpression,
            binder As Binder
        ) As Boolean
            Debug.Assert(argument.Kind <> BoundKind.OmittedArgument)

            ' See Semantics::CompareGenericityIsSignatureMismatch in native compiler.

            If leftParamType.IsSameTypeIgnoringAll(rightParamType) Then
                Return False
            Else
                ' Note: Undocumented rule.
                ' Different types. We still consider them the same if they are delegates with
                ' equivalent signatures, after possibly unwrapping Expression(Of D).
                Dim leftIsExpressionTree As Boolean, rightIsExpressionTree As Boolean
                Dim leftDelegateType As NamedTypeSymbol = leftParamType.DelegateOrExpressionDelegate(binder, leftIsExpressionTree)
                Dim rightDelegateType As NamedTypeSymbol = rightParamType.DelegateOrExpressionDelegate(binder, rightIsExpressionTree)

                ' Native compiler will only compare D1 and D2 for Expression(Of D1) and D2 if the argument is a lambda. It will compare
                ' Expression(Of D1) and Expression (Of D2) regardless of the argument.
                If leftDelegateType IsNot Nothing AndAlso rightDelegateType IsNot Nothing AndAlso
                    ((leftIsExpressionTree = rightIsExpressionTree) OrElse argument.IsAnyLambda()) Then

                    Dim leftInvoke = leftDelegateType.DelegateInvokeMethod
                    Dim rightInvoke = rightDelegateType.DelegateInvokeMethod
                    If leftInvoke IsNot Nothing AndAlso rightInvoke IsNot Nothing AndAlso
                       MethodSignatureComparer.ParametersAndReturnTypeSignatureComparer.Equals(leftInvoke, rightInvoke) Then
                        Return False
                    End If
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1.3 Depth of genericity
        ''' </summary>
        Private Shared Function ShadowBasedOnDepthOfGenericity(
            left As CandidateAnalysisResult, right As CandidateAnalysisResult,
            ByRef leftWins As Boolean, ByRef rightWins As Boolean,
            arguments As ImmutableArray(Of BoundExpression),
            binder As Binder
        ) As Boolean

            ' §11.8.1.3 Depth of Genericity
            ' A member M is determined to have greater depth of genericity than a member N if, for each pair
            ' of matching parameters  Mj and Nj, Mj has greater or equal depth of genericity than Nj, and at
            ' least one Mj has greater depth of genericity. Depth of genericity is defined as follows:
            '
            '    1. Anything other than a type parameter has greater depth of genericity than a type parameter;
            '    2. Recursively, a constructed type has greater depth of genericity than another constructed type
            '       (with the same number of type arguments) if at least one type argument has greater depth
            '       of genericity and no type argument has less depth than the corresponding type argument in the other.
            '    3. An array type has greater depth of genericity than another array type (with the same number
            '       of dimensions) if the element type of the first has greater depth of genericity than the
            '       element type of the second.
            '
            ' For example:
            '
            '        Module Test
            '            Sub f(Of T)(x As Task(Of T))
            '            End Sub
            '            Sub f(Of T)(x As T)
            '            End Sub
            '            Sub Main()
            '                Dim x As Task(Of Integer) = Nothing
            '                f(x)            ' Calls the first overload
            '            End Sub
            '        End Module

            Dim leftParamIndex As Integer = 0
            Dim rightParamIndex As Integer = 0

            For i = 0 To arguments.Length - 1 Step 1

                Dim leftParamType As TypeSymbol
                Dim leftParamTypeForGenericityCheck As TypeSymbol = Nothing

                Debug.Assert(left.ArgsToParamsOpt.IsDefault = right.ArgsToParamsOpt.IsDefault)

                If left.ArgsToParamsOpt.IsDefault Then
                    leftParamType = GetParameterTypeFromVirtualSignature(left, leftParamIndex, leftParamTypeForGenericityCheck)
                    AdvanceParameterInVirtualSignature(left, leftParamIndex)
                Else
                    leftParamType = GetParameterTypeFromVirtualSignature(left, left.ArgsToParamsOpt(i), leftParamTypeForGenericityCheck)
                End If

                Dim rightParamType As TypeSymbol
                Dim rightParamTypeForGenericityCheck As TypeSymbol = Nothing

                If right.ArgsToParamsOpt.IsDefault Then
                    rightParamType = GetParameterTypeFromVirtualSignature(right, rightParamIndex, rightParamTypeForGenericityCheck)
                    AdvanceParameterInVirtualSignature(right, rightParamIndex)
                Else
                    rightParamType = GetParameterTypeFromVirtualSignature(right, right.ArgsToParamsOpt(i), rightParamTypeForGenericityCheck)
                End If

                ' Parameters matching omitted arguments do not participate.
                If arguments(i).Kind = BoundKind.OmittedArgument Then
                    Continue For
                End If

                If SignatureMismatchForThePurposeOfShadowingBasedOnGenericity(leftParamType, rightParamType, arguments(i), binder) Then
                    Return False ' no winner if the types of the parameter are different
                End If

                Dim leftParamWins As Boolean = False
                Dim rightParamWins As Boolean = False

                If CompareParameterTypeGenericDepth(leftParamTypeForGenericityCheck, rightParamTypeForGenericityCheck, leftParamWins, rightParamWins) Then
                    Debug.Assert(leftParamWins <> rightParamWins)
                    If leftParamWins Then

                        If rightWins Then
                            rightWins = False
                            Return False ' both won
                        Else
                            leftWins = True
                        End If

                    Else
                        If leftWins Then
                            leftWins = False
                            Return False ' both won
                        Else
                            rightWins = True
                        End If
                    End If
                End If
            Next

            Debug.Assert(Not leftWins OrElse Not rightWins)
            Return leftWins OrElse rightWins
        End Function

        ''' <summary>
        '''
        ''' </summary>
        ''' <returns>False if node of candidates wins</returns>
        Private Shared Function CompareParameterTypeGenericDepth(leftType As TypeSymbol, rightType As TypeSymbol,
                                                                 ByRef leftWins As Boolean, ByRef rightWins As Boolean) As Boolean
            ' Depth of genericity is defined as follows:
            '   1. Anything other than a type parameter has greater depth of genericity than a type parameter;
            '   2. Recursively, a constructed type has greater depth of genericity than another constructed
            '      type (with the same number of type arguments) if at least one type argument has greater
            '      depth of genericity and no type argument has less depth than the corresponding type
            '      argument in the other.
            '   3. An array type has greater depth of genericity than another array type (with the same number
            '      of dimensions) if the element type of the first has greater depth of genericity than the
            '      element type of the second.
            '
            ' For exact rules see Dev11 OverloadResolution.cpp: void Semantics::CompareParameterTypeGenericDepth(...)

            If leftType Is rightType Then
                Return False
            End If

            If leftType.IsTypeParameter Then
                If rightType.IsTypeParameter Then
                    ' Both type parameters => no winner
                    Return False
                Else
                    ' Left is a type parameter, but right is not
                    rightWins = True
                    Return True
                End If

            ElseIf rightType.IsTypeParameter Then
                ' Right is a type parameter, but left is not
                leftWins = True
                Return True
            End If

            ' None of the two is a type parameter

            If leftType.IsArrayType AndAlso rightType.IsArrayType Then
                ' Both are arrays
                Dim leftArray = DirectCast(leftType, ArrayTypeSymbol)
                Dim rightArray = DirectCast(rightType, ArrayTypeSymbol)
                If leftArray.HasSameShapeAs(rightArray) Then
                    Return CompareParameterTypeGenericDepth(leftArray.ElementType, rightArray.ElementType, leftWins, rightWins)
                End If
            End If

            ' Both are generics
            If leftType.Kind = SymbolKind.NamedType AndAlso rightType.Kind = SymbolKind.NamedType Then
                Dim leftNamedType = DirectCast(leftType.GetTupleUnderlyingTypeOrSelf(), NamedTypeSymbol)
                Dim rightNamedType = DirectCast(rightType.GetTupleUnderlyingTypeOrSelf(), NamedTypeSymbol)

                ' If their arities are equal
                If leftNamedType.Arity = rightNamedType.Arity Then
                    Dim leftTypeArguments As ImmutableArray(Of TypeSymbol) = leftNamedType.TypeArgumentsNoUseSiteDiagnostics
                    Dim rightTypeArguments As ImmutableArray(Of TypeSymbol) = rightNamedType.TypeArgumentsNoUseSiteDiagnostics

                    For i = 0 To leftTypeArguments.Length - 1
                        Dim leftArgWins As Boolean = False
                        Dim rightArgWins As Boolean = False

                        If CompareParameterTypeGenericDepth(leftTypeArguments(i), rightTypeArguments(i), leftArgWins, rightArgWins) Then
                            Debug.Assert(leftArgWins <> rightArgWins)

                            If leftArgWins Then

                                If rightWins Then
                                    rightWins = False
                                    Return False
                                Else
                                    leftWins = True
                                End If

                            Else
                                If leftWins Then
                                    leftWins = False
                                    Return False
                                Else
                                    rightWins = True
                                End If
                            End If
                        End If
                    Next

                    Debug.Assert(Not leftWins OrElse Not rightWins)
                    Return leftWins OrElse rightWins
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        '''    7.3.	If M and N are extension methods and the target type of M has fewer type
        '''         parameters than the target type of N, eliminate N from the set.
        '''         !!! Note that spec talks about "fewer type parameters", but it is not really about count.
        '''         !!! It is about one refers to a type parameter and the other one doesn't.
        ''' </summary>
        Private Shared Function ShadowBasedOnExtensionMethodTargetTypeGenericity(
            left As CandidateAnalysisResult, right As CandidateAnalysisResult,
            ByRef leftWins As Boolean, ByRef rightWins As Boolean
        ) As Boolean
            If Not left.Candidate.IsExtensionMethod OrElse Not right.Candidate.IsExtensionMethod Then
                Return False
            End If

            '!!! Note, the spec does not mention this explicitly, but this rule applies only if receiver type
            '!!! is the same for both methods.
            If Not left.Candidate.ReceiverType.IsSameTypeIgnoringAll(right.Candidate.ReceiverType) Then
                Return False
            End If

            ' Only interested in method type parameters.
            Dim leftRefersToATypeParameter = DetectReferencesToGenericParameters(left.Candidate.ReceiverTypeDefinition,
                                                                                 TypeParameterKind.Method,
                                                                                 BitVector.Null)

            ' Only interested in method type parameters.
            Dim rightRefersToATypeParameter = DetectReferencesToGenericParameters(right.Candidate.ReceiverTypeDefinition,
                                                                                  TypeParameterKind.Method,
                                                                                 BitVector.Null)

            If (leftRefersToATypeParameter And TypeParameterKind.Method) <> 0 Then
                If (rightRefersToATypeParameter And TypeParameterKind.Method) = 0 Then
                    rightWins = True
                    Return True
                End If
            ElseIf (rightRefersToATypeParameter And TypeParameterKind.Method) <> 0 Then
                leftWins = True
                Return True
            End If

            Return False
        End Function

        <Flags()>
        Private Enum TypeParameterKind
            None = 0
            Method = 1 << 0
            Type = 1 << 1
            Both = Method Or Type
        End Enum

        Private Shared Function DetectReferencesToGenericParameters(
            symbol As NamedTypeSymbol,
            track As TypeParameterKind,
            methodTypeParametersToTreatAsTypeTypeParameters As BitVector
        ) As TypeParameterKind
            Dim result As TypeParameterKind = TypeParameterKind.None

            Do
                If symbol Is symbol.OriginalDefinition Then
                    If (track And TypeParameterKind.Type) = 0 Then
                        Return result
                    End If

                    If symbol.Arity > 0 Then
                        Return result Or TypeParameterKind.Type
                    End If
                Else
                    For Each argument As TypeSymbol In symbol.TypeArgumentsNoUseSiteDiagnostics
                        result = result Or DetectReferencesToGenericParameters(argument, track,
                                                                               methodTypeParametersToTreatAsTypeTypeParameters)

                        If (result And track) = track Then
                            Return result
                        End If
                    Next
                End If

                symbol = symbol.ContainingType
            Loop While symbol IsNot Nothing

            Return result
        End Function

        Private Shared Function DetectReferencesToGenericParameters(
            symbol As TypeParameterSymbol,
            track As TypeParameterKind,
            methodTypeParametersToTreatAsTypeTypeParameters As BitVector
        ) As TypeParameterKind

            If symbol.ContainingSymbol.Kind = SymbolKind.NamedType Then
                If (track And TypeParameterKind.Type) <> 0 Then
                    Return TypeParameterKind.Type
                End If
            Else
                If methodTypeParametersToTreatAsTypeTypeParameters.IsNull OrElse Not methodTypeParametersToTreatAsTypeTypeParameters(symbol.Ordinal) Then
                    If (track And TypeParameterKind.Method) <> 0 Then
                        Return TypeParameterKind.Method
                    End If
                Else
                    If (track And TypeParameterKind.Type) <> 0 Then
                        Return TypeParameterKind.Type
                    End If
                End If
            End If

            Return TypeParameterKind.None
        End Function

        Private Shared Function DetectReferencesToGenericParameters(
            this As TypeSymbol,
            track As TypeParameterKind,
            methodTypeParametersToTreatAsTypeTypeParameters As BitVector
        ) As TypeParameterKind
            Select Case this.Kind

                Case SymbolKind.TypeParameter

                    Return DetectReferencesToGenericParameters(DirectCast(this, TypeParameterSymbol), track,
                                                               methodTypeParametersToTreatAsTypeTypeParameters)
                Case SymbolKind.ArrayType

                    Return DetectReferencesToGenericParameters(DirectCast(this, ArrayTypeSymbol).ElementType, track,
                                                               methodTypeParametersToTreatAsTypeTypeParameters)

                Case SymbolKind.NamedType, SymbolKind.ErrorType

                    Return DetectReferencesToGenericParameters(DirectCast(this, NamedTypeSymbol), track,
                                                               methodTypeParametersToTreatAsTypeTypeParameters)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(this.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        '''    7.1.	If M is defined in a more derived type than N, eliminate N from the set.
        '''         This rule also applies to the types that extension methods are defined on.
        '''    7.2.	If M and N are extension methods and the target type of M is a class or
        '''         structure and the target type of N is an interface, eliminate N from the set.
        ''' </summary>
        Private Shared Function ShadowBasedOnReceiverType(
            left As CandidateAnalysisResult, right As CandidateAnalysisResult,
            ByRef leftWins As Boolean, ByRef rightWins As Boolean,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Boolean

            Dim leftType = left.Candidate.ReceiverType
            Dim rightType = right.Candidate.ReceiverType

            If Not leftType.IsSameTypeIgnoringAll(rightType) Then
                If DoesReceiverMatchInstance(leftType, rightType, useSiteInfo) Then
                    leftWins = True
                    Return True
                ElseIf DoesReceiverMatchInstance(rightType, leftType, useSiteInfo) Then
                    rightWins = True
                    Return True
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' For a receiver to match an instance, more or less, the type of that instance has to be convertible
        ''' to the type of the receiver with the same bit-representation (i.e. identity on value-types
        ''' and reference-convertibility on reference types).
        ''' Actually, we don't include the reference-convertibilities that seem nonsensical, e.g. enum() to underlyingtype()
        ''' We do include inheritance, implements and variance conversions amongst others.
        ''' </summary>
        Public Shared Function DoesReceiverMatchInstance(instanceType As TypeSymbol, receiverType As TypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Return Conversions.HasWideningDirectCastConversionButNotEnumTypeConversion(instanceType, receiverType, useSiteInfo)
        End Function

        ''' <summary>
        ''' Implements shadowing based on
        ''' §11.8.1 Overloaded Method Resolution.
        ''' •	If M has fewer parameters from an expanded paramarray than N, eliminate N from the set.
        ''' </summary>
        Private Shared Function ShadowBasedOnParamArrayUsage(
            left As CandidateAnalysisResult, right As CandidateAnalysisResult,
            ByRef leftWins As Boolean, ByRef rightWins As Boolean
        ) As Boolean
            If left.IsExpandedParamArrayForm Then
                If right.IsExpandedParamArrayForm Then
                    If left.ExpandedParamArrayArgumentsUsed > right.ExpandedParamArrayArgumentsUsed Then
                        rightWins = True
                        Return True
                    ElseIf left.ExpandedParamArrayArgumentsUsed < right.ExpandedParamArrayArgumentsUsed Then
                        leftWins = True
                        Return True
                    End If

                Else
                    rightWins = True
                    Return True
                End If

            ElseIf right.IsExpandedParamArrayForm Then
                leftWins = True
                Return True
            End If

            Return False
        End Function

        Friend Shared Function GetParameterTypeFromVirtualSignature(
            ByRef candidate As CandidateAnalysisResult,
            paramIndex As Integer
        ) As TypeSymbol
            Dim paramType As TypeSymbol = candidate.Candidate.Parameters(paramIndex).Type

            If candidate.IsExpandedParamArrayForm AndAlso
               paramIndex = candidate.Candidate.ParameterCount - 1 AndAlso
               paramType.Kind = SymbolKind.ArrayType Then
                paramType = DirectCast(paramType, ArrayTypeSymbol).ElementType
            End If

            Return paramType
        End Function

        Private Shared Function GetParameterTypeFromVirtualSignature(
            ByRef candidate As CandidateAnalysisResult,
            paramIndex As Integer,
            ByRef typeForGenericityCheck As TypeSymbol
        ) As TypeSymbol
            Dim param As ParameterSymbol = candidate.Candidate.Parameters(paramIndex)
            Dim paramForGenericityCheck = param.OriginalDefinition

            If paramForGenericityCheck.ContainingSymbol.Kind = SymbolKind.Method Then
                Dim method = DirectCast(paramForGenericityCheck.ContainingSymbol, MethodSymbol)
                If method.IsReducedExtensionMethod Then
                    paramForGenericityCheck = method.ReducedFrom.Parameters(paramIndex + 1)
                End If
            End If

            Dim paramType As TypeSymbol = param.Type
            typeForGenericityCheck = paramForGenericityCheck.Type

            If candidate.IsExpandedParamArrayForm AndAlso
               paramIndex = candidate.Candidate.ParameterCount - 1 AndAlso
               paramType.Kind = SymbolKind.ArrayType Then
                paramType = DirectCast(paramType, ArrayTypeSymbol).ElementType
                typeForGenericityCheck = DirectCast(typeForGenericityCheck, ArrayTypeSymbol).ElementType
            End If

            Return paramType
        End Function

        Friend Shared Sub AdvanceParameterInVirtualSignature(
            ByRef candidate As CandidateAnalysisResult,
            ByRef paramIndex As Integer
        )
            If Not (candidate.IsExpandedParamArrayForm AndAlso
               paramIndex = candidate.Candidate.ParameterCount - 1) Then
                paramIndex += 1
            End If
        End Sub

        Private Shared Function InferTypeArguments(
            ByRef candidate As CandidateAnalysisResult,
            arguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            delegateReturnType As TypeSymbol,
            delegateReturnTypeReferenceBoundNode As BoundNode,
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            binder As Binder,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As Boolean

            Dim parameterToArgumentMap As ArrayBuilder(Of Integer) = Nothing
            Dim paramArrayItems As ArrayBuilder(Of Integer) = Nothing

            BuildParameterToArgumentMap(candidate, arguments, argumentNames, parameterToArgumentMap, paramArrayItems)

            If candidate.State = CandidateAnalysisResultState.Applicable Then

                Dim typeArguments As ImmutableArray(Of TypeSymbol) = Nothing
                Dim inferenceLevel As TypeArgumentInference.InferenceLevel = TypeArgumentInference.InferenceLevel.None
                Dim allFailedInferenceIsDueToObject As Boolean = False
                Dim someInferenceFailed As Boolean = False
                Dim inferenceErrorReasons As InferenceErrorReasons = InferenceErrorReasons.Other
                Dim inferredTypeByAssumption As BitVector = Nothing
                Dim typeArgumentsLocation As ImmutableArray(Of SyntaxNodeOrToken) = Nothing

                Dim inferenceDiagnosticsBag = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, useSiteInfo.AccumulatesDependencies)

                If TypeArgumentInference.Infer(DirectCast(candidate.Candidate.UnderlyingSymbol, MethodSymbol),
                                               arguments, parameterToArgumentMap, paramArrayItems,
                                               delegateReturnType:=delegateReturnType,
                                               delegateReturnTypeReferenceBoundNode:=delegateReturnTypeReferenceBoundNode,
                                               typeArguments:=typeArguments,
                                               inferenceLevel:=inferenceLevel,
                                               someInferenceFailed:=someInferenceFailed,
                                               allFailedInferenceIsDueToObject:=allFailedInferenceIsDueToObject,
                                               inferenceErrorReasons:=inferenceErrorReasons,
                                               inferredTypeByAssumption:=inferredTypeByAssumption,
                                               typeArgumentsLocation:=typeArgumentsLocation,
                                               asyncLambdaSubToFunctionMismatch:=asyncLambdaSubToFunctionMismatch,
                                               useSiteInfo:=useSiteInfo,
                                               diagnostic:=inferenceDiagnosticsBag) Then
                    candidate.SetInferenceLevel(inferenceLevel)
                    candidate.Candidate = candidate.Candidate.Construct(typeArguments)

                    ' Need check for Option Strict and warn if parameter type is an assumed inferred type.
                    If binder.OptionStrict = OptionStrict.On AndAlso Not inferredTypeByAssumption.IsNull Then
                        For i As Integer = 0 To typeArguments.Length - 1 Step 1

                            If inferredTypeByAssumption(i) Then

                                Binder.ReportDiagnostic(inferenceDiagnosticsBag,
                                                        typeArgumentsLocation(i),
                                                        ERRID.WRN_TypeInferenceAssumed3,
                                                        candidate.Candidate.TypeParameters(i),
                                                        DirectCast(candidate.Candidate.UnderlyingSymbol, MethodSymbol).OriginalDefinition,
                                                        typeArguments(i))

                            End If

                        Next

                    End If
                Else
                    candidate.State = CandidateAnalysisResultState.TypeInferenceFailed

                    If someInferenceFailed Then
                        candidate.SetSomeInferenceFailed()
                    End If

                    If allFailedInferenceIsDueToObject Then
                        candidate.SetAllFailedInferenceIsDueToObject()

                        If Not candidate.Candidate.IsExtensionMethod Then
                            candidate.IgnoreExtensionMethods = True
                        End If
                    End If

                    candidate.SetInferenceErrorReasons(inferenceErrorReasons)

                    candidate.NotInferredTypeArguments = BitVector.Create(typeArguments.Length)

                    For i As Integer = 0 To typeArguments.Length - 1 Step 1
                        If typeArguments(i) Is Nothing Then
                            candidate.NotInferredTypeArguments(i) = True
                        End If
                    Next
                End If

                candidate.TypeArgumentInferenceDiagnosticsOpt = inferenceDiagnosticsBag.ToReadOnlyAndFree()

            Else
                candidate.SetSomeInferenceFailed()
            End If

            If paramArrayItems IsNot Nothing Then
                paramArrayItems.Free()
            End If

            If parameterToArgumentMap IsNot Nothing Then
                parameterToArgumentMap.Free()
            End If

            Return (candidate.State = CandidateAnalysisResultState.Applicable)
        End Function

        Private Shared Function ConstructIfNeedTo(candidate As Candidate, typeArguments As ImmutableArray(Of TypeSymbol)) As Candidate
            If typeArguments.Length > 0 Then
                Return candidate.Construct(typeArguments)
            End If

            Return candidate
        End Function

    End Class

End Namespace
