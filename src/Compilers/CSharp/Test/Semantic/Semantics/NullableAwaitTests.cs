// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    using Microsoft.CodeAnalysis.CSharp.UnitTests;
    using Roslyn.Test.Utilities;
    using Xunit;

    public class NullableAwaitTests : SemanticModelTestBase
    {
        [Fact]
        public void AwaitedNullableTypeAssignedToNonNullable_ProducesCS8600()
        {
            var src =
                @"
#nullable enable
using System.Threading.Tasks;
public class TestClass
{
    public async Task Main()
    {
        Task<string?> GetNullableStringAsync() => Task.FromResult<string?>(null);
        
        string? result = await GetNullableStringAsync();
        string nonNullableString = result;
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,36): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //          string nonNullableString = result; // warn
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "result")
                    .WithLocation(11, 36));
        }

        [Fact]
        public void AwaitResultNullableTypeAssignedToNonNullable_ProducesCS8600()
        {
            var src =
                @"
#nullable enable
using System.Threading.Tasks;
public class TestClass
{
    public void Main()
    {
        Task<string?> GetNullableStringAsync() => Task.FromResult<string?>(null);
        
        string? result = GetNullableStringAsync().Result;
        string nonNullableString = result;
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,36): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //          string nonNullableString = result; // warn
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "result")
                    .WithLocation(11, 36));
        }

        [Fact]
        [WorkItem(76886, "https://github.com/dotnet/roslyn/issues/76886")]
        public void AwaitedNullableValueTypeAssignedToNonNullable_ProducesCS8629()
        {
            var src =
                @"
#nullable enable
using System.Threading.Tasks;
public class TestClass
{
    public async Task Main()
    {
        Task<int?> GetNullableIntAsync() => Task.FromResult<int?>(null);
        
        int? result = await GetNullableIntAsync();
        int nonNullableInt = result.Value;
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,30): warning CS8629: Nullable value type may be null.
                //          int nonNullableInt = result.Value; // warn expected
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "result")
                    .WithLocation(11, 30));
        }

        [Fact]
        public void AwaitResultNullableValueTypeAssignedToNonNullable_ProducesCS8629()
        {
            var src =
                @"
#nullable enable
using System.Threading.Tasks;
public class TestClass
{
    public void Main()
    {
        Task<int?> GetNullableIntAsync() => Task.FromResult<int?>(null);
        
        int? result = GetNullableIntAsync().Result;
        int nonNullableInt = result.Value;
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,30): warning CS8629: Nullable value type may be null.
                //          int nonNullableInt = result.Value; // warn
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "result")
                    .WithLocation(11, 30));
        }

        [Fact]
        public void NullableValueTypeAssignedToNonNullable_ProducesCS8629()
        {
            var src =
                @"
#nullable enable
using System.Threading.Tasks;
public class TestClass
{
    public async Task Main()
    {
        int? result = null;
        int nonNullableInt = result.Value;
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,30): warning CS8629: Nullable value type may be null.
                //         int nonNullableInt = result.Value; // warn
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "result")
                    .WithLocation(9, 30));
        }

        [Fact]
        public void Await_GenericValueTypeResult_PreservesNullabilityFlow()
        {
            var src =
                @"
#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    public async Task<KeyValuePair<T1, T2>> M<T1, T2>(T1 t1, T2 t2) => default;

    public async Task M1()
    {
        string? s = null;
        int? i = null;
        var res = await M(s, i);
        res.Key.ToString();   // expect warning on receiver of 'ToString()'
        _ = res.Value.Value;  // warn
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         res.Key.ToString(); // expect warning on receiver of 'ToString()'
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "res.Key")
                    .WithLocation(14, 9),
                // (15,13): warning CS8629: Nullable value type may be null.
                //          _ = res.Value.Value; // warn
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "res.Value")
                    .WithLocation(15, 13));
        }

        [Fact]
        public void InvalidAwait_GenericAwaitResult_StillProducesNullabilityWarning()
        {
            var src =
                @"
#nullable enable
using System.Collections.Immutable;
using System.Threading.Tasks;
class C
{
    public async Task<ImmutableArray<T>> M<T>(T value) => [value];

    public Task M1()
    {
        string? s = null;
        var res = await M(s); // error: 'await' without 'async'
        res[0].ToString();    // expect warning on receiver of 'ToString()'
    }
}";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net100);
            comp.VerifyDiagnostics(
                // (9,17): error CS0161: 'C.M1()': not all code paths return a value
                //         public Task M1()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M1")
                    .WithArguments("C.M1()")
                    .WithLocation(9, 17),
                // (12,19): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task>'.
                //          var res = await M(s); // error: 'await' without 'async'
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await M(s)")
                    .WithArguments("System.Threading.Tasks.Task")
                    .WithLocation(12, 19),
                // (13,9): warning CS8602: Dereference of a possibly null reference.
                //         res[0].ToString();    // expect warning on receiver of 'ToString()'
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "res[0]")
                    .WithLocation(13, 9));
        }
    }
}
