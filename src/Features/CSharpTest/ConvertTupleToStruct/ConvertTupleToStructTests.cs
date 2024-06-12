// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertTupleToStruct
{
    using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertTupleToStructCodeRefactoringProvider>;

    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)]
    public class ConvertTupleToStructTests
    {
        private static OptionsCollection PreferImplicitTypeWithInfo()
            => new(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Suggestion },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, true, NotificationOption2.Suggestion },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, true, NotificationOption2.Suggestion },
            };

        private static async Task TestAsync(
            string text,
            string expected,
            int index = 0,
            string? equivalenceKey = null,
            LanguageVersion languageVersion = LanguageVersion.CSharp9,
            OptionsCollection? options = null,
            TestHost testHost = TestHost.InProcess,
            string[]? actions = null)
        {
            if (index != 0)
                Assert.NotNull(equivalenceKey);

            options ??= new OptionsCollection(LanguageNames.CSharp);

            await new VerifyCS.Test
            {
                TestCode = text,
                FixedCode = expected,
                TestHost = testHost,
                LanguageVersion = languageVersion,
                CodeActionIndex = index,
                CodeActionEquivalenceKey = equivalenceKey,
                ExactActionSetOffered = actions,
                Options = { options },
            }.RunAsync();
        }

        #region update containing member tests

        [Theory, CombinatorialData]
        public async Task ConvertSingleTupleType(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertSingleTupleTypeToRecord(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, B: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, B: 2);
                    }
                }

                internal record struct NewStruct(int a, int B)
                {
                    public static implicit operator (int a, int B)(NewStruct value)
                    {
                        return (value.a, value.B);
                    }

                    public static implicit operator NewStruct((int a, int B) value)
                    {
                        return new NewStruct(value.a, value.B);
                    }
                }
                """;
            await TestAsync(text, expected, languageVersion: LanguageVersion.CSharp12, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertSingleTupleTypeToRecord_FileScopedNamespace(TestHost host)
        {
            var text = """
                namespace N;

                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }
                }
                """;
            var expected = """
                namespace N;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                internal record struct NewStruct(int a, int b)
                {
                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, languageVersion: LanguageVersion.CSharp12, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertSingleTupleTypeToRecord_MatchedNameCasing(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](A: 1, B: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(A: 1, B: 2);
                    }
                }

                internal record struct NewStruct(int A, int B)
                {
                    public static implicit operator (int A, int B)(NewStruct value)
                    {
                        return (value.A, value.B);
                    }

                    public static implicit operator NewStruct((int A, int B) value)
                    {
                        return new NewStruct(value.A, value.B);
                    }
                }
                """;
            await TestAsync(text, expected, languageVersion: LanguageVersion.CSharp12, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/45451"), CombinatorialData]
        public async Task ConvertSingleTupleType_ChangeArgumentNameCase(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](A: 1, B: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int A;
                    public int B;

                    public NewStruct(int a, int b)
                    {
                        A = a;
                        B = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               A == other.A &&
                               B == other.B;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = -1817952719;
                        hashCode = hashCode * -1521134295 + A.GetHashCode();
                        hashCode = hashCode * -1521134295 + B.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = A;
                        b = B;
                    }

                    public static implicit operator (int A, int B)(NewStruct value)
                    {
                        return (value.A, value.B);
                    }

                    public static implicit operator NewStruct((int A, int B) value)
                    {
                        return new NewStruct(value.A, value.B);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/45451"), CombinatorialData]
        public async Task ConvertSingleTupleType_ChangeArgumentNameCase_Uppercase(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](A: 1, B: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(p_a_: 1, p_b_: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int A;
                    public int B;

                    public NewStruct(int p_a_, int p_b_)
                    {
                        A = p_a_;
                        B = p_b_;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               A == other.A &&
                               B == other.B;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = -1817952719;
                        hashCode = hashCode * -1521134295 + A.GetHashCode();
                        hashCode = hashCode * -1521134295 + B.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int p_a_, out int p_b_)
                    {
                        p_a_ = A;
                        p_b_ = B;
                    }

                    public static implicit operator (int A, int B)(NewStruct value)
                    {
                        return (value.A, value.B);
                    }

                    public static implicit operator NewStruct((int A, int B) value)
                    {
                        return new NewStruct(value.A, value.B);
                    }
                }
                """;
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name2",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)),
                accessibilityList: default,
                modifiers: default);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name2",
                prefix: "p_",
                suffix: "_",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            var options = PreferImplicitTypeWithInfo();
            options.Add(NamingStyleOptions.NamingPreferences, info);

            await TestAsync(text, expected, options: options, testHost: host);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/39916"), CombinatorialData]
        public async Task ConvertSingleTupleType_Explicit(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        int hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertSingleTupleTypeNoNames(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](1, 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(1, 2);
                    }
                }

                internal struct NewStruct
                {
                    public int Item1;
                    public int Item2;

                    public NewStruct(int item1, int item2)
                    {
                        Item1 = item1;
                        Item2 = item2;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               Item1 == other.Item1 &&
                               Item2 == other.Item2;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = -1030903623;
                        hashCode = hashCode * -1521134295 + Item1.GetHashCode();
                        hashCode = hashCode * -1521134295 + Item2.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int item1, out int item2)
                    {
                        item1 = Item1;
                        item2 = Item2;
                    }

                    public static implicit operator (int, int)(NewStruct value)
                    {
                        return (value.Item1, value.Item2);
                    }

                    public static implicit operator NewStruct((int, int) value)
                    {
                        return new NewStruct(value.Item1, value.Item2);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertSingleTupleTypePartialNames(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](1, b: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int Item1;
                    public int b;

                    public NewStruct(int item1, int b)
                    {
                        Item1 = item1;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               Item1 == other.Item1 &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 174326978;
                        hashCode = hashCode * -1521134295 + Item1.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int item1, out int b)
                    {
                        item1 = Item1;
                        b = this.b;
                    }

                    public static implicit operator (int, int b)(NewStruct value)
                    {
                        return (value.Item1, value.b);
                    }

                    public static implicit operator NewStruct((int, int b) value)
                    {
                        return new NewStruct(value.Item1, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertFromType(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        [||](int a, int b) t1 = (a: 1, b: 2);
                        (int a, int b) t2 = (a: 1, b: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        NewStruct t1 = new NewStruct(a: 1, b: 2);
                        NewStruct t2 = new NewStruct(a: 1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertFromType2(TestHost host)
        {
            var text = """
                class Test
                {
                    (int a, int b) Method()
                    {
                        [||](int a, int b) t1 = (a: 1, b: 2);
                        (int a, int b) t2 = (a: 1, b: 2);
                        return default;
                    }
                }
                """;
            var expected = """
                class Test
                {
                    NewStruct Method()
                    {
                        NewStruct t1 = new NewStruct(a: 1, b: 2);
                        NewStruct t2 = new NewStruct(a: 1, b: 2);
                        return default;
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertFromType3(TestHost host)
        {
            var text = """
                class Test
                {
                    (int a, int b) Method()
                    {
                        [||](int a, int b) t1 = (a: 1, b: 2);
                        (int b, int a) t2 = (b: 1, a: 2);
                        return default;
                    }
                }
                """;
            var expected = """
                class Test
                {
                    NewStruct Method()
                    {
                        NewStruct t1 = new NewStruct(a: 1, b: 2);
                        (int b, int a) t2 = (b: 1, a: 2);
                        return default;
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertFromType4(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        (int a, int b) t1 = (a: 1, b: 2);
                        [||](int a, int b) t2 = (a: 1, b: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        NewStruct t1 = new NewStruct(a: 1, b: 2);
                        NewStruct t2 = new NewStruct(a: 1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertSingleTupleTypeInNamespace(TestHost host)
        {
            var text = """
                namespace N
                {
                    class Test
                    {
                        void Method()
                        {
                            var t1 = [||](a: 1, b: 2);
                        }
                    }
                }
                """;
            var expected = """
                namespace N
                {
                    class Test
                    {
                        void Method()
                        {
                            var t1 = new NewStruct(a: 1, b: 2);
                        }
                    }

                    internal struct NewStruct
                    {
                        public int a;
                        public int b;

                        public NewStruct(int a, int b)
                        {
                            this.a = a;
                            this.b = b;
                        }

                        public override bool Equals(object obj)
                        {
                            return obj is NewStruct other &&
                                   a == other.a &&
                                   b == other.b;
                        }

                        public override int GetHashCode()
                        {
                            var hashCode = 2118541809;
                            hashCode = hashCode * -1521134295 + a.GetHashCode();
                            hashCode = hashCode * -1521134295 + b.GetHashCode();
                            return hashCode;
                        }

                        public void Deconstruct(out int a, out int b)
                        {
                            a = this.a;
                            b = this.b;
                        }

                        public static implicit operator (int a, int b)(NewStruct value)
                        {
                            return (value.a, value.b);
                        }

                        public static implicit operator NewStruct((int a, int b) value)
                        {
                            return new NewStruct(value.a, value.b);
                        }
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestNonLiteralNames_WithUsings(TestHost host)
        {
            var text = """
                using System.Collections.Generic;
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: {|CS0103:Goo|}(), b: {|CS0103:Bar|}());
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct({|CS0103:Goo|}(), {|CS0103:Bar|}());
                    }
                }

                internal struct NewStruct
                {
                    public object a;
                    public object b;

                    public NewStruct(object a, object b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               EqualityComparer<object>.Default.Equals(a, other.a) &&
                               EqualityComparer<object>.Default.Equals(b, other.b);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(a);
                        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(b);
                        return hashCode;
                    }

                    public void Deconstruct(out object a, out object b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (object a, object b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((object a, object b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestNonLiteralNames_WithoutUsings(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: {|CS0103:Goo|}(), b: {|CS0103:Bar|}());
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct({|CS0103:Goo|}(), {|CS0103:Bar|}());
                    }
                }

                internal struct NewStruct
                {
                    public object a;
                    public object b;

                    public NewStruct(object a, object b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               System.Collections.Generic.EqualityComparer<object>.Default.Equals(a, other.a) &&
                               System.Collections.Generic.EqualityComparer<object>.Default.Equals(b, other.b);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(a);
                        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(b);
                        return hashCode;
                    }

                    public void Deconstruct(out object a, out object b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (object a, object b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((object a, object b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertSingleTupleTypeWithInferredName(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method(int b)
                    {
                        var t1 = [||](a: 1, b);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method(int b)
                    {
                        var t1 = new NewStruct(a: 1, b);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertMultipleInstancesInSameMethod(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                        var t2 = (a: 3, b: 4);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        var t2 = new NewStruct(a: 3, b: 4);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertMultipleInstancesAcrossMethods(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                        var t2 = (a: 3, b: 4);
                    }

                    void Method2()
                    {
                        var t1 = (a: 1, b: 2);
                        var t2 = (a: 3, b: 4);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        var t2 = new NewStruct(a: 3, b: 4);
                    }

                    void Method2()
                    {
                        var t1 = (a: 1, b: 2);
                        var t2 = (a: 3, b: 4);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task OnlyConvertMatchingTypesInSameMethod(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method(int b)
                    {
                        var t1 = [||](a: 1, b: 2);
                        var t2 = (a: 3, b);
                        var t3 = (a: 4, b: 5, c: 6);
                        var t4 = (b: 5, a: 6);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method(int b)
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        var t2 = new NewStruct(a: 3, b);
                        var t3 = (a: 4, b: 5, c: 6);
                        var t4 = (b: 5, a: 6);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestFixAllMatchesInSingleMethod(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method(int b)
                    {
                        var t1 = [||](a: 1, b: 2);
                        var t2 = (a: 3, b);
                        var t3 = (a: 4, b: 5, c: 6);
                        var t4 = (b: 5, a: 6);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method(int b)
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        var t2 = new NewStruct(a: 3, b);
                        var t3 = (a: 4, b: 5, c: 6);
                        var t4 = (b: 5, a: 6);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestFixNotAcrossMethods(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                        var t2 = (a: 3, b: 4);
                    }

                    void Method2()
                    {
                        var t1 = (a: 1, b: 2);
                        var t2 = (a: 3, b: 4);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        var t2 = new NewStruct(a: 3, b: 4);
                    }

                    void Method2()
                    {
                        var t1 = (a: 1, b: 2);
                        var t2 = (a: 3, b: 4);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestTrivia_WithUsings(TestHost host)
        {
            var text = """
                using System.Collections.Generic;
                class Test
                {
                    void Method()
                    {
                        var t1 = /*1*/ [||]( /*2*/ a /*3*/ : /*4*/ 1 /*5*/ , /*6*/ {|CS0103:b|} /*7*/ = /*8*/ 2 /*9*/ ) /*10*/ ;
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;
                class Test
                {
                    void Method()
                    {
                        var t1 = /*1*/ new NewStruct( /*2*/ a /*3*/ : /*4*/ 1 /*5*/ , /*6*/ {|CS0103:b|} /*7*/ = /*8*/ 2 /*9*/ ) /*10*/ ;
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public object Item2;

                    public NewStruct(int a, object item2)
                    {
                        this.a = a;
                        Item2 = item2;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               EqualityComparer<object>.Default.Equals(Item2, other.Item2);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 913311208;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Item2);
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out object item2)
                    {
                        a = this.a;
                        item2 = Item2;
                    }

                    public static implicit operator (int a, object)(NewStruct value)
                    {
                        return (value.a, value.Item2);
                    }

                    public static implicit operator NewStruct((int a, object) value)
                    {
                        return new NewStruct(value.a, value.Item2);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestTrivia_WithoutUsings(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = /*1*/ [||]( /*2*/ a /*3*/ : /*4*/ 1 /*5*/ , /*6*/ {|CS0103:b|} /*7*/ = /*8*/ 2 /*9*/ ) /*10*/ ;
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = /*1*/ new NewStruct( /*2*/ a /*3*/ : /*4*/ 1 /*5*/ , /*6*/ {|CS0103:b|} /*7*/ = /*8*/ 2 /*9*/ ) /*10*/ ;
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public object Item2;

                    public NewStruct(int a, object item2)
                    {
                        this.a = a;
                        Item2 = item2;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               System.Collections.Generic.EqualityComparer<object>.Default.Equals(Item2, other.Item2);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 913311208;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + System.Collections.Generic.EqualityComparer<object>.Default.GetHashCode(Item2);
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out object item2)
                    {
                        a = this.a;
                        item2 = Item2;
                    }

                    public static implicit operator (int a, object)(NewStruct value)
                    {
                        return (value.a, value.Item2);
                    }

                    public static implicit operator NewStruct((int a, object) value)
                    {
                        return new NewStruct(value.a, value.Item2);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task NotIfReferencesAnonymousTypeInternally(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: new { c = 1, d = 2 });
                    }
                }
                """;

            await TestAsync(text, text, testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertMultipleNestedInstancesInSameMethod1_WithUsings(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: (object)(a: 1, b: default(object)));
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, (object)new NewStruct(a: 1, default(object)));
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public object b;

                    public NewStruct(int a, object b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               EqualityComparer<object>.Default.Equals(b, other.b);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(b);
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out object b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, object b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, object b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertMultipleNestedInstancesInSameMethod1_WithoutUsings(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: (object)(a: 1, b: default(object)));
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, (object)new NewStruct(a: 1, default(object)));
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public object b;

                    public NewStruct(int a, object b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               EqualityComparer<object>.Default.Equals(b, other.b);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(b);
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out object b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, object b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, object b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertMultipleNestedInstancesInSameMethod2_WithUsings(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: (object)[||](a: 1, b: default(object)));
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, (object)new NewStruct(a: 1, default(object)));
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public object b;

                    public NewStruct(int a, object b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               EqualityComparer<object>.Default.Equals(b, other.b);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(b);
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out object b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, object b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, object b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertMultipleNestedInstancesInSameMethod2_WithoutUsings(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: (object)[||](a: 1, b: default(object)));
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, (object)new NewStruct(a: 1, default(object)));
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public object b;

                    public NewStruct(int a, object b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               EqualityComparer<object>.Default.Equals(b, other.b);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(b);
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out object b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, object b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, object b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task RenameAnnotationOnStartingPoint(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                        var t2 = [||](a: 3, b: 4);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        var t2 = new NewStruct(a: 3, b: 4);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task CapturedMethodTypeParameters_WithUsings(TestHost host)
        {
            var text = """
                using System.Collections.Generic;
                class Test<X> where X : struct
                {
                    void Method<Y>(System.Collections.Generic.List<X> x, Y[] y) where Y : class, new()
                    {
                        var t1 = [||](a: x, b: y);
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;
                class Test<X> where X : struct
                {
                    void Method<Y>(System.Collections.Generic.List<X> x, Y[] y) where Y : class, new()
                    {
                        var t1 = new NewStruct<X, Y>(x, y);
                    }
                }

                internal struct NewStruct<X, Y>
                    where X : struct
                    where Y : class, new()
                {
                    public List<X> a;
                    public Y[] b;

                    public NewStruct(List<X> a, Y[] b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct<X, Y> other &&
                               EqualityComparer<List<X>>.Default.Equals(a, other.a) &&
                               EqualityComparer<Y[]>.Default.Equals(b, other.b);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + EqualityComparer<List<X>>.Default.GetHashCode(a);
                        hashCode = hashCode * -1521134295 + EqualityComparer<Y[]>.Default.GetHashCode(b);
                        return hashCode;
                    }

                    public void Deconstruct(out List<X> a, out Y[] b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (List<X> a, Y[] b)(NewStruct<X, Y> value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct<X, Y>((List<X> a, Y[] b) value)
                    {
                        return new NewStruct<X, Y>(value.a, value.b);
                    }
                }
                """;

            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host, actions: new[]
                {
                    FeaturesResources.updating_usages_in_containing_member
                });
        }

        [Theory, CombinatorialData]
        public async Task CapturedMethodTypeParameters_WithoutUsings(TestHost host)
        {
            var text = """
                class Test<X> where X : struct
                {
                    void Method<Y>(System.Collections.Generic.List<X> x, Y[] y) where Y : class, new()
                    {
                        var t1 = [||](a: x, b: y);
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;

                class Test<X> where X : struct
                {
                    void Method<Y>(System.Collections.Generic.List<X> x, Y[] y) where Y : class, new()
                    {
                        var t1 = new NewStruct<X, Y>(x, y);
                    }
                }

                internal struct NewStruct<X, Y>
                    where X : struct
                    where Y : class, new()
                {
                    public List<X> a;
                    public Y[] b;

                    public NewStruct(List<X> a, Y[] b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct<X, Y> other &&
                               EqualityComparer<List<X>>.Default.Equals(a, other.a) &&
                               EqualityComparer<Y[]>.Default.Equals(b, other.b);
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + EqualityComparer<List<X>>.Default.GetHashCode(a);
                        hashCode = hashCode * -1521134295 + EqualityComparer<Y[]>.Default.GetHashCode(b);
                        return hashCode;
                    }

                    public void Deconstruct(out List<X> a, out Y[] b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (List<X> a, Y[] b)(NewStruct<X, Y> value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct<X, Y>((List<X> a, Y[] b) value)
                    {
                        return new NewStruct<X, Y>(value.a, value.b);
                    }
                }
                """;

            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host, actions: new[]
                {
                    FeaturesResources.updating_usages_in_containing_member
                });
        }

        [Theory, CombinatorialData]
        public async Task NewTypeNameCollision(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }
                }

                class NewStruct
                {
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct1(a: 1, b: 2);
                    }
                }

                class NewStruct
                {
                }

                internal struct NewStruct1
                {
                    public int a;
                    public int b;

                    public NewStruct1(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct1 other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct1 value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct1((int a, int b) value)
                    {
                        return new NewStruct1(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestDuplicatedName(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, a: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, a: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int a;

                    public NewStruct(int a, int a)
                    {
                        this.a = a;
                        this.a = a;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               this.a == other.a &&
                               this.a == other.a;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2068208952;
                        hashCode = hashCode * -1521134295 + this.a.GetHashCode();
                        hashCode = hashCode * -1521134295 + this.a.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int a)
                    {
                        a = this.a;
                        a = this.a;
                    }

                    public static implicit operator (int a, int a)(NewStruct value)
                    {
                        return (value.a, value.a);
                    }

                    public static implicit operator NewStruct((int a, int a) value)
                    {
                        return new NewStruct(value.a, value.a);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = text,
                FixedCode = expected,
                TestHost = host,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(6,25): error CS8127: Tuple element names must be unique.
                    DiagnosticResult.CompilerError("CS8127").WithSpan(5, 25, 5, 26),
                },
                FixedState =
                {
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(5,22): error CS7036: There is no argument given that corresponds to the required parameter 'a' of 'NewStruct.NewStruct(int, int)'
                        DiagnosticResult.CompilerError("CS7036").WithSpan(5, 22, 5, 31).WithArguments("a", "NewStruct.NewStruct(int, int)"),
                        // /0/Test0.cs(12,16): error CS0102: The type 'NewStruct' already contains a definition for 'a'
                        DiagnosticResult.CompilerError("CS0102").WithSpan(12, 16, 12, 17).WithArguments("NewStruct", "a"),
                        // /0/Test0.cs(14,12): error CS0171: Field 'NewStruct.a' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                        DiagnosticResult.CompilerError("CS0171").WithSpan(14, 12, 14, 21).WithArguments("NewStruct.a", "11.0"),
                        // /0/Test0.cs(14,12): error CS0171: Field 'NewStruct.a' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                        DiagnosticResult.CompilerError("CS0171").WithSpan(14, 12, 14, 21).WithArguments("NewStruct.a", "11.0"),
                        // /0/Test0.cs(14,33): error CS0100: The parameter name 'a' is a duplicate
                        DiagnosticResult.CompilerError("CS0100").WithSpan(14, 33, 14, 34).WithArguments("a"),
                        // /0/Test0.cs(16,14): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(16, 14, 16, 15).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(16,18): error CS0229: Ambiguity between 'int a' and 'int a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(16, 18, 16, 19).WithArguments("int a", "int a"),
                        // /0/Test0.cs(17,14): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(17, 14, 17, 15).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(17,18): error CS0229: Ambiguity between 'int a' and 'int a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(17, 18, 17, 19).WithArguments("int a", "int a"),
                        // /0/Test0.cs(23,21): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(23, 21, 23, 22).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(23,32): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(23, 32, 23, 33).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(24,21): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(24, 21, 24, 22).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(24,32): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(24, 32, 24, 33).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(30,50): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(30, 50, 30, 51).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(31,50): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(31, 50, 31, 51).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(35,17): error CS0177: The out parameter 'a' must be assigned to before control leaves the current method
                        DiagnosticResult.CompilerError("CS0177").WithSpan(35, 17, 35, 28).WithArguments("a"),
                        // /0/Test0.cs(35,17): error CS0177: The out parameter 'a' must be assigned to before control leaves the current method
                        DiagnosticResult.CompilerError("CS0177").WithSpan(35, 17, 35, 28).WithArguments("a"),
                        // /0/Test0.cs(35,48): error CS0100: The parameter name 'a' is a duplicate
                        DiagnosticResult.CompilerError("CS0100").WithSpan(35, 48, 35, 49).WithArguments("a"),
                        // /0/Test0.cs(37,9): error CS0229: Ambiguity between 'out int a' and 'out int a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(37, 9, 37, 10).WithArguments("out int a", "out int a"),
                        // /0/Test0.cs(37,18): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(37, 18, 37, 19).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(38,9): error CS0229: Ambiguity between 'out int a' and 'out int a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(38, 9, 38, 10).WithArguments("out int a", "out int a"),
                        // /0/Test0.cs(38,18): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(38, 18, 38, 19).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(41,49): error CS8127: Tuple element names must be unique.
                        DiagnosticResult.CompilerError("CS8127").WithSpan(41, 49, 41, 50),
                        // /0/Test0.cs(43,23): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(43, 23, 43, 24).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(43,32): error CS0229: Ambiguity between 'NewStruct.a' and 'NewStruct.a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(43, 32, 43, 33).WithArguments("NewStruct.a", "NewStruct.a"),
                        // /0/Test0.cs(46,59): error CS8127: Tuple element names must be unique.
                        DiagnosticResult.CompilerError("CS8127").WithSpan(46, 59, 46, 60),
                        // /0/Test0.cs(48,36): error CS0229: Ambiguity between '(int a, int a).a' and '(int a, int a).a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(48, 36, 48, 37).WithArguments("(int a, int a).a", "(int a, int a).a"),
                        // /0/Test0.cs(48,45): error CS0229: Ambiguity between '(int a, int a).a' and '(int a, int a).a'
                        DiagnosticResult.CompilerError("CS0229").WithSpan(48, 45, 48, 46).WithArguments("(int a, int a).a", "(int a, int a).a"),
                    }
                },
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task TestInLambda1(TestHost host)
        {
            var text = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                        Action a = () =>
                        {
                            var t2 = (a: 3, b: 4);
                        };
                    }
                }
                """;
            var expected = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        Action a = () =>
                        {
                            var t2 = new NewStruct(a: 3, b: 4);
                        };
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestInLambda2(TestHost host)
        {
            var text = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                        Action a = () =>
                        {
                            var t2 = [||](a: 3, b: 4);
                        };
                    }
                }
                """;
            var expected = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        Action a = () =>
                        {
                            var t2 = new NewStruct(a: 3, b: 4);
                        };
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestInLocalFunction1(TestHost host)
        {
            var text = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                        void Goo()
                        {
                            var t2 = (a: 3, b: 4);
                        }
                    }
                }
                """;
            var expected = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        void Goo()
                        {
                            var t2 = new NewStruct(a: 3, b: 4);
                        }
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task TestInLocalFunction2(TestHost host)
        {
            var text = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                        void Goo()
                        {
                            var t2 = [||](a: 3, b: 4);
                        }
                    }
                }
                """;
            var expected = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                        void Goo()
                        {
                            var t2 = new NewStruct(a: 3, b: 4);
                        }
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task ConvertWithDefaultNames1(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = [||](1, 2);
                        var t2 = (1, 2);
                        var t3 = (a: 1, b: 2);
                        var t4 = (Item1: 1, Item2: 2);
                        var t5 = (Item1: 1, Item2: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(1, 2);
                        var t2 = new NewStruct(1, 2);
                        var t3 = (a: 1, b: 2);
                        var t4 = new NewStruct(item1: 1, item2: 2);
                        var t5 = new NewStruct(item1: 1, item2: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int Item1;
                    public int Item2;

                    public NewStruct(int item1, int item2)
                    {
                        Item1 = item1;
                        Item2 = item2;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               Item1 == other.Item1 &&
                               Item2 == other.Item2;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = -1030903623;
                        hashCode = hashCode * -1521134295 + Item1.GetHashCode();
                        hashCode = hashCode * -1521134295 + Item2.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int item1, out int item2)
                    {
                        item1 = Item1;
                        item2 = Item2;
                    }

                    public static implicit operator (int, int)(NewStruct value)
                    {
                        return (value.Item1, value.Item2);
                    }

                    public static implicit operator NewStruct((int, int) value)
                    {
                        return new NewStruct(value.Item1, value.Item2);
                    }
                }
                """;

            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host, actions: new[]
                {
                    FeaturesResources.updating_usages_in_containing_member,
                    FeaturesResources.updating_usages_in_containing_type,
                });
        }

        [Theory, CombinatorialData]
        public async Task ConvertWithDefaultNames2(TestHost host)
        {
            var text = """
                class Test
                {
                    void Method()
                    {
                        var t1 = (1, 2);
                        var t2 = (1, 2);
                        var t3 = (a: 1, b: 2);
                        var t4 = [||](Item1: 1, Item2: 2);
                        var t5 = (Item1: 1, Item2: 2);
                    }
                }
                """;
            var expected = """
                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(1, 2);
                        var t2 = new NewStruct(1, 2);
                        var t3 = (a: 1, b: 2);
                        var t4 = new NewStruct(item1: 1, item2: 2);
                        var t5 = new NewStruct(item1: 1, item2: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int Item1;
                    public int Item2;

                    public NewStruct(int item1, int item2)
                    {
                        Item1 = item1;
                        Item2 = item2;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               Item1 == other.Item1 &&
                               Item2 == other.Item2;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = -1030903623;
                        hashCode = hashCode * -1521134295 + Item1.GetHashCode();
                        hashCode = hashCode * -1521134295 + Item2.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int item1, out int item2)
                    {
                        item1 = Item1;
                        item2 = Item2;
                    }

                    public static implicit operator (int Item1, int Item2)(NewStruct value)
                    {
                        return (value.Item1, value.Item2);
                    }

                    public static implicit operator NewStruct((int Item1, int Item2) value)
                    {
                        return new NewStruct(value.Item1, value.Item2);
                    }
                }
                """;

            await TestAsync(text, expected, options: PreferImplicitTypeWithInfo(), testHost: host, actions: new[]
                {
                    FeaturesResources.updating_usages_in_containing_member,
                    FeaturesResources.updating_usages_in_containing_type,
                });
        }

        #endregion

        #region update containing type tests

        [Theory, CombinatorialData]
        public async Task TestCapturedTypeParameter_UpdateType_WithUsings(TestHost host)
        {
            var text = """
                using System;

                class Test<T>
                {
                    void Method(T t)
                    {
                        var t1 = [||](a: t, b: 2);
                    }

                    T t;
                    void Goo()
                    {
                        var t2 = (a: t, b: 4);
                    }

                    void Blah<T>(T t)
                    {
                        var t2 = (a: t, b: 4);
                    }
                }
                """;
            var expected = """
                using System;
                using System.Collections.Generic;

                class Test<T>
                {
                    void Method(T t)
                    {
                        var t1 = new NewStruct<T>(t, b: 2);
                    }

                    T t;
                    void Goo()
                    {
                        var t2 = new NewStruct<T>(t, b: 4);
                    }

                    void Blah<T>(T t)
                    {
                        var t2 = (a: t, b: 4);
                    }
                }

                internal struct NewStruct<T>
                {
                    public T a;
                    public int b;

                    public NewStruct(T a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct<T> other &&
                               EqualityComparer<T>.Default.Equals(a, other.a) &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(a);
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out T a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (T a, int b)(NewStruct<T> value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct<T>((T a, int b) value)
                    {
                        return new NewStruct<T>(value.a, value.b);
                    }
                }
                """;

            await TestAsync(
                text, expected, index: 1, equivalenceKey: Scope.ContainingType.ToString(),
                options: PreferImplicitTypeWithInfo(), testHost: host, actions: new[]
                {
                    FeaturesResources.updating_usages_in_containing_member,
                    FeaturesResources.updating_usages_in_containing_type
                });
        }

        [Theory, CombinatorialData]
        public async Task TestCapturedTypeParameter_UpdateType_WithoutUsings(TestHost host)
        {
            var text = """
                class Test<T>
                {
                    void Method(T t)
                    {
                        var t1 = [||](a: t, b: 2);
                    }

                    T t;
                    void Goo()
                    {
                        var t2 = (a: t, b: 4);
                    }

                    void Blah<T>(T t)
                    {
                        var t2 = (a: t, b: 4);
                    }
                }
                """;
            var expected = """
                using System.Collections.Generic;

                class Test<T>
                {
                    void Method(T t)
                    {
                        var t1 = new NewStruct<T>(t, b: 2);
                    }

                    T t;
                    void Goo()
                    {
                        var t2 = new NewStruct<T>(t, b: 4);
                    }

                    void Blah<T>(T t)
                    {
                        var t2 = (a: t, b: 4);
                    }
                }

                internal struct NewStruct<T>
                {
                    public T a;
                    public int b;

                    public NewStruct(T a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct<T> other &&
                               EqualityComparer<T>.Default.Equals(a, other.a) &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(a);
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out T a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (T a, int b)(NewStruct<T> value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct<T>((T a, int b) value)
                    {
                        return new NewStruct<T>(value.a, value.b);
                    }
                }
                """;

            await TestAsync(
                text, expected, index: 1, equivalenceKey: Scope.ContainingType.ToString(),
                options: PreferImplicitTypeWithInfo(), testHost: host, actions: new[]
                {
                    FeaturesResources.updating_usages_in_containing_member,
                    FeaturesResources.updating_usages_in_containing_type
                });
        }

        [Theory, CombinatorialData]
        public async Task UpdateAllInType_SinglePart_SingleFile(TestHost host)
        {
            var text = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }

                    void Goo()
                    {
                        var t2 = (a: 3, b: 4);
                    }
                }

                class Other
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;
            var expected = """
                using System;

                class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }

                    void Goo()
                    {
                        var t2 = new NewStruct(a: 3, b: 4);
                    }
                }

                class Other
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(
                text, expected, index: 1, equivalenceKey: Scope.ContainingType.ToString(),
                options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task UpdateAllInType_MultiplePart_SingleFile(TestHost host)
        {
            var text = """
                using System;

                partial class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }
                }

                partial class Test
                {
                    (int a, int b) Goo()
                    {
                        var t2 = (a: 3, b: 4);
                        return default;
                    }
                }

                class Other
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;
            var expected = """
                using System;

                partial class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                partial class Test
                {
                    NewStruct Goo()
                    {
                        var t2 = new NewStruct(a: 3, b: 4);
                        return default;
                    }
                }

                class Other
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            await TestAsync(
                text, expected, index: 1, equivalenceKey: Scope.ContainingType.ToString(),
                options: PreferImplicitTypeWithInfo(), testHost: host);
        }

        [Theory, CombinatorialData]
        public async Task UpdateAllInType_MultiplePart_MultipleFile(TestHost host)
        {
            var text1 = """
                using System;

                partial class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }
                }

                partial class Other
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;
            var text2 = """
                using System;

                partial class Test
                {
                    (int a, int b) Goo()
                    {
                        var t2 = (a: 3, b: 4);
                        return default;
                    }
                }

                partial class Other
                {
                    void Goo()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;

            var expected1 = """
                using System;

                partial class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                partial class Other
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }

                internal struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            var expected2 = """
                using System;

                partial class Test
                {
                    NewStruct Goo()
                    {
                        var t2 = new NewStruct(a: 3, b: 4);
                        return default;
                    }
                }

                partial class Other
                {
                    void Goo()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        text1,
                        text2,
                    }
                },
                FixedState =
                {
                    Sources =
                    {
                        expected1,
                        expected2,
                    }
                },
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = Scope.ContainingType.ToString(),
                TestHost = host,
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        #endregion update containing project tests

        #region update containing project tests

        [Theory, CombinatorialData]
        public async Task UpdateAllInProject_MultiplePart_MultipleFile_WithNamespace(TestHost host)
        {
            var text1 = """
                using System;

                namespace N
                {
                    partial class Test
                    {
                        void Method()
                        {
                            var t1 = [||](a: 1, b: 2);
                        }
                    }

                    partial class Other
                    {
                        void Method()
                        {
                            var t1 = (a: 1, b: 2);
                        }
                    }
                }
                """;
            var text2 = """
                using System;

                partial class Test
                {
                    (int a, int b) Goo()
                    {
                        var t2 = (a: 3, b: 4);
                        return default;
                    }
                }

                partial class Other
                {
                    void Goo()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;

            var expected1 = """
                using System;

                namespace N
                {
                    partial class Test
                    {
                        void Method()
                        {
                            var t1 = new NewStruct(a: 1, b: 2);
                        }
                    }

                    partial class Other
                    {
                        void Method()
                        {
                            var t1 = new NewStruct(a: 1, b: 2);
                        }
                    }

                    internal struct NewStruct
                    {
                        public int a;
                        public int b;

                        public NewStruct(int a, int b)
                        {
                            this.a = a;
                            this.b = b;
                        }

                        public override bool Equals(object obj)
                        {
                            return obj is NewStruct other &&
                                   a == other.a &&
                                   b == other.b;
                        }

                        public override int GetHashCode()
                        {
                            var hashCode = 2118541809;
                            hashCode = hashCode * -1521134295 + a.GetHashCode();
                            hashCode = hashCode * -1521134295 + b.GetHashCode();
                            return hashCode;
                        }

                        public void Deconstruct(out int a, out int b)
                        {
                            a = this.a;
                            b = this.b;
                        }

                        public static implicit operator (int a, int b)(NewStruct value)
                        {
                            return (value.a, value.b);
                        }

                        public static implicit operator NewStruct((int a, int b) value)
                        {
                            return new NewStruct(value.a, value.b);
                        }
                    }
                }
                """;
            var expected2 = """
                using System;

                partial class Test
                {
                    N.NewStruct Goo()
                    {
                        var t2 = new N.NewStruct(a: 3, b: 4);
                        return default;
                    }
                }

                partial class Other
                {
                    void Goo()
                    {
                        var t1 = new N.NewStruct(a: 1, b: 2);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                CodeActionIndex = 2,
                CodeActionEquivalenceKey = Scope.ContainingProject.ToString(),
                TestHost = host,
                TestState =
                {
                    Sources = { text1, text2, },
                },
                FixedState =
                {
                    Sources = { expected1, expected2 },
                },
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        #endregion

        #region update dependent projects

        [Theory, CombinatorialData]
        public async Task UpdateDependentProjects_DirectDependency(TestHost host)
        {
            var text1 = """
                using System;

                partial class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }
                }

                partial class Other
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;

            var text2 = """
                using System;

                partial class Other
                {
                    void Goo()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;
            var expected1 = """
                using System;

                partial class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                partial class Other
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                public struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;
            var expected2 = """
                using System;

                partial class Other
                {
                    void Goo()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                CodeActionIndex = 3,
                CodeActionEquivalenceKey = Scope.DependentProjects.ToString(),
                TestHost = host,
                TestState =
                {
                    Sources = { text1 },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] =
                        {
                            Sources = { text2 },
                            AdditionalProjectReferences = { "TestProject" },
                        }
                    },
                },
                FixedState =
                {
                    Sources = { expected1 },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] =
                        {
                            Sources = { expected2 },
                            AdditionalProjectReferences = { "TestProject" },
                        }
                    },
                },
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task UpdateDependentProjects_NoDependency(TestHost host)
        {
            var text1 = """
                using System;

                partial class Test
                {
                    void Method()
                    {
                        var t1 = [||](a: 1, b: 2);
                    }
                }

                partial class Other
                {
                    void Method()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;
            var text2 = """
                using System;

                partial class Other
                {
                    void Goo()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;
            var expected1 = """
                using System;

                partial class Test
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                partial class Other
                {
                    void Method()
                    {
                        var t1 = new NewStruct(a: 1, b: 2);
                    }
                }

                public struct NewStruct
                {
                    public int a;
                    public int b;

                    public NewStruct(int a, int b)
                    {
                        this.a = a;
                        this.b = b;
                    }

                    public override bool Equals(object obj)
                    {
                        return obj is NewStruct other &&
                               a == other.a &&
                               b == other.b;
                    }

                    public override int GetHashCode()
                    {
                        var hashCode = 2118541809;
                        hashCode = hashCode * -1521134295 + a.GetHashCode();
                        hashCode = hashCode * -1521134295 + b.GetHashCode();
                        return hashCode;
                    }

                    public void Deconstruct(out int a, out int b)
                    {
                        a = this.a;
                        b = this.b;
                    }

                    public static implicit operator (int a, int b)(NewStruct value)
                    {
                        return (value.a, value.b);
                    }

                    public static implicit operator NewStruct((int a, int b) value)
                    {
                        return new NewStruct(value.a, value.b);
                    }
                }
                """;

            var expected2 = """
                using System;

                partial class Other
                {
                    void Goo()
                    {
                        var t1 = (a: 1, b: 2);
                    }
                }
                """;

            await new VerifyCS.Test
            {
                CodeActionIndex = 3,
                CodeActionEquivalenceKey = Scope.DependentProjects.ToString(),
                TestHost = host,
                TestState =
                {
                    Sources = { text1 },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] = { Sources = { text2 } }
                    },
                },
                FixedState =
                {
                    Sources = { expected1 },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] = { Sources = { expected2 } }
                    },
                },
                Options = { PreferImplicitTypeWithInfo() },
            }.RunAsync();
        }

        #endregion
    }
}
