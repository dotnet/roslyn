// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class MetadataReferenceTests
    {
        [Fact]
        public async Task ResolveReferenceAssemblies_Net20()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net20.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net20_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net20.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net40()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net40.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net40_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net40.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net40_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net40.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net45()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net45.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net45_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net45.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net45_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net45.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net451()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net451.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net451_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net451.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net451_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net451.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net452()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net452.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net452_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net452.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net452_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net452.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net46()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net46.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net46_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net46.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net46_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net46.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net461()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net461.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net461_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net461.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net461_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net461.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net462()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net462.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net462_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net462.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net462_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net462.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net47()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net47.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net47_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net47.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net47_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net47.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net471()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net471.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net471_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net471.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net471_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net471.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net472()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net472.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net472_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net472.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net472_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net472.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net48()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Default;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net48_WindowsForms()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net48.WindowsForms;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net48_Wpf()
        {
            var referenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard10()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard10;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard11()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard11;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard12()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard12;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard13()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard13;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard14()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard14;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard15()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard15;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard16()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard16;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard20()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetStandard21()
        {
            var referenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetCoreApp10()
        {
            var referenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp10;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetCoreApp11()
        {
            var referenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp11;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetCoreApp20()
        {
            var referenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp20;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetCoreApp21()
        {
            var referenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp21;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetCoreApp30()
        {
            var referenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp30;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_NetCoreApp31()
        {
            var referenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
        }

        [Fact]
        public async Task ResolveReferenceAssemblies_Net50()
        {
#if NETCOREAPP1_1 || NET46
            Assert.False(ReferenceAssemblies.TestAccessor.IsPackageBased("net5.0"));
            Assert.ThrowsAny<NotSupportedException>(() => ReferenceAssemblies.Net.Net50);

            // Avoid a warning for 'async' operator
            await Task.Yield();
#else
            Assert.True(ReferenceAssemblies.TestAccessor.IsPackageBased("net5.0"));
            var referenceAssemblies = ReferenceAssemblies.Net.Net50;
            var resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Assert.NotEmpty(resolved);
#endif
        }

        [Theory]
        [InlineData("net40")]
        [InlineData("net45")]
        [InlineData("net451")]
        [InlineData("net452")]
        [InlineData("net46")]
        [InlineData("net461")]
        [InlineData("net462")]
        [InlineData("net47")]
        [InlineData("net471")]
        [InlineData("net472")]
        [InlineData("net48")]
        [InlineData("netcoreapp1.0")]
        [InlineData("netcoreapp1.1")]
        [InlineData("netcoreapp2.0")]
        [InlineData("netcoreapp2.1")]
        [InlineData("netcoreapp3.0")]
        [InlineData("netcoreapp3.1")]
#if !(NETCOREAPP1_1 || NET46)
        [InlineData("net5.0")]
#endif
        [InlineData("netstandard1.0")]
        [InlineData("netstandard1.1")]
        [InlineData("netstandard1.2")]
        [InlineData("netstandard1.3")]
        [InlineData("netstandard1.4")]
        [InlineData("netstandard1.5")]
        [InlineData("netstandard1.6")]
        [InlineData("netstandard2.0")]
        [InlineData("netstandard2.1")]
        public async Task ResolveHashSetExceptInNet20(string targetFramework)
        {
            var testCode = @"
using System.Collections.Generic;

class TestClass {
  HashSet<int> TestMethod() => throw null;
}
";

            await new CSharpTest()
            {
                TestCode = testCode,
                ReferenceAssemblies = ReferenceAssembliesForTargetFramework(targetFramework),
            }.RunAsync();
        }

        internal static ReferenceAssemblies ReferenceAssembliesForTargetFramework(string targetFramework)
        {
            return targetFramework switch
            {
                "net20" => ReferenceAssemblies.NetFramework.Net20.Default,
                "net40" => ReferenceAssemblies.NetFramework.Net40.Default,
                "net45" => ReferenceAssemblies.NetFramework.Net45.Default,
                "net451" => ReferenceAssemblies.NetFramework.Net451.Default,
                "net452" => ReferenceAssemblies.NetFramework.Net452.Default,
                "net46" => ReferenceAssemblies.NetFramework.Net46.Default,
                "net461" => ReferenceAssemblies.NetFramework.Net461.Default,
                "net462" => ReferenceAssemblies.NetFramework.Net462.Default,
                "net47" => ReferenceAssemblies.NetFramework.Net47.Default,
                "net471" => ReferenceAssemblies.NetFramework.Net471.Default,
                "net472" => ReferenceAssemblies.NetFramework.Net472.Default,
                "net48" => ReferenceAssemblies.NetFramework.Net48.Default,
                "netcoreapp1.0" => ReferenceAssemblies.NetCore.NetCoreApp10,
                "netcoreapp1.1" => ReferenceAssemblies.NetCore.NetCoreApp11,
                "netcoreapp2.0" => ReferenceAssemblies.NetCore.NetCoreApp20,
                "netcoreapp2.1" => ReferenceAssemblies.NetCore.NetCoreApp21,
                "netcoreapp3.0" => ReferenceAssemblies.NetCore.NetCoreApp30,
                "netcoreapp3.1" => ReferenceAssemblies.NetCore.NetCoreApp31,
                "net5.0" => ReferenceAssemblies.Net.Net50,
                "netstandard1.0" => ReferenceAssemblies.NetStandard.NetStandard10,
                "netstandard1.1" => ReferenceAssemblies.NetStandard.NetStandard11,
                "netstandard1.2" => ReferenceAssemblies.NetStandard.NetStandard12,
                "netstandard1.3" => ReferenceAssemblies.NetStandard.NetStandard13,
                "netstandard1.4" => ReferenceAssemblies.NetStandard.NetStandard14,
                "netstandard1.5" => ReferenceAssemblies.NetStandard.NetStandard15,
                "netstandard1.6" => ReferenceAssemblies.NetStandard.NetStandard16,
                "netstandard2.0" => ReferenceAssemblies.NetStandard.NetStandard20,
                "netstandard2.1" => ReferenceAssemblies.NetStandard.NetStandard21,
                null => throw new ArgumentNullException(nameof(targetFramework)),
                _ => throw new NotSupportedException($"Target framework '{targetFramework}' is not currently supported."),
            };
        }
    }
}
