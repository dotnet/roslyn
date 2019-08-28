// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseAutoProperty;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseAutoProperty
{
    public class UseAutoPropertyTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseAutoPropertyAnalyzer(), new CSharpUseAutoPropertyCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleGetterFromField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestNullable1()
        {
            // ⚠ The expected outcome of this test should not change.
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|MutableInt? i|];

    MutableInt? P
    {
        get
        {
            return i;
        }
    }
}
struct MutableInt { public int Value; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestNullable2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly MutableInt? i|];

    MutableInt? P
    {
        get
        {
            return i;
        }
    }
}
struct MutableInt { public int Value; }",
@"class Class
{
    MutableInt? P { get; }
}
struct MutableInt { public int Value; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestNullable3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int? i|];

    int? P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int? P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestNullable4()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly int? i|];

    int? P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int? P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestNullable5()
        {
            // Recursive type check
            await TestMissingInRegularAndScriptAsync(
@"using System;
class Class
{
    [|Nullable<MutableInt?> i|];

    Nullable<MutableInt?> P
    {
        get
        {
            return i;
        }
    }
}
struct MutableInt { public int Value; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestMutableValueType1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|MutableInt i|];

    MutableInt P
    {
        get
        {
            return i;
        }
    }
}
struct MutableInt { public int Value; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestMutableValueType2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly MutableInt i|];

    MutableInt P
    {
        get
        {
            return i;
        }
    }
}
struct MutableInt { public int Value; }",
@"class Class
{
    MutableInt P { get; }
}
struct MutableInt { public int Value; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestMutableValueType3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|MutableInt i|];

    MutableInt P
    {
        get
        {
            return i;
        }
    }
}
struct MutableInt { public int Value { get; set; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestErrorType1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|ErrorType i|];

    ErrorType P
    {
        get
        {
            return i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestErrorType2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly ErrorType i|];

    ErrorType P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    ErrorType P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestErrorType3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|ErrorType? i|];

    ErrorType? P
    {
        get
        {
            return i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestErrorType4()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly ErrorType? i|];

    ErrorType? P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    ErrorType? P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")]
        public async Task TestErrorType5()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|ErrorType[] i|];

    ErrorType[] P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    ErrorType[] P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestCSharp5_1()
        {
            await TestAsync(
@"class Class
{
    [|int i|];

    public int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    public int P { get; private set; }
}",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestCSharp5_2()
        {
            await TestMissingAsync(
@"class Class
{
    [|readonly int i|];

    int P
    {
        get
        {
            return i;
        }
    }
}", new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i = 1|];

    int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int P { get; } = 1;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestInitializer_CSharp5()
        {
            await TestMissingAsync(
@"class Class
{
    [|int i = 1|];

    int P
    {
        get
        {
            return i;
        }
    }
}", new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleGetterFromProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int i;

    [|int P
    {
        get
        {
            return i;
        }
    }|]
}",
@"class Class
{
    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleSetter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        set
        {
            i = value;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestGetterAndSetter()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }

        set
        {
            i = value;
        }
    }
}",
@"class Class
{
    int P { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleGetterWithThis()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return this.i;
        }
    }
}",
@"class Class
{
    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleSetterWithThis()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        set
        {
            this.i = value;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestGetterAndSetterWithThis()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return this.i;
        }

        set
        {
            this.i = value;
        }
    }
}",
@"class Class
{
    int P { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestGetterWithMutipleStatements()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            ;
            return i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSetterWithMutipleStatements()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        set
        {
            ;
            i = value;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }

        set
        {
            ;
            i = value;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestGetterAndSetterUseDifferentFields()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];
    int j;

    int P
    {
        get
        {
            return i;
        }

        set
        {
            j = value;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldAndPropertyHaveDifferentStaticInstance()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|static int i|];

    int P
    {
        get
        {
            return i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotIfFieldUsedInRefArgument1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    void M(ref int x)
    {
        M(ref i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotIfFieldUsedInRefArgument2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    void M(ref int x)
    {
        M(ref this.i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotIfFieldUsedInOutArgument()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    void M(out int x)
    {
        M(out i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotIfFieldUsedInInArgument()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    void M(in int x)
    {
        M(in i);
    }
}");
        }

        [WorkItem(25429, "https://github.com/dotnet/roslyn/issues/25429")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotIfFieldUsedInRefExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    void M()
    {
        ref int x = ref i;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotIfFieldUsedInRefExpression_AsCandidateSymbol()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    void M()
    {
        // because we refer to 'i' statically, it only gets resolved as a candidate symbol
        // let's be conservative here and disable the analyzer if we're not sure
        ref int x = ref Class.i;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestIfUnrelatedSymbolUsedInRefExpression()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];
    int j;

    int P
    {
        get
        {
            return i;
        }
    }

    void M()
    {
        int i;
        ref int x = ref i;
        ref int y = ref j;
    }
}",
@"class Class
{
    int j;

    int P { get; }

    void M()
    {
        int i;
        ref int x = ref i;
        ref int y = ref j;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotWithVirtualProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    public virtual int P
    {
        get
        {
            return i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotWithConstField()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|const int i|];

    int P
    {
        get
        {
            return i;
        }
    }
}");
        }

        [WorkItem(25379, "https://github.com/dotnet/roslyn/issues/25379")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotWithVolatileField()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|volatile int i|];

    int P
    {
        get
        {
            return i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldWithMultipleDeclarators1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|], j, k;

    int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int j, k;

    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldWithMultipleDeclarators2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int i, [|j|], k;

    int P
    {
        get
        {
            return j;
        }
    }
}",
@"class Class
{
    int i, k;

    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldWithMultipleDeclarators3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int i, j, [|k|];

    int P
    {
        get
        {
            return k;
        }
    }
}",
@"class Class
{
    int i, j;

    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldAndPropertyInDifferentParts()
        {
            await TestInRegularAndScriptAsync(
@"partial class Class
{
    [|int i|];
}

partial class Class
{
    int P
    {
        get
        {
            return i;
        }
    }
}",
@"partial class Class
{
}

partial class Class
{
    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotWithFieldWithAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|[A]
    int i|];

    int P
    {
        get
        {
            return i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestUpdateReferences()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    public Class()
    {
        i = 1;
    }
}",
@"class Class
{
    int P { get; }

    public Class()
    {
        P = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestUpdateReferencesConflictResolution()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    public Class(int P)
    {
        i = 1;
    }
}",
@"class Class
{
    int P { get; }

    public Class(int P)
    {
        this.P = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    public Class()
    {
        i = 1;
    }
}",
@"class Class
{
    int P { get; }

    public Class()
    {
        P = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInNotInConstructor1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }

    public void Goo()
    {
        i = 1;
    }
}",
@"class Class
{
    int P { get; set; }

    public void Goo()
    {
        P = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInNotInConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    public int P
    {
        get
        {
            return i;
        }
    }

    public void Goo()
    {
        i = 1;
    }
}",
@"class Class
{
    public int P { get; private set; }

    public void Goo()
    {
        P = 1;
    }
}");
        }

        [WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInSimpleExpressionLambdaInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    [|int i|];
    int P => i;

    C()
    {
        Action<int> x = _ => i = 1;
    }
}",
@"using System;

class C
{
    int P { get; set; }

    C()
    {
        Action<int> x = _ => P = 1;
    }
}");
        }

        [WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInSimpleBlockLambdaInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    [|int i|];
    int P => i;

    C()
    {
        Action<int> x = _ =>
        {
            i = 1;
        };
    }
}",
@"using System;

class C
{
    int P { get; set; }

    C()
    {
        Action<int> x = _ =>
        {
            P = 1;
        };
    }
}");
        }

        [WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInParenthesizedExpressionLambdaInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    [|int i|];
    int P => i;

    C()
    {
        Action x = () => i = 1;
    }
}",
@"using System;

class C
{
    int P { get; set; }

    C()
    {
        Action x = () => P = 1;
    }
}");
        }

        [WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInParenthesizedBlockLambdaInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    [|int i|];
    int P => i;

    C()
    {
        Action x = () =>
        {
            i = 1;
        };
    }
}",
@"using System;

class C
{
    int P { get; set; }

    C()
    {
        Action x = () =>
        {
            P = 1;
        };
    }
}");
        }

        [WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInAnonymousMethodInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    [|int i|];
    int P => i;

    C()
    {
        Action x = delegate ()
        {
            i = 1;
        };
    }
}",
@"using System;

class C
{
    int P { get; set; }

    C()
    {
        Action x = delegate ()
        {
            P = 1;
        };
    }
}");
        }

        [WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInLocalFunctionInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [|int i|];
    int P => i;

    C()
    {
        void F()
        {
            i = 1;
        }
    }
}",
@"class C
{
    int P { get; set; }

    C()
    {
        void F()
        {
            P = 1;
        }
    }
}");
        }

        [WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInExpressionBodiedLocalFunctionInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [|int i|];
    int P => i;

    C()
    {
        void F() => i = 1;
    }
}",
@"class C
{
    int P { get; set; }

    C()
    {
        void F() => P = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestReadInExpressionBodiedLocalFunctionInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [|int i|];
    int P => i;

    C()
    {
        bool F() => i == 1;
    }
}",
@"class C
{
    int P { get; }

    C()
    {
        bool F() => P == 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestAlreadyAutoPropertyWithGetterWithNoBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    public int [|P|] { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestAlreadyAutoPropertyWithGetterAndSetterWithNoBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    public int [|P|] { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleLine1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];
    int P { get { return i; } }
}",
@"class Class
{
    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleLine2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];
    int P
    {
        get { return i; }
    }
}",
@"class Class
{
    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleLine3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];
    int P
    {
        get { return i; }
        set { i = value; }
    }
}",
@"class Class
{
    int P { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task Tuple_SingleGetterFromField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly (int, string) i|];

    (int, string) P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    (int, string) P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TupleWithNames_SingleGetterFromField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly (int a, string b) i|];

    (int a, string b) P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    (int a, string b) P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TupleWithDifferentNames_SingleGetterFromField()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|readonly (int a, string b) i|];

    (int c, string d) P
    {
        get
        {
            return i;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TupleWithOneName_SingleGetterFromField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly (int a, string) i|];

    (int a, string) P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    (int a, string) P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task Tuple_Initializer()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|readonly (int, string) i = (1, ""hello"")|];

    (int, string) P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    (int, string) P { get; } = (1, ""hello"");
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task Tuple_GetterAndSetter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|(int, string) i|];

    (int, string) P
    {
        get
        {
            return i;
        }

        set
        {
            i = value;
        }
    }
}");
        }

        [WorkItem(23215, "https://github.com/dotnet/roslyn/issues/23215")]
        [WorkItem(23216, "https://github.com/dotnet/roslyn/issues/23216")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFixAllInDocument()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    {|FixAllInDocument:int i|};

    int P
    {
        get
        {
            return i;
        }
    }

    int j;

    int Q
    {
        get
        {
            return j;
        }
    }
}",
@"class Class
{
    int P { get; }

    int Q { get; }
}");
        }

        [WorkItem(23735, "https://github.com/dotnet/roslyn/issues/23735")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExplicitInterfaceImplementationGetterOnly()
        {
            await TestMissingInRegularAndScriptAsync(@"
namespace RoslynSandbox
{
    public interface IFoo
    {
        object Bar { get; }
    }

    class Foo : IFoo
    {
        public Foo(object bar)
        {
            this.bar = bar;
        }

        readonly object [|bar|];

        object IFoo.Bar
        {
            get { return bar; }
        }
    }
}");
        }

        [WorkItem(23735, "https://github.com/dotnet/roslyn/issues/23735")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExplicitInterfaceImplementationGetterAndSetter()
        {
            await TestMissingInRegularAndScriptAsync(@"
namespace RoslynSandbox
{
    public interface IFoo
    {
        object Bar { get; set; }
    }

    class Foo : IFoo
    {
        public Foo(object bar)
        {
            this.bar = bar;
        }

        object [|bar|];

        object IFoo.Bar
        {
            get { return bar; }
            set { bar = value; }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedMemberGetOnly()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|];
    int P
    {
        get => i;
    }
}",
@"class Class
{
    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedMemberGetOnlyWithInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|] = 1;
    int P
    {
        get => i;
    }
}",
@"class Class
{
    int P { get; } = 1;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedMemberGetOnlyWithInitializerAndNeedsSetter()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|] = 1;
    int P
    {
        get => i;
    }
    void M() { i = 2; }
}",
@"class Class
{
    int P { get; set; } = 1;
    void M() { P = 2; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedMemberGetterAndSetter()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|];
    int P
    {
        get => i;
        set { i = value; }
    }
}",
@"class Class
{
    int P { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedMemberGetter()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|];
    int P => i;
}",
@"class Class
{
    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedMemberGetterWithSetterNeeded()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|];
    int P => i;
    void M() { i = 1; }
}",
@"class Class
{
    int P { get; set; }
    void M() { P = 1; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedMemberGetterWithInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|] = 1;
    int P => i;
}",
@"class Class
{
    int P { get; } = 1;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedGetterAndSetter()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|];
    int P { 
        get => i;
        set => i = value;
    }
}",
@"class Class
{
    int P { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task ExpressionBodiedGetterAndSetterWithInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int [|i|] = 1;
    int P { 
        get => i;
        set => i = value;
    }
}",
@"class Class
{
    int P { get; set; } = 1;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(25401, "https://github.com/dotnet/roslyn/issues/25401")]
        public async Task TestGetterAccessibilityDiffers()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    public int P
    {
        protected get
        {
            return i;
        }

        set
        {
            i = value;
        }
    }
}",
@"class Class
{
    public int P { protected get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(25401, "https://github.com/dotnet/roslyn/issues/25401")]
        public async Task TestSetterAccessibilityDiffers()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    public int P
    {
        get
        {
            return i;
        }

        protected set
        {
            i = value;
        }
    }
}",
@"class Class
{
    public int P { get; protected set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(26858, "https://github.com/dotnet/roslyn/issues/26858")]
        public async Task TestPreserveTrailingTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
    private readonly object [|bar|] = new object();

    public object Bar => bar;
    public int Baz => 0;
}",
@"class Goo
{
    public object Bar { get; } = new object();
    public int Baz => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(26858, "https://github.com/dotnet/roslyn/issues/26858")]
        public async Task TestPreserveTrailingTrivia2()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
    private readonly object [|bar|] = new object();

    public object Bar => bar; // prop comment
    public int Baz => 0;
}",
@"class Goo
{
    public object Bar { get; } = new object(); // prop comment
    public int Baz => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(26858, "https://github.com/dotnet/roslyn/issues/26858")]
        public async Task TestPreserveTrailingTrivia3()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
    private readonly object [|bar|] = new object();

    // doc
    public object Bar => bar; // prop comment
    public int Baz => 0;
}",
@"class Goo
{
    // doc
    public object Bar { get; } = new object(); // prop comment
    public int Baz => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        [WorkItem(26858, "https://github.com/dotnet/roslyn/issues/26858")]
        public async Task TestKeepLeadingBlank()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{

    private readonly object [|bar|] = new object();

    // doc
    public object Bar => bar; // prop comment
    public int Baz => 0;
}",
@"class Goo
{

    // doc
    public object Bar { get; } = new object(); // prop comment
    public int Baz => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsAbove1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];
    int j;

    int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int j;

    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsAbove2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int j;
    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int j;

    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsAbove3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|int i|];

    int j;

    int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int j;

    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsAbove4()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int j;

    [|int i|];

    int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    int j;

    int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsBelow1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int P
    {
        get
        {
            return i;
        }
    }

    [|int i|];
    int j;
}",
@"class Class
{
    int P { get; }

    int j;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsBelow2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int P
    {
        get
        {
            return i;
        }
    }

    int j;
    [|int i|];
}",
@"class Class
{
    int P { get; }

    int j;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsBelow3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int P
    {
        get
        {
            return i;
        }
    }

    [|int i|];

    int j;
}",
@"class Class
{
    int P { get; }

    int j;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsBelow4()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int P
    {
        get
        {
            return i;
        }
    }

    int j;

    [|int i|];
}",
@"class Class
{
    int P { get; }

    int j;
}");
        }

        [WorkItem(27675, "https://github.com/dotnet/roslyn/issues/27675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleLineWithDirective()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    #region Test
    [|int i|];
    #endregion
    
    int P
    {
        get
        {
            return i;
        }
    }
}",
@"class Class
{
    #region Test
    #endregion

    int P { get; }
}");
        }

        [WorkItem(27675, "https://github.com/dotnet/roslyn/issues/27675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestMultipleFieldsWithDirective()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    #region Test
    [|int i|];
    int j;
    #endregion

    int P
    {
        get
        {
            return i;
        }
    }

}",
@"class Class
{
    #region Test
    int j;
    #endregion

    int P { get; }

}");
        }
        [WorkItem(27675, "https://github.com/dotnet/roslyn/issues/27675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleLineWithDoubleDirectives()
        {
            await TestInRegularAndScriptAsync(
@"class TestClass
{
    #region Field
    [|int i|];
    #endregion

    #region Property
    int P
    {
        get { return i; }
    }
    #endregion
}",
@"class TestClass
{
    #region Field
    #endregion

    #region Property
    int P { get; }
    #endregion
}");
        }
    }
}
