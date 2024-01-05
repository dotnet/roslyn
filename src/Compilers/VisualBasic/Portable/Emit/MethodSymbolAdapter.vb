' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
#If DEBUG Then
    Partial Friend Class MethodSymbolAdapter
        Inherits SymbolAdapter
#Else
    Partial Friend Class MethodSymbol
#End If
        Implements Cci.ITypeMemberReference
        Implements Cci.IMethodReference
        Implements Cci.IGenericMethodInstanceReference
        Implements Cci.ISpecializedMethodReference
        Implements Cci.ITypeDefinitionMember
        Implements Cci.IMethodDefinition

        Private ReadOnly Property IDefinition_IsEncDeleted As Boolean Implements Cci.IDefinition.IsEncDeleted
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceAsGenericMethodInstanceReference As Cci.IGenericMethodInstanceReference Implements Cci.IMethodReference.AsGenericMethodInstanceReference
            Get
                Debug.Assert(Me.IsDefinitionOrDistinct())

                If Not AdaptedMethodSymbol.IsDefinition AndAlso AdaptedMethodSymbol.IsGenericMethod AndAlso AdaptedMethodSymbol IsNot AdaptedMethodSymbol.ConstructedFrom Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceAsSpecializedMethodReference As Cci.ISpecializedMethodReference Implements Cci.IMethodReference.AsSpecializedMethodReference
            Get
                Debug.Assert(Me.IsDefinitionOrDistinct())

                If Not AdaptedMethodSymbol.IsDefinition AndAlso (Not AdaptedMethodSymbol.IsGenericMethod OrElse AdaptedMethodSymbol Is AdaptedMethodSymbol.ConstructedFrom) Then
                    Debug.Assert(AdaptedMethodSymbol.ContainingType IsNot Nothing AndAlso IsOrInGenericType(AdaptedMethodSymbol.ContainingType))
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

            If Not AdaptedMethodSymbol.IsDefinition Then
                Return moduleBeingBuilt.Translate(AdaptedMethodSymbol.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            ElseIf TypeOf AdaptedMethodSymbol Is SynthesizedGlobalMethodBase Then
                Dim privateImplClass = moduleBeingBuilt.GetPrivateImplClass(syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
                Debug.Assert(privateImplClass IsNot Nothing)
                Return privateImplClass
            End If

            Return moduleBeingBuilt.Translate(AdaptedMethodSymbol.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics, needDeclaration:=True)
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As Cci.MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not AdaptedMethodSymbol.IsDefinition Then
                If AdaptedMethodSymbol.IsGenericMethod AndAlso AdaptedMethodSymbol IsNot AdaptedMethodSymbol.ConstructedFrom Then
                    Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)
                    visitor.Visit(DirectCast(Me, Cci.IGenericMethodInstanceReference))
                Else
                    Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsSpecializedMethodReference IsNot Nothing)
                    visitor.Visit(DirectCast(Me, Cci.ISpecializedMethodReference))
                End If
            Else
                Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(visitor.Context.Module, PEModuleBuilder)
                If AdaptedMethodSymbol.ContainingModule = moduleBeingBuilt.SourceModule Then
                    Debug.Assert((DirectCast(Me, Cci.IMethodReference)).GetResolvedMethod(visitor.Context) IsNot Nothing)
                    visitor.Visit(DirectCast(Me, Cci.IMethodDefinition))
                Else
                    visitor.Visit(DirectCast(Me, Cci.IMethodReference))
                End If
            End If
        End Sub

        Private ReadOnly Property INamedEntityName As String Implements Cci.INamedEntity.Name
            Get
                Return AdaptedMethodSymbol.MetadataName
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceAcceptsExtraArguments As Boolean Implements Cci.IMethodReference.AcceptsExtraArguments
            Get
                Return AdaptedMethodSymbol.IsVararg
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceGenericParameterCount As UShort Implements Cci.IMethodReference.GenericParameterCount
            Get
                Return CType(AdaptedMethodSymbol.Arity, UShort)
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceParameterCount As UShort Implements Cci.ISignature.ParameterCount
            Get
                Return CType(AdaptedMethodSymbol.ParameterCount, UShort)
            End Get
        End Property

        Private Function IMethodReferenceGetResolvedMethod(context As EmitContext) As Cci.IMethodDefinition Implements Cci.IMethodReference.GetResolvedMethod
            Return ResolvedMethodImpl(DirectCast(context.Module, PEModuleBuilder))
        End Function

        Private Function ResolvedMethodImpl(moduleBeingBuilt As PEModuleBuilder) As Cci.IMethodDefinition
            Debug.Assert(Me.IsDefinitionOrDistinct())

            ' Can't be generic instantiation
            ' must be declared in the module we are building
            If AdaptedMethodSymbol.IsDefinition AndAlso
                AdaptedMethodSymbol.ContainingModule = moduleBeingBuilt.SourceModule Then
                Debug.Assert(AdaptedMethodSymbol.PartialDefinitionPart Is Nothing) ' must be definition
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
                Return AdaptedMethodSymbol.CallingConvention
            End Get
        End Property

        Private Function ISignatureGetParameters(context As EmitContext) As ImmutableArray(Of Cci.IParameterTypeInformation) Implements Cci.ISignature.GetParameters
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Debug.Assert(Me.IsDefinitionOrDistinct())
#If DEBUG Then
            For Each p In AdaptedMethodSymbol.Parameters
                Debug.Assert(p Is p.OriginalDefinition)
            Next
#End If

            If AdaptedMethodSymbol.IsDefinition AndAlso AdaptedMethodSymbol.ContainingModule = moduleBeingBuilt.SourceModule Then
                Return EnumerateDefinitionParameters()
            Else
                Return moduleBeingBuilt.Translate(AdaptedMethodSymbol.Parameters)
            End If
        End Function

        Private Function EnumerateDefinitionParameters() As ImmutableArray(Of Cci.IParameterTypeInformation)
            Debug.Assert(AdaptedMethodSymbol.Parameters.All(Function(p) p.IsDefinition))
#If DEBUG Then
            Return AdaptedMethodSymbol.Parameters.SelectAsArray(Of Cci.IParameterTypeInformation)(Function(p) p.GetCciAdapter())
#Else
            Return StaticCast(Of Cci.IParameterTypeInformation).From(AdaptedMethodSymbol.Parameters)
#End If
        End Function

        Private ReadOnly Property ISignatureReturnValueCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.ISignature.ReturnValueCustomModifiers
            Get
                Return AdaptedMethodSymbol.ReturnTypeCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureRefCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.ISignature.RefCustomModifiers
            Get
                Return AdaptedMethodSymbol.RefCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsByRef As Boolean Implements Cci.ISignature.ReturnValueIsByRef
            Get
                Return AdaptedMethodSymbol.ReturnsByRef
            End Get
        End Property

        Private Function ISignatureGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.ISignature.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Dim returnType As TypeSymbol = AdaptedMethodSymbol.ReturnType
            Return moduleBeingBuilt.Translate(returnType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function IGenericMethodInstanceReferenceGetGenericArguments(context As EmitContext) As IEnumerable(Of Cci.ITypeReference) Implements Cci.IGenericMethodInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)

            Return From arg In AdaptedMethodSymbol.TypeArguments
                   Select moduleBeingBuilt.Translate(arg, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function IGenericMethodInstanceReferenceGetGenericMethod(context As EmitContext) As Cci.IMethodReference Implements Cci.IGenericMethodInstanceReference.GetGenericMethod
            Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsGenericMethodInstanceReference IsNot Nothing)

            Dim container As NamedTypeSymbol = AdaptedMethodSymbol.ContainingType

            If (Not container.IsOrInGenericType()) Then
                ' NoPia method might come through here.
                Return DirectCast(context.Module, PEModuleBuilder).Translate(
                    AdaptedMethodSymbol.OriginalDefinition,
                    DirectCast(context.SyntaxNode, VisualBasicSyntaxNode),
                    context.Diagnostics,
                    needDeclaration:=True)
            End If

            Dim methodSymbol As MethodSymbol = AdaptedMethodSymbol.ConstructedFrom
            Return New SpecializedMethodReference(methodSymbol)
        End Function

        Private ReadOnly Property ISpecializedMethodReferenceUnspecializedVersion As Cci.IMethodReference Implements Cci.ISpecializedMethodReference.UnspecializedVersion
            Get
                Debug.Assert((DirectCast(Me, Cci.IMethodReference)).AsSpecializedMethodReference IsNot Nothing)
                Return AdaptedMethodSymbol.OriginalDefinition.GetCciAdapter()
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As Cci.ITypeDefinition Implements Cci.ITypeDefinitionMember.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()

                Dim synthesizedGlobalMethod = TryCast(AdaptedMethodSymbol, SynthesizedGlobalMethodBase)
                If synthesizedGlobalMethod IsNot Nothing Then
                    Return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType
                End If

                Return AdaptedMethodSymbol.ContainingType.GetCciAdapter()
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberVisibility As Cci.TypeMemberVisibility Implements Cci.ITypeDefinitionMember.Visibility
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.MetadataVisibility
            End Get
        End Property

        Private ReadOnly Property HasBody As Boolean Implements Cci.IMethodDefinition.HasBody
            Get
                CheckDefinitionInvariant()
                Return DefaultImplementations.HasBody(Me)
            End Get
        End Property

        Private Function IMethodDefinitionGetBody(context As EmitContext) As Cci.IMethodBody Implements Cci.IMethodDefinition.GetBody
            CheckDefinitionInvariant()
            Return (DirectCast(context.Module, PEModuleBuilder)).GetMethodBody(AdaptedMethodSymbol)
        End Function

        Private ReadOnly Property IMethodDefinitionGenericParameters As IEnumerable(Of Cci.IGenericMethodParameter) Implements Cci.IMethodDefinition.GenericParameters
            Get
                CheckDefinitionInvariant()
                Debug.Assert(AdaptedMethodSymbol.TypeParameters.All(Function(param) param Is param.OriginalDefinition))
#If DEBUG Then
                Return AdaptedMethodSymbol.TypeParameters.Select(Function(t) t.GetCciAdapter())
#Else
                Return AdaptedMethodSymbol.TypeParameters
#End If
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionHasDeclarativeSecurity As Boolean Implements Cci.IMethodDefinition.HasDeclarativeSecurity
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.HasDeclarativeSecurity
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsAbstract As Boolean Implements Cci.IMethodDefinition.IsAbstract
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.IsMustOverride
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsAccessCheckedOnOverride As Boolean Implements Cci.IMethodDefinition.IsAccessCheckedOnOverride
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.IsAccessCheckedOnOverride
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsConstructor As Boolean Implements Cci.IMethodDefinition.IsConstructor
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.MethodKind = MethodKind.Constructor
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsExternal As Boolean Implements Cci.IMethodDefinition.IsExternal
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.IsExternal
            End Get
        End Property

        Private Function IMethodDefinitionGetImplementationOptions(context As EmitContext) As MethodImplAttributes Implements Cci.IMethodDefinition.GetImplementationAttributes
            CheckDefinitionInvariant()
            Return AdaptedMethodSymbol.ImplementationAttributes Or
                   If(DirectCast(context.Module, PEModuleBuilder).JITOptimizationIsDisabled(AdaptedMethodSymbol), MethodSymbol.DisableJITOptimizationFlags, Nothing)
        End Function

        Private ReadOnly Property IMethodDefinitionIsHiddenBySignature As Boolean Implements Cci.IMethodDefinition.IsHiddenBySignature
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.IsHiddenBySignature
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsNewSlot As Boolean Implements Cci.IMethodDefinition.IsNewSlot
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.IsMetadataNewSlot()
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsPlatformInvoke As Boolean Implements Cci.IMethodDefinition.IsPlatformInvoke
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.GetDllImportData() IsNot Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionPlatformInvokeData As Cci.IPlatformInvokeInformation Implements Cci.IMethodDefinition.PlatformInvokeData
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.GetDllImportData()
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsRuntimeSpecial As Boolean Implements Cci.IMethodDefinition.IsRuntimeSpecial
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.HasRuntimeSpecialName
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsSealed As Boolean Implements Cci.IMethodDefinition.IsSealed
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.IsMetadataFinal
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsSpecialName As Boolean Implements Cci.IMethodDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.HasSpecialName
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsStatic As Boolean Implements Cci.IMethodDefinition.IsStatic
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.IsShared
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionIsVirtual As Boolean Implements Cci.IMethodDefinition.IsVirtual
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.IsMetadataVirtual()
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionParameters As ImmutableArray(Of Cci.IParameterDefinition) Implements Cci.IMethodDefinition.Parameters
            Get
                CheckDefinitionInvariant()

#If DEBUG Then
                For Each p In AdaptedMethodSymbol.Parameters
                    Debug.Assert(p Is p.OriginalDefinition)
                Next

                Return AdaptedMethodSymbol.Parameters.SelectAsArray(Of Cci.IParameterDefinition)(Function(p) p.GetCciAdapter())
#Else
                Return StaticCast(Of Cci.IParameterDefinition).From(AdaptedMethodSymbol.Parameters)
#End If
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

            userDefined = AdaptedMethodSymbol.GetReturnTypeAttributes()
            AdaptedMethodSymbol.AddSynthesizedReturnTypeAttributes(synthesized)

            ' Note that callers of this method (CCI and ReflectionEmitter) have to enumerate 
            ' all items of the returned iterator, otherwise the synthesized ArrayBuilder may leak.
            Return AdaptedMethodSymbol.GetCustomAttributesToEmit(userDefined, synthesized, isReturnType:=True, emittingAssemblyAttributesInNetModule:=False)
        End Function

        Private ReadOnly Property IMethodDefinitionReturnValueIsMarshalledExplicitly As Boolean Implements Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.ReturnValueIsMarshalledExplicitly
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionReturnValueMarshallingInformation As Cci.IMarshallingInformation Implements Cci.IMethodDefinition.ReturnValueMarshallingInformation
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.ReturnTypeMarshallingInformation
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionReturnValueMarshallingDescriptor As ImmutableArray(Of Byte) Implements Cci.IMethodDefinition.ReturnValueMarshallingDescriptor
            Get
                CheckDefinitionInvariant()
                Return AdaptedMethodSymbol.ReturnValueMarshallingDescriptor
            End Get
        End Property

        Private ReadOnly Property IMethodDefinitionSecurityAttributes As IEnumerable(Of Cci.SecurityAttribute) Implements Cci.IMethodDefinition.SecurityAttributes
            Get
                CheckDefinitionInvariant()
                Debug.Assert(AdaptedMethodSymbol.HasDeclarativeSecurity)
                Dim securityAttributes As IEnumerable(Of Cci.SecurityAttribute) = AdaptedMethodSymbol.GetSecurityInformation()
                Debug.Assert(securityAttributes IsNot Nothing)
                Return securityAttributes
            End Get
        End Property

        Private ReadOnly Property IMethodDefinition_ContainingNamespace As Cci.INamespace Implements Cci.IMethodDefinition.ContainingNamespace
            Get
                Return AdaptedMethodSymbol.ContainingNamespace.GetCciAdapter()
            End Get
        End Property
    End Class

    Partial Friend Class MethodSymbol
#If DEBUG Then
        Private _lazyAdapter As MethodSymbolAdapter

        Protected NotOverridable Overrides Function GetCciAdapterImpl() As SymbolAdapter
            Return GetCciAdapter()
        End Function

        Friend Shadows Function GetCciAdapter() As MethodSymbolAdapter
            If _lazyAdapter Is Nothing Then
                Return InterlockedOperations.Initialize(_lazyAdapter, Me.CreateCciAdapter())
            End If

            Return _lazyAdapter
        End Function

        Protected Overridable Function CreateCciAdapter() As MethodSymbolAdapter
            Return New MethodSymbolAdapter(Me)
        End Function
#Else
        Friend ReadOnly Property AdaptedMethodSymbol As MethodSymbol
            Get
                Return Me
            End Get
        End Property

        Friend Shadows Function GetCciAdapter() As MethodSymbol
            Return Me
        End Function
#End If

        Public Const DisableJITOptimizationFlags As MethodImplAttributes = MethodImplAttributes.NoInlining Or MethodImplAttributes.NoOptimization

        Friend Overridable ReadOnly Property IsAccessCheckedOnOverride As Boolean Implements IMethodSymbolInternal.IsAccessCheckedOnOverride
            Get
                Return Me.IsMetadataVirtual ' VB always sets this for methods where virtual is set.
            End Get
        End Property

        Friend Overridable ReadOnly Property IsExternal As Boolean Implements IMethodSymbolInternal.IsExternal
            Get
                Return Me.IsExternalMethod
            End Get
        End Property

        Friend Overridable ReadOnly Property IsHiddenBySignature As Boolean Implements IMethodSymbolInternal.IsHiddenBySignature
            Get
                Return Me.IsOverloads
            End Get
        End Property

        Private ReadOnly Property IMethodSymbolInternal_IsPlatformInvoke As Boolean Implements IMethodSymbolInternal.IsPlatformInvoke
            Get
                Return Me.GetDllImportData() IsNot Nothing
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

        Private ReadOnly Property IMethodSymbolInternal_IsMetadataNewSlot As Boolean Implements IMethodSymbolInternal.IsMetadataNewSlot
            Get
                Return IsMetadataNewSlot()
            End Get
        End Property

        Friend Overridable ReadOnly Property HasRuntimeSpecialName As Boolean Implements IMethodSymbolInternal.HasRuntimeSpecialName
            Get
                CheckDefinitionInvariant()
                Dim result = Me.MethodKind = MethodKind.Constructor OrElse
                             Me.MethodKind = MethodKind.SharedConstructor

                ' runtime-special must be special:
                Debug.Assert(Not result OrElse HasSpecialName)

                Return result
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMetadataFinal As Boolean Implements IMethodSymbolInternal.IsMetadataFinal
            Get
                ' If we are metadata virtual, but not language virtual, set the "final" bit (i.e., interface
                ' implementation methods). Also do it if we are explicitly marked "NotOverridable".
                Return Me.IsNotOverridable OrElse
                    (Me.IsMetadataVirtual AndAlso Not (Me.IsOverridable OrElse Me.IsMustOverride OrElse Me.IsOverrides))
            End Get
        End Property

        Friend Overridable ReadOnly Property ReturnValueIsMarshalledExplicitly As Boolean
            Get
                CheckDefinitionInvariant()
                Return ReturnTypeMarshallingInformation IsNot Nothing
            End Get
        End Property

        Friend Overridable ReadOnly Property ReturnValueMarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property
    End Class

#If DEBUG Then
    Partial Friend Class MethodSymbolAdapter
        Friend ReadOnly Property AdaptedMethodSymbol As MethodSymbol

        Friend Sub New(underlyingMethodSymbol As MethodSymbol)
            AdaptedMethodSymbol = underlyingMethodSymbol
        End Sub

        Friend Overrides ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return AdaptedMethodSymbol
            End Get
        End Property
    End Class
#End If
End Namespace
