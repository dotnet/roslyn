// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Resources;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public partial class DiagnosticLocalizationTests
    {
        [Fact, WorkItem(1006, "https://github.com/dotnet/roslyn/issues/1006")]
        public void TestDiagnosticLocalization()
        {
            var resourceManager = GetTestResourceManagerInstance();
            var arCulture = CultureInfo.CreateSpecificCulture("ar-SA");
            var defaultCultureResourceSet = resourceManager.GetResourceSet(CustomResourceManager.DefaultCulture, false, false);
            var arResourceSet = resourceManager.GetResourceSet(arCulture, false, false);

            var nameOfResource1 = @"Resource1";
            var nameOfResource2 = @"Resource2";
            var nameOfResource3 = @"Resource3";

            var fixedTitle = defaultCultureResourceSet.GetString(nameOfResource1);
            var fixedMessageFormat = defaultCultureResourceSet.GetString(nameOfResource2);
            var fixedDescription = defaultCultureResourceSet.GetString(nameOfResource3);

            var localizedTitle = arResourceSet.GetString(nameOfResource1);
            var localizedMessageFormat = arResourceSet.GetString(nameOfResource2);
            var localizedDescription = arResourceSet.GetString(nameOfResource3);

            // Test descriptor localization.

            // Test non-localizable title, description and message.
            var descriptor = new DiagnosticDescriptor(
                "Id",
                fixedTitle,
                fixedMessageFormat,
                "Category",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: fixedDescription);

            Assert.Equal(fixedTitle, descriptor.Title.ToString(arCulture));
            Assert.Equal(fixedMessageFormat, descriptor.MessageFormat.ToString(arCulture));
            Assert.Equal(fixedDescription, descriptor.Description.ToString(arCulture));

            // Test localizable title, description and message.
            var localizableTitle = new LocalizableResourceString(nameOfResource1, resourceManager, typeof(CustomResourceManager));
            var localizableMessageFormat = new LocalizableResourceString(nameOfResource2, resourceManager, typeof(CustomResourceManager));
            var localizableDescription = new LocalizableResourceString(nameOfResource3, resourceManager, typeof(CustomResourceManager));

            descriptor = new DiagnosticDescriptor(
                "Id",
                localizableTitle,
                localizableMessageFormat,
                "Category",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: localizableDescription);

            if (EnsureEnglishUICulture.PreferredOrNull == null)
            {
                Assert.Equal<string>(fixedTitle, descriptor.Title.ToString());
                Assert.Equal<string>(fixedMessageFormat, descriptor.MessageFormat.ToString());
                Assert.Equal<string>(fixedDescription, descriptor.Description.ToString());
            }

            Assert.Equal<string>(fixedTitle, descriptor.Title.ToString(CustomResourceManager.DefaultCulture));
            Assert.Equal<string>(fixedMessageFormat, descriptor.MessageFormat.ToString(CustomResourceManager.DefaultCulture));
            Assert.Equal<string>(fixedDescription, descriptor.Description.ToString(CustomResourceManager.DefaultCulture));

            Assert.Equal(localizedTitle, descriptor.Title.ToString(arCulture));
            Assert.Equal(localizedMessageFormat, descriptor.MessageFormat.ToString(arCulture));
            Assert.Equal(localizedDescription, descriptor.Description.ToString(arCulture));

            // Test diagnostic localization.
            var localizableDiagnostic = Diagnostic.Create(descriptor, Location.None);

            if (EnsureEnglishUICulture.PreferredOrNull == null)
            {
                // Test non-localized title, description and message.
                Assert.Equal(fixedTitle, localizableDiagnostic.Descriptor.Title.ToString());
                Assert.Equal(fixedMessageFormat, localizableDiagnostic.GetMessage());
                Assert.Equal(fixedDescription, localizableDiagnostic.Descriptor.Description.ToString());
            }

            Assert.Equal(fixedTitle, localizableDiagnostic.Descriptor.Title.ToString(CustomResourceManager.DefaultCulture));
            Assert.Equal(fixedMessageFormat, localizableDiagnostic.GetMessage(CustomResourceManager.DefaultCulture));
            Assert.Equal(fixedDescription, localizableDiagnostic.Descriptor.Description.ToString(CustomResourceManager.DefaultCulture));

            // Test localized title, description and message.
            Assert.Equal(localizedTitle, localizableDiagnostic.Descriptor.Title.ToString(arCulture));
            Assert.Equal(localizedMessageFormat, localizableDiagnostic.GetMessage(arCulture));
            Assert.Equal(localizedDescription, localizableDiagnostic.Descriptor.Description.ToString(arCulture));

            // Test argument formatting for localized string
            var nameOfResourceWithArguments = @"ResourceWithArguments";
            var argument = "formatted";
            var localizableResource = new LocalizableResourceString(nameOfResourceWithArguments, resourceManager, typeof(CustomResourceManager), argument);

            // Verify without culture
            var defaultCultureLocalizedStringWithArguments = defaultCultureResourceSet.GetString(nameOfResourceWithArguments);
            var expected = string.Format(defaultCultureLocalizedStringWithArguments, argument);

            if (EnsureEnglishUICulture.PreferredOrNull == null)
            {
                Assert.Equal(expected, localizableResource.ToString());
            }

            Assert.Equal(expected, localizableResource.ToString(CustomResourceManager.DefaultCulture));

            // Verify with loc culture
            var arLocalizedStringWithArguments = arResourceSet.GetString(nameOfResourceWithArguments);
            expected = string.Format(arLocalizedStringWithArguments, argument);
            Assert.Equal(expected, localizableResource.ToString(arCulture));
        }

        private static CustomResourceManager GetTestResourceManagerInstance()
        {
            var defaultCultureResources = new Dictionary<string, string>()
                {
                    { "Resource1", "My Resource 1 DefaultCulture string" },
                    { "Resource2", "My Resource 2 DefaultCulture string" },
                    { "Resource3", "My Resource 3 DefaultCulture string" },
                    { "ResourceWithArguments", "My Resource DefaultCulture string {0}" }
                };

            var arResources = new Dictionary<string, string>()
                {
                    { "Resource1", "ARABIC string for My Resource 1" },
                    { "Resource2", "ARABIC string for My Resource 2" },
                    { "Resource3", "ARABIC string for My Resource 3" },
                    { "ResourceWithArguments", "{0} ARABIC string for My Resource" }
                };

            var resourceSetMap = new Dictionary<string, Dictionary<string, string>>()
                {
                    { CustomResourceManager.DefaultCulture.Name, defaultCultureResources },
                    { "ar-SA", arResources }
                };

            return new CustomResourceManager(resourceSetMap);
        }

        private class CustomResourceManager : ResourceManager
        {
            private readonly Dictionary<string, CustomResourceSet> _resourceSetMap;

            public CustomResourceManager(Dictionary<string, CustomResourceSet> resourceSetMap)
            {
                _resourceSetMap = resourceSetMap;
            }

            public CustomResourceManager(Dictionary<string, Dictionary<string, string>> resourceSetMap)
            {
                _resourceSetMap = new Dictionary<string, CustomResourceSet>();

                foreach (var kvp in resourceSetMap)
                {
                    var resourceSet = new CustomResourceSet(kvp.Value);
                    _resourceSetMap.Add(kvp.Key, resourceSet);
                }
            }

            public static CultureInfo DefaultCulture => CultureInfo.CurrentUICulture;

            public void VerifyResourceValue(string resourceName, string cultureName, string expectedResourceValue)
            {
                var actual = this.GetString(resourceName, CultureInfo.CreateSpecificCulture(cultureName));
                Assert.Equal(expectedResourceValue, actual);
            }

            public override string GetString(string name, CultureInfo culture)
            {
                return GetResourceSet(culture, false, false).GetString(name);
            }

            public override ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
            {
                return _resourceSetMap[culture.Name];
            }

            public override string GetString(string name)
            {
                return GetString(name, CultureInfo.InvariantCulture);
            }

            public override object GetObject(string name)
            {
                return GetString(name);
            }

            public override object GetObject(string name, CultureInfo culture)
            {
                return GetString(name, culture);
            }

            public class CustomResourceSet : ResourceSet
            {
                private readonly Dictionary<string, string> _resourcesMap;
                public CustomResourceSet(Dictionary<string, string> resourcesMap)
                {
                    _resourcesMap = resourcesMap;
                }

                public override string GetString(string name)
                {
                    return _resourcesMap[name];
                }

                public override string GetString(string name, bool ignoreCase)
                {
                    throw new NotImplementedException();
                }

                public override object GetObject(string name)
                {
                    return GetString(name);
                }

                public override object GetObject(string name, bool ignoreCase)
                {
                    throw new NotImplementedException();
                }
            }
        }

        [Fact]
        public void LocalizableResourceStringEquality()
        {
            var resourceManager = GetTestResourceManagerInstance();
            var unit = EqualityUnit
                .Create(new LocalizableResourceString(@"ResourceWithArguments", resourceManager, typeof(CustomResourceManager), "arg"))
                .WithEqualValues(
                    new LocalizableResourceString(@"ResourceWithArguments", resourceManager, typeof(CustomResourceManager), "arg"))
                .WithNotEqualValues(
                    new LocalizableResourceString(@"ResourceWithArguments", resourceManager, typeof(CustomResourceManager), "otherarg"),
                    new LocalizableResourceString(@"Resource1", resourceManager, typeof(CustomResourceManager)));
            EqualityUtil.RunAll(unit, checkIEquatable: false);

            var str = new LocalizableResourceString(@"ResourceWithArguments", resourceManager, typeof(CustomResourceManager), "arg");
            var threw = false;
            str.OnException += (sender, e) => { threw = true; };
            Assert.False(str.Equals(42));
            Assert.False(str.Equals(42));
            Assert.False(threw);
        }

        [Fact, WorkItem(887, "https://github.com/dotnet/roslyn/issues/887")]
        public void TestDescriptorIsExceptionSafe()
        {
            // Test descriptor with LocalizableResourceString fields that can throw.
            var descriptor1 = GetDescriptorWithLocalizableResourceStringsThatThrow();
            TestDescriptorIsExceptionSafeCore(descriptor1);

            // Test descriptor with Custom implemented LocalizableString fields that can throw.
            var descriptor2 = GetDescriptorWithCustomLocalizableStringsThatThrow();
            TestDescriptorIsExceptionSafeCore(descriptor2);

            // Also verify exceptions from Equals and GetHashCode don't go unhandled.
            var unused1 = descriptor2.Title.GetHashCode();
            var unused2 = descriptor2.Equals(descriptor1);
        }

        private static void TestDescriptorIsExceptionSafeCore(DiagnosticDescriptor descriptor)
        {
            var localizableTitle = descriptor.Title;
            var localizableMessage = descriptor.MessageFormat;
            var localizableDescription = descriptor.Description;

            // Verify exceptions from LocalizableResourceString don't go unhandled.
            var title = localizableTitle.ToString();
            var message = localizableMessage.ToString();
            var description = localizableDescription.ToString();

            // Verify exceptions from LocalizableResourceString are raised if OnException is set.
            var exceptions = new List<Exception>();
            var handler = new EventHandler<Exception>((sender, ex) => exceptions.Add(ex));
            localizableTitle.OnException += handler;
            localizableMessage.OnException += handler;
            localizableDescription.OnException += handler;

            // Access and evaluate localizable fields.
            var unused1 = localizableTitle.ToString();
            var unused2 = localizableMessage.ToString();
            var unused3 = localizableDescription.ToString();

            Assert.Equal(3, exceptions.Count);

            // Verify DiagnosticAnalyzer.SupportedDiagnostics is also exception safe.
            var analyzer = new MyAnalyzer(descriptor);
            var exceptionDiagnostics = new List<Diagnostic>();

            Action<Exception, DiagnosticAnalyzer, Diagnostic, CancellationToken> onAnalyzerException = (ex, a, diag, ct) => exceptionDiagnostics.Add(diag);
            var analyzerManager = new AnalyzerManager(analyzer);
            var compilation = CSharp.CSharpCompilation.Create("test");
            var analyzerExecutor = AnalyzerExecutor.Create(
                compilation,
                AnalyzerOptions.Empty,
                addNonCategorizedDiagnostic: (_, _, _) => { },
                onAnalyzerException,
                analyzerExceptionFilter: null,
                isCompilerAnalyzer: _ => false,
                diagnosticAnalyzers: [analyzer],
                getAnalyzerConfigOptionsProvider: null,
                analyzerManager,
                shouldSkipAnalysisOnGeneratedCode: _ => false,
                shouldSuppressGeneratedCodeDiagnostic: (_, _, _, _) => false,
                isGeneratedCodeLocation: (_, _, _) => false,
                isAnalyzerSuppressedForTree: (_, _, _, _) => false,
                getAnalyzerGate: _ => null,
                getSemanticModel: tree => compilation.GetSemanticModel(tree, ignoreAccessibility: true),
                SeverityFilter.None);
            var descriptors = analyzerManager.GetSupportedDiagnosticDescriptors(analyzer, analyzerExecutor, CancellationToken.None);

            Assert.Equal(1, descriptors.Length);
            Assert.Equal(descriptor.Id, descriptors[0].Id);

            // Access and evaluate localizable fields.
            unused1 = descriptors[0].Title.ToString();
            unused2 = descriptors[0].MessageFormat.ToString();
            unused3 = descriptors[0].Description.ToString();

            // Verify logged analyzer exception diagnostics.
            Assert.Equal(3, exceptionDiagnostics.Count);
            Assert.True(exceptionDiagnostics.TrueForAll(AnalyzerExecutor.IsAnalyzerExceptionDiagnostic));
        }

        private static DiagnosticDescriptor GetDescriptorWithLocalizableResourceStringsThatThrow()
        {
            var resourceManager = GetTestResourceManagerInstance();

            // Test localizable title that throws.
            var localizableTitle = new LocalizableResourceString("NonExistentTitleResourceName", resourceManager, typeof(CustomResourceManager));
            var localizableMessage = new LocalizableResourceString("NonExistentMessageResourceName", resourceManager, typeof(CustomResourceManager));
            var localizableDescription = new LocalizableResourceString("NonExistentDescriptionResourceName", resourceManager, typeof(CustomResourceManager));

            return new DiagnosticDescriptor(
                "Id",
                localizableTitle,
                localizableMessage,
                "Category",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: localizableDescription);
        }

        private static DiagnosticDescriptor GetDescriptorWithCustomLocalizableStringsThatThrow()
        {
            // Test localizable title that throws.
            var localizableTitle = new ThrowingLocalizableString();
            var localizableMessage = new ThrowingLocalizableString();
            var localizableDescription = new ThrowingLocalizableString();

            return new DiagnosticDescriptor(
                "Id",
                localizableTitle,
                localizableMessage,
                "Category",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: localizableDescription);
        }

        private class MyAnalyzer : DiagnosticAnalyzer
        {
            private readonly DiagnosticDescriptor _descriptor;

            public MyAnalyzer(DiagnosticDescriptor descriptor)
            {
                _descriptor = descriptor;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(_descriptor);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
            }
        }

        private class ThrowingLocalizableString : LocalizableString
        {
            protected override bool AreEqual(object other)
            {
                throw new NotImplementedException();
            }

            protected override int GetHash()
            {
                throw new NotImplementedException();
            }

            protected override string GetText(IFormatProvider formatProvider)
            {
                throw new NotImplementedException();
            }
        }
    }
}
