// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

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
    }
}
