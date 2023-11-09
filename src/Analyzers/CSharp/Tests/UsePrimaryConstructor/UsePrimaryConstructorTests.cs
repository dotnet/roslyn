﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UsePrimaryConstructor;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUsePrimaryConstructorDiagnosticAnalyzer,
    CSharpUsePrimaryConstructorCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUsePrimaryConstructor)]
public partial class UsePrimaryConstructorTests
{
    [Fact]
    public async Task TestInCSharp12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public [|C|](int i)
                    {
                    }
                }
                """,
            FixedCode = """
                class C(int i)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInCSharp11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public C(int i)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonPublicConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private C(int i)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestStruct()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct C
                {
                    public [|C|](int i)
                    {
                    }
                }
                """,
            FixedCode = """
                struct C(int i)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithUnchainedConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public C(int i)
                    {
                    }

                    public C()
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithThisChainedConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public [|C|](int i)
                    {
                    }

                    public C() : this(0)
                    {
                    }
                }
                """,
            FixedCode = """
                class C(int i)
                {
                    public C() : this(0)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithBaseChainedConstructor1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class B(int i)
                {
                }

                class C : B
                {
                    public [|C|](int i) : base(i)
                    {
                    }

                    public C() : this(0)
                    {
                    }
                }
                """,
            FixedCode = """
                class B(int i)
                {
                }

                class C(int i) : B(i)
                {
                    public C() : this(0)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithBaseChainedConstructor2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class B(int i)
                {
                }

                class C : B
                {
                    public [|C|](int i) : base(i * i)
                    {
                    }

                    public C() : this(0)
                    {
                    }
                }
                """,
            FixedCode = """
                class B(int i)
                {
                }

                class C(int i) : B(i * i)
                {
                    public C() : this(0)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithBaseChainedConstructor3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class B(int i, int j)
                {
                }

                class C : B
                {
                    public [|C|](int i, int j) : base(i,
                        j)
                    {
                    }

                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            FixedCode = """
                class B(int i, int j)
                {
                }

                class C(int i, int j) : B(i,
                    j)
                {
                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithBaseChainedConstructor4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class B(int i, int j)
                {
                }

                class C : B
                {
                    public [|C|](int i, int j) : base(
                        i, j)
                    {
                    }

                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            FixedCode = """
                class B(int i, int j)
                {
                }

                class C(int i, int j) : B(
                    i, j)
                {
                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithBaseChainedConstructor5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class B(int i, int j)
                {
                }

                class C : B
                {
                    public [|C|](int i, int j) : base(
                        i,
                        j)
                    {
                    }

                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            FixedCode = """
                class B(int i, int j)
                {
                }

                class C(int i, int j) : B(
                    i,
                    j)
                {
                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithBadExpressionBody()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public C(int i)
                        => System.Console.WriteLine(i);
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithBadBlockBody()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public C(int i)
                    {
                        System.Console.WriteLine(i);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithExpressionBodyAssignmentToField1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;

                    public [|C|](int i)
                        => this.i = i;
                }
                """,
            FixedCode = """
                class C(int i)
                {
                    private int i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithExpressionBodyAssignmentToProperty1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int I { get; }

                    public [|C|](int i)
                        => this.I = i;
                }
                """,
            FixedCode = """
                class C(int i)
                {
                    private int I { get; } = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithExpressionBodyAssignmentToField2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;

                    public [|C|](int j)
                        => this.i = j;
                }
                """,
            FixedCode = """
                class C(int j)
                {
                    private int i = j;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithExpressionBodyAssignmentToField3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;

                    public [|C|](int j)
                        => i = j;
                }
                """,
            FixedCode = """
                class C(int j)
                {
                    private int i = j;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithAssignmentToBaseField1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class B
                {
                    public int i;
                }

                class C : B
                {
                    public C(int i)
                        => this.i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithAssignmentToBaseField2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class B
                {
                    public int i;
                }

                class C : B
                {
                    public C(int i)
                        => base.i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithCompoundAssignmentToField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public int i;

                    public C(int i)
                        => this.i += i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithTwoWritesToTheSameMember()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public int i;

                    public C(int i, int j)
                    {
                        this.i = i;
                        this.i = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithComplexRightSide1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;

                    public [|C|](int i)
                        => this.i = i * 2;
                }
                """,
            FixedCode = """
                class C(int i)
                {
                    private int i = i * 2;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestBlockWithMultipleAssignments1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public int i;
                    public int j;

                    public [|C|](int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }
                }
                """,
            FixedCode = """
                class C(int i, int j)
                {
                    public int i = i;
                    public int j = j;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestBlockWithMultipleAssignments2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public int i, j;

                    public [|C|](int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }
                }
                """,
            FixedCode = """
                class C(int i, int j)
                {
                    public int i = i, j = j;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembers1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;
                    private int j;

                    public [|C|](int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }
                }
                """,
            FixedCode = """
                class C(int i, int j)
                {
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembers2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int I { get; }
                    private int J { get; }

                    public [|C|](int i, int j)
                    {
                        this.I = i;
                        this.J = j;
                    }
                }
                """,
            FixedCode = """
                class C(int i, int j)
                {
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersOnlyWithMatchingType()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int I { get; }
                    private long J { get; }

                    public [|C|](int i, int j)
                    {
                        this.I = i;
                        this.J = j;
                    }
                }
                """,
            FixedCode = """
                class C(int i, int j)
                {
                    private long J { get; } = j;
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestDoNotRemovePublicMembers1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public int i;
                    public int j;

                    public [|C|](int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotRemoveMembersUsedInNestedTypes()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class OuterType
                {
                    private int _i;
                    private int _j;

                    public [|OuterType|](int i, int j)
                    {
                        _i = i;
                        _j = j;
                    }

                    public struct Enumerator
                    {
                        private int _i;

                        public Enumerator(OuterType c)
                        {
                            _i = c._i;
                            Console.WriteLine(c);
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                class OuterType(int i, int j)
                {
                    private int _i = i;

                    public struct Enumerator
                    {
                        private int _i;
                
                        public Enumerator(OuterType c)
                        {
                            _i = c._i;
                            Console.WriteLine(c);
                        }
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersUpdateReferences1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class C
                {
                    private int i;
                    private int j;

                    public [|C|](int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }

                    void M()
                    {
                        Console.WriteLine(this.i + this.j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C(int i, int j)
                {
                    void M()
                    {
                        Console.WriteLine(i + j);
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersUpdateReferences2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class C
                {
                    private int i;
                    private int j;

                    public [|C|](int @this, int @delegate)
                    {
                        this.i = @this;
                        this.j = @delegate;
                    }

                    void M()
                    {
                        Console.WriteLine(this.i + this.j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C(int @this, int @delegate)
                {
                    void M()
                    {
                        Console.WriteLine(@this + @delegate);
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersUpdateReferencesWithRename1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class C
                {
                    private int _i;
                    private int _j;

                    public [|C|](int i, int j)
                    {
                        _i = i;
                        _j = j;
                    }

                    void M()
                    {
                        Console.WriteLine(_i + _j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C(int i, int j)
                {
                    void M()
                    {
                        Console.WriteLine(i + j);
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersOnlyPrivateMembers()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class C
                {
                    private int _i;
                    public int _j;

                    public [|C|](int i, int j)
                    {
                        _i = i;
                        _j = j;
                    }

                    void M()
                    {
                        Console.WriteLine(_i + _j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C(int i, int j)
                {
                    public int _j = j;

                    void M()
                    {
                        Console.WriteLine(i + _j);
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersOnlyMembersWithoutAttributes()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            class C
            {
                private int _i;
                [CLSCompliant(true)]
                private int _j;

                public [|C|](int i, int j)
                {
                    _i = i;
                    _j = j;
                }

                void M()
                {
                    Console.WriteLine(_i + _j);
                }
            }
            """,
            FixedCode = """
            using System;
            class C(int i, int j)
            {
                [CLSCompliant(true)]
                private int _j = j;

                void M()
                {
                    Console.WriteLine(i + _j);
                }
            }
            """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersAccessedOffThis()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class C
                {
                    private int _i;
                    private int _j;

                    public [|C|](int i, int j)
                    {
                        _i = i;
                        _j = j;
                    }

                    void M(C c)
                    {
                        Console.WriteLine(_i);
                        Console.WriteLine(_j == c._j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C(int i, int j)
                {
                    private int _j = j;

                    void M(C c)
                    {
                        Console.WriteLine(i);
                        Console.WriteLine(_j == c._j);
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWhenRightSideReferencesThis1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int x;

                    public C(int i)
                    {
                        x = M(i);
                    }

                    int M(int y) => y;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWhenRightSideReferencesThis2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int x;

                    public C(int i)
                    {
                        x = this.M(i);
                    }

                    int M(int y) => y;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWhenRightSideDoesNotReferenceThis()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int x;

                    public [|C|](int i)
                    {
                        x = M(i);
                    }

                    static int M(int y) => y;
                }
                """,
            FixedCode = """
                class C(int i)
                {
                    private int x = M(i);

                    static int M(int y) => y;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorDocCommentWhenNothingOnType_SingleLine_1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    class C
                    {
                        private int i;

                        /// <summary>Doc comment on single line</summary>
                        /// <param name="i">Doc about i single line</param>
                        public [|C|](int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>Doc comment on single line</summary>
                    /// <param name="i">Doc about i single line</param>
                    class C(int i)
                    {
                        private int i = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorDocCommentWhenNothingOnType_IfDef1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                #if true
                    class C
                    {
                        private int i;

                        /// <summary>Doc comment on single line</summary>
                        /// <param name="i">Doc about i single line</param>
                        public [|C|](int i)
                        {
                            this.i = i;
                        }
                    }
                #endif
                }
                """,
            FixedCode = """
                namespace N
                {
                #if true
                    /// <summary>Doc comment on single line</summary>
                    /// <param name="i">Doc about i single line</param>
                    class C(int i)
                    {
                        private int i = i;
                    }
                #endif
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorDocCommentWhenNothingOnType_SingleLine_2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;

                    /// <summary>Doc comment on single line</summary>
                    /// <param name="i">Doc about i single line</param>
                    public [|C|](int i)
                    {
                        this.i = i;
                    }
                }
                """,
            FixedCode = """
                /// <summary>Doc comment on single line</summary>
                /// <param name="i">Doc about i single line</param>
                class C(int i)
                {
                    private int i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorDocCommentWhenNothingOnType_MultiLine_1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    class C
                    {
                        private int i;

                        /// <summary>
                        /// Doc comment
                        /// On multiple lines
                        /// </summary>
                        /// <param name="i">
                        /// Doc about i
                        /// on multiple lines
                        /// </param>
                        public [|C|](int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// Doc comment
                    /// On multiple lines
                    /// </summary>
                    /// <param name="i">
                    /// Doc about i
                    /// on multiple lines
                    /// </param>
                    class C(int i)
                    {
                        private int i = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorDocCommentWhenNothingOnType_MultiLine_2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    class C
                    {
                        private int i;

                        /// <summary>
                        /// Doc comment
                        /// On multiple lines</summary>
                        /// <param name="i">
                        /// Doc about i
                        /// on multiple lines</param>
                        public [|C|](int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// Doc comment
                    /// On multiple lines</summary>
                    /// <param name="i">
                    /// Doc about i
                    /// on multiple lines</param>
                    class C(int i)
                    {
                        private int i = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorDocCommentWhenNothingOnType_MultiLine_3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    class C
                    {
                        private int i;

                        /// <summary>Doc comment
                        /// On multiple lines</summary>
                        /// <param name="i">Doc about i
                        /// on multiple lines</param>
                        public [|C|](int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>Doc comment
                    /// On multiple lines</summary>
                    /// <param name="i">Doc about i
                    /// on multiple lines</param>
                    class C(int i)
                    {
                        private int i = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorParamDocCommentsIntoTypeDocComments1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// Existing doc comment
                    /// </summary>
                    class C
                    {
                        private int i;
                        private int j;

                        /// <summary>Constructor comment
                        /// On multiple lines</summary>
                        /// <param name="i">Doc about i</param>
                        /// <param name="i">Doc about j</param>
                        public [|C|](int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// Existing doc comment
                    /// </summary>
                    /// <remarks>Constructor comment
                    /// On multiple lines</remarks>
                    /// <param name="i">Doc about i</param>
                    /// <param name="i">Doc about j</param>
                    class C(int i, int j)
                    {
                        private int i = i;
                        private int j = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorParamDocCommentsIntoTypeDocComments2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>Existing doc comment</summary>
                    class C
                    {
                        private int i;
                        private int j;

                        /// <summary>Constructor comment</summary>
                        /// <param name="i">Doc about
                        /// i</param>
                        /// <param name="i">Doc about
                        /// j</param>
                        public [|C|](int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>Existing doc comment</summary>
                    /// <remarks>Constructor comment</remarks>
                    /// <param name="i">Doc about
                    /// i</param>
                    /// <param name="i">Doc about
                    /// j</param>
                    class C(int i, int j)
                    {
                        private int i = i;
                        private int j = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersMoveDocComments_WhenNoTypeDocComments1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    class C
                    {
                        /// <summary>Docs for i.</summary>
                        private int i;
                        /// <summary>
                        /// Docs for j.
                        /// </summary>
                        private int j;

                        public [|C|](int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <param name="i">Docs for i.</param>
                    /// <param name="j">
                    /// Docs for j.
                    /// </param>
                    class C(int i, int j)
                    {
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersMoveDocComments_WhenNoTypeDocComments_MembersWithDifferentNames1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    class C
                    {
                        /// <summary>Docs for x.</summary>
                        private int x;
                        /// <summary>
                        /// Docs for y.
                        /// </summary>
                        private int y;

                        public [|C|](int i, int j)
                        {
                            this.x = i;
                            this.y = j;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <param name="i">Docs for x.</param>
                    /// <param name="j">
                    /// Docs for y.
                    /// </param>
                    class C(int i, int j)
                    {
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersMoveDocComments_WhenTypeDocComments1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    class C
                    {
                        /// <summary>Docs for i.</summary>
                        private int i;
                        /// <summary>
                        /// Docs for j.
                        /// </summary>
                        private int j;

                        public [|C|](int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    /// <param name="i">Docs for i.</param>
                    /// <param name="j">
                    /// Docs for j.
                    /// </param>
                    class C(int i, int j)
                    {
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersKeepConstructorDocs1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    class C
                    {
                        /// <summary>Field docs for i.</summary>
                        private int i;
                        /// <summary>
                        /// Field docs for j.
                        /// </summary>
                        private int j;

                        /// <param name="i">Param docs for i</param>
                        /// <param name="j">Param docs for j</param>
                        public [|C|](int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    /// <param name="i">Param docs for i</param>
                    /// <param name="j">Param docs for j</param>
                    class C(int i, int j)
                    {
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersKeepConstructorDocs2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    class C
                    {
                        /// <summary>Field docs for i.</summary>
                        private int i;
                        /// <summary>
                        /// Field docs for j.
                        /// </summary>
                        private int j;

                        /// <param name="j">Param docs for j</param>
                        public [|C|](int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    /// <param name="j">Param docs for j</param>
                    /// <param name="i">Field docs for i.</param>
                    class C(int i, int j)
                    {
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRemoveMembersKeepConstructorDocs3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    class C
                    {
                        /// <summary>Field docs for i.</summary>
                        private int i;
                        /// <summary>
                        /// Field docs for j.
                        /// </summary>
                        private int j;

                        /// <param name="i">Param docs for i</param>
                        public [|C|](int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    /// <param name="i">Param docs for i</param>
                    /// <param name="j">
                    /// Field docs for j.
                    /// </param>
                    class C(int i, int j)
                    {
                    }
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAll1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public [|C|](int i)
                    {
                    }
                }

                class D
                {
                    public [|D|](int j)
                    {
                    }
                }
                """,
            FixedCode = """
                class C(int i)
                {
                }

                class D(int j)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAll2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public [|C|](int i)
                    {
                    }
                
                    class D
                    {
                        public [|D|](int j)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C(int i)
                {
                    class D(int j)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAll3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;

                    public [|C|](int i)
                    {
                        this.i = i;
                    }
                
                    class D
                    {
                        private int J { get; }

                        public [|D|](int j)
                        {
                            this.J = j;
                        }
                    }
                }
                """,
            FixedCode = """
                class C(int i)
                {
                    private int i = i;

                    class D(int j)
                    {
                        private int J { get; } = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAll4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class C
                {
                    private int i;

                    public [|C|](int i)
                    {
                        this.i = i;
                    }

                    void M()
                    {
                        Console.WriteLine(i);
                    }
                
                    class D
                    {
                        private int J { get; }

                        public [|D|](int j)
                        {
                            this.J = j;
                        }
                
                        void N()
                        {
                            Console.WriteLine(J);
                        }
                    }
                }
                """,
            FixedCode = """
                using System;
                class C(int i)
                {
                    void M()
                    {
                        Console.WriteLine(i);
                    }

                    class D(int j)
                    {
                        void N()
                        {
                            Console.WriteLine(j);
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            CodeActionIndex = 1,
            NumberOfFixAllIterations = 1,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
               using System;
               class C
               {
                   [Obsolete("", error: true)]
                   public [|C|](int i)
                   {
                   }
               }
               """,
            FixedCode = """
               using System;
               [method: Obsolete("", error: true)]
               class C(int i)
               {
               }
               """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes1A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
               using System;
               [Obsolete("", error: true)]
               class C
               {
                   [Obsolete("", error: true)]
                   public [|C|](int i)
                   {
                   }
               }
               """,
            FixedCode = """
               using System;
               [Obsolete("", error: true)]
               [method: Obsolete("", error: true)]
               class C(int i)
               {
               }
               """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
     
                namespace N
                {
                    class C
                    {
                        [Obsolete("", error: true)]
                        public [|C|](int i)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    [method: Obsolete("", error: true)]
                    class C(int i)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes2A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
     
                namespace N
                {
                    [Obsolete("", error: true)]
                    class C
                    {
                        [Obsolete("", error: true)]
                        public [|C|](int i)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    [Obsolete("", error: true)]
                    [method: Obsolete("", error: true)]
                    class C(int i)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
     
                namespace N
                {
                    class C
                    {
                        int x;

                        [Obsolete("", error: true)]
                        public [|C|](int i)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    [method: Obsolete("", error: true)]
                    class C(int i)
                    {
                        int x;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes3A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
     
                namespace N
                {
                    [Obsolete("", error: true)]
                    class C
                    {
                        int x;

                        [Obsolete("", error: true)]
                        public [|C|](int i)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    [Obsolete("", error: true)]
                    [method: Obsolete("", error: true)]
                    class C(int i)
                    {
                        int x;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                [Serializable]
                class C
                {
                    [CLSCompliant(false)]
                    [Obsolete("", error: true)]
                    public [|C|](int i)
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                [Serializable]
                [method: CLSCompliant(false)]
                [method: Obsolete("", error: true)]
                class C(int i)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes4A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                [Serializable]
                class C
                {
                    int x;
                
                    [CLSCompliant(false)]
                    [Obsolete("", error: true)]
                    public [|C|](int i)
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                [Serializable]
                [method: CLSCompliant(false)]
                [method: Obsolete("", error: true)]
                class C(int i)
                {
                    int x;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersMove1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    public [|C|](int i,
                        int j)
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                class C(int i,
                    int j)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersMove1A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                namespace N
                {
                    class C
                    {
                        public [|C|](int i,
                            int j)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    class C(int i,
                        int j)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersMove2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    public [|C|](
                        int i,
                        int j)
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                class C(
                    int i,
                    int j)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersMove2A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                namespace N
                {
                    class C
                    {
                        public [|C|](
                            int i,
                            int j)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    class C(
                        int i,
                        int j)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersMove3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    public [|C|](
                        int i, int j)
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                class C(
                    int i, int j)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersMove3A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                namespace N
                {
                    class C
                    {
                        public [|C|](
                            int i, int j)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    class C(
                        int i, int j)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersMove4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    public [|C|](
                int i,
                int j)
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                class C(
                int i,
                int j)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersMove4A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                namespace N
                {
                    class C
                    {
                        public [|C|](
                int i,
                int j)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    class C(
                int i,
                int j)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReferenceToNestedType1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public class D
                    {
                    }

                    public [|C|](D d)
                    {
                    }
                }
                """,
            FixedCode = """
                class C(C.D d)
                {
                    public class D
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReferenceToNestedType2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C<T>
                {
                    public class D
                    {
                    }

                    public [|C|](List<D> d)
                    {
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C<T>(List<C<T>.D> d)
                {
                    public class D
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReferenceToNestedType3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public class D
                    {
                    }

                    public [|C|](C.D d)
                    {
                    }
                }
                """,
            FixedCode = """
                class C(C.D d)
                {
                    public class D
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReferenceToNestedType4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C<T>
                {
                    public class D
                    {
                    }

                    public [|C|](List<C<T>.D> d)
                    {
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C<T>(List<C<T>.D> d)
                {
                    public class D
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70586")]
    public async Task TestReferenceToNestedType5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                public class B(B.A a)
                {
                    public class A { }
                }

                public class C : B
                {
                    public [|C|](A a) : base(a) { }
                }
                """,
            FixedCode = """
                public class B(B.A a)
                {
                    public class A { }
                }
                
                public class C(B.A a) : B(a)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonAutoProperty()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    // Can't assign a primary constructor parameter to a non-auto property.
                    private int I { get { return 0; } set { } }

                    public C(int i)
                    {
                        this.I = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInParameter1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;

                    public [|C|](in int i)
                    {
                        this.i = i;
                    }
                }
                """,
            FixedCode = """
                class C(in int i)
                {
                    private int i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInParameter2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int i;

                    public [|C|](in int i)
                    {
                        this.i = i;
                    }
                }
                """,
            FixedCode = """
                class C(int i)
                {
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPreprocessorRegion1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    public C(int i)
                    {
                #if NET6
                        Console.WriteLine();
                #endif
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPreprocessorRegion2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    public C(int i)
                    {
                #if false
                        Console.WriteLine();
                #endif
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPreprocessorRegion3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    private int _i;

                    public C(int i)
                #if NET6
                        => _i = i;
                #else
                        => this._i = i;
                #endif
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPreprocessorRegion4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    private int _i;

                    public C(int i)
                #if true
                        => _i = i;
                #else
                        => this._i = i;
                #endif
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPreprocessorRegion5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    private int _i;

                    public C(int i)
                #if false
                        => _i = i;
                #else
                        => this._i = i;
                #endif
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPreprocessorRegion6()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    private int _i;

                    public C(int i)
                    {
                #if true
                        _i = i;
                #endif
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPreprocessorRegion7()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    private int _i;

                    public C(int i)
                    {
                #if true
                        _i = i;
                #else
                        this._i = i;
                #endif
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPreprocessorRegion8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    private int _i;

                    public C(int i)
                    {
                #if false
                        _i = i;
                #else
                        this._i = i;
                #endif
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithRegionDirective1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {

                    #region constructors

                    public [|C|](int i)
                    {
                    }

                    #endregion

                }
                """,
            FixedCode = """
                class C(int i)
                {
                
                    #region constructors

                    #endregion

                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithRegionDirective2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {

                    #region constructors

                    public [|C|](int i)
                    {
                    }

                    public C(string s) : this(s.Length)
                    {
                    }

                    #endregion

                }
                """,
            FixedCode = """
                class C(int i)
                {

                    #region constructors

                    public C(string s) : this(s.Length)
                    {
                    }

                    #endregion

                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSeeTag1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                /// <summary>
                /// Provides strongly typed wrapper around <see cref="_i"/>.
                /// </summary>
                class C
                {
                    private int _i;

                    public [|C|](int i)
                    {
                        _i = i;
                    }
                }
                """,
            FixedCode = """
                /// <summary>
                /// Provides strongly typed wrapper around <see cref="_i"/>.
                /// </summary>
                class C(int i)
                {
                    private int _i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSeeTag2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                /// <summary>
                /// Provides strongly typed wrapper around <see cref="_i"/>.
                /// </summary>
                class C
                {
                    private int _i;

                    public [|C|](int i)
                    {
                        _i = i;
                    }
                }
                """,
            FixedCode = """
                /// <summary>
                /// Provides strongly typed wrapper around <paramref name="i"/>.
                /// </summary>
                class C(int i)
                {
                }
                """,
            CodeActionIndex = 1,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReferenceToConstantInParameterInitializer1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private const int Default = 0;
                    private int _i;

                    public [|C|](int i = Default)
                    {
                        _i = i;
                    }
                }
                """,
            FixedCode = """
                class C(int i = C.Default)
                {
                    private const int Default = 0;
                    private int _i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReferenceToConstantInParameterInitializer2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private const int Default = 0;
                    private int _i;

                    public [|C|](int i = C.Default)
                    {
                        _i = i;
                    }
                }
                """,
            FixedCode = """
                class C(int i = C.Default)
                {
                    private const int Default = 0;
                    private int _i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReferenceToConstantInParameterInitializer3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C<T>
                {
                    private const int Default = 0;
                    private int _i;

                    public [|C|](int i = Default)
                    {
                        _i = i;
                    }
                }
                """,
            FixedCode = """
                class C<T>(int i = C<T>.Default)
                {
                    private const int Default = 0;
                    private int _i = i;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMergeConstructorSummaryIntoTypeDocComment()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
                {
                    /// <summary>
                    /// Active instruction identifier.
                    /// It has the information necessary to track an active instruction within the debug session.
                    /// </summary>
                    [CLSCompliant(false)]
                    internal readonly struct ManagedInstructionId
                    {
                        /// <summary>
                        /// Method which the instruction is scoped to.
                        /// </summary>
                        public string Method { get; }
                
                        /// <summary>
                        /// The IL offset for the instruction.
                        /// </summary>
                        public int ILOffset { get; }

                        /// <summary>
                        /// Creates an ActiveInstructionId.
                        /// </summary>
                        /// <param name="method">Method which the instruction is scoped to.</param>
                        /// <param name="ilOffset">IL offset for the instruction.</param>
                        public [|ManagedInstructionId|](
                            string method,
                            int ilOffset)
                        {
                            Method = method;
                            ILOffset = ilOffset;
                        }
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
                {
                    /// <summary>
                    /// Active instruction identifier.
                    /// It has the information necessary to track an active instruction within the debug session.
                    /// </summary>
                    /// <remarks>
                    /// Creates an ActiveInstructionId.
                    /// </remarks>
                    /// <param name="method">Method which the instruction is scoped to.</param>
                    /// <param name="ilOffset">IL offset for the instruction.</param>
                    [CLSCompliant(false)]
                    internal readonly struct ManagedInstructionId(
                        string method,
                        int ilOffset)
                    {
                        /// <summary>
                        /// Method which the instruction is scoped to.
                        /// </summary>
                        public string Method { get; } = method;

                        /// <summary>
                        /// The IL offset for the instruction.
                        /// </summary>
                        public int ILOffset { get; } = ilOffset;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70658")]
    public async Task TestPartialType1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class Base(int i)
                {
                }

                partial class C
                {
                    public [|C|](int i) : base(i)
                    {
                    }
                }

                partial class C : Base
                {
                }
                """,
            FixedCode = """
                class Base(int i)
                {
                }
                
                partial class C(int i) : Base(i)
                {
                }
                
                partial class C : Base
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70658")]
    public async Task TestPartialType2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class Base(int i)
                {
                }

                partial class C : IDisposable
                {
                    public [|C|](int i) : base(i)
                    {
                    }

                    public void Dispose() { }
                }

                partial class C : Base
                {
                }
                """,
            FixedCode = """
                using System;
                
                class Base(int i)
                {
                }
                
                partial class C(int i) : Base(i), IDisposable
                {
                    public void Dispose() { }
                }
                
                partial class C : Base
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70658")]
    public async Task TestPartialType3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class Base(int i)
                {
                }

                partial class C<T>
                {
                    public [|C|](int i) : base(i)
                    {
                    }
                }

                partial class C<T> : Base
                {
                }
                """,
            FixedCode = """
                class Base(int i)
                {
                }
                
                partial class C<T>(int i) : Base(i)
                {
                }
                
                partial class C<T> : Base
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70658")]
    public async Task TestPartialType4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class Base(int i)
                {
                }

                partial class C<T> where T : IDisposable
                {
                    public [|C|](int i) : base(i)
                    {
                    }
                }

                partial class C<T> : Base
                {
                }
                """,
            FixedCode = """
                using System;

                class Base(int i)
                {
                }
                
                partial class C<T>(int i) : Base(i) where T : IDisposable
                {
                }
                
                partial class C<T> : Base
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }
}
