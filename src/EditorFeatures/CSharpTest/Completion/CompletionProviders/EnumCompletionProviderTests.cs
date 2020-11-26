// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class EnumCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType() => typeof(EnumCompletionProvider);

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEditorBrowsable_EnumTypeDotMemberAlways()
        {
            var markup = @"
class P
{
    public void S()
    {
        MyEnum d = $$;
    }
}";
            var referencedCode = @"
public enum MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    Member
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEditorBrowsable_EnumTypeDotMemberNever()
        {
            var markup = @"
class P
{
    public void S()
    {
        MyEnum d = $$;
    }
}";
            var referencedCode = @"
public enum MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEditorBrowsable_EnumTypeDotMemberAdvanced()
        {
            var markup = @"
class P
{
    public void S()
    {
        MyEnum d = $$;
    }
}";
            var referencedCode = @"
public enum MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTriggeredOnOpenParen()
        {
            var markup = @"
static class Program
{
    public static void Main(string[] args)
    {
        // type after this line
        Bar($$
    }

    public static void Bar(Goo f)
    {
    }
}

enum Goo
{
    AMember,
    BMember,
    CMember
}
";
            await VerifyItemExistsAsync(markup, "Goo.AMember", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRightSideOfAssignment()
        {
            var markup = @"
static class Program
{
    public static void Main(string[] args)
    {
        Goo x;
        x = $$;
    }
}

enum Goo
{
    AMember,
    BMember,
    CMember
}
";

            await VerifyItemExistsAsync(markup, "Goo.AMember", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCaseStatement()
        {
            var markup = @"
enum E
{
    A,
    B,
    C
}

static class Module1
{
    public static void Main(string[] args)
    {
        var value = E.A;

        switch (value)
        {
            case $$
        }
    }
}
";

            await VerifyItemExistsAsync(markup, "E.A", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInYieldReturn()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<DayOfWeek> M()
    {
        yield return $$;
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.Friday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInAsyncMethodReturnStatement()
        {
            var markup = @"
using System;
using System.Threading.Tasks;

class C
{
    public async Task<DayOfWeek> M()
    {
        await Task.Delay(1);
        return $$;
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.Friday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInIndexedProperty()
        {
            var markup = @"
static class Module1
{
    public enum MyEnum
    {
        flower
    }

    public class MyClass1
    {
        public bool this[MyEnum index]
        {
            set
            {
            }
        }
    }

    public static void Main()
    {
        var c = new MyClass1();
        c[$$MyEnum.flower] = true;
    }
}
";

            await VerifyItemExistsAsync(markup, "MyEnum.flower");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestFullyQualified()
        {
            var markup = @"
class C
{
    public void M(System.DayOfWeek day)
    {
        M($$);
    }

    enum DayOfWeek
    {
        A,
        B
    }
}
";

            await VerifyItemExistsAsync(markup, "System.DayOfWeek.Friday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTriggeredForNamedArgument()
        {
            var markup = @"
class C
{
    public void M(DayOfWeek day)
    {
        M(day:$$);
    }

    enum DayOfWeek
    {
        A,
        B
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.A", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestNotTriggeredAfterAssignmentEquals()
        {
            var markup = @"
class C
{
    public void M(DayOfWeek day)
    {
        var x = $$;
    }

    enum DayOfWeek
    {
        A,
        B
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "DayOfWeek.A", usePreviousCharAsTrigger: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCaseStatementWithInt32InferredType()
        {
            var markup = @"
class C
{
    public void M(DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.A:
                break;

            case $$
        }
    }

    enum DayOfWeek
    {
        A,
        B
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.A");
            await VerifyItemExistsAsync(markup, "DayOfWeek.B");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestLocalNoAs()
        {
            var markup = @"
enum E
{
    A
}

class C
{
    public void M()
    {
        const E e = e$$;
    }
}
";

            await VerifyItemExistsAsync(markup, "e");
            await VerifyItemIsAbsentAsync(markup, "e as E");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestIncludeEnumAfterTyping()
        {
            var markup = @"
enum E
{
    A
}

class C
{
    public void M()
    {
        const E e = e$$;
    }
}
";

            await VerifyItemExistsAsync(markup, "E");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCommitOnComma()
        {
            var markup = @"
enum E
{
    A
}

class C
{
    public void M()
    {
        const E e = $$
    }
}
";

            var expected = @"
enum E
{
    A
}

class C
{
    public void M()
    {
        const E e = E.A;
    }
}
";

            await VerifyProviderCommitAsync(markup, "E.A", expected, ';');
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterDot()
        {
            var markup = @"
static class Module1
{
    public static void Main()
    {
        while (System.Console.ReadKey().Key == System.ConsoleKey.$$
        {
        }
    }
}
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInCollectionInitializer1()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class C
{
    public void Main()
    {
        var y = new List<DayOfWeek>()
        {
            $$
        };
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.Monday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInCollectionInitializer2()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class C
{
    public void Main()
    {
        var y = new List<DayOfWeek>()
        {
            DayOfWeek.Monday,
            $$
        };
    }
}
";

            await VerifyItemExistsAsync(markup, "DayOfWeek.Monday");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumHasFlag()
        {
            var markup = @"
using System.IO;

class C
{
    public void Main()
    {
        FileInfo f;
        f.Attributes.HasFlag($$
    }
}
";

            await VerifyItemExistsAsync(markup, "FileAttributes.Hidden");
        }
    }
}
