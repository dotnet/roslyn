// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// An alternative for <see cref="SynthesizedDelegateSymbol"/> for delegate types with
        /// parameter types or return type that cannot be used as generic type arguments.
        /// </summary>
        internal sealed class AnonymousDelegateTemplateSymbol : AnonymousTypeOrDelegateTemplateSymbol
        {
            private readonly ImmutableArray<Symbol> _members;

            internal AnonymousDelegateTemplateSymbol(AnonymousTypeManager manager, AnonymousDelegateTypeDescriptor typeDescr)
                : base(manager, typeDescr.Location)
            {
                // AnonymousTypeOrDelegateComparer requires an actual location.
                Debug.Assert(SmallestLocation != null);
                Debug.Assert(SmallestLocation != Location.None);

                int typeParameterCount = typeDescr.TypeParameterCount;
                TypeMap typeMap;
                if (typeParameterCount == 0)
                {
                    TypeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                    typeMap = TypeMap.Empty;
                }
                else
                {
                    var typeParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance(typeParameterCount);
                    for (int i = 0; i < typeParameterCount; i++)
                    {
                        typeParameters.Add(new AnonymousTypeParameterSymbol(this, i, "T" + (i + 1)));
                    }
                    TypeParameters = typeParameters.ToImmutableAndFree();
                    typeMap = new TypeMap(IndexedTypeParameterSymbol.TakeSymbols(typeParameterCount), TypeParameters);
                }

                var constructor = new SynthesizedDelegateConstructor(this, manager.System_Object, manager.System_IntPtr);
                // https://github.com/dotnet/roslyn/issues/56808: Synthesized delegates should include BeginInvoke() and EndInvoke().
                var invokeMethod = createInvokeMethod(this, typeDescr.Fields, typeMap);
                _members = ImmutableArray.Create<Symbol>(constructor, invokeMethod);

                static SynthesizedDelegateInvokeMethod createInvokeMethod(
                    AnonymousDelegateTemplateSymbol containingType,
                    ImmutableArray<AnonymousTypeField> fields,
                    TypeMap typeMap)
                {
                    var parameterCount = fields.Length - 1;
                    var parameterTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance(parameterCount);
                    var parameterRefKinds = ArrayBuilder<RefKind>.GetInstance(parameterCount);
                    for (int i = 0; i < parameterCount; i++)
                    {
                        var parameterOrReturn = fields[i];
                        parameterTypes.Add(typeMap.SubstituteType(parameterOrReturn.Type));
                        parameterRefKinds.Add(parameterOrReturn.RefKind);
                    }

                    var returnParameter = fields[^1];
                    var returnType = typeMap.SubstituteType(returnParameter.Type);
                    var returnRefKind = returnParameter.RefKind;

                    var method = new SynthesizedDelegateInvokeMethod(containingType, parameterTypes, parameterRefKinds, returnType, returnRefKind);
                    parameterRefKinds.Free();
                    parameterTypes.Free();
                    return method;
                }
            }

            // AnonymousTypeOrDelegateComparer should not be calling this property for delegate
            // types since AnonymousTypeOrDelegateComparer is only used during emit and we
            // should only be emitting delegate types inferred from distinct locations in source.
            internal override string TypeDescriptorKey => throw new System.NotImplementedException();

            public override TypeKind TypeKind => TypeKind.Delegate;

            public override IEnumerable<string> MemberNames => GetMembers().SelectAsArray(member => member.Name);

            public override ImmutableArray<Symbol> GetMembers() => _members;

            public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray((member, name) => member.Name == name, name);

            internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => SpecializedCollections.EmptyEnumerable<FieldSymbol>();

            internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

            internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => Manager.System_MulticastDelegate;

            public override ImmutableArray<TypeParameterSymbol> TypeParameters { get; }
        }
    }
}
