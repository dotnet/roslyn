// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.UseImplicitTyping;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypingStyles;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitTyping
{
    public partial class UseImplicitTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace) =>
            new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseImplicitTypingDiagnosticAnalyzer(), new UseImplicitTypingCodeFixProvider());

        private readonly SimpleCodeStyleOption onWithNone = new SimpleCodeStyleOption(true, NotificationOption.None);
        private readonly SimpleCodeStyleOption offWithNone = new SimpleCodeStyleOption(false, NotificationOption.None);
        private readonly SimpleCodeStyleOption onWithInfo = new SimpleCodeStyleOption(true, NotificationOption.Info);
        private readonly SimpleCodeStyleOption offWithInfo = new SimpleCodeStyleOption(false, NotificationOption.Info);
        private readonly SimpleCodeStyleOption onWithWarning = new SimpleCodeStyleOption(true, NotificationOption.Warning);
        private readonly SimpleCodeStyleOption offWithWarning = new SimpleCodeStyleOption(false, NotificationOption.Warning);
        private readonly SimpleCodeStyleOption onWithError = new SimpleCodeStyleOption(true, NotificationOption.Error);
        private readonly SimpleCodeStyleOption offWithError = new SimpleCodeStyleOption(false, NotificationOption.Error);

        // specify all options explicitly to override defaults.
        private IDictionary<OptionKey, object> ImplicitTypingEverywhere() => 
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypingWhereApparent() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypingWhereApparentAndForIntrinsics() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypingButKeepIntrinsics() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, offWithInfo)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypingEnforcements() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithWarning)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithError)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo);

        private IDictionary<OptionKey, object> ImplicitTypingNoneEnforcement() =>
            Options(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithNone)
            .With(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithNone)
            .With(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithNone);

        private IDictionary<OptionKey, object> Options(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>();
            options.Add(option, value);
            return options;
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task NotOnFieldDeclaration()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    [|int|] _myfield = 5;
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task NotOnFieldLikeEvents()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    public event [|D|] _myevent;
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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

}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
}", options: ImplicitTypingEverywhere());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task DoNotSuggestVarOnIntrinsicTypeWithOption()
        {
            await TestMissingAsync(
@"using System;
class C
{
    static void M()
    {
        [|int|] s = 5;
    }
}", options: ImplicitTypingButKeepIntrinsics());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_DefaultExpression()
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
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_Literals()
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
}", options: ImplicitTypingWhereApparentAndForIntrinsics());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task DoNotSuggestVarWhereTypingIsEvident_Literals()
        {
            await TestMissingAsync(
@"using System;
class C
{
    public void Process()
    {
        [|int|] text = 5;
    }
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_ObjectCreationExpression()
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
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_CastExpression()
        {
            await TestAsync(
@"using System;
class C
{
    public void Process()
    {
        object o = int.MaxValue;
        [|int|] i = (int)o;
    }
}",
@"using System;
class C
{
    public void Process()
    {
        object o = int.MaxValue;
        var i = (int)o;
    }
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_IsExpression()
        {
            await TestAsync(
@"using System;
class C
{
    public void Process()
    {
        A a = new A();
        [|bool|] s = a is IInterface;
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

}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_AsExpression()
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

}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_ConversionHelpers()
        {
            await TestAsync(
@"using System;
class C
{
    public void Process()
    {
        [|int|] a = int.Parse(""1"");
    }
}",
@"using System;
class C
{
    public void Process()
    {
        var a = int.Parse(""1"");
    }
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_CreationHelpers()
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
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_CreationHelpersWithInferredTypeArguments()
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
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_ConvertToType()
        {
            await TestAsync(
@"using System;
class C
{
    public void Process()
    {
        int integralValue = 12534;
        [|decimal|] decimalValue = Convert.ToDecimal(integralValue);
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
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarWhereTypingIsEvident_IConvertibleToType()
        {
            await TestAsync(
@"using System;
class C
{
    public void Process()
    {
        int codePoint = 1067;
        IConvertible iConv = codePoint;
        [|char|] ch = iConv.ToChar(null);
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
}", options: ImplicitTypingWhereApparent());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
            await TestMissingAsync(source, ImplicitTypingNoneEnforcement());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
                options: ImplicitTypingEnforcements(), 
                diagnosticCount: 1, 
                diagnosticId: IDEDiagnosticIds.UseImplicitTypingDiagnosticId, 
                diagnosticSeverity: DiagnosticSeverity.Info);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
                options: ImplicitTypingEnforcements(),
                diagnosticCount: 1,
                diagnosticId: IDEDiagnosticIds.UseImplicitTypingDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
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
                options: ImplicitTypingEnforcements(),
                diagnosticCount: 1,
                diagnosticId: IDEDiagnosticIds.UseImplicitTypingDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Error);
        }
    }
}