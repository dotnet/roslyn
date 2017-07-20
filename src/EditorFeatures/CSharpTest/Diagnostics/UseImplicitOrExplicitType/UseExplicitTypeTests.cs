﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseExplicitType
{
    public partial class UseExplicitTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseExplicitTypeDiagnosticAnalyzer(), new UseExplicitTypeCodeFixProvider());

        private readonly CodeStyleOption<bool> onWithNone = new CodeStyleOption<bool>(true, NotificationOption.None);
        private readonly CodeStyleOption<bool> offWithNone = new CodeStyleOption<bool>(false, NotificationOption.None);
        private readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> offWithInfo = new CodeStyleOption<bool>(false, NotificationOption.Suggestion);
        private readonly CodeStyleOption<bool> onWithWarning = new CodeStyleOption<bool>(true, NotificationOption.Warning);
        private readonly CodeStyleOption<bool> offWithWarning = new CodeStyleOption<bool>(false, NotificationOption.Warning);
        private readonly CodeStyleOption<bool> onWithError = new CodeStyleOption<bool>(true, NotificationOption.Error);
        private readonly CodeStyleOption<bool> offWithError = new CodeStyleOption<bool>(false, NotificationOption.Error);

        // specify all options explicitly to override defaults.
        private IDictionary<OptionKey, object> ExplicitTypeEverywhere() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo));

        private IDictionary<OptionKey, object> ExplicitTypeExceptWhereApparent() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo));

        private IDictionary<OptionKey, object> ExplicitTypeForBuiltInTypesOnly() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo));

        private IDictionary<OptionKey, object> ExplicitTypeEnforcements() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithWarning),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithError),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo));

        private IDictionary<OptionKey, object> ExplicitTypeNoneEnforcement() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithNone),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithNone),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithNone));

        private IDictionary<OptionKey, object> Options(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>();
            options.Add(option, value);
            return options;
        }

        #region Error Cases

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnFieldDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    [|var|] _myfield = 5;
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnFieldLikeEvents()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    public event [|var|] _myevent;
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnAnonymousMethodExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] comparer = delegate (string value) {
            return value != ""0"";
        };
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnLambdaExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] x = y => y * y;
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnDeclarationWithMultipleDeclarators()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] x = 5, y = x;
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnDeclarationWithoutInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] x;
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotDuringConflicts()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] p = new var();
    }

    class var
    {
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotIfAlreadyExplicitlyTyped()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|Program|] p = new Program();
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnRHS()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var c = new [|var|]();
    }
}

class var
{
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnErrorSymbol()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] x = new Foo();
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
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
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnForEachVarWithAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Linq;

class Program
{
    void Method()
    {
        var values = Enumerable.Range(1, 5).Select(i => new { Value = i });

        foreach ([|var|] value in values)
        {
            Console.WriteLine(value.Value);
        }
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task OnForEachVarWithExplicitType()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Linq;

class Program
{
    void Method()
    {
        var values = Enumerable.Range(1, 5);

        foreach ([|var|] value in values)
        {
            Console.WriteLine(value.Value);
        }
    }
}",
@"using System;
using System.Linq;

class Program
{
    void Method()
    {
        var values = Enumerable.Range(1, 5);

        foreach (int value in values)
        {
            Console.WriteLine(value.Value);
        }
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] x = new { Amount = 108, Message = ""Hello"" };
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnArrayOfAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] x = new[] { new { name = ""apple"", diam = 4 }, new { name = ""grape"", diam = 1 } };
    }
}", new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnEnumerableOfAnonymousTypeFromAQueryExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Method()
    {
        var products = new List<Product>();
        [|var|] productQuery = from prod in products
                           select new { prod.Color, prod.Price };
    }
}

class Product
{
    public ConsoleColor Color { get; set; }
    public int Price { get; set; }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeString()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|var|] s = ""hello"";
    }
}",
@"using System;

class C
{
    static void M()
    {
        string s = ""hello"";
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnIntrinsicType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|var|] s = 5;
    }
}",
@"using System;

class C
{
    static void M()
    {
        int s = 5;
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnFrameworkType()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    static void M()
    {
        [|var|] c = new List<int>();
    }
}",
@"using System.Collections.Generic;

class C
{
    static void M()
    {
        List<int> c = new List<int>();
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnUserDefinedType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        [|var|] c = new C();
    }
}",
@"using System;

class C
{
    void M()
    {
        C c = new C();
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnGenericType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C<T>
{
    static void M()
    {
        [|var|] c = new C<int>();
    }
}",
@"using System;

class C<T>
{
    static void M()
    {
        C<int> c = new C<int>();
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|var|] n1 = new int[4] { 2, 4, 6, 8 };
    }
}",
@"using System;

class C
{
    static void M()
    {
        int[] n1 = new int[4] { 2, 4, 6, 8 };
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|var|] n1 = new[] { 2, 4, 6, 8 };
    }
}",
@"using System;

class C
{
    static void M()
    {
        int[] n1 = new[] { 2, 4, 6, 8 };
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnSingleDimensionalJaggedArrayType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|var|] cs = new[] {
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
        int[][] cs = new[] {
            new[] { 1, 2, 3, 4 },
            new[] { 5, 6, 7, 8 }
        };
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnDeclarationWithObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        [|var|] cc = new Customer { City = ""Madras"" };
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
        Customer cc = new Customer { City = ""Madras"" };
    }

    private class Customer
    {
        public string City { get; set; }
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnDeclarationWithCollectionInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        [|var|] digits = new List<int> { 1, 2, 3 };
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<int> digits = new List<int> { 1, 2, 3 };
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnDeclarationWithCollectionAndObjectInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        [|var|] cs = new List<Customer>
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
        List<Customer> cs = new List<Customer>
        {
            new Customer { City = ""Madras"" }
        };
    }

    private class Customer
    {
        public string City { get; set; }
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnForStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        for ([|var|] i = 0; i < 5; i++)
        {
        }
    }
}",
@"using System;

class C
{
    static void M()
    {
        for (int i = 0; i < 5; i++)
        {
        }
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnForeachStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void M()
    {
        var l = new List<int> { 1, 3, 5 };
        foreach ([|var|] item in l)
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
        foreach (int item in l)
        {
        }
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnQueryExpression()
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
        [|var|] expr = from c in customers
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
        IEnumerable<Customer> expr = from c in customers
                                     where c.City == ""London""
                                     select c;
    }

    private class Customer
    {
        public string City { get; set; }
    }
}
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeInUsingStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        using ([|var|] r = new Res())
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
        using (Res r = new Res())
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
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnInterpolatedString()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    void Method()
    {
        [|var|] s = $""Hello, {name}""
    }
}",
@"using System;

class Program
{
    void Method()
    {
        string s = $""Hello, {name}""
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnExplicitConversion()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        double x = 1234.7;
        [|var|] a = (int)x;
    }
}",
@"using System;

class C
{
    static void M()
    {
        double x = 1234.7;
        int a = (int)x;
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        C obj = new C();
        [|var|] anotherObj = obj?.Test();
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
        C anotherObj = obj?.Test();
    }

    C Test()
    {
        return this;
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeInCheckedExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        long number1 = int.MaxValue + 20L;
        [|var|] intNumber = checked((int)number1);
    }
}",
@"using System;

class C
{
    static void M()
    {
        long number1 = int.MaxValue + 20L;
        int intNumber = checked((int)number1);
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeInAwaitExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public async void ProcessRead()
    {
        [|var|] text = await ReadTextAsync(null);
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
        string text = await ReadTextAsync(null);
    }

    private async Task<string> ReadTextAsync(string filePath)
    {
        return string.Empty;
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeInBuiltInNumericType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void ProcessRead()
    {
        [|var|] text = 1;
    }
}",
@"using System;

class C
{
    public void ProcessRead()
    {
        int text = 1;
    }
}", options: ExplicitTypeForBuiltInTypesOnly());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeInBuiltInCharType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void ProcessRead()
    {
        [|var|] text = GetChar();
    }

    public char GetChar() => 'c';
}",
@"using System;

class C
{
    public void ProcessRead()
    {
        char text = GetChar();
    }

    public char GetChar() => 'c';
}", options: ExplicitTypeForBuiltInTypesOnly());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeInBuiltInType_string()
        {
            // though string isn't an intrinsic type per the compiler
            // we in the IDE treat it as an intrinsic type for this feature.
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void ProcessRead()
    {
        [|var|] text = string.Empty;
    }
}",
@"using System;

class C
{
    public void ProcessRead()
    {
        string text = string.Empty;
    }
}", options: ExplicitTypeForBuiltInTypesOnly());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeInBuiltInType_object()
        {
            // object isn't an intrinsic type per the compiler
            // we in the IDE treat it as an intrinsic type for this feature.
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public void ProcessRead()
    {
        object j = new C();
        [|var|] text = j;
    }
}",
@"using System;

class C
{
    public void ProcessRead()
    {
        object j = new C();
        object text = j;
    }
}", options: ExplicitTypeForBuiltInTypesOnly());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeNotificationLevelNone()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        [|var|] n1 = new C();
    }
}";
            await TestMissingInRegularAndScriptAsync(source,
                new TestParameters(options: ExplicitTypeNoneEnforcement()));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeNotificationLevelInfo()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        [|var|] s = 5;
    }
}";
            await TestDiagnosticInfoAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Info);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeNotificationLevelWarning()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        [|var|] n1 = new[] {2, 4, 6, 8};
    }
}";
            await TestDiagnosticInfoAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestExplicitTypeNotificationLevelError()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        [|var|] n1 = new C();
    }
}";
            await TestDiagnosticInfoAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Error);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTuple()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        [|var|] s = (1, ""hello"");
    }
}",
@"class C
{
    static void M()
    {
        (int, string) s = (1, ""hello"");
    }
}",
options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTupleWithNames()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        [|var|] s = (a: 1, b: ""hello"");
    }
}",
@"class C
{
    static void M()
    {
        (int a, string b) s = (a: 1, b: ""hello"");
    }
}",
options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTupleWithOneName()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        [|var|] s = (a: 1, ""hello"");
    }
}",
@"class C
{
    static void M()
    {
        (int a, string) s = (a: 1, ""hello"");
    }
}",
options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        [WorkItem(20437, "https://github.com/dotnet/roslyn/issues/20437")]
        public async Task SuggestExplicitTypeOnDeclarationExpressionSyntax()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    static void M()
    {
        DateTime.TryParse(string.Empty, [|out var|] date);
    }
}",
@"using System;

class C
{
    static void M()
    {
        DateTime.TryParse(string.Empty, out DateTime date);
    }
}",
options: ExplicitTypeEverywhere());
        }
    }
}
