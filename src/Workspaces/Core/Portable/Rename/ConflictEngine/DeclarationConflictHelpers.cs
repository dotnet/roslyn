// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal static class DeclarationConflictHelpers
    {
        public static IEnumerable<Location> GetMembersWithConflictingSignatures(IMethodSymbol renamedMethod, bool trimOptionalParameters)
        {
            var potentiallyConfictingMethods =
                renamedMethod.ContainingType.GetMembers(renamedMethod.Name)
                                            .OfType<IMethodSymbol>()
                                            .Where(m => !m.Equals(renamedMethod) && m.Arity == renamedMethod.Arity);

            var signatureToConflictingMember = new Dictionary<List<ITypeSymbol>, IMethodSymbol>(new ConflictingSignatureComparer());

            foreach (var method in potentiallyConfictingMethods)
            {
                foreach (var signature in GetAllSignatures(method, trimOptionalParameters))
                {
                    signatureToConflictingMember[signature] = method;
                }
            }

            foreach (var signature in GetAllSignatures(renamedMethod, trimOptionalParameters))
            {
                IMethodSymbol conflictingSymbol;

                if (signatureToConflictingMember.TryGetValue(signature, out conflictingSymbol))
                {
                    if (!(conflictingSymbol.PartialDefinitionPart != null && conflictingSymbol.PartialDefinitionPart == renamedMethod) &&
                        !(conflictingSymbol.PartialImplementationPart != null && conflictingSymbol.PartialImplementationPart == renamedMethod))
                    {
                        foreach (var location in conflictingSymbol.Locations)
                        {
                            yield return location;
                        }
                    }
                }
            }
        }

        private sealed class ConflictingSignatureComparer : IEqualityComparer<List<ITypeSymbol>>
        {
            public bool Equals(List<ITypeSymbol> x, List<ITypeSymbol> y)
            {
                if (x.Count != y.Count)
                {
                    return false;
                }

                return x.SequenceEqual(y);
            }

            public int GetHashCode(List<ITypeSymbol> obj)
            {
                // This is a very simple GetHashCode implementation. Doing something "fancier" here
                // isn't really worth it, since to compute a proper hash we'd end up walking all the
                // ITypeSymbols anyways.
                return obj.Count;
            }
        }

        private static IEnumerable<List<ITypeSymbol>> GetAllSignatures(IMethodSymbol method, bool trimOptionalParameters)
        {
            // First we'll construct the full signature. This consists of the types of the
            // parameters, as well as the return type if it's a conversion operator
            var signature = new List<ITypeSymbol>();

            if (method.MethodKind == MethodKind.Conversion)
            {
                signature.Add(method.ReturnType);
            }

            foreach (var parameter in method.Parameters)
            {
                signature.Add(parameter.Type);
            }

            yield return signature;

            // In VB, a method effectively creates multiple signatures which are produced by
            // chopping off each of the optional parameters on the end, last to first, per 4.1.1 of
            // the spec.
            if (trimOptionalParameters)
            {
                for (int i = method.Parameters.Length - 1; i >= 0; i--)
                {
                    if (method.Parameters[i].IsOptional)
                    {
                        signature = signature.GetRange(0, signature.Count - 1);
                        yield return signature;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
