// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertPrimaryToRegularConstructor;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertPrimaryToRegularConstructor;

using VerifyCS = CSharpCodeRefactoringVerifier<ConvertPrimaryToRegularConstructorCodeRefactoringProvider>;

[UseExportProvider]
public sealed class ConvertPrimaryToRegularConstructorTests
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
    public Task TestInCSharp12()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotWithRecord()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestStruct()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithThisChainedConstructor()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithBaseChainedConstructor1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithBaseChainedConstructor2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithBaseChainedConstructor3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithBaseChainedConstructor4()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithBaseChainedConstructor5()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithReferenceOnlyInExistingSameNamedField()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithReferenceOnlyInPropertyInitializer1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithReferenceInDifferentNamedField()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithComplexFieldInitializer1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithComplexFieldInitializer2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithComplexFieldInitializer3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersWithFieldInitializers1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersWithFieldInitializers2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithParametersReferencedOutsideOfConstructor()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithParametersReferencedOutsideOfConstructor_Mutation1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithoutParametersReferencedOutsideOfConstructor()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestAssignmentToPropertyOfDifferentType()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestAssignedToFieldUsedInNestedType()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithNotMutatedReferencesOutsideOfConstructor1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestEscapedParameterNames()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithNotMutatedReferencesOutsideOfConstructor2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithNotMutatedReferencesOutsideOfConstructor3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestGenerateUnderscoreName1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestGenerateUnderscoreName2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestComplexFieldInitializer()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs4()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs5()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs6()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs7()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs9()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs10()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs11()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs13()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs14()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs15()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs16()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs17()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs18()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs19()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs20()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs21()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveParamDocs22()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveConstructorAttributes1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveConstructorAttributes1A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveConstructorAttributes2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveConstructorAttributes2A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveConstructorAttributes3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveConstructorAttributes3A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveConstructorAttributes4()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMoveConstructorAttributes4A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersMove1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersMove1A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersMove2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersMove2A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersMove3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersMove3A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersMove4()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleParametersMove4A()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestReferenceToNestedType1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestReferenceToNestedType2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestReferenceToNestedType3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestReferenceToNestedType4()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestInParameter1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestInParameter2_Unused()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithRegionDirective1()
        => new VerifyCS.Test
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

                    #region constructors

                    #endregion
                    public C(int i)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestWithRegionDirective2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestSeeTag1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestSeeTag2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestSeeTag3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestReferenceToConstantInParameterInitializer1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestReferenceToConstantInParameterInitializer2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestReferenceToConstantInParameterInitializer3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72340")]
    public Task TestClassWithoutBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class [|Class1()|];
                """,
            FixedCode = """
                class Class1
                {
                    public Class1()
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestNotOnExtension()
        => new VerifyCS.Test
        {
            TestCode = """
                static class Class1
                {
                    [|extension(string s)|]
                    {
                        public void Goo() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76981")]
    public Task TestConvertWithReferencesToParameter1()
        => new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    internal class [|Goo(int bar)|]
                    {
                        public int Bar { get; private set; } = bar;

                        public void Baz()
                        {
                            Bar = bar;
                        }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    internal class Goo
                    {
                        private readonly int bar;

                        public Goo(int bar)
                        {
                            this.bar = bar;
                            Bar = bar;
                        }

                        public int Bar { get; private set; }

                        public void Baz()
                        {
                            Bar = bar;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_NotUsed()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                    }
                    """, """
                    partial class C
                    {
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        public C(int i)
                        {
                        }
                    }
                    """, """
                    partial class C
                    {
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_UsedInSamePartialPart()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                        public int I { get; } = i;
                    }
                    """, """
                    partial class C
                    {
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        public int I { get; }

                        public C(int i)
                        {
                            I = i;
                        }
                    }
                    """, """
                    partial class C
                    {
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_UsedInOtherPartialPart()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                    }
                    """, """
                    partial class C
                    {
                        public int I { get; } = i;
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        public C(int i)
                        {
                            I = i;
                        }
                    }
                    """, """
                    partial class C
                    {
                        public int I { get; }
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_UsedInAllPartialParts()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                        public int I1 { get; } = i + 1;
                    }
                    """, """
                    partial class C
                    {
                        public int I2 { get; } = i + 2;
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        public int I1 { get; }

                        public C(int i)
                        {
                            I1 = i + 1;
                            I2 = i + 2;
                        }
                    }
                    """, """
                    partial class C
                    {
                        public int I2 { get; }
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_CapturedInSamePartialPart_SameFieldName()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                        int M()
                        {
                            return i;
                        }
                    }
                    """, """
                    partial class C
                    {
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
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
                    """, """
                    partial class C
                    {
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_CapturedInSamePartialPart_UnderscoreFieldName()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                        int M()
                        {
                            return i;
                        }
                    }
                    """, """
                    partial class C
                    {
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
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
                    """, """
                    partial class C
                    {
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_CapturedInOtherPartialPart_SameFieldName()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                    }
                    """, """
                    partial class C
                    {
                        int M()
                        {
                            return i;
                        }
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        private readonly int i;

                        public C(int i)
                        {
                            this.i = i;
                        }
                    }
                    """, """
                    partial class C
                    {
                        int M()
                        {
                            return i;
                        }
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_CapturedInOtherPartialPart_UnderscoreFieldName()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                    }
                    """, """
                    partial class C
                    {
                        int M()
                        {
                            return i;
                        }
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
                    {
                        private readonly int _i;

                        public C(int i)
                        {
                            _i = i;
                        }
                    }
                    """, """
                    partial class C
                    {
                        int M()
                        {
                            return _i;
                        }
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_CapturedInAllPartialParts_SameFieldName()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                        int M()
                        {
                            return i;
                        }
                    }
                    """, """
                    partial class C
                    {
                        int N()
                        {
                            return i;
                        }
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
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
                    """, """
                    partial class C
                    {
                        int N()
                        {
                            return i;
                        }
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79077")]
    public Task TestOnPartialType_CapturedInAllPartialParts_UnderscoreFieldName()
        => new VerifyCS.Test()
        {
            TestState =
            {
                Sources =
                {
                    """
                    partial class [|C(int i)|]
                    {
                        int M()
                        {
                            return i;
                        }
                    }
                    """, """
                    partial class C
                    {
                        int N()
                        {
                            return i;
                        }
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    partial class C
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
                    """, """
                    partial class C
                    {
                        int N()
                        {
                            return _i;
                        }
                    }
                    """
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = FieldNamesCamelCaseWithFieldUnderscorePrefixEditorConfig,
        }.RunAsync();
}
