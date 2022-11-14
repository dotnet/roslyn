// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
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

        private const string PostSharpEssentials
            = "1-ZEQQQQQQATQEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAEJFEB4V4U8DUPY3JP4Y9SXVNF9CSV3ADB53Z69RDR7PZMZGF7GRQPQQ5ZH3PQF7PHJZQTP2";

        private const string PostSharpFramework
            = "1-ZEQQQQQQVTQEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAEDQ66TTJ4DS3VU8WVCU82LXX67S8SZ6X54UMRTATBV5CA7LDPHS2SQC85ZLBNMBFJKZQQDTFJJPA";

        private const string PostSharpUltimate
            = "1-ZEQQQQQQZTQEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAEJZWKEM8SCXJK6KJLFD92CAJFQKCGC67A9NVYA2JGNEHLB8QQG4JAF94J58KUJQZW8ZQQDTFJJPA";

        private const string MetalamaFreePersonal
            = "2-ZTQQQQQQ6QZEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAEJ2D6DXU2WGSFJYN7NBESWRPV5AX9D5WWKRKRQQK4J3YELXMLRVDXRVBTZSKQADXRJZQQDEZJGP4Q8USJG4X6P2";

        private const string MetalamaStarterBusiness
            = "3-ZUQQQQQQZUAEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAED3WYKF9V8KYEUMGXKBYPRQNNUVQDV3BHMGEHNJHLBKGSD57EEJQRD4F3SWWEA42CUZQQDEZJGP4Q8USJG4X6P2";

        private const string MetalamaProfessionalBusiness
            = "4-ZQAQQQQQZTAEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAEGGU48KSPSAKJRSXFLZAF72CTB3QUMT9RR6WTPVYSNJT46PKU645EAPBXKGNFCJPVKZQQDEZJGP4Q8USJG4X6P2";

        private const string MetalamaUltimateBusiness
            = "1-ZEQQQQQQZEAEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7EAEASD8CXFHZY99JSJCPGSS6F3Q258BHCEBQCCLP85GRPFUZWBPAKLCV8CDZQ3JUUZFPZQQDEZJGP4Q8USJG4X6P2";

        private const string MetalamaUltimateOpenSourceRedistribution
            = "8-ZQZQQQQQXEAEQCRCE4UW3UFEB4URXMHRB8KQBJJSB64LX7YQ2GYCXBSF629W7YDRH29BN7JFYCJX3MFVVAHZXJ9RS29KYTHFS8KQ7TFRS6ZTBVWZLKJVF3HZZHWA4ZKSX3DXZYBKR4MWCZF4AW43L2DLEPB5T8HFVMFKBYLUG2X78SQQBTWB2P7QNG4B27RXP3";

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
        [InlineData(PostSharpEssentials)]
        [InlineData(PostSharpFramework)]
        [InlineData(PostSharpUltimate)]
        [InlineData(MetalamaFreePersonal)]
        [InlineData(MetalamaStarterBusiness)]
        [InlineData(MetalamaProfessionalBusiness)]
        [InlineData(MetalamaUltimateBusiness)]
        [InlineData(MetalamaUltimateOpenSourceRedistribution)]
        public void LicenseNotRequiredWithoutTransformers(string license)
        {
            Test(license: license);
        }

        [Theory]
        [InlineData(None, InvalidLicenseOverallErrorCode)]
        [InlineData(PostSharpEssentials, InvalidLicenseOverallErrorCode)]
        [InlineData(PostSharpFramework, null)]
        [InlineData(PostSharpUltimate, null)]
        [InlineData(MetalamaFreePersonal, null)]
        [InlineData(MetalamaStarterBusiness, null)]
        [InlineData(MetalamaProfessionalBusiness, null)]
        [InlineData(MetalamaUltimateBusiness, null)]
        [InlineData(MetalamaUltimateOpenSourceRedistribution, null)]
        public void TransformersByPostSharpRequireLicense(string license, string? expectedErrorCode)
        {
            Test(new DummyTransformer(), false, license, expectedErrorCode);
        }

        [Theory]
        [InlineData(None, InvalidLicenseOverallErrorCode)]
        [InlineData(PostSharpEssentials, InvalidLicenseOverallErrorCode)]
        [InlineData(PostSharpFramework, null)]
        [InlineData(PostSharpUltimate, null)]
        [InlineData(MetalamaFreePersonal, InvalidLicenseForSdkErrorCode)]
        [InlineData(MetalamaStarterBusiness, InvalidLicenseForSdkErrorCode)]
        [InlineData(MetalamaProfessionalBusiness, null)]
        [InlineData(MetalamaUltimateBusiness, null)]
        [InlineData(MetalamaUltimateOpenSourceRedistribution, null)]
        public void ThirdPartyTransformersRequireLicense(string license, string? expectedErrorCode)
        {
            Test(new ThirdPartyDummyTransformer(), false, license, expectedErrorCode);
        }

        [Theory]
        [InlineData(None, InvalidLicenseOverallErrorCode)]
        [InlineData(PostSharpEssentials, InvalidLicenseOverallErrorCode)]
        [InlineData(PostSharpFramework, null)]
        [InlineData(PostSharpUltimate, null)]
        [InlineData(MetalamaFreePersonal, InvalidLicenseForTransformedCodeErrorCode)]
        [InlineData(MetalamaStarterBusiness, null)]
        [InlineData(MetalamaProfessionalBusiness, null)]
        [InlineData(MetalamaUltimateBusiness, null)]
        [InlineData(MetalamaUltimateOpenSourceRedistribution, null)]
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
