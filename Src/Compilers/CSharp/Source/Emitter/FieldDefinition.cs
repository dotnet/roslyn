using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal sealed class FieldDefinition : FieldReference, Microsoft.Cci.IFieldDefinition
    {
        public FieldDefinition(Module moduleBeingBuilt, FieldSymbol underlyingField)
            : base(moduleBeingBuilt, underlyingField)
        { }

        public override void Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IFieldDefinition)this);
        }

        Microsoft.Cci.IMetadataConstant Microsoft.Cci.IFieldDefinition.CompileTimeValue
        {
            get 
            {
                return null; 
            }
        }

        Microsoft.Cci.ISectionBlock Microsoft.Cci.IFieldDefinition.FieldMapping
        {
            get { throw new NotImplementedException(); }
        }

        bool Microsoft.Cci.IFieldDefinition.IsCompileTimeConstant
        {
            get { return false; }
        }

        bool Microsoft.Cci.IFieldDefinition.IsMapped
        {
            get { return false; }
        }

        bool Microsoft.Cci.IFieldDefinition.IsMarshalledExplicitly
        {
            get { return false; }
        }

        bool Microsoft.Cci.IFieldDefinition.IsNotSerialized
        {
            get { return false; }
        }

        bool Microsoft.Cci.IFieldDefinition.IsReadOnly
        {
            get 
            { 
                return UnderlyingField.IsReadOnly; 
            }
        }

        bool Microsoft.Cci.IFieldDefinition.IsRuntimeSpecial
        {
            get { return false; }
        }

        bool Microsoft.Cci.IFieldDefinition.IsSpecialName
        {
            get { return false; }
        }

        bool Microsoft.Cci.IFieldDefinition.IsStatic
        {
            get 
            {
                return UnderlyingField.IsStatic;
            }
        }

        Microsoft.Cci.IMarshallingInformation Microsoft.Cci.IFieldDefinition.MarshallingInformation
        {
            get { throw new NotImplementedException(); }
        }

        uint Microsoft.Cci.IFieldDefinition.Offset
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get 
            { 
                return (Microsoft.Cci.ITypeDefinition)ModuleBeingBuilt.Translate(UnderlyingField.ContainingType, true); 
            }
        }

        Microsoft.Cci.TypeMemberVisibility Microsoft.Cci.ITypeDefinitionMember.Visibility
        {
            get 
            {
                return Module.MemberVisibility(UnderlyingField.DeclaredAccessibility);
            }
        }

        Microsoft.Cci.IMetadataConstant Microsoft.Cci.IMetadataConstantContainer.Constant
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.INestedTypeDefinition Microsoft.Cci.ITypeDefinitionMember.AsNestedTypeDefinition
        {
            get { return null; }
        }
    }
}
