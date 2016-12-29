using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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
public class C
{
    C $$
}
";
            await VerifyItemExistsAsync(markup, "C", glyph: (int)Glyph.PropertyPublic);
            await VerifyItemExistsAsync(markup, "c", glyph: (int)Glyph.FieldPublic);
            await VerifyItemExistsAsync(markup, "GetC", glyph: (int)Glyph.MethodPublic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AsyncTask()
        {
            var markup = @"
public class C
{
    async Task $$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AsyncTaskOfT()
        {
            var markup = @"
public class C
{
    async Task<C> $$
}
";
            var workspace = WorkspaceFixture.GetWorkspace();
            var oldOptions = workspace.Options;
            try
            {
                var newOptions = oldOptions.WithChangedOption(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), AsyncMethodsEndWithAsync());
                workspace.Options = newOptions;
                await VerifyItemExistsAsync(markup, "GetCAsync");
            }
            finally
            {
                workspace.Options = oldOptions;
            }
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
            await VerifyItemExistsAsync(markup, "I");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void WordBreaking3()
        {
            var markup = @"
interface II {}
public class C
{
    I $$
}
";
            await VerifyItemExistsAsync(markup, "I");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void WordBreaking4()
        {
            var markup = @"
interface IFoo {}
public class C
{
    IFoo $$
}
";
            await VerifyItemExistsAsync(markup, "Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void WordBreaking5()
        {
            var markup = @"
class AWonderfullyLongClassName {}
public class C
{
    AWonderfullyLongClassName $$
}
";
            await VerifyItemExistsAsync(markup, "A");
            await VerifyItemExistsAsync(markup, "AWonderfully");
            await VerifyItemExistsAsync(markup, "AWonderfullyLong");
            await VerifyItemExistsAsync(markup, "AWonderfullyLongClass");
            await VerifyItemExistsAsync(markup, "Name");
            await VerifyItemExistsAsync(markup, "ClassName");
            await VerifyItemExistsAsync(markup, "LongClassName");
            await VerifyItemExistsAsync(markup, "WonderfullyLongClassName");
            await VerifyItemExistsAsync(markup, "AWonderfullyLongClassName");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void Parameter1()
        {
            var markup = @"
using System.Threading;
public class C
{
    void Foo(CancellationToken $$
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
    void Foo(int x, CancellationToken c$$
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
    void Foo(CancellationToken c$$) {}
}
";
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForInt()
        {
            var markup = @"
using System.Threading;
public class C
{
    int $$
}
";
            await VerifyItemExistsAsync(markup, "v");
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
            await VerifyItemExistsAsync(markup, "v");
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
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForFloat()
        {
            var markup = @"
using System.Threading;
public class C
{
    float $$
}
";
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForSbyte()
        {
            var markup = @"
using System.Threading;
public class C
{
    sbyte $$
}
";
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForShort()
        {
            var markup = @"
using System.Threading;
public class C
{
    short $$
}
";
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForUint()
        {
            var markup = @"
using System.Threading;
public class C
{
    uint $$
}
";
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForUlong()
        {
            var markup = @"
using System.Threading;
public class C
{
    ulong $$
}
";
            await VerifyItemExistsAsync(markup, "v");
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
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForBool()
        {
            var markup = @"
using System.Threading;
public class C
{
    bool $$
}
";
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForByte()
        {
            var markup = @"
using System.Threading;
public class C
{
    byte $$
}
";
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForChar()
        {
            var markup = @"
using System.Threading;
public class C
{
    char $$
}
";
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void SuggestionsForString()
        {
            var markup = @"
public class C
{
    string $$
}
";
            await VerifyItemExistsAsync(markup, "v");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ArrayElementTypeSuggested()
        {
            var markup = @"
using System.Threading;
public class C
{
    C[] $$
}
";
            await VerifyItemExistsAsync(markup, "C");
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
        public async void NptAfterVoid()
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
            await VerifyItemExistsAsync(markup, "C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void NothingAfterVar()
        {
            var markup = @"
public class C
{
    void foo()
    {
        var $$
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        private NamingStylePreferences AsyncMethodsEndWithAsync()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Method)),
                ImmutableArray.Create<Accessibility>(),
                ImmutableArray.Create(new SymbolSpecification.ModifierKind(ModifierKindEnum.IsAsync)));

            var namingStyle = new NamingStyle(Guid.NewGuid(),
                name: "Name",
                prefix: "",
                suffix: "",
                wordSeparator: "",
                capitalizationScheme: Capitalization.PascalCase);
                

            var namingRule = new SerializableNamingRule();
            namingRule.SymbolSpecificationID = symbolSpecification.ID;
            namingRule.NamingStyleID = namingStyle.ID;
            namingRule.EnforcementLevel = DiagnosticSeverity.Error;

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }
    }
}
