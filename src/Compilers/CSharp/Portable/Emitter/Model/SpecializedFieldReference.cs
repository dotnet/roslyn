// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a reference to a field of a generic type instantiation.
    /// e.g.
    /// A{int}.Field
    /// A{int}.B{string}.C.Field
    /// </summary>
    internal sealed class SpecializedFieldReference : TypeMemberReference, Cci.ISpecializedFieldReference
    {
        private readonly FieldSymbol _underlyingField;

        public SpecializedFieldReference(FieldSymbol underlyingField)
        {
            Debug.Assert((object)underlyingField != null);

            _underlyingField = underlyingField;
        }

        protected override Symbol UnderlyingSymbol
        {
            get
            {
                return _underlyingField;
            }
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.ISpecializedFieldReference)this);
        }

        Cci.IFieldReference Cci.ISpecializedFieldReference.UnspecializedVersion
        {
            get
            {
                Debug.Assert(_underlyingField.OriginalDefinition.IsDefinition);
                return _underlyingField.OriginalDefinition.GetCciAdapter();
            }
        }

        Cci.ISpecializedFieldReference Cci.IFieldReference.AsSpecializedFieldReference
        {
            get
            {
                return this;
            }
        }

        Cci.ITypeReference Cci.IFieldReference.GetType(EmitContext context)
        {
            TypeWithAnnotations oldType = _underlyingField.TypeWithAnnotations;
            var customModifiers = oldType.CustomModifiers;
            var type = ((PEModuleBuilder)context.Module).Translate(oldType.Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode, diagnostics: context.Diagnostics, eraseExtensions: true);

            if (customModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Cci.ModifiedTypeReference(type, ImmutableArray<Cci.ICustomModifier>.CastUp(customModifiers));
            }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.IFieldReference.RefCustomModifiers =>
            ImmutableArray<Cci.ICustomModifier>.CastUp(_underlyingField.RefCustomModifiers);

        bool Cci.IFieldReference.IsByReference => _underlyingField.RefKind != RefKind.None;

        Cci.IFieldDefinition Cci.IFieldReference.GetResolvedField(EmitContext context)
        {
            return null;
        }

        bool Cci.IFieldReference.IsContextualNamedEntity
        {
            get
            {
                return false;
            }
        }
    }
}
