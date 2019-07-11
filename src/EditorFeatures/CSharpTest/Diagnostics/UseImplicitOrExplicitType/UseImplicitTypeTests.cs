// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitType
{
    public partial class UseImplicitTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseImplicitTypeDiagnosticAnalyzer(), new UseImplicitTypeCodeFixProvider());

        private static readonly CodeStyleOption<bool> onWithSilent = new CodeStyleOption<bool>(true, NotificationOption.Silent);
        private static readonly CodeStyleOption<bool> offWithSilent = new CodeStyleOption<bool>(false, NotificationOption.Silent);
        private static readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private static readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);
        private static readonly CodeStyleOption<bool> onWithWarning = new CodeStyleOption<bool>(true, NotificationOption.Warning);
        private static readonly CodeStyleOption<bool> offWithWarning = new CodeStyleOption<bool>(false, NotificationOption.Warning);
        private static readonly CodeStyleOption<bool> onWithError = new CodeStyleOption<bool>(true, NotificationOption.Error);
        private static readonly CodeStyleOption<bool> offWithError = new CodeStyleOption<bool>(false, NotificationOption.Error);

        // specify all options explicitly to override defaults.
        public IDictionary<OptionKey, object> ImplicitTypeEverywhere() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo));

        private IDictionary<OptionKey, object> ImplicitTypeWhereApparent() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo));

        private IDictionary<OptionKey, object> ImplicitTypeWhereApparentAndForIntrinsics() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo));

        public IDictionary<OptionKey, object> ImplicitTypeButKeepIntrinsics() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo));

        private IDictionary<OptionKey, object> ImplicitTypeEnforcements() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithWarning),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithError),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo));

        private IDictionary<OptionKey, object> ImplicitTypeSilentEnforcement() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.VarElsewhere, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithSilent),
            SingleOption(CSharpCodeStyleOptions.VarForBuiltInTypes, onWithSilent));

        private static IDictionary<OptionKey, object> Options(OptionKey option, object value)
            => new Dictionary<OptionKey, object> { { option, value } };

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnFieldDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    [|int|] _myfield = 5;
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnFieldLikeEvents()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    public event [|D|] _myevent;
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnConstants()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        const [|int|] x = 5;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnNullLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Program|] x = null;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(27221, "https://github.com/dotnet/roslyn/issues/27221")]
        public async Task NotOnRefVar()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Program
{
    void Method()
    {
        ref [|var|] x = Method2();
    }
    ref int Method2() => throw null;
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnDynamic()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|dynamic|] x = 1;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnAnonymousMethodExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Func<string, bool>|] comparer = delegate (string value) {
            return value != ""0"";
        };
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnLambdaExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Func<int, int>|] x = y => y * y;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnMethodGroup()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Func<string, string>|] copyStr = string.Copy;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnDeclarationWithMultipleDeclarators()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|int|] x = 5, y = x;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnDeclarationWithoutInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Program|] x;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnIFormattable()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|IFormattable|] s = $""Hello, {name}""
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnFormattableString()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|FormattableString|] s = $""Hello, {name}""
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotInCatchDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        try
        {
        }
        catch ([|Exception|] e)
        {
            throw;
        }
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotDuringConflicts()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Program|] p = new Program();
    }

    class var
    {
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotIfAlreadyImplicitlyTyped()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] p = new Program();
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnImplicitConversion()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        int i = int.MaxValue;
        [|long|] l = i;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnBoxingImplicitConversion()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        int i = int.MaxValue;
        [|object|] o = i;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnRHS()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        C c = new [|C|]();
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnVariablesUsedInInitalizerExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [|int|] i = (i = 20);
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(26894, "https://github.com/dotnet/roslyn/issues/26894")]
        public async Task NotOnVariablesOfEnumTypeNamedAsEnumTypeUsedInInitalizerExpressionAtFirstPosition()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

enum A { X, Y }

class C
{
    void M()
    {
        [|A|] A = A.X;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(26894, "https://github.com/dotnet/roslyn/issues/26894")]
        public async Task NotOnVariablesNamedAsTypeUsedInInitalizerExpressionContainingTypeNameAtFirstPositionOfMemberAccess()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class A 
{ 
    public static A Instance;
}

class C
{
    void M()
    {
        [|A|] A = A.Instance;
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(26894, "https://github.com/dotnet/roslyn/issues/26894")]
        public async Task SuggestOnVariablesUsedInInitalizerExpressionAsInnerPartsOfQualifiedNameStartedWithGlobal()
        {
            await TestAsync(
@"enum A { X, Y }

class C
{
    void M()
    {
        [|A|] A = global::A.X;
    }
}",
@"enum A { X, Y }

class C
{
    void M()
    {
        var A = global::A.X;
    }
}", CSharpParseOptions.Default, options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(26894, "https://github.com/dotnet/roslyn/issues/26894")]
        public async Task SuggestOnVariablesUsedInInitalizerExpressionAsInnerPartsOfQualifiedName()
        {
            await TestInRegularAndScriptAsync(
@"using System;

namespace N
{
    class A 
    { 
        public static A Instance;
    }
}

class C
{
    void M()
    {
        [|N.A|] A = N.A.Instance;
    }
}",
@"using System;

namespace N
{
    class A 
    { 
        public static A Instance;
    }
}

class C
{
    void M()
    {
        var A = N.A.Instance;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(26894, "https://github.com/dotnet/roslyn/issues/26894")]
        public async Task SuggestOnVariablesUsedInInitalizerExpressionAsLastPartOfQualifiedName()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class A {}

class X
{ 
    public static A A;
}

class C
{
    void M()
    {
        [|A|] A = X.A;
    }
}",
@"using System;

class A {}

class X
{ 
    public static A A;
}

class C
{
    void M()
    {
        var A = X.A;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnAssignmentToInterfaceType()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public void ProcessRead()
    {
        [|IInterface|] i = new A();
    }
}

class A : IInterface
{
}

interface IInterface
{
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnArrayInitializerWithoutNewKeyword()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|int[]|] n1 = {
            2,
            4,
            6,
            8
        };
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnLocalWithIntrinsicTypeString()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|string|] s = ""hello"";
    }
}",
@"using System;

class C
{
    static void M()
    {
        var s = ""hello"";
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnIntrinsicType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|int|] s = 5;
    }
}",
@"using System;

class C
{
    static void M()
    {
        var s = 5;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(27221, "https://github.com/dotnet/roslyn/issues/27221")]
        public async Task SuggestVarOnRefIntrinsicType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        ref [|int|] s = Ref();
    }
    static ref int Ref() => throw null;
}",
@"using System;

class C
{
    static void M()
    {
        ref var s = Ref();
    }
    static ref int Ref() => throw null;
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(27221, "https://github.com/dotnet/roslyn/issues/27221")]
        public async Task WithRefIntrinsicTypeInForeach()
        {
            var before = @"
class E
{
    public ref int Current => throw null;
    public bool MoveNext() => throw null;
    public E GetEnumerator() => throw null;

    void M()
    {
        foreach (ref [|int|] x in this) { }
    }
}";
            var after = @"
class E
{
    public ref int Current => throw null;
    public bool MoveNext() => throw null;
    public E GetEnumerator() => throw null;

    void M()
    {
        foreach (ref var x in this) { }
    }
}";
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnFrameworkType()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    static void M()
    {
        [|List<int>|] c = new List<int>();
    }
}",
@"using System.Collections.Generic;

class C
{
    static void M()
    {
        var c = new List<int>();
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnUserDefinedType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [|C|] c = new C();
    }
}",
@"using System;

class C
{
    void M()
    {
        var c = new C();
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnGenericType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C<T>
{
    static void M()
    {
        [|C<int>|] c = new C<int>();
    }
}",
@"using System;

class C<T>
{
    static void M()
    {
        var c = new C<int>();
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnSeeminglyConflictingType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class var<T>
{
    void M()
    {
        [|var<int>|] c = new var<int>();
    }
}",
@"using System;

class var<T>
{
    void M()
    {
        var c = new var<int>();
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnSingleDimensionalArrayTypeWithNewOperator()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|int[]|] n1 = new int[4] { 2, 4, 6, 8 };
    }
}",
@"using System;

class C
{
    static void M()
    {
        var n1 = new int[4] { 2, 4, 6, 8 };
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnSingleDimensionalArrayTypeWithNewOperator2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|int[]|] n1 = new[] { 2, 4, 6, 8 };
    }
}",
@"using System;

class C
{
    static void M()
    {
        var n1 = new[] { 2, 4, 6, 8 };
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnSingleDimensionalJaggedArrayType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|int[][]|] cs = new[] {
            new[] { 1, 2, 3, 4 },
            new[] { 5, 6, 7, 8 }
        };
    }
}",
@"using System;

class C
{
    static void M()
    {
        var cs = new[] {
            new[] { 1, 2, 3, 4 },
            new[] { 5, 6, 7, 8 }
        };
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnDeclarationWithObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|Customer|] cc = new Customer { City = ""Madras"" };
    }

    private class Customer
    {
        public string City { get; set; }
    }
}",
@"using System;

class C
{
    static void M()
    {
        var cc = new Customer { City = ""Madras"" };
    }

    private class Customer
    {
        public string City { get; set; }
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnDeclarationWithCollectionInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        [|List<int>|] digits = new List<int> { 1, 2, 3 };
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        var digits = new List<int> { 1, 2, 3 };
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnDeclarationWithCollectionAndObjectInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        [|List<Customer>|] cs = new List<Customer>
        {
            new Customer { City = ""Madras"" }
        };
    }

    private class Customer
    {
        public string City { get; set; }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        var cs = new List<Customer>
        {
            new Customer { City = ""Madras"" }
        };
    }

    private class Customer
    {
        public string City { get; set; }
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnForStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        for ([|int|] i = 0; i < 5; i++)
        {
        }
    }
}",
@"using System;

class C
{
    static void M()
    {
        for (var i = 0; i < 5; i++)
        {
        }
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnForeachStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        var l = new List<int> { 1, 3, 5 };
        foreach ([|int|] item in l)
        {
        }
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        var l = new List<int> { 1, 3, 5 };
        foreach (var item in l)
        {
        }
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnQueryExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    static void M()
    {
        var customers = new List<Customer>();
        [|IEnumerable<Customer>|] expr = from c in customers
                                     where c.City == ""London""
                                     select c;
    }

    private class Customer
    {
        public string City { get; set; }
    }
}
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    static void M()
    {
        var customers = new List<Customer>();
        var expr = from c in customers
                                     where c.City == ""London""
                                     select c;
    }

    private class Customer
    {
        public string City { get; set; }
    }
}
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarInUsingStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        using ([|Res|] r = new Res())
        {
        }
    }

    private class Res : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}",
@"using System;

class C
{
    static void M()
    {
        using (var r = new Res())
        {
        }
    }

    private class Res : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnExplicitConversion()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        double x = 1234.7;
        [|int|] a = (int)x;
    }
}",
@"using System;

class Program
{
    void Method()
    {
        double x = 1234.7;
        var a = (int)x;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarInConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        C obj = new C();
        [|C|] anotherObj = obj?.Test();
    }

    C Test()
    {
        return this;
    }
}",
@"using System;

class C
{
    static void M()
    {
        C obj = new C();
        var anotherObj = obj?.Test();
    }

    C Test()
    {
        return this;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarInCheckedExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        long number1 = int.MaxValue + 20L;
        [|int|] intNumber = checked((int)number1);
    }
}",
@"using System;

class C
{
    static void M()
    {
        long number1 = int.MaxValue + 20L;
        var intNumber = checked((int)number1);
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarInUnCheckedExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        long number1 = int.MaxValue + 20L;
        [|int|] intNumber = unchecked((int)number1);
    }
}",
@"using System;

class C
{
    static void M()
    {
        long number1 = int.MaxValue + 20L;
        var intNumber = unchecked((int)number1);
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarInAwaitExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public async void ProcessRead()
    {
        [|string|] text = await ReadTextAsync(null);
    }

    private async Task<string> ReadTextAsync(string filePath)
    {
        return string.Empty;
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public async void ProcessRead()
    {
        var text = await ReadTextAsync(null);
    }

    private async Task<string> ReadTextAsync(string filePath)
    {
        return string.Empty;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarInParenthesizedExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void ProcessRead()
    {
        [|int|] text = (5);
    }
}",
@"using System;

class C
{
    public void ProcessRead()
    {
        var text = (5);
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task DoNotSuggestVarOnBuiltInType_Literal_WithOption()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|int|] s = 5;
    }
}", new TestParameters(options: ImplicitTypeButKeepIntrinsics()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task DoNotSuggestVarOnBuiltInType_WithOption()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    private const int maxValue = int.MaxValue;

    static void M()
    {
        [|int|] s = (unchecked(maxValue + 10));
    }
}", new TestParameters(options: ImplicitTypeButKeepIntrinsics()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task DoNotSuggestVarOnFrameworkTypeEquivalentToBuiltInType()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    private const int maxValue = int.MaxValue;

    static void M()
    {
        [|Int32|] s = (unchecked(maxValue + 10));
    }
}", new TestParameters(options: ImplicitTypeButKeepIntrinsics()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_DefaultExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        [|C|] text = default(C);
    }
}",
@"using System;

class C
{
    public void Process()
    {
        var text = default(C);
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_Literals()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        [|int|] text = 5;
    }
}",
@"using System;

class C
{
    public void Process()
    {
        var text = 5;
    }
}", options: ImplicitTypeWhereApparentAndForIntrinsics());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task DoNotSuggestVarWhereTypeIsEvident_Literals()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        [|int|] text = 5;
    }
}", new TestParameters(options: ImplicitTypeWhereApparent()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_ObjectCreationExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        [|C|] c = new C();
    }
}",
@"using System;

class C
{
    public void Process()
    {
        var c = new C();
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_CastExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        object o = DateTime.MaxValue;
        [|DateTime|] date = (DateTime)o;
    }
}",
@"using System;

class C
{
    public void Process()
    {
        object o = DateTime.MaxValue;
        var date = (DateTime)o;
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task DoNotSuggestVar_BuiltInTypesRulePrecedesOverTypeIsApparentRule1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        object o = int.MaxValue;
        [|int|] i = (Int32)o;
    }
}", new TestParameters(options: ImplicitTypeWhereApparent()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task DoNotSuggestVar_BuiltInTypesRulePrecedesOverTypeIsApparentRule2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        object o = int.MaxValue;
        [|Int32|] i = (Int32)o;
    }
}", new TestParameters(options: ImplicitTypeWhereApparent()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task DoNotSuggestVarWhereTypeIsEvident_IsExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        A a = new A();
        [|Boolean|] s = a is IInterface;
    }
}

class A : IInterface
{
}

interface IInterface
{
}", new TestParameters(options: ImplicitTypeWhereApparent()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_AsExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        A a = new A();
        [|IInterface|] s = a as IInterface;
    }
}

class A : IInterface
{
}

interface IInterface
{
}",
@"using System;

class C
{
    public void Process()
    {
        A a = new A();
        var s = a as IInterface;
    }
}

class A : IInterface
{
}

interface IInterface
{
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_ConversionHelpers()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        [|DateTime|] a = DateTime.Parse(""1"");
    }
}",
@"using System;

class C
{
    public void Process()
    {
        var a = DateTime.Parse(""1"");
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_CreationHelpers()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public void Process()
    {
        [|XElement|] a = XElement.Load();
    }
}

class XElement
{
    internal static XElement Load() => return null;
}",
@"class C
{
    public void Process()
    {
        var a = XElement.Load();
    }
}

class XElement
{
    internal static XElement Load() => return null;
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_CreationHelpersWithInferredTypeArguments()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        [|Tuple<int, bool>|] a = Tuple.Create(0, true);
    }
}",
@"using System;

class C
{
    public void Process()
    {
        var a = Tuple.Create(0, true);
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_ConvertToType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        int integralValue = 12534;
        [|DateTime|] date = Convert.ToDateTime(integralValue);
    }
}",
@"using System;

class C
{
    public void Process()
    {
        int integralValue = 12534;
        var date = Convert.ToDateTime(integralValue);
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_IConvertibleToType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void Process()
    {
        int codePoint = 1067;
        IConvertible iConv = codePoint;
        [|DateTime|] date = iConv.ToDateTime(null);
    }
}",
@"using System;

class C
{
    public void Process()
    {
        int codePoint = 1067;
        IConvertible iConv = codePoint;
        var date = iConv.ToDateTime(null);
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarNotificationLevelSilent()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        [|C|] n1 = new C();
    }
}";
            await TestDiagnosticInfoAsync(source,
                options: ImplicitTypeSilentEnforcement(),
                diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Hidden);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarNotificationLevelInfo()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        [|int|] s = 5;
    }
}";
            await TestDiagnosticInfoAsync(source,
                options: ImplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Info);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarNotificationLevelWarning()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        [|C[]|] n1 = new[] { new C() }; // type not apparent and not intrinsic
    }
}";
            await TestDiagnosticInfoAsync(source,
                options: ImplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarNotificationLevelError()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        [|C|] n1 = new C();
    }
}";
            await TestDiagnosticInfoAsync(source,
                options: ImplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Error);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(23893, "https://github.com/dotnet/roslyn/issues/23893")]
        public async Task SuggestVarOnLocalWithIntrinsicArrayType()
        {
            var before = @"class C { static void M() { [|int[]|] s = new int[0]; } }";
            var after = @"class C { static void M() { var s = new int[0]; } }";

            //The type is intrinsic and apparent
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeButKeepIntrinsics()));
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparent())); // Preference of intrinsic types dominates
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(23893, "https://github.com/dotnet/roslyn/issues/23893")]
        public async Task SuggestVarOnLocalWithCustomArrayType()
        {
            var before = @"class C { static void M() { [|C[]|] s = new C[0]; } }";
            var after = @"class C { static void M() { var s = new C[0]; } }";

            //The type is not intrinsic but apparent
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeButKeepIntrinsics());
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(23893, "https://github.com/dotnet/roslyn/issues/23893")]
        public async Task SuggestVarOnLocalWithNonApparentCustomArrayType()
        {
            var before = @"class C { static void M() { [|C[]|] s = new[] { new C() }; } }";
            var after = @"class C { static void M() { var s = new[] { new C() }; } }";

            //The type is not intrinsic and not apparent
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeButKeepIntrinsics());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparent()));
        }

        private static readonly string trivial2uple =
                    @"
namespace System
{
    public class ValueTuple
    {
        public static ValueTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) => new ValueTuple<T1, T2>(item1, item2);
    }
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2) { }
    }
} ";

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(11094, "https://github.com/dotnet/roslyn/issues/11094")]
        public async Task SuggestVarOnLocalWithIntrinsicTypeTuple()
        {
            var before = @"class C { static void M() { [|(int a, string)|] s = (a: 1, ""hello""); } }";
            var after = @"class C { static void M() { var s = (a: 1, ""hello""); } }";

            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeWhereApparentAndForIntrinsics());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparent()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(11094, "https://github.com/dotnet/roslyn/issues/11094")]
        public async Task SuggestVarOnLocalWithNonApparentTupleType()
        {
            var before = @"class C { static void M(C c) { [|(int a, C b)|] s = (a: 1, b: c); } }";
            var after = @"class C { static void M(C c) { var s = (a: 1, b: c); } }";

            await TestInRegularAndScriptAsync(before, after, options: ImplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparentAndForIntrinsics()));
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeWhereApparent()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(11154, "https://github.com/dotnet/roslyn/issues/11154")]
        public async Task ValueTupleCreate()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|ValueTuple<int, int>|] s = ValueTuple.Create(1, 1);
    }
}" + trivial2uple,
@"using System;

class C
{
    static void M()
    {
        var s = ValueTuple.Create(1, 1);
    }
}" + trivial2uple,
options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(11095, "https://github.com/dotnet/roslyn/issues/11095")]
        public async Task ValueTupleCreate_2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|(int, int)|] s = ValueTuple.Create(1, 1);
    }
}" + trivial2uple,
@"using System;

class C
{
    static void M()
    {
        var s = ValueTuple.Create(1, 1);
    }
}" + trivial2uple,
options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task TupleWithDifferentNames()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        [|(int, string)|] s = (c: 1, d: ""hello"");
    }
}",
new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(14052, "https://github.com/dotnet/roslyn/issues/14052")]
        public async Task DoNotOfferOnForEachConversionIfItChangesSemantics()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

interface IContractV1
{
}

interface IContractV2 : IContractV1
{
}

class ContractFactory
{
    public IEnumerable<IContractV1> GetContracts()
    {
    }
}

class Program
{
    static void M()
    {
        var contractFactory = new ContractFactory();
        foreach ([|IContractV2|] contract in contractFactory.GetContracts())
        {
        }
    }
}",
new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [WorkItem(14052, "https://github.com/dotnet/roslyn/issues/14052")]
        public async Task OfferOnForEachConversionIfItDoesNotChangesSemantics()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

interface IContractV1
{
}

interface IContractV2 : IContractV1
{
}

class ContractFactory
{
    public IEnumerable<IContractV1> GetContracts()
    {
    }
}

class Program
{
    static void M()
    {
        var contractFactory = new ContractFactory();
        foreach ([|IContractV1|] contract in contractFactory.GetContracts())
        {
        }
    }
}",
@"using System;
using System.Collections.Generic;

interface IContractV1
{
}

interface IContractV2 : IContractV1
{
}

class ContractFactory
{
    public IEnumerable<IContractV1> GetContracts()
    {
    }
}

class Program
{
    static void M()
    {
        var contractFactory = new ContractFactory();
        foreach (var contract in contractFactory.GetContracts())
        {
        }
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(20437, "https://github.com/dotnet/roslyn/issues/20437")]
        public async Task SuggestVarOnDeclarationExpressionSyntax()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        DateTime.TryParse(string.Empty, [|out DateTime|] date);
    }
}",
@"using System;

class C
{
    static void M()
    {
        DateTime.TryParse(string.Empty, out var date);
    }
}",
options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23893, "https://github.com/dotnet/roslyn/issues/23893")]
        public async Task DoNotSuggestVarOnDeclarationExpressionSyntaxWithIntrinsicType()
        {
            var before =
@"class C
{
    static void M(out int x)
    {
        M([|out int|] x);
    }
}";
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ImplicitTypeButKeepIntrinsics()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(22768, "https://github.com/dotnet/roslyn/issues/22768")]
        public async Task DoNotSuggestVarOnStackAllocExpressions_SpanType()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class C
{
    static void M()
    {
        [|Span<int>|] x = stackalloc int [10];
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(22768, "https://github.com/dotnet/roslyn/issues/22768")]
        public async Task DoNotSuggestVarOnStackAllocExpressions_SpanType_NestedConditional()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class C
{
    static void M(bool choice)
    {
        [|Span<int>|] x = choice ? stackalloc int [10] : stackalloc int [100];
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(22768, "https://github.com/dotnet/roslyn/issues/22768")]
        public async Task DoNotSuggestVarOnStackAllocExpressions_SpanType_NestedCast()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class C
{
    static void M()
    {
        [|Span<int>|] x = (Span<int>)stackalloc int [100];
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(22768, "https://github.com/dotnet/roslyn/issues/22768")]
        public async Task SuggestVarOnLambdasWithNestedStackAllocs()
        {
            await TestInRegularAndScriptAsync(@"
using System.Linq;
class C
{
    unsafe static void M()
    {
        [|int|] x = new int[] { 1, 2, 3 }.First(i =>
        {
            int* y = stackalloc int[10];
            return i == 1;
        });
    }
}", @"
using System.Linq;
class C
{
    unsafe static void M()
    {
        var x = new int[] { 1, 2, 3 }.First(i =>
        {
            int* y = stackalloc int[10];
            return i == 1;
        });
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(22768, "https://github.com/dotnet/roslyn/issues/22768")]
        public async Task SuggestVarOnAnonymousMethodsWithNestedStackAllocs()
        {
            await TestInRegularAndScriptAsync(@"
using System.Linq;
class C
{
    unsafe static void M()
    {
        [|int|] x = new int[] { 1, 2, 3 }.First(delegate (int i)
        {
            int* y = stackalloc int[10];
            return i == 1;
        });
    }
}", @"
using System.Linq;
class C
{
    unsafe static void M()
    {
        var x = new int[] { 1, 2, 3 }.First(delegate (int i)
        {
            int* y = stackalloc int[10];
            return i == 1;
        });
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(22768, "https://github.com/dotnet/roslyn/issues/22768")]
        public async Task SuggestVarOnStackAllocsNestedInLambdas()
        {
            await TestInRegularAndScriptAsync(@"
using System.Linq;
class C
{
    unsafe static void M()
    {
        var x = new int[] { 1, 2, 3 }.First(i =>
        {
            [|int*|] y = stackalloc int[10];
            return i == 1;
        });
    }
}", @"
using System.Linq;
class C
{
    unsafe static void M()
    {
        var x = new int[] { 1, 2, 3 }.First(i =>
        {
            var y = stackalloc int[10];
            return i == 1;
        });
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(22768, "https://github.com/dotnet/roslyn/issues/22768")]
        public async Task SuggestVarOnStackAllocsNestedInAnonymousMethods()
        {
            await TestInRegularAndScriptAsync(@"
using System.Linq;
class C
{
    unsafe static void M()
    {
        var x = new int[] { 1, 2, 3 }.First(delegate (int i)
        {
            [|int*|] y = stackalloc int[10];
            return i == 1;
        });
    }
}", @"
using System.Linq;
class C
{
    unsafe static void M()
    {
        var x = new int[] { 1, 2, 3 }.First(delegate (int i)
        {
            var y = stackalloc int[10];
            return i == 1;
        });
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(22768, "https://github.com/dotnet/roslyn/issues/22768")]
        public async Task SuggestVarOnStackAllocsInOuterMethodScope()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    unsafe static void M()
    {
        [|int*|] x = stackalloc int [10];
    }
}", @"
class C
{
    unsafe static void M()
    {
        var x = stackalloc int [10];
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23116, "https://github.com/dotnet/roslyn/issues/23116")]
        public async Task DoSuggestForDeclarationExpressionIfItWouldNotChangeOverloadResolution2()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    static int Main(string[] args)
    {
        TryGetValue(""key"", out [|int|] value);
        return value;
    }

    public static bool TryGetValue(string key, out int value) => false;
    public static bool TryGetValue(string key, out bool value, int x) => false;
}", @"
class Program
{
    static int Main(string[] args)
    {
        TryGetValue(""key"", out var value);
        return value;
    }

    public static bool TryGetValue(string key, out int value) => false;
    public static bool TryGetValue(string key, out bool value, int x) => false;
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23116, "https://github.com/dotnet/roslyn/issues/23116")]
        public async Task DoNotSuggestForDeclarationExpressionIfItWouldChangeOverloadResolution()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Program
{
    static int Main(string[] args)
    {
        TryGetValue(""key"", out [|int|] value);
        return value;
    }

    public static bool TryGetValue(string key, out object value) => false;

    public static bool TryGetValue<T>(string key, out T value) => false;
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23116, "https://github.com/dotnet/roslyn/issues/23116")]
        public async Task DoNotSuggestIfChangesGenericTypeInference()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Program
{
    static int Main(string[] args)
    {
        TryGetValue(""key"", out [|int|] value);
        return value;
    }

    public static bool TryGetValue<T>(string key, out T value) => false;
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23116, "https://github.com/dotnet/roslyn/issues/23116")]
        public async Task SuggestIfDoesNotChangeGenericTypeInference1()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    static int Main(string[] args)
    {
        TryGetValue<int>(""key"", out [|int|] value);
        return value;
    }

    public static bool TryGetValue<T>(string key, out T value) => false;
}", @"
class Program
{
    static int Main(string[] args)
    {
        TryGetValue<int>(""key"", out var value);
        return value;
    }

    public static bool TryGetValue<T>(string key, out T value) => false;
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23116, "https://github.com/dotnet/roslyn/issues/23116")]
        public async Task SuggestIfDoesNotChangeGenericTypeInference2()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    static int Main(string[] args)
    {
        TryGetValue(0, out [|int|] value);
        return value;
    }

    public static bool TryGetValue<T>(T key, out T value) => false;
}", @"
class Program
{
    static int Main(string[] args)
    {
        TryGetValue(0, out var value);
        return value;
    }

    public static bool TryGetValue<T>(T key, out T value) => false;
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23711, "https://github.com/dotnet/roslyn/issues/23711")]
        public async Task SuggestVarForDelegateType()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    static void Main(string[] args)
    {
        [|GetHandler|] handler = Handler;
    }

    private static GetHandler Handler;

    delegate object GetHandler();
}", @"
class Program
{
    static void Main(string[] args)
    {
        var handler = Handler;
    }

    private static GetHandler Handler;

    delegate object GetHandler();
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23711, "https://github.com/dotnet/roslyn/issues/23711")]
        public async Task DoNotSuggestVarForDelegateType1()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Program
{
    static void Main(string[] args)
    {
        [|GetHandler|] handler = () => new object();
    }

    private static GetHandler Handler;

    delegate object GetHandler();
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23711, "https://github.com/dotnet/roslyn/issues/23711")]
        public async Task DoNotSuggestVarForDelegateType2()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Program
{
    static void Main(string[] args)
    {
        [|GetHandler|] handler = Foo;
    }

    private static GetHandler Handler;

    private static object Foo() => new object();

    delegate object GetHandler();
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(23711, "https://github.com/dotnet/roslyn/issues/23711")]
        public async Task DoNotSuggestVarForDelegateType3()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Program
{
    static void Main(string[] args)
    {
        [|GetHandler|] handler = delegate { return new object(); };
    }

    private static GetHandler Handler;

    delegate object GetHandler();
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(24262, "https://github.com/dotnet/roslyn/issues/24262")]
        public async Task DoNotSuggestVarForInterfaceVariableInForeachStatement()
        {
            await TestMissingInRegularAndScriptAsync(@"
public interface ITest
{
    string Value { get; }
}
public class TestInstance : ITest
{
    string ITest.Value => ""Hi"";
}

public class Test
{
    public TestInstance[] Instances { get; }

    public void TestIt()
    {
        foreach ([|ITest|] test in Instances)
        {
            Console.WriteLine(test.Value);
        }
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(24262, "https://github.com/dotnet/roslyn/issues/24262")]
        public async Task DoNotSuggestVarForInterfaceVariableInDeclarationStatement()
        {
            await TestMissingInRegularAndScriptAsync(@"
public interface ITest
{
    string Value { get; }
}
public class TestInstance : ITest
{
    string ITest.Value => ""Hi"";
}

public class Test
{
    public void TestIt()
    {
        [|ITest|] test = new TestInstance();
        Console.WriteLine(test.Value);
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(24262, "https://github.com/dotnet/roslyn/issues/24262")]
        public async Task DoNotSuggestVarForAbstractClassVariableInForeachStatement()
        {
            await TestMissingInRegularAndScriptAsync(@"
public abstract class MyAbClass
{
    string Value { get; }
}

public class TestInstance : MyAbClass
{
    public string Value => ""Hi"";
}

public class Test
{
    public TestInstance[] Instances { get; }

    public void TestIt()
    {
        foreach ([|MyAbClass|] instance in Instances)
        {
            Console.WriteLine(instance);
        }
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(24262, "https://github.com/dotnet/roslyn/issues/24262")]
        public async Task DoNotSuggestVarForAbstractClassVariableInDeclarationStatement()
        {
            await TestMissingInRegularAndScriptAsync(@"
public abstract class MyAbClass
{
    string Value { get; }
}

public class TestInstance : MyAbClass
{
    public string Value => ""Hi"";
}

public class Test
{
    public TestInstance[] Instances { get; }

    public void TestIt()
    {
        [|MyAbClass|]  test = new TestInstance();
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [Fact]
        public async Task DoNoSuggestVarForRefForeachVar()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System;
namespace System
{
    public readonly ref struct Span<T>
    {
        unsafe public Span(void* pointer, int length) { }

        public ref SpanEnum GetEnumerator() => throw new Exception();

        public struct SpanEnum
        {
            public ref int Current => 0;
            public bool MoveNext() => false;
        }
    }
}
class C
{
    public void M(Span<int> span)
    {
        foreach ([|ref|] var rx in span)
        {
        }
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }

        [Fact, WorkItem(26923, "https://github.com/dotnet/roslyn/issues/26923")]
        public async Task NoSuggestionOnForeachCollectionExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void Main(string[] args)
    {
        foreach (string arg in [|args|])
        {

        }
    }
}", new TestParameters(options: ImplicitTypeEverywhere()));
        }
    }
}
