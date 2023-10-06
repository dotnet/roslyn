// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using Metalama.Backstage.Testing;
using Metalama.Compiler.UnitTests.ThirdParty;
using Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Metalama.Compiler.UnitTests
{
    public class LicensingTests : CommandLineTestBase
    {
        // We don't test all the combinations of products and license types as all the combinations are tested in Metalama.Backstage.

        private const string None
            = "";

        private const string InvalidLicenseOverallErrorCode = "LAMA0608";

        private const string InvalidLicenseForTransformedCodeErrorCode = "LAMA0609";

        private const string InvalidLicenseForSdkErrorCode = "LAMA0615";

        private TempFile? _src;

        private MockCSharpCompiler CreateCompiler(ISourceTransformer? transformer = null, bool debugTransformedCode = false, string license = "")
        {
            var dir = Temp.CreateDirectory();
            var fileNameWithoutExtension = "temp";
            _src = dir.CreateFile($"{fileNameWithoutExtension}.cs").WriteAllText("class C { }");
            var config = dir.CreateFile($"{fileNameWithoutExtension}.editorconfig").WriteAllText(
                $@"is_global = true
build_property.MetalamaLicense = {license}
build_property.MetalamaIgnoreUserLicenses = True
build_property.MetalamaDebugTransformedCode = {(debugTransformedCode ? "True" : "False")}
");
            var args = new[] { "/nologo", "/t:library", _src.Path, $"/analyzerconfig:{config.Path}" };
            var csc = CreateCSharpCompiler(null, dir.Path, args,
                transformers: transformer == null
                    ? null
                    : new[] { transformer },
                bypassLicensing: false);

            return csc;
        }

        private void Test(ISourceTransformer? transformer = null, bool debugTransformedCode = false, string license = "", string? expectedErrorCode = null)
        {
            var csc = CreateCompiler(transformer, debugTransformedCode, license);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString().Trim();

            if (expectedErrorCode == null)
            {
                Assert.Equal(0, exitCode);
                Assert.Empty(output);
            }
            else
            {
                Assert.NotEqual(0, exitCode);
                Assert.Contains(expectedErrorCode, output);
            }
        }

        [Theory]
        [InlineData(None)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpEssentials))]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpFramework))]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpUltimate))]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaFreePersonal))]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaStarterBusiness))]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaProfessionalBusiness))]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaUltimateBusiness))]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaUltimateOpenSourceRedistribution))]
        public void LicenseNotRequiredWithoutTransformers(string license)
        {
            Test(license: license);
        }

        [Theory]
        [InlineData(None, InvalidLicenseOverallErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpEssentials), InvalidLicenseOverallErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpFramework), null)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpUltimate), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaFreePersonal), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaStarterBusiness), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaProfessionalBusiness), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaUltimateBusiness), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaUltimateOpenSourceRedistribution), null)]
        public void TransformersByPostSharpRequireLicense(string license, string? expectedErrorCode)
        {
            Test(new DummyTransformer(), false, license, expectedErrorCode);
        }

        [Theory]
        [InlineData(None, InvalidLicenseOverallErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpEssentials), InvalidLicenseOverallErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpFramework), null)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpUltimate), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaFreePersonal), InvalidLicenseForSdkErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaStarterBusiness), InvalidLicenseForSdkErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaProfessionalBusiness), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaUltimateBusiness), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaUltimateOpenSourceRedistribution), null)]
        public void ThirdPartyTransformersRequireLicense(string license, string? expectedErrorCode)
        {
            Test(new ThirdPartyDummyTransformer(), false, license, expectedErrorCode);
        }

        [Theory]
        [InlineData(None, InvalidLicenseOverallErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpEssentials), InvalidLicenseOverallErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpFramework), null)]
        [TestLicensesInlineData(nameof(TestLicenses.PostSharpUltimate), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaFreePersonal), InvalidLicenseForTransformedCodeErrorCode)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaStarterBusiness), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaProfessionalBusiness), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaUltimateBusiness), null)]
        [TestLicensesInlineData(nameof(TestLicenses.MetalamaUltimateOpenSourceRedistribution), null)]
        public void DebuggingTransformedCodeRequiresLicense(string license, string? expectedErrorCode)
        {
            Test(new DummyTransformer(), true, license, expectedErrorCode);
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
