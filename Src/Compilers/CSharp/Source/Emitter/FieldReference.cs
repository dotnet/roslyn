using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal abstract class FieldReference : TypeMemberReference, Microsoft.Cci.IFieldReference
    {
        protected readonly FieldSymbol UnderlyingField;

        public FieldReference(Module moduleBeingBuilt, FieldSymbol underlyingField)
            : base(moduleBeingBuilt)
        {
            Contract.ThrowIfNull(underlyingField);

            this.UnderlyingField = underlyingField;
        }

        protected override Symbol UnderlyingSymbol
        {
            get 
            {
                return UnderlyingField;
            }
        }

        public override void Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IFieldReference)this);
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.IFieldReference.GetType(object m)
        {
            var customModifiers = UnderlyingField.CustomModifiers;

            if (customModifiers.Count == 0)
            {
                return ModuleBeingBuilt.Translate(UnderlyingField.Type);
            }
            else 
            {
                return new ModifiedTypeReference(ModuleBeingBuilt, UnderlyingField.Type, customModifiers); 
            }
        }

        Microsoft.Cci.IFieldDefinition Microsoft.Cci.IFieldReference.GetResolvedField(object m)
        {
            return null;
        }

        protected override Microsoft.Cci.IDefinition AsDefinition
        {
            get { return null; }
        }

        public virtual Microsoft.Cci.ISpecializedFieldReference AsSpecializedFieldReference
        {
            get
            {
                return null;
            }
        }

    }
}
