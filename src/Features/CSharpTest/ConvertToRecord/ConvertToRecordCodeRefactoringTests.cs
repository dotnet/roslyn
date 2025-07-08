// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertToRecord;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRecord;

using VerifyCSRefactoring = CSharpCodeRefactoringVerifier<CSharpConvertToRecordRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsConvertToRecord)]
public sealed class ConvertToRecordCodeRefactoringTests
{
    [Fact]
    public async Task VerifyRefactoringAndFixHaveSameEquivalenceKey()
    {
        var changedMarkup = """
            namespace N
            {
                public record B
                {
                    public int Foo { get; init; }
                }

                public record C(int P) : B;
            }
            """;
        CodeAction? codeAction = null;
        var refactoringTest = new RefactoringTest
        {
            TestCode = """
            namespace N
            {
                public record B
                {
                    public int Foo { get; init; }
                }

                public class [|C : {|CS8865:B|}|]
                {
                    public int P { get; init; }
                }
            }
            """,
            FixedCode = changedMarkup,
            CodeActionVerifier = Verify,
        };
        var codeFixTest = new CodeFixTest
        {
            TestCode = """
            namespace N
            {
                public record B
                {
                    public int Foo { get; init; }
                }

                public class C : [|B|]
                {
                    public int P { get; init; }
                }
            }
            """,
            FixedCode = changedMarkup,
            CodeActionVerifier = Verify,
        };
        await refactoringTest.RunAsync();
        await codeFixTest.RunAsync();
        Assert.NotNull(codeAction);

        void Verify(CodeAction action, IVerifier _)
        {
            if (codeAction == null)
            {
                codeAction = action;
            }
            else
            {
                // verify that the same code actions don't show up twice
                Assert.Equal(codeAction.EquivalenceKey, action.EquivalenceKey);
            }
        }
    }

    [Fact]
    public async Task TestNoProperties_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    // field, not property
                    public int f = 0;
                }
            }
            """);
    }

    [Fact]
    public async Task TestPartialClass_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace N
            {
                public partial class [|C|]
                {
                    public int F { get; init; }
                }

                public partial class C
                {
                    public bool B { get; init; }
                }
            }
            """);
    }

    [Fact]
    public async Task TestExplicitProperty_NoAction1()
    {
        await TestNoRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    private int f;

                    public int P 
                    {
                        get => f; 
                        init => f = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestExplicitProperty_NoAction2()
    {
        await TestNoRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    private int f;

                    public int P 
                    {
                        get
                        {
                            return f;
                        }

                        init
                        {
                            f = value;
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestExplicitProperty_NoAction3()
    {
        await TestNoRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; } = 4;
                }
            }
            """);
    }

    [Fact]
    public async Task TestPrivateGetProperty_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { private get; init; }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSetProperty()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; set; }
                }
            }
            """, """
            namespace N
            {
                public record [|C|](int P);
            }
            """);
    }

    [Fact]
    public async Task TestInitPropertyOnStruct()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public struct [|C|]
                {
                    public int P { get; init; }
                }
            }
            """, """
            namespace N
            {
                public record struct [|C|](int P)
                {
                    public int P { get; init; } = P;
                }
            }
            """);
    }

    [Fact]
    public async Task TestPrivateSetProperty()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; private set; }
                }
            }
            """, """
            namespace N
            {
                public record [|C|](int P)
                {
                    public int P { get; private set; } = P;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMoveSimpleProperty()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                }
            }
            """, """
            namespace N
            {
                public record C(int P);
            }
            """);
    }

    [Fact]
    public async Task TestReadonlyProperty()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; }
                }
            }
            """, """
            namespace N
            {
                public record [|C|](int P);
            }
            """);
    }

    [Fact]
    public async Task TestInitPropertyOnReadonlyStruct()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public readonly struct [|C|]
                {
                    public int P { get; init; }
                }
            }
            """, """
            namespace N
            {
                public readonly record struct [|C|](int P);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertySimpleInheritance()
    {
        // three of the same error on C because the generated
        // EqualityConstract, Equals, and PrintMembers are all declared override
        // and there's nothing to override.
        // The other errors are copy constructor expected in B, and the
        // "records can't inherit from class" on B as well
        await TestRefactoringAsync("""
            namespace N
            {
                public class B
                {
                }

                public class [|C|] : B
                {
                    public int P { get; init; }
                }
            }
            """, """
            namespace N
            {
                public class B
                {
                }

                public record {|CS0115:{|CS0115:{|CS0115:{|CS8867:C|}|}|}|}(int P) : {|CS8864:B|};
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertySimpleRecordInheritance()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public record B
                {
                    public int Foo { get; init; }
                }

                public class [|C|] : {|CS8865:B|}
                {
                    public int P { get; init; }
                }
            }
            """, """
            namespace N
            {
                public record B
                {
                    public int Foo { get; init; }
                }

                public record C(int P) : B;
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertyPositionalParameterRecordInheritance()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public record B(int Foo, int Bar);

                public class [|{|CS1729:C|}|] : {|CS8865:B|}
                {
                    public int P { get; init; }
                }
            }
            """, """
            namespace N
            {
                public record B(int Foo, int Bar);

                public record C(int Foo, int Bar, int P) : B(Foo, Bar);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertyPositionalParameterRecordInheritanceWithComments()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                /// <summary> B </summary>
                /// <param name="Foo"> Foo is an int </param>
                /// <param name="Bar"> Bar is an int as well </param>
                public record B(int Foo, int Bar);

                /// <summary> C inherits from B </summary>
                public class [|{|CS1729:C|}|] : {|CS8865:B|}
                {
                    /// <summary> P can be initialized </summary>
                    public int P { get; init; }
                }
            }
            """, """
            namespace N
            {
                /// <summary> B </summary>
                /// <param name="Foo"> Foo is an int </param>
                /// <param name="Bar"> Bar is an int as well </param>
                public record B(int Foo, int Bar);

                /// <summary> C inherits from B </summary>
                /// <param name="Foo"><inheritdoc/></param>
                /// <param name="Bar"><inheritdoc/></param>
                /// <param name="P"> P can be initialized </param>
                public record C(int Foo, int Bar, int P) : B(Foo, Bar);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertyAndReorderWithPositionalParameterRecordInheritance()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public record B(int Foo, int Bar);

                public class [|C|] : {|CS8865:B|}
                {
                    public int P { get; init; }

                    public {|CS1729:C|}(int p, int bar, int foo)
                    {
                        P = p;
                        Bar = bar;
                        Foo = foo;
                    }
                }
            }
            """, """
            namespace N
            {
                public record B(int Foo, int Bar);

                public record C(int P, int Bar, int Foo) : B(Foo, Bar);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertySimpleInterfaceInheritance()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public interface IInterface
                {
                    public int Foo();
                }

                public class [|C|] : IInterface
                {
                    public int P { get; init; }

                    public int Foo()
                    {
                        return P;
                    }
                }
            }
            """, """
            namespace N
            {
                public interface IInterface
                {
                    public int Foo();
                }

                public record C(int P) : IInterface
                {
                    public int Foo()
                    {
                        return P;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMoveMultiplePropertiesWithInterfaceImplementation()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|] : IComparable
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public int CompareTo(object? other) => 0;
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B) : IComparable
                {
                    public int CompareTo(object? other) => 0;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMoveMultipleProperties()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMoveMultiplePropertiesOnStruct()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public struct [|C|]
                {
                    public int P { get; set; }
                    public bool B { get; set; }
                }
            }
            """, """
            namespace N
            {
                public record struct C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMoveMultiplePropertiesOnReadonlyStruct()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public readonly struct [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                public readonly record struct C(int P, bool B);
            }
            """);
    }

    // if there are both init and set properties, convert both but keep set property override
    [Fact]
    public async Task TestSetAndInitProperties()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; set; }

                    public int Q { get; init; }
                }
            }
            """, """
            namespace N
            {
                public record [|C|](int P, int Q)
                {
                    public int P { get; set; } = P;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMoveMultiplePropertiesOnGeneric()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C<TA, TB>|]
                {
                    public TA? P { get; init; }
                    public TB? B { get; init; }
                }
            }
            """, """
            namespace N
            {
                public record C<TA, TB>(TA? P, TB? B);
            }
            """);
    }

    [Fact]
    public async Task TestMoveMultiplePropertiesOnGenericWithConstraints()
    {
        await TestRefactoringAsync("""
            using System;
            using System.Collections.Generic;

            namespace N
            {
                public class [|C|]<TA, TB>
                    where TA : Exception
                    where TB : IEnumerable<TA>
                {
                    public TA? P { get; init; }
                    public TB? B { get; init; }
                }
            }
            """, """
            using System;
            using System.Collections.Generic;

            namespace N
            {
                public record C<TA, TB>(TA? P, TB? B) where TA : Exception
                        where TB : IEnumerable<TA>;
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithAttributes()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    [Obsolete("P is Obsolete", error: true)]
                    public int P { get; init; }

                    [Obsolete("B will be obsolete, error: false")]
                    public bool B { get; init; }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C([property: Obsolete("P is Obsolete", error: true)] int P, [property: Obsolete("B will be obsolete, error: false")] bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithAttributesAndComments1()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    // comment before
                    [Obsolete("P is Obsolete", error: true)]
                    public int P { get; init; }

                    [Obsolete("B will be obsolete, error: false")]
                    // comment after
                    public bool B { get; init; }
                }
            }
            """, """
            using System;

            namespace N
            {
                // comment before
                // comment after
                public record C([property: Obsolete("P is Obsolete", error: true)] int P, [property: Obsolete("B will be obsolete, error: false")] bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithAttributesAndComments2()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    [/*comment before*/ Obsolete("P is Obsolete", error: true)]
                    public int P { get; init; }

                    [Obsolete("B will be obsolete, error: false") /* comment after*/]
                    public bool B { get; init; }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C([/*comment before*/ property: Obsolete("P is Obsolete", error: true)] int P, [property: Obsolete("B will be obsolete, error: false") /* comment after*/] bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEquals1()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && otherC.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEqualsWithFields()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                    private int num = 10;

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && otherC.P == P && otherC.B == B && num == otherC.num;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    private int num = 10;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepSimpleEqualsWithConstFields()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                    private const int num = 10;

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && otherC.P == P && otherC.B == B && num == C.num;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    private const int num = 10;

                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        return other is C otherC && otherC.P == P && otherC.B == B && num == C.num;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEqualsWithConstAndStaticFieldsAndProps()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                    private const int num = 10;
                    public static int Foo = 100;
                    public static bool StaticProp { get; set; } = false;

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && otherC.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    private const int num = 10;
                    public static int Foo = 100;
                    public static bool StaticProp { get; set; } = false;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEquals2()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && P == otherC.P && B == otherC.B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteInvertedEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return !(other is not C otherC || otherC.P != P || otherC.B != B);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteEqualsDoubleComparison()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && otherC.P == P && otherC.B == B && otherC.P == P;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepEqualsMissingComparison()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && otherC.P == P;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        return other is C otherC && otherC.P == P;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepEqualsSelfComparison1()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && this.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        return other is C otherC && this.P == P && otherC.B == B;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepEqualsSelfComparison2()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && otherC.P == otherC.P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        return other is C otherC && otherC.P == otherC.P && otherC.B == B;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepEqualsWithSideEffect()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        Console.WriteLine("testing equals...");
                        return other is C otherC && otherC.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        Console.WriteLine("testing equals...");
                        return other is C otherC && otherC.P == P && otherC.B == B;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepEqualsIncorrectComparison()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public int B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return other is C otherC && otherC.P == B && otherC.B == P;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, int B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        return other is C otherC && otherC.P == B && otherC.B == P;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepWrongInvertedEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return !(other is C otherC && otherC.P == P && otherC.B == B);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        return !(other is C otherC && otherC.P == P && otherC.B == B);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepOrEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is C otherC)
                        {
                            return otherC.P == P || otherC.B == B;
                        }

                        return false;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        if (other is C otherC)
                        {
                            return otherC.P == P || otherC.B == B;
                        }

                        return false;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteIfCastEquals1()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is C otherC)
                        {
                            return otherC.P == P && otherC.B == B;
                        }

                        return false;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteIfCastEquals2()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is C otherC && otherC.P == P && otherC.B == B)
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteInvertedIfCastEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is not C otherC)
                        {
                            return false;
                        }

                        return otherC.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepWrongInvertedIfCastEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is C)
                        {
                            return false;
                        }

                        var otherC = {|CS8600:(C)other|};
                        return {|CS8602:otherC|}.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        if (other is C)
                        {
                            return false;
                        }

                        var otherC = {|CS8600:(C)other|};
                        return {|CS8602:otherC|}.P == P && otherC.B == B;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteIfThenCastEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is C)
                        {
                            var otherC = (C)other;
                            return otherC.P == P && otherC.B == B;
                        }

                        return false;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteIfChainEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is not C)
                        {
                            return false;
                        }

                        var otherC = (C)other;
                        if (P != otherC.P)
                        {
                            return false;
                        }

                        if (otherC.B != B)
                        {
                            return false;
                        }

                        return true;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteIfElseChainEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is not C)
                        {
                            return false;
                        }
                        else {
                            var otherC = (C)other;
                            if (P != otherC.P)
                            {
                                return false;
                            }
                            else if (otherC.B != B)
                            {
                                return false;
                            }

                            return true;
                        }
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteInvertedIfChainEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        if (other is C)
                        {
                            var otherC = (C)other;
                            if (otherC.P == P)
                            {
                                if (otherC.B == B)
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteAsCastEquals()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        var otherC = other as C;
                        return otherC != null && otherC.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteAsCastEqualsWithIsNotNull()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        var otherC = other as C;
                        return otherC is not null && otherC.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepAsCastEqualsWithIncorrectIsNull()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        var otherC = other as C;
                        return otherC is null && {|CS8602:otherC|}.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        var otherC = other as C;
                        return otherC is null && {|CS8602:otherC|}.P == P && otherC.B == B;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteAsCastEqualsWithIsNull()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        var otherC = other as C;
                        return !(otherC is null || otherC.P != P || otherC.B != B);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleTypeEquals()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|] : IEquatable<C>
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public bool Equals(C? otherC)
                    {
                        return {|CS8602:otherC|}.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleTypeEqualsWithAdditionalInterface()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|] : IEquatable<C>, IComparable
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public bool Equals(C? otherC)
                    {
                        return {|CS8602:otherC|}.P == P && otherC.B == B;
                    }

                    public int CompareTo(object? other) => 0;
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B) : IComparable
                {
                    public int CompareTo(object? other) => 0;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleTypeEqualsAndObjectEquals()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|] : IEquatable<C>
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public bool Equals(C? otherC)
                    {
                        return {|CS8602:otherC|}.P == P && otherC.B == B;
                    }

                    public override bool Equals(object? other)
                    {
                        return Equals(other as C);
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteTypeEqualsIfChain()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|] : IEquatable<C>
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public bool Equals(C? otherC)
                    {
                        if (otherC == null)
                        {
                            return false;
                        }

                        if (P != otherC.P)
                        {
                            return false;
                        }

                        if (otherC.B != B)
                        {
                            return false;
                        }

                        return true;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteObjectAndTypeEquals()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|] : IEquatable<C>
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return Equals(other as C);
                    }

                    public bool Equals(C? otherC)
                    {
                        return otherC is not null && otherC.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepIncorrectObjectAndDeleteCorrectTypeEquals()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|] : IEquatable<C>
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override bool Equals(object? other)
                    {
                        return Foo(other as C);
                    }

                    public bool Foo(C? c)
                    {
                        return c?.B ?? false;
                    }

                    public bool Equals(C? otherC)
                    {
                        return otherC is not null && otherC.P == P && otherC.B == B;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public override bool {|CS0111:Equals|}(object? other)
                    {
                        return Foo(other as C);
                    }

                    public bool Foo(C? c)
                    {
                        return c?.B ?? false;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteHashCode1()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override int GetHashCode()
                    {
                        var hashCode = 339610899;
                        hashCode = hashCode * -1521134295 + P.GetHashCode();
                        hashCode = hashCode * -1521134295 + B.GetHashCode();
                        return hashCode;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteHashCode2()
    {
        await TestRefactoringAsync("""
            using System.Collections.Generic;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override int GetHashCode()
                    {
                        var hashCode = 339610899;
                        hashCode = hashCode * -1521134295 + P.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(B);
                        return hashCode;
                    }
                }
            }
            """, """
            using System.Collections.Generic;

            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepComplexHashCode()
    {
        await TestRefactoringAsync("""
            using System;
            using System.Collections.Generic;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public override int GetHashCode()
                    {
                        var hashCode = 339610899;
                        Console.WriteLine("This could potentially be a side effect");
                        hashCode = hashCode * -1521134295 + P.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(B);
                        return hashCode;
                    }
                }
            }
            """, """
            using System;
            using System.Collections.Generic;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public override int GetHashCode()
                    {
                        var hashCode = 339610899;
                        Console.WriteLine("This could potentially be a side effect");
                        hashCode = hashCode * -1521134295 + P.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(B);
                        return hashCode;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam1()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object? c2) {
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object? c2) {
                        return !(c1 == c2);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam2()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object? c2) {
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object? c2) {
                        return !c1.Equals(c2);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam3()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object? c2) {
                        return c2!.Equals(c1);
                    }

                    public static bool operator !=(C c1, object? c2) {
                        return !(c2 == c1);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithObjectParam()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object c2) {
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object c2) {
                        return !(c1 == c2);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteEqualOperatorsWithExpressionBodies()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object c2)
                        => c1.Equals(c2);

                    public static bool operator !=(C c1, object c2)
                        => !(c1 == c2);
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithSameTypeParams()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, C c2) {
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, C c2) {
                        return !(c1 == c2);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableTypeParams()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C? c1, C? c2) {
                        return c1!.Equals(c2);
                    }

                    public static bool operator !=(C? c1, C? c2) {
                        return !(c1 == c2);
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepSideEffectOperator1()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object? c2)
                    {
                        Console.WriteLine("checking equality");
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object? c2)
                    {
                        return !(c1 == c2);
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public static bool operator ==(C c1, object? c2)
                    {
                        Console.WriteLine("checking equality");
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object? c2)
                    {
                        return !(c1 == c2);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepSideEffectOperator2()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object? c2)
                    {
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object? c2)
                    {
                        Console.WriteLine("checking equality");
                        return !(c1 == c2);
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public static bool operator ==(C c1, object? c2)
                    {
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object? c2)
                    {
                        Console.WriteLine("checking equality");
                        return !(c1 == c2);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepSideEffectOperator_WhenSameParamUsed1()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object? c2)
                    {
                        return c1.Equals(c1);
                    }

                    public static bool operator !=(C c1, object? c2)
                    {
                        return !(c1 == c2);
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public static bool operator ==(C c1, object? c2)
                    {
                        return c1.Equals(c1);
                    }

                    public static bool operator !=(C c1, object? c2)
                    {
                        return !(c1 == c2);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepSideEffectOperator_WhenSameParamUsed2()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static bool operator ==(C c1, object? c2)
                    {
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object? c2)
                    {
                        return !(c1 == c1);
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public static bool operator ==(C c1, object? c2)
                    {
                        return c1.Equals(c2);
                    }

                    public static bool operator !=(C c1, object? c2)
                    {
                        return !(c1 == c1);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteClone()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C Clone()
                    {
                        return this;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleCopyConstructor()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(C other)
                    {
                        P = other.P;
                        B = other.B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteCopyConstructorWithFields()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                    int foo = 0;

                    public C(C other)
                    {
                        P = other.P;
                        B = other.B;
                        foo = other.foo;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    int foo = 0;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteCopyConstructorWithConstAndStaticFieldsAndProps()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                    const int foo = 0;
                    public static int statFoo = 10;
                    public static bool StaticProp { get; set; } = false;

                    public C(C other)
                    {
                        P = other.P;
                        B = other.B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    const int foo = 0;
                    public static int statFoo = 10;
                    public static bool StaticProp { get; set; } = false;
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepCopyConstructorWithoutFieldAccess()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                    private int foo = 0;

                    public C(C other)
                    {
                        P = other.P;
                        B = other.B;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    private int foo = 0;

                    public C(C other)
                    {
                        P = other.P;
                        B = other.B;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimplePrimaryConstructor()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(int p, bool b)
                    {
                        P = p;
                        B = b;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDeleteSimpleExpressionPrimaryConstructor()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }

                    public C(int p)
                        => P = p;
                }
            }
            """, """
            namespace N
            {
                public record C(int P);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndModifyOrderFromPrimaryConstructor()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int p)
                    {
                        P = p;
                        B = b;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(bool B, int P);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndModifyPrimaryConstructorOrderAndDefaults()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b = false, int p = 0)
                    {
                        P = p;
                        B = b;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(bool B = false, int P = 0);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesWithOperators()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(bool b1, bool b2, bool b3)
                    {
                        P = b1 ? 1 : 0;
                        B = !b2 == b3;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(bool B, int P)
                {
                    public C(bool b1, bool b2, bool b3) : this(!b2 == b3, b1 ? 1 : 0)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesWithStaticMemberAndInvocation()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public static class Stuff
                {
                    public static bool GetB(bool b1, bool b2)
                    {
                        return b1 || b2;
                    }
                }

                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                    public static int DefaultP { get; set; } = 10;

                    public C(bool b1, bool b2, bool b3)
                    {
                        P = b3 ? DefaultP : 0;
                        B = Stuff.GetB(b1, b2);
                    }
                }
            }
            """, """
            namespace N
            {
                public static class Stuff
                {
                    public static bool GetB(bool b1, bool b2)
                    {
                        return b1 || b2;
                    }
                }

                public record C(int P, bool B)
                {
                    public static int DefaultP { get; set; } = 10;

                    public C(bool b1, bool b2, bool b3) : this(b3 ? DefaultP : 0, Stuff.GetB(b1, b2))
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesWithReferences()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public record R(int Foo, int Bar)
                {
                    public int field = 10;

                    public bool IsBarNum(int num)
                    {
                        return Bar == num;
                    }
                }

                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(R r)
                    {
                        P = r.field;
                        B = r.IsBarNum(r.Foo);
                    }
                }
            }
            """, """
            namespace N
            {
                public record R(int Foo, int Bar)
                {
                    public int field = 10;

                    public bool IsBarNum(int num)
                    {
                        return Bar == num;
                    }
                }

                public record C(bool B, int P)
                {
                    public C(R r) : this(r.IsBarNum(r.Foo), r.field)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesWithNullOperations()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public record R(int? Foo, int Bar)
                {
                    public int field = 10;

                    public bool IsBarNum(int num)
                    {
                        return Bar == num;
                    }
                }

                public class [|C|]
                {
                    public int P { get; init; }
                    public bool? B { get; init; }

                    public C(bool? b, int p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(R? r, int backup)
                    {
                        P = r?.Foo ?? 10;
                        B = r?.IsBarNum(backup);
                    }
                }
            }
            """, """
            namespace N
            {
                public record R(int? Foo, int Bar)
                {
                    public int field = 10;

                    public bool IsBarNum(int num)
                    {
                        return Bar == num;
                    }
                }

                public record C(bool? B, int P)
                {
                    public C(R? r, int backup) : this(r?.IsBarNum(backup), r?.Foo ?? 10)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesWithIsExpressions()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(object b1, bool b2, object b3)
                    {
                        P = b1 is int ? 1 : 0;
                        B = !b2 && b3 is C { P: 10 };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(bool B, int P)
                {
                    public C(object b1, bool b2, object b3) : this(!b2 && b3 is C { P: 10 }, b1 is int ? 1 : 0)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesWithSwitchExpressions()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(int f1, bool b2, bool b3)
                    {
                        P = f1 switch
                        {
                            1 => 0,
                            0 => 1,
                            _ => default
                        };
                        B = !b2 && b3;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(bool B, int P)
                {
                    public C(int f1, bool b2, bool b3) : this(!b2 && b3, f1 switch
                    {
                        1 => 0,
                        0 => 1,
                        _ => default
                    })
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesWithSideEffects()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(bool b1, bool b2)
                    {
                        P = b1 ? 1 : 0;
                        Console.WriteLine("Side effect");
                        B = !b2;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(bool B, int P)
                {
                    public C(bool b1, bool b2) : this(!b2, b1 ? 1 : 0)
                    {
                        Console.WriteLine("Side effect");
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesComplex()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(bool b1, bool b2)
                    {
                        P = b1 ? 1 : 0;
                        var b = !b2;
                        B = b;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(bool B, int P)
                {
                    public C(bool b1, bool b2) : this(default, b1 ? 1 : 0)
                    {
                        var b = !b2;
                        B = b;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerValuesPatternVariable()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(bool b1, object b2)
                    {
                        P = b1 ? 1 : 0;
                        B = b2 switch
                        {
                            C cb2 => cb2.B,
                            _ => false
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(bool B, int P)
                {
                    public C(bool b1, object b2) : this(default, b1 ? 1 : 0)
                    {
                        B = b2 switch
                        {
                            C cb2 => cb2.B,
                            _ => false
                        };
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndProvideThisInitializerDefaultAndNull()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int? P { get; init; }
                    public bool B { get; init; }

                    public C(bool b, int? p)
                    {
                        P = p;
                        B = b;
                    }

                    public C(bool b1, bool b2)
                    {
                        var b = !b2 || b2;
                        B = b;
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(bool B, int? P)
                {
                    public C(bool b1, bool b2) : this(default, null)
                    {
                        var b = !b2 || b2;
                        B = b;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepComplexPrimaryConstructor1()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(int p, bool b)
                    {
                        Console.WriteLine("Constructing C...");
                        P = p;
                        B = b;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public {|CS0111:{|CS8862:C|}|}(int p, bool b)
                    {
                        Console.WriteLine("Constructing C...");
                        P = p;
                        B = b;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepComplexPrimaryConstructor2()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(int p, bool b)
                    {
                        P = p + 1;
                        B = b;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public {|CS0111:{|CS8862:C|}|}(int p, bool b)
                    {
                        P = p + 1;
                        B = b;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndKeepComplexPrimaryConstructor3()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(int p, bool b)
                    {
                        B = b;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(int P, bool B)
                {
                    public {|CS0111:{|CS8862:C|}|}(int p, bool b)
                    {
                        B = b;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithMultiplePotentialPrimaryConstructors()
    {
        await TestRefactoringAsync("""
            using System;

            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(int p, bool b)
                    {
                        B = b;
                    }

                    public C(bool b, int p)
                    {
                        B = b;
                        P = p;
                    }
                }
            }
            """, """
            using System;

            namespace N
            {
                public record C(bool B, int P)
                {
                    public C(int p, bool b) : this(b, default)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithSimpleDocComments()
    {
        await TestRefactoringAsync("""
            namespace N
            {

                /// <summary>
                /// some summary
                /// </summary>
                public class [|C|]
                {

                    /// <summary>
                    /// P is an int
                    /// </summary>
                    public int P { get; init; }

                    /// <summary>
                    /// B is a bool
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /// <summary>
                /// some summary
                /// </summary>
                /// <param name="P"> P is an int </param>
                /// <param name="B"> B is a bool </param>
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithMultilineDocComments()
    {
        // this is what it should be
        //            var changedMarkup = @"
        //namespace N
        //{
        //    /** 
        //     * <summary>
        //     * some summary
        //     * </summary>
        //     * <param name=""P""> P is an int </param>
        //     * <param name=""B""> B is a bool </param>
        //     */
        //    public record C(int P, bool B);
        //}
        //";

        // this is what it is currently
        await TestRefactoringAsync("""
            namespace N
            {

                /** 
                 * <summary>
                 * some summary
                 * </summary>
                 */
                public class [|C|]
                {

                    /** 
                     * <summary>
                     * P is an int
                     * </summary>
                     */
                    public int P { get; init; }

                    /** 
                     * <summary>
                     * B is a bool
                     * </summary>
                     */
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /** 
                     * <summary>
                     * some summary
                     * </summary>
                     * <param name="P"> P is an int </param>
                     * <param name="B"> B is a bool </param>
                     */
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithMultilineDocComments_NoClassSummary()
    {
        // this is what it should be
        //            var changedMarkup = @"
        //namespace N
        //{
        //    /**
        //     * <param name=""P""> P is an int </param>
        //     * <param name=""B""> B is a bool </param>
        //     */
        //    public record C(int P, bool B);
        //}
        //";
        // this is what it is currently
        await TestRefactoringAsync("""
            namespace N
            {

                public class [|C|]
                {

                    /** 
                     * <summary>
                     * P is an int
                     * </summary>
                     */
                    public int P { get; init; }

                    /** 
                     * <summary>
                     * B is a bool
                     * </summary>
                     */
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /**
                         *<param name="P"> P is an int </param>
                         * <param name="B"> B is a bool </param>
                */
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithMixedDocComments1()
    {
        await TestRefactoringAsync("""
            namespace N
            {

                /// <summary>
                /// some summary
                /// </summary>
                public class [|C|]
                {

                    /** 
                     * <summary>
                     * P is an int
                     * </summary>
                     */
                    public int P { get; init; }

                    /** 
                     * <summary>
                     * B is a bool
                     * </summary>
                     */
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /// <summary>
                /// some summary
                /// </summary>
                /// <param name="P"> P is an int </param>
                /// <param name="B"> B is a bool </param>
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithMixedDocComments2()
    {
        // class-level comment should be default
        // This is what it should be
        //            var changedMarkup = @"
        //namespace N
        //{
        //    /** 
        //     * <summary>
        //     * some summary
        //     * </summary>
        //     * <param name=""P""> P is an int </param>
        //     * <param name=""B""> B is a bool </param>
        //     */
        //    public record C(int P, bool B);
        //}
        //";

        // this is what it is right now
        await TestRefactoringAsync("""
            namespace N
            {

                /** 
                 * <summary>
                 * some summary
                 * </summary>
                 */
                public class [|C|]
                {

                    /// <summary>
                    /// P is an int
                    /// </summary>
                    public int P { get; init; }

                    /// <summary>
                    /// B is a bool
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /** 
                     * <summary>
                     * some summary
                     * </summary>
                     * <param name="P"> P is an int </param>
                     * <param name="B"> B is a bool </param>
                     */
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithMixedDocComments3()
    {
        // this is what it should be
        //            var changedMarkup = @"
        //namespace N
        //{
        //    /** 
        //     * <summary>
        //     * some summary
        //     * </summary>
        //     * <param name=""P""> P is an int
        //     * with a multiline comment </param>
        //     * <param name=""B""> B is a bool </param>
        //     */
        //    public record C(int P, bool B);
        //}
        //";

        // this is what it actually is
        await TestRefactoringAsync("""
            namespace N
            {

                /** 
                 * <summary>
                 * some summary
                 * </summary>
                 */
                public class [|C|]
                {

                    /// <summary>
                    /// P is an int
                    /// with a multiline comment
                    /// </summary>
                    public int P { get; init; }

                    /// <summary>
                    /// B is a bool
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /** 
                     * <summary>
                     * some summary
                     * </summary>
                     * <param name="P"> P is an int
                     * with a multiline comment </param>
                     * <param name="B"> B is a bool </param>
                     */
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithDocComments_NoClassSummary()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {

                    /// <summary>
                    /// P is an int
                    /// </summary>
                    public int P { get; init; }

                    /// <summary>
                    /// B is a bool
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /// <param name="P"> P is an int </param>
                /// <param name="B"> B is a bool </param>
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithDocComments_MissingPropertySummary()
    {
        await TestRefactoringAsync("""
            namespace N
            {

                /// <summary>
                /// some summary
                /// </summary>
                public class [|C|]
                {
                    public int P { get; init; }

                    /// <summary>
                    /// B is a bool
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /// <summary>
                /// some summary
                /// </summary>
                /// <param name="P"></param>
                /// <param name="B"> B is a bool </param>
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithDocComments_AdditionalClassSection()
    {
        await TestRefactoringAsync("""
            namespace N
            {

                /// <summary>
                /// some summary
                /// </summary>
                /// <remarks>
                /// Some remarks as well
                /// </reamrks>
                public class [|C|]
                {

                    /// <summary>
                    /// P is an int
                    /// </summary>
                    public int P { get; init; }

                    /// <summary>
                    /// B is a bool
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /// <summary>
                /// some summary
                /// </summary>
                /// <param name="P"> P is an int </param>
                /// <param name="B"> B is a bool </param>
                /// <remarks>
                /// Some remarks as well
                /// </reamrks>
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithDocComments_NestedPropertyElements()
    {
        await TestRefactoringAsync("""
            namespace N
            {

                /// <summary>
                /// some summary
                /// </summary>
                public class [|C|]
                {

                    /// <summary>
                    /// P is an int <see cref="C.B"/>
                    /// </summary>
                    public int P { get; init; }

                    /// <summary>
                    /// B is a bool
                    /// <c> Some code text </c>
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /// <summary>
                /// some summary
                /// </summary>
                /// <param name="P"> P is an int <see cref="C.B"/> </param>
                /// <param name="B"> B is a bool
                /// <c> Some code text </c> </param>
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithDocAndNonDocComments1()
    {
        await TestRefactoringAsync("""
            namespace N
            {

                // Non-Doc comment before class
                /// <summary>
                /// some summary
                /// </summary>
                public class [|C|]
                {

                    // Non-Doc property comment for P
                    /// <summary>
                    /// P is an int
                    /// </summary>
                    public int P { get; init; }

                    // Non-Doc property comment for B
                    /// <summary>
                    /// B is a bool
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                // Non-Doc comment before class
                // Non-Doc property comment for P
                // Non-Doc property comment for B
                /// <summary>
                /// some summary
                /// </summary>
                /// <param name="P"> P is an int </param>
                /// <param name="B"> B is a bool </param>
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesWithDocAndNonDocComments2()
    {
        await TestRefactoringAsync("""
            namespace N
            {

                /// <summary>
                /// some summary
                /// </summary>
                // Non-Doc comment after class
                public class [|C|]
                {

                    // Non-Doc property comment for P
                    /// <summary>
                    /// P is an int
                    /// </summary>
                    public int P { get; init; }

                    // Non-Doc property comment for B
                    /// <summary>
                    /// B is a bool
                    /// </summary>
                    public bool B { get; init; }
                }
            }
            """, """
            namespace N
            {
                /// <summary>
                /// some summary
                /// </summary>
                /// <param name="P"> P is an int </param>
                /// <param name="B"> B is a bool </param>
                // Non-Doc comment after class
                // Non-Doc property comment for P
                // Non-Doc property comment for B
                public record C(int P, bool B);
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializer()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0,
                            B = false
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);

                public static class D
                {
                    public static C GetC()
                    {
                        return new C(0, false);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerWithNullableReferenceTypes()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public C? Node { get; init; }
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, C? Node);

                public static class D
                {
                    public static C GetC()
                    {
                        return new C(0, null);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerWithNullableValueTypes()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool? B { get; init; }
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool? B);

                public static class D
                {
                    public static C GetC()
                    {
                        return new C(0, null);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerInSameClass1()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0,
                            B = false
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public static C GetC()
                    {
                        return new C(0, false);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerInSameClass2()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public C? Node { get; init; }

                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0,
                            Node = new C
                            {
                                P = 1,
                                Node = null,
                            }
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, C? Node)
                {
                    public static C GetC()
                    {
                        return new C(0, new C(1, null));
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerInSameClass3()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public static C Default = new C
                    {
                        P = 0,
                        B = true,
                    };
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public static C Default = new C(0, true);
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerWithNestedInitializers()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public record Foo
                {
                    public int Bar { get; init; }
                }

                public class [|C|]
                {
                    public int P { get; init; }
                    public Foo? B { get; init; }

                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0,
                            B = new Foo { Bar = 0 }
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record Foo
                {
                    public int Bar { get; init; }
                }

                public record C(int P, Foo? B)
                {
                    public static C GetC()
                    {
                        return new C(0, new Foo { Bar = 0 });
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerKeepSomeProperties()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public int I { get; set; } = 4;
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0,
                            B = false,
                            I = 10,
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public int I { get; set; } = 4;
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new C(0, false)
                        {
                            I = 10,
                        };
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerWithDefault()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0,
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);

                public static class D
                {
                    public static C GetC()
                    {
                        return new C(0, default);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndDoNotRefactorInitializerWithExistingConstructor()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }

                    public C(int p)
                    {
                        P = p;
                        B = false;
                    }
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new C(0)
                        {
                            B = true,
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B)
                {
                    public C(int p) : this(p, false)
                    {
                    }
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new C(0)
                        {
                            B = true,
                        };
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMovePropertiesAndRefactorInitializerInSeparateFile()
    {
        var initialMarkup1 = """
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                }
            }
            """;
        var initialMarkup2 = """
            using N;

            namespace N2
            {
                public static class D
                {
                    public static C GetC()
                    {
                        return new C
                        {
                            P = 0,
                            B = true,
                        };
                    }
                }
            }
            """;
        var changedMarkup1 = """
            namespace N
            {
                public record C(int P, bool B);
            }
            """;
        var changedMarkup2 = """
            using N;

            namespace N2
            {
                public static class D
                {
                    public static C GetC()
                    {
                        return new C(0, true);
                    }
                }
            }
            """;
        await new RefactoringTest
        {
            TestState =
            {
                Sources =
                {
                    initialMarkup1,
                    initialMarkup2
                }
            },
            FixedState =
            {
                Sources =
                {
                    changedMarkup1,
                    changedMarkup2
                }
            }
        }.RunAsync();
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1932546")]
    public async Task TestInvalidObjectCreation()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; }
                    public bool B { get; init; }
                }

                public static class D
                {
                    public static C GetC()
                    {
                        return new {|CS0246:IDoesNotExist|}
                        {
                            P = 0,
                            B = false
                        };
                    }
                }
            }
            """, """
            namespace N
            {
                public record C(int P, bool B);

                public static class D
                {
                    public static C GetC()
                    {
                        return new {|CS0246:IDoesNotExist|}
                        {
                            P = 0,
                            B = false
                        };
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72032")]
    public async Task TestConstructorWithoutBody()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; private set; }

                    public extern C();
                }
            }
            """, """
            namespace N
            {
                public record [|C|](int P)
                {
                    public int P { get; private set; } = P;
            
                    public extern C();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72067")]
    public Task TestMovePropertiesAndRefactorInitializer_SourceGeneratedDocuments()
        => new RefactoringTestWithGenerator
        {
            TestCode = """
                namespace N
                {
                    public class [|C|]
                    {
                        public int P { get; init; }
                        public bool B { get; init; }
                    }
                }
                """,
            FixedState =
            {
                Sources =
                {
                    """
                    namespace N
                    {
                        public record C(int P, bool B);
                    }
                    """
                },
                ExpectedDiagnostics =
                {
                    // Microsoft.CodeAnalysis.CSharp.Features.UnitTests\Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRecord.ConvertToRecordCodeRefactoringTests+ConvertToRecordTestGenerator\file.cs(7,24): error CS7036: There is no argument given that corresponds to the required parameter 'P' of 'C.C(int, bool)'
                    DiagnosticResult.CompilerError("CS7036").WithSpan(@"Microsoft.CodeAnalysis.CSharp.Features.UnitTests\Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRecord.ConvertToRecordCodeRefactoringTests+ConvertToRecordTestGenerator\file.cs", 7, 24, 7, 25).WithArguments("P", "N.C.C(int, bool)"),
                }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78664")]
    public async Task TestDoesNotCrashOnAbstractMethod()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public abstract class [|C|]
                {
                    public string? S { get; set; }

                    public abstract System.Guid F();
                }
            }
            """, """
            namespace N
            {
                public abstract record C(string? S)
                {
                    public abstract System.Guid F();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78664")]
    public async Task TestDoesNotCrashOnAbstractCloneMethod()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public abstract class [|C|]
                {
                    public string? S { get; set; }

                    public abstract object Clone();
                }
            }
            """, """
            namespace N
            {
                public abstract record C(string? S);
            }
            """);
    }

#pragma warning disable RS1042 // Do not implement
    private sealed class ConvertToRecordTestGenerator : ISourceGenerator
#pragma warning restore RS1042 // Do not implement
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(
                "file.cs",
                """
                namespace N
                {
                    public static class D
                    {
                        public static C GetC()
                        {
                            return new C
                            {
                                P = 0,
                                B = false
                            };
                        }
                    }
                }
                """);
        }
    }

    #region selection

    [Fact]
    public async Task TestSelectOnProperty_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace N
            {
                public class C
                {
                    public int [|P|] { get; init; }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSelectOnNamespace_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace [|N|]
            {
                public class C
                {
                    public int P { get; init; }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSelectLargeRegionIncludingNamespace_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace [|N
            {
                public clas|]s C
                {
                    public int P { get; init; }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSelectMultipleMembersWithinClass()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class C
                {
                    [|public int P { get; init; }

                    public int Foo()
                    {
                        return 0;
                    }|]
                }
            }
            """, """
            namespace N
            {
                public record C(int P)
                {
                    public int Foo()
                    {
                        return 0;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSelectRegionIncludingClass()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public class [|C
                {
                    public int P { get; init; }|]
                }
            }
            """, """
            namespace N
            {
                public record C(int P);
            }
            """);
    }

    [Fact]
    public async Task TestSelectClassKeyword()
    {
        await TestRefactoringAsync("""
            namespace N
            {
                public cl[||]ass C
                {
                    public int P { get; init; }
                }
            }
            """, """
            namespace N
            {
                public record C(int P);
            }
            """);
    }

    [Fact]
    public async Task TestSelectBaseClassItem_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace N
            {
                public class B {}

                public class C : [|B|]
                {
                    public int P { get; init; }
                }
            }
            """);
    }

    #endregion

    private class RefactoringTest : VerifyCSRefactoring.Test
    {
        public RefactoringTest()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
            LanguageVersion = LanguageVersion.CSharp10;
            MarkupOptions = MarkupOptions.UseFirstDescriptor;
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();

            // enable nullable
            compilationOptions = compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable);

            return compilationOptions;
        }
    }

    private sealed class RefactoringTestWithGenerator : RefactoringTest
    {
        protected override IEnumerable<Type> GetSourceGenerators()
        {
            yield return typeof(ConvertToRecordTestGenerator);
        }
    }

    private static async Task TestRefactoringAsync(
        string initialMarkup,
        string changedMarkup)
    {
        var test = new RefactoringTest()
        {
            TestCode = initialMarkup,
            FixedCode = changedMarkup,
        };
        await test.RunAsync();
    }

    private static Task TestNoRefactoringAsync(string initialMarkup)
        => TestRefactoringAsync(initialMarkup, initialMarkup);

    private sealed class CodeFixTest :
        CSharpCodeFixVerifier<TestAnalyzer, CSharpConvertToRecordCodeFixProvider>.Test
    {
        public CodeFixTest()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
            LanguageVersion = LanguageVersion.CSharp10;
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();

            // enable nullable
            compilationOptions = compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable);

            return compilationOptions;
        }
    }

    private sealed class TestAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => [new DiagnosticDescriptor(
                "CS8865",
                "Only records may inherit from records.",
                "Only records may inherit from records.",
                "Compiler error",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true)];

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}
