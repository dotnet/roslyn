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
    public Task TestNoProperties_NoAction()
        => TestNoRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    // field, not property
                    public int f = 0;
                }
            }
            """);

    [Fact]
    public Task TestPartialClass_NoAction()
        => TestNoRefactoringAsync("""
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

    [Fact]
    public Task TestExplicitProperty_NoAction1()
        => TestNoRefactoringAsync("""
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

    [Fact]
    public Task TestExplicitProperty_NoAction2()
        => TestNoRefactoringAsync("""
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

    [Fact]
    public Task TestExplicitProperty_NoAction3()
        => TestNoRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { get; init; } = 4;
                }
            }
            """);

    [Fact]
    public Task TestPrivateGetProperty_NoAction()
        => TestNoRefactoringAsync("""
            namespace N
            {
                public class [|C|]
                {
                    public int P { private get; init; }
                }
            }
            """);

    [Fact]
    public Task TestSetProperty()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestInitPropertyOnStruct()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestPrivateSetProperty()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMoveSimpleProperty()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestReadonlyProperty()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestInitPropertyOnReadonlyStruct()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertySimpleInheritance()
        => TestRefactoringAsync("""
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

                public record C(int P) : {|CS8864:B|};
            }
            """);

    [Fact]
    public Task TestMovePropertySimpleRecordInheritance()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertyPositionalParameterRecordInheritance()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertyPositionalParameterRecordInheritanceWithComments()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertyAndReorderWithPositionalParameterRecordInheritance()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertySimpleInterfaceInheritance()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMoveMultiplePropertiesWithInterfaceImplementation()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMoveMultipleProperties()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMoveMultiplePropertiesOnStruct()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMoveMultiplePropertiesOnReadonlyStruct()
        => TestRefactoringAsync("""
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

    // if there are both init and set properties, convert both but keep set property override
    [Fact]
    public Task TestSetAndInitProperties()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMoveMultiplePropertiesOnGeneric()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMoveMultiplePropertiesOnGenericWithConstraints()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithAttributes()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithAttributesAndComments1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithAttributesAndComments2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEquals1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEqualsWithFields()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepSimpleEqualsWithConstFields()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEqualsWithConstAndStaticFieldsAndProps()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEquals2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteInvertedEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteEqualsDoubleComparison()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepEqualsMissingComparison()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepEqualsSelfComparison1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepEqualsSelfComparison2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepEqualsWithSideEffect()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepEqualsIncorrectComparison()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepWrongInvertedEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepOrEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteIfCastEquals1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteIfCastEquals2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteInvertedIfCastEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepWrongInvertedIfCastEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteIfThenCastEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteIfChainEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteIfElseChainEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteInvertedIfChainEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteAsCastEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteAsCastEqualsWithIsNotNull()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepAsCastEqualsWithIncorrectIsNull()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteAsCastEqualsWithIsNull()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleTypeEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleTypeEqualsWithAdditionalInterface()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleTypeEqualsAndObjectEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteTypeEqualsIfChain()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteObjectAndTypeEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepIncorrectObjectAndDeleteCorrectTypeEquals()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteHashCode1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteHashCode2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepComplexHashCode()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam3()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithObjectParam()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteEqualOperatorsWithExpressionBodies()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithSameTypeParams()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableTypeParams()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepSideEffectOperator1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepSideEffectOperator2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepSideEffectOperator_WhenSameParamUsed1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepSideEffectOperator_WhenSameParamUsed2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteClone()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleCopyConstructor()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteCopyConstructorWithFields()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteCopyConstructorWithConstAndStaticFieldsAndProps()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepCopyConstructorWithoutFieldAccess()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimplePrimaryConstructor()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDeleteSimpleExpressionPrimaryConstructor()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndModifyOrderFromPrimaryConstructor()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndModifyPrimaryConstructorOrderAndDefaults()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesWithOperators()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesWithStaticMemberAndInvocation()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesWithReferences()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesWithNullOperations()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesWithIsExpressions()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesWithSwitchExpressions()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesWithSideEffects()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesComplex()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerValuesPatternVariable()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndProvideThisInitializerDefaultAndNull()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepComplexPrimaryConstructor1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepComplexPrimaryConstructor2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndKeepComplexPrimaryConstructor3()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithMultiplePotentialPrimaryConstructors()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithSimpleDocComments()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithMultilineDocComments()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithMultilineDocComments_NoClassSummary()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithMixedDocComments1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithMixedDocComments2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithMixedDocComments3()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithDocComments_NoClassSummary()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithDocComments_MissingPropertySummary()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithDocComments_AdditionalClassSection()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithDocComments_NestedPropertyElements()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithDocAndNonDocComments1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesWithDocAndNonDocComments2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializer()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializerWithNullableReferenceTypes()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializerWithNullableValueTypes()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializerInSameClass1()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializerInSameClass2()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializerInSameClass3()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializerWithNestedInitializers()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializerKeepSomeProperties()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndRefactorInitializerWithDefault()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestMovePropertiesAndDoNotRefactorInitializerWithExistingConstructor()
        => TestRefactoringAsync("""
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
    public Task TestInvalidObjectCreation()
        => TestRefactoringAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72032")]
    public Task TestConstructorWithoutBody()
        => TestRefactoringAsync("""
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
    public Task TestDoesNotCrashOnAbstractMethod()
        => TestRefactoringAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78664")]
    public Task TestDoesNotCrashOnAbstractCloneMethod()
        => TestRefactoringAsync("""
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
    public Task TestSelectOnProperty_NoAction()
        => TestNoRefactoringAsync("""
            namespace N
            {
                public class C
                {
                    public int [|P|] { get; init; }
                }
            }
            """);

    [Fact]
    public Task TestSelectOnNamespace_NoAction()
        => TestNoRefactoringAsync("""
            namespace [|N|]
            {
                public class C
                {
                    public int P { get; init; }
                }
            }
            """);

    [Fact]
    public Task TestSelectLargeRegionIncludingNamespace_NoAction()
        => TestNoRefactoringAsync("""
            namespace [|N
            {
                public clas|]s C
                {
                    public int P { get; init; }
                }
            }
            """);

    [Fact]
    public Task TestSelectMultipleMembersWithinClass()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestSelectRegionIncludingClass()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestSelectClassKeyword()
        => TestRefactoringAsync("""
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

    [Fact]
    public Task TestSelectBaseClassItem_NoAction()
        => TestNoRefactoringAsync("""
            namespace N
            {
                public class B {}

                public class C : [|B|]
                {
                    public int P { get; init; }
                }
            }
            """);

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
