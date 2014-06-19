using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp.Emit
{
    /// <summary>
    /// Special type &lt;Module&gt;
    /// </summary>
    internal class RootModuleType : Microsoft.Cci.INamespaceTypeDefinition
    {
        public uint TypeDefRowId
        {
            get { return 0; }
        }

        public Microsoft.Cci.ITypeDefinition ResolvedType
        {
            get { return this; }
        }

        public IEnumerable<Microsoft.Cci.ICustomAttribute> Attributes
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>(); }
        }

        public bool MangleName
        {
            get { return false; }
        }

        public string Name
        {
            get { return "<Module>"; }
        }

        public ushort Alignment
        {
            get { return 0; }
        }

        public IEnumerable<Microsoft.Cci.ITypeReference> GetBaseClasses(object moduleBeingBuilt)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ITypeReference>(); 
        }

        public IEnumerable<Microsoft.Cci.IEventDefinition> Events
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IEventDefinition>(); }
        }

        public IEnumerable<Microsoft.Cci.IMethodImplementation> ExplicitImplementationOverrides
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IMethodImplementation>(); }
        }

        public IEnumerable<Microsoft.Cci.IFieldDefinition> Fields
        {
            get
            {
                return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IFieldDefinition>();
            }
        }

        public bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        public IEnumerable<Microsoft.Cci.ITypeReference> Interfaces(object m)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ITypeReference>(); 
        }

        public bool IsAbstract
        {
            get { return false; }
        }

        public bool IsBeforeFieldInit
        {
            get { return true; }
        }

        public bool IsComObject
        {
            get { return false; }
        }

        public bool IsGeneric
        {
            get { return false; }
        }

        public bool IsInterface
        {
            get { return false; }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool IsSerializable
        {
            get { return false; }
        }

        public bool IsSpecialName
        {
            get { return false; }
        }

        public bool IsSealed
        {
            get { return false; }
        }

        public Microsoft.Cci.LayoutKind Layout
        {
            get { return Microsoft.Cci.LayoutKind.Auto; }
        }

        public IEnumerable<Microsoft.Cci.IMethodDefinition> Methods
        {
            get
            {
                return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IMethodDefinition>();
            }
        }

        public IEnumerable<Microsoft.Cci.INestedTypeDefinition> NestedTypes
        {
            get
            {
                return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.INestedTypeDefinition>();
            }
        }

        public IEnumerable<Microsoft.Cci.ITypeDefinitionMember> PrivateHelperMembers
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ITypeDefinitionMember>(); }
        }

        public IEnumerable<Microsoft.Cci.IPropertyDefinition> Properties
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IPropertyDefinition>(); }
        }

        public uint SizeOf
        {
            get { return 0; }
        }

        public Microsoft.Cci.StringFormatKind StringFormat
        {
            get { return Microsoft.Cci.StringFormatKind.Ansi; }
        }

        public bool IsPublic
        {
            get { return true; }
        }

        public bool IsNested
        {
            get { return false; }
        }

        IEnumerable<Microsoft.Cci.IGenericTypeParameter> Microsoft.Cci.ITypeDefinition.GenericParameters
        {
            get { throw new NotImplementedException();}
        }

        ushort Microsoft.Cci.ITypeDefinition.GenericParameterCount
        {
            get
            {
                return 0;
            }
        }

        IEnumerable<Microsoft.Cci.ISecurityAttribute> Microsoft.Cci.ITypeDefinition.SecurityAttributes
        {
            get { throw new NotImplementedException(); }
        }

        void Microsoft.Cci.IReference.Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            throw new NotImplementedException();
        }

        IEnumerable<Microsoft.Cci.ILocation> Microsoft.Cci.IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        bool Microsoft.Cci.ITypeReference.IsEnum
        {
            get { throw new NotImplementedException(); }
        }

        bool Microsoft.Cci.ITypeReference.IsValueType
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeReference.GetResolvedType(object m)
        {
            return this; 
        }

        Microsoft.Cci.PrimitiveTypeCode Microsoft.Cci.ITypeReference.TypeCode(object m)
        {
            throw new NotImplementedException(); 
        }

        ushort Microsoft.Cci.INamedTypeReference.GenericParameterCount
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.IUnitReference Microsoft.Cci.INamespaceTypeReference.GetUnit(object m)
        {
            throw new NotImplementedException(); 
        }

        string Microsoft.Cci.INamespaceTypeReference.NamespaceName
        {
            get
            {
                return string.Empty;
            }
        }

        Microsoft.Cci.IGenericMethodParameterReference Microsoft.Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.IGenericTypeInstanceReference Microsoft.Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.IGenericTypeParameterReference Microsoft.Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.INamespaceTypeDefinition Microsoft.Cci.ITypeReference.AsNamespaceTypeDefinition(object moduleBeingBuilt)
        {
            return this;
        }

        Microsoft.Cci.INamespaceTypeReference Microsoft.Cci.ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                return this;
            }
        }

        Microsoft.Cci.INestedTypeDefinition Microsoft.Cci.ITypeReference.AsNestedTypeDefinition(object moduleBeingBuilt)
        {
            return null;
        }

        Microsoft.Cci.INestedTypeReference Microsoft.Cci.ITypeReference.AsNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.ISpecializedNestedTypeReference Microsoft.Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeReference.AsTypeDefinition(object m)
        {
            return this;
        }

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition(object m)
        {
            return this;
        }
    }
}
