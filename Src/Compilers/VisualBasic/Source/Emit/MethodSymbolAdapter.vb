' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Class MethodSymbol
        Implements ITypeMemberReference
        Implements IMethodReference
        Implements IGenericMethodInstanceReference
        Implements ISpecializedMethodReference
        Implements ITypeDefinitionMember
        Implements IMethodDefinition

        Public Const DisableJITOptimizationFlags As Reflection.MethodImplAttributes = Reflection.MethodImplAttributes.NoInlining Or Reflection.MethodImplAttributes.NoOptimization

        Private ReadOnly Property IMethodReferenceAsGenericMethodInstanceReference As IGenericMethodInstanceReference Implements IMethodReference.AsGenericMethodInstanceReference
            Get
                Debug.Assert(Me.IsDefinitionOrDistinct())

                If Not Me.IsDefinition AndAlso Me.IsGenericMethod AndAlso Me IsNot Me.ConstructedFrom Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceAsSpecializedMethodReference As ISpecializedMethodReference Implements IMethodReference.AsSpecializedMethodReference
            Get
                Debug.Assert(Me.IsDefinitionOrDistinct())

                If Not Me.IsDefinition AndAlso (Not Me.IsGenericMethod OrElse Me Is Me.ConstructedFrom) Then
                    Debug.Assert(Me.ContainingType IsNot Nothing AndAlso IsOrInGenericType(Me.ContainingType))
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As IDefinition ' Implements IReference.AsDefinition
            Return ResolvedMethodImpl(DirectCast(context.Module, PEModuleBuilder))
        End Function

        Private Function ITypeMemberReferenceGetContainingType(context As Microsoft.CodeAnalysis.Emit.Context) As ITypeReference Implements ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not Me.IsDefinition Then
                Return moduleBeingBuilt.Translate(Me.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            ElseIf TypeOf Me Is SynthesizedGlobalMethodBase Then
                Dim privateImplClass = moduleBeingBuilt.GetPrivateImplClass(syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
                Debug.Assert(privateImplClass IsNot Nothing)
                Return privateImplClass
            End If

            Return Me.ContainingType
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(visitor.Context.Module, PEModuleBuilder)

            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not Me.IsDefinition Then
                If Me.IsGenericMethod AndAlso Me IsNot Me.ConstructedFrom Then
                    Debug.Assert((DirectCast(Me, IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)
                    visitor.Visit(DirectCast(Me, IGenericMethodInstanceReference))
                Else
                    Debug.Assert((DirectCast(Me, IMethodReference)).AsSpecializedMethodReference IsNot Nothing)
                    visitor.Visit(DirectCast(Me, ISpecializedMethodReference))
                End If
            Else
                If Me.ContainingModule = moduleBeingBuilt.SourceModule Then
                    Debug.Assert((DirectCast(Me, IMethodReference)).GetResolvedMethod(visitor.Context) IsNot Nothing)
                    visitor.Visit(DirectCast(Me, IMethodDefinition))
                Else
                    visitor.Visit(DirectCast(Me, IMethodReference))
                End If
            End If
        End Sub

        Private ReadOnly Property INamedEntityName As String Implements INamedEntity.Name
            Get
                Return Me.MetadataName
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceAcceptsExtraArguments As Boolean Implements IMethodReference.AcceptsExtraArguments
            Get
                Return Me.IsVararg
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceGenericParameterCount As UShort Implements IMethodReference.GenericParameterCount
            Get
                Return CType(Me.Arity, UShort)
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceIsGeneric As Boolean Implements IMethodReference.IsGeneric
            Get
                Return Me.IsGenericMethod
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceParameterCount As UShort Implements ISignature.ParameterCount
            Get
                Return CType(Me.ParameterCount, UShort)
            End Get
        End Property

        Private Function IMethodReferenceGetResolvedMethod(context As Microsoft.CodeAnalysis.Emit.Context) As IMethodDefinition Implements IMethodReference.GetResolvedMethod
            Return ResolvedMethodImpl(DirectCast(context.Module, PEModuleBuilder))
        End Function

        Private Function ResolvedMethodImpl(moduleBeingBuilt As PEModuleBuilder) As IMethodDefinition
            Debug.Assert(Me.IsDefinitionOrDistinct())

            ' Can't be generic instantiation
            ' must be declared in the module we are building
            If Me.IsDefinition AndAlso
               Me.ContainingModule = moduleBeingBuilt.SourceModule Then
                Return Me
            End If

            Return Nothing
        End Function

        Private ReadOnly Property IMethodReferenceExtraParameters As ImmutableArray(Of IParameterTypeInformation) Implements IMethodReference.ExtraParameters
            Get
                Return ImmutableArray(Of IParameterTypeInformation).Empty
            End Get
        End Property

        Private ReadOnly Property ISignatureCallingConvention As CallingConvention Implements ISignature.CallingConvention
            Get
                Return Me.CallingConvention
            End Get
        End Property

        Private Function ISignatureGetParameters(context As Microsoft.CodeAnalysis.Emit.Context) As ImmutableArray(Of IParameterTypeInformation) Implements ISignature.GetParameters
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

            Return StaticCast(Of IParameterTypeInformation).From(Me.Parameters)
        End Function

        Private ReadOnly Property ISignatureReturnValueCustomModifiers As IEnumerable(Of Microsoft.Cci.ICustomModifier) Implements ISignature.ReturnValueCustomModifiers
            Get
                Return Me.ReturnTypeCustomModifiers
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsByRef As Boolean Implements ISignature.ReturnValueIsByRef
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsModified As Boolean Implements ISignature.ReturnValueIsModified
            Get
                Return Me.ReturnTypeCustomModifiers.Length <> 0
            End Get
        End Property

        Private Function ISignatureGetType(context As Microsoft.CodeAnalysis.Emit.Context) As ITypeReference Implements ISignature.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Dim returnType As TypeSymbol = Me.ReturnType
            Return moduleBeingBuilt.Translate(returnType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function IGenericMethodInstanceReferenceGetGenericArguments(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of ITypeReference) Implements IGenericMethodInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Debug.Assert((DirectCast(Me, IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)

            Return From arg In Me.TypeArguments
                   Select moduleBeingBuilt.Translate(arg, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function IGenericMethodInstanceReferenceGetGenericMethod(context As Microsoft.CodeAnalysis.Emit.Context) As IMethodReference Implements IGenericMethodInstanceReference.GetGenericMethod
            Debug.Assert((DirectCast(Me, IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)

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

        Private ReadOnly Property ISpecializedMethodReferenceUnspecializedVersion As IMethodReference Implements ISpecializedMethodReference.UnspecializedVersion
            Get
                Debug.Assert((DirectCast(Me, IMethodReference)).AsSpecializedMethodReference IsNot Nothing)
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As ITypeDefinition Implements ITypeDefinitionMember.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()

                Dim synthesizedGlobalMethod = TryCast(Me, SynthesizedGlobalMethodBase)
                If synthesizedGlobalMethod IsNot Nothing Then
                    Return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType
                End If

                Return Me.ContainingType
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberVisibility As TypeMemberVisibility Implements ITypeDefinitionMember.Visibility
            Get
                CheckDefinitionInvariant()
                Return PEModuleBuilder.MemberVisibility(Me)
            End Get
        End Property

        Private Function IMethodDefinitionGetBody(context As Microsoft.CodeAnalysis.Emit.Context) As IMethodBody Implements IMethodDefinition.GetBody
            CheckDefinitionInvariant()
            Return (DirectCast(context.Module, PEModuleBuilder)).GetMethodBody(Me)
        End Function

        Private ReadOnly Property IMethodDefinitionGenericParameters As IEnumerable(Of IGenericMethodParameter) Implements IMethodDefinition.GenericParameters
            Get
                CheckDefinitionInvariant()
                Debug.Assert(Me.TypeParameters.All(Function(param) param Is param.OriginalDefinition))
                Return Me.TypeParameters
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionHasDeclarativeSecurity As Boolean Implements IMethodDefinition.HasDeclarativeSecurity
            Get
                CheckDefinitionInvariant()
                Return Me.HasDeclarativeSecurity
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsAbstract As Boolean Implements IMethodDefinition.IsAbstract
            Get
                CheckDefinitionInvariant()
                Return Me.IsMustOverride
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsAccessCheckedOnOverride As Boolean Implements IMethodDefinition.IsAccessCheckedOnOverride
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

        Private ReadOnly Property IMethodDefinitionIsConstructor As Boolean Implements IMethodDefinition.IsConstructor
            Get
                CheckDefinitionInvariant()
                Return Me.MethodKind = MethodKind.Constructor
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsExternal As Boolean Implements IMethodDefinition.IsExternal
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

        Private Function IMethodDefinitionGetImplementationOptions(context As Microsoft.CodeAnalysis.Emit.Context) As System.Reflection.MethodImplAttributes Implements IMethodDefinition.GetImplementationAttributes
            CheckDefinitionInvariant()
            Return Me.ImplementationAttributes Or
                   If(DirectCast(context.Module, PEModuleBuilder).JITOptimizationIsDisabled(Me), DisableJITOptimizationFlags, Nothing)
        End Function

        Private ReadOnly Property IMethodDefinitionIsHiddenBySignature As Boolean Implements IMethodDefinition.IsHiddenBySignature
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

        Private ReadOnly Property IMethodDefinitionIsNewSlot As Boolean Implements IMethodDefinition.IsNewSlot
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

        Private ReadOnly Property IMethodDefinitionIsPlatformInvoke As Boolean Implements IMethodDefinition.IsPlatformInvoke
            Get
                CheckDefinitionInvariant()
                Return Me.GetDllImportData() IsNot Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionPlatformInvokeData As IPlatformInvokeInformation Implements IMethodDefinition.PlatformInvokeData
            Get
                CheckDefinitionInvariant()
                Return Me.GetDllImportData()
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsRuntimeSpecial As Boolean Implements IMethodDefinition.IsRuntimeSpecial
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

        Private ReadOnly Property IMethodDefinitionIsSealed As Boolean Implements IMethodDefinition.IsSealed
            Get
                CheckDefinitionInvariant()
                Return Me.HasFinalFlag
            End Get
        End Property

        Friend Overridable ReadOnly Property HasFinalFlag As Boolean
            Get
                CheckDefinitionInvariant()

                ' If we are metadata virtual, but not language virtual, set the "final" bit (i.e., interface
                ' implementation methods). Also do it if we are explicitly marked "NotOverridable".
                Return Me.IsNotOverridable OrElse
                    (Me.IsMetadataVirtual AndAlso Not (Me.IsOverridable OrElse Me.IsMustOverride OrElse Me.IsOverrides))
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsSpecialName As Boolean Implements IMethodDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return Me.HasSpecialName
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsStatic As Boolean Implements IMethodDefinition.IsStatic
            Get
                CheckDefinitionInvariant()
                Return Me.IsShared
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsVirtual As Boolean Implements IMethodDefinition.IsVirtual
            Get
                CheckDefinitionInvariant()
                Return Me.IsMetadataVirtual()
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionParameters As ImmutableArray(Of IParameterDefinition) Implements IMethodDefinition.Parameters
            Get
                CheckDefinitionInvariant()

#If DEBUG Then
                For Each p In Me.Parameters
                    Debug.Assert(p Is p.OriginalDefinition)
                Next
#End If
                Return StaticCast(Of IParameterDefinition).From(Me.Parameters)
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionRequiresSecurityObject As Boolean Implements IMethodDefinition.RequiresSecurityObject
            Get
                CheckDefinitionInvariant()
                Return False
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionReturnValueAttributes As IEnumerable(Of ICustomAttribute) Implements IMethodDefinition.ReturnValueAttributes
            Get
                CheckDefinitionInvariant()
                Return GetCustomAttributesToEmit(Me.GetReturnTypeAttributes(), synthesized:=Nothing, isReturnType:=True, emittingAssemblyAttributesInNetModule:=False)
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionReturnValueIsMarshalledExplicitly As Boolean Implements IMethodDefinition.ReturnValueIsMarshalledExplicitly
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

        Private ReadOnly Property IMethodDefinitionReturnValueMarshallingInformation As IMarshallingInformation Implements IMethodDefinition.ReturnValueMarshallingInformation
            Get
                CheckDefinitionInvariant()
                Return ReturnTypeMarshallingInformation
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionReturnValueMarshallingDescriptor As ImmutableArray(Of Byte) Implements IMethodDefinition.ReturnValueMarshallingDescriptor
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

        Private ReadOnly Property IMethodDefinitionSecurityAttributes As IEnumerable(Of SecurityAttribute) Implements IMethodDefinition.SecurityAttributes
            Get
                CheckDefinitionInvariant()
                Debug.Assert(Me.HasDeclarativeSecurity)
                Dim securityAttributes As IEnumerable(Of SecurityAttribute) = Me.GetSecurityInformation()
                Debug.Assert(securityAttributes IsNot Nothing)
                Return securityAttributes
            End Get
        End Property
    End Class
End Namespace
