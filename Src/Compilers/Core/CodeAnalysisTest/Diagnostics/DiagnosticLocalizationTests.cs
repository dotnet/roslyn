// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;
using System.Resources;
using System.Globalization;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public partial class DiagnosticLocalizationTests
    {
        [Fact]
        public void TestDiagnosticLocalization()
        {
            var resourceManager = GetTestResourceManagerInstance();
            var enCulture = CultureInfo.CreateSpecificCulture("en-US");
            var arCulture = CultureInfo.CreateSpecificCulture("ar-SA");
            var enResourceSet = resourceManager.GetResourceSet(enCulture, false, false);
            var arResourceSet = resourceManager.GetResourceSet(arCulture, false, false);

            var nameOfResource1 = @"Resource1";
            var nameOfResource2 = @"Resource2";
            var nameOfResource3 = @"Resource3";
            
            var fixedTitle = enResourceSet.GetString(nameOfResource1);
            var fixedMessageFormat = enResourceSet.GetString(nameOfResource2);
            var fixedDescription = enResourceSet.GetString(nameOfResource3);

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

            Assert.Equal<string>(fixedTitle, descriptor.Title.ToString());
            Assert.Equal<string>(fixedMessageFormat, descriptor.MessageFormat.ToString());
            Assert.Equal<string>(fixedDescription, descriptor.Description.ToString());

            Assert.Equal(localizedTitle, descriptor.Title.ToString(arCulture));
            Assert.Equal(localizedMessageFormat, descriptor.MessageFormat.ToString(arCulture));
            Assert.Equal(localizedDescription, descriptor.Description.ToString(arCulture));

            // Test diagnostic localization.
            var localizableDiagnostic = Diagnostic.Create(descriptor, Location.None);

            // Test non-localized title, description and message.
            Assert.Equal(fixedTitle, localizableDiagnostic.Descriptor.Title.ToString());
            Assert.Equal(fixedMessageFormat, localizableDiagnostic.GetMessage());
            Assert.Equal(fixedDescription, localizableDiagnostic.Descriptor.Description.ToString());

            // Test localized title, description and message.
            Assert.Equal(localizedTitle, localizableDiagnostic.Descriptor.Title.ToString(arCulture));
            Assert.Equal(localizedMessageFormat, localizableDiagnostic.GetMessage(arCulture));
            Assert.Equal(localizedDescription, localizableDiagnostic.Descriptor.Description.ToString(arCulture));

            // Test argument formatting for localized string
            var nameOfResourceWithArguments = @"ResourceWithArguments";
            var argument = "formatted";
            var localizableResource = new LocalizableResourceString(nameOfResourceWithArguments, resourceManager, typeof(CustomResourceManager), argument);

            // Verify without culture
            var enuLocalizedStringWithArguments = enResourceSet.GetString(nameOfResourceWithArguments);
            var expected = string.Format(enuLocalizedStringWithArguments, argument);
            Assert.Equal(expected, localizableResource.ToString());

            // Verify with loc culture
            var arLocalizedStringWithArguments = arResourceSet.GetString(nameOfResourceWithArguments);
            expected = string.Format(arLocalizedStringWithArguments, argument);
            Assert.Equal(expected, localizableResource.ToString(arCulture));
        }

        private static CustomResourceManager GetTestResourceManagerInstance()
        {
            var enResources = new Dictionary<string, string>()
                {
                    { "Resource1", "My Resource 1 ENU string" },
                    { "Resource2", "My Resource 2 ENU string" },
                    { "Resource3", "My Resource 3 ENU string" },
                    { "ResourceWithArguments", "My Resource ENU string {0}" }
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
                    { "en-US", enResources },
                    { "ar-SA", arResources }
                };

            return new CustomResourceManager(resourceSetMap);
        }

        private class CustomResourceManager : ResourceManager
        {
            private readonly Dictionary<string, CustomResourceSet> resourceSetMap;
            internal static readonly CustomResourceManager TestInstance = GetTestResourceManagerInstance();

            public CustomResourceManager(Dictionary<string, CustomResourceSet> resourceSetMap)
            {
                this.resourceSetMap = resourceSetMap;
            }

            public CustomResourceManager(Dictionary<string, Dictionary<string, string>> resourceSetMap)
            {
                this.resourceSetMap = new Dictionary<string, CustomResourceSet>();

                foreach (var kvp in resourceSetMap)
                {
                    var resourceSet = new CustomResourceSet(kvp.Value);
                    this.resourceSetMap.Add(kvp.Key, resourceSet);
                }
            }

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
                return resourceSetMap[culture.Name];
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
                private readonly Dictionary<string, string> resourcesMap;
                public CustomResourceSet(Dictionary<string, string> resourcesMap)
                {
                    this.resourcesMap = resourcesMap;
                }

                public override string GetString(string name)
                {
                    return resourcesMap[name];
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
    }
}
