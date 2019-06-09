// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.MetadataReferences
{
    public class FusionAssemblyIdentityTests
    {
        /// <summary>
        /// Converts <see cref="FusionAssemblyIdentity.IAssemblyName"/> to <see cref="AssemblyName"/> with possibly
        /// missing name components.
        /// </summary>
        /// <returns>
        /// An <see cref="AssemblyName"/> whose fields are be null if not present in <paramref name="nameObject"/>.
        /// </returns>
        internal static AssemblyName ToAssemblyName(FusionAssemblyIdentity.IAssemblyName nameObject)
        {
            var result = new AssemblyName();
            result.Name = FusionAssemblyIdentity.GetName(nameObject);
            result.Version = FusionAssemblyIdentity.GetVersion(nameObject);

            var cultureName = FusionAssemblyIdentity.GetCulture(nameObject);
            result.CultureInfo = (cultureName != null) ? new CultureInfo(cultureName) : null;

            byte[] publicKey = FusionAssemblyIdentity.GetPublicKey(nameObject);
            if (publicKey != null && publicKey.Length != 0)
            {
                result.SetPublicKey(publicKey);
            }
            else
            {
                result.SetPublicKeyToken(FusionAssemblyIdentity.GetPublicKeyToken(nameObject));
            }

            result.Flags = FusionAssemblyIdentity.GetFlags(nameObject);
            result.ContentType = FusionAssemblyIdentity.GetContentType(nameObject);
            return result;
        }

        private void RoundTrip(AssemblyName name, bool testFullName = true)
        {
            AssemblyName rtName;
            FusionAssemblyIdentity.IAssemblyName obj;

            if (testFullName)
            {
                string fullName = name.FullName;

                obj = FusionAssemblyIdentity.ToAssemblyNameObject(fullName);
                rtName = ToAssemblyName(obj);
                Assert.Equal(name.Name, rtName.Name);
                Assert.Equal(name.Version, rtName.Version);
                Assert.Equal(name.CultureInfo, rtName.CultureInfo);
                Assert.Equal(name.GetPublicKeyToken(), rtName.GetPublicKeyToken());
                Assert.Equal(name.Flags, rtName.Flags);
                Assert.Equal(name.ContentType, rtName.ContentType);

                string displayName = FusionAssemblyIdentity.GetDisplayName(obj, FusionAssemblyIdentity.ASM_DISPLAYF.FULL);
                Assert.Equal(fullName, displayName);
            }

            obj = FusionAssemblyIdentity.ToAssemblyNameObject(name);
            rtName = ToAssemblyName(obj);
            Assert.Equal(name.Name, rtName.Name);
            Assert.Equal(name.Version, rtName.Version);
            Assert.Equal(name.CultureInfo, rtName.CultureInfo);
            Assert.Equal(name.GetPublicKeyToken(), rtName.GetPublicKeyToken());
            Assert.Equal(name.Flags, rtName.Flags);
            Assert.Equal(name.ContentType, rtName.ContentType);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void FusionAssemblyNameRoundTrip()
        {
            RoundTrip(new AssemblyName("goo"));
            RoundTrip(new AssemblyName { Name = "~!@#$%^&*()_+={}:\"<>?[];',./" });
            RoundTrip(new AssemblyName("\\,"));
            RoundTrip(new AssemblyName("\\\""));

            RoundTrip(new AssemblyIdentity("goo").ToAssemblyName());

            // 0xffff version is not included in AssemblyName.FullName for some reason:
            var name = new AssemblyIdentity("goo", version: new Version(0xffff, 0xffff, 0xffff, 0xffff)).ToAssemblyName();
            RoundTrip(name, testFullName: false);
            var obj = FusionAssemblyIdentity.ToAssemblyNameObject(name);
            var display = FusionAssemblyIdentity.GetDisplayName(obj, FusionAssemblyIdentity.ASM_DISPLAYF.FULL);
            Assert.Equal("goo, Version=65535.65535.65535.65535, Culture=neutral, PublicKeyToken=null", display);

            RoundTrip(new AssemblyIdentity("goo", version: new Version(1, 2, 3, 4)).ToAssemblyName());
            RoundTrip(new AssemblyName("goo") { Version = new Version(1, 2, 3, 4) });

            RoundTrip(new AssemblyIdentity("goo", cultureName: CultureInfo.CurrentCulture.Name).ToAssemblyName());
            RoundTrip(new AssemblyIdentity("goo", cultureName: "").ToAssemblyName());
            RoundTrip(new AssemblyName("goo") { CultureInfo = CultureInfo.InvariantCulture });

            RoundTrip(new AssemblyIdentity("goo", version: new Version(1, 2, 3, 4), cultureName: "en-US").ToAssemblyName());
            RoundTrip(new AssemblyIdentity("goo", publicKeyOrToken: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }.AsImmutableOrNull()).ToAssemblyName());
            RoundTrip(new AssemblyIdentity("goo", version: new Version(1, 2, 3, 4), cultureName: CultureInfo.CurrentCulture.Name, publicKeyOrToken: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }.AsImmutableOrNull()).ToAssemblyName());

            RoundTrip(new AssemblyIdentity("goo", isRetargetable: true).ToAssemblyName());
            RoundTrip(new AssemblyIdentity("goo", contentType: AssemblyContentType.WindowsRuntime).ToAssemblyName());
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void FusionGetBestMatch()
        {
            var goo = FusionAssemblyIdentity.ToAssemblyNameObject("goo");
            var goo1 = FusionAssemblyIdentity.ToAssemblyNameObject("goo, Version=1.0.0.0, Culture=neutral");
            var goo2 = FusionAssemblyIdentity.ToAssemblyNameObject("goo, Version=2.0.0.0, Culture=neutral");
            var goo3 = FusionAssemblyIdentity.ToAssemblyNameObject("goo, Version=3.0.0.0, Culture=neutral");
            var goo3_enUS = FusionAssemblyIdentity.ToAssemblyNameObject("goo, Version=3.0.0.0, Culture=en-US");
            var goo3_deDE = FusionAssemblyIdentity.ToAssemblyNameObject("goo, Version=3.0.0.0, Culture=de-DE");

            var m = FusionAssemblyIdentity.GetBestMatch(new[] { goo2, goo1, goo3 }, null);
            Assert.Equal(goo3, m);

            m = FusionAssemblyIdentity.GetBestMatch(new[] { goo3, goo2, goo1 }, null);
            Assert.Equal(goo3, m);

            // only simple name is used 
            m = FusionAssemblyIdentity.GetBestMatch(new[] { goo2, goo3 }, null);
            Assert.Equal(goo3, m);

            // the first match if preferred cultures not specified 
            m = FusionAssemblyIdentity.GetBestMatch(new[] { goo1, goo3_deDE, goo3_enUS, goo2 }, null);
            Assert.Equal(goo3_deDE, m);

            // the first match if preferred cultures not specified 
            m = FusionAssemblyIdentity.GetBestMatch(new[] { goo1, goo3_deDE, goo3_enUS, goo2 }, null);
            Assert.Equal(goo3_deDE, m);

            m = FusionAssemblyIdentity.GetBestMatch(new[] { goo1, goo3, goo3_deDE, goo3_enUS, goo2 }, "en-US");
            Assert.Equal(goo3_enUS, m);

            m = FusionAssemblyIdentity.GetBestMatch(new[] { goo1, goo3_deDE, goo3, goo3_enUS, goo2 }, "cz-CZ");
            Assert.Equal(goo3, m);

            m = FusionAssemblyIdentity.GetBestMatch(new[] { goo3_deDE, goo2 }, "en-US");
            Assert.Equal(goo3_deDE, m);

            // neutral culture wins over specific non-matching one:
            m = FusionAssemblyIdentity.GetBestMatch(new[] { goo3_deDE, goo3, goo2 }, "en-US");
            Assert.Equal(goo3, m);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void FusionToAssemblyName()
        {
            var nameObject = FusionAssemblyIdentity.ToAssemblyNameObject("mscorlib");
            var name = ToAssemblyName(nameObject);

            Assert.Equal("mscorlib", name.Name);
            Assert.Null(name.Version);
            Assert.Null(name.CultureInfo);
            Assert.Null(name.GetPublicKey());
            Assert.Null(name.GetPublicKeyToken());
            Assert.Equal(AssemblyContentType.Default, name.ContentType);

            nameObject = FusionAssemblyIdentity.ToAssemblyNameObject("mscorlib, Version=2.0.0.0");
            name = ToAssemblyName(nameObject);
            Assert.Equal("mscorlib", name.Name);
            Assert.Equal(new Version(2, 0, 0, 0), name.Version);
            Assert.Null(name.CultureInfo);
            Assert.Null(name.GetPublicKey());
            Assert.Null(name.GetPublicKeyToken());
            Assert.Equal(AssemblyContentType.Default, name.ContentType);

            nameObject = FusionAssemblyIdentity.ToAssemblyNameObject("mscorlib, Version=2.0.0.0, Culture=neutral");
            name = ToAssemblyName(nameObject);
            Assert.Equal("mscorlib", name.Name);
            Assert.Equal(new Version(2, 0, 0, 0), name.Version);
            Assert.Equal(name.CultureInfo, CultureInfo.InvariantCulture);
            Assert.Null(name.GetPublicKey());
            Assert.Null(name.GetPublicKeyToken());
            Assert.Equal(AssemblyContentType.Default, name.ContentType);

            nameObject = FusionAssemblyIdentity.ToAssemblyNameObject("mscorlib, Version=2.0.0.0, Culture=en-US");
            name = ToAssemblyName(nameObject);
            Assert.Equal("mscorlib", name.Name);
            Assert.Equal(new Version(2, 0, 0, 0), name.Version);
            Assert.NotNull(name.CultureInfo);
            Assert.Equal("en-US", name.CultureInfo.Name);
            Assert.Null(name.GetPublicKey());
            Assert.Null(name.GetPublicKeyToken());
            Assert.Equal(AssemblyContentType.Default, name.ContentType);

            nameObject = FusionAssemblyIdentity.ToAssemblyNameObject("Windows, Version=255.255.255.255, ContentType=WindowsRuntime");
            name = ToAssemblyName(nameObject);
            Assert.Equal("Windows", name.Name);
            Assert.Equal(new Version(255, 255, 255, 255), name.Version);
            Assert.Null(name.CultureInfo);
            Assert.Null(name.GetPublicKey());
            Assert.Null(name.GetPublicKeyToken());
            Assert.Equal(AssemblyContentType.WindowsRuntime, name.ContentType);

            nameObject = FusionAssemblyIdentity.ToAssemblyNameObject("mscorlib, Version=2.0.0.0, Culture=nonsense");
            Assert.NotNull(nameObject);
            Assert.Throws<CultureNotFoundException>(() => ToAssemblyName(nameObject));

            nameObject = FusionAssemblyIdentity.ToAssemblyNameObject("mscorlib, Version=2.0.0.0, Culture=null");
            Assert.NotNull(nameObject);
            Assert.Throws<CultureNotFoundException>(() => ToAssemblyName(nameObject));

            Assert.Throws<CultureNotFoundException>(() => new AssemblyName("mscorlib, Version=2.0.0.0, Culture=nonsense"));
            Assert.Throws<CultureNotFoundException>(() => new AssemblyName("mscorlib, Version=2.0.0.0, Culture=null"));

            Assert.Throws<ArgumentException>(() => FusionAssemblyIdentity.ToAssemblyNameObject(new AssemblyName { Name = "x\0x" }));

            // invalid characters are ok in the name, the FullName can't be built though:
            foreach (char c in AssemblyIdentityTests.ClrInvalidCharacters)
            {
                nameObject = FusionAssemblyIdentity.ToAssemblyNameObject(new AssemblyName { Name = c.ToString() });
                name = ToAssemblyName(nameObject);
                Assert.Equal(c.ToString(), name.Name);
                Assert.Throws<FileLoadException>(() => name.FullName);
            }
        }
    }
}

