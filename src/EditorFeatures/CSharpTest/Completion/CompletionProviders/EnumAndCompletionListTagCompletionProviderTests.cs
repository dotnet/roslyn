﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class EnumAndCompletionListTagCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(EnumAndCompletionListTagCompletionProvider);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NullableEnum()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
        Colors? d = $$
        Colors c = Colors.Blue;
    }
}
";
            var colors = @"
enum Colors
{
    Red,
    Blue,
    Green,
}
";
            var colorsLike = @"
readonly struct Colors
{
    public static readonly Colors Red;
    public static readonly Colors Blue;
    public static readonly Colors Green;
}
";

            await VerifyItemExistsAsync(markup + colors, "Colors");
            await VerifyItemIsAbsentAsync(markup + colorsLike, "Colors");
        }

        [Fact]
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_EnumMemberAlways()
        {
            var markup = @"
class Program
{
    public void M()
    {
        Goo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public enum Goo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_EnumMemberNever()
        {
            var markup = @"
class Program
{
    public void M()
    {
        Goo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public enum Goo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact]
        [WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_EnumMemberAdvanced()
        {
            var markup = @"
class Program
{
    public void M()
    {
        Goo d = $$
    }
}
";
            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public enum Goo
{
    Member
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(854099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854099")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInComment()
        {
            var markup = @"class Program
{
    static void Main(string[] args)
    {
        Colors c = // $$
    }
";
            var colors = @"
enum Colors
{
    Red,
    Blue,
    Green,
}
";
            var colorsLike = @"
readonly struct Colors
{
    public static readonly Colors Red;
    public static readonly Colors Blue;
    public static readonly Colors Green;
}
";

            await VerifyNoItemsExistAsync(markup + colors);
            await VerifyNoItemsExistAsync(markup + colorsLike);
        }

        [WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task InYieldReturnInMethod(string typeName)
        {
            var markup =
$@"using System;
using System.Collections.Generic;

class Program
{{
    IEnumerable<{typeName}> M()
    {{
        yield return $$
    }}
}}";

            if (typeName == nameof(DayOfWeek))
                await VerifyItemExistsAsync(markup, typeName);
            else
                await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [WorkItem(30235, "https://github.com/dotnet/roslyn/issues/30235")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task InYieldReturnInLocalFunction(string typeName)
        {
            var markup =
$@"using System;
using System.Collections.Generic;

class Program
{{
    void M()
    {{
        IEnumerable<{typeName}> F()
        {{
            yield return $$
        }}
    }}
}}";

            if (typeName == nameof(DayOfWeek))
                await VerifyItemExistsAsync(markup, typeName);
            else
                await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task InAsyncMethodReturnStatement(string typeName)
        {
            var markup =
$@"using System;
using System.Threading.Tasks;

class Program
{{
    async Task<{typeName}> M()
    {{
        await Task.Delay(1);
        return $$
    }}
}}";

            if (typeName == nameof(DayOfWeek))
                await VerifyItemExistsAsync(markup, typeName);
            else
                await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task InSimpleLambdaAfterArrow(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<bool, {typeName}> M()
    {{
        return _ => $$
    }}
}}";

            if (typeName == nameof(DayOfWeek))
                await VerifyItemExistsAsync(markup, typeName);
            else
                await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task InParenthesizedLambdaAfterArrow(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<{typeName}> M()
    {{
        return () => $$
    }}
}}";

            if (typeName == nameof(DayOfWeek))
                await VerifyItemExistsAsync(markup, typeName);
            else
                await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task NotInAnonymousMethodAfterParameterList(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<{typeName}> M()
    {{
        return delegate () $$
    }}
}}";
            await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task NotInSimpleLambdaAfterAsync(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<bool, {typeName}> M()
    {{
        return async $$ _ =>
    }}
}}";
            await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task NotInParenthesizedLambdaAfterAsync(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<{typeName}> M()
    {{
        return async $$ () =>
    }}
}}";
            await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task NotInAnonymousMethodAfterAsync(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<{typeName}> M()
    {{
        return async $$ delegate ()
    }}
}}";
            await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task NotInSimpleLambdaBlock(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<bool, {typeName}> M()
    {{
        return _ => {{ $$ }}
    }}
}}";
            await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task NotInParenthesizedLambdaBlock(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<{typeName}> M()
    {{
        return () => {{ $$ }}
    }}
}}";
            await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task NotInAnonymousMethodBlock(string typeName)
        {
            var markup =
$@"using System;

class Program
{{
    Func<{typeName}> M()
    {{
        return delegate () {{ $$ }}
    }}
}}";
            await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task InExpressionTreeSimpleLambdaAfterArrow(string typeName)
        {
            var markup =
$@"using System;
using System.Linq.Expressions;

class Program
{{
    Expression<Func<bool, {typeName}>> M()
    {{
        return _ => $$
    }}
}}";

            if (typeName == nameof(DayOfWeek))
                await VerifyItemExistsAsync(markup, typeName);
            else
                await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task InExpressionTreeParenthesizedLambdaAfterArrow(string typeName)
        {
            var markup =
$@"using System;
using System.Linq.Expressions;

class Program
{{
    Expression<Func<{typeName}>> M()
    {{
        return () => $$
    }}
}}";

            if (typeName == nameof(DayOfWeek))
                await VerifyItemExistsAsync(markup, typeName);
            else
                await VerifyItemIsAbsentAsync(markup, typeName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoCompletionListTag()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

class C
{
    
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionList()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""C""/>
class C
{
    
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyItemExistsAsync(markup, "C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionListCrefToString()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""string""/>
class C
{
    
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyItemExistsAsync(markup, "string", glyph: (int)Glyph.ClassPublic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionListEmptyCref()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""""/>
class C
{
    
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionListInaccessibleType()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""C.Inner""/>
class C
{
    private class Inner
    {   
    }
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionListNotAType()
        {
            var markup =
@"using System;
using System.Threading.Tasks;

/// <completionlist cref=""C.Z()""/>
class C
{
    public void Z()
    {   
    }
}

class Program
{
    void Goo()
    {
        C c = $$
    }
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("System.Globalization.DigitShapes")]
        [InlineData("System.DateTime")]
        public async Task SuggestAlias(string fullTypeName)
        {
            var markup = $@"
using D = {fullTypeName}; 
class Program
{{
    static void Main(string[] args)
    {{
        D d=  $$
    }}
}}";

            if (fullTypeName == "System.Globalization.DigitShapes")
                await VerifyItemExistsAsync(markup, "D");
            else
                await VerifyItemIsAbsentAsync(markup, "D");
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("System.Globalization.DigitShapes")]
        [InlineData("System.DateTime")]
        public async Task SuggestAlias2(string fullTypeName)
        {
            var markup = $@"
namespace N
{{
using D = {fullTypeName}; 

class Program
{{
    static void Main(string[] args)
    {{
        D d=  $$
    }}
}}
}}
";

            if (fullTypeName == "System.Globalization.DigitShapes")
                await VerifyItemExistsAsync(markup, "D");
            else
                await VerifyItemIsAbsentAsync(markup, "D");
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("System.Globalization.DigitShapes")]
        [InlineData("System.DateTime")]
        public async Task SuggestAlias3(string fullTypeName)
        {
            var markup = $@"
namespace N
{{
using D = {fullTypeName}; 

class Program
{{
    private void Goo({fullTypeName} shape)
    {{
    }}

    static void Main(string[] args)
    {{
        Goo($$
    }}
}}
}}
";

            if (fullTypeName == "System.Globalization.DigitShapes")
                await VerifyItemExistsAsync(markup, "D");
            else
                await VerifyItemIsAbsentAsync(markup, "D");
        }

        [WorkItem(828196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828196")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInParameterNameContext()
        {
            var enumE = @"
enum E
{
    a
}
";
            var enumLikeE = @"
readonly struct E
{
    public static readonly E a;
}
";
            var markup = @"
class C
{
    void goo(E first, E second) 
    {
        goo(first: E.a, $$
    }
}
";

            await VerifyItemIsAbsentAsync(enumE + markup, "E");

            await VerifyItemIsAbsentAsync(enumLikeE + markup, "E");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InExpressionBodiedProperty()
        {
            var markup =
@"class C
{
    Colors Colors => $$
}
";
            var colors = @"
enum Colors
{
    Red,
    Blue,
    Green,
}
";
            var colorsLike = @"
readonly struct Colors
{
    public static readonly Colors Red;
    public static readonly Colors Blue;
    public static readonly Colors Green;
}
";

            await VerifyItemExistsAsync(markup + colors, "Colors");
            await VerifyItemIsAbsentAsync(markup + colorsLike, "Colors");
        }

        [WorkItem(4310, "https://github.com/dotnet/roslyn/issues/4310")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InExpressionBodiedMethod()
        {
            var markup =
@"class C
{
    Colors GetColors() => $$
}
";
            var colors = @"
enum Colors
{
    Red,
    Blue,
    Green,
}
";
            var colorsLike = @"
readonly struct Colors
{
    public static readonly Colors Red;
    public static readonly Colors Blue;
    public static readonly Colors Green;
}
";

            await VerifyItemExistsAsync(markup + colors, "Colors");
            await VerifyItemIsAbsentAsync(markup + colorsLike, "Colors");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterDot()
        {
            var markup =
@"namespace ConsoleApplication253
{
    class Program
    {
        static void Main(string[] args)
        {
            M(E.$$)
        }

        static void M(E e) { }
    }
}
";
            var enumE = @"
enum E
{
    A,
    B,
}
";
            var enumLikeE = @"
readonly struct E
{
    public static readonly E A;
    public static readonly E B;
}
";

            await VerifyNoItemsExistAsync(markup + enumE);
            await VerifyNoItemsExistAsync(markup + enumLikeE);
        }

        [WorkItem(18359, "https://github.com/dotnet/roslyn/issues/18359")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterDotWithTextTyped()
        {
            var markup =
@"namespace ConsoleApplication253
{
    class Program
    {
        static void Main(string[] args)
        {
            M(E.a$$)
        }

        static void M(E e) { }
    }
}
";
            var enumE = @"
enum E
{
    A,
    B,
}
";
            var enumLikeE = @"
readonly struct E
{
    public static readonly E A;
    public static readonly E B;
}
";

            await VerifyNoItemsExistAsync(markup + enumE);
            await VerifyNoItemsExistAsync(markup + enumLikeE);
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer1()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath | Visibility | $$
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer2()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath | $$ Visibility
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer3()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath | $$ | Visibility
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer4()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath ^ $$
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [WorkItem(5419, "https://github.com/dotnet/roslyn/issues/5419")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumInitializer5()
        {
            var markup =
@"using System;

[Flags]
internal enum ProjectTreeWriterOptions
{
    None,
    Tags,
    FilePath,
    Capabilities,
    Visibility,
    AllProperties = FilePath & $$
}";
            await VerifyItemExistsAsync(markup, "ProjectTreeWriterOptions");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInEnumHasFlag()
        {
            var markup =
@"using System.IO;

class C
{
    void M()
    {
        FileInfo f;
        f.Attributes.HasFlag($$
    }
}";
            await VerifyItemExistsAsync(markup, "FileAttributes");
        }

        #region enum members

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
            var referencedCode_EnumLike = @"
public readonly struct MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public static readonly MyEnum Member;
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode_EnumLike,
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
            var referencedCode_EnumLike = @"
public readonly struct MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static readonly MyEnum Member;
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode_EnumLike,
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
            var referencedCode_EnumLike = @"
public readonly struct MyEnum
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static readonly MyEnum Member;
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

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode_EnumLike,
                item: "MyEnum.Member",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode_EnumLike,
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
";
            var goo = @"
enum Goo
{
    AMember,
    BMember,
    CMember
}
";
            var gooLike = @"
readonly struct Goo
{
    public static readonly Goo AMember;
    public static readonly Goo BMember;
    public static readonly Goo CMember;
}
";

            await VerifyItemExistsAsync(markup + goo, "Goo.AMember", usePreviousCharAsTrigger: true);
            await VerifyItemExistsAsync(markup + goo, "Goo.AMember", usePreviousCharAsTrigger: false);
            await VerifyItemIsAbsentAsync(markup + gooLike, "Goo.AMember", usePreviousCharAsTrigger: true);
            await VerifyItemExistsAsync(markup + gooLike, "Goo.AMember", usePreviousCharAsTrigger: false);
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
";
            var goo = @"
enum Goo
{
    AMember,
    BMember,
    CMember
}
";
            var gooLike = @"
readonly struct Goo
{
    public static readonly Goo AMember;
    public static readonly Goo BMember;
    public static readonly Goo CMember;
}
";

            await VerifyItemExistsAsync(markup + goo, "Goo.AMember", usePreviousCharAsTrigger: true);
            await VerifyItemExistsAsync(markup + goo, "Goo.AMember", usePreviousCharAsTrigger: false);
            await VerifyItemIsAbsentAsync(markup + gooLike, "Goo.AMember", usePreviousCharAsTrigger: true);
            await VerifyItemExistsAsync(markup + gooLike, "Goo.AMember", usePreviousCharAsTrigger: false);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCaseStatement()
        {
            var markup = @"
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
            var e = @"
enum E
{
    A,
    B,
    C
}
";
            var eLike = @"
readonly struct E
{
    public static readonly E A;
    public static readonly E B;
    public static readonly E C;
}
";

            await VerifyItemExistsAsync(e + markup, "E.A", usePreviousCharAsTrigger: true);
            await VerifyItemExistsAsync(e + markup, "E.A", usePreviousCharAsTrigger: false);
            await VerifyItemIsAbsentAsync(eLike + markup, "E.A", usePreviousCharAsTrigger: true);
            await VerifyItemExistsAsync(eLike + markup, "E.A", usePreviousCharAsTrigger: false);
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
        [InlineData(nameof(DateTime), nameof(DateTime.Now))]
        public async Task TestInYieldReturn(string typeName, string memberName)
        {
            var markup = $@"
using System;
using System.Collections.Generic;

class C
{{
    public IEnumerable<{typeName}> M()
    {{
        yield return $$;
    }}
}}
";

            await VerifyItemExistsAsync(markup, $"{typeName}.{memberName}");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
        [InlineData(nameof(DateTime), nameof(DateTime.Now))]
        public async Task TestInAsyncMethodReturnStatement(string typeName, string memberName)
        {
            var markup = $@"
using System;
using System.Threading.Tasks;

class C
{{
    public async Task<{typeName}> M()
    {{
        await Task.Delay(1);
        return $$;
    }}
}}
";

            await VerifyItemExistsAsync(markup, $"{typeName}.{memberName}");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
        [InlineData(nameof(DateTime), nameof(DateTime.Now))]
        public async Task TestInIndexedProperty(string typeName, string memberName)
        {
            var markup = $@"
using System;
static class Module1
{{
    public class MyClass1
    {{
        public bool this[{typeName} index]
        {{
            set
            {{
            }}
        }}
    }}

    public static void Main()
    {{
        var c = new MyClass1();
        c[$${typeName}.{memberName}] = true;
    }}
}}
";

            await VerifyItemExistsAsync(markup, $"{typeName}.{memberName}");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
        [InlineData(nameof(DateTime), nameof(DateTime.Now))]
        public async Task TestFullyQualified(string typeName, string memberName)
        {
            var markup = $@"
class C
{{
    public void M(System.{typeName} day)
    {{
        M($$);
    }}

    enum DayOfWeek
    {{
        A,
        B
    }}

    struct DateTime
    {{
        public static readonly DateTime A;
        public static readonly DateTime B;
    }}
}}
";

            await VerifyItemExistsAsync(markup, $"System.{typeName}.{memberName}");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task TestTriggeredForNamedArgument(string typeName)
        {
            var markup = $@"
class C
{{
    public void M({typeName} day)
    {{
        M(day: $$);
    }}

    enum DayOfWeek
    {{
        A,
        B
    }}

    struct DateTime
    {{
        public static readonly DateTime A;
        public static readonly DateTime B;
    }}
}}
";

            if (typeName == nameof(DayOfWeek))
                await VerifyItemExistsAsync(markup, $"{typeName}.A", usePreviousCharAsTrigger: true);
            else
                await VerifyItemIsAbsentAsync(markup, $"{typeName}.A", usePreviousCharAsTrigger: true);

            await VerifyItemExistsAsync(markup, $"{typeName}.A", usePreviousCharAsTrigger: false);
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek))]
        [InlineData(nameof(DateTime))]
        public async Task TestNotTriggeredAfterAssignmentEquals(string typeName)
        {
            var markup = $@"
class C
{{
    public void M({typeName} day)
    {{
        var x = $$;
    }}

    enum DayOfWeek
    {{
        A,
        B
    }}

    struct DateTime
    {{
        public static readonly DateTime A;
        public static readonly DateTime B;
    }}
}}
";

            await VerifyItemIsAbsentAsync(markup, $"{typeName}.A", usePreviousCharAsTrigger: true);
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

            await VerifyItemExistsAsync(markup, "E.A");
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

            await VerifyItemExistsAsync(markup, "E.A");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestNotInTrivia()
        {
            var markup = @"
class C
{
    public void M(DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.A:
            case DayOfWeek.B// $$
                :
                    break;
        }
    }

    enum DayOfWeek
    {
        A,
        B
    }
}
";
            await VerifyNoItemsExistAsync(markup);
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

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(ConsoleKey))]
        [InlineData(nameof(DateTime))]
        public async Task EnumMember_NotAfterDot(string typeName)
        {
            var markup = $@"
static class Module1
{{
    public static void Main({typeName} x)
    {{
        while (x == System.{typeName}.$$
        {{
        }}
    }}
}}
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Monday))]
        [InlineData(nameof(DateTime), nameof(DateTime.Now))]
        public async Task TestInCollectionInitializer1(string typeName, string memberName)
        {
            var markup = $@"
using System;
using System.Collections.Generic;

class C
{{
    public void Main()
    {{
        var y = new List<{typeName}>()
        {{
            $$
        }};
    }}
}}
";

            await VerifyItemExistsAsync(markup, $"{typeName}.{memberName}");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Monday))]
        [InlineData(nameof(DateTime), nameof(DateTime.Now))]
        public async Task TestInCollectionInitializer2(string typeName, string memberName)
        {
            var markup = $@"
using System;
using System.Collections.Generic;

class C
{{
    public void Main()
    {{
        var y = new List<{typeName}>()
        {{
            {typeName}.{memberName},
            $$
        }};
    }}
}}
";

            await VerifyItemExistsAsync(markup, $"{typeName}.{memberName}");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EnumMember_TestInEnumHasFlag()
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestMultipleEnumsCausedByOverloads()
        {
            var markup = @"
class C
{
    public enum Color
    {
        Red,
        Green,
    }

    public enum Palette
    {
        AccentColor1,
        AccentColor2,
    }

    public readonly struct ColorLike
    {
        public static readonly ColorLike Red;
        public static readonly ColorLike Green;
    }

    public readonly struct PaletteLike
    {
        public static readonly PaletteLike AccentColor1;
        public static readonly PaletteLike AccentColor2;
    }

    public void SetColor(Color color) { }
    public void SetColor(Palette palette) { }
    public void SetColor(ColorLike color) { }
    public void SetColor(PaletteLike palette) { }

    public void Main()
    {
        SetColor($$
    }
}
";

            await VerifyItemExistsAsync(markup, "Color.Red");
            await VerifyItemExistsAsync(markup, "Palette.AccentColor1");

            await VerifyItemExistsAsync(markup, "ColorLike.Red");
            await VerifyItemExistsAsync(markup, "PaletteLike.AccentColor1");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
        [InlineData(nameof(DateTime), nameof(DateTime.Now))]
        public async Task TestNullableEnum(string typeName, string memberName)
        {
            var markup = $@"
using System;
class C
{{
    public void SetValue({typeName}? value) {{ }}

    public void Main()
    {{
        SetValue($$
    }}
}}
";
            await VerifyItemExistsAsync(markup, $"{typeName}.{memberName}");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData(nameof(DayOfWeek), nameof(DayOfWeek.Friday))]
        [InlineData(nameof(DateTime), nameof(DateTime.Now))]
        public async Task TestTypeAlias(string typeName, string memberName)
        {
            var markup = $@"
using AT = System.{typeName};

public class Program
{{
    static void M(AT attributeTargets) {{ }}
    
    public static void Main()
    {{
        M($$
    }}
}}";
            await VerifyItemExistsAsync(markup, $"AT.{memberName}");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("")]
        [InlineData("Re")]
        [InlineData("Col")]
        [InlineData("Color.Green or", false)]
        [InlineData("Color.Green or ")]
        [InlineData("(Color.Green or ")] // start of: is (Color.Red or Color.Green) and not Color.Blue
        [InlineData("Color.Green or Re")]
        [InlineData("Color.Green or Color.Red or ")]
        [InlineData("Color.Green orWrittenWrong ", false)]
        [InlineData("not ")]
        [InlineData("not Re")]
        public async Task TestPatterns_Is_ConstUnaryAndBinaryPattern(string isPattern, bool shouldOfferRed = true)
        {
            var markup = @$"
class C
{{
    public enum Color
    {{
        Red,
        Green,
    }}

    public void M(Color c)
    {{
        var isRed = c is {isPattern}$$;
    }}
}}
";
            if (shouldOfferRed)
            {
                await VerifyItemExistsAsync(markup, "Color.Red");
            }
            else
            {
                await VerifyItemIsAbsentAsync(markup, "Color.Red");
            }
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("")]
        [InlineData("Col")]
        [InlineData("Red")]
        [InlineData("Color.Green or ")]
        [InlineData("Color.Green or Re")]
        [InlineData("not ")]
        [InlineData("not Re")]
        public async Task TestPatterns_Is_PropertyPattern(string partialWritten)
        {
            var markup = @$"
public enum Color
{{
    Red,
    Green,
}}

class C
{{
    public Color Color {{ get; }}

    public void M()
    {{
        var isRed = this is {{ Color: {partialWritten}$$
    }}
}}
";
            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestPatterns_Is_PropertyPattern_NotAfterEnumDot()
        {
            var markup = @$"
public enum Color
{{
    Red,
    Green,
}}

class C
{{
    public Color Color {{ get; }}

    public void M()
    {{
        var isRed = this is {{ Color: Color.R$$
    }}
}}
";
            await VerifyItemIsAbsentAsync(markup, "Color.Red");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestPatterns_SwitchStatement_PropertyPattern()
        {
            var markup = @"
public enum Color
{
    Red,
    Green,
}

class C
{
    public Color Color { get; }

    public void M()
    {
        switch (this)
        {
            case { Color: $$
    }
}
";
            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestPatterns_SwitchExpression_PropertyPattern()
        {
            var markup = @"
public enum Color
{
    Red,
    Green,
}

class C
{
    public Color Color { get; }

    public void M()
    {
        var isRed = this switch
        {
            { Color: $$
    }
}
";
            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestStaticAndInstanceMembers()
        {
            var markup = @"
public readonly struct Color
{
    public static readonly Color Red;
    public readonly Color Green;
}

class C
{
    public void M(Color color)
    {
        M($$
    }
}
";

            await VerifyItemExistsAsync(markup, "Color.Red");
            await VerifyItemIsAbsentAsync(markup, "Color.Green");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestProperties()
        {
            var markup = @"
public readonly struct Color
{
    public static Color Red { get; }
    public Color Green { get; }
}

class C
{
    public void M(Color color)
    {
        M($$
    }
}
";

            await VerifyItemExistsAsync(markup, "Color.Red");
            await VerifyItemIsAbsentAsync(markup, "Color.Green");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("protected")]
        [InlineData("private protected")]
        [InlineData("private")]
        public async Task TestAccessibilityDifferentType(string modifier)
        {
            var markup = $@"
public class Color
{{
    {modifier} static readonly Color Red;
}}

class C
{{
    public void M(Color color)
    {{
        M($$
    }}
}}
";

            var expected = modifier switch
            {
                "public" => true,
                "internal" => true,
                "protected internal" => true,
                _ => false,
            };

            if (expected)
                await VerifyItemExistsAsync(markup, "Color.Red");
            else
                await VerifyItemIsAbsentAsync(markup, "Color.Red");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        [InlineData("protected")]
        [InlineData("private protected")]
        [InlineData("private")]
        public async Task TestAccessibilitySameType(string modifier)
        {
            var markup = $@"
public class Color
{{
    {modifier} static readonly Color Red;

    public void M(Color color)
    {{
        M($$
    }}
}}
";

            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        public async Task TestEnumLikeTypeKinds(string typeKeyword)
        {
            var markup = $@"
public {typeKeyword} Color
{{
    public static readonly Color Red;
}}

class C
{{
    public void M(Color color)
    {{
        M($$
    }}
}}
";

            await VerifyItemExistsAsync(markup, "Color.Red");
        }

        #endregion
    }
}
