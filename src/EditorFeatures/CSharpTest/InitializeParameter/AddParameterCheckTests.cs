﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.CSharp.InitializeParameter.CSharpAddParameterCheckCodeRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InitializeParameter
{
    public class AddParameterCheckTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestEmptyFile()
        {
            var code = @"[||]";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSimpleReferenceType()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSimpleReferenceType_CSharp6()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp6,
                TestCode = @"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}"
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNullable()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C([||]int? i)
    {
    }
}",
@"
using System;

class C
{
    public C(int? i)
    {
        if (i is null)
        {
            throw new ArgumentNullException(nameof(i));
        }
    }
}");
        }

        [WorkItem(47030, "https://github.com/dotnet/roslyn/issues/47030")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnOutParameter()
        {
            var code = @"
class C
{
    public C([||]out string s)
    {
        s = """";
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnValueType()
        {
            var code = @"
using System;

class C
{
    public C([||]int i)
    {
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnInterfaceParameter()
        {
            var code = @"
using System;

interface I
{
    void M([||]string s);
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnNullableParameter()
        {
            var code = @"
#nullable enable

using System;

class C
{
    void M([||]string? s)
    {
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnAbstractParameter()
        {
            var code = @"
using System;

abstract class C
{
    public abstract void M([||]string s);
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnExternParameter()
        {
            var code = @"
using System;

class C
{
    extern void M([||]string s);
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnPartialMethodDefinition1()
        {
            var code = @"
using System;

partial class C
{
    partial void M([||]string s);

    partial void M(string s)
    {
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnExtendedPartialMethodDefinition1()
        {
            var code = @"
using System;

partial class C
{
    public partial void M([||]string s);

    public partial void M(string s)
    {
    }
}";
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnPartialMethodDefinition2()
        {
            var code = @"
using System;

partial class C
{
    partial void M(string s)
    {
    }

    partial void M([||]string s);
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnExtendedPartialMethodDefinition2()
        {
            var code = @"
using System;

partial class C
{
    public partial void M(string s)
    {
    }

    public partial void M([||]string s);
}";
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnPartialMethodImplementation1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

partial class C
{
    partial void M(string s);

    partial void M([||]string s)
    {
    }
}",
@"
using System;

partial class C
{
    partial void M(string s);

    partial void M(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnExtendedPartialMethodImplementation1()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = @"
using System;

partial class C
{
    public partial void M(string s);

    public partial void M([||]string s)
    {
    }
}",
                FixedCode = @"
using System;

partial class C
{
    public partial void M(string s);

    public partial void M(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}"
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnPartialMethodImplementation2()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

partial class C
{
    partial void M([||]string s)
    {
    }

    partial void M(string s);
}",
@"
using System;

partial class C
{
    partial void M(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }

    partial void M(string s);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnExtendedPartialMethodImplementation2()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = @"
using System;

partial class C
{
    public partial void M([||]string s)
    {
    }

    public partial void M(string s);
}",
                FixedCode = @"
using System;

partial class C
{
    public partial void M(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }

    public partial void M(string s);
}"
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExistingFieldAssignment()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    private string _s;

    public C([||]string s)
    {
        _s = s;
    }
}",
@"
using System;

class C
{
    private string _s;

    public C(string s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableParameters()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]string a, string b, string c)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string a, string b, string c)
    {{
        if (string.IsNullOrEmpty(a))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(a)}").Replace("\"", "\\\"")}"", nameof(a));
        }}

        if (string.IsNullOrEmpty(b))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(b)}").Replace("\"", "\\\"")}"", nameof(b));
        }}

        if (string.IsNullOrEmpty(c))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(c)}").Replace("\"", "\\\"")}"", nameof(c));
        }}
    }}
}}",
                CodeActionIndex = 3,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_null_checks_for_all_parameters)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableParametersSomeNullableReferenceTypes()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
#nullable enable

using System;

class C
{
    public C([||]string a, string b, string? c)
    {
    }
}",
                FixedCode = @$"
#nullable enable

using System;

class C
{{
    public C(string a, string b, string? c)
    {{
        if (string.IsNullOrEmpty(a))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(a)}").Replace("\"", "\\\"")}"", nameof(a));
        }}

        if (string.IsNullOrEmpty(b))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(b)}").Replace("\"", "\\\"")}"", nameof(b));
        }}
    }}
}}",
                CodeActionIndex = 3,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_null_checks_for_all_parameters)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCursorNotOnParameters()
        {
            var code = @"
using System;

class C
{
    public C(string a[|,|] string b, string c)
    {
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableWithCursorOnNonNullable()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C(string a, [||]bool b, string c)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string a, bool b, string c)
    {{
        if (string.IsNullOrEmpty(a))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(a)}").Replace("\"", "\\\"")}"", nameof(a));
        }}

        if (string.IsNullOrEmpty(c))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(c)}").Replace("\"", "\\\"")}"", nameof(c));
        }}
    }}
}}",
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_null_checks_for_all_parameters)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableNonNullable()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]string a, bool b, string c)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string a, bool b, string c)
    {{
        if (string.IsNullOrEmpty(a))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(a)}").Replace("\"", "\\\"")}"", nameof(a));
        }}

        if (string.IsNullOrEmpty(c))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(c)}").Replace("\"", "\\\"")}"", nameof(c));
        }}
    }}
}}",
                CodeActionIndex = 3,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_null_checks_for_all_parameters)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableStringsAndObjects()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]string a, object b, string c)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string a, object b, string c)
    {{
        if (string.IsNullOrEmpty(a))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(a)}").Replace("\"", "\\\"")}"", nameof(a));
        }}

        if (b is null)
        {{
            throw new ArgumentNullException(nameof(b));
        }}

        if (string.IsNullOrEmpty(c))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(c)}").Replace("\"", "\\\"")}"", nameof(c));
        }}
    }}
}}",
                CodeActionIndex = 3,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_null_checks_for_all_parameters)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableObjects()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]object a, object b, object c)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(object a, object b, object c)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        if (c is null)
        {
            throw new ArgumentNullException(nameof(c));
        }
    }
}",
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_null_checks_for_all_parameters)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMultiNullableStructs()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]int ? a, bool ? b, double ? c)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(int ? a, bool ? b, double ? c)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        if (c is null)
        {
            throw new ArgumentNullException(nameof(c));
        }
    }
}",
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_null_checks_for_all_parameters)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExistingPropertyAssignment()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    private string S;

    public C([||]string s)
    {
        S = s;
    }
}",
@"
using System;

class C
{
    private string S;

    public C(string s)
    {
        S = s ?? throw new ArgumentNullException(nameof(s));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task DoNotUseThrowExpressionBeforeCSharp7()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp6,
                TestCode = @"
using System;

class C
{
    private string S;

    public C([||]string s)
    {
        S = s;
    }
}",
                FixedCode = @"
using System;

class C
{
    private string S;

    public C(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        S = s;
    }
}"
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task RespectUseThrowExpressionOption()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    private string S;

    public C([||]string s)
    {
        S = s;
    }
}",
                FixedCode = @"
using System;

class C
{
    private string S;

    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        S = s;
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false, NotificationOption2.Silent }
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExpressionBody1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    private string S;

    public C([||]string s)
        => S = s;
}",
@"
using System;

class C
{
    private string S;

    public C(string s)
        => S = s ?? throw new ArgumentNullException(nameof(s));
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExpressionBody2()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C([||]string s)
        => Init();

    private void Init()
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        Init();
    }

    private void Init()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateExpressionBody3()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]string s)
        => Init();

    private void Init()
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        Init();
    }

    private void Init()
    {
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement }
                }
            }.RunAsync();
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateLocalFunctionExpressionBody_NonVoid()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    void M()
    {
        int F([||]string s) => Init();
    }

    private int Init() => 1;
}",
@"
using System;

class C
{
    void M()
    {
        int F(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return Init();
        }
    }

    private int Init() => 1;
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateLocalFunctionExpressionBody_Void()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    void M()
    {
        void F([||]string s) => Init();
    }

    private int Init() => 1;
}",
@"
using System;

class C
{
    void M()
    {
        void F(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            Init();
        }
    }

    private int Init() => 1;
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateLambdaExpressionBody_NonVoid()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    void M()
    {
        Func<string, int> f = [||]s => GetValue();

        int GetValue() => 0;
    }
}",
@"
using System;

class C
{
    void M()
    {
        Func<string, int> f = s =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return GetValue();
        };

        int GetValue() => 0;
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestUpdateLambdaExpressionBody_Void()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    void M()
    {
        Action<string> f = [||]s => NoValue();

        void NoValue() { }
    }
}",
@"
using System;

class C
{
    void M()
    {
        Action<string> f = s =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            NoValue();
        };

        void NoValue() { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertAfterExistingNullCheck1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C(string a, [||]string s)
    {
        if (a == null)
        {
        }
    }
}",
@"
using System;

class C
{
    public C(string a, string s)
    {
        if (a == null)
        {
        }

        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertBeforeExistingNullCheck1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C(string [||]a, string s)
    {
        if (s == null)
        {
        }
    }
}",
@"
using System;

class C
{
    public C(string a, string s)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (s == null)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck1()
        {
            var code = @"
using System;

class C
{
    public C([||]string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException();
        }
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck2()
        {
            var code = @"
using System;

class C
{
    private string _s;

    public C([||]string s)
    {
        _s = s ?? throw new ArgumentNullException();
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck3()
        {
            var code = @"
using System;

class C
{
    public C([||]string s)
    {
        if (string.IsNullOrEmpty(s))
        {
        }
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck4()
        {
            var code = @"
using System;

class C
{
    public C([||]string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
        }
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck5()
        {
            var code = @"
using System;

class C
{
    public C([||]string s)
    {
        if (null == s)
        {
            throw new ArgumentNullException();
        }
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheck6()
        {
            var code = @"
using System;

class C
{
    public C([||]string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException();
        }
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheckInLocalFunction()
        {
            var code = @"
using System;

class C
{
    public C()
    {
        void F([||]string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException();
            }
        }
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithExistingNullCheckInLambda()
        {
            var code = @"
using System;

class C
{
    public C()
    {
        Action<string> f = ([||]string s) => { if (s == null) { throw new ArgumentNullException(nameof(s)); } };
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingWithoutParameterName()
        {
            var code = @"
using System;

class C
{
    public C([||]string{|CS1001:)|}
    {
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInMethod()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    void F([||]string s)
    {
    }
}",
@"
using System;

class C
{
    void F(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInOperator()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public static C operator +(C c1, [||]string s)
    {
        return null;
    }
}",
@"
using System;

class C
{
    public static C operator +(C c1, [||]string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        return null;
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnSimpleLambdaParameter()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = [||]s => { return 0; };
    }
}",
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = s =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return 0;
        };
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnSimpleLambdaParameter_EmptyBlock()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C()
    {
        Action<string> f = [||]s => { };
    }
}",
@"
using System;

class C
{
    public C()
    {
        Action<string> f = s =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }
        };
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnParenthesizedLambdaParameter()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = ([||]string s) => { return 0; };
    }
}",
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = (string s) =>
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return 0;
        };
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnAnonymousMethodParameter()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = delegate ([||]string s) { return 0; };
    }
}",
@"
using System;

class C
{
    public C()
    {
        Func<string, int> f = delegate (string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return 0;
        };
    }
}");
        }

        [WorkItem(20983, "https://github.com/dotnet/roslyn/issues/20983")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestOnLocalFunctionParameter()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C()
    {
        void F([||]string s)
        {
        }
    }
}",
@"
using System;

class C
{
    public C()
    {
        void F(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnIndexerParameter()
        {
            var code = @"
class C
{
    int this[[||]string s]
    {
        get
        {
            return 0;
        }
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotOnIndexerParameters()
        {
            var code = @"
class C
{
    int this[[|object a|], object b, object c]
    {
        get
        {
            return 0;
        }
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSpecialStringCheck1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
                FixedCode = $@"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrEmpty(s))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
        }}
    }}
}}",
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSpecialStringCheck2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
                FixedCode = $@"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrWhiteSpace(s))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_whitespace, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
        }}
    }}
}}",
                CodeActionIndex = 2,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrWhiteSpace_check)
            }.RunAsync();
        }

        [WorkItem(51338, "https://github.com/dotnet/roslyn/issues/51338")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [UseCulture("de-DE", "de-DE")]
        public async Task TestSpecialStringCheck3()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
                FixedCode = $@"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrEmpty(s))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
        }}
    }}
}}",
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)
            }.RunAsync();
        }

        [WorkItem(19173, "https://github.com/dotnet/roslyn/issues/19173")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingOnUnboundTypeWithExistingNullCheck()
        {
            var code = @"
class C
{
    public C(string [||]s)
    {
        if (s == null)
        {
            throw new System.Exception();
        }
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [WorkItem(19174, "https://github.com/dotnet/roslyn/issues/19174")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestRespectPredefinedTypePreferences()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class Program
{
    static void Main([||]String bar)
    {
    }
}",
                FixedCode = @$"
using System;

class Program
{{
    static void Main(String bar)
    {{
        if (String.IsNullOrEmpty(bar))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(bar)}").Replace("\"", "\\\"")}"", nameof(bar));
        }}
    }}
}}",
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check),
                Options =
                {
                    { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, CodeStyleOptions2.FalseWithSuggestionEnforcement }
                }
            }.RunAsync();
        }

        [WorkItem(19172, "https://github.com/dotnet/roslyn/issues/19172")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [InlineData((int)PreferBracesPreference.None)]
        [InlineData((int)PreferBracesPreference.WhenMultiline)]
        public async Task TestPreferNoBlock(int preferBraces)
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]string s)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(string s)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferBraces, new CodeStyleOption2<PreferBracesPreference>((PreferBracesPreference)preferBraces, NotificationOption2.Silent) },
                }
            }.RunAsync();
        }

        [WorkItem(19956, "https://github.com/dotnet/roslyn/issues/19956")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNoBlock()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C(string s[||])
}",
                ExpectedDiagnostics = {
                    // /0/Test0.cs(6,12): error CS0501: 'C.C(string)' must declare a body because it is not marked abstract, extern, or partial
                    DiagnosticResult.CompilerError("CS0501").WithLocation(6, 12).WithArguments("C.C(string)"),
                    // /0/Test0.cs(6,23): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithLocation(6, 23),
                },
                FixedState =
                {
                    Sources = { @"
using System;

class C
{
    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}" },
                    InheritanceMode = StateInheritanceMode.Explicit
                }
            }.RunAsync();
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInArrowExpression1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array[||]) =>
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
}",
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array)
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        return array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
    }
}");
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInArrowExpression2()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array[||]) /* Bar */ => /* Bar */
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count(); /* Bar */
}",
@"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array) /* Bar */
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        /* Bar */
        return array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count(); /* Bar */
    }
}");
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingInArrowExpression1()
        {
            var code = @"
using System;
using System.Linq;

class C
{
    public void Foo(string bar[||]) =>
#if DEBUG
        Console.WriteLine(""debug"" + bar);
#else
        Console.WriteLine(""release"" + bar);
#endif
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingInArrowExpression2()
        {
            var code = @"
using System;
using System.Linq;

class C
{
    public int Foo(int[] array[||]) =>
#if DEBUG
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
#else
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
#endif
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [WorkItem(21501, "https://github.com/dotnet/roslyn/issues/21501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInArrowExpression3()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;
using System.Linq;

class C
{
    public void Foo(int[] array[||]) =>
        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
}",
@"
using System;
using System.Linq;

class C
{
    public void Foo(int[] array)
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        array.Where(x => x > 3)
            .OrderBy(x => x)
            .Count();
    }
}");
        }

        [WorkItem(29190, "https://github.com/dotnet/roslyn/issues/29190")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSimpleReferenceTypeWithParameterNameSelected1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
using System;

class C
{
    public C(string [|s|])
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [WorkItem(29333, "https://github.com/dotnet/roslyn/issues/29333")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestLambdaWithIncorrectNumberOfParameters()
        {
            var code = @"
using System;

class C
{
    void M(Action<int, int> a)
    {
        M((x[||]
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code,
                new[]
                {
                    // /0/Test0.cs(8,12): error CS0103: The name 'x' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithLocation(8, 12).WithArguments("x"),
                    // /0/Test0.cs(8,13): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithLocation(8, 13),
                    // /0/Test0.cs(8,13): error CS1026: ) expected
                    DiagnosticResult.CompilerError("CS1026").WithLocation(8, 13),
                    // /0/Test0.cs(8,13): error CS1026: ) expected
                    DiagnosticResult.CompilerError("CS1026").WithLocation(8, 13),
                }, code);
        }

        [WorkItem(41824, "https://github.com/dotnet/roslyn/issues/41824")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingInArgList()
        {
            var code = @"
class C
{
    private static void M()
    {
        M2(__arglist(1, 2, 3, 5, 6));
    }

    public static void M2([||]__arglist)
    {
    }
}";
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52383, "https://github.com/dotnet/roslyn/issues/52383")]
        public async Task TestImportSystem()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
class C
{
    public C([||]string s)
    {
    }
}",
@"
using System;

class C
{
    public C(string s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_NullCheck_BracesNone_SameLineFalse()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$object o)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(object o)
    {
        if (o is null)
            throw new ArgumentNullException(nameof(o));
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.None },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, false },
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_NullCheck_BracesWhenMultiline_SameLineFalse()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$object o)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(object o)
    {
        if (o is null)
            throw new ArgumentNullException(nameof(o));
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.WhenMultiline },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, false },
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_NullCheck_BracesAlways_SameLineFalse()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$object o)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(object o)
    {
        if (o is null)
        {
            throw new ArgumentNullException(nameof(o));
        }
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.Always },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, false },
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_NullCheck_BracesNone_SameLineTrue()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$object o)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(object o)
    {
        if (o is null) throw new ArgumentNullException(nameof(o));
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.None },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true },
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_NullCheck_BracesWhenMultiline_SameLineTrue()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$object o)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(object o)
    {
        if (o is null) throw new ArgumentNullException(nameof(o));
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.WhenMultiline },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true },
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_NullCheck_BracesAlways_SameLineTrue()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$object o)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(object o)
    {
        if (o is null)
        {
            throw new ArgumentNullException(nameof(o));
        }
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.Always },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true },
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_StringIsNullOrEmpty_BracesNone_SameLineFalse()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$string s)
    {
    }
}",
                FixedCode = $@"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrEmpty(s))
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
    }}
}}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.None },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, false },
                },
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_StringIsNullOrEmpty_BracesWhenMultiline_SameLineFalse()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$string s)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrEmpty(s))
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
    }}
}}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.WhenMultiline},
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, false },
                },
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_StringIsNullOrEmpty_BracesAlways_SameLineFalse()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$string s)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrEmpty(s))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
        }}
    }}
}}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.Always },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, false },
                },
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_StringIsNullOrEmpty_BracesNone_SameLineTrue()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$string s)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrEmpty(s)) throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
    }}
}}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.None },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true },
                },
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_StringIsNullOrEmpty_BracesWhenMultiline_SameLineTrue()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$string s)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrEmpty(s)) throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
    }}
}}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.WhenMultiline },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true },
                },
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_StringIsNullOrEmpty_BracesAlways_SameLineTrue()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C($$string s)
    {
    }
}",
                FixedCode = @$"
using System;

class C
{{
    public C(string s)
    {{
        if (string.IsNullOrEmpty(s))
        {{
            throw new ArgumentException($""{string.Format(FeaturesResources._0_cannot_be_null_or_empty, "{nameof(s)}").Replace("\"", "\\\"")}"", nameof(s));
        }}
    }}
}}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.Always },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true },
                },
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_string_IsNullOrEmpty_check)
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        [WorkItem(52385, "https://github.com/dotnet/roslyn/issues/52385")]
        public async Task SingleLineStatement_NullCheck_AllParameters()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    public C([||]object a, object b, object c)
    {
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(object a, object b, object c)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        if (c is null) throw new ArgumentNullException(nameof(c));
    }
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferThrowExpression, false },
                    { CSharpCodeStyleOptions.PreferBraces, PreferBracesPreference.None },
                    { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true },
                },
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Add_null_checks_for_all_parameters)
            }.RunAsync();
        }
    }
}
