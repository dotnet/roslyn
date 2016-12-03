// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    public class CSharpParsingTests : CSharpTestBase
    {
        [Fact]
        public void Parsing()
        {
            // Method name only.
            VerifySignature("F",
                SignatureNameOnly(
                    Name("F")));
            // Method name and empty parameters.
            VerifySignature("F()",
                Signature(
                    Name("F")));
            // Method name and parameters.
            VerifySignature("F(A, B)",
                Signature(
                    Name("F"),
                    Identifier("A"),
                    Identifier("B")));
            // Type and method name.
            VerifySignature("C.F",
                SignatureNameOnly(
                    Qualified(Name("C"), "F")));
            // Qualified type and method name.
            VerifySignature("A.B.F",
                SignatureNameOnly(
                    Qualified(
                        Qualified(
                            Name("A"),
                            "B"),
                        "F")));
            // Generic types and method names.
            VerifySignature("A<T>.B<U>.F<V>",
                SignatureNameOnly(
                    Generic(
                        Qualified(
                            Generic(
                                Qualified(
                                    Generic(
                                        Name("A"),
                                        "T"),
                                    "B"),
                                "U"),
                            "F"),
                        "V")));
        }

        [Fact]
        public void Spaces()
        {
            VerifySignature(" \tC . F ( System.Object\t,object) ",
                Signature(
                    Qualified(Name("C"), "F"),
                    Qualified("System", "Object"),
                    Qualified("System", "Object")));
        }

        [Fact]
        public void ArraysAndPointers()
        {
            VerifySignature("F(C*)",
                Signature(
                    Name("F"),
                    Pointer(
                        Identifier("C"))));
            VerifySignature("F(C**)",
                Signature(
                    Name("F"),
                    Pointer(
                        Pointer(
                            Identifier("C")))));
            VerifySignature("F(C*[,,,])",
                Signature(
                    Name("F"),
                    Array(
                        Pointer(
                            Identifier("C")),
                        4)));
            VerifySignature("F(C[,][]*)",
                Signature(
                    Name("F"),
                    Pointer(
                        Array(
                            Array(
                                Identifier("C"),
                                2),
                            1))));
            VerifySignature("F(C<T>*)",
                Signature(
                    Name("F"),
                    Pointer(
                        Generic(
                            Identifier("C"),
                            Identifier("T")))));
            VerifySignature("F(C<T[,]*>)",
                Signature(
                    Name("F"),
                    Generic(
                        Identifier("C"),
                        Pointer(
                            Array(
                                Identifier("T"),
                                2)))));
        }

        [Fact]
        public void ParseErrors()
        {
            Assert.Null(MemberSignatureParser.Parse("A<T>B"));
            Assert.Null(MemberSignatureParser.Parse("A<<T>>"));
            Assert.Null(MemberSignatureParser.Parse("A<T><U>"));
            Assert.Null(MemberSignatureParser.Parse("A.<T>"));
            Assert.Null(MemberSignatureParser.Parse("A<T>.<U>"));
            Assert.Null(MemberSignatureParser.Parse("A+B"));
            Assert.Null(MemberSignatureParser.Parse("F("));
            Assert.Null(MemberSignatureParser.Parse("F())"));
            Assert.Null(MemberSignatureParser.Parse("F(]"));
            Assert.Null(MemberSignatureParser.Parse("F(,B)"));
            Assert.Null(MemberSignatureParser.Parse("F(A,)"));
            Assert.Null(MemberSignatureParser.Parse("F<"));
            Assert.Null(MemberSignatureParser.Parse("F<()"));
            Assert.Null(MemberSignatureParser.Parse("F<T>>"));
            Assert.Null(MemberSignatureParser.Parse("F<T()"));
            Assert.Null(MemberSignatureParser.Parse("F<T()"));
            Assert.Null(MemberSignatureParser.Parse("F?"));
            Assert.Null(MemberSignatureParser.Parse("F[]"));
            Assert.Null(MemberSignatureParser.Parse("F*"));
            Assert.Null(MemberSignatureParser.Parse(".F"));
            Assert.Null(MemberSignatureParser.Parse("()"));
            Assert.Null(MemberSignatureParser.Parse("<T>"));
            Assert.Null(MemberSignatureParser.Parse("1"));
        }

        [Fact]
        public void ByRef()
        {
            VerifySignature("F(ref A, out B)",
                Signature(
                    Name("F"),
                    Identifier("A"),
                    Identifier("B")));
            Assert.Null(MemberSignatureParser.Parse("F(ref out C)"));
            Assert.Null(MemberSignatureParser.Parse("F(ref)"));
            Assert.Null(MemberSignatureParser.Parse("F<out>"));
            Assert.Null(MemberSignatureParser.Parse("F<out C>"));
            Assert.Null(MemberSignatureParser.Parse("F(C<ref>)"));
            Assert.Null(MemberSignatureParser.Parse("F(C<ref C>)"));
        }

        // Special types are treated as keywords in names,
        // but not recognized as special types.
        [Fact]
        public void SpecialTypes_Names()
        {
            // Method name only.
            Assert.Null(MemberSignatureParser.Parse("int"));
            Assert.Null(MemberSignatureParser.Parse("params"));
            VerifySignature("@int",
                SignatureNameOnly(
                    Name("int")));
            // Type and method name.
            Assert.Null(MemberSignatureParser.Parse("@object.int"));
            Assert.Null(MemberSignatureParser.Parse("@public.private"));
            VerifySignature("@object.@int",
                SignatureNameOnly(
                    Qualified(
                        Name("object"),
                        "int")));
            // Type parameters.
            Assert.Null(MemberSignatureParser.Parse("F<void>"));
            Assert.Null(MemberSignatureParser.Parse("F<bool>"));
            Assert.Null(MemberSignatureParser.Parse("F<char>"));
            Assert.Null(MemberSignatureParser.Parse("F<sbyte>"));
            Assert.Null(MemberSignatureParser.Parse("F<byte>"));
            Assert.Null(MemberSignatureParser.Parse("F<short>"));
            Assert.Null(MemberSignatureParser.Parse("F<ushort>"));
            VerifySignature("F<@void, @bool, @char, @sbyte, @byte, @short, @ushort, @int, @uint, @long, @ulong, @float, @double, @string, @object, @decimal>()",
                Signature(
                    Generic(Name("F"),
                    "void",
                    "bool",
                    "char",
                    "sbyte",
                    "byte",
                    "short",
                    "ushort",
                    "int",
                    "uint",
                    "long",
                    "ulong",
                    "float",
                    "double",
                    "string",
                    "object",
                    "decimal")));
        }

        // Special types are recognized in type references.
        [Fact]
        public void SpecialTypes_TypeReferences()
        {
            // Parameters.
            VerifySignature("F(void, bool, char, sbyte, byte, short, ushort, int, uint, long, ulong, float, double, string, object, decimal)",
                Signature(
                    Name("F"),
                    Qualified("System", "Void"),
                    Qualified("System", "Boolean"),
                    Qualified("System", "Char"),
                    Qualified("System", "SByte"),
                    Qualified("System", "Byte"),
                    Qualified("System", "Int16"),
                    Qualified("System", "UInt16"),
                    Qualified("System", "Int32"),
                    Qualified("System", "UInt32"),
                    Qualified("System", "Int64"),
                    Qualified("System", "UInt64"),
                    Qualified("System", "Single"),
                    Qualified("System", "Double"),
                    Qualified("System", "String"),
                    Qualified("System", "Object"),
                    Qualified("System", "Decimal")));
            // Type arguments.
            VerifySignature("F(C<void, int, string, object>)",
                Signature(
                    Name("F"),
                    Generic(
                        Identifier("C"),
                        Qualified("System", "Void"),
                        Qualified("System", "Int32"),
                        Qualified("System", "String"),
                        Qualified("System", "Object"))));
            // Not special types.
            VerifySignature("F(Void, Int32, @string, @object)",
                Signature(
                    Name("F"),
                    Identifier("Void"),
                    Identifier("Int32"),
                    Identifier("string"),
                    Identifier("object")));
            // dynamic is not special.
            VerifySignature("F(dynamic)",
                Signature(
                    Name("F"),
                    Identifier("dynamic")));
        }

        [Fact]
        public void EscapedNames()
        {
            VerifySignature("@F",
                SignatureNameOnly(
                    Name("F")));
            VerifySignature("@_",
                SignatureNameOnly(
                    Name("_")));
            VerifySignature("@int",
                SignatureNameOnly(
                    Name("int")));
            VerifySignature("A.B.@int",
                SignatureNameOnly(
                    Qualified(
                        Qualified(
                            Name("A"),
                            "B"),
                        "int")));
            VerifySignature("F(@int)",
                Signature(
                    Name("F"),
                    Identifier("int")));
            VerifySignature("F(System.@int)",
                Signature(
                    Name("F"),
                    Qualified(
                        Identifier("System"),
                        "int")));
            VerifySignature("A<@object>.B<@int>.F<@void>",
                SignatureNameOnly(
                    Generic(
                        Qualified(
                            Generic(
                                Qualified(
                                    Generic(
                                        Name("A"),
                                        "object"),
                                    "B"),
                                "int"),
                            "F"),
                        "void")));
            VerifySignature("F(C<int, @void>)",
                Signature(
                    Name("F"),
                    Generic(
                        Identifier("C"),
                        Qualified("System", "Int32"),
                        Identifier("void"))));
            Assert.Null(MemberSignatureParser.Parse("@"));
            Assert.Null(MemberSignatureParser.Parse("@1"));
            Assert.Null(MemberSignatureParser.Parse("@@F"));
            Assert.Null(MemberSignatureParser.Parse("@F@"));
            Assert.Null(MemberSignatureParser.Parse("@<T>"));
            Assert.Null(MemberSignatureParser.Parse("@.F"));
            Assert.Null(MemberSignatureParser.Parse("@()"));
            Assert.Null(MemberSignatureParser.Parse("F(@)"));
            Assert.Null(MemberSignatureParser.Parse("F(A, @)"));
            Assert.Null(MemberSignatureParser.Parse("F<@>"));
            Assert.Null(MemberSignatureParser.Parse("F<T, @>"));
        }

        private static RequestSignature SignatureNameOnly(Name name)
        {
            return new RequestSignature(name, default(ImmutableArray<ParameterSignature>));
        }

        private static RequestSignature Signature(Name name)
        {
            return new RequestSignature(name, ImmutableArray<ParameterSignature>.Empty);
        }

        private static RequestSignature Signature(Name name, params TypeSignature[] parameterTypes)
        {
            return Signature(name, parameterTypes.Select(t => new ParameterSignature(t, isByRef: false)).ToArray());
        }

        private static RequestSignature Signature(Name name, params ParameterSignature[] parameters)
        {
            return new RequestSignature(name, ImmutableArray.CreateRange(parameters));
        }

        private static QualifiedName Name(string name)
        {
            return new QualifiedName(null, name);
        }

        private static GenericName Generic(QualifiedName name, params string[] typeArguments)
        {
            Assert.True(typeArguments.Length > 0);
            return new GenericName(name, ImmutableArray.CreateRange(typeArguments));
        }

        private static QualifiedName Qualified(Name left, string right)
        {
            return new QualifiedName(left, right);
        }

        private static QualifiedTypeSignature Identifier(string name)
        {
            return new QualifiedTypeSignature(null, name);
        }

        private static GenericTypeSignature Generic(QualifiedTypeSignature name, params TypeSignature[] typeArguments)
        {
            Assert.True(typeArguments.Length > 0);
            return new GenericTypeSignature(name, ImmutableArray.CreateRange(typeArguments));
        }

        private static QualifiedTypeSignature Qualified(TypeSignature left, string right)
        {
            return new QualifiedTypeSignature(left, right);
        }

        private static QualifiedTypeSignature Qualified(params string[] names)
        {
            QualifiedTypeSignature signature = null;
            foreach (var name in names)
            {
                signature = new QualifiedTypeSignature(signature, name);
            }
            return signature;
        }

        private static ArrayTypeSignature Array(TypeSignature elementType, int rank)
        {
            return new ArrayTypeSignature(elementType, rank);
        }

        private static PointerTypeSignature Pointer(TypeSignature pointedAtType)
        {
            return new PointerTypeSignature(pointedAtType);
        }

        private static void VerifySignature(string str, RequestSignature expectedSignature)
        {
            var actualSignature = MemberSignatureParser.Parse(str);
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
