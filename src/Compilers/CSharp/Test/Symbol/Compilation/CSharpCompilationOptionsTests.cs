// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal static class CSharpCompilationOptionsExtensions
    {
        public static void VerifyErrors(this CSharpCompilationOptions options, params DiagnosticDescription[] expected)
        {
            options.Errors.Verify(expected);
        }
    }

    public class CSharpCompilationOptionsTests : CSharpTestBase
    {
        private void TestProperty<T>(
            Func<CSharpCompilationOptions, T, CSharpCompilationOptions> factory,
            Func<CSharpCompilationOptions, T> getter,
            T validNonDefaultValue)
        {
            var oldOpt1 = new CSharpCompilationOptions(OutputKind.ConsoleApplication);

            var validDefaultValue = getter(oldOpt1);

            // we need non-default value to test Equals and GetHashCode
            Assert.NotEqual(validNonDefaultValue, validDefaultValue);

            // check that the assigned value can be read:
            var newOpt1 = factory(oldOpt1, validNonDefaultValue);
            Assert.Equal(validNonDefaultValue, getter(newOpt1));
            Assert.Equal(0, newOpt1.Errors.Length);

            // check that creating new options with the same value yields the same options instance:
            var newOpt1_alias = factory(newOpt1, validNonDefaultValue);
            Assert.Same(newOpt1_alias, newOpt1);

            // check that Equals and GetHashCode work
            var newOpt2 = factory(oldOpt1, validNonDefaultValue);
            Assert.False(newOpt1.Equals(oldOpt1));
            Assert.True(newOpt1.Equals(newOpt2));

            Assert.Equal(newOpt1.GetHashCode(), newOpt2.GetHashCode());

            // test default(T):
            Assert.NotNull(factory(oldOpt1, default(T)));
        }

        [Fact]
        public void Invariants()
        {
            TestProperty((old, value) => old.WithOutputKind(value), opt => opt.OutputKind, OutputKind.DynamicallyLinkedLibrary);
            TestProperty((old, value) => old.WithModuleName(value), opt => opt.ModuleName, "foo.dll");
            TestProperty((old, value) => old.WithMainTypeName(value), opt => opt.MainTypeName, "Foo.Bar");
            TestProperty((old, value) => old.WithScriptClassName(value), opt => opt.ScriptClassName, "<Script>");
            TestProperty((old, value) => old.WithUsings(value), opt => opt.Usings, ImmutableArray.Create("A", "B"));
            TestProperty((old, value) => old.WithOptimizationLevel(value), opt => opt.OptimizationLevel, OptimizationLevel.Release);
            TestProperty((old, value) => old.WithOverflowChecks(value), opt => opt.CheckOverflow, true);
            TestProperty((old, value) => old.WithAllowUnsafe(value), opt => opt.AllowUnsafe, true);
            TestProperty((old, value) => old.WithCryptoKeyContainer(value), opt => opt.CryptoKeyContainer, "foo");
            TestProperty((old, value) => old.WithCryptoKeyFile(value), opt => opt.CryptoKeyFile, "foo");
            TestProperty((old, value) => old.WithCryptoPublicKey(value), opt => opt.CryptoPublicKey, ImmutableArray.Create<byte>(0, 1, 2, 3));
            TestProperty((old, value) => old.WithDelaySign(value), opt => opt.DelaySign, true);
            TestProperty((old, value) => old.WithPlatform(value), opt => opt.Platform, Platform.Itanium);
            TestProperty((old, value) => old.WithGeneralDiagnosticOption(value), opt => opt.GeneralDiagnosticOption, ReportDiagnostic.Suppress);
            TestProperty((old, value) => old.WithWarningLevel(value), opt => opt.WarningLevel, 3);

            TestProperty((old, value) => old.WithSpecificDiagnosticOptions(value), opt => opt.SpecificDiagnosticOptions,
                new Dictionary<string, ReportDiagnostic> { { "CS0001", ReportDiagnostic.Error } }.ToImmutableDictionary());
            TestProperty((old, value) => old.WithReportSuppressedDiagnostics(value), opt => opt.ReportSuppressedDiagnostics, true);

            TestProperty((old, value) => old.WithConcurrentBuild(value), opt => opt.ConcurrentBuild, false);
            TestProperty((old, value) => old.WithExtendedCustomDebugInformation(value), opt => opt.ExtendedCustomDebugInformation, false);
            TestProperty((old, value) => old.WithDebugPlusMode(value), opt => opt.DebugPlusMode, true);

            TestProperty((old, value) => old.WithXmlReferenceResolver(value), opt => opt.XmlReferenceResolver, new XmlFileResolver(null));
            TestProperty((old, value) => old.WithMetadataReferenceResolver(value), opt => opt.MetadataReferenceResolver, new TestMetadataReferenceResolver());
            TestProperty((old, value) => old.WithAssemblyIdentityComparer(value), opt => opt.AssemblyIdentityComparer, new DesktopAssemblyIdentityComparer(new AssemblyPortabilityPolicy()));
            TestProperty((old, value) => old.WithStrongNameProvider(value), opt => opt.StrongNameProvider, new DesktopStrongNameProvider());
        }

        [Fact]
        public void WithXxx()
        {
            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName(null).VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: 'null'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "null"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName("blah\0foo").VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: 'blah\0foo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "blah\0foo"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName("").VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", ""));

            Assert.Equal(0, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithMainTypeName(null).Errors.Length);
            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithMainTypeName("blah\0foo").VerifyErrors(
                // error CS7088: Invalid 'MainTypeName' value: 'blah\0foo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MainTypeName", "blah\0foo"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithMainTypeName("").VerifyErrors(
                // error CS7088: Invalid 'MainTypeName' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MainTypeName", ""));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithOutputKind((OutputKind)Int32.MaxValue).VerifyErrors(
                // error CS7088: Invalid 'OutputKind' value: 'Int32.MaxValue'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("OutputKind", Int32.MaxValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithOutputKind((OutputKind)Int32.MinValue).VerifyErrors(
                // error CS7088: Invalid 'OutputKind' value: 'Int32.MinValue'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("OutputKind", Int32.MinValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithOptimizationLevel((OptimizationLevel)Int32.MaxValue).VerifyErrors(
                // error CS7088: Invalid 'OptimizationLevel' value: 'Int32.MaxValue'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("OptimizationLevel", Int32.MaxValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithOptimizationLevel((OptimizationLevel)Int32.MinValue).VerifyErrors(
                // error CS7088: Invalid 'OptimizationLevel' value: 'Int32.MinValue'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("OptimizationLevel", Int32.MinValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithPlatform((Platform)Int32.MaxValue).VerifyErrors(
                // error CS1672: Invalid option 'Int32.MaxValue' for /platform; must be anycpu, x86, Itanium or x64
                Diagnostic(ErrorCode.ERR_BadPlatformType).WithArguments(Int32.MaxValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithPlatform((Platform)Int32.MinValue).VerifyErrors(
                // error CS1672: Invalid option 'Int32.MinValue' for /platform; must be anycpu, x86, Itanium or x64
                Diagnostic(ErrorCode.ERR_BadPlatformType).WithArguments(Int32.MinValue.ToString()));

            var defaultWarnings = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
            Assert.Equal(ReportDiagnostic.Default, defaultWarnings.GeneralDiagnosticOption);
            Assert.Equal(4, defaultWarnings.WarningLevel);

            Assert.Equal(ReportDiagnostic.Error, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithGeneralDiagnosticOption(ReportDiagnostic.Error).GeneralDiagnosticOption);
            Assert.Equal(ReportDiagnostic.Default, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithGeneralDiagnosticOption(ReportDiagnostic.Default).GeneralDiagnosticOption);
        }

        [Fact]
        public void WithUsings()
        {
            var actual1 = new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithUsings(new[] { "A", "B" }).Usings;
            Assert.True(actual1.SequenceEqual(new[] { "A", "B" }));

            var actual2 = new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithUsings(Enumerable.Repeat("A", 1)).Usings;
            Assert.True(actual2.SequenceEqual(Enumerable.Repeat("A", 1)));

            Assert.Equal(0, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithUsings("A", "B").WithUsings(null).Usings.Count());
            Assert.Equal(0, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithUsings("A", "B").WithUsings((string[])null).Usings.Count());

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithUsings(new string[] { null }).VerifyErrors(
                // error CS7088: Invalid 'Usings' value: 'null'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", "null"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithUsings(new string[] { "" }).VerifyErrors(
                // error CS7088: Invalid 'Usings' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", ""));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithUsings(new string[] { "blah\0foo" }).VerifyErrors(
                // error CS7088: Invalid 'Usings' value: 'blah\0foo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", "blah\0foo"));
        }

        [Fact]
        public void WithWarnings()
        {
            var warnings = new Dictionary<string, ReportDiagnostic>
            {
                { MessageProvider.Instance.GetIdForErrorCode(1), ReportDiagnostic.Error },
                { MessageProvider.Instance.GetIdForErrorCode(2), ReportDiagnostic.Suppress },
                { MessageProvider.Instance.GetIdForErrorCode(3), ReportDiagnostic.Warn }
            };

            Assert.Equal(3, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithSpecificDiagnosticOptions(warnings).SpecificDiagnosticOptions.Count);

            Assert.Equal(0, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithSpecificDiagnosticOptions(null).SpecificDiagnosticOptions.Count);

            Assert.Equal(1, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithWarningLevel(1).WarningLevel);
            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithWarningLevel(-1).VerifyErrors(
                // error CS7088: Invalid 'WarningLevel' value: '-1'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("WarningLevel", "-1"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithWarningLevel(5).VerifyErrors(
                // error CS7088: Invalid 'WarningLevel' value: '5'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("WarningLevel", "5"));
        }

        [Fact]
        public void WithModuleName()
        {
            // ModuleName
            Assert.Equal(null, TestOptions.ReleaseDll.WithModuleName(null).ModuleName);
            TestOptions.ReleaseDll.WithModuleName("").VerifyErrors(
    // error CS7087: Name cannot be empty.
    // Parameter name: ModuleName
    Diagnostic(ErrorCode.ERR_BadCompilationOption).WithArguments(new ArgumentException(CodeAnalysisResources.NameCannotBeEmpty, "ModuleName").Message));

            TestOptions.ReleaseDll.WithModuleName("a\0a").VerifyErrors(
    // error CS7087: Name contains invalid characters.
    // Parameter name: ModuleName
    Diagnostic(ErrorCode.ERR_BadCompilationOption).WithArguments(new ArgumentException(CodeAnalysisResources.NameContainsInvalidCharacter, "ModuleName").Message)
                );

            TestOptions.ReleaseDll.WithModuleName("a\uD800b").VerifyErrors(
    // error CS7087: Name contains invalid characters.
    // Parameter name: ModuleName
    Diagnostic(ErrorCode.ERR_BadCompilationOption).WithArguments(new ArgumentException(CodeAnalysisResources.NameContainsInvalidCharacter, "ModuleName").Message)
                );

            TestOptions.ReleaseDll.WithModuleName("a\\b").VerifyErrors(
    // error CS7087: Name contains invalid characters.
    // Parameter name: ModuleName
    Diagnostic(ErrorCode.ERR_BadCompilationOption).WithArguments(new ArgumentException(CodeAnalysisResources.NameContainsInvalidCharacter, "ModuleName").Message)
                );

            TestOptions.ReleaseDll.WithModuleName("a/b").VerifyErrors(
    // error CS7087: Name contains invalid characters.
    // Parameter name: ModuleName
    Diagnostic(ErrorCode.ERR_BadCompilationOption).WithArguments(new ArgumentException(CodeAnalysisResources.NameContainsInvalidCharacter, "ModuleName").Message)
                );

            TestOptions.ReleaseDll.WithModuleName("a:b").VerifyErrors(
    // error CS7087: Name contains invalid characters.
    // Parameter name: ModuleName
    Diagnostic(ErrorCode.ERR_BadCompilationOption).WithArguments(new ArgumentException(CodeAnalysisResources.NameContainsInvalidCharacter, "ModuleName").Message)
                );
        }

        [Fact]
        public void ConstructorValidation()
        {
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, usings: new string[] { null }).VerifyErrors(
                // error CS7088: Invalid 'Usings' value: 'null'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", "null"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, usings: new string[] { "" }).VerifyErrors(
                // error CS7088: Invalid 'Usings' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", ""));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, usings: new string[] { "blah\0foo" }).VerifyErrors(
                // error CS7088: Invalid 'Usings' value: 'blah\0foo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", "blah\0foo"));

            Assert.Equal("Script", new CSharpCompilationOptions(OutputKind.ConsoleApplication, scriptClassName: null).ScriptClassName);

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, scriptClassName: "blah\0foo").VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: 'blah\0foo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "blah\0foo"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, scriptClassName: "").VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", ""));

            Assert.Equal(0, new CSharpCompilationOptions(OutputKind.ConsoleApplication, mainTypeName: null).Errors.Length);
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, mainTypeName: "blah\0foo").VerifyErrors(
                // error CS7088: Invalid 'MainTypeName' value: 'blah\0foo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MainTypeName", "blah\0foo"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, mainTypeName: "").VerifyErrors(
                // error CS7088: Invalid 'MainTypeName' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MainTypeName", ""));

            new CSharpCompilationOptions(outputKind: (OutputKind)Int32.MaxValue).VerifyErrors(
                // error CS7088: Invalid 'OutputKind' value: 'Int32.MaxValue'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("OutputKind", Int32.MaxValue.ToString()));

            new CSharpCompilationOptions(outputKind: (OutputKind)Int32.MinValue).VerifyErrors(
                // error CS7088: Invalid 'OutputKind' value: 'Int32.MinValue'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("OutputKind", Int32.MinValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: (OptimizationLevel)Int32.MaxValue).VerifyErrors(
                // error CS7088: Invalid 'OptimizationLevel' value: 'Int32.MaxValue'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("OptimizationLevel", Int32.MaxValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: (OptimizationLevel)Int32.MinValue).VerifyErrors(
                // error CS7088: Invalid 'OptimizationLevel' value: 'Int32.MinValue'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("OptimizationLevel", Int32.MinValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, platform: (Platform)Int32.MinValue).VerifyErrors(
                // error CS1672: Invalid option 'Int32.MinValue' for /platform; must be anycpu, x86, Itanium or x64
                Diagnostic(ErrorCode.ERR_BadPlatformType).WithArguments(Int32.MinValue.ToString()));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, warningLevel: -1).VerifyErrors(
                // error CS7088: Invalid 'WarningLevel' value: '-1'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("WarningLevel", "-1"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, warningLevel: 5).VerifyErrors(
                // error CS7088: Invalid 'WarningLevel' value: '5'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("WarningLevel", "5"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, platform: Platform.AnyCpu32BitPreferred).VerifyErrors();

            new CSharpCompilationOptions(OutputKind.WindowsRuntimeApplication, platform: Platform.AnyCpu32BitPreferred).VerifyErrors();

            new CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata, platform: Platform.AnyCpu32BitPreferred).VerifyErrors(
                Diagnostic(ErrorCode.ERR_BadPrefer32OnLib));

            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, platform: Platform.AnyCpu32BitPreferred).VerifyErrors(
                Diagnostic(ErrorCode.ERR_BadPrefer32OnLib));
        }

        /// <summary>
        /// If this test fails, please update the <see cref="CSharpCompilationOptions.GetHashCode"/>
        /// and <see cref="CSharpCompilationOptions.Equals(CSharpCompilationOptions)"/> methods to
        /// make sure they are doing the right thing with your new field and then update the baseline
        /// here.
        /// </summary>
        [Fact]
        public void TestFieldsForEqualsAndGetHashCode()
        {
            ReflectionAssert.AssertPublicAndInternalFieldsAndProperties(
                typeof(CSharpCompilationOptions),
                "AllowUnsafe",
                "Usings");
        }

        [Fact]
        public void TestEqualitySemantics()
        {
            Assert.Equal(CreateCSharpCompilationOptions(), CreateCSharpCompilationOptions());
        }

        private static CSharpCompilationOptions CreateCSharpCompilationOptions()
        {
            string moduleName = null;
            string mainTypeName = null;
            string scriptClassName = null;
            IEnumerable<string> usings = null;
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug;
            bool checkOverflow = false;
            bool allowUnsafe = false;
            string cryptoKeyContainer = null;
            string cryptoKeyFile = null;
            ImmutableArray<byte> cryptoPublicKey = default(ImmutableArray<byte>);
            bool? delaySign = null;
            Platform platform = 0;
            ReportDiagnostic generalDiagnosticOption = 0;
            int warningLevel = 0;
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions = null;
            bool concurrentBuild = false;
            bool deterministic = false;
            bool extendedCustomDebugInformation = true;
            bool debugPlusMode = false;
            XmlReferenceResolver xmlReferenceResolver = new XmlFileResolver(null);
            SourceReferenceResolver sourceReferenceResolver = new SourceFileResolver(ImmutableArray<string>.Empty, null);
            MetadataReferenceResolver metadataReferenceResolver = new MetadataReferenceResolverWithEquality();
            AssemblyIdentityComparer assemblyIdentityComparer = AssemblyIdentityComparer.Default;           // Currently uses reference equality
            StrongNameProvider strongNameProvider = new DesktopStrongNameProvider();
            MetadataImportOptions metadataImportOptions = 0;
            bool reportSuppressedDiagnostics = false;
            return new CSharpCompilationOptions(OutputKind.ConsoleApplication, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName, usings,
                optimizationLevel, checkOverflow, allowUnsafe, cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign,
                platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions,
                concurrentBuild, deterministic, extendedCustomDebugInformation, debugPlusMode, xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver,
                assemblyIdentityComparer, strongNameProvider, metadataImportOptions,
                publicSign: false);
        }

        private sealed class MetadataReferenceResolverWithEquality : MetadataReferenceResolver
        {
            public override bool Equals(object other) => true;
            public override int GetHashCode() => 1;

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void WithCryptoPublicKey()
        {
            var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication);

            Assert.Equal(ImmutableArray<byte>.Empty, options.CryptoPublicKey);
            Assert.Equal(ImmutableArray<byte>.Empty, options.WithCryptoPublicKey(default(ImmutableArray<byte>)).CryptoPublicKey);

            Assert.Same(options, options.WithCryptoPublicKey(default(ImmutableArray<byte>)));
            Assert.Same(options, options.WithCryptoPublicKey(ImmutableArray<byte>.Empty));
        }
    }
}
