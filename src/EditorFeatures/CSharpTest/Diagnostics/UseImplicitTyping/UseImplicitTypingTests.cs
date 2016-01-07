// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.UseImplicitTyping;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.UseImplicitTyping;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitTyping
{
    public class UseImplicitTypingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseImplicitTypingDiagnosticAnalyzer(), new UseImplicitTypingCodeFixProvider());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task NotOnFieldDeclaration()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    [|int|] _myfield = 5;
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task NotOnFieldLikeEvents()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    public event [|D|] _myevent;
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
        }

        [WpfFact(Skip = "TODO"), Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task NotOnImplicitConversion()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
    }
}");
        }

        // TODO: should we or should we not? also, check boxing cases.
        [WpfFact(Skip = "TODO"), Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task NotOnExplicitConversion()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
    }
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
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
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarOnDeclarationWithCollectionInitializer()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|List<int>|] digits = new List<int> { 1, 2, 3 };
    }
}",
@"using System;
class C
{
    static void M()
    {
        var digits = new List<int> { 1, 2, 3 };
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitTyping)]
        public async Task SuggestVarOnDeclarationWithCollectionAndObjectInitializers()
        {
            await TestAsync(
@"using System;
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
}");
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
}");
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
}");
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
}");
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
}");
        }

        // TODO: Tests for ConditionalAccessExpression.
        // TODO: Tests with various options - where apparent, primitive types etc.
    }
}