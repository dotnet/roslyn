' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Delegate Function GenerateMethodBody(
        method As EEMethodSymbol,
        diagnostics As DiagnosticBag,
        <Out> ByRef properties As ResultProperties) As BoundStatement

    Friend NotInheritable Class EEMethodSymbol
        Inherits MethodSymbol

        Friend ReadOnly TypeMap As TypeSubstitution
        Friend ReadOnly SubstitutedSourceMethod As MethodSymbol
        Friend ReadOnly Locals As ImmutableArray(Of LocalSymbol)
        Friend ReadOnly LocalsForBinding As ImmutableArray(Of LocalSymbol)

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _container As EENamedTypeSymbol
        Private ReadOnly _name As String
        Private ReadOnly _locations As ImmutableArray(Of Location)
        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _meParameter As ParameterSymbol
        Private ReadOnly _displayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable)
        Private ReadOnly _voidType As NamedTypeSymbol

        ''' <summary>
        ''' Invoked at most once to generate the method body.
        ''' (If the compilation has no errors, it will be invoked
        ''' exactly once, otherwise it may be skipped.)
        ''' </summary>
        Private ReadOnly _generateMethodBody As GenerateMethodBody

        Private _lazyReturnType As TypeSymbol
        Private _lazyResultProperties As ResultProperties

        ' NOTE: This is only used for asserts, so it could be conditional on DEBUG.
        Private ReadOnly _allTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Friend Sub New(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            name As String,
            location As Location,
            sourceMethod As MethodSymbol,
            sourceLocals As ImmutableArray(Of LocalSymbol),
            sourceLocalsForBinding As ImmutableArray(Of LocalSymbol),
            sourceDisplayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable),
            voidType As NamedTypeSymbol,
            generateMethodBody As GenerateMethodBody)

            Debug.Assert(sourceMethod.IsDefinition)
            Debug.Assert(TypeSymbol.Equals(sourceMethod.ContainingType, container.SubstitutedSourceType.OriginalDefinition, TypeCompareKind.ConsiderEverything))
            Debug.Assert(sourceLocals.All(Function(l) l.ContainingSymbol = sourceMethod))

            _compilation = compilation
            _container = container
            _name = name
            _locations = ImmutableArray.Create(location)
            _voidType = voidType

            ' What we want is to map all original type parameters to the corresponding new type parameters
            ' (since the old ones have the wrong owners).  Unfortunately, we have a circular dependency:
            '   1) Each new type parameter requires the entire map in order to be able to construct its constraint list.
            '   2) The map cannot be constructed until all new type parameters exist.
            ' Our solution is to pass each new type parameter a lazy reference to the type map.  We then 
            ' initialize the map as soon as the new type parameters are available - and before they are 
            ' handed out - so that there is never a period where they can require the type map and find
            ' it uninitialized.

            Dim sourceMethodTypeParameters = sourceMethod.TypeParameters
            Dim allSourceTypeParameters = container.SourceTypeParameters.Concat(sourceMethodTypeParameters)

            Dim getTypeMap As New Func(Of TypeSubstitution)(Function() TypeMap)
            _typeParameters = sourceMethodTypeParameters.SelectAsArray(
                Function(tp As TypeParameterSymbol, i As Integer, arg As Object) DirectCast(New EETypeParameterSymbol(Me, tp, i, getTypeMap), TypeParameterSymbol),
                DirectCast(Nothing, Object))
            _allTypeParameters = container.TypeParameters.Concat(_typeParameters)
            Me.TypeMap = TypeSubstitution.Create(sourceMethod, allSourceTypeParameters, ImmutableArrayExtensions.Cast(Of TypeParameterSymbol, TypeSymbol)(_allTypeParameters))

            EENamedTypeSymbol.VerifyTypeParameters(Me, _typeParameters)

            Dim substitutedSourceType = container.SubstitutedSourceType
            Me.SubstitutedSourceMethod = sourceMethod.AsMember(substitutedSourceType)
            If _typeParameters.Any() Then
                Me.SubstitutedSourceMethod = Me.SubstitutedSourceMethod.Construct(_typeParameters.As(Of TypeSymbol)())
            End If
            TypeParameterChecker.Check(Me.SubstitutedSourceMethod, _allTypeParameters)

            ' Create a map from original parameter to target parameter.
            Dim parameterBuilder = ArrayBuilder(Of ParameterSymbol).GetInstance()

            Dim substitutedSourceMeParameter = Me.SubstitutedSourceMethod.MeParameter
            Dim substitutedSourceHasMeParameter = substitutedSourceMeParameter IsNot Nothing
            If substitutedSourceHasMeParameter Then
                _meParameter = MakeParameterSymbol(0, GeneratedNames.MakeStateMachineCapturedMeName(), substitutedSourceMeParameter) ' NOTE: Name doesn't actually matter.
                Debug.Assert(TypeSymbol.Equals(_meParameter.Type, Me.SubstitutedSourceMethod.ContainingType, TypeCompareKind.ConsiderEverything))
                parameterBuilder.Add(_meParameter)
            End If

            Dim ordinalOffset = If(substitutedSourceHasMeParameter, 1, 0)
            For Each substitutedSourceParameter In Me.SubstitutedSourceMethod.Parameters
                Dim ordinal = substitutedSourceParameter.Ordinal + ordinalOffset
                Debug.Assert(ordinal = parameterBuilder.Count)
                Dim parameter = MakeParameterSymbol(ordinal, substitutedSourceParameter.Name, substitutedSourceParameter)
                parameterBuilder.Add(parameter)
            Next

            _parameters = parameterBuilder.ToImmutableAndFree()

            Dim localsBuilder = ArrayBuilder(Of LocalSymbol).GetInstance()
            Dim localsMap = PooledDictionary(Of LocalSymbol, LocalSymbol).GetInstance()
            For Each sourceLocal In sourceLocals
                Dim local = sourceLocal.ToOtherMethod(Me, Me.TypeMap)
                localsMap.Add(sourceLocal, local)
                localsBuilder.Add(local)
            Next
            Me.Locals = localsBuilder.ToImmutableAndFree()
            localsBuilder = ArrayBuilder(Of LocalSymbol).GetInstance()
            For Each sourceLocal In sourceLocalsForBinding
                Dim local As LocalSymbol = Nothing
                If Not localsMap.TryGetValue(sourceLocal, local) Then
                    local = sourceLocal.ToOtherMethod(Me, Me.TypeMap)
                    localsMap.Add(sourceLocal, local)
                End If
                localsBuilder.Add(local)
            Next
            Me.LocalsForBinding = localsBuilder.ToImmutableAndFree()

            ' Create a map from variable name to display class field.
            _displayClassVariables = SubstituteDisplayClassVariables(sourceDisplayClassVariables, localsMap, Me, Me.TypeMap)
            localsMap.Free()

            _generateMethodBody = generateMethodBody
        End Sub

        Private Shared Function SubstituteDisplayClassVariables(
            oldDisplayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable),
            localsMap As Dictionary(Of LocalSymbol, LocalSymbol),
            otherMethod As MethodSymbol,
            typeMap As TypeSubstitution) As ImmutableDictionary(Of String, DisplayClassVariable)

            ' Create a map from variable name to display class field.
            Dim newDisplayClassVariables = PooledDictionary(Of String, DisplayClassVariable).GetInstance()
            For Each pair In oldDisplayClassVariables
                Dim variable = pair.Value
                Dim oldDisplayClassInstance = variable.DisplayClassInstance

                ' Note: we don't call ToOtherMethod in the local case because doing so would produce
                ' a new LocalSymbol that would not be ReferenceEquals to the one in this.LocalsForBinding.
                Dim oldDisplayClassInstanceFromLocal = TryCast(oldDisplayClassInstance, DisplayClassInstanceFromLocal)
                Dim newDisplayClassInstance = If(oldDisplayClassInstanceFromLocal Is Nothing,
                    oldDisplayClassInstance.ToOtherMethod(otherMethod, typeMap),
                    New DisplayClassInstanceFromLocal(DirectCast(localsMap(oldDisplayClassInstanceFromLocal.Local), EELocalSymbol)))

                variable = variable.SubstituteFields(newDisplayClassInstance, typeMap)
                newDisplayClassVariables.Add(pair.Key, variable)
            Next

            Dim result = newDisplayClassVariables.ToImmutableDictionary()
            newDisplayClassVariables.Free()
            Return result
        End Function

        Private Function MakeParameterSymbol(ordinal As Integer, name As String, sourceParameter As ParameterSymbol) As ParameterSymbol
            Return SynthesizedParameterSymbol.Create(
                Me,
                sourceParameter.Type,
                ordinal,
                sourceParameter.IsByRef,
                name,
                sourceParameter.CustomModifiers,
                sourceParameter.RefCustomModifiers)
        End Function

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As MethodImplAttributes
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Return Nothing
        End Function

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function TryGetMeParameter(<Out> ByRef meParameter As ParameterSymbol) As Boolean
            meParameter = Nothing
            Return True
        End Function

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return ReturnType.SpecialType = SpecialType.System_Void
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                If _lazyReturnType Is Nothing Then
                    Throw New InvalidOperationException()
                End If
                Return _lazyReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArrayExtensions.Cast(Of TypeParameterSymbol, TypeSymbol)(_typeParameters)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _typeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImmutableArray(Of MethodSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property CallingConvention As Cci.CallingConvention
            Get
                Debug.Assert(Me.IsShared)
                Dim cc = Cci.CallingConvention.Default
                If Me.IsVararg Then
                    cc = cc Or Cci.CallingConvention.ExtraArguments
                End If
                If Me.IsGenericMethod Then
                    cc = cc Or Cci.CallingConvention.Generic
                End If
                Return cc
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(Of VisualBasicSyntaxNode)(_locations)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Internal
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsInitOnly As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Friend ReadOnly Property ResultProperties As ResultProperties
            Get
                Return _lazyResultProperties
            End Get
        End Property

#Disable Warning CA1200 ' Avoid using cref tags with a prefix
        ''' <remarks>
        ''' The corresponding C# method, 
        ''' <see cref="M:Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.EEMethodSymbol.GenerateMethodBody(Microsoft.CodeAnalysis.CSharp.TypeCompilationState,Microsoft.CodeAnalysis.DiagnosticBag)"/>, 
        ''' invokes the <see cref="LocalRewriter"/> and the <see cref="LambdaRewriter"/> explicitly.
        ''' In VB, the caller (of this method) does that.
        ''' </remarks>
#Enable Warning CA1200 ' Avoid using cref tags with a prefix
        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, <Out> ByRef Optional methodBodyBinder As Binder = Nothing) As BoundBlock
            Debug.Assert(diagnostics.DiagnosticBag IsNot Nothing)

            Dim body = _generateMethodBody(Me, diagnostics.DiagnosticBag, _lazyResultProperties)
            Debug.Assert(body IsNot Nothing)

            _lazyReturnType = CalculateReturnType(body)

            ' Can't do this until the return type has been computed.
            TypeParameterChecker.Check(Me, _allTypeParameters)

            Dim syntax As SyntaxNode = body.Syntax
            Dim statementsBuilder = ArrayBuilder(Of BoundStatement).GetInstance()
            statementsBuilder.Add(body)
            ' Insert an implicit return statement if necessary.
            If body.Kind <> BoundKind.ReturnStatement Then
                statementsBuilder.Add(New BoundReturnStatement(syntax, Nothing, Nothing, Nothing))
            End If

            Dim originalLocalsBuilder = ArrayBuilder(Of LocalSymbol).GetInstance()
            Dim originalLocalsSet = PooledHashSet(Of LocalSymbol).GetInstance()
            For Each local In LocalsForBinding
                Debug.Assert(Not originalLocalsSet.Contains(local))
                originalLocalsBuilder.Add(local)
                originalLocalsSet.Add(local)
            Next
            For Each local In Me.Locals
                If originalLocalsSet.Add(local) Then
                    originalLocalsBuilder.Add(local)
                End If
            Next
            originalLocalsSet.Free()
            Dim originalLocals = originalLocalsBuilder.ToImmutableAndFree()
            Dim newBody = New BoundBlock(syntax, Nothing, originalLocals, statementsBuilder.ToImmutableAndFree())

            If diagnostics.HasAnyErrors() Then
                Return newBody
            End If

            DiagnosticsPass.IssueDiagnostics(newBody, diagnostics.DiagnosticBag, Me)
            If diagnostics.HasAnyErrors() Then
                Return newBody
            End If

            ' Check for use-site errors (e.g. missing types in the signature).
            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = Me.CalculateUseSiteInfo()
            If useSiteInfo.DiagnosticInfo IsNot Nothing Then
                diagnostics.Add(useSiteInfo, _locations(0))
                Return newBody
            End If

            Debug.Assert(Not newBody.HasErrors)

            ' NOTE: In C#, EE rewriting happens AFTER local rewriting.  However, that order would be difficult
            ' to accommodate in VB, so we reverse it.

            Try
                ' Rewrite local declaration statement.
                newBody = LocalDeclarationRewriter.Rewrite(_compilation, _container, newBody)

                ' Rewrite pseudo-variable references to helper method calls.
                newBody = DirectCast(PlaceholderLocalRewriter.Rewrite(_compilation, _container, newBody, diagnostics.DiagnosticBag), BoundBlock)
                If diagnostics.HasAnyErrors() Then
                    Return newBody
                End If

                ' Create a map from original local to target local.
                Dim localMap = PooledDictionary(Of LocalSymbol, LocalSymbol).GetInstance()
                Dim targetLocals = newBody.Locals
                Debug.Assert(originalLocals.Length = targetLocals.Length)
                For i = 0 To originalLocals.Length - 1
                    Dim originalLocal = originalLocals(i)
                    Dim targetLocal = targetLocals(i)
                    Debug.Assert(TypeOf originalLocal IsNot EELocalSymbol OrElse
                        DirectCast(originalLocal, EELocalSymbol).Ordinal = DirectCast(targetLocal, EELocalSymbol).Ordinal)
                    localMap.Add(originalLocal, targetLocal)
                Next

                ' Variables may have been captured by lambdas in the original method
                ' or in the expression, and we need to preserve the existing values of
                ' those variables in the expression. This requires rewriting the variables
                ' in the expression based on the closure classes from both the original
                ' method and the expression, and generating a preamble that copies
                ' values into the expression closure classes.
                '
                ' Consider the original method:
                ' Shared Sub M()
                '     Dim x, y, z as Integer
                '     ...
                '     F(Function() x + y)
                ' End Sub
                ' and the expression in the EE: "F(Function() x + z)".
                '
                ' The expression is first rewritten using the closure class and local <1>
                ' from the original method: F(Function() <1>.x + z)
                ' Then lambda rewriting introduces a new closure class that includes
                ' the locals <1> and z, and a corresponding local <2>: F(Function() <2>.<1>.x + <2>.z)
                ' And a preamble is added to initialize the fields of <2>:
                '     <2> = New <>c__DisplayClass0()
                '     <2>.<1> = <1>
                '     <2>.z = z
                '
                ' Note: The above behavior is actually implemented in the LambdaRewriter and
                '       is triggered by overriding PreserveOriginalLocals to return "True".

                ' Create a map from variable name to display class field.
                Dim displayClassVariables = SubstituteDisplayClassVariables(_displayClassVariables, localMap, Me, Me.TypeMap)

                ' Rewrite references to "Me" to refer to this method's "Me" parameter.
                ' Rewrite variables within body to reference existing display classes.
                newBody = DirectCast(CapturedVariableRewriter.Rewrite(
                    If(Me.SubstitutedSourceMethod.IsShared, Nothing, Me.Parameters(0)),
                    displayClassVariables,
                    newBody,
                    diagnostics.DiagnosticBag), BoundBlock)

                If diagnostics.HasAnyErrors() Then
                    Return newBody
                End If

                ' Insert locals from the original method, followed by any new locals.
                Dim localBuilder = ArrayBuilder(Of LocalSymbol).GetInstance()
                For Each originalLocal In Me.Locals
                    Dim targetLocal = localMap(originalLocal)
                    Debug.Assert(TypeOf targetLocal IsNot EELocalSymbol OrElse DirectCast(targetLocal, EELocalSymbol).Ordinal = localBuilder.Count)
                    localBuilder.Add(targetLocal)
                Next

                localMap.Free()
                newBody = newBody.Update(newBody.StatementListSyntax, localBuilder.ToImmutableAndFree(), newBody.Statements)
                TypeParameterChecker.Check(newBody, _allTypeParameters)

            Catch ex As BoundTreeVisitor.CancelledByStackGuardException
                ex.AddAnError(diagnostics)
            End Try

            Return newBody
        End Function

        Private Function CalculateReturnType(body As BoundStatement) As TypeSymbol
            Select Case body.Kind
                Case BoundKind.ReturnStatement
                    Return DirectCast(body, BoundReturnStatement).ExpressionOpt.Type
                Case BoundKind.ExpressionStatement,
                     BoundKind.RedimStatement
                    Return _voidType
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(body.Kind)
            End Select
        End Function

        Friend Overrides Sub AddSynthesizedReturnTypeAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedReturnTypeAttributes(attributes)

            Dim returnType = Me.ReturnType
            If returnType.ContainsTupleNames() AndAlso DeclaringCompilation.HasTupleNamesAttributes() Then
                AddSynthesizedAttribute(attributes, DeclaringCompilation.SynthesizeTupleNamesAttribute(returnType))
            End If
        End Sub

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Return localPosition
        End Function

        Friend Overrides ReadOnly Property PreserveOriginalLocals As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSetsRequiredMembers As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

End Namespace
