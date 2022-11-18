' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class MethodToClassRewriter(Of TProxy)

        Private Function SubstituteMethodForMyBaseOrMyClassCall(receiverOpt As BoundExpression, originalMethodBeingCalled As MethodSymbol) As MethodSymbol
            If (originalMethodBeingCalled.IsMetadataVirtual OrElse Me.IsInExpressionLambda) AndAlso
                    receiverOpt IsNot Nothing AndAlso (receiverOpt.Kind = BoundKind.MyBaseReference OrElse receiverOpt.Kind = BoundKind.MyClassReference) Then

                ' NOTE: We can only call a virtual method non-virtually if the type of Me reference 
                '       we pass to this method IS or INHERITS FROM the type of the method we want to call;
                '
                '       Thus, for MyBase/MyClass receivers we MAY need to replace 
                '       the method with a wrapper one to be able to call it non-virtually;
                '
                Dim callingMethodType As TypeSymbol = Me.CurrentMethod.ContainingType
                Dim topLevelMethodType As TypeSymbol = Me.TopLevelMethod.ContainingType

                If callingMethodType IsNot topLevelMethodType OrElse Me.IsInExpressionLambda Then
                    Dim newMethod = GetOrCreateMyBaseOrMyClassWrapperFunction(receiverOpt, originalMethodBeingCalled)

                    ' substitute type parameters if needed
                    If newMethod.IsGenericMethod Then
                        Debug.Assert(originalMethodBeingCalled.IsGenericMethod)

                        Dim typeArgs = originalMethodBeingCalled.TypeArguments
                        Debug.Assert(typeArgs.Length = newMethod.Arity)

                        Dim visitedTypeArgs(typeArgs.Length - 1) As TypeSymbol
                        For i = 0 To typeArgs.Length - 1
                            visitedTypeArgs(i) = VisitType(typeArgs(i))
                        Next
                        newMethod = newMethod.Construct(visitedTypeArgs.AsImmutableOrNull())
                    End If

                    Return newMethod
                End If

            End If
            Return originalMethodBeingCalled
        End Function

        Private Function GetOrCreateMyBaseOrMyClassWrapperFunction(receiver As BoundExpression, method As MethodSymbol) As MethodSymbol
            Debug.Assert(receiver IsNot Nothing)
            Debug.Assert(receiver.IsMyClassReference() OrElse receiver.IsMyBaseReference())
            Debug.Assert(method IsNot Nothing)

            method = method.ConstructedFrom()

            Dim methodWrapper As MethodSymbol = CompilationState.GetMethodWrapper(method)
            If methodWrapper IsNot Nothing Then
                Return methodWrapper
            End If

            ' Disregarding what was passed as the receiver, we only need to create one wrapper 
            ' method for a method _symbol_ being called. Thus, if topLevelMethod.ContainingType
            ' overrides the virtual method M1, 'MyClass.M1' will use a wrapper method for
            ' topLevelMethod.ContainingType.M1 method symbol and 'MyBase.M1' will use
            ' a wrapper method for topLevelMethod.ContainingType.BaseType.M1 method symbol.
            ' In case topLevelMethod.ContainingType DOES NOT override M1, both 'MyClass.M1' and 
            ' 'MyBase.M1' will use a wrapper method for topLevelMethod.ContainingType.BaseType.M1.

            Dim containingType As NamedTypeSymbol = Me.TopLevelMethod.ContainingType
            Dim methodContainingType As NamedTypeSymbol = method.ContainingType

            Dim isMyBase As Boolean = Not methodContainingType.Equals(containingType)
            Debug.Assert(isMyBase OrElse receiver.Kind = BoundKind.MyClassReference)

            Dim syntax As SyntaxNode = Me.CurrentMethod.Syntax

            ' generate and register wrapper method
            Dim wrapperMethodName As String = GeneratedNames.MakeBaseMethodWrapperName(method.Name, isMyBase)
            Dim wrapperMethod As New SynthesizedWrapperMethod(DirectCast(containingType, InstanceTypeSymbol), method, wrapperMethodName, syntax)

            ' register a new method
            If Me.CompilationState.ModuleBuilderOpt IsNot Nothing Then
                Me.CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(containingType, wrapperMethod.GetCciAdapter())
            End If

            ' generate method body
            Dim wrappedMethod As MethodSymbol = wrapperMethod.WrappedMethod
            Debug.Assert(wrappedMethod.ConstructedFrom() Is method)

            Dim boundArguments(wrapperMethod.ParameterCount - 1) As BoundExpression
            For argIndex = 0 To wrapperMethod.ParameterCount - 1
                Dim parameterSymbol As ParameterSymbol = wrapperMethod.Parameters(argIndex)
                boundArguments(argIndex) = New BoundParameter(syntax, parameterSymbol, isLValue:=parameterSymbol.IsByRef, type:=parameterSymbol.Type)
            Next

            Dim meParameter As ParameterSymbol = wrapperMethod.MeParameter
            Dim newReceiver As BoundExpression = Nothing
            If isMyBase Then
                newReceiver = New BoundMyBaseReference(syntax, meParameter.Type)
            Else
                newReceiver = New BoundMyClassReference(syntax, meParameter.Type)
            End If

            Dim boundCall As New BoundCall(syntax, wrappedMethod, Nothing, newReceiver,
                                           boundArguments.AsImmutableOrNull, Nothing, wrappedMethod.ReturnType)

            Dim boundMethodBody As BoundStatement = If(Not wrappedMethod.ReturnType.IsVoidType(),
                                                       DirectCast(New BoundReturnStatement(syntax, boundCall, Nothing, Nothing), BoundStatement),
                                                       New BoundBlock(syntax, Nothing, ImmutableArray(Of LocalSymbol).Empty,
                                                                      ImmutableArray.Create(Of BoundStatement)(
                                                                          New BoundExpressionStatement(syntax, boundCall),
                                                                          New BoundReturnStatement(syntax, Nothing, Nothing, Nothing))))

            ' add to generated method collection and return
            CompilationState.AddMethodWrapper(method, wrapperMethod, boundMethodBody)
            Return wrapperMethod
        End Function

        ''' <summary>
        ''' A method that wraps the call to a method through MyBase/MyClass receiver.
        ''' </summary>
        ''' <remarks>
        ''' <example>
        ''' Class A
        '''     Protected Overridable Sub F(a As Integer)
        '''     End Sub
        ''' End Class
        ''' 
        ''' Class B
        '''     Inherits A
        ''' 
        '''     Public Sub M()
        '''         Dim b As Integer = 1
        '''         Dim f As System.Action = Sub() MyBase.F(b)
        '''     End Sub
        ''' End Class
        ''' </example>
        ''' </remarks>
        Friend NotInheritable Class SynthesizedWrapperMethod
            Inherits SynthesizedMethod

            Private ReadOnly _wrappedMethod As MethodSymbol
            Private ReadOnly _typeMap As TypeSubstitution
            Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
            Private ReadOnly _returnType As TypeSymbol
            Private ReadOnly _locations As ImmutableArray(Of Location)

            ''' <summary>
            ''' Creates a symbol for a method that wraps the call to a method through MyBase/MyClass receiver.
            ''' </summary>
            ''' <param name="containingType">Type that contains wrapper method.</param>
            ''' <param name="methodToWrap">Method to wrap</param>
            ''' <param name="wrapperName">Wrapper method name</param>
            ''' <param name="syntax">Syntax node.</param>
            Friend Sub New(containingType As InstanceTypeSymbol,
                           methodToWrap As MethodSymbol,
                           wrapperName As String,
                           syntax As SyntaxNode)

                MyBase.New(syntax, containingType, wrapperName, False)

                Me._locations = ImmutableArray.Create(Of Location)(syntax.GetLocation())

                Me._typeMap = Nothing

                If Not methodToWrap.IsGenericMethod Then
                    Me._typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                    Me._wrappedMethod = methodToWrap
                Else
                    Me._typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(methodToWrap.OriginalDefinition.TypeParameters, Me, CreateTypeParameter)

                    Dim typeArgs(Me._typeParameters.Length - 1) As TypeSymbol
                    For ind = 0 To Me._typeParameters.Length - 1
                        typeArgs(ind) = Me._typeParameters(ind)
                    Next

                    Dim newConstructedWrappedMethod As MethodSymbol = methodToWrap.Construct(typeArgs.AsImmutableOrNull())

                    Me._typeMap = TypeSubstitution.Create(newConstructedWrappedMethod.OriginalDefinition,
                                                           newConstructedWrappedMethod.OriginalDefinition.TypeParameters,
                                                           typeArgs.AsImmutableOrNull())

                    Me._wrappedMethod = newConstructedWrappedMethod
                End If

                Dim params(Me._wrappedMethod.ParameterCount - 1) As ParameterSymbol
                For i = 0 To params.Length - 1
                    Dim curParam = Me._wrappedMethod.Parameters(i)
                    params(i) = SynthesizedMethod.WithNewContainerAndType(Me, curParam.Type.InternalSubstituteTypeParameters(Me._typeMap).Type, curParam)
                Next
                Me._parameters = params.AsImmutableOrNull()

                Me._returnType = Me._wrappedMethod.ReturnType.InternalSubstituteTypeParameters(Me._typeMap).Type
            End Sub

            Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
                Get
                    Return Me._typeMap
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

                Dim compilation = Me.DeclaringCompilation

                AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

                ' Dev11 emits DebuggerNonUserCode. We emit DebuggerHidden to hide the method even if JustMyCode is off.
                AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerHiddenAttribute())
            End Sub

            Public ReadOnly Property WrappedMethod As MethodSymbol
                Get
                    Return Me._wrappedMethod
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return _typeParameters
                End Get
            End Property

            Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
                Get
                    ' This is always a method definition, so the type arguments are the same as the type parameters.
                    If Arity > 0 Then
                        Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
                    Else
                        Return ImmutableArray(Of TypeSymbol).Empty
                    End If
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return _locations
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return _parameters
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return _returnType
                End Get
            End Property

            Public Overrides ReadOnly Property IsShared As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsSub As Boolean
                Get
                    Return Me._wrappedMethod.IsSub
                End Get
            End Property

            Public Overrides ReadOnly Property IsVararg As Boolean
                Get
                    Return Me._wrappedMethod.IsVararg
                End Get
            End Property

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return _typeParameters.Length
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return Accessibility.Private
                End Get
            End Property

            Friend Overrides ReadOnly Property ParameterCount As Integer
                Get
                    Return Me._parameters.Length
                End Get
            End Property

            Friend Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
                Return False
            End Function

            Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
                Throw ExceptionUtilities.Unreachable
            End Function
        End Class
    End Class
End Namespace
