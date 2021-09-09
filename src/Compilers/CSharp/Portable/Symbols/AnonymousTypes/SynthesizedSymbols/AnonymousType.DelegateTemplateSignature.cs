// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        // PROTOTYPE: Do we need this signature type or is AnonymousTypeDescriptor sufficient?
        internal sealed class AnonymousDelegateTemplateSignature : IEquatable<AnonymousDelegateTemplateSignature>
        {
            internal readonly int TypeParameterCount;
            internal readonly ImmutableArray<AnonymousDelegateParameterOrReturn> ParametersAndReturn;
            internal readonly Location Location;

            internal static AnonymousDelegateTemplateSignature FromTypeDescriptor(ImmutableArray<TypeParameterSymbol> typeParameters, AnonymousTypeDescriptor typeDescr)
            {
                int typeParameterCount = typeParameters.Length;
                if (typeParameterCount > 0)
                {
                    var typeMap = new TypeMap(typeParameters, IndexedTypeParameterSymbol.Take(typeParameterCount), allowAlpha: true);
                    typeDescr = typeDescr.SubstituteTypes(typeMap, out _);
                }

                Debug.Assert(typeDescr.Fields.All(f => f.Type.VisitType((t, _, _) => t.IsTypeParameter() && t is not IndexedTypeParameterSymbol, arg: (object?)null) is null));

                // PROTOTYPE: If signature is independent of the original containing symbol, the signature should depend on the constraints of the type parameters.
                return new AnonymousDelegateTemplateSignature(typeParameterCount, typeDescr.Fields.SelectAsArray(static f => new AnonymousDelegateParameterOrReturn(f.RefKind, f.TypeWithAnnotations)), typeDescr.Location);
            }

            private AnonymousDelegateTemplateSignature(int typeParameterCount, ImmutableArray<AnonymousDelegateParameterOrReturn> parametersAndReturn, Location location)
            {
                TypeParameterCount = typeParameterCount;
                ParametersAndReturn = parametersAndReturn;
                Location = location;
            }

            /// <summary>
            /// True if any of the parameter types or return type are specific
            /// types rather than type parameters.
            /// </summary>
            internal bool ContainsFixedParameterTypeOrReturnType()
            {
                int n = ParametersAndReturn.Length;
                for (int i = 0; i < n - 1; i++)
                {
                    if (!isValidTypeArgument(ParametersAndReturn[i]))
                    {
                        return true;
                    }
                }
                var returnParameter = ParametersAndReturn[n - 1];
                return !returnParameter.Type.Type.IsVoidType() && !isValidTypeArgument(returnParameter);

                static bool isValidTypeArgument(AnonymousDelegateParameterOrReturn parameterOrReturn)
                {
                    return parameterOrReturn.Type.Type is { } type &&
                        !type.IsPointerOrFunctionPointer() &&
                        !type.IsRestrictedType();
                }
            }

            public bool Equals(AnonymousDelegateTemplateSignature? other)
            {
                return Equals(other, TypeCompareKind.ConsiderEverything);
            }

            public bool Equals(AnonymousDelegateTemplateSignature? other, TypeCompareKind comparison)
            {
                return other is { } &&
                    TypeParameterCount == other.TypeParameterCount &&
                    ParametersAndReturn.SequenceEqual(other.ParametersAndReturn, comparison, static (x, y, comparison) => x.Equals(y, comparison));
            }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as AnonymousDelegateTemplateSignature);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(
                    TypeParameterCount.GetHashCode(),
                    Hash.CombineValues(ParametersAndReturn));
            }
        }

        internal readonly struct AnonymousDelegateParameterOrReturn
        {
            internal readonly RefKind RefKind;
            internal readonly TypeWithAnnotations Type;

            internal AnonymousDelegateParameterOrReturn(RefKind refKind, TypeWithAnnotations type)
            {
                RefKind = refKind;
                Type = type;
            }

            public bool Equals(AnonymousDelegateParameterOrReturn other, TypeCompareKind comparison)
            {
                return RefKind == other.RefKind &&
                    Type.Equals(other.Type, comparison);
            }

            public override bool Equals(object? obj)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override int GetHashCode()
            {
                return Type.GetHashCode();
            }
        }
    }
}
