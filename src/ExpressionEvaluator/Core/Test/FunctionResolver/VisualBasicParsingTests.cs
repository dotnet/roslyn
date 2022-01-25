// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    public class VisualBasicParsingTests : ParsingTestBase
    {
        // Verify the set of keywords in the parser matches the VB compiler.
        [Fact]
        public void Keywords()
        {
            var builder = ImmutableHashSet.CreateBuilder<SyntaxKind>();
            // SyntaxFacts.GetReservedKeywordKinds() contains ReferenceKeyword
            // although "Reference" should not be considered a keyword.
            // (see https://github.com/dotnet/roslyn/issues/15242).
            builder.Add(SyntaxKind.ReferenceKeyword);
            foreach (var text in MemberSignatureParser.Keywords)
            {
                var kind = SyntaxFacts.GetKeywordKind(text);
                Assert.NotEqual(SyntaxKind.None, kind);
                bool added = builder.Add(kind);
                Assert.True(added);
            }

            var actualKeywordKinds = builder.ToImmutable();
            var expectedKeywordKinds = ImmutableHashSet.CreateRange(SyntaxFacts.GetReservedKeywordKinds());
            AssertEx.SetEqual(actualKeywordKinds, expectedKeywordKinds);
        }

        // Verify the set of keywords used in the parser are valid.
        [Fact]
        public void KeywordKinds()
        {
            // Verify the labels are consistent with the VB compiler.
            foreach (var pair in MemberSignatureParser.KeywordKinds)
            {
                var expectedKind = SyntaxFacts.GetKeywordKind(pair.Key).ToString();
                var actualKind = pair.Value.ToString();
                Assert.Equal(expectedKind, actualKind);
            }

            // Verify all values are also in Keywords.
            foreach (var keyword in MemberSignatureParser.KeywordKinds.Keys)
            {
                Assert.True(MemberSignatureParser.Keywords.Contains(keyword));
            }

            // Verify all values of SyntaxKind are recognized.
            const string keywordSuffix = "Keyword";
            foreach (var value in typeof(MemberSignatureParser.SyntaxKind).GetEnumValues())
            {
                var kind = (MemberSignatureParser.SyntaxKind)value;
                if (kind == MemberSignatureParser.SyntaxKind.None)
                {
                    continue;
                }
                var pair = MemberSignatureParser.KeywordKinds.First(p => p.Value == kind);
                var kindText = kind.ToString();
                Assert.EndsWith(keywordSuffix, kindText);
                var expectedText = kindText.Substring(0, kindText.Length - keywordSuffix.Length);
                Assert.Equal(expectedText, pair.Key, StringComparer.OrdinalIgnoreCase);
            }
        }

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
            VerifySignature("A(Of T).B(Of U).F(Of V)",
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
        public void Arrays()
        {
            VerifySignature("F(C(,,,))",
                Signature(
                    Name("F"),
                    Array(
                        Identifier("C"),
                        4)));
            VerifySignature("F(C(,)())",
                Signature(
                    Name("F"),
                    Array(
                        Array(
                            Identifier("C"),
                            2),
                        1)));
            VerifySignature("F(C(Of T(,)))",
                Signature(
                    Name("F"),
                    Generic(
                        Identifier("C"),
                        Array(
                            Identifier("T"),
                            2))));
        }

        [Fact]
        public void ParseErrors()
        {
            Assert.Null(MemberSignatureParser.Parse("A(Of)"));
            Assert.Null(MemberSignatureParser.Parse("A(Of Of)"));
            Assert.Null(MemberSignatureParser.Parse("A(Of T)B"));
            Assert.Null(MemberSignatureParser.Parse("A(Of (Of T))"));
            Assert.Null(MemberSignatureParser.Parse("A(Of T)(Of U)"));
            Assert.Null(MemberSignatureParser.Parse("A(Of T, Of U)"));
            Assert.Null(MemberSignatureParser.Parse("A.(Of T)"));
            Assert.Null(MemberSignatureParser.Parse("A(Of T).(Of U)"));
            Assert.Null(MemberSignatureParser.Parse("A+B"));
            Assert.Null(MemberSignatureParser.Parse("F("));
            Assert.Null(MemberSignatureParser.Parse("F())"));
            Assert.Null(MemberSignatureParser.Parse("F(]"));
            Assert.Null(MemberSignatureParser.Parse("F(,B)"));
            Assert.Null(MemberSignatureParser.Parse("F(A,)"));
            Assert.Null(MemberSignatureParser.Parse("F(Of "));
            Assert.Null(MemberSignatureParser.Parse("F(Of ()"));
            Assert.Null(MemberSignatureParser.Parse("F(Of T))"));
            Assert.Null(MemberSignatureParser.Parse("F(Of T()"));
            Assert.Null(MemberSignatureParser.Parse("F(Of T()"));
            Assert.Null(MemberSignatureParser.Parse("F?"));
            Assert.Null(MemberSignatureParser.Parse("F[]"));
            Assert.Null(MemberSignatureParser.Parse("F*"));
            Assert.Null(MemberSignatureParser.Parse(".F"));
            Assert.Null(MemberSignatureParser.Parse("()"));
            Assert.Null(MemberSignatureParser.Parse("(Of T)"));
            Assert.Null(MemberSignatureParser.Parse("1"));
            Assert.Null(MemberSignatureParser.Parse("F(C*)"));
            Assert.Null(MemberSignatureParser.Parse("F(C[])"));
            Assert.Null(MemberSignatureParser.Parse("global:C.F"));
        }

        [Fact]
        public void ByRef()
        {
            VerifySignature("F(ByVal A, ByRef B)",
                Signature(
                    Name("F"),
                    Identifier("A"),
                    Identifier("B")));
            Assert.Null(MemberSignatureParser.Parse("F(ByVal, B)"));
            Assert.Null(MemberSignatureParser.Parse("F(A, ByRef)"));
            Assert.Null(MemberSignatureParser.Parse("F(ByVal ByRef A, B)"));
            Assert.Null(MemberSignatureParser.Parse("F(A, ByRef ByVal B)"));
            Assert.Null(MemberSignatureParser.Parse("F(ByRef ByRef A)"));
            Assert.Null(MemberSignatureParser.Parse("F(A, ByVal ByVal B)"));
            Assert.Null(MemberSignatureParser.Parse("F(Of ByVal)"));
            Assert.Null(MemberSignatureParser.Parse("F(Of ByRef C)"));
            Assert.Null(MemberSignatureParser.Parse("F(C(Of ByRef))"));
            Assert.Null(MemberSignatureParser.Parse("F(C(Of ByRef C))"));
        }

        // Special types are treated as keywords in names,
        // but not recognized as special types.
        [Fact]
        public void SpecialTypes_Names()
        {
            // Method name only.
            Assert.Null(MemberSignatureParser.Parse("Integer"));
            Assert.Null(MemberSignatureParser.Parse("paramarray"));
            VerifySignature("[Integer]",
                SignatureNameOnly(
                    Name("Integer")));
            // Type and method name.
            VerifySignature("[Object].Integer",
                SignatureNameOnly(
                    Qualified(
                        Name("Object"),
                        "Integer")));
            // Type parameters.
            VerifySignature("F(Of Void)",
                SignatureNameOnly(
                    Generic(Name("F"),
                    "Void")));
            Assert.Null(MemberSignatureParser.Parse("F(Of boolean)"));
            Assert.Null(MemberSignatureParser.Parse("F(Of char)"));
            Assert.Null(MemberSignatureParser.Parse("F(Of SBYTE)"));
            Assert.Null(MemberSignatureParser.Parse("F(Of BYTE)"));
            Assert.Null(MemberSignatureParser.Parse("F(Of Short)"));
            Assert.Null(MemberSignatureParser.Parse("F(Of UShort)"));
            VerifySignature("F(Of [Boolean], [Char], [sbyte], [byte], [SHORT], [USHORT], [Integer], [UInteger], [Long], [ULong], [Single], [Double], [String], [Object], [Decimal], [Date])()",
                Signature(
                    Generic(Name("F"),
                    "Boolean",
                    "Char",
                    "sbyte",
                    "byte",
                    "SHORT",
                    "USHORT",
                    "Integer",
                    "UInteger",
                    "Long",
                    "ULong",
                    "Single",
                    "Double",
                    "String",
                    "Object",
                    "Decimal",
                    "Date")));
        }

        // Special types are recognized in type references.
        [Fact]
        public void SpecialTypes_TypeReferences()
        {
            // Parameters.
            VerifySignature("F(boolean, char, sbyte, byte, short, ushort, integer, uinteger, long, ulong, single, double, string, object, decimal, date)",
                Signature(
                    Name("F"),
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
                    Qualified("System", "Decimal"),
                    Qualified("System", "DateTime")));
            // Type arguments.
            VerifySignature("F(C(OF DECIMAL, INTEGER, STRING, OBJECT))",
                Signature(
                    Name("F"),
                    Generic(
                        Identifier("C"),
                        Qualified("System", "Decimal"),
                        Qualified("System", "Int32"),
                        Qualified("System", "String"),
                        Qualified("System", "Object"))));
            // Not special types.
            VerifySignature("F(Void, Int, [Object], A.[Integer], B.Single)",
                Signature(
                    Name("F"),
                    Identifier("Void"),
                    Identifier("Int"),
                    Identifier("Object"),
                    Qualified("A", "Integer"),
                    Qualified("B", "Single")));
        }

        [Fact]
        public void EscapedNames()
        {
            VerifySignature("[F3]",
                SignatureNameOnly(
                    Name("F3")));
            VerifySignature("[_]",
                SignatureNameOnly(
                    Name("_")));
            VerifySignature("[Integer]",
                SignatureNameOnly(
                    Name("Integer")));
            VerifySignature("A.B.[Integer]",
                SignatureNameOnly(
                    Qualified(
                        Qualified(
                            Name("A"),
                            "B"),
                        "Integer")));
            VerifySignature("F([Integer])",
                Signature(
                    Name("F"),
                    Identifier("Integer")));
            VerifySignature("F(System.[Integer])",
                Signature(
                    Name("F"),
                    Qualified(
                        Identifier("System"),
                        "Integer")));
            VerifySignature("A(Of [Object]).B(Of [Integer]).F(Of [Of])",
                SignatureNameOnly(
                    Generic(
                        Qualified(
                            Generic(
                                Qualified(
                                    Generic(
                                        Name("A"),
                                        "Object"),
                                    "B"),
                                "Integer"),
                            "F"),
                        "Of")));
            VerifySignature("F(C(Of Integer, [Date]))",
                Signature(
                    Name("F"),
                    Generic(
                        Identifier("C"),
                        Qualified("System", "Int32"),
                        Identifier("Date"))));
            Assert.Null(MemberSignatureParser.Parse("@"));
            Assert.Null(MemberSignatureParser.Parse("@Integer"));
            Assert.Null(MemberSignatureParser.Parse("["));
            Assert.Null(MemberSignatureParser.Parse("[]"));
            Assert.Null(MemberSignatureParser.Parse("[3"));
            Assert.Null(MemberSignatureParser.Parse("[3]"));
            Assert.Null(MemberSignatureParser.Parse("[[F"));
            Assert.Null(MemberSignatureParser.Parse("[F"));
            Assert.Null(MemberSignatureParser.Parse("[F["));
            Assert.Null(MemberSignatureParser.Parse("F]"));
            Assert.Null(MemberSignatureParser.Parse("[(T)"));
            Assert.Null(MemberSignatureParser.Parse("[Object]]"));
            Assert.Null(MemberSignatureParser.Parse("[Object+]"));
            Assert.Null(MemberSignatureParser.Parse("[Object ]"));
            Assert.Null(MemberSignatureParser.Parse("[.F"));
            Assert.Null(MemberSignatureParser.Parse("[()"));
            Assert.Null(MemberSignatureParser.Parse("F([)"));
            Assert.Null(MemberSignatureParser.Parse("F(A, [)"));
        }

        private static void VerifySignature(string str, RequestSignature expectedSignature)
        {
            var actualSignature = MemberSignatureParser.Parse(str);
            VerifySignature(actualSignature, expectedSignature);
        }
    }
}
