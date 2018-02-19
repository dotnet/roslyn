using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Immutable;
using static Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationNameCompletionProvider;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DeclarationInfoTests
{
    public class DeclarationNameCompletion_ContextTests
    {
        protected CSharpTestWorkspaceFixture fixture = new CSharpTestWorkspaceFixture();

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AfterTypeInClass1()
        {
            var markup = @"
class C
{
    int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Field, SymbolKind.Method, SymbolKind.Property);
            await VerifyNoModifiers(markup);
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AfterTypeInClassWithAccessibility()
        {
            var markup = @"
class C
{
    public int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Field, SymbolKind.Method, SymbolKind.Property);
            await VerifyNoModifiers(markup);
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.Public);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AfterTypeInClassVirtual()
        {
            var markup = @"
class C
{
    public virtual int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Method, SymbolKind.Property);
            await VerifyModifiers(markup, new DeclarationModifiers(isVirtual: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.Public);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AfterTypeInClassStatic()
        {
            var markup = @"
class C
{
    private static int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Field, SymbolKind.Method, SymbolKind.Property);
            await VerifyModifiers(markup, new DeclarationModifiers(isStatic: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.Private);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void AfterTypeInClassConst()
        {
            var markup = @"
class C
{
    private const int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Field);
            await VerifyModifiers(markup, new DeclarationModifiers(isConst: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.Private);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void VariableDeclaration1()
        {
            var markup = @"
class C
{
    void goo()
    {
        int $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Local);
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void VariableDeclaration2()
        {
            var markup = @"
class C
{
    void goo()
    {
        int c1, $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Local);
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ReadonlyVariableDeclaration1()
        {
            var markup = @"
class C
{
    void goo()
    {
        readonly int $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Local);
            await VerifyModifiers(markup, new DeclarationModifiers(isReadOnly: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ReadonlyVariableDeclaration2()
        {
            var markup = @"
class C
{
    void goo()
    {
        readonly int c1, $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Local);
            await VerifyModifiers(markup, new DeclarationModifiers(isReadOnly: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void Parameter1()
        {
            var markup = @"
class C
{
    void goo(C $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Parameter);
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "global::C");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void Parameter2()
        {
            var markup = @"
class C
{
    void goo(C c1, C $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Parameter);
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "global::C");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ParameterAfterPredefinedType1()
        {
            var markup = @"
class C
{
    void goo(string $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Parameter);
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "string");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ParameterAfterPredefinedType2()
        {
            var markup = @"
class C
{
    void goo(C c1, string $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Parameter);
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "string");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ParameterAfterGeneric()
        {
            var markup = @"
using System.Collections.Generic;
class C
{
    void goo(C c1, List<string> $$
    }
}
";
            await VerifySymbolKinds(markup, SymbolKind.Parameter);
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "global::System.Collections.Generic.List<string>");
            await VerifyAccessibility(markup, Accessibility.NotApplicable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ClassTypeParameter1()
        {
            var markup = @"
class C<$$
{
}
";
            await VerifySymbolKinds(markup, SymbolKind.TypeParameter);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ClassTypeParameter2()
        {
            var markup = @"
class C<T1, $$
{
}
";
            await VerifySymbolKinds(markup, SymbolKind.TypeParameter);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ModifierExclusion1()
        {
            var markup = @"
class C
{
    readonly int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Field);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ModifierExclusion2()
        {
            var markup = @"
class C
{
    const int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Field);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ModifierExclusion3()
        {
            var markup = @"
class C
{
    abstract int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Method, SymbolKind.Property);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ModifierExclusion4()
        {
            var markup = @"
class C
{
    virtual int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Method, SymbolKind.Property);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ModifierExclusion5()
        {
            var markup = @"
class C
{
    sealed int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Method, SymbolKind.Property);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ModifierExclusion6()
        {
            var markup = @"
class C
{
    override int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Method, SymbolKind.Property);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ModifierExclusion7()
        {
            var markup = @"
class C
{
    async int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Method);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void ModifierExclusion8()
        {
            // Note that the async is not included in the incomplete member syntax
            var markup = @"
class C
{
    partial int $$
}
";
            await VerifySymbolKinds(markup, SymbolKind.Field, SymbolKind.Method, SymbolKind.Property);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void LocalInsideMethod1()
        {
            var markup = @"
namespace ConsoleApp1
{
    class ReallyLongClassName { }
    class Program
    {
        static void Main(string[] args)
        {
            ReallyLongClassName $$
        }
    }
";
            await VerifySymbolKinds(markup, SymbolKind.Local);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void LocalInsideMethod2()
        {
            var markup = @"
namespace ConsoleApp1
{
    class ReallyLongClassName<T> { }
    class Program
    {
        static void Main(string[] args)
        {
            ReallyLongClassName<int> $$
        }
    }
";
            await VerifySymbolKinds(markup, SymbolKind.Local);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void LocalInsideMethodAfterPredefinedTypeKeyword()
        {
            var markup = @"
namespace ConsoleApp1
{
    class ReallyLongClassName { }
    class Program
    {
        static void Main(string[] args)
        {
            string $$
        }
    }
";
            await VerifySymbolKinds(markup, SymbolKind.Local);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async void LocalInsideMethodAfterArray()
        {
            var markup = @"
namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] $$
        }
    }
";
            await VerifySymbolKinds(markup, SymbolKind.Local);
        }

        private async Task VerifyNoType(string markup)
        {
            var result = await GetResultsAsync(markup);
            Assert.Null(result.Type);
        }

        private async Task VerifyTypeName(string markup, string typeName)
        {
            var result = await GetResultsAsync(markup);
            Assert.Equal(typeName, result.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        private async Task VerifyNoModifiers(string markup)
        {
            var result = await GetResultsAsync(markup);
            Assert.Equal(default(DeclarationModifiers), result.Modifiers);
        }

        private async Task VerifySymbolKinds(string markup, params SymbolKind[] expectedSymbolKinds)
        {
            var result = await GetResultsAsync(markup);
            Assert.True(expectedSymbolKinds.SequenceEqual(result.PossibleSymbolKinds));
        }

        private async Task VerifyModifiers(string markup, DeclarationModifiers modifiers)
        {
            var result = await GetResultsAsync(markup);
            Assert.Equal(modifiers, result.Modifiers);
        }

        private async Task VerifyAccessibility(string markup, Accessibility accessibility)
        {
            var result = await GetResultsAsync(markup);
            Assert.Equal(accessibility, result.DeclaredAccessibility);
        }

        private async Task<NameDeclarationInfo> GetResultsAsync(string markup)
        {
            var (document, position) = ApplyChangesToFixture(markup);
            var result = await NameDeclarationInfo.GetDeclarationInfo(document, position, CancellationToken.None);
            return result;
        }

        private (Document, int) ApplyChangesToFixture(string markup)
        {
            MarkupTestFile.GetPosition(markup, out var text, out int position);
            return (fixture.UpdateDocument(text, SourceCodeKind.Regular), position);
        }
    }
}
