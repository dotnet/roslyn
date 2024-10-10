// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents a baking field for an anonymous type template property symbol.
        /// </summary>
        private sealed class AnonymousTypeFieldSymbol : FieldSymbol
        {
            private readonly PropertySymbol _property;
            private string _lazyName;

            public AnonymousTypeFieldSymbol(PropertySymbol property)
            {
                Debug.Assert((object)property != null);
                _property = property;
            }

            internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            {
                return _property.TypeWithAnnotations;
            }

            public override RefKind RefKind => RefKind.None;

            public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public override string Name
            {
                get { return _lazyName ??= GeneratedNames.MakeAnonymousTypeBackingFieldName(_property.Name); }
            }

            public override FlowAnalysisAnnotations FlowAnalysisAnnotations
                => FlowAnalysisAnnotations.None;

            internal override bool HasSpecialName
            {
                get { return false; }
            }

            internal override bool HasRuntimeSpecialName
            {
                get { return false; }
            }

            internal override bool IsNotSerialized
            {
                get { return false; }
            }

            internal override MarshalPseudoCustomAttributeData MarshallingInformation
            {
                get { return null; }
            }

            internal override int? TypeLayoutOffset
            {
                get { return null; }
            }

            public override Symbol AssociatedSymbol
            {
                get
                {
                    return _property;
                }
            }

            public override bool IsReadOnly
            {
                get { return true; }
            }

            public override bool IsVolatile
            {
                get { return false; }
            }

            public override bool IsConst
            {
                get { return false; }
            }

            internal sealed override ObsoleteAttributeData ObsoleteAttributeData
            {
                get { return null; }
            }

            internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
            {
                return null;
            }

            public override Symbol ContainingSymbol
            {
                get { return _property.ContainingType; }
            }

            public override NamedTypeSymbol ContainingType
            {
                get
                {
                    return _property.ContainingType;
                }
            }

            public override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray<Location>.Empty; }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return ImmutableArray<SyntaxReference>.Empty;
                }
            }

            public override Accessibility DeclaredAccessibility
            {
                get { return Accessibility.Private; }
            }

            public override bool IsStatic
            {
                get { return false; }
            }

            public override bool IsImplicitlyDeclared
            {
                get { return true; }
            }

            internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

                AnonymousTypeManager manager = ((AnonymousTypeTemplateSymbol)this.ContainingSymbol).Manager;

                AddSynthesizedAttribute(ref attributes, manager.Compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Diagnostics_DebuggerBrowsableAttribute__ctor,
                    ImmutableArray.Create(
                        new TypedConstant(manager.System_Diagnostics_DebuggerBrowsableState, TypedConstantKind.Enum, DebuggerBrowsableState.Never))));
            }

            internal override bool IsRequired => false;
        }
    }
}
