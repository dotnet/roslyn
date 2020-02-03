using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationNameCompletionProvider;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DeclarationInfoTests
{
    [UseExportProvider]
    public class DeclarationNameCompletion_ContextTests
    {
        protected CSharpTestWorkspaceFixture fixture = new CSharpTestWorkspaceFixture();

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterTypeInClass1()
        {
            var markup = @"
class C
{
    int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Field),
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
            await VerifyNoModifiers(markup);
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterTypeInClassWithAccessibility()
        {
            var markup = @"
class C
{
    public int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Field),
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
            await VerifyNoModifiers(markup);
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.Public);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterTypeInClassVirtual()
        {
            var markup = @"
class C
{
    public virtual int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
            await VerifyModifiers(markup, new DeclarationModifiers(isVirtual: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.Public);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterTypeInClassStatic()
        {
            var markup = @"
class C
{
    private static int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Field),
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
            await VerifyModifiers(markup, new DeclarationModifiers(isStatic: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.Private);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterTypeInClassConst()
        {
            var markup = @"
class C
{
    private const int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Field));
            await VerifyModifiers(markup, new DeclarationModifiers(isConst: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, Accessibility.Private);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VariableDeclaration1()
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
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local),
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VariableDeclaration2()
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
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadonlyVariableDeclaration1()
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
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local),
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
            await VerifyModifiers(markup, new DeclarationModifiers(isReadOnly: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadonlyVariableDeclaration2()
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
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
            await VerifyModifiers(markup, new DeclarationModifiers(isReadOnly: true));
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingVariableDeclaration1()
        {
            var markup = @"
class C
{
    void M()
    {
        using (int i$$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingVariableDeclaration2()
        {
            var markup = @"
class C
{
    void M()
    {
        using (int i1, $$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForVariableDeclaration1()
        {
            var markup = @"
class C
{
    void M()
    {
        for (int i$$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForVariableDeclaration2()
        {
            var markup = @"
class C
{
    void M()
    {
        for (int i1, $$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForEachVariableDeclaration()
        {
            var markup = @"
class C
{
    void M()
    {
        foreach (int $$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "int");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Parameter1()
        {
            var markup = @"
class C
{
    void goo(C $$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Parameter));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "global::C");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Parameter2()
        {
            var markup = @"
class C
{
    void goo(C c1, C $$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Parameter));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "global::C");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParameterAfterPredefinedType1()
        {
            var markup = @"
class C
{
    void goo(string $$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Parameter));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "string");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParameterAfterPredefinedType2()
        {
            var markup = @"
class C
{
    void goo(C c1, string $$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Parameter));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "string");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParameterAfterGeneric()
        {
            var markup = @"
using System.Collections.Generic;
class C
{
    void goo(C c1, List<string> $$
    }
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Parameter));
            await VerifyModifiers(markup, new DeclarationModifiers());
            await VerifyTypeName(markup, "global::System.Collections.Generic.List<string>");
            await VerifyAccessibility(markup, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ClassTypeParameter1()
        {
            var markup = @"
class C<$$
{
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.TypeParameter));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ClassTypeParameter2()
        {
            var markup = @"
class C<T1, $$
{
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.TypeParameter));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ModifierExclusion1()
        {
            var markup = @"
class C
{
    readonly int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Field));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ModifierExclusion2()
        {
            var markup = @"
class C
{
    const int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Field));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ModifierExclusion3()
        {
            var markup = @"
class C
{
    abstract int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ModifierExclusion4()
        {
            var markup = @"
class C
{
    virtual int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ModifierExclusion5()
        {
            var markup = @"
class C
{
    sealed int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ModifierExclusion6()
        {
            var markup = @"
class C
{
    override int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ModifierExclusion7()
        {
            var markup = @"
class C
{
    async int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ModifierExclusion8()
        {
            // Note that the async is not included in the incomplete member syntax
            var markup = @"
class C
{
    partial int $$
}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Field),
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_Const(string type)
        {
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        const {type} $$
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_ConstLocalDeclaration(string type)
        {
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        const {type} v$$ = default;
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_ConstLocalFunction(string type)
        {
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        const {type} v$$()
        {{
        }}
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_Async(string type)
        {
            // This only works with a partially written name.
            // Because async is not a keyword, the syntax tree when the name is missing is completely broken
            // in that there can be multiple statements full of missing and skipped tokens depending on the type syntax.
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        async {type} v$$
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_AsyncLocalDeclaration(string type)
        {
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        async {type} v$$ = default;
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_AsyncLocalFunction(string type)
        {
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        async {type} v$$()
        {{
        }}
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_Unsafe(string type)
        {
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        unsafe {type} $$
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_UnsafeLocalDeclaration(string type)
        {
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        unsafe {type} v$$ = default;
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("int")]
        [InlineData("C")]
        [InlineData("List<string>")]
        public async Task ModifierExclusionInsideMethod_UnsafeLocalFunction(string type)
        {
            var markup = $@"
using System.Collections.Generic;
class C
{{
    void M()
    {{
        unsafe {type} v$$()
        {{
        }}
    }}
}}
";
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalInsideMethod1()
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
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local),
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalInsideMethod2()
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
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local),
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalInsideMethodAfterPredefinedTypeKeyword()
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
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local),
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalInsideMethodAfterArray()
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
            await VerifySymbolKinds(markup,
                new SymbolKindOrTypeKind(SymbolKind.Local),
                new SymbolKindOrTypeKind(MethodKind.LocalFunction));
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
            Assert.Equal(default, result.Modifiers);
        }

        private async Task VerifySymbolKinds(string markup, params SymbolKindOrTypeKind[] expectedSymbolKinds)
        {
            var result = await GetResultsAsync(markup);
            Assert.True(expectedSymbolKinds.SequenceEqual(result.PossibleSymbolKinds));
        }

        private async Task VerifyModifiers(string markup, DeclarationModifiers modifiers)
        {
            var result = await GetResultsAsync(markup);
            Assert.Equal(modifiers, result.Modifiers);
        }

        private async Task VerifyAccessibility(string markup, Accessibility? accessibility)
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
