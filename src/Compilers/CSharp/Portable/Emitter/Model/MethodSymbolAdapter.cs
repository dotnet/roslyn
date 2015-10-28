// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class MethodSymbol :
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

                if (!this.IsDefinition &&
                    this.IsGenericMethod)
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

                if (!this.IsDefinition &&
                    (!this.IsGenericMethod || PEModuleBuilder.IsGenericType(this.ContainingType)))
                {
                    Debug.Assert((object)this.ContainingType != null &&
                            PEModuleBuilder.IsGenericType(this.ContainingType));
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

            var synthesizedGlobalMethod = this as SynthesizedGlobalMethodSymbol;
            if ((object)synthesizedGlobalMethod != null)
            {
                return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType;
            }

            if (!this.IsDefinition)
            {
                PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
                return moduleBeingBuilt.Translate(this.ContainingType,
                                                  syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                  diagnostics: context.Diagnostics);
            }

            return this.ContainingType;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (!this.IsDefinition)
            {
                if (this.IsGenericMethod)
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
                if (this.ContainingModule == moduleBeingBuilt.SourceModule)
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
            get { return this.MetadataName; }
        }

        bool Cci.IMethodReference.AcceptsExtraArguments
        {
            get
            {
                return this.IsVararg;
            }
        }

        ushort Cci.IMethodReference.GenericParameterCount
        {
            get
            {
                return (ushort)this.Arity;
            }
        }

        bool Cci.IMethodReference.IsGeneric
        {
            get
            {
                return this.IsGenericMethod;
            }
        }

        ushort Cci.ISignature.ParameterCount
        {
            get
            {
                return (ushort)this.ParameterCount;
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

            if (this.IsDefinition && // can't be generic instantiation
                this.ContainingModule == moduleBeingBuilt.SourceModule) // must be declared in the module we are building
            {
                Debug.Assert((object)this.PartialDefinitionPart == null); // must be definition
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
                return this.CallingConvention;
            }
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            if (this.IsDefinition && this.ContainingModule == moduleBeingBuilt.SourceModule)
            {
                return StaticCast<Cci.IParameterTypeInformation>.From(this.EnumerateDefinitionParameters());
            }
            else
            {
                return moduleBeingBuilt.Translate(this.Parameters);
            }
        }

        private ImmutableArray<Cci.IParameterDefinition> EnumerateDefinitionParameters()
        {
            Debug.Assert(this.Parameters.All(p => p.IsDefinition));

            return StaticCast<Cci.IParameterDefinition>.From(this.Parameters);
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
        {
            get
            {
                return this.ReturnType.CustomModifiers.As<Cci.ICustomModifier>();
            }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get
            {
                return this.ReturnType.TypeSymbol is ByRefReturnErrorTypeSymbol;
            }
        }

        Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
        {
            ByRefReturnErrorTypeSymbol byRefType = this.ReturnType.TypeSymbol as ByRefReturnErrorTypeSymbol;
            return ((PEModuleBuilder)context.Module).Translate(
                (object)byRefType == null ? this.ReturnType.TypeSymbol : byRefType.ReferencedType,
                syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                diagnostics: context.Diagnostics);
        }

        IEnumerable<Cci.ITypeReference> Cci.IGenericMethodInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.IMethodReference)this).AsGenericMethodInstanceReference != null);

            foreach (var arg in this.TypeArguments)
            {
                Debug.Assert(arg.CustomModifiers.IsEmpty);
                yield return moduleBeingBuilt.Translate(arg.TypeSymbol,
                                                        syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                        diagnostics: context.Diagnostics);
            }
        }

        Cci.IMethodReference Cci.IGenericMethodInstanceReference.GetGenericMethod(EmitContext context)
        {
            Debug.Assert(((Cci.IMethodReference)this).AsGenericMethodInstanceReference != null);

            NamedTypeSymbol container = this.ContainingType;

            if (!PEModuleBuilder.IsGenericType(container))
            {
                // NoPia method might come through here.
                return ((PEModuleBuilder)context.Module).Translate(
                    (MethodSymbol)this.OriginalDefinition,
                    syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                    diagnostics: context.Diagnostics,
                    needDeclaration: true);
            }

            MethodSymbol methodSymbol = this.ConstructedFrom;

            return new SpecializedMethodReference(methodSymbol);
        }

        Cci.IMethodReference Cci.ISpecializedMethodReference.UnspecializedVersion
        {
            get
            {
                Debug.Assert(((Cci.IMethodReference)this).AsSpecializedMethodReference != null);
                return (MethodSymbol)this.OriginalDefinition;
            }
        }

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();

                var synthesizedGlobalMethod = this as SynthesizedGlobalMethodSymbol;
                if ((object)synthesizedGlobalMethod != null)
                {
                    return synthesizedGlobalMethod.ContainingPrivateImplementationDetailsType;
                }

                return this.ContainingType;
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return PEModuleBuilder.MemberVisibility(this);
            }
        }

        Cci.IMethodBody Cci.IMethodDefinition.GetBody(EmitContext context)
        {
            CheckDefinitionInvariant();
            return ((PEModuleBuilder)context.Module).GetMethodBody(this);
        }

        IEnumerable<Cci.IGenericMethodParameter> Cci.IMethodDefinition.GenericParameters
        {
            get
            {
                CheckDefinitionInvariant();

                foreach (var @param in this.TypeParameters)
                {
                    Debug.Assert(@param.IsDefinition);
                    yield return @param;
                }
            }
        }

        bool Cci.IMethodDefinition.HasDeclarativeSecurity
        {
            get
            {
                CheckDefinitionInvariant();
                return this.HasDeclarativeSecurity;
            }
        }

        IEnumerable<Cci.SecurityAttribute> Cci.IMethodDefinition.SecurityAttributes
        {
            get
            {
                CheckDefinitionInvariant();
                Debug.Assert(this.HasDeclarativeSecurity);
                return this.GetSecurityInformation();
            }
        }

        bool Cci.IMethodDefinition.IsAbstract
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsAbstract;
            }
        }

        bool Cci.IMethodDefinition.IsAccessCheckedOnOverride
        {
            get
            {
                CheckDefinitionInvariant();

                return this.IsAccessCheckedOnOverride;
            }
        }

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
                       && ((Cci.IMethodDefinition)this).IsVirtual && !((Cci.IMethodDefinition)this).IsSealed;
            }
        }

        bool Cci.IMethodDefinition.IsConstructor
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MethodKind == MethodKind.Constructor;
            }
        }

        bool Cci.IMethodDefinition.IsExternal
        {
            get
            {
                CheckDefinitionInvariant();

                return this.IsExternal;
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

        bool Cci.IMethodDefinition.IsHiddenBySignature
        {
            get
            {
                CheckDefinitionInvariant();
                return !this.HidesBaseMethodsByName;
            }
        }

        bool Cci.IMethodDefinition.IsNewSlot
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsMetadataNewSlot();
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

        bool Cci.IMethodDefinition.IsPlatformInvoke
        {
            get
            {
                CheckDefinitionInvariant();
                return this.GetDllImportData() != null;
            }
        }

        Cci.IPlatformInvokeInformation Cci.IMethodDefinition.PlatformInvokeData
        {
            get
            {
                CheckDefinitionInvariant();
                return this.GetDllImportData();
            }
        }

        System.Reflection.MethodImplAttributes Cci.IMethodDefinition.GetImplementationAttributes(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this.ImplementationAttributes;
        }

        bool Cci.IMethodDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return this.HasRuntimeSpecialName;
            }
        }

        internal virtual bool HasRuntimeSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MethodKind == MethodKind.Constructor
                    || this.MethodKind == MethodKind.StaticConstructor;
            }
        }

        bool Cci.IMethodDefinition.IsSealed
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsMetadataFinal;
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

        bool Cci.IMethodDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return this.HasSpecialName;
            }
        }

        bool Cci.IMethodDefinition.IsStatic
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsStatic;
            }
        }

        bool Cci.IMethodDefinition.IsVirtual
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsMetadataVirtual();
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

                return this.RequiresSecurityObject;
            }
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IMethodDefinition.ReturnValueAttributes
        {
            get
            {
                return GetReturnValueCustomAttributesToEmit();
            }
        }

        private IEnumerable<CSharpAttributeData> GetReturnValueCustomAttributesToEmit()
        {
            CheckDefinitionInvariant();

            ImmutableArray<CSharpAttributeData> userDefined;
            ArrayBuilder<SynthesizedAttributeData> synthesized = null;

            userDefined = this.GetReturnTypeAttributes();
            this.AddSynthesizedReturnTypeAttributes(ref synthesized);

            if (userDefined.IsEmpty && synthesized == null)
            {
                return SpecializedCollections.EmptyEnumerable<CSharpAttributeData>();
            }

            // Note that callers of this method (CCI and ReflectionEmitter) have to enumerate 
            // all items of the returned iterator, otherwise the synthesized ArrayBuilder may leak.
            return GetCustomAttributesToEmit(userDefined, synthesized, isReturnType: true, emittingAssemblyAttributesInNetModule: false);
        }

        bool Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return this.ReturnValueIsMarshalledExplicitly;
            }
        }

        internal virtual bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return this.ReturnValueMarshallingInformation != null;
            }
        }

        Cci.IMarshallingInformation Cci.IMethodDefinition.ReturnValueMarshallingInformation
        {
            get
            {
                CheckDefinitionInvariant();
                return this.ReturnValueMarshallingInformation;
            }
        }

        ImmutableArray<byte> Cci.IMethodDefinition.ReturnValueMarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return this.ReturnValueMarshallingDescriptor;
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

        Cci.INamespace Cci.IMethodDefinition.ContainingNamespace
        {
            get
            {
                return ContainingNamespace;
            }
        }
    }
}
