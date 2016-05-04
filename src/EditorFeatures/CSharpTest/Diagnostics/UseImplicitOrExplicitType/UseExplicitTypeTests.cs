// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseExplicitType
{
    public partial class UseExplicitTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace) =>
            new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseExplicitTypeDiagnosticAnalyzer(), new UseExplicitTypeCodeFixProvider());

        private readonly SimpleCodeStyleOption onWithNone = new SimpleCodeStyleOption(true, NotificationOption.None);
        private readonly SimpleCodeStyleOption offWithNone = new SimpleCodeStyleOption(false, NotificationOption.None);
        private readonly SimpleCodeStyleOption onWithInfo = new SimpleCodeStyleOption(true, NotificationOption.Info);
        private readonly SimpleCodeStyleOption offWithInfo = new SimpleCodeStyleOption(false, NotificationOption.Info);
        private readonly SimpleCodeStyleOption onWithWarning = new SimpleCodeStyleOption(true, NotificationOption.Warning);
        private readonly SimpleCodeStyleOption offWithWarning = new SimpleCodeStyleOption(false, NotificationOption.Warning);
        private readonly SimpleCodeStyleOption onWithError = new SimpleCodeStyleOption(true, NotificationOption.Error);
        private readonly SimpleCodeStyleOption offWithError = new SimpleCodeStyleOption(false, NotificationOption.Error);

        // specify all options explicitly to override defaults.
        private IDictionary<OptionKey, object> ExplicitTypeEverywhere() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo);

        private IDictionary<OptionKey, object> ExplicitTypeExceptWhereApparent() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo);

        private IDictionary<OptionKey, object> ExplicitTypeForBuiltInTypesOnly() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo);

        private IDictionary<OptionKey, object> ExplicitTypeEnforcements() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithWarning)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithError)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo);

        private IDictionary<OptionKey, object> ExplicitTypeNoneEnforcement() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithNone)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, offWithNone)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithNone);

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
            await TestMissingAsync(
@"using System;
class Program
{
    [|var|] _myfield = 5;
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnFieldLikeEvents()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    public event [|var|] _myevent;
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnAnonymousMethodExpression()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] comparer = delegate(string value) {
            return value != ""0"";
        };
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnLambdaExpression()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = y => y * y;
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnDeclarationWithMultipleDeclarators()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = 5, y = x;
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnDeclarationWithoutInitializer()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x;
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotDuringConflicts()
        {
            await TestMissingAsync(
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
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotIfAlreadyExplicitlyTyped()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
         [|Program|] p = new Program();
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnRHS()
        {
            await TestMissingAsync(
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

}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnErrorSymbol()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = new Foo();
    }
}", options: ExplicitTypeEverywhere());
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnDynamic()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|dynamic|] x = 1;
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnAnonymousType()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = new { Amount = 108, Message = ""Hello"" };
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnArrayOfAnonymousType()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = new[] { new { name = ""apple"", diam = 4 }, new { name = ""grape"", diam = 1 }};
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task NotOnEnumerableOfAnonymousTypeFromAQueryExpression()
        {
            await TestMissingAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
class Program
{
    void Method()
    {
        var products = new List<Product>();
        [|var|] productQuery =
            from prod in products
            select new { prod.Color, prod.Price };
    }
}
class Product
{
    public ConsoleColor Color { get; set; }
    public int Price { get; set; }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeString()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] n1 = new int[4] {2, 4, 6, 8};
    }
}",
@"using System;
class C
{
    static void M()
    {
        int[] n1 = new int[4] {2, 4, 6, 8};
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator2()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] n1 = new[] {2, 4, 6, 8};
    }
}",
@"using System;
class C
{
    static void M()
    {
        int[] n1 = new[] {2, 4, 6, 8};
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnSingleDimensionalJaggedArrayType()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] cs = new[]
        {
            new[]{1,2,3,4},
            new[]{5,6,7,8}
        };
    }
}",
@"using System;
class C
{
    static void M()
    {
        int[][] cs = new[]
        {
            new[]{1,2,3,4},
            new[]{5,6,7,8}
        };
    }
}", options: ExplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
        public async Task SuggestExplicitTypeOnDeclarationWithObjectInitializer()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    static void M()
    {
        var customers = new List<Customer>();
        [|var|] expr =
            from c in customers
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
        IEnumerable<Customer> expr =
            from c in customers
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestMissingAsync(source, ExplicitTypeNoneEnforcement());
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
            await TestDiagnosticSeverityAndCountAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticCount: 1,
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
            await TestDiagnosticSeverityAndCountAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticCount: 1,
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
            await TestDiagnosticSeverityAndCountAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticCount: 1,
                diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Error);
        }
    }
}