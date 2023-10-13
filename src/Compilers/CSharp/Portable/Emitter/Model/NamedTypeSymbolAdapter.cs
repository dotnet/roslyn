// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        NamedTypeSymbolAdapter : SymbolAdapter,
#else
        NamedTypeSymbol :
#endif 
        Cci.ITypeReference,
        Cci.ITypeDefinition,
        Cci.INamedTypeReference,
        Cci.INamedTypeDefinition,
        Cci.INamespaceTypeReference,
        Cci.INamespaceTypeDefinition,
        Cci.INestedTypeReference,
        Cci.INestedTypeDefinition,
        Cci.IGenericTypeInstanceReference,
        Cci.ISpecializedNestedTypeReference
    {
        bool Cci.ITypeReference.IsEnum
        {
            get { return AdaptedNamedTypeSymbol.TypeKind == TypeKind.Enum; }
        }

        bool Cci.ITypeReference.IsValueType
        {
            get { return AdaptedNamedTypeSymbol.IsValueType; }
        }

        Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return AsTypeDefinitionImpl(moduleBeingBuilt);
        }

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (AdaptedNamedTypeSymbol.IsDefinition)
                {
                    return AdaptedNamedTypeSymbol.PrimitiveTypeCode;
                }

                return Cci.PrimitiveTypeCode.NotPrimitive;
            }
        }

        TypeDefinitionHandle Cci.ITypeReference.TypeDef
        {
            get
            {
                PENamedTypeSymbol peNamedType = AdaptedNamedTypeSymbol as PENamedTypeSymbol;
                if ((object)peNamedType != null)
                {
                    return peNamedType.Handle;
                }

                return default(TypeDefinitionHandle);
            }
        }

        Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get { return null; }
        }

        Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (!AdaptedNamedTypeSymbol.IsDefinition &&
                    AdaptedNamedTypeSymbol.Arity > 0)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get { return null; }
        }

        Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (AdaptedNamedTypeSymbol.IsDefinition &&
                    (object)AdaptedNamedTypeSymbol.ContainingType == null)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(this.IsDefinitionOrDistinct());

            if ((object)AdaptedNamedTypeSymbol.ContainingType == null &&
                AdaptedNamedTypeSymbol.IsDefinition &&
                AdaptedNamedTypeSymbol.ContainingModule == moduleBeingBuilt.SourceModule)
            {
                return this;
            }

            return null;
        }

        Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference
        {
            get
            {
                if ((object)AdaptedNamedTypeSymbol.ContainingType != null)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return AsNestedTypeDefinitionImpl(moduleBeingBuilt);
        }

        private Cci.INestedTypeDefinition AsNestedTypeDefinitionImpl(PEModuleBuilder moduleBeingBuilt)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if ((object)AdaptedNamedTypeSymbol.ContainingType != null &&
                AdaptedNamedTypeSymbol.IsDefinition &&
                AdaptedNamedTypeSymbol.ContainingModule == moduleBeingBuilt.SourceModule)
            {
                return this;
            }

            return null;
        }

        Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (!AdaptedNamedTypeSymbol.IsDefinition &&
                    (AdaptedNamedTypeSymbol.Arity == 0 || PEModuleBuilder.IsGenericType(AdaptedNamedTypeSymbol.ContainingType)))
                {
                    Debug.Assert((object)AdaptedNamedTypeSymbol.ContainingType != null &&
                            PEModuleBuilder.IsGenericType(AdaptedNamedTypeSymbol.ContainingType));
                    return this;
                }

                return null;
            }
        }

        Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return AsTypeDefinitionImpl(moduleBeingBuilt);
        }

        private Cci.ITypeDefinition AsTypeDefinitionImpl(PEModuleBuilder moduleBeingBuilt)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (AdaptedNamedTypeSymbol.IsDefinition && // can't be generic instantiation
                AdaptedNamedTypeSymbol.ContainingModule == moduleBeingBuilt.SourceModule) // must be declared in the module we are building
            {
                return this;
            }

            return null;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable();
            //We've not yet discovered a scenario in which we need this.
            //If you're hitting this exception. Uncomment the code below
            //and add a unit test.
#if false
            Module moduleBeingBuilt = (Module)visitor.Context;

            Debug.Assert(this.IsDefinitionOrDistinct());

            if (!this.IsDefinition)
            {
                if (this.Arity > 0)
                {
                    Debug.Assert(((ITypeReference)this).AsGenericTypeInstanceReference != null);
                    visitor.Visit((IGenericTypeInstanceReference)this);
                }
                else
                {
                    Debug.Assert(((ITypeReference)this).AsSpecializedNestedTypeReference != null);
                    visitor.Visit((ISpecializedNestedTypeReference)this);
                }
            }
            else
            {
                bool asDefinition = (this.ContainingModule == moduleBeingBuilt.SourceModule);

                if (this.ContainingType == null)
                {
                    if (asDefinition)
                    {
                        Debug.Assert(((ITypeReference)this).AsNamespaceTypeDefinition(moduleBeingBuilt) != null);
                        visitor.Visit((INamespaceTypeDefinition)this);
                    }
                    else
                    {
                        Debug.Assert(((ITypeReference)this).AsNamespaceTypeReference != null);
                        visitor.Visit((INamespaceTypeReference)this);
                    }
                }
                else
                {
                    if (asDefinition)
                    {
                        Debug.Assert(((ITypeReference)this).AsNestedTypeDefinition(moduleBeingBuilt) != null);
                        visitor.Visit((INestedTypeDefinition)this);
                    }
                    else
                    {
                        Debug.Assert(((ITypeReference)this).AsNestedTypeReference != null);
                        visitor.Visit((INestedTypeReference)this);
                    }
                }
            }
#endif
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return AsTypeDefinitionImpl(moduleBeingBuilt);
        }

        Cci.ITypeReference Cci.ITypeDefinition.GetBaseClass(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.ITypeReference)this).AsTypeDefinition(context) != null);
            NamedTypeSymbol baseType = AdaptedNamedTypeSymbol.BaseTypeNoUseSiteDiagnostics;

            if (AdaptedNamedTypeSymbol.IsScriptClass)
            {
                // although submission and scripts semantically doesn't have a base we need to emit one into metadata:
                Debug.Assert((object)baseType == null);
                baseType = AdaptedNamedTypeSymbol.ContainingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object);
            }

            return ((object)baseType != null) ? moduleBeingBuilt.Translate(baseType,
                                                                   syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                                   diagnostics: context.Diagnostics) : null;
        }

        IEnumerable<Cci.IEventDefinition> Cci.ITypeDefinition.GetEvents(EmitContext context)
        {
            CheckDefinitionInvariant();
            foreach (EventSymbol e in AdaptedNamedTypeSymbol.GetEventsToEmit())
            {
                IEventDefinition definition = e.GetCciAdapter();

                // If any accessor should be included, then the event should be included too
                if (definition.ShouldInclude(context) || !definition.GetAccessors(context).IsEmpty())
                {
                    yield return definition;
                }
            }
        }

        IEnumerable<Cci.MethodImplementation> Cci.ITypeDefinition.GetExplicitImplementationOverrides(EmitContext context)
        {
            CheckDefinitionInvariant();

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (var member in AdaptedNamedTypeSymbol.GetMembers())
            {
                if (member.Kind == SymbolKind.Method)
                {
                    var method = (MethodSymbol)member;
                    Debug.Assert((object)method.PartialDefinitionPart == null); // must be definition

                    var explicitImplementations = method.ExplicitInterfaceImplementations;
                    if (explicitImplementations.Length != 0)
                    {
                        var adapter = method.GetCciAdapter();

                        foreach (var implemented in method.ExplicitInterfaceImplementations)
                        {
                            yield return new Microsoft.Cci.MethodImplementation(adapter, moduleBeingBuilt.TranslateOverriddenMethodReference(implemented, (CSharpSyntaxNode)context.SyntaxNode, context.Diagnostics));
                        }
                    }

                    if (AdaptedNamedTypeSymbol.IsInterface)
                    {
                        continue;
                    }

                    if (method.RequiresExplicitOverride(out _))
                    {
                        // If C# and the runtime don't agree on the overridden method, then 
                        // we will mark the method as newslot (see MethodSymbolAdapter) and
                        // specify the override explicitly.
                        // This affects accessors - C# ignores method interactions
                        // between accessors and non-accessors, whereas the runtime does not.
                        // It also affects covariant returns - C# ignores the return type in
                        // determining if one method overrides another, while the runtime considers
                        // the return type part of the signature.
                        yield return new Microsoft.Cci.MethodImplementation(method.GetCciAdapter(), moduleBeingBuilt.TranslateOverriddenMethodReference(method.OverriddenMethod, (CSharpSyntaxNode)context.SyntaxNode, context.Diagnostics));
                    }
                    else if (method.MethodKind == MethodKind.Destructor && AdaptedNamedTypeSymbol.SpecialType != SpecialType.System_Object)
                    {
                        // New in Roslyn: all destructors explicitly override (or are) System.Object.Finalize so that
                        // they are guaranteed to be runtime finalizers.  As a result, it is no longer possible to create
                        // a destructor that will never be invoked by the runtime.
                        // NOTE: If System.Object doesn't contain a destructor, you're on your own - this destructor may
                        // or not be called by the runtime.
                        TypeSymbol objectType = AdaptedNamedTypeSymbol.DeclaringCompilation.GetSpecialType(CodeAnalysis.SpecialType.System_Object);
                        foreach (Symbol objectMember in objectType.GetMembers(WellKnownMemberNames.DestructorName))
                        {
                            MethodSymbol objectMethod = objectMember as MethodSymbol;
                            if ((object)objectMethod != null && objectMethod.MethodKind == MethodKind.Destructor)
                            {
                                yield return new Microsoft.Cci.MethodImplementation(method.GetCciAdapter(), moduleBeingBuilt.TranslateOverriddenMethodReference(objectMethod, (CSharpSyntaxNode)context.SyntaxNode, context.Diagnostics));
                            }
                        }
                    }
                }
            }

            if (AdaptedNamedTypeSymbol.IsInterface)
            {
                yield break;
            }

            if (AdaptedNamedTypeSymbol is SourceMemberContainerTypeSymbol container)
            {
                foreach ((MethodSymbol body, MethodSymbol implemented) in container.GetSynthesizedExplicitImplementations(cancellationToken: default).MethodImpls)
                {
                    Debug.Assert(body.ContainingType == (object)container);
                    yield return new Microsoft.Cci.MethodImplementation(body.GetCciAdapter(), moduleBeingBuilt.TranslateOverriddenMethodReference(implemented, (CSharpSyntaxNode)context.SyntaxNode, context.Diagnostics));
                }
            }

            var syntheticMethods = moduleBeingBuilt.GetSynthesizedMethods(AdaptedNamedTypeSymbol);
            if (syntheticMethods != null)
            {
                foreach (var m in syntheticMethods)
                {
                    var method = m.GetInternalSymbol() as MethodSymbol;
                    if ((object)method != null)
                    {
                        Debug.Assert((object)method.PartialDefinitionPart == null); // must be definition

                        foreach (var implemented in method.ExplicitInterfaceImplementations)
                        {
                            yield return new Microsoft.Cci.MethodImplementation(m, moduleBeingBuilt.TranslateOverriddenMethodReference(implemented, (CSharpSyntaxNode)context.SyntaxNode, context.Diagnostics));
                        }

                        Debug.Assert(!method.RequiresExplicitOverride(out _));
                    }
                }
            }
        }

        IEnumerable<Cci.IFieldDefinition> Cci.ITypeDefinition.GetFields(EmitContext context)
        {
            CheckDefinitionInvariant();

            // All fields in a struct should be emitted
            bool isStruct = AdaptedNamedTypeSymbol.IsStructType();

            foreach (var f in AdaptedNamedTypeSymbol.GetFieldsToEmit())
            {
                Debug.Assert((object)(f.TupleUnderlyingField ?? f) == f);
                Debug.Assert(!(f is TupleErrorFieldSymbol));
                if (isStruct || f.GetCciAdapter().ShouldInclude(context))
                {
                    yield return f.GetCciAdapter();
                }
            }

            IEnumerable<Cci.IFieldDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedFields(AdaptedNamedTypeSymbol);

            if (generated != null)
            {
                foreach (var f in generated)
                {
                    if (isStruct || f.ShouldInclude(context))
                    {
                        yield return f;
                    }
                }
            }
        }

        IEnumerable<Cci.IGenericTypeParameter> Cci.ITypeDefinition.GenericParameters
        {
            get
            {
                CheckDefinitionInvariant();

                foreach (var t in AdaptedNamedTypeSymbol.TypeParameters)
                {
                    yield return t.GetCciAdapter();
                }
            }
        }

        ushort Cci.ITypeDefinition.GenericParameterCount
        {
            get
            {
                CheckDefinitionInvariant();

                return GenericParameterCountImpl;
            }
        }

        private ushort GenericParameterCountImpl
        {
            get { return (ushort)AdaptedNamedTypeSymbol.Arity; }
        }

        IEnumerable<Cci.TypeReferenceWithAttributes> Cci.ITypeDefinition.Interfaces(EmitContext context)
        {
            Debug.Assert(((Cci.ITypeReference)this).AsTypeDefinition(context) != null);

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (NamedTypeSymbol @interface in AdaptedNamedTypeSymbol.GetInterfacesToEmit())
            {
                var typeRef = moduleBeingBuilt.Translate(
                    @interface,
                    syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                    diagnostics: context.Diagnostics,
                    fromImplements: true);

                var type = TypeWithAnnotations.Create(@interface);
                yield return type.GetTypeRefWithAttributes(
                    moduleBeingBuilt,
                    declaringSymbol: AdaptedNamedTypeSymbol,
                    typeRef);
            }
        }

        bool Cci.ITypeDefinition.IsAbstract
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.IsMetadataAbstract;
            }
        }

        bool Cci.ITypeDefinition.IsBeforeFieldInit
        {
            get
            {
                CheckDefinitionInvariant();

                switch (AdaptedNamedTypeSymbol.TypeKind)
                {
                    case TypeKind.Enum:
                    case TypeKind.Delegate:
                        return false;
                }

                //apply the beforefieldinit attribute unless there is an explicitly specified static constructor
                foreach (var member in AdaptedNamedTypeSymbol.GetMembers(WellKnownMemberNames.StaticConstructorName))
                {
                    if (!member.IsImplicitlyDeclared)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        bool Cci.ITypeDefinition.IsComObject
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.IsComImport;
            }
        }

        bool Cci.ITypeDefinition.IsGeneric
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.Arity != 0;
            }
        }

        bool Cci.ITypeDefinition.IsInterface
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.IsInterface;
            }
        }

        bool Cci.ITypeDefinition.IsDelegate
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.IsDelegateType();
            }
        }

        bool Cci.ITypeDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }

        bool Cci.ITypeDefinition.IsSerializable
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.IsSerializable;
            }
        }

        bool Cci.ITypeDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.HasSpecialName;
            }
        }

        bool Cci.ITypeDefinition.IsWindowsRuntimeImport
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.IsWindowsRuntimeImport;
            }
        }

        bool Cci.ITypeDefinition.IsSealed
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.IsMetadataSealed;
            }
        }

        IEnumerable<Cci.IMethodDefinition> Cci.ITypeDefinition.GetMethods(EmitContext context)
        {
            CheckDefinitionInvariant();

            // All constructors in attributes should be emitted.
            // Don't compute IsAttributeType if IncludePrivateMembers is true, as we'll include it anyway.
            bool alwaysIncludeConstructors = context.IncludePrivateMembers || AdaptedNamedTypeSymbol.DeclaringCompilation.IsAttributeType(AdaptedNamedTypeSymbol);

            foreach (var method in AdaptedNamedTypeSymbol.GetMethodsToEmit())
            {
                Debug.Assert((object)method != null);

                if ((alwaysIncludeConstructors && method.MethodKind == MethodKind.Constructor) || method.GetCciAdapter().ShouldInclude(context))
                {
                    yield return method.GetCciAdapter();
                }
            }

            IEnumerable<Cci.IMethodDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedMethods(AdaptedNamedTypeSymbol);

            if (generated != null)
            {
                foreach (var m in generated)
                {
                    if ((alwaysIncludeConstructors && m.IsConstructor) || m.ShouldInclude(context))
                    {
                        yield return m;
                    }
                }
            }
        }

        IEnumerable<Cci.INestedTypeDefinition> Cci.ITypeDefinition.GetNestedTypes(EmitContext context)
        {
            CheckDefinitionInvariant();

            foreach (NamedTypeSymbol type in AdaptedNamedTypeSymbol.GetTypeMembers()) // Ordered.
            {
                yield return type.GetCciAdapter();
            }

            IEnumerable<Cci.INestedTypeDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedTypes(AdaptedNamedTypeSymbol);

            if (generated != null)
            {
                foreach (var t in generated)
                {
                    yield return t;
                }
            }
        }

        IEnumerable<Cci.IPropertyDefinition> Cci.ITypeDefinition.GetProperties(EmitContext context)
        {
            CheckDefinitionInvariant();

            foreach (PropertySymbol property in AdaptedNamedTypeSymbol.GetPropertiesToEmit())
            {
                Debug.Assert((object)property != null);
                IPropertyDefinition definition = property.GetCciAdapter();
                // If any accessor should be included, then the property should be included too
                if (definition.ShouldInclude(context) || !definition.GetAccessors(context).IsEmpty())
                {
                    yield return definition;
                }
            }

            IEnumerable<Cci.IPropertyDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedProperties(AdaptedNamedTypeSymbol);

            if (generated != null)
            {
                foreach (IPropertyDefinition m in generated)
                {
                    if (m.ShouldInclude(context) || !m.GetAccessors(context).IsEmpty())
                    {
                        yield return m;
                    }
                }
            }
        }

        bool Cci.ITypeDefinition.HasDeclarativeSecurity
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.HasDeclarativeSecurity;
            }
        }

        IEnumerable<Cci.SecurityAttribute> Cci.ITypeDefinition.SecurityAttributes
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.GetSecurityInformation() ?? SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
            }
        }

        ushort Cci.ITypeDefinition.Alignment
        {
            get
            {
                CheckDefinitionInvariant();
                var layout = AdaptedNamedTypeSymbol.Layout;
                return (ushort)layout.Alignment;
            }
        }

        LayoutKind Cci.ITypeDefinition.Layout
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.Layout.Kind;
            }
        }

        uint Cci.ITypeDefinition.SizeOf
        {
            get
            {
                CheckDefinitionInvariant();
                return (uint)AdaptedNamedTypeSymbol.Layout.Size;
            }
        }

        CharSet Cci.ITypeDefinition.StringFormat
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedNamedTypeSymbol.MarshallingCharSet;
            }
        }

        ushort Cci.INamedTypeReference.GenericParameterCount
        {
            get { return GenericParameterCountImpl; }
        }

        bool Cci.INamedTypeReference.MangleName
        {
            get
            {
                return AdaptedNamedTypeSymbol.MangleName;
            }
        }

#nullable enable
        string? Cci.INamedTypeReference.AssociatedFileIdentifier
        {
            get
            {
                return AdaptedNamedTypeSymbol.GetFileLocalTypeMetadataNamePrefix();
            }
        }
#nullable disable

        string Cci.INamedEntity.Name
        {
            get
            {
                string unsuffixedName = AdaptedNamedTypeSymbol.Name;

                // CLR generally allows names with dots, however some APIs like IMetaDataImport
                // can only return full type names combined with namespaces. 
                // see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
                // When working with such APIs, names with dots become ambiguous since metadata 
                // consumer cannot figure where namespace ends and actual type name starts.
                // Therefore it is a good practice to avoid type names with dots.
                // Exception: The EE copies type names from metadata, which may contain dots already.
                Debug.Assert(AdaptedNamedTypeSymbol.IsErrorType() ||
                    !unsuffixedName.Contains(".") ||
                    AdaptedNamedTypeSymbol.OriginalDefinition is PENamedTypeSymbol, "type name contains dots: " + unsuffixedName);

                return unsuffixedName;
            }
        }

        Cci.IUnitReference Cci.INamespaceTypeReference.GetUnit(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.ITypeReference)this).AsNamespaceTypeReference != null);
            return moduleBeingBuilt.Translate(AdaptedNamedTypeSymbol.ContainingModule, context.Diagnostics);
        }

        string Cci.INamespaceTypeReference.NamespaceName
        {
            get
            {
                // INamespaceTypeReference is a type contained in a namespace
                // if this method is called for a nested type, we are in big trouble.
                Debug.Assert(((Cci.ITypeReference)this).AsNamespaceTypeReference != null);

                return AdaptedNamedTypeSymbol.ContainingNamespace.QualifiedName;
            }
        }

        bool Cci.INamespaceTypeDefinition.IsPublic
        {
            get
            {
                Debug.Assert((object)AdaptedNamedTypeSymbol.ContainingType == null && AdaptedNamedTypeSymbol.ContainingModule is SourceModuleSymbol);

                return PEModuleBuilder.MemberVisibility(AdaptedNamedTypeSymbol) == Cci.TypeMemberVisibility.Public;
            }
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.ITypeReference)this).AsNestedTypeReference != null);

            Debug.Assert(this.IsDefinitionOrDistinct());

            return moduleBeingBuilt.Translate(AdaptedNamedTypeSymbol.ContainingType,
                                              syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                              diagnostics: context.Diagnostics,
                                              needDeclaration: AdaptedNamedTypeSymbol.IsDefinition);
        }

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                Debug.Assert((object)AdaptedNamedTypeSymbol.ContainingType != null);
                CheckDefinitionInvariant();

                return AdaptedNamedTypeSymbol.ContainingType.GetCciAdapter();
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                Debug.Assert((object)AdaptedNamedTypeSymbol.ContainingType != null);
                CheckDefinitionInvariant();

                return PEModuleBuilder.MemberVisibility(AdaptedNamedTypeSymbol);
            }
        }

        ImmutableArray<Cci.ITypeReference> Cci.IGenericTypeInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var builder = ArrayBuilder<Microsoft.Cci.ITypeReference>.GetInstance();
            Debug.Assert(((Cci.ITypeReference)this).AsGenericTypeInstanceReference != null);

            var arguments = AdaptedNamedTypeSymbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

            for (int i = 0; i < arguments.Length; i++)
            {
                var arg = moduleBeingBuilt.Translate(arguments[i].Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode, diagnostics: context.Diagnostics);
                var modifiers = arguments[i].CustomModifiers;
                if (!modifiers.IsDefaultOrEmpty)
                {
                    arg = new Cci.ModifiedTypeReference(arg, ImmutableArray<Cci.ICustomModifier>.CastUp(modifiers));
                }

                builder.Add(arg);
            }

            return builder.ToImmutableAndFree();
        }

        Cci.INamedTypeReference Cci.IGenericTypeInstanceReference.GetGenericType(EmitContext context)
        {
            Debug.Assert(((Cci.ITypeReference)this).AsGenericTypeInstanceReference != null);
            return GenericTypeImpl(context);
        }

        private Cci.INamedTypeReference GenericTypeImpl(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.Translate(AdaptedNamedTypeSymbol.OriginalDefinition, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                              diagnostics: context.Diagnostics, needDeclaration: true);
        }

        Cci.INestedTypeReference Cci.ISpecializedNestedTypeReference.GetUnspecializedVersion(EmitContext context)
        {
            Debug.Assert(((Cci.ITypeReference)this).AsSpecializedNestedTypeReference != null);
            var result = GenericTypeImpl(context).AsNestedTypeReference;

            Debug.Assert(result != null);
            return result;
        }
    }

    internal partial class NamedTypeSymbol
    {
#if DEBUG
        private NamedTypeSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new NamedTypeSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new NamedTypeSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal NamedTypeSymbol AdaptedNamedTypeSymbol => this;

        internal new NamedTypeSymbol GetCciAdapter()
        {
            return this;
        }
#endif

        internal virtual IEnumerable<EventSymbol> GetEventsToEmit()
        {
            CheckDefinitionInvariant();

            foreach (var m in this.GetMembers())
            {
                if (m.Kind == SymbolKind.Event)
                {
                    yield return (EventSymbol)m;
                }
            }
        }

        internal abstract IEnumerable<FieldSymbol> GetFieldsToEmit();

        /// <summary>
        /// Gets the set of interfaces to emit on this type. This set can be different from the set returned by Interfaces property.
        /// </summary>
        internal abstract ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit();

        protected ImmutableArray<NamedTypeSymbol> CalculateInterfacesToEmit()
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(this.ContainingModule is SourceModuleSymbol);

            ArrayBuilder<NamedTypeSymbol> builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            HashSet<NamedTypeSymbol> seen = null;
            InterfacesVisit(this, builder, ref seen);
            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Add the type to the builder and then recurse on its interfaces.
        /// </summary>
        /// <remarks>
        /// Pre-order depth-first search.
        /// </remarks>
        private static void InterfacesVisit(NamedTypeSymbol namedType, ArrayBuilder<NamedTypeSymbol> builder, ref HashSet<NamedTypeSymbol> seen)
        {
            // It's not clear how important the order of these interfaces is, but Dev10
            // maintains pre-order depth-first/declaration order, so we probably should as well.
            // That's why we're not using InterfacesAndTheirBaseInterfaces - it's an unordered set.
            foreach (NamedTypeSymbol @interface in namedType.InterfacesNoUseSiteDiagnostics())
            {
                if (seen == null)
                {
                    // Don't allocate until we see at least one interface.
                    seen = new HashSet<NamedTypeSymbol>(Symbols.SymbolEqualityComparer.CLRSignature);
                }
                if (seen.Add(@interface))
                {
                    builder.Add(@interface);
                    InterfacesVisit(@interface, builder, ref seen);
                }
            }
        }

        internal virtual bool IsMetadataAbstract
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsAbstract || this.IsStatic;
            }
        }

        internal virtual bool IsMetadataSealed
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsSealed || this.IsStatic;
            }
        }

        /// <summary>
        /// To represent a gap in interface's v-table null value should be returned in the appropriate position,
        /// unless the gap has a symbol (happens if it is declared in source, for example).
        /// </summary>
        internal virtual IEnumerable<MethodSymbol> GetMethodsToEmit()
        {
            CheckDefinitionInvariant();

            foreach (var m in this.GetMembers())
            {
                if (m.Kind == SymbolKind.Method)
                {
                    var method = (MethodSymbol)m;
                    if (method.ShouldEmit())
                    {
                        yield return method;
                    }
                }
            }
        }

        internal virtual IEnumerable<PropertySymbol> GetPropertiesToEmit()
        {
            CheckDefinitionInvariant();

            foreach (var m in this.GetMembers())
            {
                if (m.Kind == SymbolKind.Property)
                {
                    yield return (PropertySymbol)m;
                }
            }
        }
    }

#if DEBUG
    internal partial class NamedTypeSymbolAdapter
    {
        internal NamedTypeSymbolAdapter(NamedTypeSymbol underlyingNamedTypeSymbol)
        {
            AdaptedNamedTypeSymbol = underlyingNamedTypeSymbol;

            if (underlyingNamedTypeSymbol is NativeIntegerTypeSymbol)
            {
                // Emit should use underlying symbol only.
                throw ExceptionUtilities.Unreachable();
            }
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedNamedTypeSymbol;
        internal NamedTypeSymbol AdaptedNamedTypeSymbol { get; }
    }
#endif
}
