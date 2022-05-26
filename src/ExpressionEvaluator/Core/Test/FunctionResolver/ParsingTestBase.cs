// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    public abstract class ParsingTestBase : CSharpTestBase
    {
        internal static RequestSignature SignatureNameOnly(Name name)
        {
            return new RequestSignature(name, default(ImmutableArray<ParameterSignature>));
        }

        internal static RequestSignature Signature(Name name)
        {
            return new RequestSignature(name, ImmutableArray<ParameterSignature>.Empty);
        }

        internal static RequestSignature Signature(Name name, params TypeSignature[] parameterTypes)
        {
            return Signature(name, parameterTypes.Select(t => new ParameterSignature(t, isByRef: false)).ToArray());
        }

        internal static RequestSignature Signature(Name name, params ParameterSignature[] parameters)
        {
            return new RequestSignature(name, ImmutableArray.CreateRange(parameters));
        }

        internal static QualifiedName Name(string name)
        {
            return new QualifiedName(null, name);
        }

        internal static GenericName Generic(QualifiedName name, params string[] typeArguments)
        {
            Assert.True(typeArguments.Length > 0);
            return new GenericName(name, ImmutableArray.CreateRange(typeArguments));
        }

        internal static QualifiedName Qualified(Name left, string right)
        {
            return new QualifiedName(left, right);
        }

        internal static QualifiedTypeSignature Identifier(string name)
        {
            return new QualifiedTypeSignature(null, name);
        }

        internal static GenericTypeSignature Generic(QualifiedTypeSignature name, params TypeSignature[] typeArguments)
        {
            Assert.True(typeArguments.Length > 0);
            return new GenericTypeSignature(name, ImmutableArray.CreateRange(typeArguments));
        }

        internal static QualifiedTypeSignature Qualified(TypeSignature left, string right)
        {
            return new QualifiedTypeSignature(left, right);
        }

        internal static QualifiedTypeSignature Qualified(params string[] names)
        {
            QualifiedTypeSignature signature = null;
            foreach (var name in names)
            {
                signature = new QualifiedTypeSignature(signature, name);
            }
            return signature;
        }

        internal static ArrayTypeSignature Array(TypeSignature elementType, int rank)
        {
            return new ArrayTypeSignature(elementType, rank);
        }

        internal static PointerTypeSignature Pointer(TypeSignature pointedAtType)
        {
            return new PointerTypeSignature(pointedAtType);
        }

        internal static void VerifySignature(RequestSignature actualSignature, RequestSignature expectedSignature)
        {
            if (expectedSignature == null)
            {
                Assert.Null(actualSignature);
            }
            else
            {
                Assert.NotNull(actualSignature);
                Assert.Equal(expectedSignature.MemberName, actualSignature.MemberName, NameComparer.Instance);
                if (expectedSignature.Parameters.IsDefault)
                {
                    Assert.True(actualSignature.Parameters.IsDefault);
                }
                else
                {
                    AssertEx.Equal(
                        expectedSignature.Parameters,
                        actualSignature.Parameters,
                        comparer: ParameterComparer.Instance,
                        itemInspector: p => p.Type.GetDebuggerDisplay());
                }
            }
        }
    }
}
