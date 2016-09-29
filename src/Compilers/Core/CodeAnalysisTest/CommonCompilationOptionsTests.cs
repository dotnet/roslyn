// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                "ExtendedCustomDebugInformation",
                "DebugPlusMode",
                "Features",
                "GeneralDiagnosticOption",
                "MainTypeName",
                "MetadataImportOptions",
                "MetadataReferenceResolver",
                "ModuleName",
                "OptimizationLevel",
                "OutputKind",
                "Platform",
                "PublicSign",
                "ReferencesSupersedeLowerVersions",
                "ScriptClassName",
                "SourceReferenceResolver",
                "SpecificDiagnosticOptions",
                "StrongNameProvider",
                "ReportSuppressedDiagnostics",
                "WarningLevel",
                "XmlReferenceResolver");
        }
    }
}
