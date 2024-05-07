// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal sealed class SignatureComparer
    {
        public static readonly SignatureComparer Instance = new(SymbolEquivalenceComparer.Instance);
        public static readonly SignatureComparer IgnoreAssembliesInstance = new(SymbolEquivalenceComparer.IgnoreAssembliesInstance);

        private readonly SymbolEquivalenceComparer _symbolEquivalenceComparer;

        private SignatureComparer(SymbolEquivalenceComparer symbolEquivalenceComparer)
            => _symbolEquivalenceComparer = symbolEquivalenceComparer;

        private IEqualityComparer<IParameterSymbol> ParameterEquivalenceComparer => _symbolEquivalenceComparer.ParameterEquivalenceComparer;

        private IEqualityComparer<ITypeSymbol> SignatureTypeEquivalenceComparer => _symbolEquivalenceComparer.SignatureTypeEquivalenceComparer;

        public bool HaveSameSignature(ISymbol symbol1, ISymbol symbol2, bool caseSensitive)
        {
            // NOTE - we're deliberately using reference equality here for speed.
            if (symbol1 == symbol2)
                return true;

            if (symbol1 == null || symbol2 == null)
                return false;

            if (symbol1.Kind != symbol2.Kind)
                return false;

            return symbol1.Kind switch
            {
                SymbolKind.Method => HaveSameSignature((IMethodSymbol)symbol1, (IMethodSymbol)symbol2, caseSensitive),
                SymbolKind.Property => HaveSameSignature((IPropertySymbol)symbol1, (IPropertySymbol)symbol2, caseSensitive),
                SymbolKind.Event => HaveSameSignature((IEventSymbol)symbol1, (IEventSymbol)symbol2, caseSensitive),
                _ => true,
            };
        }

        private static bool HaveSameSignature(IEventSymbol event1, IEventSymbol event2, bool caseSensitive)
            => IdentifiersMatch(event1.Name, event2.Name, caseSensitive);

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

        private static bool BadPropertyAccessor(IMethodSymbol method1, IMethodSymbol method2)
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
                    return false;
            }

            if (method1.MethodKind != method2.MethodKind ||
                method1.Arity != method2.Arity)
            {
                return false;
            }

            return HaveSameSignature(method1.Parameters, method2.Parameters, compareParameterName, isParameterCaseSensitive);
        }

        private static bool IdentifiersMatch(string identifier1, string identifier2, bool caseSensitive)
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
                return true;

            if (!HaveSameSignature(symbol1, symbol2, caseSensitive))
                return false;

            switch (symbol1.Kind)
            {
                case SymbolKind.Method:
                    var method1 = (IMethodSymbol)symbol1;
                    var method2 = (IMethodSymbol)symbol2;

                    return HaveSameSignatureAndConstraintsAndReturnType(method1, method2);
                case SymbolKind.Property:
                    var property1 = (IPropertySymbol)symbol1;
                    var property2 = (IPropertySymbol)symbol2;

                    return property1.ReturnsByRef == property2.ReturnsByRef &&
                           property1.ReturnsByRefReadonly == property2.ReturnsByRefReadonly &&
                           this.SignatureTypeEquivalenceComparer.Equals(property1.Type, property2.Type) &&
                           HaveSameAccessors(property1, property2);
                case SymbolKind.Event:
                    var ev1 = (IEventSymbol)symbol1;
                    var ev2 = (IEventSymbol)symbol2;

                    return HaveSameReturnType(ev1, ev2);
            }

            return true;
        }

        private static bool HaveSameAccessors(IPropertySymbol property1, IPropertySymbol property2)
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
            if (method1.ReturnsVoid != method2.ReturnsVoid ||
                method1.ReturnsByRef != method2.ReturnsByRef ||
                method1.ReturnsByRefReadonly != method2.ReturnsByRefReadonly)
            {
                return false;
            }

            if (!method1.ReturnsVoid && !this.SignatureTypeEquivalenceComparer.Equals(method1.ReturnType, method2.ReturnType))
                return false;

            for (var i = 0; i < method1.TypeParameters.Length; i++)
            {
                var typeParameter1 = method1.TypeParameters[i];
                var typeParameter2 = method2.TypeParameters[i];

                if (!HaveSameConstraints(typeParameter1, typeParameter2))
                    return false;
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

        private bool HaveSameReturnType(IEventSymbol ev1, IEventSymbol ev2)
            => this.SignatureTypeEquivalenceComparer.Equals(ev1.Type, ev2.Type);
    }
}
