// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class OperatorCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public OperatorCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override Type GetCompletionProviderType()
            => typeof(OperatorCompletionProvider);

        // The suggestion is e.g. "+". If the user actually types "+" the completion list is closed. Operators therefore do not support partially written items.
        protected override string? ItemPartiallyWritten(string? expectedItemOrNull) => "";

        private static IEnumerable<string[]> BinaryOperators()
        {
            yield return new[] { "+" };
            yield return new[] { "&" };
            yield return new[] { "|" };
            yield return new[] { "/" };
            yield return new[] { "==" };
            yield return new[] { "^" };
            yield return new[] { ">" };
            yield return new[] { ">=" };
            yield return new[] { "!=" };
            yield return new[] { "<<" };
            yield return new[] { "<" };
            yield return new[] { "<=" };
            yield return new[] { "%" };
            yield return new[] { "*" };
            yield return new[] { ">>" };
            yield return new[] { "-" };
        }

        private static IEnumerable<string[]> PostfixOperators()
        {
            yield return new[] { "++" };
            yield return new[] { "--" };
        }

        private static IEnumerable<string[]> PrefixOperators()
        {
            yield return new[] { "!" };
            yield return new[] { "~" };
            yield return new[] { "-" };
            yield return new[] { "+" };
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync(@"
public class C
{
    public static C operator +(C _, C _) => default;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$;
    }
}
", "+");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorsAreSortedByImporttanceAndGroupedByTopic()
        {
            var items = await GetCompletionItemsAsync(@"
public class C
{
    public static C operator +(C a, C b) => null;
    public static C operator -(C a, C b) => null;
    public static C operator *(C a, C b) => null;
    public static C operator /(C a, C b) => null;
    public static C operator %(C a, C b) => null;
    public static bool operator ==(C a, C b) => true;
    public static bool operator !=(C a, C b) => false;
    public static bool operator <(C a, C b) => true;
    public static bool operator >(C a, C b) => false;
    public static bool operator <=(C a, C b) => true;
    public static bool operator >=(C a, C b) => false;
    public static C operator +(C a) => null;
    public static C operator -(C a) => null;
    public static C operator ++(C a) => null;
    public static C operator --(C a) => null;
    public static bool operator true(C w) => true;
    public static bool operator false(C w) => false;
    public static bool operator &(C a, C b) => true;
    public static bool operator |(C a, C b) => true;
    public static C operator !(C a) => null;
    public static C operator ^(C a, C b) => null;
    public static C operator <<(C a, int b) => null;
    public static C operator >>(C a, int b) => null;
    public static C operator ~(C a) => null;

}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$;
    }
}
", SourceCodeKind.Regular);
            // true and false operators are not listed
            Assert.Collection(items,
                i => Assert.Equal("==", i.DisplayText),
                i => Assert.Equal("!=", i.DisplayText),
                i => Assert.Equal(">", i.DisplayText),
                i => Assert.Equal(">=", i.DisplayText),
                i => Assert.Equal("<", i.DisplayText),
                i => Assert.Equal("<=", i.DisplayText),
                i => Assert.Equal("!", i.DisplayText),
                i => Assert.Equal("+", i.DisplayText), // Addition a+b
                i => Assert.Equal("-", i.DisplayText), // Subtraction a-b
                i => Assert.Equal("*", i.DisplayText),
                i => Assert.Equal("/", i.DisplayText),
                i => Assert.Equal("%", i.DisplayText),
                i => Assert.Equal("++", i.DisplayText),
                i => Assert.Equal("--", i.DisplayText),
                i => Assert.Equal("+", i.DisplayText), // Unary plus +a
                i => Assert.Equal("-", i.DisplayText), // Unary minus -a
                i => Assert.Equal("&", i.DisplayText),
                i => Assert.Equal("|", i.DisplayText),
                i => Assert.Equal("^", i.DisplayText),
                i => Assert.Equal("<<", i.DisplayText),
                i => Assert.Equal(">>", i.DisplayText),
                i => Assert.Equal("~", i.DisplayText)
            );
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("bool", 0)]
        [InlineData("System.Boolean", 0)]
        [InlineData("char", 0)]
        [InlineData("System.Char", 0)]
        [InlineData("string", 2)] // Invalid: misses concatenation (2 = equality operators)
        [InlineData("System.String", 2)] // Invalid: misses concatenation (2 = equality operators)
        [InlineData("sbyte", 0)]
        [InlineData("System.SByte", 0)]
        [InlineData("byte", 0)]
        [InlineData("System.Byte", 0)]
        [InlineData("short", 0)]
        [InlineData("System.Int16", 0)]
        [InlineData("ushort", 0)]
        [InlineData("System.UInt16", 0)]
        [InlineData("int", 0)]
        [InlineData("System.Int32", 0)]
        [InlineData("uint", 0)]
        [InlineData("System.UInt32", 0)]
        [InlineData("long", 0)]
        [InlineData("System.Int64", 0)]
        [InlineData("ulong", 0)]
        [InlineData("System.UInt64", 0)]
        [InlineData("float", 6)] // Invalid: misses arithmetical operators (6 = comparison operators)
        [InlineData("System.Single", 6)] // Invalid: misses arithmetical operators (6 = comparison operators)
        [InlineData("double", 6)] // Invalid: misses arithmetical operators (6 = comparison operators)
        [InlineData("System.Double", 6)] // Invalid: misses arithmetical operators (6 = comparison operators)
        [InlineData("decimal", 15)]
        [InlineData("System.Decimal", 15)]
        [InlineData("System.IntPtr", 4)]
        [InlineData("System.UIntPtr", 4)]
        [InlineData("System.DateTime", 8)]
        [InlineData("System.TimeSpan", 10)]
        [InlineData("System.DateTimeOffset", 8)]
        [InlineData("System.Guid", 2)]
        // TODO: Add Span, System.Numeric and System.Data.SqlTypes to the test workspace. These types are currently "ErrorTypes" in the semanticModel.
        [InlineData("System.ReadOnlySpan<int>", 0)]
        [InlineData("System.Span<int>", 0)]
        [InlineData("System.Numerics.BigInteger", 0)]
        [InlineData("System.Numerics.Complex", 0)]
        [InlineData("System.Numerics.Matrix3x2", 0)]
        [InlineData("System.Numerics.Matrix4x4", 0)]
        [InlineData("System.Numerics.Plane", 0)]
        [InlineData("System.Numerics.Quaternion", 0)]
        [InlineData("System.Numerics.Vector<int>", 0)]
        [InlineData("System.Numerics.Vector2", 0)]
        [InlineData("System.Numerics.Vector3", 0)]
        [InlineData("System.Numerics.Vector4", 0)]
        [InlineData("System.Data.SqlTypes.SqlBinary", 0)]
        [InlineData("System.Data.SqlTypes.SqlBoolean", 0)]
        [InlineData("System.Data.SqlTypes.SqlByte", 0)]
        [InlineData("System.Data.SqlTypes.SqlDateTime", 0)]
        [InlineData("System.Data.SqlTypes.SqlDecimal", 0)]
        [InlineData("System.Data.SqlTypes.SqlDouble", 0)]
        [InlineData("System.Data.SqlTypes.SqlGuid", 0)]
        [InlineData("System.Data.SqlTypes.SqlInt16", 0)]
        [InlineData("System.Data.SqlTypes.SqlInt32", 0)]
        [InlineData("System.Data.SqlTypes.SqlInt64", 0)]
        [InlineData("System.Data.SqlTypes.SqlMoney", 0)]
        [InlineData("System.Data.SqlTypes.SqlSingle", 0)]
        [InlineData("System.Data.SqlTypes.SqlString", 0)]
        public async Task OperatorSuggestionForSpecialTypes(string specialType, int numberOfSuggestions)
        {
            var completionItems = await GetCompletionItemsAsync(@$"
public class Program
{{
    public void Main()
    {{
        {specialType} i = default({specialType});
        i.$$
    }}
}}
", SourceCodeKind.Regular);
            Assert.Equal(numberOfSuggestions, completionItems.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorNoSuggestionForTrueAndFalse()
        {
            await VerifyNoItemsExistAsync(@"
public class C
{
    public static bool operator true(C _) => true;
    public static bool operator false(C _) => true;
}

public class Program
{
    public void Main()
    {
        var c = new C();
        c.$$
    }
}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(BinaryOperators))]
        public async Task OperatorBinaryIsCompleted(string binaryOperator)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator {binaryOperator}(C _, C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", binaryOperator, @$"
public class C
{{
    public static C operator {binaryOperator}(C _, C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        c {binaryOperator} $$
    }}
}}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(PostfixOperators))]
        public async Task OperatorPostfixIsCompleted(string postfixOperator)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator {postfixOperator}(C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", postfixOperator, @$"
public class C
{{
    public static C operator {postfixOperator}(C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        c{postfixOperator} $$
    }}
}}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(PrefixOperators))]
        public async Task OperatorPrefixIsCompleted(string prefixOperator)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator {prefixOperator}(C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", prefixOperator, @$"
public class C
{{
    public static C operator {prefixOperator}(C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        {prefixOperator}c.$$
    }}
}}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(PrefixOperators))]
        public async Task OperatorDuplicateOperatorsAreListedBoth()
        {
            var items = await GetCompletionItemsAsync($@"
public class C
{{
    public static C operator +(C _, C_) => default;
    public static C operator +(C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", SourceCodeKind.Regular);
            Assert.Collection(items,
                i =>
                {
                    Assert.Equal("+", i.DisplayText);
                    Assert.EndsWith("003007", i.SortText); // Addition
                },
                i =>
                {
                    Assert.Equal("+", i.DisplayText);
                    Assert.EndsWith("003014", i.SortText); // unary plus
                });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(PrefixOperators))]
        public async Task OperatorDuplicateOperatorsAreCompleted()
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator +(C _, C_) => default;
    public static C operator +(C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", "+", @$"
public class C
{{
    public static C operator +(C _, C_) => default;
    public static C operator +(C _) => default;
}}

public class Program
{{
    public void Main()
    {{
        var c = new C();
        c + $$
    }}
}}
");
        }
    }
}
