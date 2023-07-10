// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        MethodSymbolAdapter : SymbolAdapter,
#else
        MethodSymbol :
#endif 
        Cci.ITypeMemberReference,
        Cci.IMethodReference,
        Cci.IGenericMethodInstanceReference,
        Cci.ISpecializedMethodReference,
        Cci.ITypeDefinitionMember,
        Cci.IMethodDefinition
    {
        Cci.IGenericMethodInstanceReference Cci.IMethodReference.AsGenericMethodInstanceReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (!AdaptedMethodSymbol.IsDefinition &&
                    AdaptedMethodSymbol.IsGenericMethod)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.ISpecializedMethodReference Cci.IMethodReference.AsSpecializedMethodReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (!AdaptedMethodSymbol.IsDefinition &&
                    (!AdaptedMethodSymbol.IsGenericMethod || PEModuleBuilder.IsGenericType(AdaptedMethodSymbol.ContainingType)))
                {
                    Debug.Assert((object)AdaptedMethodSymbol.ContainingType != null &&
                            PEModuleBuilder.IsGenericType(AdaptedMethodSymbol.ContainingType));
                    return this;
                }

                return null;
            }
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return ResolvedMethodImpl(context);
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            var synthesizedGlobalMethod = AdaptedMethodSymbol.OriginalDefinition as SynthesizedGlobalMethodSymbol;
            if ((object)synthesizedGlobalMethod != null)
            {
                return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType;
            }

            NamedTypeSymbol containingType = AdaptedMethodSymbol.ContainingType;
            var moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return moduleBeingBuilt.Translate(containingType,
                syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                diagnostics: context.Diagnostics,
                needDeclaration: AdaptedMethodSymbol.IsDefinition);
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (!AdaptedMethodSymbol.IsDefinition)
            {
                if (AdaptedMethodSymbol.IsGenericMethod)
                {
                    Debug.Assert(((Cci.IMethodReference)this).AsGenericMethodInstanceReference != null);
                    visitor.Visit((Cci.IGenericMethodInstanceReference)this);
                }
                else
                {
                    Debug.Assert(((Cci.IMethodReference)this).AsSpecializedMethodReference != null);
                    visitor.Visit((Cci.ISpecializedMethodReference)this);
                }
            }
            else
            {
                PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)visitor.Context.Module;
                if (AdaptedMethodSymbol.ContainingModule == moduleBeingBuilt.SourceModule)
                {
                    Debug.Assert(((Cci.IMethodReference)this).GetResolvedMethod(visitor.Context) != null);
                    visitor.Visit((Cci.IMethodDefinition)this);
                }
                else
                {
                    visitor.Visit((Cci.IMethodReference)this);
                }
            }
        }

        string Cci.INamedEntity.Name
        {
            get { return AdaptedMethodSymbol.MetadataName; }
        }

        bool Cci.IMethodReference.AcceptsExtraArguments
        {
            get
            {
                return AdaptedMethodSymbol.IsVararg;
            }
        }

        ushort Cci.IMethodReference.GenericParameterCount
        {
            get
            {
                return (ushort)AdaptedMethodSymbol.Arity;
            }
        }

        bool Cci.IMethodReference.IsGeneric
        {
            get
            {
                return AdaptedMethodSymbol.IsGenericMethod;
            }
        }

        ushort Cci.ISignature.ParameterCount
        {
            get
            {
                return (ushort)AdaptedMethodSymbol.ParameterCount;
            }
        }

        Cci.IMethodDefinition Cci.IMethodReference.GetResolvedMethod(EmitContext context)
        {
            return ResolvedMethodImpl(context);
        }

        private Cci.IMethodDefinition ResolvedMethodImpl(EmitContext context)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            if (AdaptedMethodSymbol.IsDefinition && // can't be generic instantiation
                AdaptedMethodSymbol.ContainingModule == moduleBeingBuilt.SourceModule) // must be declared in the module we are building
            {
                Debug.Assert((object)AdaptedMethodSymbol.PartialDefinitionPart == null); // must be definition
                return this;
            }

            return null;
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.IMethodReference.ExtraParameters
        {
            get
            {
                return ImmutableArray<Cci.IParameterTypeInformation>.Empty;
            }
        }

        Cci.CallingConvention Cci.ISignature.CallingConvention
        {
            get
            {
                return AdaptedMethodSymbol.CallingConvention;
            }
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            if (AdaptedMethodSymbol.IsDefinition && AdaptedMethodSymbol.ContainingModule == moduleBeingBuilt.SourceModule)
            {
                return StaticCast<Cci.IParameterTypeInformation>.From(this.EnumerateDefinitionParameters());
            }
            else
            {
                return moduleBeingBuilt.Translate(AdaptedMethodSymbol.Parameters);
            }
        }

        private ImmutableArray<Cci.IParameterDefinition> EnumerateDefinitionParameters()
        {
            Debug.Assert(AdaptedMethodSymbol.Parameters.All(p => p.IsDefinition));

#if DEBUG
            return AdaptedMethodSymbol.Parameters.SelectAsArray<ParameterSymbol, Cci.IParameterDefinition>(p => p.GetCciAdapter());
#else
            return StaticCast<Cci.IParameterDefinition>.From(AdaptedMethodSymbol.Parameters);
#endif
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(AdaptedMethodSymbol.ReturnTypeWithAnnotations.CustomModifiers);
            }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.RefCustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(AdaptedMethodSymbol.RefCustomModifiers);
            }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get
            {
                return AdaptedMethodSymbol.RefKind.IsManagedReference();
            }
        }

        Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(AdaptedMethodSymbol.ReturnType,
                syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                diagnostics: context.Diagnostics);
        }

        IEnumerable<Cci.ITypeReference> Cci.IGenericMethodInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.IMethodReference)this).AsGenericMethodInstanceReference != null);

            foreach (var arg in AdaptedMethodSymbol.TypeArgumentsWithAnnotations)
            {
                Debug.Assert(arg.CustomModifiers.IsEmpty);
                yield return moduleBeingBuilt.Translate(arg.Type,
                                                        syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                        diagnostics: context.Diagnostics);
            }
        }

        Cci.IMethodReference Cci.IGenericMethodInstanceReference.GetGenericMethod(EmitContext context)
        {
            Debug.Assert(((Cci.IMethodReference)this).AsGenericMethodInstanceReference != null);

            NamedTypeSymbol container = AdaptedMethodSymbol.ContainingType;

            if (!PEModuleBuilder.IsGenericType(container))
            {
                // NoPia method might come through here.
                return ((PEModuleBuilder)context.Module).Translate(
                    (MethodSymbol)AdaptedMethodSymbol.OriginalDefinition,
                    syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                    diagnostics: context.Diagnostics,
                    needDeclaration: true);
            }

            MethodSymbol methodSymbol = AdaptedMethodSymbol.ConstructedFrom;

            return new SpecializedMethodReference(methodSymbol);
        }

        Cci.IMethodReference Cci.ISpecializedMethodReference.UnspecializedVersion
        {
            get
            {
                Debug.Assert(((Cci.IMethodReference)this).AsSpecializedMethodReference != null);
                return ((MethodSymbol)AdaptedMethodSymbol.OriginalDefinition).GetCciAdapter();
            }
        }

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();

                var synthesizedGlobalMethod = AdaptedMethodSymbol.OriginalDefinition as SynthesizedGlobalMethodSymbol;
                if ((object)synthesizedGlobalMethod != null)
                {
                    return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType;
                }

                return AdaptedMethodSymbol.ContainingType.GetCciAdapter();
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return PEModuleBuilder.MemberVisibility(AdaptedMethodSymbol);
            }
        }

        Cci.IMethodBody Cci.IMethodDefinition.GetBody(EmitContext context)
        {
            CheckDefinitionInvariant();
            return ((PEModuleBuilder)context.Module).GetMethodBody(AdaptedMethodSymbol);
        }

        IEnumerable<Cci.IGenericMethodParameter> Cci.IMethodDefinition.GenericParameters
        {
            get
            {
                CheckDefinitionInvariant();

                foreach (var @param in AdaptedMethodSymbol.TypeParameters)
                {
                    Debug.Assert(@param.IsDefinition);
                    yield return @param.GetCciAdapter();
                }
            }
        }

        bool Cci.IMethodDefinition.HasDeclarativeSecurity
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.HasDeclarativeSecurity;
            }
        }

        IEnumerable<Cci.SecurityAttribute> Cci.IMethodDefinition.SecurityAttributes
        {
            get
            {
                CheckDefinitionInvariant();
                Debug.Assert(AdaptedMethodSymbol.HasDeclarativeSecurity);
                return AdaptedMethodSymbol.GetSecurityInformation();
            }
        }

        bool Cci.IMethodDefinition.IsAbstract
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.IsAbstract;
            }
        }

        bool Cci.IMethodDefinition.IsAccessCheckedOnOverride
        {
            get
            {
                CheckDefinitionInvariant();

                return AdaptedMethodSymbol.IsAccessCheckedOnOverride;
            }
        }

        bool Cci.IMethodDefinition.IsConstructor
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.MethodKind == MethodKind.Constructor;
            }
        }

        bool Cci.IMethodDefinition.IsExternal
        {
            get
            {
                CheckDefinitionInvariant();

                return AdaptedMethodSymbol.IsExternal;
            }
        }

        bool Cci.IMethodDefinition.IsHiddenBySignature
        {
            get
            {
                CheckDefinitionInvariant();
                return !AdaptedMethodSymbol.HidesBaseMethodsByName;
            }
        }

        bool Cci.IMethodDefinition.IsNewSlot
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.IsMetadataNewSlot();
            }
        }

        bool Cci.IMethodDefinition.IsPlatformInvoke
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.GetDllImportData() != null;
            }
        }

        Cci.IPlatformInvokeInformation Cci.IMethodDefinition.PlatformInvokeData
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.GetDllImportData();
            }
        }

        System.Reflection.MethodImplAttributes Cci.IMethodDefinition.GetImplementationAttributes(EmitContext context)
        {
            CheckDefinitionInvariant();
            return AdaptedMethodSymbol.ImplementationAttributes;
        }

        bool Cci.IMethodDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.HasRuntimeSpecialName;
            }
        }

        bool Cci.IMethodDefinition.IsSealed
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.IsMetadataFinal;
            }
        }

        bool Cci.IMethodDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.HasSpecialName;
            }
        }

        bool Cci.IMethodDefinition.IsStatic
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.IsStatic;
            }
        }

        bool Cci.IMethodDefinition.IsVirtual
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.IsMetadataVirtual();
            }
        }

        ImmutableArray<Cci.IParameterDefinition> Cci.IMethodDefinition.Parameters
        {
            get
            {
                CheckDefinitionInvariant();
                return EnumerateDefinitionParameters();
            }
        }

        bool Cci.IMethodDefinition.RequiresSecurityObject
        {
            get
            {
                CheckDefinitionInvariant();

                return AdaptedMethodSymbol.RequiresSecurityObject;
            }
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IMethodDefinition.GetReturnValueAttributes(EmitContext context)
        {
            CheckDefinitionInvariant();

            ImmutableArray<CSharpAttributeData> userDefined = AdaptedMethodSymbol.GetReturnTypeAttributes();
            ArrayBuilder<SynthesizedAttributeData> synthesized = null;
            AdaptedMethodSymbol.AddSynthesizedReturnTypeAttributes((PEModuleBuilder)context.Module, ref synthesized);

            // Note that callers of this method (CCI and ReflectionEmitter) have to enumerate 
            // all items of the returned iterator, otherwise the synthesized ArrayBuilder may leak.
            return AdaptedMethodSymbol.GetCustomAttributesToEmit(userDefined, synthesized, isReturnType: true, emittingAssemblyAttributesInNetModule: false);
        }

        bool Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.ReturnValueIsMarshalledExplicitly;
            }
        }

        Cci.IMarshallingInformation Cci.IMethodDefinition.ReturnValueMarshallingInformation
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.ReturnValueMarshallingInformation;
            }
        }

        ImmutableArray<byte> Cci.IMethodDefinition.ReturnValueMarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedMethodSymbol.ReturnValueMarshallingDescriptor;
            }
        }

        Cci.INamespace Cci.IMethodDefinition.ContainingNamespace
        {
            get
            {
                return AdaptedMethodSymbol.ContainingNamespace.GetCciAdapter();
            }
        }
    }

    internal partial class MethodSymbol
    {
#if DEBUG
        private MethodSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new MethodSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, CreateCciAdapter());
            }

            return _lazyAdapter;
        }

        protected virtual MethodSymbolAdapter CreateCciAdapter()
        {
            return new MethodSymbolAdapter(this);
        }
#else
        internal MethodSymbol AdaptedMethodSymbol => this;

        internal new MethodSymbol GetCciAdapter()
        {
            return this;
        }
#endif

        internal virtual bool IsAccessCheckedOnOverride
        {
            get
            {
                CheckDefinitionInvariant();

                // Enforce C#'s notion of internal virtual
                // If the method is private or internal and virtual but not final
                // Set the new bit to indicate that it can only be overridden
                // by classes that can normally access this member.
                Accessibility accessibility = this.DeclaredAccessibility;
                return (accessibility == Accessibility.Private ||
                        accessibility == Accessibility.ProtectedAndInternal ||
                        accessibility == Accessibility.Internal)
                       && this.IsMetadataVirtual() && !this.IsMetadataFinal;
            }
        }

        internal virtual bool IsExternal
        {
            get
            {
                CheckDefinitionInvariant();

                // Delegate methods are implemented by the runtime.
                // Note that we don't force methods marked with MethodImplAttributes.InternalCall or MethodImplAttributes.Runtime
                // to be external, so it is possible to mark methods with bodies by these flags. It's up to the VM to interpret these flags
                // and throw runtime exception if they are applied incorrectly.
                return this.IsExtern || (object)ContainingType != null && ContainingType.TypeKind == TypeKind.Delegate;
            }
        }

        /// <summary>
        /// This method indicates whether or not the runtime will regard the method
        /// as newslot (as indicated by the presence of the "newslot" modifier in the
        /// signature).
        /// WARN WARN WARN: We won't have a final value for this until declaration
        /// diagnostics have been computed for all <see cref="SourceMemberContainerTypeSymbol"/>s, so pass
        /// ignoringInterfaceImplementationChanges: true if you need a value sooner
        /// and aren't concerned about tweaks made to satisfy interface implementation 
        /// requirements.
        /// NOTE: Not ignoring changes can only result in a value that is more true.
        /// </summary>
        internal abstract bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false);

        internal virtual bool HasRuntimeSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MethodKind == MethodKind.Constructor
                    || this.MethodKind == MethodKind.StaticConstructor;
            }
        }

        internal virtual bool IsMetadataFinal
        {
            get
            {
                // destructors should override this behavior
                Debug.Assert(this.MethodKind != MethodKind.Destructor);

                return this.IsSealed ||
                    (this.IsMetadataVirtual() &&
                     !(this.IsVirtual || this.IsOverride || this.IsAbstract || this.MethodKind == MethodKind.Destructor));
            }
        }

        /// <summary>
        /// This method indicates whether or not the runtime will regard the method
        /// as virtual (as indicated by the presence of the "virtual" modifier in the
        /// signature).
        /// WARN WARN WARN: We won't have a final value for this until declaration
        /// diagnostics have been computed for all <see cref="SourceMemberContainerTypeSymbol"/>s, so pass
        /// ignoringInterfaceImplementationChanges: true if you need a value sooner
        /// and aren't concerned about tweaks made to satisfy interface implementation 
        /// requirements.
        /// NOTE: Not ignoring changes can only result in a value that is more true.
        /// </summary>
        internal abstract bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false);

        internal virtual bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return this.ReturnValueMarshallingInformation != null;
            }
        }

        internal virtual ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return default(ImmutableArray<byte>);
            }
        }
    }

#if DEBUG
    internal partial class MethodSymbolAdapter
    {
        internal MethodSymbolAdapter(MethodSymbol underlyingMethodSymbol)
        {
            AdaptedMethodSymbol = underlyingMethodSymbol;

            if (underlyingMethodSymbol is NativeIntegerMethodSymbol)
            {
                // Emit should use underlying symbol only.
                throw ExceptionUtilities.Unreachable();
            }
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedMethodSymbol;
        internal MethodSymbol AdaptedMethodSymbol { get; }
    }
#endif
}
