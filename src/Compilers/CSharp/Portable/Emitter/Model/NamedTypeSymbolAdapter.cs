// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal partial class NamedTypeSymbol :
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
            get { return this.TypeKind == TypeKind.Enum; }
        }

        bool Cci.ITypeReference.IsValueType
        {
            get { return this.IsValueType; }
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

                if (this.IsDefinition)
                {
                    return this.PrimitiveTypeCode;
                }

                return Cci.PrimitiveTypeCode.NotPrimitive;
            }
        }

        TypeDefinitionHandle Cci.ITypeReference.TypeDef
        {
            get
            {
                PENamedTypeSymbol peNamedType = this as PENamedTypeSymbol;
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

                if (!this.IsDefinition &&
                    this.Arity > 0)
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

                if (this.IsDefinition &&
                    (object)this.ContainingType == null)
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

            if ((object)this.ContainingType == null &&
                this.IsDefinition &&
                this.ContainingModule == moduleBeingBuilt.SourceModule)
            {
                return this;
            }

            return null;
        }


        Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference
        {
            get
            {
                if ((object)this.ContainingType != null)
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

            if ((object)this.ContainingType != null &&
                this.IsDefinition &&
                this.ContainingModule == moduleBeingBuilt.SourceModule)
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

                if (!this.IsDefinition &&
                    (this.Arity == 0 || PEModuleBuilder.IsGenericType(this.ContainingType)))
                {
                    Debug.Assert((object)this.ContainingType != null &&
                            PEModuleBuilder.IsGenericType(this.ContainingType));
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

            if (this.IsDefinition && // can't be generic instantiation
                this.ContainingModule == moduleBeingBuilt.SourceModule) // must be declared in the module we are building
            {
                return this;
            }

            return null;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
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
            NamedTypeSymbol baseType = this.BaseTypeNoUseSiteDiagnostics;

            if (this.IsScriptClass)
            {
                // although submission and scripts semantically doesn't have a base we need to emit one into metadata:
                Debug.Assert((object)baseType == null);
                baseType = this.ContainingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object);
            }

            return ((object)baseType != null) ? moduleBeingBuilt.Translate(baseType,
                                                                   syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                                   diagnostics: context.Diagnostics) : null;
        }

        IEnumerable<Cci.IEventDefinition> Cci.ITypeDefinition.GetEvents(EmitContext context)
        {
            CheckDefinitionInvariant();
            foreach (IEventDefinition e in GetEventsToEmit())
            {
                // If any accessor should be included, then the event should be included too
                if (e.ShouldInclude(context) || !e.GetAccessors(context).IsEmpty())
                {
                    yield return e;
                }
            }
        }

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

        IEnumerable<Cci.MethodImplementation> Cci.ITypeDefinition.GetExplicitImplementationOverrides(EmitContext context)
        {
            CheckDefinitionInvariant();

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (var member in this.GetMembers())
            {
                if (member.Kind == SymbolKind.Method)
                {
                    var method = (MethodSymbol)member;
                    Debug.Assert((object)method.PartialDefinitionPart == null); // must be definition

                    var explicitImplementations = method.ExplicitInterfaceImplementations;
                    if (explicitImplementations.Length != 0)
                    {
                        foreach (var implemented in method.ExplicitInterfaceImplementations)
                        {
                            yield return new Microsoft.Cci.MethodImplementation(method, moduleBeingBuilt.TranslateOverriddenMethodReference(implemented, (CSharpSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics));
                        }
                    }

                    if (this.IsInterface)
                    {
                        continue;
                    }

                    if (method.RequiresExplicitOverride())
                    {
                        // If C# and the runtime don't agree on the overridden method, then 
                        // we will mark the method as newslot (see MethodSymbolAdapter) and
                        // specify the override explicitly.
                        // This mostly affects accessors - C# ignores method interactions
                        // between accessors and non-accessors, whereas the runtime does not.
                        yield return new Microsoft.Cci.MethodImplementation(method, moduleBeingBuilt.TranslateOverriddenMethodReference(method.OverriddenMethod, (CSharpSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics));
                    }
                    else if (method.MethodKind == MethodKind.Destructor && this.SpecialType != SpecialType.System_Object)
                    {
                        // New in Roslyn: all destructors explicitly override (or are) System.Object.Finalize so that
                        // they are guaranteed to be runtime finalizers.  As a result, it is no longer possible to create
                        // a destructor that will never be invoked by the runtime.
                        // NOTE: If System.Object doesn't contain a destructor, you're on your own - this destructor may
                        // or not be called by the runtime.
                        TypeSymbol objectType = this.DeclaringCompilation.GetSpecialType(CodeAnalysis.SpecialType.System_Object);
                        foreach (Symbol objectMember in objectType.GetMembers(WellKnownMemberNames.DestructorName))
                        {
                            MethodSymbol objectMethod = objectMember as MethodSymbol;
                            if ((object)objectMethod != null && objectMethod.MethodKind == MethodKind.Destructor)
                            {
                                yield return new Microsoft.Cci.MethodImplementation(method, moduleBeingBuilt.TranslateOverriddenMethodReference(objectMethod, (CSharpSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics));
                            }
                        }
                    }
                }
            }

            if (this.IsInterface)
            {
                yield break;
            }

            var syntheticMethods = moduleBeingBuilt.GetSynthesizedMethods(this);
            if (syntheticMethods != null)
            {
                foreach (var m in syntheticMethods)
                {
                    var method = m as MethodSymbol;
                    if ((object)method != null)
                    {
                        Debug.Assert((object)method.PartialDefinitionPart == null); // must be definition

                        foreach (var implemented in method.ExplicitInterfaceImplementations)
                        {
                            yield return new Microsoft.Cci.MethodImplementation(method, moduleBeingBuilt.TranslateOverriddenMethodReference(implemented, (CSharpSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics));
                        }

                        Debug.Assert(!method.RequiresExplicitOverride());
                    }
                }
            }
        }

        IEnumerable<Cci.IFieldDefinition> Cci.ITypeDefinition.GetFields(EmitContext context)
        {
            CheckDefinitionInvariant();

            // All fields in a struct should be emitted
            bool isStruct = this.IsStructType();

            foreach (var f in GetFieldsToEmit())
            {
                if (isStruct || f.ShouldInclude(context))
                {
                    yield return f;
                }
            }

            IEnumerable<Cci.IFieldDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedFields(this);

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

        internal abstract IEnumerable<FieldSymbol> GetFieldsToEmit();

        IEnumerable<Cci.IGenericTypeParameter> Cci.ITypeDefinition.GenericParameters
        {
            get
            {
                CheckDefinitionInvariant();

                foreach (var t in this.TypeParameters)
                {
                    yield return t;
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
            get { return (ushort)this.Arity; }
        }

        IEnumerable<Cci.TypeReferenceWithAttributes> Cci.ITypeDefinition.Interfaces(EmitContext context)
        {
            Debug.Assert(((Cci.ITypeReference)this).AsTypeDefinition(context) != null);

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (NamedTypeSymbol @interface in this.GetInterfacesToEmit())
            {
                var typeRef = moduleBeingBuilt.Translate(
                    @interface,
                    syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                    diagnostics: context.Diagnostics,
                    fromImplements: true);

                var type = TypeWithAnnotations.Create(@interface);
                yield return type.GetTypeRefWithAttributes(
                    moduleBeingBuilt,
                    declaringSymbol: this,
                    typeRef);
            }
        }

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
                    seen = new HashSet<NamedTypeSymbol>(TypeSymbol.EqualsCLRSignatureComparer);
                }
                if (seen.Add(@interface))
                {
                    builder.Add(@interface);
                    InterfacesVisit(@interface, builder, ref seen);
                }
            }
        }

        bool Cci.ITypeDefinition.IsAbstract
        {
            get
            {
                CheckDefinitionInvariant();
                return IsMetadataAbstract;
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

        bool Cci.ITypeDefinition.IsBeforeFieldInit
        {
            get
            {
                CheckDefinitionInvariant();

                switch (this.TypeKind)
                {
                    case TypeKind.Enum:
                    case TypeKind.Delegate:
                    //C# interfaces don't have fields so the flag doesn't really matter, but Dev10 omits it
                    case TypeKind.Interface:
                        return false;
                }

                //apply the beforefieldinit attribute unless there is an explicitly specified static constructor
                foreach (var member in GetMembers(WellKnownMemberNames.StaticConstructorName))
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
                return this.IsComImport;
            }
        }

        bool Cci.ITypeDefinition.IsGeneric
        {
            get
            {
                CheckDefinitionInvariant();
                return this.Arity != 0;
            }
        }

        bool Cci.ITypeDefinition.IsInterface
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsInterface;
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
                return this.IsSerializable;
            }
        }

        bool Cci.ITypeDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return this.HasSpecialName;
            }
        }

        bool Cci.ITypeDefinition.IsWindowsRuntimeImport
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsWindowsRuntimeImport;
            }
        }

        bool Cci.ITypeDefinition.IsSealed
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsMetadataSealed;
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

        IEnumerable<Cci.IMethodDefinition> Cci.ITypeDefinition.GetMethods(EmitContext context)
        {
            CheckDefinitionInvariant();

            foreach (var method in this.GetMethodsToEmit())
            {
                Debug.Assert((object)method != null);
                if (method.ShouldInclude(context))
                {
                    yield return method;
                }
            }

            IEnumerable<Cci.IMethodDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedMethods(this);

            if (generated != null)
            {
                foreach (var m in generated)
                {
                    if (m.ShouldInclude(context))
                    {
                        yield return m;
                    }
                }
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

                    if (method.IsPartialDefinition())
                    {
                        // Don't emit partial methods without an implementation part.
                        if ((object)method.PartialImplementationPart == null)
                        {
                            continue;
                        }
                    }
                    // Don't emit the default value type constructor - the runtime handles that
                    else if (method.IsDefaultValueTypeConstructor())
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        IEnumerable<Cci.INestedTypeDefinition> Cci.ITypeDefinition.GetNestedTypes(EmitContext context)
        {
            CheckDefinitionInvariant();

            foreach (NamedTypeSymbol type in this.GetTypeMembers()) // Ordered.
            {
                yield return type;
            }

            IEnumerable<Cci.INestedTypeDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedTypes(this);

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

            foreach (IPropertyDefinition property in this.GetPropertiesToEmit())
            {
                Debug.Assert((object)property != null);
                // If any accessor should be included, then the property should be included too
                if (property.ShouldInclude(context) || !property.GetAccessors(context).IsEmpty())
                {
                    yield return property;
                }
            }

            IEnumerable<Cci.IPropertyDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedProperties(this);

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

        bool Cci.ITypeDefinition.HasDeclarativeSecurity
        {
            get
            {
                CheckDefinitionInvariant();
                return this.HasDeclarativeSecurity;
            }
        }

        IEnumerable<Cci.SecurityAttribute> Cci.ITypeDefinition.SecurityAttributes
        {
            get
            {
                CheckDefinitionInvariant();
                return this.GetSecurityInformation() ?? SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
            }
        }

        ushort Cci.ITypeDefinition.Alignment
        {
            get
            {
                CheckDefinitionInvariant();
                var layout = this.Layout;
                return (ushort)layout.Alignment;
            }
        }

        LayoutKind Cci.ITypeDefinition.Layout
        {
            get
            {
                CheckDefinitionInvariant();
                return this.Layout.Kind;
            }
        }

        uint Cci.ITypeDefinition.SizeOf
        {
            get
            {
                CheckDefinitionInvariant();
                return (uint)this.Layout.Size;
            }
        }

        CharSet Cci.ITypeDefinition.StringFormat
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MarshallingCharSet;
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
                return MangleName;
            }
        }

        string Cci.INamedEntity.Name
        {
            get
            {
                string unsuffixedName = this.Name;

                // CLR generally allows names with dots, however some APIs like IMetaDataImport
                // can only return full type names combined with namespaces. 
                // see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
                // When working with such APIs, names with dots become ambiguous since metadata 
                // consumer cannot figure where namespace ends and actual type name starts.
                // Therefore it is a good practice to avoid type names with dots.
                // Exception: The EE copies type names from metadata, which may contain dots already.
                Debug.Assert(this.IsErrorType() ||
                    !unsuffixedName.Contains(".") ||
                    this.OriginalDefinition is PENamedTypeSymbol, "type name contains dots: " + unsuffixedName);

                return unsuffixedName;
            }
        }

        Cci.IUnitReference Cci.INamespaceTypeReference.GetUnit(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.ITypeReference)this).AsNamespaceTypeReference != null);
            return moduleBeingBuilt.Translate(this.ContainingModule, context.Diagnostics);
        }

        string Cci.INamespaceTypeReference.NamespaceName
        {
            get
            {
                // INamespaceTypeReference is a type contained in a namespace
                // if this method is called for a nested type, we are in big trouble.
                Debug.Assert(((Cci.ITypeReference)this).AsNamespaceTypeReference != null);

                return this.ContainingNamespace.QualifiedName;
            }
        }

        bool Cci.INamespaceTypeDefinition.IsPublic
        {
            get
            {
                Debug.Assert((object)this.ContainingType == null && this.ContainingModule is SourceModuleSymbol);

                return PEModuleBuilder.MemberVisibility(this) == Cci.TypeMemberVisibility.Public;
            }
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.ITypeReference)this).AsNestedTypeReference != null);

            Debug.Assert(this.IsDefinitionOrDistinct());

            return moduleBeingBuilt.Translate(this.ContainingType,
                                              syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                              diagnostics: context.Diagnostics,
                                              needDeclaration: this.IsDefinition);
        }

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                Debug.Assert((object)this.ContainingType != null);
                CheckDefinitionInvariant();

                return this.ContainingType;
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                Debug.Assert((object)this.ContainingType != null);
                CheckDefinitionInvariant();

                return PEModuleBuilder.MemberVisibility(this);
            }
        }

        ImmutableArray<Cci.ITypeReference> Cci.IGenericTypeInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var builder = ArrayBuilder<Microsoft.Cci.ITypeReference>.GetInstance();
            Debug.Assert(((Cci.ITypeReference)this).AsGenericTypeInstanceReference != null);

            var arguments = this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

            for (int i = 0; i < arguments.Length; i++)
            {
                var arg = moduleBeingBuilt.Translate(arguments[i].Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
                var modifiers = arguments[i].CustomModifiers;
                if (!modifiers.IsDefaultOrEmpty)
                {
                    arg = new Cci.ModifiedTypeReference(arg, modifiers.As<Cci.ICustomModifier>());
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
            return moduleBeingBuilt.Translate(this.OriginalDefinition, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
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
}
