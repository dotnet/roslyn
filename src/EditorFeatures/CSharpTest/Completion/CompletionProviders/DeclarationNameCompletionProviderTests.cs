// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{
    public class DeclarationNameCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public DeclarationNameCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new DeclarationNameCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NameWithOnlyType1()
        {
            var markup = @"
public class MyClass
{
    MyClass $$
}
";
            await VerifyItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.MethodPublic);
            await VerifyItemExistsAsync(markup, "myClass", glyph: (int)Glyph.FieldPublic);
            await VerifyItemExistsAsync(markup, "GetMyClass", glyph: (int)Glyph.MethodPublic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AsyncTaskOfT()
        {
            var markup = @"
using System.Threading.Tasks;
public class C
{
    async Task<C> $$
}
";
            await VerifyItemExistsAsync(markup, "GetCAsync");
        }

        [Fact(Skip = "not yet implemented"), Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NonAsyncTaskOfT()
        {
            var markup = @"
public class C
{
    Task<C> $$
}
";
            await VerifyItemExistsAsync(markup, "GetCAsync");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void MethodDeclaration1()
        {
            var markup = @"
public class C
{
    virtual C $$
}
";
            await VerifyItemExistsAsync(markup, "GetC");
            await VerifyItemIsAbsentAsync(markup, "C");
            await VerifyItemIsAbsentAsync(markup, "c");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void WordBreaking1()
        {
            var markup = @"
using System.Threading;
public class C
{
    CancellationToken $$
}
";
            await VerifyItemExistsAsync(markup, "cancellationToken");
            await VerifyItemExistsAsync(markup, "cancellation");
            await VerifyItemExistsAsync(markup, "token");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void WordBreaking2()
        {
            var markup = @"
interface I {}
public class C
{
    I $$
}
";
            await VerifyItemExistsAsync(markup, "GetI");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void WordBreaking3()
        {
            var markup = @"
interface II {}
public class C
{
    II $$
}
";
            await VerifyItemExistsAsync(markup, "GetI");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void WordBreaking4()
        {
            var markup = @"
interface IGoo {}
public class C
{
    IGoo $$
}
";
            await VerifyItemExistsAsync(markup, "Goo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void WordBreaking5()
        {
            var markup = @"
class SomeWonderfullyLongClassName {}
public class C
{
    SomeWonderfullyLongClassName $$
}
";
            await VerifyItemExistsAsync(markup, "Some");
            await VerifyItemExistsAsync(markup, "SomeWonderfully");
            await VerifyItemExistsAsync(markup, "SomeWonderfullyLong");
            await VerifyItemExistsAsync(markup, "SomeWonderfullyLongClass");
            await VerifyItemExistsAsync(markup, "Name");
            await VerifyItemExistsAsync(markup, "ClassName");
            await VerifyItemExistsAsync(markup, "LongClassName");
            await VerifyItemExistsAsync(markup, "WonderfullyLongClassName");
            await VerifyItemExistsAsync(markup, "SomeWonderfullyLongClassName");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void Parameter1()
        {
            var markup = @"
using System.Threading;
public class C
{
    void Goo(CancellationToken $$
}
";
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void Parameter2()
        {
            var markup = @"
using System.Threading;
public class C
{
    void Goo(int x, CancellationToken c$$
}
";
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void Parameter3()
        {
            var markup = @"
using System.Threading;
public class C
{
    void Goo(CancellationToken c$$) {}
}
";
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [WorkItem(19260, "https://github.com/dotnet/roslyn/issues/19260")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void EscapeKeywords1()
        {
            var markup = @"
using System.Text;
public class C
{
    void Goo(StringBuilder $$) {}
}
";
            await VerifyItemExistsAsync(markup, "stringBuilder", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "@string", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "builder", glyph: (int)Glyph.Parameter);
        }

        [WorkItem(19260, "https://github.com/dotnet/roslyn/issues/19260")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void EscapeKeywords2()
        {
            var markup = @"
class For { }
public class C
{
    void Goo(For $$) {}
}
";
            await VerifyItemExistsAsync(markup, "@for", glyph: (int)Glyph.Parameter);
        }

        [WorkItem(19260, "https://github.com/dotnet/roslyn/issues/19260")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void EscapeKeywords3()
        {
            var markup = @"
class For { }
public class C
{
    void goo()
    {
        For $$
    }
}
";
            await VerifyItemExistsAsync(markup, "@for");
        }

        [WorkItem(19260, "https://github.com/dotnet/roslyn/issues/19260")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void EscapeKeywords4()
        {
            var markup = @"
using System.Text;
public class C
{
    void goo()
    {
        StringBuilder $$
    }
}
";
            await VerifyItemExistsAsync(markup, "stringBuilder");
            await VerifyItemExistsAsync(markup, "@string");
            await VerifyItemExistsAsync(markup, "builder");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForInt()
        {
            var markup = @"
using System.Threading;
public class C
{
    int $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForLong()
        {
            var markup = @"
using System.Threading;
public class C
{
    long $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForDouble()
        {
            var markup = @"
using System.Threading;
public class C
{
    double $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForFloat()
        {
            var markup = @"
using System.Threading;
public class C
{
    float $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForSbyte()
        {
            var markup = @"
using System.Threading;
public class C
{
    sbyte $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForShort()
        {
            var markup = @"
using System.Threading;
public class C
{
    short $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForUint()
        {
            var markup = @"
using System.Threading;
public class C
{
    uint $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForUlong()
        {
            var markup = @"
using System.Threading;
public class C
{
    ulong $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForUShort()
        {
            var markup = @"
using System.Threading;
public class C
{
    ushort $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForBool()
        {
            var markup = @"
using System.Threading;
public class C
{
    bool $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForByte()
        {
            var markup = @"
using System.Threading;
public class C
{
    byte $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForChar()
        {
            var markup = @"
using System.Threading;
public class C
{
    char $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSuggestionsForString()
        {
            var markup = @"
public class C
{
    string $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NoSingleLetterClassNameSuggested()
        {
            var markup = @"
public class C
{
    C $$
}
";
            await VerifyItemIsAbsentAsync(markup, "C");
            await VerifyItemIsAbsentAsync(markup, "c");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ArrayElementTypeSuggested()
        {
            var markup = @"
using System.Threading;
public class MyClass
{
    MyClass[] $$
}
";
            await VerifyItemExistsAsync(markup, "MyClass");
            await VerifyItemIsAbsentAsync(markup, "Array");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NotTriggeredByVar()
        {
            var markup = @"
public class C
{
    var $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NotAfterVoid()
        {
            var markup = @"
public class C
{
    void $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AfterGeneric()
        {
            var markup = @"
public class C
{
    System.Collections.Generic.IEnumerable<C> $$
}
";
            await VerifyItemExistsAsync(markup, "GetC");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NothingAfterVar()
        {
            var markup = @"
public class C
{
    void goo()
    {
        var $$
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void TestDescription()
        {
            var markup = @"
public class MyClass
{
    MyClass $$
}
";
            await VerifyItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.MethodPublic, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [WorkItem(20273, "https://github.com/dotnet/roslyn/issues/20273")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void Alias1()
        {
            var markup = @"
using MyType = System.String;
public class C
{
    MyType $$
}
";
            await VerifyItemExistsAsync(markup, "my");
            await VerifyItemExistsAsync(markup, "type");
            await VerifyItemExistsAsync(markup, "myType");
        }
        [WorkItem(20273, "https://github.com/dotnet/roslyn/issues/20273")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AliasWithInterfacePattern()
        {
            var markup = @"
using IMyType = System.String;
public class C
{
    MyType $$
}
";
            await VerifyItemExistsAsync(markup, "my");
            await VerifyItemExistsAsync(markup, "type");
            await VerifyItemExistsAsync(markup, "myType");
        }

        [WorkItem(20016, "https://github.com/dotnet/roslyn/issues/20016")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NotAfterExistingName1()
        {
            var markup = @"
using IMyType = System.String;
public class C
{
    MyType myType $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(20016, "https://github.com/dotnet/roslyn/issues/20016")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NotAfterExistingName2()
        {
            var markup = @"
using IMyType = System.String;
public class C
{
    MyType myType, MyType $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(19409, "https://github.com/dotnet/roslyn/issues/19409")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void OutVarArgument()
        {
            var markup = @"
class Test
{
    void Do(out Test goo)
    {
        Do(out var $$
    }
}
";
            await VerifyItemExistsAsync(markup, "test");
        }

        [WorkItem(19409, "https://github.com/dotnet/roslyn/issues/19409")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void OutArgument()
        {
            var markup = @"
class Test
{
    void Do(out Test goo)
    {
        Do(out Test $$
    }
}
";
            await VerifyItemExistsAsync(markup, "test");
        }

        [WorkItem(19409, "https://github.com/dotnet/roslyn/issues/19409")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void OutGenericArgument()
        {
            var markup = @"
class Test
{
    void Do<T>(out T goo)
    {
        Do(out Test $$
    }
}
";
            await VerifyItemExistsAsync(markup, "test");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void DisabledByOption()
        {
            var workspace = WorkspaceFixture.GetWorkspace();
            var originalOptions = WorkspaceFixture.GetWorkspace().Options;
            try
            {
                workspace.Options = originalOptions.
                    WithChangedOption(CompletionOptions.ShowNameSuggestions, LanguageNames.CSharp, false);

                var markup = @"
class Test
{
    Test $$
}
";
                await VerifyNoItemsExistAsync(markup);
            }
            finally
            {
                workspace.Options = originalOptions;
            }

        }
    }
}
