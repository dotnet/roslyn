// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertPrimaryToRegularConstructor;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertPrimaryToRegularConstructor;

using VerifyCS = CSharpCodeRefactoringVerifier<ConvertPrimaryToRegularConstructorCodeRefactoringProvider>;

public class ConvertPrimaryToRegularConstructorTests
{
    private const string FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig = """
        [*.cs]
        dotnet_naming_style.field_camel_case.capitalization         = camel_case
        dotnet_naming_style.field_camel_case.required_prefix        = _
        dotnet_naming_symbols.fields.applicable_kinds               = field
        dotnet_naming_symbols.fields.applicable_accessibilities     = *
        dotnet_naming_rule.fields_should_be_camel_case.severity     = error
        dotnet_naming_rule.fields_should_be_camel_case.symbols      = fields
        dotnet_naming_rule.fields_should_be_camel_case.style        = field_camel_case
        """;

    [Fact]
    public async Task TestInCSharp12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i)|]
                {
                }
                """,
            FixedCode = """
                class C
                {
                    public C(int i)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithRecord()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                record class [|C(int i)|]
                {
                }
                """,
            FixedCode = """
                record class C(int i)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestStruct()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct [|C(int i)|]
                {
                }
                """,
            FixedCode = """
                struct C
                {
                    public C(int i)
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
                class [|C(int i)|]
                {
                    public C() : this(0)
                    {
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    public C(int i)
                    {
                    }

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

                class [|C(int i)|] : B(i)
                {
                    public C() : this(0)
                    {
                    }
                }
                """,
            FixedCode = """
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

                class [|C(int i)|] : B(i * i)
                {
                    public C() : this(0)
                    {
                    }
                }
                """,
            FixedCode = """
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

                class [|C(int i, int j)|] : B(i,
                    j)
                {
                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            FixedCode = """
                class B(int i, int j)
                {
                }

                class C : B
                {
                    public C(int i, int j) : base(i,
                        j)
                    {
                    }

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

                class [|C(int i, int j)|] : B(
                    i, j)
                {
                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            FixedCode = """
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

                class [|C(int i, int j)|] : B(
                    i,
                    j)
                {
                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            FixedCode = """
                class B(int i, int j)
                {
                }

                class C : B
                {
                    public C(int i, int j) : base(
                        i,
                        j)
                    {
                    }

                    public C() : this(0, 0)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReferenceOnlyInExistingSameNamedField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i)|]
                {
                    private int i = i;
                }
                """,
            FixedCode = """
                class C
                {
                    private int i;

                    public C(int i)
                    {
                        this.i = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReferenceOnlyInPropertyInitializer1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i)|]
                {
                    private int I { get; } = i;
                }
                """,
            FixedCode = """
                class C
                {
                    private int I { get; }

                    public C(int i)
                    {
                        I = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReferenceInDifferentNamedField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int j)|]
                {
                    private int i = j;
                }
                """,
            FixedCode = """
                class C
                {
                    private int i;

                    public C(int j)
                    {
                        i = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithComplexFieldInitializer1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i)|]
                {
                    private int i = i * 2;
                }
                """,
            FixedCode = """
                class C
                {
                    private int i;

                    public C(int i)
                    {
                        this.i = i * 2;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithComplexFieldInitializer2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i)|]
                {
                    private int i = i * i;
                }
                """,
            FixedCode = """
                class C
                {
                    private int i;

                    public C(int i)
                    {
                        this.i = i * i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithComplexFieldInitializer3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i)|]
                {
                    private int i = i * i;
                    private int j = i + i;
                }
                """,
            FixedCode = """
                class C
                {
                    private int i;
                    private int j;

                    public C(int i)
                    {
                        this.i = i * i;
                        j = i + i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersWithFieldInitializers1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i, int j)|]
                {
                    public int i = i;
                    public int j = j;
                }
                """,
            FixedCode = """
                class C
                {
                    public int i;
                    public int j;

                    public C(int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleParametersWithFieldInitializers2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i, int j)|]
                {
                    public int i = i, j = j;
                }
                """,
            FixedCode = """
                class C
                {
                    public int i, j;

                    public C(int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithParametersReferencedOutsideOfConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i, int j)|]
                {
                    int M()
                    {
                        return i + j;
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    private readonly int i;
                    private readonly int j;

                    public [|C|](int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }

                    int M()
                    {
                        return i + j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithParametersReferencedOutsideOfConstructor_Mutation1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i, int j)|]
                {
                    int M()
                    {
                        return i++ + j;
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    private readonly int j;
                    private int i;

                    public [|C|](int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }

                    int M()
                    {
                        return i++ + j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithoutParametersReferencedOutsideOfConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i, int j)|]
                {
                }
                """,
            FixedCode = """
                class C
                {
                    public C(int i, int j)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAssignmentToPropertyOfDifferentType()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i, int j)|]
                {
                    private long J { get; } = j;
                }
                """,
            FixedCode = """
                class C
                {
                    private long J { get; }

                    public C(int i, int j)
                    {
                        J = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAssignedToFieldUsedInNestedType()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class [|OuterType(int i, int j)|]
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
            FixedCode = """
                using System;

                class OuterType
                {
                    private int _i;

                    public [|OuterType|](int i, int j)
                    {
                        _i = i;
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNotMutatedReferencesOutsideOfConstructor1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class [|C(int i, int j)|]
                {
                    void M()
                    {
                        Console.WriteLine(i + j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C
                {
                    private readonly int i;
                    private readonly int j;

                    public C(int i, int j)
                    {
                        this.i = i;
                        this.j = j;
                    }

                    void M()
                    {
                        Console.WriteLine(i + j);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestEscapedParameterNames()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class [|C(int @this, int @delegate)|]
                {
                    void M()
                    {
                        Console.WriteLine(@this + @delegate);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C
                {
                    private readonly int @this;
                    private readonly int @delegate;

                    public C(int @this, int @delegate)
                    {
                        this.@this = @this;
                        this.@delegate = @delegate;
                    }

                    void M()
                    {
                        Console.WriteLine(@this + @delegate);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNotMutatedReferencesOutsideOfConstructor2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class [|C(int i, int j)|]
                {
                    public int _j = j;

                    void M()
                    {
                        Console.WriteLine(i + _j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C
                {
                    private readonly int i;
                    public int _j;

                    public C(int i, int j)
                    {
                        this.i = i;
                        _j = j;
                    }

                    void M()
                    {
                        Console.WriteLine(i + _j);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNotMutatedReferencesOutsideOfConstructor3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class [|C(int i, int j)|]
                {
                    [CLSCompliant(true)]
                    private int _j = j;

                    void M()
                    {
                        Console.WriteLine(i + _j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C
                {
                    private readonly int i;
                    [CLSCompliant(true)]
                    private int _j;

                    public [|C|](int i, int j)
                    {
                        this.i = i;
                        _j = j;
                    }

                    void M()
                    {
                        Console.WriteLine(i + _j);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestGenerateUnderscoreName1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class [|C(int i, int j)|]
                {
                    private int _j = j;

                    void M(C c)
                    {
                        Console.WriteLine(i);
                        Console.WriteLine(_j == c._j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C
                {
                    private readonly int _i;
                    private int _j;

                    public C(int i, int j)
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
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig,
        }.RunAsync();
    }

    [Fact]
    public async Task TestGenerateUnderscoreName2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class [|C(int i, int j)|]
                {
                    private int _j = j;

                    void M(C c)
                    {
                        Console.WriteLine(i++);
                        Console.WriteLine(_j == c._j);
                    }
                }
                """,
            FixedCode = """
                using System;
                class C
                {
                    private int _j;
                    private int _i;

                    public C(int i, int j)
                    {
                        _i = i;
                        _j = j;
                    }

                    void M(C c)
                    {
                        Console.WriteLine(_i++);
                        Console.WriteLine(_j == c._j);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig,
        }.RunAsync();
    }

    [Fact]
    public async Task TestComplexFieldInitializer()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i)|]
                {
                    private int x = M(i);

                    static int M(int y) => y;
                }
                """,
            FixedCode = """
                class C
                {
                    private int x;

                    public C(int i)
                    {
                        x = M(i);
                    }

                    static int M(int y) => y;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>Doc comment on single line</summary>
                    /// <param name="i">Doc about i single line</param>
                    class [|C(int i)|]
                    {
                        private int i = i;
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>Doc comment on single line</summary>
                    class C
                    {
                        private int i;

                        /// <param name="i">Doc about i single line</param>
                        public C(int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                #if true
                    /// <summary>Doc comment on single line</summary>
                    /// <param name="i">Doc about i single line</param>
                    class [|C(int i)|]
                    {
                        private int i = i;
                    }
                #endif
                }
                """,
            FixedCode = """
                namespace N
                {
                #if true
                    /// <summary>Doc comment on single line</summary>
                    class C
                    {
                        private int i;

                        /// <param name="i">Doc about i single line</param>
                        public C(int i)
                        {
                            this.i = i;
                        }
                    }
                #endif
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                ///<summary>Doc comment on single line</summary>
                ///<param name="i">Doc about i single line</param>
                class [|C(int i)|]
                {
                    private int i = i;
                }
                """,
            FixedCode = """
                ///<summary>Doc comment on single line</summary>
                class C
                {
                    private int i;

                    ///<param name="i">Doc about i single line</param>
                    public C(int i)
                    {
                        this.i = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
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
                    class [|C(int i)|]
                    {
                        private int i = i;
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
                    class C
                    {
                        private int i;

                        /// <param name="i">
                        /// Doc about i
                        /// on multiple lines
                        /// </param>
                        public C(int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// Doc comment
                    /// On multiple lines</summary>
                    /// <param name="i">
                    /// Doc about i
                    /// on multiple lines</param>
                    class [|C(int i)|]
                    {
                        private int i = i;
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// Doc comment
                    /// On multiple lines</summary>
                    class C
                    {
                        private int i;

                        /// <param name="i">
                        /// Doc about i
                        /// on multiple lines</param>
                        public C(int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs6()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>Doc comment
                    /// On multiple lines</summary>
                    /// <param name="i">Doc about i
                    /// on multiple lines</param>
                    class [|C(int i)|]
                    {
                        private int i = i;
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>Doc comment
                    /// On multiple lines</summary>
                    class C
                    {
                        private int i;

                        /// <param name="i">Doc about i
                        /// on multiple lines</param>
                        public C(int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs7()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    ///<summary>
                    ///Doc comment
                    ///On multiple lines
                    ///</summary>
                    ///<param name="i">
                    ///Doc about i
                    ///on multiple lines
                    ///</param>
                    class [|C(int i)|]
                    {
                        private int i = i;
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    ///<summary>
                    ///Doc comment
                    ///On multiple lines
                    ///</summary>
                    class C
                    {
                        private int i;

                            ///<param name="i">
                        ///Doc about i
                        ///on multiple lines
                        ///</param>
                        public C(int i)
                        {
                            this.i = i;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs9()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// Existing doc comment
                    /// </summary>
                    /// <remarks>Constructor comment
                    /// On multiple lines</remarks>
                    /// <param name="i">Doc about i</param>
                    /// <param name="i">Doc about j</param>
                    class [|C(int i, int j)|]
                    {
                        private int i = i;
                        private int j = j;
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
                    class C
                    {
                        private int i;
                        private int j;

                        /// <param name="i">Doc about i</param>
                        /// <param name="i">Doc about j</param>
                        public C(int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs10()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>Existing doc comment</summary>
                    /// <remarks>Constructor comment</remarks>
                    /// <param name="i">Doc about
                    /// i</param>
                    /// <param name="i">Doc about
                    /// j</param>
                    class [|C(int i, int j)|]
                    {
                        private int i = i;
                        private int j = j;
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>Existing doc comment</summary>
                    /// <remarks>Constructor comment</remarks>
                    class C
                    {
                        private int i;
                        private int j;

                        /// <param name="i">Doc about
                        /// i</param>
                        /// <param name="i">Doc about
                        /// j</param>
                        public C(int i, int j)
                        {
                            this.i = i;
                            this.j = j;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <param name="i">Docs for i.</param>
                    /// <param name="j">
                    /// Docs for j.
                    /// </param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    class C
                    {
                        /// <param name="i">Docs for i.</param>
                        /// <param name="j">
                        /// Docs for j.
                        /// </param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs13()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <param name="i">Docs for i.</param> 
                    /// <param name="j">Docs for j.</param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    class C
                    {
                        /// <param name="i">Docs for i.</param> 
                        /// <param name="j">Docs for j.</param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs14()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <param name="i">Docs for i.</param> 
                    /// <param name="j">Docs for j.</param> 
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    class C
                    {
                        /// <param name="i">Docs for i.</param> 
                        /// <param name="j">Docs for j.</param> 
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs15()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <param name="i">Docs for i.</param> 
                    ///<param name="j">Docs for j.</param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    class C
                    {
                        /// <param name="i">Docs for i.</param> 
                        ///<param name="j">Docs for j.</param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs16()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    ///<param name="i">Docs for i.</param>
                    ///<param name="j">
                    ///Docs for j.
                    ///</param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    class C
                    {
                        ///<param name="i">Docs for i.</param>
                        ///<param name="j">
                        ///Docs for j.
                        ///</param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs17()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    ///<param name="i">Docs for i.</param>
                    ///<param name="j">Docs for j.</param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    class C
                    {
                        ///<param name="i">Docs for i.</param>
                        ///<param name="j">Docs for j.</param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs18()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <param name="i">Docs for x.</param>
                    /// <param name="j">
                    /// Docs for y.
                    /// </param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    class C
                    {
                        /// <param name="i">Docs for x.</param>
                        /// <param name="j">
                        /// Docs for y.
                        /// </param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs19()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    /// <param name="i">Docs for i.</param>
                    /// <param name="j">
                    /// Docs for j.
                    /// </param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    class C
                    {
                        /// <param name="i">Docs for i.</param>
                        /// <param name="j">
                        /// Docs for j.
                        /// </param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs20()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    /// <param name="i">Param docs for i</param>
                    /// <param name="j">Param docs for j</param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    class C
                    {
                        /// <param name="i">Param docs for i</param>
                        /// <param name="j">Param docs for j</param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs21()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    /// <param name="j">Param docs for j</param>
                    /// <param name="i">Field docs for i.</param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    class C
                    {
                        /// <param name="j">Param docs for j</param>
                        /// <param name="i">Field docs for i.</param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveParamDocs22()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    /// <param name="i">Param docs for i</param>
                    /// <param name="j">
                    /// Field docs for j.
                    /// </param>
                    class [|C(int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    /// <summary>
                    /// C docs
                    /// </summary>
                    class C
                    {
                        /// <param name="i">Param docs for i</param>
                        /// <param name="j">
                        /// Field docs for j.
                        /// </param>
                        public C(int i, int j)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveConstructorAttributes1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
               using System;
               [method: Obsolete("", error: true)]
               class [|C(int i)|]
               {
               }
               """,
            FixedCode = """
               using System;

               class C
               {
                   [Obsolete("", error: true)]
                   public [|C|](int i)
                   {
                   }
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
               [method: Obsolete("", error: true)]
               class [|C(int i)|]
               {
               }
               """,
            FixedCode = """
               using System;
               [Obsolete("", error: true)]
               class C
               {
                   [Obsolete("", error: true)]
                   public C(int i)
                   {
                   }
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
                    [method: Obsolete("", error: true)]
                    class [|C(int i)|]
                    {
                    }
                }
                """,
            FixedCode = """
                using System;
     
                namespace N
                {
                    class C
                    {
                        [Obsolete("", error: true)]
                        public C(int i)
                        {
                        }
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
                    [method: Obsolete("", error: true)]
                    class [|C(int i)|]
                    {
                    }
                }
                """,
            FixedCode = """
                using System;
     
                namespace N
                {
                    [Obsolete("", error: true)]
                    class C
                    {
                        [Obsolete("", error: true)]
                        public C(int i)
                        {
                        }
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
                    [method: Obsolete("", error: true)]
                    class [|C(int i)|]
                    {
                        int x;
                    }
                }
                """,
            FixedCode = """
                using System;
     
                namespace N
                {
                    class C
                    {
                        int x;

                        [Obsolete("", error: true)]
                        public C(int i)
                        {
                        }
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
                    [method: Obsolete("", error: true)]
                    class [|C(int i)|]
                    {
                        int x;
                    }
                }
                """,
            FixedCode = """
                using System;
     
                namespace N
                {
                    [Obsolete("", error: true)]
                    class C
                    {
                        int x;

                        [Obsolete("", error: true)]
                        public C(int i)
                        {
                        }
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
                [method: CLSCompliant(false)]
                [method: Obsolete("", error: true)]
                class [|C(int i)|]
                {
                }
                """,
            FixedCode = """
                using System;

                [Serializable]
                class C
                {
                    [CLSCompliant(false)]
                    [Obsolete("", error: true)]
                    public C(int i)
                    {
                    }
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
                [method: CLSCompliant(false)]
                [method: Obsolete("", error: true)]
                class [|C(int i)|]
                {
                    int x;
                }
                """,
            FixedCode = """
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

                class [|C(int i,
                    int j)|]
                {
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    public [|C|](int i,
                        int j)
                    {
                    }
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
                    class [|C(int i,
                        int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
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

                class [|C(
                    int i,
                    int j)|]
                {
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    public C(
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
    public async Task TestMultipleParametersMove2A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                namespace N
                {
                    class [|C(
                        int i,
                        int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    class C
                    {
                        public C(
                            int i,
                            int j)
                        {
                        }
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

                class [|C(
                    int i, int j)|]
                {
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    public C(
                        int i, int j)
                    {
                    }
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
                    class [|C(
                        int i, int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    class C
                    {
                        public C(
                            int i, int j)
                        {
                        }
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

                class [|C(
                int i,
                int j)|]
                {
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    public C(
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
    public async Task TestMultipleParametersMove4A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                namespace N
                {
                    class [|C(
                int i,
                int j)|]
                    {
                    }
                }
                """,
            FixedCode = """
                using System;

                namespace N
                {
                    class C
                    {
                        public C(
                int i,
                int j)
                        {
                        }
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
                class [|C(C.D d)|]
                {
                    public class D
                    {
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    public C(D d)
                    {
                    }

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

                class [|C<T>(List<C<T>.D> d)|]
                {
                    public class D
                    {
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C<T>
                {
                    public C(List<D> d)
                    {
                    }

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
                class [|C(C.D d)|]
                {
                    public class D
                    {
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    public C(D d)
                    {
                    }

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

                class [|C<T>(List<C<T>.D> d)|]
                {
                    public class D
                    {
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C<T>
                {
                    public C(List<D> d)
                    {
                    }

                    public class D
                    {
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
                class [|C(in int i)|]
                {
                    private int i = i;
                }
                """,
            FixedCode = """
                class C
                {
                    private int i;

                    public C(in int i)
                    {
                        this.i = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInParameter2_Unused()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(in int i)|]
                {
                }
                """,
            FixedCode = """
                class C
                {
                    public C(in int i)
                    {
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
                class [|C(int i)|]
                {
                
                    #region constructors

                    #endregion

                }
                """,
            FixedCode = """
                class C
                {
                    public C(int i)
                    {
                    }

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
                class [|C(int i)|]
                {

                    #region constructors

                    public C(string s) : this(s.Length)
                    {
                    }

                    #endregion

                }
                """,
            FixedCode = """
                class C
                {
                    public C(int i)
                    {
                    }

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
                class [|C(int i)|]
                {
                    private int _i = i;
                }
                """,
            FixedCode = """
                /// <summary>
                /// Provides strongly typed wrapper around <see cref="_i"/>.
                /// </summary>
                class C
                {
                    private int _i;

                    public C(int i)
                    {
                        _i = i;
                    }
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
                /// Provides strongly typed wrapper around <paramref name="i"/>.
                /// </summary>
                class [|C(int i)|]
                {
                    int M()
                    {
                        return i;
                    }
                }
                """,
            FixedCode = """
                /// <summary>
                /// Provides strongly typed wrapper around <see cref="i"/>.
                /// </summary>
                class C
                {
                    private readonly int i;

                    public C(int i)
                    {
                        this.i = i;
                    }

                    int M()
                    {
                        return i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSeeTag3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                /// <summary>
                /// Provides strongly typed wrapper around <paramref name="i"/>.
                /// </summary>
                class [|C(int i)|]
                {
                    int M()
                    {
                        return i;
                    }
                }
                """,
            FixedCode = """
                /// <summary>
                /// Provides strongly typed wrapper around <see cref="_i"/>.
                /// </summary>
                class C
                {
                    private readonly int _i;

                    public C(int i)
                    {
                        _i = i;
                    }

                    int M()
                    {
                        return _i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig,
        }.RunAsync();
    }

    [Fact]
    public async Task TestReferenceToConstantInParameterInitializer1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class [|C(int i = C.Default)|]
                {
                    private const int Default = 0;
                    private int _i = i;
                }
                """,
            FixedCode = """
                class C
                {
                    private const int Default = 0;
                    private int _i;

                    public C(int i = Default)
                    {
                        _i = i;
                    }
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
                class [|C(int i = C.Default)|]
                {
                    private const int Default = 0;
                    private int _i = i;
                }
                """,
            FixedCode = """
                class C
                {
                    private const int Default = 0;
                    private int _i;

                    public C(int i = Default)
                    {
                        _i = i;
                    }
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
                class [|C<T>(int i = C<T>.Default)|]
                {
                    private const int Default = 0;
                    private int _i = i;
                }
                """,
            FixedCode = """
                class C<T>
                {
                    private const int Default = 0;
                    private int _i;

                    public C(int i = Default)
                    {
                        _i = i;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }
}
