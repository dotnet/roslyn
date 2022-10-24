﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        internal sealed class AnonymousDelegateTemplateSymbol : AnonymousTypeOrDelegateTemplateSymbol
        {
            private readonly ImmutableArray<Symbol> _members;

            /// <summary>
            /// True if any of the delegate parameter types or return type are
            /// fixed types rather than type parameters.
            /// </summary>
            internal readonly bool HasFixedTypes;

            /// <summary>
            /// A delegate type where the parameter types and return type
            /// of the delegate signature are type parameters.
            /// </summary>
            internal AnonymousDelegateTemplateSymbol(
                AnonymousTypeManager manager,
                string name,
                TypeSymbol objectType,
                TypeSymbol intPtrType,
                TypeSymbol? voidReturnTypeOpt,
                int parameterCount,
                RefKindVector refKinds)
                : base(manager, Location.None) // Location is not needed since NameAndIndex is set explicitly below.
            {
                Debug.Assert(refKinds.IsNull || parameterCount == refKinds.Capacity - (voidReturnTypeOpt is { } ? 0 : 1));

                HasFixedTypes = false;
                TypeParameters = createTypeParameters(this, parameterCount, returnsVoid: voidReturnTypeOpt is { });
                NameAndIndex = new NameAndIndex(name, index: 0);

                var constructor = new SynthesizedDelegateConstructor(this, objectType, intPtrType);
                // https://github.com/dotnet/roslyn/issues/56808: Synthesized delegates should include BeginInvoke() and EndInvoke().
                var invokeMethod = createInvokeMethod(this, refKinds, voidReturnTypeOpt);
                _members = ImmutableArray.Create<Symbol>(constructor, invokeMethod);

                static SynthesizedDelegateInvokeMethod createInvokeMethod(AnonymousDelegateTemplateSymbol containingType, RefKindVector refKinds, TypeSymbol? voidReturnTypeOpt)
                {
                    var typeParams = containingType.TypeParameters;

                    int parameterCount = typeParams.Length - (voidReturnTypeOpt is null ? 1 : 0);
                    var parameters = ArrayBuilder<(TypeWithAnnotations Type, RefKind RefKind, DeclarationScope Scope, ConstantValue? DefaultValue, bool IsParams)>.GetInstance(parameterCount);
                    for (int i = 0; i < parameterCount; i++)
                    {
                        parameters.Add((TypeWithAnnotations.Create(typeParams[i]), refKinds.IsNull ? RefKind.None : refKinds[i], DeclarationScope.Unscoped, DefaultValue: null, IsParams: false));
                    }

                    // if we are given Void type the method returns Void, otherwise its return type is the last type parameter of the delegate:
                    var returnType = TypeWithAnnotations.Create(voidReturnTypeOpt ?? typeParams[parameterCount]);
                    var returnRefKind = (refKinds.IsNull || voidReturnTypeOpt is { }) ? RefKind.None : refKinds[parameterCount];

                    var method = new SynthesizedDelegateInvokeMethod(containingType, parameters, returnType, returnRefKind);
                    parameters.Free();
                    return method;
                }

                static ImmutableArray<TypeParameterSymbol> createTypeParameters(AnonymousDelegateTemplateSymbol containingType, int parameterCount, bool returnsVoid)
                {
                    var typeParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance(parameterCount + (returnsVoid ? 0 : 1));
                    for (int i = 0; i < parameterCount; i++)
                    {
                        typeParameters.Add(new AnonymousTypeManager.AnonymousTypeParameterSymbol(containingType, i, "T" + (i + 1)));
                    }

                    if (!returnsVoid)
                    {
                        typeParameters.Add(new AnonymousTypeManager.AnonymousTypeParameterSymbol(containingType, parameterCount, "TResult"));
                    }

                    return typeParameters.ToImmutableAndFree();
                }
            }

            /// <summary>
            /// A delegate type where at least one of the parameter types or return type
            /// of the delegate signature is a fixed type not a type parameter.
            /// </summary>
            internal AnonymousDelegateTemplateSymbol(AnonymousTypeManager manager, AnonymousTypeDescriptor typeDescr, ImmutableArray<TypeParameterSymbol> typeParametersToSubstitute)
                : base(manager, typeDescr.Location)
            {
                // AnonymousTypeOrDelegateComparer requires an actual location.
                Debug.Assert(SmallestLocation != null);
                Debug.Assert(SmallestLocation != Location.None);

                HasFixedTypes = true;

                TypeMap typeMap;
                int typeParameterCount = typeParametersToSubstitute.Length;
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
                    typeMap = new TypeMap(typeParametersToSubstitute, TypeParameters, allowAlpha: true);
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
                    var parameters = ArrayBuilder<(TypeWithAnnotations, RefKind, DeclarationScope, ConstantValue?, bool)>.GetInstance(parameterCount);
                    for (int i = 0; i < parameterCount; i++)
                    {
                        var field = fields[i];
                        parameters.Add((typeMap.SubstituteType(field.Type), field.RefKind, field.Scope, field.DefaultValue, field.IsParams));
                    }

                    var returnParameter = fields[^1];
                    var returnType = typeMap.SubstituteType(returnParameter.Type);
                    var returnRefKind = returnParameter.RefKind;

                    var method = new SynthesizedDelegateInvokeMethod(containingType, parameters, returnType, returnRefKind);
                    parameters.Free();
                    return method;
                }
            }

            // AnonymousTypeOrDelegateComparer should not be calling this property for delegate
            // types since AnonymousTypeOrDelegateComparer is only used during emit and we
            // should only be emitting delegate types inferred from distinct locations in source.
            internal override string TypeDescriptorKey => throw new System.NotImplementedException();

            public override TypeKind TypeKind => TypeKind.Delegate;

            public override IEnumerable<string> MemberNames => GetMembers().SelectAsArray(member => member.Name);

            internal override bool HasDeclaredRequiredMembers => false;

            public override ImmutableArray<Symbol> GetMembers() => _members;

            public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray((member, name) => member.Name == name, name);

            internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => SpecializedCollections.EmptyEnumerable<FieldSymbol>();

            internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

            internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => Manager.System_MulticastDelegate;

            public override ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

            internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

                var compilation = ContainingSymbol.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes,
                    compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }
        }
    }
}
