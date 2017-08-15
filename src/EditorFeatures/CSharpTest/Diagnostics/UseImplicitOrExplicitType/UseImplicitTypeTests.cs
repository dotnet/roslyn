// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitType
{
    public partial class UseImplicitTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseImplicitTypeDiagnosticAnalyzer(), new UseImplicitTypeCodeFixProvider());

        private static readonly CodeStyleOption<bool> onWithNone = new CodeStyleOption<bool>(true, NotificationOption.None);
        private static readonly CodeStyleOption<bool> offWithNone = new CodeStyleOption<bool>(false, NotificationOption.None);
        private static readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private static readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);
        private static readonly CodeStyleOption<bool> onWithWarning = new CodeStyleOption<bool>(true, NotificationOption.Warning);
        private static readonly CodeStyleOption<bool> offWithWarning = new CodeStyleOption<bool>(false, NotificationOption.Warning);
        private static readonly CodeStyleOption<bool> onWithError = new CodeStyleOption<bool>(true, NotificationOption.Error);
        private static readonly CodeStyleOption<bool> offWithError = new CodeStyleOption<bool>(false, NotificationOption.Error);

        // specify all options explicitly to override defaults.
        public IDictionary<OptionKey, object> ImplicitTypeEverywhere() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo));

        private IDictionary<OptionKey, object> ImplicitTypeWhereApparent() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo));

        private IDictionary<OptionKey, object> ImplicitTypeWhereApparentAndForIntrinsics() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo));

        public IDictionary<OptionKey, object> ImplicitTypeButKeepIntrinsics() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo));

        private IDictionary<OptionKey, object> ImplicitTypeEnforcements() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithWarning),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithError),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo));

        private IDictionary<OptionKey, object> ImplicitTypeNoneEnforcement() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithNone),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithNone),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithNone));

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
        public async Task SuggestVarOnFrameworkTypeEquivalentToBuiltInType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    private const int maxValue = int.MaxValue;

    static void M()
    {
        [|Int32|] s = (unchecked(maxValue + 10));
    }
}",
@"using System;

class C
{
    private const int maxValue = int.MaxValue;

    static void M()
    {
        var s = (unchecked(maxValue + 10));
    }
}", options: ImplicitTypeButKeepIntrinsics());
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
        object o = int.MaxValue;
        [|Int32|] i = (Int32)o;
    }
}",
@"using System;

class C
{
    public void Process()
    {
        object o = int.MaxValue;
        var i = (Int32)o;
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVar_BuiltInTypesRulePrecedesOverTypeIsApparentRule()
        {
            // The option settings here say
            // "use explicit type for built-in types" and
            // "use implicit type where apparent".
            // The rationale for preferring explicit type for built-in types is
            // they have short names and using var doesn't gain anything.
            // Accordingly, the `built-in type` rule precedes over the `where apparent` rule
            // and we do not suggest `use var` here.
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
        public async Task SuggestVarWhereTypeIsEvident_IsExpression()
        {
            await TestInRegularAndScriptAsync(
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
}",
@"using System;

class C
{
    public void Process()
    {
        A a = new A();
        var s = a is IInterface;
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
        [|Int32|] a = int.Parse(""1"");
    }
}",
@"using System;

class C
{
    public void Process()
    {
        var a = int.Parse(""1"");
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
        [|Decimal|] decimalValue = Convert.ToDecimal(integralValue);
    }
}",
@"using System;

class C
{
    public void Process()
    {
        int integralValue = 12534;
        var decimalValue = Convert.ToDecimal(integralValue);
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
        [|Char|] ch = iConv.ToChar(null);
    }
}",
@"using System;

class C
{
    public void Process()
    {
        int codePoint = 1067;
        IConvertible iConv = codePoint;
        var ch = iConv.ToChar(null);
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarNotificationLevelNone()
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
            await TestMissingInRegularAndScriptAsync(source,
                new TestParameters(options: ImplicitTypeNoneEnforcement()));
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
        [|int[]|] n1 = new[] {2, 4, 6, 8};
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

        private static string trivial2uple =
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

            // We would rather this refactoring also worked. See https://github.com/dotnet/roslyn/issues/11094
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
    }
}
