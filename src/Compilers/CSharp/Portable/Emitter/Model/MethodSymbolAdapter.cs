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
#if DEBUG
        internal MethodSymbolAdapter(MethodSymbol underlyingMethodSymbol)
        {
            UnderlyingMethodSymbol = underlyingMethodSymbol;

            if (underlyingMethodSymbol is NativeIntegerMethodSymbol)
            {
                // Emit should use underlying symbol only.
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal sealed override Symbol AdaptedSymbol => UnderlyingMethodSymbol;
        internal MethodSymbol UnderlyingMethodSymbol { get; }
#else
        internal MethodSymbol UnderlyingMethodSymbol => this;
#endif 
    }

    internal partial class MethodSymbol
    {
#if DEBUG
        private MethodSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetAdapterImpl() => GetAdapter();
#endif
        internal new
#if DEBUG
            MethodSymbolAdapter
#else
            MethodSymbol
#endif
            GetAdapter()
        {
#if DEBUG
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new MethodSymbolAdapter(this));
            }

            return _lazyAdapter;
#else
            return this;
#endif
        }
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {

        Cci.IGenericMethodInstanceReference Cci.IMethodReference.AsGenericMethodInstanceReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (!UnderlyingMethodSymbol.IsDefinition &&
                    UnderlyingMethodSymbol.IsGenericMethod)
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

                if (!UnderlyingMethodSymbol.IsDefinition &&
                    (!UnderlyingMethodSymbol.IsGenericMethod || PEModuleBuilder.IsGenericType(UnderlyingMethodSymbol.ContainingType)))
                {
                    Debug.Assert((object)UnderlyingMethodSymbol.ContainingType != null &&
                            PEModuleBuilder.IsGenericType(UnderlyingMethodSymbol.ContainingType));
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

            var synthesizedGlobalMethod = UnderlyingMethodSymbol as SynthesizedGlobalMethodSymbol;
            if ((object)synthesizedGlobalMethod != null)
            {
                return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType;
            }

            NamedTypeSymbol containingType = UnderlyingMethodSymbol.ContainingType;
            var moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return moduleBeingBuilt.Translate(containingType,
                syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                diagnostics: context.Diagnostics,
                needDeclaration: UnderlyingMethodSymbol.IsDefinition);
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (!UnderlyingMethodSymbol.IsDefinition)
            {
                if (UnderlyingMethodSymbol.IsGenericMethod)
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
                if (UnderlyingMethodSymbol.ContainingModule == moduleBeingBuilt.SourceModule)
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
            get { return UnderlyingMethodSymbol.MetadataName; }
        }

        bool Cci.IMethodReference.AcceptsExtraArguments
        {
            get
            {
                return UnderlyingMethodSymbol.IsVararg;
            }
        }

        ushort Cci.IMethodReference.GenericParameterCount
        {
            get
            {
                return (ushort)UnderlyingMethodSymbol.Arity;
            }
        }

        bool Cci.IMethodReference.IsGeneric
        {
            get
            {
                return UnderlyingMethodSymbol.IsGenericMethod;
            }
        }

        ushort Cci.ISignature.ParameterCount
        {
            get
            {
                return (ushort)UnderlyingMethodSymbol.ParameterCount;
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

            if (UnderlyingMethodSymbol.IsDefinition && // can't be generic instantiation
                UnderlyingMethodSymbol.ContainingModule == moduleBeingBuilt.SourceModule) // must be declared in the module we are building
            {
                Debug.Assert((object)UnderlyingMethodSymbol.PartialDefinitionPart == null); // must be definition
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
                return UnderlyingMethodSymbol.CallingConvention;
            }
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            if (UnderlyingMethodSymbol.IsDefinition && UnderlyingMethodSymbol.ContainingModule == moduleBeingBuilt.SourceModule)
            {
                return StaticCast<Cci.IParameterTypeInformation>.From(this.EnumerateDefinitionParameters());
            }
            else
            {
                return moduleBeingBuilt.Translate(UnderlyingMethodSymbol.Parameters);
            }
        }

        private ImmutableArray<Cci.IParameterDefinition> EnumerateDefinitionParameters()
        {
            Debug.Assert(UnderlyingMethodSymbol.Parameters.All(p => p.IsDefinition));

#if DEBUG
            return UnderlyingMethodSymbol.Parameters.SelectAsArray<ParameterSymbol, Cci.IParameterDefinition>(p => p.GetAdapter());
#else
            return StaticCast<Cci.IParameterDefinition>.From(UnderlyingMethodSymbol.Parameters);
#endif
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(UnderlyingMethodSymbol.ReturnTypeWithAnnotations.CustomModifiers);
            }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.RefCustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(UnderlyingMethodSymbol.RefCustomModifiers);
            }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get
            {
                return UnderlyingMethodSymbol.RefKind.IsManagedReference();
            }
        }

        Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(UnderlyingMethodSymbol.ReturnType,
                syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                diagnostics: context.Diagnostics);
        }

        IEnumerable<Cci.ITypeReference> Cci.IGenericMethodInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.IMethodReference)this).AsGenericMethodInstanceReference != null);

            foreach (var arg in UnderlyingMethodSymbol.TypeArgumentsWithAnnotations)
            {
                Debug.Assert(arg.CustomModifiers.IsEmpty);
                yield return moduleBeingBuilt.Translate(arg.Type,
                                                        syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                        diagnostics: context.Diagnostics);
            }
        }

        Cci.IMethodReference Cci.IGenericMethodInstanceReference.GetGenericMethod(EmitContext context)
        {
            Debug.Assert(((Cci.IMethodReference)this).AsGenericMethodInstanceReference != null);

            NamedTypeSymbol container = UnderlyingMethodSymbol.ContainingType;

            if (!PEModuleBuilder.IsGenericType(container))
            {
                // NoPia method might come through here.
                return ((PEModuleBuilder)context.Module).Translate(
                    (MethodSymbol)UnderlyingMethodSymbol.OriginalDefinition,
                    syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                    diagnostics: context.Diagnostics,
                    needDeclaration: true);
            }

            MethodSymbol methodSymbol = UnderlyingMethodSymbol.ConstructedFrom;

            return new SpecializedMethodReference(methodSymbol);
        }

        Cci.IMethodReference Cci.ISpecializedMethodReference.UnspecializedVersion
        {
            get
            {
                Debug.Assert(((Cci.IMethodReference)this).AsSpecializedMethodReference != null);
                return ((MethodSymbol)UnderlyingMethodSymbol.OriginalDefinition).GetAdapter();
            }
        }

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();

                var synthesizedGlobalMethod = UnderlyingMethodSymbol as SynthesizedGlobalMethodSymbol;
                if ((object)synthesizedGlobalMethod != null)
                {
                    return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType;
                }

                return UnderlyingMethodSymbol.ContainingType.GetAdapter();
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return PEModuleBuilder.MemberVisibility(UnderlyingMethodSymbol);
            }
        }

        Cci.IMethodBody Cci.IMethodDefinition.GetBody(EmitContext context)
        {
            CheckDefinitionInvariant();
            return ((PEModuleBuilder)context.Module).GetMethodBody(UnderlyingMethodSymbol);
        }

        IEnumerable<Cci.IGenericMethodParameter> Cci.IMethodDefinition.GenericParameters
        {
            get
            {
                CheckDefinitionInvariant();

                foreach (var @param in UnderlyingMethodSymbol.TypeParameters)
                {
                    Debug.Assert(@param.IsDefinition);
                    yield return @param.GetAdapter();
                }
            }
        }

        bool Cci.IMethodDefinition.HasDeclarativeSecurity
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.HasDeclarativeSecurity;
            }
        }

        IEnumerable<Cci.SecurityAttribute> Cci.IMethodDefinition.SecurityAttributes
        {
            get
            {
                CheckDefinitionInvariant();
                Debug.Assert(UnderlyingMethodSymbol.HasDeclarativeSecurity);
                return UnderlyingMethodSymbol.GetSecurityInformation();
            }
        }

        bool Cci.IMethodDefinition.IsAbstract
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.IsAbstract;
            }
        }

        bool Cci.IMethodDefinition.IsAccessCheckedOnOverride
        {
            get
            {
                CheckDefinitionInvariant();

                return UnderlyingMethodSymbol.IsAccessCheckedOnOverride;
            }
        }
    }

    internal partial class MethodSymbol
    {
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
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {
        bool Cci.IMethodDefinition.IsConstructor
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.MethodKind == MethodKind.Constructor;
            }
        }

        bool Cci.IMethodDefinition.IsExternal
        {
            get
            {
                CheckDefinitionInvariant();

                return UnderlyingMethodSymbol.IsExternal;
            }
        }
    }

    internal partial class MethodSymbol
    {
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
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {
        bool Cci.IMethodDefinition.IsHiddenBySignature
        {
            get
            {
                CheckDefinitionInvariant();
                return !UnderlyingMethodSymbol.HidesBaseMethodsByName;
            }
        }

        bool Cci.IMethodDefinition.IsNewSlot
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.IsMetadataNewSlot();
            }
        }
    }

    internal partial class MethodSymbol
    {
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
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {
        bool Cci.IMethodDefinition.IsPlatformInvoke
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.GetDllImportData() != null;
            }
        }

        Cci.IPlatformInvokeInformation Cci.IMethodDefinition.PlatformInvokeData
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.GetDllImportData();
            }
        }

        System.Reflection.MethodImplAttributes Cci.IMethodDefinition.GetImplementationAttributes(EmitContext context)
        {
            CheckDefinitionInvariant();
            return UnderlyingMethodSymbol.ImplementationAttributes;
        }

        bool Cci.IMethodDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.HasRuntimeSpecialName;
            }
        }
    }

    internal partial class MethodSymbol
    {
        internal virtual bool HasRuntimeSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MethodKind == MethodKind.Constructor
                    || this.MethodKind == MethodKind.StaticConstructor;
            }
        }
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {
        bool Cci.IMethodDefinition.IsSealed
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.IsMetadataFinal;
            }
        }
    }

    internal partial class MethodSymbol
    {
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
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {
        bool Cci.IMethodDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.HasSpecialName;
            }
        }

        bool Cci.IMethodDefinition.IsStatic
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.IsStatic;
            }
        }

        bool Cci.IMethodDefinition.IsVirtual
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.IsMetadataVirtual();
            }
        }
    }

    internal partial class MethodSymbol
    {
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
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {
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

                return UnderlyingMethodSymbol.RequiresSecurityObject;
            }
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IMethodDefinition.GetReturnValueAttributes(EmitContext context)
        {
            CheckDefinitionInvariant();

            ImmutableArray<CSharpAttributeData> userDefined = UnderlyingMethodSymbol.GetReturnTypeAttributes();
            ArrayBuilder<SynthesizedAttributeData> synthesized = null;
            UnderlyingMethodSymbol.AddSynthesizedReturnTypeAttributes((PEModuleBuilder)context.Module, ref synthesized);

            // Note that callers of this method (CCI and ReflectionEmitter) have to enumerate 
            // all items of the returned iterator, otherwise the synthesized ArrayBuilder may leak.
            return UnderlyingMethodSymbol.GetCustomAttributesToEmit(userDefined, synthesized, isReturnType: true, emittingAssemblyAttributesInNetModule: false);
        }

        bool Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.ReturnValueIsMarshalledExplicitly;
            }
        }
    }

    internal partial class MethodSymbol
    {
        internal virtual bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return this.ReturnValueMarshallingInformation != null;
            }
        }
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {
        Cci.IMarshallingInformation Cci.IMethodDefinition.ReturnValueMarshallingInformation
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.ReturnValueMarshallingInformation;
            }
        }

        ImmutableArray<byte> Cci.IMethodDefinition.ReturnValueMarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return UnderlyingMethodSymbol.ReturnValueMarshallingDescriptor;
            }
        }
    }

    internal partial class MethodSymbol
    {
        internal virtual ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return default(ImmutableArray<byte>);
            }
        }
    }

    internal partial class
#if DEBUG
        MethodSymbolAdapter
#else
        MethodSymbol
#endif
    {
        Cci.INamespace Cci.IMethodDefinition.ContainingNamespace
        {
            get
            {
                return UnderlyingMethodSymbol.ContainingNamespace.GetAdapter();
            }
        }
    }
}
