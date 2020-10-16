﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseIndexOperatorDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator.CSharpUseIndexOperatorCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIndexOrRangeOperator
{
    public class UseIndexOperatorTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestNotInCSharp7()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s[s.Length - 1];
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
        var v = s[s.Length - {|#3:1|}];
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
                    // /0/Test0.cs(5,30): error CS0518: Predefined type 'System.Int32' is not defined or imported
                    DiagnosticResult.CompilerError("CS0518").WithLocation(3).WithArguments("System.Int32"),
                },
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestSimple()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s[[|s.Length - 1|]];
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s)
    {
        var v = s[^1];
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
        var v = s[[|s.Length - 1|]];
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s)
    {
        var v = s[^1];
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestComplexSubtaction()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s[[|s.Length - (1 + 1)|]];
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s)
    {
        var v = s[^(1 + 1)];
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
        public async Task TestComplexInstance()
        {
            var source =
@"
using System.Linq;

class C
{
    void Goo(string[] ss)
    {
        var v = ss.Last()[[|ss.Last().Length - 3|]];
    }
}";
            var fixedSource =
@"
using System.Linq;

class C
{
    void Goo(string[] ss)
    {
        var v = ss.Last()[^3];
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
        public async Task TestNotWithoutSubtraction1()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s[s.Length];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestNotWithoutSubtraction2()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v = s[s.Length + 1];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestNotWithMultipleArgs()
        {
            var source =
@"
struct S { public int Length { get; } public int this[int i] { get => 0; } public int this[int i, int j] { get => 0; } public int this[System.Index i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[s.Length - 1, 2];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestUserDefinedTypeWithLength()
        {
            var source =
@"
struct S { public int Length { get; } public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[[|s.Length - 2|]];
    }
}";
            var fixedSource =
@"
struct S { public int Length { get; } public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[^2];
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
        public async Task TestUserDefinedTypeWithCount()
        {
            var source =
@"
struct S { public int Count { get; } public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[[|s.Count - 2|]];
    }
}";
            var fixedSource =
@"
struct S { public int Count { get; } public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[^2];
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
        public async Task TestUserDefinedTypeWithNoLengthOrCount()
        {
            var source =
@"
struct S { public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[s.{|CS1061:Count|} - 2];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestUserDefinedTypeWithNoInt32Indexer()
        {
            var source =
@"
struct S { public int Length { get; } public int this[System.Index i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[s.Length - 2];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestUserDefinedTypeWithNoIndexIndexer()
        {
            var source =
@"
struct S { public int Count { get; } public int this[int i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[[|s.Count - 2|]];
    }
}";
            var fixedSource =
@"
struct S { public int Count { get; } public int this[int i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s[^2];
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
        public async Task TestMethodToMethod()
        {
            var source =
@"
struct S { public int Length { get; } public int Get(int i) => 0; public int Get(System.Index i) => 0; }
class C
{
    void Goo(S s)
    {
        var v = s.Get([|s.Length - 1|]);
    }
}";
            var fixedSource =
@"
struct S { public int Length { get; } public int Get(int i) => 0; public int Get(System.Index i) => 0; }
class C
{
    void Goo(S s)
    {
        var v = s.Get(^1);
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
        public async Task TestMethodToMethodMissingIndexIndexer()
        {
            var source =
@"
struct S { public int Length { get; } public int Get(int i) => 0; }
class C
{
    void Goo(S s)
    {
        var v = s.Get(s.Length - 1);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestMethodToMethodWithIntIndexer()
        {
            var source =
@"
struct S { public int Length { get; } public int Get(int i) => 0; public int this[int i] { get => 0; } }
class C
{
    void Goo(S s)
    {
        var v = s.Get(s.Length - 1);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [WorkItem(36909, "https://github.com/dotnet/roslyn/issues/36909")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestMissingWithNoSystemIndex()
        {
            var source =
@"
class C
{
    void Goo(string[] s)
    {
        var v = s[s.Length - 1];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp20,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestArray()
        {
            var source =
@"
class C
{
    void Goo(string[] s)
    {
        var v = s[[|s.Length - 1|]];
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string[] s)
    {
        var v = s[^1];
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
        public async Task TestFixAll1()
        {
            var source =
@"
class C
{
    void Goo(string s)
    {
        var v1 = s[[|s.Length - 1|]];
        var v2 = s[[|s.Length - 1|]];
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string s)
    {
        var v1 = s[^1];
        var v2 = s[^1];
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
        public async Task TestNestedFixAll1()
        {
            var source =
@"
class C
{
    void Goo(string[] s)
    {
        var v1 = s[[|s.Length - 2|]][[|s[[|s.Length - 2|]].Length - 1|]];
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string[] s)
    {
        var v1 = s[^2][^1];
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
        public async Task TestNestedFixAll2()
        {
            var source =
@"
class C
{
    void Goo(string[] s)
    {
        var v1 = s[[|s.Length - 2|]][[|s[[|s.Length - 2|]].Length - 1|]];
    }
}";
            var fixedSource =
@"
class C
{
    void Goo(string[] s)
    {
        var v1 = s[^2][^1];
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
        public async Task TestSimple_NoIndexIndexer_SupportsIntIndexer()
        {
            var source =
@"
using System.Collections.Generic;
class C
{
    void Goo(List<int> s)
    {
        var v = s[[|s.Count - 1|]];
    }
}";
            var fixedSource =
@"
using System.Collections.Generic;
class C
{
    void Goo(List<int> s)
    {
        var v = s[^1];
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
        public async Task TestSimple_NoIndexIndexer_SupportsIntIndexer_Set()
        {
            var source =
@"
using System.Collections.Generic;
class C
{
    void Goo(List<int> s)
    {
        s[[|s.Count - 1|]] = 1;
    }
}";
            var fixedSource =
@"
using System.Collections.Generic;
class C
{
    void Goo(List<int> s)
    {
        s[^1] = 1;
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
        public async Task NotOnConstructedIndexer()
        {
            var source =
@"
using System.Collections.Generic;
class C
{
    void Goo(Dictionary<int, string> s)
    {
        var v = s[s.Count - 1];
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }
    }
}
