// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.Mocks;
using PostSharp.Backstage.Licensing.Consumption.Sources;
using PostSharp.Backstage.Licensing.Licenses;
using Xunit;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Caravela.Compiler.UnitTests
{
    public class LicensingTests : CommandLineTestBase
    {
        private TempFile? _src;

        private MockCSharpCompiler CreateCompiler(ISourceTransformer? transformer = null, bool debugTransformedCode = false)
        {
            var dir = Temp.CreateDirectory();
            var fileNameWithoutExtension = "temp";
            _src = dir.CreateFile($"{fileNameWithoutExtension}.cs").WriteAllText("class C { }");
            var config = dir.CreateFile($"{fileNameWithoutExtension}.editorconfig").WriteAllText(
                $@"is_global = true
build_property.CaravelaLicenseSources = Property
build_property.CaravelaFirstRunLicenseActivatorEnabled = False
build_property.CaravelaDebugTransformedCode = {(debugTransformedCode ? "True" : "False")}
");
            var args = new[] { "/nologo", "/t:library", _src.Path, $"/analyzerconfig:{config.Path}" };
            var csc = CreateCSharpCompiler(null, dir.Path, args,
                transformers: transformer == null
                    ? ImmutableArray<ISourceTransformer>.Empty
                    : new[] { transformer }.ToImmutableArray(),
                bypassLicensing: false);

            return csc;
        }

        [Fact]
        public void LicenseNotRequiredWithoutTransformers()
        {
            var csc = CreateCompiler();

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);
            Assert.Empty(output);
        }

        [Fact]
        public void CustomTransformersRequireCommunityLicense()
        {
            var csc = CreateCompiler(new DummyTransformer());

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString().Trim();

            Assert.NotEqual(0, exitCode);
            Assert.Equal("error RE0007: No license available for feature(s) Community", output);
        }

        [Fact]
        public void TransformedCodeDebuggingRequiresCaravelaLicense()
        {
            var csc = CreateCompiler(new DummyTransformer(), debugTransformedCode: true);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString().Trim();

            Assert.NotEqual(0, exitCode);
            Assert.Equal($"error RE0007: No license available for feature(s) Community{Environment.NewLine}error RE0007: No license available for feature(s) Caravela", output);
        }

        public override void Dispose()
        {
            if (_src != null)
            {
                CleanupAllGeneratedFiles(_src.Path);
            }

            base.Dispose();
        }

        private class DummyTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
            }
        }
    }
}
