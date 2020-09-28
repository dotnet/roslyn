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

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("bool")]
        [InlineData("System.Boolean")]
        [InlineData("char")]
        [InlineData("System.Char")]
        [InlineData("string")]
        [InlineData("System.String")]
        [InlineData("sbyte")]
        [InlineData("System.SByte")]
        [InlineData("byte")]
        [InlineData("System.Byte")]
        [InlineData("short")]
        [InlineData("System.Int16")]
        [InlineData("ushort")]
        [InlineData("System.UInt16")]
        [InlineData("int")]
        [InlineData("System.Int32")]
        [InlineData("uint")]
        [InlineData("System.UInt32")]
        [InlineData("long")]
        [InlineData("System.Int64")]
        [InlineData("ulong")]
        [InlineData("System.UInt64")]
        [InlineData("float")]
        [InlineData("System.Single")]
        [InlineData("double")]
        [InlineData("System.Double")]
        [InlineData("decimal")]
        [InlineData("System.Decimal")]
        [InlineData("System.IntPtr")]
        [InlineData("System.UIntPtr")]
        [InlineData("System.DateTime")]
        [InlineData("System.TimeSpan")]
        [InlineData("System.DateTimeOffset")]
        [InlineData("System.Guid")]
        // TODO: Add Span, System.Numeric and System.Data.SqlTypes to the test workspace. These types are currently "ErrorTypes" in the semanticModel.
        [InlineData("System.ReadOnlySpan<int>")]
        [InlineData("System.Span<int>")]
        [InlineData("System.Numerics.BigInteger")]
        [InlineData("System.Numerics.Complex")]
        [InlineData("System.Numerics.Matrix3x2")]
        [InlineData("System.Numerics.Matrix4x4")]
        [InlineData("System.Numerics.Plane")]
        [InlineData("System.Numerics.Quaternion")]
        [InlineData("System.Numerics.Vector<int>")]
        [InlineData("System.Numerics.Vector2")]
        [InlineData("System.Numerics.Vector3")]
        [InlineData("System.Numerics.Vector4")]
        [InlineData("System.Data.SqlTypes.SqlBinary")]
        [InlineData("System.Data.SqlTypes.SqlBoolean")]
        [InlineData("System.Data.SqlTypes.SqlByte")]
        [InlineData("System.Data.SqlTypes.SqlDateTime")]
        [InlineData("System.Data.SqlTypes.SqlDecimal")]
        [InlineData("System.Data.SqlTypes.SqlDouble")]
        [InlineData("System.Data.SqlTypes.SqlGuid")]
        [InlineData("System.Data.SqlTypes.SqlInt16")]
        [InlineData("System.Data.SqlTypes.SqlInt32")]
        [InlineData("System.Data.SqlTypes.SqlInt64")]
        [InlineData("System.Data.SqlTypes.SqlMoney")]
        [InlineData("System.Data.SqlTypes.SqlSingle")]
        [InlineData("System.Data.SqlTypes.SqlString")]
        public async Task OperatorNoSuggestionForSpecialTypes(string specialType)
        {
            await VerifyNoItemsExistAsync(@$"
public class Program
{{
    public void Main()
    {{
        {specialType} i = default({specialType});
        i.$$
    }}
}}
");
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
                    Assert.EndsWith("+op_Addition", i.SortText);
                },
                i =>
                {
                    Assert.Equal("+", i.DisplayText);
                    Assert.EndsWith("+op_UnaryPlus", i.SortText);
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
