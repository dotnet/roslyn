// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        internal sealed class AnonymousDelegatePublicSymbol : AnonymousTypeOrDelegatePublicSymbol
        {
            private ImmutableArray<Symbol> _lazyMembers;

            /// <summary>
            /// This member does not participate in equality because it is not reflecting any semantic aspect of the symbol.
            /// It is only used to determine if we need to check for <see cref="MessageID.IDS_FeatureParamsCollections"/>
            /// feature availability, which happens if this field is set to 'true'. 
            /// If in the process of merging equivalent types, the one with 'false' wins over the one with 'true',
            /// that is fine, because that means that the feature availability check is performed on a
            /// method declared in this compilation.
            /// </summary>
            internal readonly bool CheckParamsCollectionsFeatureAvailability;

            internal AnonymousDelegatePublicSymbol(AnonymousTypeManager manager, AnonymousTypeDescriptor typeDescr, bool checkParamsCollectionsFeatureAvailability) :
                base(manager, typeDescr)
            {
                CheckParamsCollectionsFeatureAvailability = checkParamsCollectionsFeatureAvailability;
            }

            internal override NamedTypeSymbol MapToImplementationSymbol()
            {
                return Manager.ConstructAnonymousDelegateImplementationSymbol(this, generation: 0);
            }

            internal override AnonymousTypeOrDelegatePublicSymbol SubstituteTypes(AbstractTypeMap map)
            {
                var typeDescr = TypeDescriptor.SubstituteTypes(map, out bool changed);
                return changed ?
                    new AnonymousDelegatePublicSymbol(Manager, typeDescr, CheckParamsCollectionsFeatureAvailability) :
                    this;
            }

            public override TypeKind TypeKind => TypeKind.Delegate;

            internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => Manager.System_MulticastDelegate;

            public override IEnumerable<string> MemberNames => GetMembers().SelectAsArray(member => member.Name);

            public override ImmutableArray<Symbol> GetMembers()
            {
                if (_lazyMembers.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyMembers, CreateMembers());
                }
                return _lazyMembers;
            }

            private ImmutableArray<Symbol> CreateMembers()
            {
                var constructor = new SynthesizedDelegateConstructor(this, Manager.System_Object, Manager.System_IntPtr);
                var fields = TypeDescriptor.Fields;
                int parameterCount = fields.Length - 1;
                var parameters = ArrayBuilder<SynthesizedDelegateInvokeMethod.ParameterDescription>.GetInstance(parameterCount);
                for (int i = 0; i < parameterCount; i++)
                {
                    var field = fields[i];
                    parameters.Add(
                        new SynthesizedDelegateInvokeMethod.ParameterDescription(field.TypeWithAnnotations, field.RefKind, field.Scope, field.DefaultValue, isParams: field.IsParams, hasUnscopedRefAttribute: field.HasUnscopedRefAttribute));
                }
                var returnField = fields.Last();
                var invokeMethod = new SynthesizedDelegateInvokeMethod(this, parameters, returnField.TypeWithAnnotations, returnField.RefKind);
                parameters.Free();
                // https://github.com/dotnet/roslyn/issues/56808: Synthesized delegates should include BeginInvoke() and EndInvoke().
                return ImmutableArray.Create<Symbol>(constructor, invokeMethod);
            }

            public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray((member, name) => member.Name == name, name);

            public override bool IsImplicitlyDeclared => true;

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

            internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
            {
                if (ReferenceEquals(this, t2))
                {
                    return true;
                }

                var other = t2 as AnonymousDelegatePublicSymbol;
                return other is { } && this.TypeDescriptor.Equals(other.TypeDescriptor, comparison);
            }

            public override int GetHashCode()
            {
                return this.TypeDescriptor.GetHashCode();
            }
        }
    }
}
