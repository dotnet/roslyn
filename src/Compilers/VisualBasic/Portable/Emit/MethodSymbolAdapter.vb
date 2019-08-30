' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Partial Class MethodSymbol
        Implements Cci.ITypeMemberReference
        Implements Cci.IMethodReference
        Implements Cci.IGenericMethodInstanceReference
        Implements Cci.ISpecializedMethodReference
        Implements Cci.ITypeDefinitionMember
        Implements Cci.IMethodDefinition

        Public Const DisableJITOptimizationFlags As MethodImplAttributes = MethodImplAttributes.NoInlining Or MethodImplAttributes.NoOptimization

        Private ReadOnly Property IMethodReferenceAsGenericMethodInstanceReference As Cci.IGenericMethodInstanceReference Implements Cci.IMethodReference.AsGenericMethodInstanceReference
            Get
                Debug.Assert(Me.IsDefinitionOrDistinct())

                If Not Me.IsDefinition AndAlso Me.IsGenericMethod AndAlso Me IsNot Me.ConstructedFrom Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceAsSpecializedMethodReference As Cci.ISpecializedMethodReference Implements Cci.IMethodReference.AsSpecializedMethodReference
            Get
                Debug.Assert(Me.IsDefinitionOrDistinct())

                If Not Me.IsDefinition AndAlso (Not Me.IsGenericMethod OrElse Me Is Me.ConstructedFrom) Then
                    Debug.Assert(Me.ContainingType IsNot Nothing AndAlso IsOrInGenericType(Me.ContainingType))
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As EmitContext) As Cci.IDefinition ' Implements IReference.AsDefinition
            Return ResolvedMethodImpl(DirectCast(context.Module, PEModuleBuilder))
        End Function

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As Cci.ITypeReference Implements Cci.ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not Me.IsDefinition Then
                Return moduleBeingBuilt.Translate(Me.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            ElseIf TypeOf Me Is SynthesizedGlobalMethodBase Then
                Dim privateImplClass = moduleBeingBuilt.GetPrivateImplClass(syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
                Debug.Assert(privateImplClass IsNot Nothing)
                Return privateImplClass
            End If

            Return moduleBeingBuilt.Translate(Me.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics, needDeclaration:=True)
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As Cci.MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not Me.IsDefinition Then
                If Me.IsGenericMethod AndAlso Me IsNot Me.ConstructedFrom Then
                    Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)
                    visitor.Visit(DirectCast(Me, Cci.IGenericMethodInstanceReference))
                Else
                    Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsSpecializedMethodReference IsNot Nothing)
                    visitor.Visit(DirectCast(Me, Cci.ISpecializedMethodReference))
                End If
            Else
                Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(visitor.Context.Module, PEModuleBuilder)
                If Me.ContainingModule = moduleBeingBuilt.SourceModule Then
                    Debug.Assert((DirectCast(Me, Cci.IMethodReference)).GetResolvedMethod(visitor.Context) IsNot Nothing)
                    visitor.Visit(DirectCast(Me, Cci.IMethodDefinition))
                Else
                    visitor.Visit(DirectCast(Me, Cci.IMethodReference))
                End If
            End If
        End Sub

        Private ReadOnly Property INamedEntityName As String Implements Cci.INamedEntity.Name
            Get
                Return Me.MetadataName
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceAcceptsExtraArguments As Boolean Implements Cci.IMethodReference.AcceptsExtraArguments
            Get
                Return Me.IsVararg
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceGenericParameterCount As UShort Implements Cci.IMethodReference.GenericParameterCount
            Get
                Return CType(Me.Arity, UShort)
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceIsGeneric As Boolean Implements Cci.IMethodReference.IsGeneric
            Get
                Return Me.IsGenericMethod
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceParameterCount As UShort Implements Cci.ISignature.ParameterCount
            Get
                Return CType(Me.ParameterCount, UShort)
            End Get
        End Property

        Private Function IMethodReferenceGetResolvedMethod(context As EmitContext) As Cci.IMethodDefinition Implements Cci.IMethodReference.GetResolvedMethod
            Return ResolvedMethodImpl(DirectCast(context.Module, PEModuleBuilder))
        End Function

        Private Function ResolvedMethodImpl(moduleBeingBuilt As PEModuleBuilder) As Cci.IMethodDefinition
            Debug.Assert(Me.IsDefinitionOrDistinct())

            ' Can't be generic instantiation
            ' must be declared in the module we are building
            If Me.IsDefinition AndAlso
                Me.ContainingModule = moduleBeingBuilt.SourceModule Then
                Debug.Assert(Me.PartialDefinitionPart Is Nothing) ' must be definition
                Return Me
            End If

            Return Nothing
        End Function

        Private ReadOnly Property IMethodReferenceExtraParameters As ImmutableArray(Of Cci.IParameterTypeInformation) Implements Cci.IMethodReference.ExtraParameters
            Get
                Return ImmutableArray(Of Cci.IParameterTypeInformation).Empty
            End Get
        End Property

        Private ReadOnly Property ISignatureCallingConvention As Cci.CallingConvention Implements Cci.ISignature.CallingConvention
            Get
                Return Me.CallingConvention
            End Get
        End Property

        Private Function ISignatureGetParameters(context As EmitContext) As ImmutableArray(Of Cci.IParameterTypeInformation) Implements Cci.ISignature.GetParameters
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Debug.Assert(Me.IsDefinitionOrDistinct())
#If DEBUG Then
            For Each p In Me.Parameters
                Debug.Assert(p Is p.OriginalDefinition)
            Next
#End If

            If Me.IsDefinition AndAlso Me.ContainingModule = moduleBeingBuilt.SourceModule Then
                Return EnumerateDefinitionParameters()
            Else
                Return moduleBeingBuilt.Translate(Me.Parameters)
            End If
        End Function

        Private Function EnumerateDefinitionParameters() As ImmutableArray(Of Cci.IParameterTypeInformation)
            Debug.Assert(Me.Parameters.All(Function(p) p.IsDefinition))

            Return StaticCast(Of Cci.IParameterTypeInformation).From(Me.Parameters)
        End Function

        Private ReadOnly Property ISignatureReturnValueCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.ISignature.ReturnValueCustomModifiers
            Get
                Return Me.ReturnTypeCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureRefCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.ISignature.RefCustomModifiers
            Get
                Return Me.RefCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsByRef As Boolean Implements Cci.ISignature.ReturnValueIsByRef
            Get
                Return ReturnsByRef
            End Get
        End Property

        Private Function ISignatureGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.ISignature.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Dim returnType As TypeSymbol = Me.ReturnType
            Return moduleBeingBuilt.Translate(returnType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function IGenericMethodInstanceReferenceGetGenericArguments(context As EmitContext) As IEnumerable(Of Cci.ITypeReference) Implements Cci.IGenericMethodInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)

            Return From arg In Me.TypeArguments
                   Select moduleBeingBuilt.Translate(arg, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function IGenericMethodInstanceReferenceGetGenericMethod(context As EmitContext) As Cci.IMethodReference Implements Cci.IGenericMethodInstanceReference.GetGenericMethod
            Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)

            If Me.OriginalDefinition IsNot Me.ConstructedFrom Then
                Return Me.ConstructedFrom
            End If

            Dim container As NamedTypeSymbol = Me.ContainingType

            If (Not container.IsOrInGenericType()) Then
                Return Me.OriginalDefinition
                ' NoPia method might come through here.
                Return DirectCast(context.Module, PEModuleBuilder).Translate(
                    Me.OriginalDefinition,
                    DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode),
                    context.Diagnostics,
                    needDeclaration:=True)
            End If

            Dim methodSymbol As MethodSymbol = Me.ConstructedFrom
            Return New SpecializedMethodReference(methodSymbol)
        End Function

        Private ReadOnly Property ISpecializedMethodReferenceUnspecializedVersion As Cci.IMethodReference Implements Cci.ISpecializedMethodReference.UnspecializedVersion
            Get
                Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsSpecializedMethodReference IsNot Nothing)
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As Cci.ITypeDefinition Implements Cci.ITypeDefinitionMember.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()

                Dim synthesizedGlobalMethod = TryCast(Me, SynthesizedGlobalMethodBase)
                If synthesizedGlobalMethod IsNot Nothing Then
                    Return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType
                End If

                Return Me.ContainingType
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberVisibility As Cci.TypeMemberVisibility Implements Cci.ITypeDefinitionMember.Visibility
            Get
                CheckDefinitionInvariant()
                Return PEModuleBuilder.MemberVisibility(Me)
            End Get
        End Property

        Private Function IMethodDefinitionGetBody(context As EmitContext) As Cci.IMethodBody Implements Cci.IMethodDefinition.GetBody
            CheckDefinitionInvariant()
            Return (DirectCast(context.Module, PEModuleBuilder)).GetMethodBody(Me)
        End Function

        Private ReadOnly Property IMethodDefinitionGenericParameters As IEnumerable(Of Cci.IGenericMethodParameter) Implements Cci.IMethodDefinition.GenericParameters
            Get
                CheckDefinitionInvariant()
                Debug.Assert(Me.TypeParameters.All(Function(param) param Is param.OriginalDefinition))
                Return Me.TypeParameters
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionHasDeclarativeSecurity As Boolean Implements Cci.IMethodDefinition.HasDeclarativeSecurity
            Get
                CheckDefinitionInvariant()
                Return Me.HasDeclarativeSecurity
            End Get
        End Property


        Private ReadOnly Property IMethodDefinitionIsImplicitlyDeclared As Boolean Implements Cci.IMethodDefinition.IsImplicitlyDeclared
            Get
                CheckDefinitionInvariant()
                Return Me.IsImplicitlyDeclared
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsAbstract As Boolean Implements Cci.IMethodDefinition.IsAbstract
            Get
                CheckDefinitionInvariant()
                Return Me.IsMustOverride
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsAccessCheckedOnOverride As Boolean Implements Cci.IMethodDefinition.IsAccessCheckedOnOverride
            Get
                CheckDefinitionInvariant()
                Return Me.IsAccessCheckedOnOverride
            End Get
        End Property

        Friend Overridable ReadOnly Property IsAccessCheckedOnOverride As Boolean
            Get
                CheckDefinitionInvariant()
                Return Me.IsMetadataVirtual ' VB always sets this for methods where virtual is set.
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsConstructor As Boolean Implements Cci.IMethodDefinition.IsConstructor
            Get
                CheckDefinitionInvariant()
                Return Me.MethodKind = MethodKind.Constructor
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsExternal As Boolean Implements Cci.IMethodDefinition.IsExternal
            Get
                CheckDefinitionInvariant()
                Return Me.IsExternal
            End Get
        End Property

        Friend Overridable ReadOnly Property IsExternal As Boolean
            Get
                CheckDefinitionInvariant()
                Return Me.IsExternalMethod
            End Get
        End Property

        Private Function IMethodDefinitionGetImplementationOptions(context As EmitContext) As MethodImplAttributes Implements Cci.IMethodDefinition.GetImplementationAttributes
            CheckDefinitionInvariant()
            Return Me.ImplementationAttributes Or
                   If(DirectCast(context.Module, PEModuleBuilder).JITOptimizationIsDisabled(Me), DisableJITOptimizationFlags, Nothing)
        End Function

        Private ReadOnly Property IMethodDefinitionIsHiddenBySignature As Boolean Implements Cci.IMethodDefinition.IsHiddenBySignature
            Get
                CheckDefinitionInvariant()
                Return Me.IsHiddenBySignature
            End Get
        End Property

        Friend Overridable ReadOnly Property IsHiddenBySignature As Boolean
            Get
                CheckDefinitionInvariant()
                Return Me.IsOverloads
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsNewSlot As Boolean Implements Cci.IMethodDefinition.IsNewSlot
            Get
                CheckDefinitionInvariant()
                Return Me.IsMetadataNewSlot()
            End Get
        End Property

        ''' <summary>
        ''' This method indicates whether or not the runtime will regard the method
        ''' as newslot (as indicated by the presence of the "newslot" modifier in the
        ''' signature).
        ''' WARN WARN WARN: We won't have a final value for this until declaration
        ''' diagnostics have been computed for all <see cref="SourceMemberContainerTypeSymbol"/>s,
        ''' so pass ignoringInterfaceImplementationChanges: True if you need a value sooner
        ''' and aren't concerned about tweaks made to satisfy interface implementation 
        ''' requirements.
        ''' NOTE: Not ignoring changes can only result in a value that is more true.
        ''' </summary>
        Friend Overridable Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            If Me.IsOverrides Then
                Return OverrideHidingHelper.RequiresExplicitOverride(Me)
            Else
                Return Me.IsMetadataVirtual
            End If
        End Function

        Private ReadOnly Property IMethodDefinitionIsPlatformInvoke As Boolean Implements Cci.IMethodDefinition.IsPlatformInvoke
            Get
                CheckDefinitionInvariant()
                Return Me.GetDllImportData() IsNot Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionPlatformInvokeData As Cci.IPlatformInvokeInformation Implements Cci.IMethodDefinition.PlatformInvokeData
            Get
                CheckDefinitionInvariant()
                Return Me.GetDllImportData()
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsRuntimeSpecial As Boolean Implements Cci.IMethodDefinition.IsRuntimeSpecial
            Get
                CheckDefinitionInvariant()
                Return Me.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overridable ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                CheckDefinitionInvariant()
                Dim result = Me.MethodKind = MethodKind.Constructor OrElse
                             Me.MethodKind = MethodKind.SharedConstructor

                ' runtime-special must be special:
                Debug.Assert(Not result OrElse HasSpecialName)

                Return result
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsSealed As Boolean Implements Cci.IMethodDefinition.IsSealed
            Get
                CheckDefinitionInvariant()
                Return Me.IsMetadataFinal
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMetadataFinal As Boolean
            Get
                ' If we are metadata virtual, but not language virtual, set the "final" bit (i.e., interface
                ' implementation methods). Also do it if we are explicitly marked "NotOverridable".
                Return Me.IsNotOverridable OrElse
                    (Me.IsMetadataVirtual AndAlso Not (Me.IsOverridable OrElse Me.IsMustOverride OrElse Me.IsOverrides))
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsSpecialName As Boolean Implements Cci.IMethodDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return Me.HasSpecialName
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsStatic As Boolean Implements Cci.IMethodDefinition.IsStatic
            Get
                CheckDefinitionInvariant()
                Return Me.IsShared
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsVirtual As Boolean Implements Cci.IMethodDefinition.IsVirtual
            Get
                CheckDefinitionInvariant()
                Return Me.IsMetadataVirtual()
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionParameters As ImmutableArray(Of Cci.IParameterDefinition) Implements Cci.IMethodDefinition.Parameters
            Get
                CheckDefinitionInvariant()

#If DEBUG Then
                For Each p In Me.Parameters
                    Debug.Assert(p Is p.OriginalDefinition)
                Next
#End If
                Return StaticCast(Of Cci.IParameterDefinition).From(Me.Parameters)
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionRequiresSecurityObject As Boolean Implements Cci.IMethodDefinition.RequiresSecurityObject
            Get
                CheckDefinitionInvariant()
                Return False
            End Get
        End Property

        Private Function IMethodDefinitionGetReturnValueAttributes(context As EmitContext) As IEnumerable(Of Cci.ICustomAttribute) Implements Cci.IMethodDefinition.GetReturnValueAttributes
            CheckDefinitionInvariant()

            Dim userDefined As ImmutableArray(Of VisualBasicAttributeData)
            Dim synthesized As ArrayBuilder(Of SynthesizedAttributeData) = Nothing

            userDefined = Me.GetReturnTypeAttributes()
            Me.AddSynthesizedReturnTypeAttributes(synthesized)

            ' Note that callers of this method (CCI and ReflectionEmitter) have to enumerate 
            ' all items of the returned iterator, otherwise the synthesized ArrayBuilder may leak.
            Return GetCustomAttributesToEmit(userDefined, synthesized, isReturnType:=True, emittingAssemblyAttributesInNetModule:=False)
        End Function

        Private ReadOnly Property IMethodDefinitionReturnValueIsMarshalledExplicitly As Boolean Implements Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly
            Get
                CheckDefinitionInvariant()
                Return Me.ReturnValueIsMarshalledExplicitly
            End Get
        End Property

        Friend Overridable ReadOnly Property ReturnValueIsMarshalledExplicitly As Boolean
            Get
                CheckDefinitionInvariant()
                Return ReturnTypeMarshallingInformation IsNot Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionReturnValueMarshallingInformation As Cci.IMarshallingInformation Implements Cci.IMethodDefinition.ReturnValueMarshallingInformation
            Get
                CheckDefinitionInvariant()
                Return ReturnTypeMarshallingInformation
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionReturnValueMarshallingDescriptor As ImmutableArray(Of Byte) Implements Cci.IMethodDefinition.ReturnValueMarshallingDescriptor
            Get
                CheckDefinitionInvariant()
                Return ReturnValueMarshallingDescriptor
            End Get
        End Property

        Friend Overridable ReadOnly Property ReturnValueMarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionSecurityAttributes As IEnumerable(Of Cci.SecurityAttribute) Implements Cci.IMethodDefinition.SecurityAttributes
            Get
                CheckDefinitionInvariant()
                Debug.Assert(Me.HasDeclarativeSecurity)
                Dim securityAttributes As IEnumerable(Of Cci.SecurityAttribute) = Me.GetSecurityInformation()
                Debug.Assert(securityAttributes IsNot Nothing)
                Return securityAttributes
            End Get
        End Property

        Private ReadOnly Property IMethodDefinition_ContainingNamespace As Cci.INamespace Implements Cci.IMethodDefinition.ContainingNamespace
            Get
                Return ContainingNamespace
            End Get
        End Property
    End Class
End Namespace
