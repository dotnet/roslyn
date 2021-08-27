// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias InteractiveHost;

using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    [Trait(Traits.Feature, Traits.Features.InteractiveHost)]
    public sealed class InteractiveHostDesktopInitTests : AbstractInteractiveHostTests
    {
        internal override InteractiveHostPlatform DefaultPlatform => InteractiveHostPlatform.Desktop32;
        internal override bool UseDefaultInitializationFile => true;

        [Fact]
        public async Task SearchPaths1()
        {
            var fxDir = await GetHostRuntimeDirectoryAsync();

            var dll = Temp.CreateFile(extension: ".dll").WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01);
            var srcDir = Temp.CreateDirectory();
            var dllDir = Path.GetDirectoryName(dll.Path)!;
            srcDir.CreateFile("goo.csx").WriteAllText("ReferencePaths.Add(@\"" + dllDir + "\");");

            // print default:
            await Host.ExecuteAsync(@"ReferencePaths");
            var output = await ReadOutputToEndAsync();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(PrintSearchPaths(fxDir), output);

            await Host.ExecuteAsync(@"SourcePaths");
            output = await ReadOutputToEndAsync();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(PrintSearchPaths(), output);

            // add and test if added:
            await Host.ExecuteAsync("SourcePaths.Add(@\"" + srcDir + "\");");

            await Host.ExecuteAsync(@"SourcePaths");

            output = await ReadOutputToEndAsync();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(PrintSearchPaths(srcDir.Path), output);

            // execute file (uses modified search paths), the file adds a reference path
            await Host.ExecuteFileAsync("goo.csx");

            await Host.ExecuteAsync(@"ReferencePaths");

            output = await ReadOutputToEndAsync();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(PrintSearchPaths(fxDir, dllDir), output);

            await Host.AddReferenceAsync(Path.GetFileName(dll.Path));

            await Host.ExecuteAsync(@"typeof(Metadata.ICSProp)");

            var error = await ReadErrorOutputToEndAsync();
            output = await ReadOutputToEndAsync();
            Assert.Equal("", error);
            Assert.Equal("[Metadata.ICSProp]\r\n", output);
        }

        [Fact]
        public async Task AddReference_AssemblyAlreadyLoaded()
        {
            var result = await LoadReferenceAsync("System.Core");
            var output = await ReadOutputToEndAsync();
            var error = await ReadErrorOutputToEndAsync();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", output);
            Assert.True(result);

            result = await LoadReferenceAsync("System.Core.dll");
            output = await ReadOutputToEndAsync();
            error = await ReadErrorOutputToEndAsync();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", output);
            Assert.True(result);
        }
    }
}
