// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class SignatureComparer
    {
        public static readonly SignatureComparer Instance = new SignatureComparer(SymbolEquivalenceComparer.Instance);
        public static readonly SignatureComparer IgnoreAssembliesInstance = new SignatureComparer(SymbolEquivalenceComparer.IgnoreAssembliesInstance);

        private readonly SymbolEquivalenceComparer _symbolEquivalenceComparer;

        private SignatureComparer(SymbolEquivalenceComparer symbolEquivalenceComparer)
        {
            _symbolEquivalenceComparer = symbolEquivalenceComparer;
        }

        private IEqualityComparer<IParameterSymbol> ParameterEquivalenceComparer => _symbolEquivalenceComparer.ParameterEquivalenceComparer;

        private IEqualityComparer<ITypeSymbol> SignatureTypeEquivalenceComparer => _symbolEquivalenceComparer.SignatureTypeEquivalenceComparer;

        public bool HaveSameSignature(ISymbol symbol1, ISymbol symbol2, bool caseSensitive)
        {
            // NOTE - we're deliberately using reference equality here for speed.
            if (symbol1 == symbol2)
            {
                return true;
            }

            if (symbol1 == null || symbol2 == null)
            {
                return false;
            }

            if (symbol1.Kind != symbol2.Kind)
            {
                return false;
            }

            switch (symbol1.Kind)
            {
                case SymbolKind.Method:
                    return HaveSameSignature((IMethodSymbol)symbol1, (IMethodSymbol)symbol2, caseSensitive);
                case SymbolKind.Property:
                    return HaveSameSignature((IPropertySymbol)symbol1, (IPropertySymbol)symbol2, caseSensitive);
                case SymbolKind.Event:
                    return HaveSameSignature((IEventSymbol)symbol1, (IEventSymbol)symbol2, caseSensitive);
            }

            return true;
        }

        private bool HaveSameSignature(IEventSymbol event1, IEventSymbol event2, bool caseSensitive)
        {
            return IdentifiersMatch(event1.Name, event2.Name, caseSensitive);
        }

        public bool HaveSameSignature(IPropertySymbol property1, IPropertySymbol property2, bool caseSensitive)
        {
            if (!IdentifiersMatch(property1.Name, property2.Name, caseSensitive) ||
                property1.Parameters.Length != property2.Parameters.Length ||
                property1.IsIndexer != property2.IsIndexer)
            {
                return false;
            }

            return property1.Parameters.SequenceEqual(
                property2.Parameters,
                this.ParameterEquivalenceComparer);
        }

        private bool BadPropertyAccessor(IMethodSymbol method1, IMethodSymbol method2)
        {
            return method1 != null &&
                (method2 == null || method2.DeclaredAccessibility != Accessibility.Public);
        }

        public bool HaveSameSignature(IMethodSymbol method1,
            IMethodSymbol method2,
            bool caseSensitive,
            bool compareParameterName = false,
            bool isParameterCaseSensitive = false)
        {
            if ((method1.MethodKind == MethodKind.AnonymousFunction) !=
                (method2.MethodKind == MethodKind.AnonymousFunction))
            {
                return false;
            }

            if (method1.MethodKind != MethodKind.AnonymousFunction)
            {
                if (!IdentifiersMatch(method1.Name, method2.Name, caseSensitive))
                {
                    return false;
                }
            }

            if (method1.MethodKind != method2.MethodKind ||
                method1.Arity != method2.Arity)
            {
                return false;
            }

            return HaveSameSignature(method1.Parameters, method2.Parameters, compareParameterName, isParameterCaseSensitive);
        }

        private bool IdentifiersMatch(string identifier1, string identifier2, bool caseSensitive)
        {
            return caseSensitive
                ? identifier1 == identifier2
                : string.Equals(identifier1, identifier2, StringComparison.OrdinalIgnoreCase);
        }

        public bool HaveSameSignature(
            IList<IParameterSymbol> parameters1,
            IList<IParameterSymbol> parameters2)
        {
            if (parameters1.Count != parameters2.Count)
            {
                return false;
            }

            return parameters1.SequenceEqual(parameters2, this.ParameterEquivalenceComparer);
        }

        public bool HaveSameSignature(
            IList<IParameterSymbol> parameters1,
            IList<IParameterSymbol> parameters2,
            bool compareParameterName,
            bool isCaseSensitive)
        {
            if (parameters1.Count != parameters2.Count)
            {
                return false;
            }

            for (var i = 0; i < parameters1.Count; ++i)
            {
                if (!_symbolEquivalenceComparer.ParameterEquivalenceComparer.Equals(parameters1[i], parameters2[i], compareParameterName, isCaseSensitive))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(ISymbol symbol1, ISymbol symbol2, bool caseSensitive)
        {
            // NOTE - we're deliberately using reference equality here for speed.
            if (symbol1 == symbol2)
            {
                return true;
            }

            if (!HaveSameSignature(symbol1, symbol2, caseSensitive))
            {
                return false;
            }

            switch (symbol1.Kind)
            {
                case SymbolKind.Method:
                    var method1 = (IMethodSymbol)symbol1;
                    var method2 = (IMethodSymbol)symbol2;

                    return HaveSameSignatureAndConstraintsAndReturnType(method1, method2);
                case SymbolKind.Property:
                    var property1 = (IPropertySymbol)symbol1;
                    var property2 = (IPropertySymbol)symbol2;

                    return HaveSameReturnType(property1, property2) && HaveSameAccessors(property1, property2);
                case SymbolKind.Event:
                    var ev1 = (IEventSymbol)symbol1;
                    var ev2 = (IEventSymbol)symbol2;

                    return HaveSameReturnType(ev1, ev2);
            }

            return true;
        }

        private bool HaveSameAccessors(IPropertySymbol property1, IPropertySymbol property2)
        {
            if (property1.ContainingType == null ||
                property1.ContainingType.TypeKind == TypeKind.Interface)
            {
                if (BadPropertyAccessor(property1.GetMethod, property2.GetMethod) ||
                    BadPropertyAccessor(property1.SetMethod, property2.SetMethod))
                {
                    return false;
                }
            }

            if (property2.ContainingType == null ||
                property2.ContainingType.TypeKind == TypeKind.Interface)
            {
                if (BadPropertyAccessor(property2.GetMethod, property1.GetMethod) ||
                    BadPropertyAccessor(property2.SetMethod, property1.SetMethod))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HaveSameSignatureAndConstraintsAndReturnType(IMethodSymbol method1, IMethodSymbol method2)
        {
            if (method1.ReturnsVoid != method2.ReturnsVoid)
            {
                return false;
            }

            if (!method1.ReturnsVoid && !this.SignatureTypeEquivalenceComparer.Equals(method1.ReturnType, method2.ReturnType))
            {
                return false;
            }

            for (var i = 0; i < method1.TypeParameters.Length; i++)
            {
                var typeParameter1 = method1.TypeParameters[i];
                var typeParameter2 = method2.TypeParameters[i];

                if (!HaveSameConstraints(typeParameter1, typeParameter2))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HaveSameConstraints(ITypeParameterSymbol typeParameter1, ITypeParameterSymbol typeParameter2)
        {
            if (typeParameter1.HasConstructorConstraint != typeParameter2.HasConstructorConstraint ||
                typeParameter1.HasReferenceTypeConstraint != typeParameter2.HasReferenceTypeConstraint ||
                typeParameter1.HasValueTypeConstraint != typeParameter2.HasValueTypeConstraint)
            {
                return false;
            }

            if (typeParameter1.ConstraintTypes.Length != typeParameter2.ConstraintTypes.Length)
            {
                return false;
            }

            return typeParameter1.ConstraintTypes.SetEquals(
                typeParameter2.ConstraintTypes, this.SignatureTypeEquivalenceComparer);
        }

        private bool HaveSameReturnType(IPropertySymbol property1, IPropertySymbol property2)
        {
            return this.SignatureTypeEquivalenceComparer.Equals(property1.Type, property2.Type);
        }

        private bool HaveSameReturnType(IEventSymbol ev1, IEventSymbol ev2)
        {
            return this.SignatureTypeEquivalenceComparer.Equals(ev1.Type, ev2.Type);
        }
    }
}
