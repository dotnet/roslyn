using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal abstract class NamedTypeDefinition : NamedTypeReference, Microsoft.Cci.INamedTypeDefinition
    {
        public NamedTypeDefinition(Module moduleBeingBuilt, NamedTypeSymbol underlyingNamedType)
            : base(moduleBeingBuilt, underlyingNamedType)
        {
            System.Diagnostics.Debug.Assert(underlyingNamedType is SourceNamedTypeSymbol);
        }

        ushort Microsoft.Cci.ITypeDefinition.Alignment
        {
            get { return 0; }
        }

        IEnumerable<Microsoft.Cci.ITypeReference> Microsoft.Cci.ITypeDefinition.BaseClasses
        {
            get 
            {
                NamedTypeSymbol baseType = UnderlyingNamedType.BaseType;

                if (baseType != null)
                {
                    yield return ModuleBeingBuilt.Translate(UnderlyingNamedType.BaseType, false);
                }
            }
        }

        IEnumerable<Microsoft.Cci.IEventDefinition> Microsoft.Cci.ITypeDefinition.Events
        {
            get { return Enumerable.Empty<Microsoft.Cci.IEventDefinition>(); }
        }

        IEnumerable<Microsoft.Cci.IMethodImplementation> Microsoft.Cci.ITypeDefinition.ExplicitImplementationOverrides
        {
            get { return Enumerable.Empty<Microsoft.Cci.IMethodImplementation>(); }
        }

        private IEnumerable<Microsoft.Cci.IFieldDefinition> fields;
        IEnumerable<Microsoft.Cci.IFieldDefinition> Microsoft.Cci.ITypeDefinition.Fields
        {
            get
            {
                if (fields == null)
                {
                    var tmp = new List<Microsoft.Cci.IFieldDefinition>();
                    foreach (var m in UnderlyingNamedType.GetMembers())
                    {
                        if (m.Kind == SymbolKind.Field)
                        {
                            tmp.Add((Microsoft.Cci.IFieldDefinition)ModuleBeingBuilt.Translate((FieldSymbol)m, true));
                        }
                    }
                    fields = tmp;
                }
                return fields;
            }
        }

        IEnumerable<Microsoft.Cci.IGenericTypeParameter> Microsoft.Cci.ITypeDefinition.GenericParameters
        {
            get
            {
                foreach (var t in UnderlyingNamedType.TypeParameters)
                {
                    yield return (Microsoft.Cci.IGenericTypeParameter)ModuleBeingBuilt.Translate(t);
                }
            }
        }

        ushort Microsoft.Cci.ITypeDefinition.GenericParameterCount
        {
            get { return (ushort)UnderlyingNamedType.Arity; }
        }

        bool Microsoft.Cci.ITypeDefinition.HasDeclarativeSecurity
        {
            get { return false; }
        }

        IEnumerable<Microsoft.Cci.ITypeReference> Microsoft.Cci.ITypeDefinition.Interfaces
        {
            get 
            {
                foreach (var i in UnderlyingNamedType.Interfaces)
                {
                    yield return ModuleBeingBuilt.Translate(i, false);
                }
            }
        }

        bool Microsoft.Cci.ITypeDefinition.IsAbstract
        {
            get { return UnderlyingNamedType.IsAbstract || UnderlyingNamedType.IsStatic; }
        }

        bool Microsoft.Cci.ITypeDefinition.IsBeforeFieldInit
        {
            get { return false; }
        }

        bool Microsoft.Cci.ITypeDefinition.IsComObject
        {
            get { return false; }
        }

        bool Microsoft.Cci.ITypeDefinition.IsGeneric
        {
            get { return UnderlyingNamedType.Arity != 0; }
        }

        bool Microsoft.Cci.ITypeDefinition.IsInterface
        {
            get { return UnderlyingNamedType.TypeKind == TypeKind.Interface; }
        }

        bool Microsoft.Cci.ITypeDefinition.IsRuntimeSpecial
        {
            get { return false; }
        }

        bool Microsoft.Cci.ITypeDefinition.IsSerializable
        {
            get { return false; }
        }

        bool Microsoft.Cci.ITypeDefinition.IsSpecialName
        {
            get { return false; }
        }

        bool Microsoft.Cci.ITypeDefinition.IsSealed
        {
            get { return UnderlyingNamedType.IsSealed || UnderlyingNamedType.IsStatic; }
        }

        Microsoft.Cci.LayoutKind Microsoft.Cci.ITypeDefinition.Layout
        {
            get { return (TypeKind.Struct == UnderlyingNamedType.TypeKind) ? Microsoft.Cci.LayoutKind.Sequential : Microsoft.Cci.LayoutKind.Auto; }
        }

        private IEnumerable<Microsoft.Cci.IMethodDefinition> methods; 
        IEnumerable<Microsoft.Cci.IMethodDefinition> Microsoft.Cci.ITypeDefinition.Methods
        {
            get
            {
                if (methods == null)
                {
                    List<Microsoft.Cci.IMethodDefinition> tmp = new List<Microsoft.Cci.IMethodDefinition>();
                    foreach (var m in UnderlyingNamedType.GetMembers())
                    {
                        if (m.Kind == SymbolKind.Method)
                        {
                            tmp.Add((Microsoft.Cci.IMethodDefinition)ModuleBeingBuilt.Translate((MethodSymbol)m, true));
                        }
                    }
                    methods = tmp;
                }
                return methods;
            }
        }

        IEnumerable<Microsoft.Cci.INestedTypeDefinition> Microsoft.Cci.ITypeDefinition.NestedTypes
        {
            get 
            {
                foreach (NamedTypeSymbol type in UnderlyingNamedType.GetTypeMembers())
                {
                    yield return (Microsoft.Cci.INestedTypeDefinition)ModuleBeingBuilt.Translate(type, true);
                }
            }
        }

        IEnumerable<Microsoft.Cci.ITypeDefinitionMember> Microsoft.Cci.ITypeDefinition.PrivateHelperMembers
        {
            // here's where we could go off to an auxiliary data structure for compiler-generated types.
            get { return Enumerable.Empty<Microsoft.Cci.ITypeDefinitionMember>(); }
        }

        IEnumerable<Microsoft.Cci.IPropertyDefinition> Microsoft.Cci.ITypeDefinition.Properties
        {
            get { return Enumerable.Empty<Microsoft.Cci.IPropertyDefinition>(); }
        }

        IEnumerable<Microsoft.Cci.ISecurityAttribute> Microsoft.Cci.ITypeDefinition.SecurityAttributes
        {
            get { throw new NotImplementedException(); }
        }

        uint Microsoft.Cci.ITypeDefinition.SizeOf
        {
            get 
            {
                // EDMAURER this needs a better calculation. Current implementation is there to satisfy MD rule that a struct
                // have either field defns or a non-zero size.
                return (TypeKind.Struct == UnderlyingNamedType.TypeKind &&
                    !UnderlyingNamedType.GetMembers().Any((m) => m.Kind == SymbolKind.Field)) ? 1U : 0;
            }
        }

        Microsoft.Cci.StringFormatKind Microsoft.Cci.ITypeDefinition.StringFormat
        {
            get { return Microsoft.Cci.StringFormatKind.Unspecified; }
        }

        public override uint TypeDefRowId
        {
            get { return 0; }
        }
    }
}
