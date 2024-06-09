// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

#if !DEBUG
using NamedTypeSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol;
using FieldSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.FieldSymbol;
using MethodSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.MethodSymbol;
using EventSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.EventSymbol;
using PropertySymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.PropertySymbol;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedType : EmbeddedTypesManager.CommonEmbeddedType
    {
        private bool _embeddedAllMembersOfImplementedInterface;

        public EmbeddedType(EmbeddedTypesManager typeManager, NamedTypeSymbolAdapter underlyingNamedType) :
            base(typeManager, underlyingNamedType)
        {
            Debug.Assert(underlyingNamedType.AdaptedNamedTypeSymbol.IsDefinition);
            Debug.Assert(underlyingNamedType.AdaptedNamedTypeSymbol.IsTopLevelType());
            Debug.Assert(!underlyingNamedType.AdaptedNamedTypeSymbol.IsGenericType);
        }

        public void EmbedAllMembersOfImplementedInterface(SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(UnderlyingNamedType.AdaptedNamedTypeSymbol.IsInterfaceType());

            if (_embeddedAllMembersOfImplementedInterface)
            {
                return;
            }

            _embeddedAllMembersOfImplementedInterface = true;

            // Embed all members
            foreach (MethodSymbol m in UnderlyingNamedType.AdaptedNamedTypeSymbol.GetMethodsToEmit())
            {
                if ((object)m != null)
                {
                    TypeManager.EmbedMethod(this, m.GetCciAdapter(), syntaxNodeOpt, diagnostics);
                }
            }

            // We also should embed properties and events, but we don't need to do this explicitly here
            // because accessors embed them automatically.

            // Do the same for implemented interfaces.
            foreach (NamedTypeSymbol @interface in UnderlyingNamedType.AdaptedNamedTypeSymbol.GetInterfacesToEmit())
            {
                Debug.Assert(!@interface.IsExtension);
                TypeManager.ModuleBeingBuilt.Translate(@interface, syntaxNodeOpt, diagnostics, keepExtension: false, fromImplements: true);
            }
        }

        protected override int GetAssemblyRefIndex()
        {
            ImmutableArray<AssemblySymbol> refs = TypeManager.ModuleBeingBuilt.SourceModule.GetReferencedAssemblySymbols();
            return refs.IndexOf(UnderlyingNamedType.AdaptedNamedTypeSymbol.ContainingAssembly, ReferenceEqualityComparer.Instance);
        }

        protected override bool IsPublic
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.DeclaredAccessibility == Accessibility.Public;
            }
        }

        protected override Cci.ITypeReference GetBaseClass(PEModuleBuilder moduleBuilder, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            NamedTypeSymbol baseType = UnderlyingNamedType.AdaptedNamedTypeSymbol.BaseTypeNoUseSiteDiagnostics;
            Debug.Assert(baseType?.IsExtension != true);
            return (object)baseType != null ? moduleBuilder.Translate(baseType, syntaxNodeOpt, diagnostics, keepExtension: false) : null;
        }

        protected override IEnumerable<FieldSymbolAdapter> GetFieldsToEmit()
        {
            return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetFieldsToEmit()
#if DEBUG
                .Select(s => s.GetCciAdapter())
#endif
                ;
        }

        protected override IEnumerable<MethodSymbolAdapter> GetMethodsToEmit()
        {
            return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetMethodsToEmit()
#if DEBUG
                .Select(s => s?.GetCciAdapter())
#endif
                ;
        }

        protected override IEnumerable<EventSymbolAdapter> GetEventsToEmit()
        {
            return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetEventsToEmit()
#if DEBUG
                .Select(s => s.GetCciAdapter())
#endif
                ;
        }

        protected override IEnumerable<PropertySymbolAdapter> GetPropertiesToEmit()
        {
            return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetPropertiesToEmit()
#if DEBUG
                .Select(s => s.GetCciAdapter())
#endif
                ;
        }

        protected override IEnumerable<Cci.TypeReferenceWithAttributes> GetInterfaces(EmitContext context)
        {
            Debug.Assert((object)TypeManager.ModuleBeingBuilt == context.Module);

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (NamedTypeSymbol @interface in UnderlyingNamedType.AdaptedNamedTypeSymbol.GetInterfacesToEmit())
            {
                Debug.Assert(!@interface.IsExtension);

                var typeRef = moduleBeingBuilt.Translate(
                    @interface,
                    (CSharpSyntaxNode)context.SyntaxNode,
                    context.Diagnostics, keepExtension: false);

                var type = TypeWithAnnotations.Create(@interface);
                yield return type.GetTypeRefWithAttributes(
                    moduleBeingBuilt,
                    declaringSymbol: UnderlyingNamedType.AdaptedNamedTypeSymbol,
                    typeRef);
            }
        }

        protected override bool IsAbstract
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsMetadataAbstract;
            }
        }

        protected override bool IsBeforeFieldInit
        {
            get
            {
                switch (UnderlyingNamedType.AdaptedNamedTypeSymbol.TypeKind)
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
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsComImport;
            }
        }

        protected override bool IsInterface
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsInterfaceType();
            }
        }

        protected override bool IsDelegate
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsDelegateType();
            }
        }

        protected override bool IsSerializable
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsSerializable;
            }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.HasSpecialName;
            }
        }

        protected override bool IsWindowsRuntimeImport
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsWindowsRuntimeImport;
            }
        }

        protected override bool IsSealed
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsMetadataSealed;
            }
        }

        protected override TypeLayout? GetTypeLayoutIfStruct()
        {
            if (UnderlyingNamedType.AdaptedNamedTypeSymbol.IsStructType())
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.Layout;
            }
            return null;
        }

        protected override System.Runtime.InteropServices.CharSet StringFormat
        {
            get
            {
                return UnderlyingNamedType.AdaptedNamedTypeSymbol.MarshallingCharSet;
            }
        }

        protected override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetCustomAttributesToEmit(moduleBuilder);
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
                return SynthesizedAttributeData.Create(TypeManager.ModuleBeingBuilt.Compilation, ctor, ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
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
                    string guidString = TypeManager.GetAssemblyGuidString(UnderlyingNamedType.AdaptedNamedTypeSymbol.ContainingAssembly);
                    return SynthesizedAttributeData.Create(TypeManager.ModuleBeingBuilt.Compilation, ctor,
                                    ImmutableArray.Create(new TypedConstant(stringType, TypedConstantKind.Primitive, guidString),
                                                    new TypedConstant(stringType, TypedConstantKind.Primitive,
                                                                            UnderlyingNamedType.AdaptedNamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))),
                                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }
            }

            return null;
        }

        protected override void ReportMissingAttribute(AttributeDescription description, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            EmbeddedTypesManager.Error(diagnostics, ErrorCode.ERR_InteropTypeMissingAttribute, syntaxNodeOpt, UnderlyingNamedType.AdaptedNamedTypeSymbol, description.FullName);
        }

        protected override void EmbedDefaultMembers(string defaultMember, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            foreach (Symbol s in UnderlyingNamedType.AdaptedNamedTypeSymbol.GetMembers(defaultMember))
            {
                switch (s.Kind)
                {
                    case SymbolKind.Field:
                        TypeManager.EmbedField(this, ((FieldSymbol)s).GetCciAdapter(), syntaxNodeOpt, diagnostics);
                        break;
                    case SymbolKind.Method:
                        TypeManager.EmbedMethod(this, ((MethodSymbol)s).GetCciAdapter(), syntaxNodeOpt, diagnostics);
                        break;
                    case SymbolKind.Property:
                        TypeManager.EmbedProperty(this, ((PropertySymbol)s).GetCciAdapter(), syntaxNodeOpt, diagnostics);
                        break;
                    case SymbolKind.Event:
                        TypeManager.EmbedEvent(this, ((EventSymbol)s).GetCciAdapter(), syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding: false);
                        break;
                }
            }
        }
    }
}
