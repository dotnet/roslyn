// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitType
{
    public partial class UseImplicitTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace) =>
            new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseImplicitTypeDiagnosticAnalyzer(), new UseImplicitTypeCodeFixProvider());

        private readonly SimpleCodeStyleOption onWithNone = new SimpleCodeStyleOption(true, NotificationOption.None);
        private readonly SimpleCodeStyleOption offWithNone = new SimpleCodeStyleOption(false, NotificationOption.None);
        private readonly SimpleCodeStyleOption onWithInfo = new SimpleCodeStyleOption(true, NotificationOption.Info);
        private readonly SimpleCodeStyleOption offWithInfo = new SimpleCodeStyleOption(false, NotificationOption.Info);
        private readonly SimpleCodeStyleOption onWithWarning = new SimpleCodeStyleOption(true, NotificationOption.Warning);
        private readonly SimpleCodeStyleOption offWithWarning = new SimpleCodeStyleOption(false, NotificationOption.Warning);
        private readonly SimpleCodeStyleOption onWithError = new SimpleCodeStyleOption(true, NotificationOption.Error);
        private readonly SimpleCodeStyleOption offWithError = new SimpleCodeStyleOption(false, NotificationOption.Error);

        // specify all options explicitly to override defaults.
        private IDictionary<OptionKey, object> ImplicitTypeEverywhere() => 
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypeWhereApparent() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypeWhereApparentAndForIntrinsics() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypeButKeepIntrinsics() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypeEnforcements() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithWarning)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithError)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypeNoneEnforcement() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithNone)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithNone)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithNone);

        private IDictionary<OptionKey, object> Options(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>();
            options.Add(option, value);
            return options;
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnFieldDeclaration()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    [|int|] _myfield = 5;
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnFieldLikeEvents()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    public event [|D|] _myevent;
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnConstants()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        const [|int|] x = 5;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnNullLiteral()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|Program|] x = null;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
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
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnAnonymousMethodExpression()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|Func<string, bool>|] comparer = delegate(string value) {
            return value != ""0"";
        };
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnLambdaExpression()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|Func<int, int>|] x = y => y * y;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnMethodGroup()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|Func<string, string>|] copyStr = string.Copy;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnDeclarationWithMultipleDeclarators()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|int|] x = 5, y = x;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnDeclarationWithoutInitializer()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|Program|] x;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnIFormattable()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|IFormattable|] s = $""Hello, {name}""
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnFormattableString()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|FormattableString|] s = $""Hello, {name}""
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotInCatchDeclaration()
        {
            await TestMissingAsync(
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
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotDuringConflicts()
        {
            await TestMissingAsync(
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
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotIfAlreadyImplicitlyTyped()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
         [|var|] p = new Program();
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnImplicitConversion()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        int i = int.MaxValue;
        [|long|] l = i;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnBoxingImplicitConversion()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        int i = int.MaxValue;
        [|object|] o = i;
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnRHS()
        {
            await TestMissingAsync(
@"using System;
class C
{
    void M()
    {
        C c = new [|C|]();
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnVariablesUsedInInitalizerExpression()
        {
            await TestMissingAsync(
@"using System;
class C
{
    void M()
    {
        [|int|] i = (i = 20);
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnAssignmentToInterfaceType()
        {
            await TestMissingAsync(
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

}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task NotOnArrayInitializerWithoutNewKeyword()
        {
            await TestMissingAsync(
@"using System;
class C
{
    static void M()
    {
        [|int[]|] n1 = {2, 4, 6, 8};
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnLocalWithIntrinsicTypeString()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|int[]|] n1 = new int[4] {2, 4, 6, 8};
    }
}",
@"using System;
class C
{
    static void M()
    {
        var n1 = new int[4] {2, 4, 6, 8};
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnSingleDimensionalArrayTypeWithNewOperator2()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|int[]|] n1 = new[] {2, 4, 6, 8};
    }
}",
@"using System;
class C
{
    static void M()
    {
        var n1 = new[] {2, 4, 6, 8};
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnSingleDimensionalJaggedArrayType()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|int[][]|] cs = new[]
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
        var cs = new[]
        {
            new[]{1,2,3,4},
            new[]{5,6,7,8}
        };
    }
}", options: ImplicitTypeEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnDeclarationWithObjectInitializer()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
@"using System;
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
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    static void M()
    {
        var customers = new List<Customer>();
        [|IEnumerable<Customer>|] expr =
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
        var expr =
            from c in customers
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestMissingAsync(
@"using System;
class C
{
    static void M()
    {
        [|int|] s = 5;
    }
}", options: ImplicitTypeButKeepIntrinsics());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task DoNotSuggestVarOnBuiltInType_WithOption()
        {
            await TestMissingAsync(
@"using System;
class C
{
    private const int maxValue = int.MaxValue;

    static void M()
    {
        [|int|] s = (unchecked(maxValue + 10));
    }
}", options: ImplicitTypeButKeepIntrinsics());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarOnFrameworkTypeEquivalentToBuiltInType()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestMissingAsync(
@"using System;
class C
{
    public void Process()
    {
        [|int|] text = 5;
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_ObjectCreationExpression()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestMissingAsync(
@"using System;
class C
{
    public void Process()
    {
        object o = int.MaxValue;
        [|int|] i = (Int32)o;
    }
}", options: ImplicitTypeWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        public async Task SuggestVarWhereTypeIsEvident_IsExpression()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestMissingAsync(source, ImplicitTypeNoneEnforcement());
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
            await TestDiagnosticSeverityAndCountAsync(source, 
                options: ImplicitTypeEnforcements(), 
                diagnosticCount: 1, 
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
            await TestDiagnosticSeverityAndCountAsync(source,
                options: ImplicitTypeEnforcements(),
                diagnosticCount: 1,
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
            await TestDiagnosticSeverityAndCountAsync(source,
                options: ImplicitTypeEnforcements(),
                diagnosticCount: 1,
                diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Error);
        }
    }
}