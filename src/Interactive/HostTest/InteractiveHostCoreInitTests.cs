// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias InteractiveHost;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
    using Xunit.Abstractions;

    [Trait(Traits.Feature, Traits.Features.InteractiveHost)]
    public sealed class InteractiveHostCoreInitTests(ITestOutputHelper testOutputHelper) : AbstractInteractiveHostTests(testOutputHelper)
    {
        internal override InteractiveHostPlatform DefaultPlatform => InteractiveHostPlatform.Core;
        internal override bool UseDefaultInitializationFile => true;

        [Fact]
        public async Task DefaultReferencesAndImports()
        {
            await Execute(@"
dynamic d = (""home"", Directory.GetCurrentDirectory(), await Task.FromResult(1));
WriteLine(d.ToString());
");

            var dir = Path.GetDirectoryName(typeof(InteractiveHostCoreInitTests).Assembly.Location);

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences($"(home, {dir}, 1)", output);
        }

        [Fact]
        public async Task InteractiveHostImplAssemblies()
        {
            var scriptingAssemblyName = typeof(CSharpScript).Assembly.GetName().Name;

            await Execute($@"#r ""{scriptingAssemblyName}""");

            var error = await ReadErrorOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(@$"
(1,1): error CS0006: {string.Format(CSharpResources.ERR_NoMetadataFile, scriptingAssemblyName)}", error);
        }

        [Fact]
        public async Task SearchPaths1()
        {
            var dll = Temp.CreateFile(extension: ".dll").WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01);
            var srcDir = Temp.CreateDirectory();
            var dllDir = Path.GetDirectoryName(dll.Path)!;
            srcDir.CreateFile("goo.csx").WriteAllText("ReferencePaths.Add(@\"" + dllDir + "\");");

            // print default:
            await Host.ExecuteAsync(@"ReferencePaths");
            var output = await ReadOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(PrintSearchPaths(), output);

            await Host.ExecuteAsync(@"SourcePaths");
            output = await ReadOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(PrintSearchPaths(), output);

            // add and test if added:
            await Host.ExecuteAsync("SourcePaths.Add(@\"" + srcDir + "\");");

            await Host.ExecuteAsync(@"SourcePaths");

            output = await ReadOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(PrintSearchPaths(srcDir.Path), output);

            // execute file (uses modified search paths), the file adds a reference path
            await Host.ExecuteFileAsync("goo.csx");

            await Host.ExecuteAsync(@"ReferencePaths");

            output = await ReadOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(PrintSearchPaths(dllDir), output);

            await Host.AddReferenceAsync(Path.GetFileName(dll.Path));

            await Host.ExecuteAsync(@"typeof(Metadata.ICSProp)");

            var error = await ReadErrorOutputToEnd();
            output = await ReadOutputToEnd();
            Assert.Equal("", error);
            Assert.Equal("[Metadata.ICSProp]\r\n", output);
        }
    }
}
