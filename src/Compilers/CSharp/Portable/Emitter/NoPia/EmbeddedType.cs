// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedType : EmbeddedTypesManager.CommonEmbeddedType
    {
        private bool _embeddedAllMembersOfImplementedInterface;

        public EmbeddedType(EmbeddedTypesManager typeManager, NamedTypeSymbol underlyingNamedType) :
            base(typeManager, underlyingNamedType)
        {
            Debug.Assert(underlyingNamedType.IsDefinition);
            Debug.Assert(underlyingNamedType.IsTopLevelType());
            Debug.Assert(!underlyingNamedType.IsGenericType);
        }

        public void EmbedAllMembersOfImplementedInterface(SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(UnderlyingNamedType.IsInterfaceType());

            if (_embeddedAllMembersOfImplementedInterface)
            {
                return;
            }

            _embeddedAllMembersOfImplementedInterface = true;

            // Embed all members
            foreach (MethodSymbol m in UnderlyingNamedType.GetMethodsToEmit())
            {
                if ((object)m != null)
                {
                    TypeManager.EmbedMethod(this, m, syntaxNodeOpt, diagnostics);
                }
            }

            // We also should embed properties and events, but we don't need to do this explicitly here
            // because accessors embed them automatically.

            // Do the same for implemented interfaces.
            foreach (NamedTypeSymbol @interface in UnderlyingNamedType.GetInterfacesToEmit())
            {
                TypeManager.ModuleBeingBuilt.Translate(@interface, syntaxNodeOpt, diagnostics, fromImplements: true);
            }
        }

        protected override int GetAssemblyRefIndex()
        {
            ImmutableArray<AssemblySymbol> refs = TypeManager.ModuleBeingBuilt.SourceModule.GetReferencedAssemblySymbols();
            return refs.IndexOf(UnderlyingNamedType.ContainingAssembly, ReferenceEqualityComparer.Instance);
        }

        protected override bool IsPublic
        {
            get
            {
                return UnderlyingNamedType.DeclaredAccessibility == Accessibility.Public;
            }
        }

        protected override Cci.ITypeReference GetBaseClass(PEModuleBuilder moduleBuilder, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            NamedTypeSymbol baseType = UnderlyingNamedType.BaseTypeNoUseSiteDiagnostics;
            return (object)baseType != null ? moduleBuilder.Translate(baseType, syntaxNodeOpt, diagnostics) : null;
        }

        protected override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            return UnderlyingNamedType.GetFieldsToEmit();
        }

        protected override IEnumerable<MethodSymbol> GetMethodsToEmit()
        {
            return UnderlyingNamedType.GetMethodsToEmit();
        }

        protected override IEnumerable<EventSymbol> GetEventsToEmit()
        {
            return UnderlyingNamedType.GetEventsToEmit();
        }

        protected override IEnumerable<PropertySymbol> GetPropertiesToEmit()
        {
            return UnderlyingNamedType.GetPropertiesToEmit();
        }

        protected override IEnumerable<Cci.TypeReferenceWithAttributes> GetInterfaces(EmitContext context)
        {
            Debug.Assert((object)TypeManager.ModuleBeingBuilt == context.Module);

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (NamedTypeSymbol @interface in UnderlyingNamedType.GetInterfacesToEmit())
            {
                var typeRef = moduleBeingBuilt.Translate(
                    @interface,
                    (CSharpSyntaxNode)context.SyntaxNodeOpt,
                    context.Diagnostics);

                var type = TypeWithAnnotations.Create(@interface);
                yield return type.GetTypeRefWithAttributes(
                    moduleBeingBuilt,
                    declaringSymbol: UnderlyingNamedType,
                    typeRef);
            }
        }

        protected override bool IsAbstract
        {
            get
            {
                return UnderlyingNamedType.IsMetadataAbstract;
            }
        }

        protected override bool IsBeforeFieldInit
        {
            get
            {
                switch (UnderlyingNamedType.TypeKind)
                {
                    case TypeKind.Enum:
                    case TypeKind.Delegate:
                    //C# interfaces don't have fields so the flag doesn't really matter, but Dev10 omits it
                    case TypeKind.Interface:
                        return false;
                }

                // We shouldn't embed static constructor.
                return true;
            }
        }

        protected override bool IsComImport
        {
            get
            {
                return UnderlyingNamedType.IsComImport;
            }
        }

        protected override bool IsInterface
        {
            get
            {
                return UnderlyingNamedType.IsInterfaceType();
            }
        }

        protected override bool IsDelegate
        {
            get
            {
                return UnderlyingNamedType.IsDelegateType();
            }
        }

        protected override bool IsSerializable
        {
            get
            {
                return UnderlyingNamedType.IsSerializable;
            }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingNamedType.HasSpecialName;
            }
        }

        protected override bool IsWindowsRuntimeImport
        {
            get
            {
                return UnderlyingNamedType.IsWindowsRuntimeImport;
            }
        }

        protected override bool IsSealed
        {
            get
            {
                return UnderlyingNamedType.IsMetadataSealed;
            }
        }

        protected override TypeLayout? GetTypeLayoutIfStruct()
        {
            if (UnderlyingNamedType.IsStructType())
            {
                return UnderlyingNamedType.Layout;
            }
            return null;
        }

        protected override System.Runtime.InteropServices.CharSet StringFormat
        {
            get
            {
                return UnderlyingNamedType.MarshallingCharSet;
            }
        }

        protected override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return UnderlyingNamedType.GetCustomAttributesToEmit(moduleBuilder);
        }

        protected override CSharpAttributeData CreateTypeIdentifierAttribute(bool hasGuid, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            var member = hasGuid ?
                WellKnownMember.System_Runtime_InteropServices_TypeIdentifierAttribute__ctor :
                WellKnownMember.System_Runtime_InteropServices_TypeIdentifierAttribute__ctorStringString;
            var ctor = TypeManager.GetWellKnownMethod(member, syntaxNodeOpt, diagnostics);
            if ((object)ctor == null)
            {
                return null;
            }

            if (hasGuid)
            {
                // This is an interface with a GuidAttribute, so we will generate the no-parameter TypeIdentifier.
                return new SynthesizedAttributeData(ctor, ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }
            else
            {
                // This is an interface with no GuidAttribute, or some other type, so we will generate the
                // TypeIdentifier with name and scope parameters.

                // Look for a GUID attribute attached to type's containing assembly. If we find one, we'll use it; 
                // otherwise, we expect that we will have reported an error (ERRID_PIAHasNoAssemblyGuid1) about this assembly, since
                // you can't /link against an assembly which lacks a GuidAttribute.

                var stringType = TypeManager.GetSystemStringType(syntaxNodeOpt, diagnostics);

                if ((object)stringType != null)
                {
                    string guidString = TypeManager.GetAssemblyGuidString(UnderlyingNamedType.ContainingAssembly);
                    return new SynthesizedAttributeData(ctor,
                                    ImmutableArray.Create(new TypedConstant(stringType, TypedConstantKind.Primitive, guidString),
                                                    new TypedConstant(stringType, TypedConstantKind.Primitive,
                                                                            UnderlyingNamedType.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))),
                                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }
            }

            return null;
        }

        protected override void ReportMissingAttribute(AttributeDescription description, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            EmbeddedTypesManager.Error(diagnostics, ErrorCode.ERR_InteropTypeMissingAttribute, syntaxNodeOpt, UnderlyingNamedType, description.FullName);
        }

        protected override void EmbedDefaultMembers(string defaultMember, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            foreach (Symbol s in UnderlyingNamedType.GetMembers(defaultMember))
            {
                switch (s.Kind)
                {
                    case SymbolKind.Field:
                        TypeManager.EmbedField(this, (FieldSymbol)s, syntaxNodeOpt, diagnostics);
                        break;
                    case SymbolKind.Method:
                        TypeManager.EmbedMethod(this, (MethodSymbol)s, syntaxNodeOpt, diagnostics);
                        break;
                    case SymbolKind.Property:
                        TypeManager.EmbedProperty(this, (PropertySymbol)s, syntaxNodeOpt, diagnostics);
                        break;
                    case SymbolKind.Event:
                        TypeManager.EmbedEvent(this, (EventSymbol)s, syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding: false);
                        break;
                }
            }
        }
    }
}
