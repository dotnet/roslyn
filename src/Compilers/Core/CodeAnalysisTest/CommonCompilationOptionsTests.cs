// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CommonCompilationOptionsTests
    {
        /// <summary>
        /// If this test fails, please update the <see cref="CompilationOptions.GetHashCodeHelper"/>
        /// and <see cref="CompilationOptions.EqualsHelper(CompilationOptions)"/> methods to
        /// make sure they are doing the right thing with your new field and then update the baseline
        /// here.
        /// </summary>
        [Fact]
        public void TestFieldsForEqualsAndGetHashCode()
        {
            ReflectionAssert.AssertPublicAndInternalFieldsAndProperties(
                typeof(CompilationOptions),
                "AssemblyIdentityComparer",
                "Language",
                "CheckOverflow",
                "ConcurrentBuild",
                "CryptoKeyContainer",
                "CryptoKeyFile",
                "CryptoPublicKey",
                "CurrentLocalTime",
                "DelaySign",
                "Deterministic",
                "EnableEditAndContinue",
                "Errors",
                "DebugPlusMode",
                "Features",
                "GeneralDiagnosticOption",
                "MainTypeName",
                "MetadataImportOptions",
                "MetadataReferenceResolver",
                "ModuleName",
                "NullableContextOptions",
                "OptimizationLevel",
                "OutputKind",
                "Platform",
                "PublicSign",
                "ReferencesSupersedeLowerVersions",
                "ScriptClassName",
                "SourceReferenceResolver",
                "SpecificDiagnosticOptions",
                "StrongNameProvider",
                "SyntaxTreeOptionsProvider",
                "ReportSuppressedDiagnostics",
                "WarningLevel",
                "XmlReferenceResolver");
        }
    }
}
