//#if DEBUG
//#define GETSTATISTICS
//#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal partial class Module
    {
        private readonly Dictionary<Microsoft.Cci.IReference, uint> referencesInILMap = new Dictionary<Microsoft.Cci.IReference, uint>();
        private readonly List<Microsoft.Cci.IReference> referencesInIL = new List<Microsoft.Cci.IReference>();

        private readonly Dictionary<string, uint> stringsInILMap = new Dictionary<string, uint>();
        private readonly List<string> stringsInIL = new List<string>();

        // TODO: Need to estimate amount of elements for this map and pass that value to the constructor. 
        protected readonly Dictionary<Symbol, object> SymbolsMap = new Dictionary<Symbol, object>();
        private readonly Dictionary<Symbol, object> genericInstanceMap = new Dictionary<Symbol, object>();
        private readonly Dictionary<MethodSymbol, Microsoft.Cci.IMethodBody> methodBodyMap = new Dictionary<MethodSymbol, Microsoft.Cci.IMethodBody>();

#if GETSTATISTICS
        private int namedTypeLookups;
        private int namedTypeLookupMisses;

        private int fieldLookups;
        private int fieldLookupMisses;

        private int methodLookups;
        private int methodLookupMisses;
#endif

        public Microsoft.Cci.INamedTypeReference GetCorLibType(CorLibTypes.TypeId type)
        {
            return sourceModule.ContainingAssembly.GetCorLibType(type);
        }

        public uint GetFakeSymbolTokenForIL(Microsoft.Cci.IReference symbol)
        {
            uint result;

            if (!referencesInILMap.TryGetValue(symbol, out result))
            {
                result = (uint)referencesInIL.Count;
                referencesInILMap.Add(symbol, result);
                referencesInIL.Add(symbol);
            }

            return result;
        }

        public Microsoft.Cci.IReference GetReferenceFromToken(uint token)
        {
            return referencesInIL[(int)token];
        }

        public uint GetFakeStringTokenForIL(string str)
        {
            uint result;

            if (!stringsInILMap.TryGetValue(str, out result))
            {
                result = (uint)stringsInIL.Count;
                stringsInILMap.Add(str, result);
                stringsInIL.Add(str);
            }

            return result;
        }

        public string GetStringFromToken(uint token)
        {
            return stringsInIL[(int)token];
        }

        IEnumerable<Microsoft.Cci.IReference> Microsoft.Cci.IModule.ReferencesInIL(out int count)
        {
            count = referencesInIL.Count;
            return referencesInIL;
        }

        internal Microsoft.Cci.IAssemblyReference Translate(AssemblySymbol assembly)
        {
            if (ReferenceEquals(sourceModule.ContainingAssembly, assembly))
            {
                return (Microsoft.Cci.IAssemblyReference)this;
            }

            object reference;

            if (SymbolsMap.TryGetValue(assembly, out reference))
            {
                return (Microsoft.Cci.IAssemblyReference)reference;
            }

            AssemblyReference asmRef = new AssemblyReference(assembly);

            SymbolsMap.Add(assembly, asmRef);
            SymbolsMap.Add(assembly.Modules[0], asmRef);

            return asmRef;
        }

        internal Microsoft.Cci.IModuleReference Translate(ModuleSymbol module)
        {
            if (ReferenceEquals(sourceModule, module))
            {
                return this;
            }

            object reference;

            if (SymbolsMap.TryGetValue(module, out reference))
            {
                return (Microsoft.Cci.IModuleReference)reference;
            }

            Microsoft.Cci.IModuleReference moduleRef;
            AssemblySymbol container = module.ContainingAssembly;

            if (container != null && ReferenceEquals(container.Modules[0], module))
            {
                moduleRef = new AssemblyReference(container);
                SymbolsMap.Add(container, moduleRef);
            }
            else
            {
                moduleRef = new ModuleReference(this, module);
            }

            SymbolsMap.Add(module, moduleRef);

            return moduleRef;
        }

        internal Microsoft.Cci.INamedTypeReference Translate(NamedTypeSymbol namedTypeSymbol, bool needDeclaration)
        {
            System.Diagnostics.Debug.Assert(ReferenceEquals(namedTypeSymbol, namedTypeSymbol.OriginalDefinition) ||
                !namedTypeSymbol.Equals(namedTypeSymbol.OriginalDefinition));

            if (!ReferenceEquals(namedTypeSymbol, namedTypeSymbol.OriginalDefinition))
            {
                // generic instantiation for sure
                System.Diagnostics.Debug.Assert(!needDeclaration);

                return namedTypeSymbol;
            }
            else if (!needDeclaration)
            {
                object reference;
                Microsoft.Cci.INamedTypeReference typeRef;

                NamedTypeSymbol container = namedTypeSymbol.ContainingType;

                if (namedTypeSymbol.Arity > 0)
                {
                    if (genericInstanceMap.TryGetValue(namedTypeSymbol, out reference))
                    {
                        return (Microsoft.Cci.INamedTypeReference)reference;
                    }

                    if (container != null)
                    {
                        if (IsGenericType(container))
                        {
                            // Container is a generic instance too.
                            typeRef = new SpecializedGenericNestedTypeInstanceReference(namedTypeSymbol);
                        }
                        else
                        {
                            typeRef = new GenericNestedTypeInstanceReference(namedTypeSymbol);
                        }
                    }
                    else
                    {
                        typeRef = new GenericNamespaceTypeInstanceReference(namedTypeSymbol);
                    }

                    genericInstanceMap.Add(namedTypeSymbol, typeRef);

                    return typeRef;
                }
                else if (IsGenericType(container))
                {
                    System.Diagnostics.Debug.Assert(container != null);

                    if (genericInstanceMap.TryGetValue(namedTypeSymbol, out reference))
                    {
                        return (Microsoft.Cci.INamedTypeReference)reference;
                    }

                    typeRef = new SpecializedNestedTypeReference(namedTypeSymbol);

                    genericInstanceMap.Add(namedTypeSymbol, typeRef);

                    return typeRef;
                }
            }

            return namedTypeSymbol;
        }

        public static bool IsGenericType(NamedTypeSymbol toCheck)
        {
            while (toCheck != null)
            {
                if (toCheck.Arity > 0)
                {
                    return true;
                }

                toCheck = toCheck.ContainingType;
            }

            return false;
        }

        internal Microsoft.Cci.IGenericParameterReference Translate(TypeParameterSymbol param)
        {
            Contract.ThrowIfFalse(ReferenceEquals(param, param.OriginalDefinition));
            return param;
        }

        internal Microsoft.Cci.ITypeReference Translate(TypeSymbol typeSymbol)
        {
            return (Microsoft.Cci.ITypeReference)typeSymbol.Accept(this, false);
        }

        internal Microsoft.Cci.IFieldReference Translate(FieldSymbol fieldSymbol, bool needDeclaration)
        {
            System.Diagnostics.Debug.Assert(ReferenceEquals(fieldSymbol, fieldSymbol.OriginalDefinition) ||
                !fieldSymbol.Equals(fieldSymbol.OriginalDefinition));

            if (!ReferenceEquals(fieldSymbol, fieldSymbol.OriginalDefinition))
            {
                System.Diagnostics.Debug.Assert(!needDeclaration);

                return fieldSymbol;
            }
            else if (!needDeclaration && IsGenericType(fieldSymbol.ContainingType))
            {
                object reference;
                Microsoft.Cci.IFieldReference fieldRef;

                if (genericInstanceMap.TryGetValue(fieldSymbol, out reference))
                {
                    return (Microsoft.Cci.IFieldReference)reference;
                }

                fieldRef = new SpecializedFieldReference(fieldSymbol);

                genericInstanceMap.Add(fieldSymbol, fieldRef);

                return fieldRef;
            }


            return fieldSymbol;
        }

        public static Microsoft.Cci.TypeMemberVisibility MemberVisibility(Accessibility declaredAccessibility)
        {
            switch (declaredAccessibility)
            {
                case Accessibility.Private:
                    return Microsoft.Cci.TypeMemberVisibility.Private;
                case Accessibility.Public:
                    return Microsoft.Cci.TypeMemberVisibility.Public;
                case Accessibility.Internal:
                    return Microsoft.Cci.TypeMemberVisibility.Assembly;
                case Accessibility.Protected:
                    return Microsoft.Cci.TypeMemberVisibility.Family;
                case Accessibility.ProtectedAndInternal: // Not supported by language, but we should be able to import it.
                    return Microsoft.Cci.TypeMemberVisibility.FamilyAndAssembly;
                case Accessibility.ProtectedInternal:
                    return Microsoft.Cci.TypeMemberVisibility.FamilyOrAssembly;

                default:
                    throw new NotImplementedException();
            }
        }

        internal Microsoft.Cci.IMethodReference Translate(MethodSymbol methodSymbol, bool needDeclaration)
        {
            object reference;
            Microsoft.Cci.IMethodReference methodRef;
            NamedTypeSymbol container = methodSymbol.ContainingType;

            System.Diagnostics.Debug.Assert(ReferenceEquals(methodSymbol, methodSymbol.OriginalDefinition) ||
                !methodSymbol.Equals(methodSymbol.OriginalDefinition));

            if (!ReferenceEquals(methodSymbol.OriginalDefinition, methodSymbol))
            {
                System.Diagnostics.Debug.Assert(!needDeclaration);

                return methodSymbol;
            }
            else if (!needDeclaration)
            {
                bool methodIsGeneric = methodSymbol.IsGeneric;
                bool typeIsGeneric = IsGenericType(container);

                if (methodIsGeneric || typeIsGeneric)
                {
                    if (genericInstanceMap.TryGetValue(methodSymbol, out reference))
                    {
                        return (Microsoft.Cci.IMethodReference)reference;
                    }

                    if (methodIsGeneric)
                    {
                        if (typeIsGeneric)
                        {
                            // Specialized and generic instance at the same time.
                            throw new NotImplementedException();
                        }
                        else
                        {
                            methodRef = new GenericMethodInstanceReference(methodSymbol);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(typeIsGeneric);
                        methodRef = new SpecializedMethodReference(methodSymbol);
                    }

                    genericInstanceMap.Add(methodSymbol, methodRef);

                    return methodRef;
                }
            }

            return methodSymbol;
        }

        public void SetMethodBody(MethodSymbol methodSymbol, Microsoft.Cci.IMethodBody body)
        {
            Contract.ThrowIfFalse(methodSymbol.ContainingModule == this.SourceModule &&
                ReferenceEquals(methodSymbol, methodSymbol.OriginalDefinition));

            methodBodyMap.Add(methodSymbol, body);
        }

        public Microsoft.Cci.IMethodBody GetMethodBody(MethodSymbol methodSymbol)
        {
            System.Diagnostics.Debug.Assert(methodSymbol.ContainingModule == this.SourceModule &&
                ReferenceEquals(methodSymbol, methodSymbol.OriginalDefinition)); 

            Microsoft.Cci.IMethodBody body;

            if (methodBodyMap.TryGetValue(methodSymbol, out body))
            {
                return body;
            }

            return null;
        }

        internal Microsoft.Cci.IParameterTypeInformation Translate(ParameterSymbol param, bool needDeclaration)
        {
            System.Diagnostics.Debug.Assert(ReferenceEquals(param, param.OriginalDefinition) ||
                !param.Equals(param.OriginalDefinition));

            if (!ReferenceEquals(param, param.OriginalDefinition))
            {
                return param;
            }
            else if (!needDeclaration)
            {
                Symbol container = param.ContainingSymbol;
                bool containerIsGeneric = false;

                if (container.Kind == SymbolKind.Method)
                {
                    if (((MethodSymbol)container).IsGeneric)
                    {
                        containerIsGeneric = true;
                    }
                    else
                    {
                        containerIsGeneric = IsGenericType(container.ContainingType);
                    }
                }
                else
                {
                    containerIsGeneric = IsGenericType(container.ContainingType);
                }

                if (containerIsGeneric)
                {
                    object reference;
                    Microsoft.Cci.IParameterTypeInformation paramRef;

                    if (genericInstanceMap.TryGetValue(param, out reference))
                    {
                        return (Microsoft.Cci.IParameterTypeInformation)reference;
                    }

                    paramRef = new ParameterTypeInformation(param);

                    genericInstanceMap.Add(param, paramRef);

                    return paramRef;
                }
            }

            return param;
        }

        internal Microsoft.Cci.IReference Translate(Symbol symbol)
        {
            return symbol.Accept(this, false);
        }

        internal Microsoft.Cci.IArrayTypeReference Translate(ArrayTypeSymbol symbol)
        {
            return symbol;
        }

        internal Microsoft.Cci.IManagedPointerTypeReference Translate(RefTypeSymbol symbol)
        {
            return symbol;
        }

        internal Microsoft.Cci.IPointerTypeReference Translate(PointerTypeSymbol symbol)
        {
            return symbol;
        }

        public override Microsoft.Cci.IReference VisitArrayType(ArrayTypeSymbol symbol, bool a)
        {
            return Translate(symbol);
        }

        internal override Microsoft.Cci.IReference VisitDynamicType(DynamicTypeSymbol symbol, bool a)
        {
            throw new NotImplementedException();
        }

        public override Microsoft.Cci.IReference VisitNamedType(NamedTypeSymbol symbol, bool a)
        {
            return Translate(symbol, false);
        }

        public override Microsoft.Cci.IReference VisitPointerType(PointerTypeSymbol symbol, bool a)
        {
            return Translate(symbol);
        }

        public override Microsoft.Cci.IReference VisitRefType(RefTypeSymbol symbol, bool a)
        {
            return Translate(symbol);
        }

        public override Microsoft.Cci.IReference VisitTypeParameter(TypeParameterSymbol symbol, bool a)
        {
            return Translate(symbol);
        }

        public override Microsoft.Cci.IReference VisitMethod(MethodSymbol symbol, bool a)
        {
            return Translate(symbol, false);
        }

        public override Microsoft.Cci.IReference VisitField(FieldSymbol symbol, bool a)
        {
            return Translate(symbol, false);
        }
    }
}