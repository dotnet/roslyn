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
        /// <summary>
        /// Using an instance of <see cref="CSharpCompilationOptions"/>, tests a property in <see cref="CompilationOptions"/> , even it is hidden by <see cref="CSharpCompilationOptions"/>.
        /// </summary>
        private void TestHiddenProperty<T>(
            Func<CompilationOptions, T, CompilationOptions> factory,
            Func<CompilationOptions, T> getter,
            T validNonDefaultValue)
        {
            TestPropertyGeneric(new CSharpCompilationOptions(OutputKind.ConsoleApplication), factory, getter, validNonDefaultValue);
        }

        private static void TestPropertyGeneric<TOptions, T>(TOptions oldOptions, Func<TOptions, T, TOptions> factory,
            Func<TOptions, T> getter, T validNonDefaultValue)
            where TOptions : CompilationOptions
        {
            var validDefaultValue = getter(oldOptions);

            // we need non-default value to test Equals and GetHashCode
            Assert.NotEqual(validNonDefaultValue, validDefaultValue);

            // check that the assigned value can be read:
            var newOpt1 = factory(oldOptions, validNonDefaultValue);
            Assert.Equal(validNonDefaultValue, getter(newOpt1));
            Assert.Equal(0, newOpt1.Errors.Length);

            // check that creating new options with the same value yields the same options instance:
            var newOpt1_alias = factory(newOpt1, validNonDefaultValue);
            Assert.Same(newOpt1_alias, newOpt1);

            // check that Equals and GetHashCode work
            var newOpt2 = factory(oldOptions, validNonDefaultValue);
            Assert.False(newOpt1.Equals(oldOptions));
            Assert.True(newOpt1.Equals(newOpt2));

            Assert.Equal(newOpt1.GetHashCode(), newOpt2.GetHashCode());

            // test default(T):
            Assert.NotNull(factory(oldOptions, default(T)));
        }

        [Fact]
        public void ShadowInvariants()
        {
            TestHiddenProperty((old, value) => old.WithOutputKind(value), opt => opt.OutputKind, OutputKind.DynamicallyLinkedLibrary);
            TestHiddenProperty((old, value) => old.WithModuleName(value), opt => opt.ModuleName, "goo.dll");
            TestHiddenProperty((old, value) => old.WithMainTypeName(value), opt => opt.MainTypeName, "Goo.Bar");
            TestHiddenProperty((old, value) => old.WithScriptClassName(value), opt => opt.ScriptClassName, "<Script>");
            TestHiddenProperty((old, value) => old.WithOptimizationLevel(value), opt => opt.OptimizationLevel, OptimizationLevel.Release);
            TestHiddenProperty((old, value) => old.WithOverflowChecks(value), opt => opt.CheckOverflow, true);
            TestHiddenProperty((old, value) => old.WithCryptoKeyContainer(value), opt => opt.CryptoKeyContainer, "goo");
            TestHiddenProperty((old, value) => old.WithCryptoKeyFile(value), opt => opt.CryptoKeyFile, "goo");
            TestHiddenProperty((old, value) => old.WithCryptoPublicKey(value), opt => opt.CryptoPublicKey, ImmutableArray.Create<byte>(0, 1, 2, 3));
            TestHiddenProperty((old, value) => old.WithDelaySign(value), opt => opt.DelaySign, true);
            TestHiddenProperty((old, value) => old.WithPlatform(value), opt => opt.Platform, Platform.Itanium);
            TestHiddenProperty((old, value) => old.WithGeneralDiagnosticOption(value), opt => opt.GeneralDiagnosticOption, ReportDiagnostic.Suppress);

            TestHiddenProperty((old, value) => old.WithSpecificDiagnosticOptions(value), opt => opt.SpecificDiagnosticOptions,
                new Dictionary<string, ReportDiagnostic> { { "CS0001", ReportDiagnostic.Error } }.ToImmutableDictionary());
            TestHiddenProperty((old, value) => old.WithReportSuppressedDiagnostics(value), opt => opt.ReportSuppressedDiagnostics, true);

            TestHiddenProperty((old, value) => old.WithConcurrentBuild(value), opt => opt.ConcurrentBuild, false);

            TestHiddenProperty((old, value) => old.WithXmlReferenceResolver(value), opt => opt.XmlReferenceResolver, new XmlFileResolver(null));
            TestHiddenProperty((old, value) => old.WithMetadataReferenceResolver(value), opt => opt.MetadataReferenceResolver, new TestMetadataReferenceResolver());
            TestHiddenProperty((old, value) => old.WithAssemblyIdentityComparer(value), opt => opt.AssemblyIdentityComparer, new DesktopAssemblyIdentityComparer(new AssemblyPortabilityPolicy()));
            TestHiddenProperty((old, value) => old.WithStrongNameProvider(value), opt => opt.StrongNameProvider, new DesktopStrongNameProvider());
        }

        private void TestProperty<T>(
            Func<CSharpCompilationOptions, T, CSharpCompilationOptions> factory,
            Func<CSharpCompilationOptions, T> getter,
            T validNonDefaultValue)
        {
            TestPropertyGeneric(new CSharpCompilationOptions(OutputKind.ConsoleApplication), factory, getter, validNonDefaultValue);
        }

        [Fact]
        public void Invariants()
        {
            TestProperty((old, value) => old.WithOutputKind(value), opt => opt.OutputKind, OutputKind.DynamicallyLinkedLibrary);
            TestProperty((old, value) => old.WithModuleName(value), opt => opt.ModuleName, "goo.dll");
            TestProperty((old, value) => old.WithMainTypeName(value), opt => opt.MainTypeName, "Goo.Bar");
            TestProperty((old, value) => old.WithScriptClassName(value), opt => opt.ScriptClassName, "<Script>");
            TestProperty((old, value) => old.WithUsings(value), opt => opt.Usings, ImmutableArray.Create("A", "B"));
            TestProperty((old, value) => old.WithOptimizationLevel(value), opt => opt.OptimizationLevel, OptimizationLevel.Release);
            TestProperty((old, value) => old.WithOverflowChecks(value), opt => opt.CheckOverflow, true);
            TestProperty((old, value) => old.WithAllowUnsafe(value), opt => opt.AllowUnsafe, true);
            TestProperty((old, value) => old.WithCryptoKeyContainer(value), opt => opt.CryptoKeyContainer, "goo");
            TestProperty((old, value) => old.WithCryptoKeyFile(value), opt => opt.CryptoKeyFile, "goo");
            TestProperty((old, value) => old.WithCryptoPublicKey(value), opt => opt.CryptoPublicKey, ImmutableArray.Create<byte>(0, 1, 2, 3));
            TestProperty((old, value) => old.WithDelaySign(value), opt => opt.DelaySign, true);
            TestProperty((old, value) => old.WithPlatform(value), opt => opt.Platform, Platform.Itanium);
            TestProperty((old, value) => old.WithGeneralDiagnosticOption(value), opt => opt.GeneralDiagnosticOption, ReportDiagnostic.Suppress);
            TestProperty((old, value) => old.WithWarningLevel(value), opt => opt.WarningLevel, 3);

            TestProperty((old, value) => old.WithSpecificDiagnosticOptions(value), opt => opt.SpecificDiagnosticOptions,
                new Dictionary<string, ReportDiagnostic> { { "CS0001", ReportDiagnostic.Error } }.ToImmutableDictionary());
            TestProperty((old, value) => old.WithReportSuppressedDiagnostics(value), opt => opt.ReportSuppressedDiagnostics, true);

            TestProperty((old, value) => old.WithConcurrentBuild(value), opt => opt.ConcurrentBuild, false);
            TestProperty((old, value) => old.WithCurrentLocalTime(value), opt => opt.CurrentLocalTime, new DateTime(2005, 1, 1));
            TestProperty((old, value) => old.WithDebugPlusMode(value), opt => opt.DebugPlusMode, true);

            TestProperty((old, value) => old.WithXmlReferenceResolver(value), opt => opt.XmlReferenceResolver, new XmlFileResolver(null));
            TestProperty((old, value) => old.WithMetadataReferenceResolver(value), opt => opt.MetadataReferenceResolver, new TestMetadataReferenceResolver());
            TestProperty((old, value) => old.WithAssemblyIdentityComparer(value), opt => opt.AssemblyIdentityComparer, new DesktopAssemblyIdentityComparer(new AssemblyPortabilityPolicy()));
            TestProperty((old, value) => old.WithStrongNameProvider(value), opt => opt.StrongNameProvider, new DesktopStrongNameProvider());

            TestProperty((old, value) => old.WithTopLevelBinderFlags(value), opt => opt.TopLevelBinderFlags, BinderFlags.IgnoreCorLibraryDuplicatedTypes);
            TestProperty((old, value) => old.WithMetadataImportOptions(value), opt => opt.MetadataImportOptions, MetadataImportOptions.Internal);
            TestProperty((old, value) => old.WithReferencesSupersedeLowerVersions(value), opt => opt.ReferencesSupersedeLowerVersions, true);
            TestProperty((old, value) => old.WithNullableContextOptions(value), opt => opt.NullableContextOptions, NullableContextOptions.Enable);
        }

        [Fact]
        public void WithXxx()
        {
            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName(null).VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: 'null'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "null"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName("blah\0goo").VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: 'blah\0goo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "blah\0goo"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName("").VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", ""));

            Assert.Equal(0, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithMainTypeName(null).Errors.Length);
            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithMainTypeName("blah\0goo").VerifyErrors(
                // error CS7088: Invalid 'MainTypeName' value: 'blah\0goo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MainTypeName", "blah\0goo"));

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

            new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithUsings(new string[] { "blah\0goo" }).VerifyErrors(
                // error CS7088: Invalid 'Usings' value: 'blah\0goo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", "blah\0goo"));
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
                // error CS7087: Invalid module name: Name cannot be empty.
                Diagnostic(ErrorCode.ERR_BadModuleName).WithArguments("Name cannot be empty.").WithLocation(1, 1)
                );

            TestOptions.ReleaseDll.WithModuleName("a\0a").VerifyErrors(
                // error CS7087: Invalid module name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadModuleName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );

            TestOptions.ReleaseDll.WithModuleName("a\uD800b").VerifyErrors(
                // error CS7087: Invalid module name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadModuleName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );

            TestOptions.ReleaseDll.WithModuleName("a\\b").VerifyErrors(
                // error CS7087: Invalid module name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadModuleName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );

            TestOptions.ReleaseDll.WithModuleName("a/b").VerifyErrors(
                // error CS7087: Invalid module name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadModuleName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );

            TestOptions.ReleaseDll.WithModuleName("a:b").VerifyErrors(
                // error CS7087: Invalid module name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadModuleName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
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

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, usings: new string[] { "blah\0goo" }).VerifyErrors(
                // error CS7088: Invalid 'Usings' value: 'blah\0goo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", "blah\0goo"));

            Assert.Equal("Script", new CSharpCompilationOptions(OutputKind.ConsoleApplication, scriptClassName: null).ScriptClassName);

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, scriptClassName: "blah\0goo").VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: 'blah\0goo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", "blah\0goo"));

            new CSharpCompilationOptions(OutputKind.ConsoleApplication, scriptClassName: "").VerifyErrors(
                // error CS7088: Invalid 'ScriptClassName' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("ScriptClassName", ""));

            Assert.Equal(0, new CSharpCompilationOptions(OutputKind.ConsoleApplication, mainTypeName: null).Errors.Length);
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, mainTypeName: "blah\0goo").VerifyErrors(
                // error CS7088: Invalid 'MainTypeName' value: 'blah\0goo'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MainTypeName", "blah\0goo"));

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
                "Language",
                "AllowUnsafe",
                "Usings",
                "TopLevelBinderFlags",
                "NullableContextOptions");
        }

        [Fact]
        public void TestEqualitySemantics()
        {
            CSharpCompilationOptions first = CreateCSharpCompilationOptions();
            CSharpCompilationOptions second = CreateCSharpCompilationOptions();
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
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
            DateTime currentLocalTime = default(DateTime);
            bool debugPlusMode = false;
            XmlReferenceResolver xmlReferenceResolver = new XmlFileResolver(null);
            SourceReferenceResolver sourceReferenceResolver = new SourceFileResolver(ImmutableArray<string>.Empty, null);
            MetadataReferenceResolver metadataReferenceResolver = new MetadataReferenceResolverWithEquality();
            AssemblyIdentityComparer assemblyIdentityComparer = AssemblyIdentityComparer.Default;           // Currently uses reference equality
            StrongNameProvider strongNameProvider = new DesktopStrongNameProvider();
            MetadataImportOptions metadataImportOptions = 0;
            bool referencesSupersedeLowerVersions = false;
            bool reportSuppressedDiagnostics = false;
            var topLevelBinderFlags = BinderFlags.None;
            var publicSign = false;
            NullableContextOptions nullableContextOptions = NullableContextOptions.Disable;

            return new CSharpCompilationOptions(OutputKind.ConsoleApplication, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName, usings,
                optimizationLevel, checkOverflow, allowUnsafe, cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign,
                platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions,
                concurrentBuild, deterministic, currentLocalTime, debugPlusMode, xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver,
                assemblyIdentityComparer, strongNameProvider, metadataImportOptions, referencesSupersedeLowerVersions, publicSign, topLevelBinderFlags, nullableContextOptions);
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

        [Fact]
        public void WithNullable()
        {
            Assert.Equal(NullableContextOptions.Disable, new CSharpCompilationOptions(OutputKind.ConsoleApplication).NullableContextOptions);

            var values = (NullableContextOptions[])System.Enum.GetValues(typeof(NullableContextOptions));
            var options = new CSharpCompilationOptions[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                options[i] = new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: values[i]);
                Assert.Equal(values[i], options[i].NullableContextOptions);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var oldOptions = options[i];

                for (int j = 0; j < values.Length; j++)
                {
                    var newOptions = oldOptions.WithNullableContextOptions(values[j]);
                    Assert.Equal(values[j], newOptions.NullableContextOptions);
                    Assert.Equal(options[j], newOptions);
                    Assert.Equal(options[j].GetHashCode(), newOptions.GetHashCode());

                    if (i == j)
                    {
                        Assert.Same(oldOptions, newOptions);
                    }
                    else
                    {
                        Assert.NotSame(oldOptions, newOptions);
                        Assert.NotEqual(oldOptions, newOptions);
                    }
                }
            }
        }
    }
}
