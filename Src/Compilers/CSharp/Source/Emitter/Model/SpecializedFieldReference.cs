// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a reference to a field of a generic type instantiation.
    /// e.g.
    /// A{int}.Field
    /// A{int}.B{string}.C.Field
    /// </summary>
    internal sealed class SpecializedFieldReference : TypeMemberReference, Microsoft.Cci.ISpecializedFieldReference
    {
        private readonly FieldSymbol underlyingField;

        public SpecializedFieldReference(FieldSymbol underlyingField)
        {
            Debug.Assert((object)underlyingField != null);

            this.underlyingField = underlyingField;
        }

        protected override Symbol UnderlyingSymbol
        {
            get
            {
                return underlyingField;
            }
        }

        public override void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.ISpecializedFieldReference)this);
        }

        Microsoft.Cci.IFieldReference Microsoft.Cci.ISpecializedFieldReference.UnspecializedVersion
        {
            get
            {
                System.Diagnostics.Debug.Assert(underlyingField.OriginalDefinition.IsDefinition);
                return (FieldSymbol)underlyingField.OriginalDefinition;
            }
        }

        Microsoft.Cci.ISpecializedFieldReference Microsoft.Cci.IFieldReference.AsSpecializedFieldReference
        {
            get
            {
                return this;
            }
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.IFieldReference.GetType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            var customModifiers = underlyingField.CustomModifiers;
            var type = ((PEModuleBuilder)context.Module).Translate(underlyingField.Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);

            if (customModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Microsoft.Cci.ModifiedTypeReference(type, customModifiers);
            }
        }

        Microsoft.Cci.IFieldDefinition Microsoft.Cci.IFieldReference.GetResolvedField(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        bool Microsoft.Cci.IFieldReference.IsContextualNamedEntity
        {
            get
            {
                return false;
            }
        }

    }
}
