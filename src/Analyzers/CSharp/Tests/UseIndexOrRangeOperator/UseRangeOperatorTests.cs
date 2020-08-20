// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseRangeOperatorDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseRangeOperatorCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIndexOrRangeOperator
{
    public class UseRangeOperatorTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNotInCSharp7()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring(1, s.Length - 1);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestWithMissingReference()
        {
            var source =
@"class {|#0:C|}
{
    {|#1:void|} Goo({|#2:string|} s)
    {
        var v = s.Substring({|#3:1|}, s.Length - {|#4:1|});
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = new ReferenceAssemblies("custom"),
                TestCode = source,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(1,7): error CS0518: Predefined type 'System.Object' is not defined or imported
                    DiagnosticResult.CompilerError("CS0518").WithLocation(0).WithArguments("System.Object"),
                    // /0/Test0.cs(1,7): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                    DiagnosticResult.CompilerError("CS1729").WithLocation(0).WithArguments("object", "0"),
                    // /0/Test0.cs(3,5): error CS0518: Predefined type 'System.Void' is not defined or imported
                    DiagnosticResult.CompilerError("CS0518").WithLocation(1).WithArguments("System.Void"),
                    // /0/Test0.cs(3,14): error CS0518: Predefined type 'System.String' is not defined or imported
                    DiagnosticResult.CompilerError("CS0518").WithLocation(2).WithArguments("System.String"),
                    // /0/Test0.cs(5,29): error CS0518: Predefined type 'System.Int32' is not defined or imported
                    DiagnosticResult.CompilerError("CS0518").WithLocation(3).WithArguments("System.Int32"),
                    // /0/Test0.cs(5,43): error CS0518: Predefined type 'System.Int32' is not defined or imported
                    DiagnosticResult.CompilerError("CS0518").WithLocation(4).WithArguments("System.Int32"),
                },
                FixedCode = source,
            }.RunAsync();
        }

        [WorkItem(36909, "https://github.com/dotnet/roslyn/issues/36909")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNotWithoutSystemRange()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring(1, s.Length - 1);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp20,
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestSimple()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring([|1, s.Length - 1|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s)
    {
        var v = s[1..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestMultipleDefinitions()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring([|1, s.Length - 1|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s)
    {
        var v = s[1..];
    }
}";

            // Adding a dependency with internal definitions of Index and Range should not break the feature
            var source1 = "namespace System { internal struct Index { } internal struct Range { } }";
            var dependencyReferences = await ReferenceAssemblies.NetStandard.NetStandard20.ResolveAsync(null, CancellationToken.None);

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var dependencyProject = solution.AddProject("DependencyProject", "DependencyProject", LanguageNames.CSharp)
                            .WithCompilationOptions(solution.GetProject(projectId).CompilationOptions)
                            .WithParseOptions(solution.GetProject(projectId).ParseOptions)
                            .WithMetadataReferences(dependencyReferences)
                            .AddDocument("Test0.cs", source1, filePath: "Test0.cs").Project;

                        return dependencyProject.Solution.AddProjectReference(projectId, new ProjectReference(dependencyProject.Id));
                    },
                },
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestComplexSubstraction()
        {
            var source =
@"
class C
{
    void Goo(string s, int bar, int baz)
    {
        var v = s.Substring([|bar, s.Length - baz - bar|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s, int bar, int baz)
    {
        var v = s[bar..^baz];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestSubstringOneArgument()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring([|1|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s)
    {
        var v = s[1..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestSliceOneArgument()
        {
            var source =
@"
using System;
class C
{
    void Goo(Span<int> s)
    {
        var v = s.Slice([|1|]);
    }
}";
            var fixedSource =
@"
using System;
class C
{
    void Goo(Span<int> s)
    {
        var v = s[1..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestExpressionOneArgument()
        {
            var source =
@"
class C
{
    void Goo(string s, int bar)
    {
        var v = s.Substring([|bar|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s, int bar)
    {
        var v = s[bar..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestConstantSubtraction1()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring([|1, s.Length - 2|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s)
    {
        var v = s[1..^1];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNotWithoutSubtraction()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring(1, 2);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNonStringType()
        {
            var source =
@"
struct S { public S Slice(int start, int length) => default; public int Length { get; } public S this[System.Range r] { get => default; } }
class C
{
    void Goo(S s)
    {
        var v = s.Slice([|1, s.Length - 2|]);
    }
}";
            var fixedSource =
@"
struct S { public S Slice(int start, int length) => default; public int Length { get; } public S this[System.Range r] { get => default; } }
class C
{
    void Goo(S s)
    {
        var v = s[1..^1];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNonStringTypeWithoutRangeIndexer()
        {
            var source =
@"
struct S { public S Slice(int start, int length) => default; public int Length { get; } }
class C
{
    void Goo(S s)
    {
        var v = s.Slice([|1, s.Length - 2|]);
    }
}";
            var fixedSource =
@"
struct S { public S Slice(int start, int length) => default; public int Length { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[1..^1];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNonStringType_Assignment()
        {
            var source =
@"
struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public ref S this[System.Range r] { get => throw null; } }
class C
{
    void Goo(S s)
    {
        s.Slice([|1, s.Length - 2|]) = default;
    }
}";
            var fixedSource =
@"
struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public ref S this[System.Range r] { get => throw null; } }
class C
{
    void Goo(S s)
    {
        s[1..^1] = default;
    }
}";
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestMethodToMethod()
        {
            var source =
@"
struct S { public int Slice(int start, int length) => 0; public int Length { get; } public int Slice(System.Range r) => 0; }
class C
{
    void Goo(S s)
    {
        var v = s.Slice([|1, s.Length - 2|]);
    }
}";
            var fixedSource =
@"
struct S { public int Slice(int start, int length) => 0; public int Length { get; } public int Slice(System.Range r) => 0; }
class C
{
    void Goo(S s)
    {
        var v = s.Slice(1..^1);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestFixAllInvocationToElementAccess1()
        {
            // Note: once the IOp tree has support for range operators, this should 
            // simplify even further.
            var source =
@"
class C
{
    void Goo(string s, string t)
    {
        var v = t.Substring([|s.Substring([|1, s.Length - 2|])[0], t.Length - s.Substring([|1, s.Length - 2|])[0]|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s, string t)
    {
        var v = t[s[1..^1][0]..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestFixAllInvocationToElementAccess2()
        {
            // Note: once the IOp tree has support for range operators, this should 
            // simplify even further.
            var source =
@"
class C
{
    void Goo(string s, string t)
    {
        var v = t.Substring([|s.Substring([|1|])[0]|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s, string t)
    {
        var v = t[s[1..][0]..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestWithTypeWithActualSliceMethod1()
        {
            var source =
@"
using System;
class C
{
    void Goo(Span<int> s)
    {
        var v = s.Slice([|1, s.Length - 1|]);
    }
}";
            var fixedSource =
@"
using System;
class C
{
    void Goo(Span<int> s)
    {
        var v = s[1..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestWithTypeWithActualSliceMethod2()
        {
            var source =
@"
using System;
class C
{
    void Goo(Span<int> s)
    {
        var v = s.Slice([|1, s.Length - 2|]);
    }
}";
            var fixedSource =
@"
using System;
class C
{
    void Goo(Span<int> s)
    {
        var v = s[1..^1];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [WorkItem(43202, "https://github.com/dotnet/roslyn/issues/43202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestWritableType()
        {
            var source =
@"
using System;
struct S { 
    public ref S Slice(int start, int length) => throw null; 
    public int Length { get; } 
    public S this[System.Range r] { get => default; } 
}

class C
{
    void Goo(S s)
    {
        s.Slice(1, s.Length - 2) = default;
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestReturnByRef()
        {
            var source =
@"
struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public S this[System.Range r] { get => throw null; } }
class C
{
    void Goo(S s)
    {
        var x = s.Slice([|1, s.Length - 2|]);
    }
}";
            var fixedSource =
@"
struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public S this[System.Range r] { get => throw null; } }
class C
{
    void Goo(S s)
    {
        var x = s[1..^1];
    }
}";
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [WorkItem(43202, "https://github.com/dotnet/roslyn/issues/43202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestIntWritableType()
        {
            var source =
@"
using System;
struct S { 
    public ref S Slice(int start, int length) => throw null;
    public int Length { get; }
    public S this[int r] { get => default; }
}

class C
{
    void Goo(S s)
    {
        s.Slice([|1, s.Length - 2|]) = default;
    }
}";
            var fixedSource =
@"
using System;
struct S { 
    public ref S Slice(int start, int length) => throw null;
    public int Length { get; }
    public S this[int r] { get => default; }
}

class C
{
    void Goo(S s)
    {
        s[1..^1] = default;
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [WorkItem(43202, "https://github.com/dotnet/roslyn/issues/43202")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestReadWriteProperty()
        {
            var source =
@"
using System;
struct S { 
    public ref S Slice(int start, int length) => throw null;
    public int Length { get; }
    public S this[System.Range r] { get => default; set { } }
}

class C
{
    void Goo(S s)
    {
        s.Slice([|1, s.Length - 2|]) = default;
    }
}";
            var fixedSource =
@"
using System;
struct S { 
    public ref S Slice(int start, int length) => throw null;
    public int Length { get; }
    public S this[System.Range r] { get => default; set { } }
}

class C
{
    void Goo(S s)
    {
        s[1..^1] = default;
    }
}";
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestWithTypeWithActualSliceMethod3()
        {
            var source =
@"
using System;
class C
{
    void Goo(Span<int> s)
    {
        var v = s.Slice([|1|]);
    }
}";
            var fixedSource =
@"
using System;
class C
{
    void Goo(Span<int> s)
    {
        var v = s[1..];
    }
}";
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [WorkItem(36997, "https://github.com/dotnet/roslyn/issues/36997")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestExpressionWithAddOperatorArgument()
        {
            var source =
@"
class C
{
    void Goo(string s, int bar)
    {
        var v = s.Substring([|bar + 1|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s, int bar)
    {
        var v = s[(bar + 1)..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestExpressionWithElementAccessShouldNotAddParentheses()
        {
            var source =
@"
class C
{
    void Goo(string s, int[] bar)
    {
        _ = s.Substring([|bar[0]|]);
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s, int[] bar)
    {
        _ = s[bar[0]..];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }
    }
}
