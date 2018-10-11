// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyPortabilityPolicyTests : TestBase
    {
        private const string correctAppConfigText = @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/> <!-- platform -->
    </assemblyBinding>
  </runtime>
</configuration>
";
        private static void AssertIsEnabled(string appConfigPath, bool platform, bool nonPlatform, bool fusionOnly = false)
        {
            using (var policy = FusionAssemblyPortabilityPolicy.LoadFromFile(appConfigPath))
            {
                // portability is suppressed if the identities are not equivalent

                Assert.Equal(platform, IsEquivalent(policy,
                    "System, Version=5.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                    "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));

                Assert.Equal(nonPlatform, IsEquivalent(policy,
                    "System.ComponentModel.Composition, Version=5.0.5.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                    "System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
            }

            if (!fusionOnly)
            {
                using (var stream = new FileStream(appConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var actual = AssemblyPortabilityPolicy.LoadFromXml(stream);
                    Assert.Equal(platform, !actual.SuppressSilverlightPlatformAssembliesPortability);
                    Assert.Equal(nonPlatform, !actual.SuppressSilverlightLibraryAssembliesPortability);
                }
            }
        }

        private static bool IsEquivalent(FusionAssemblyPortabilityPolicy policy, string reference, string ported)
        {
            bool equivalent;
            FusionAssemblyIdentityComparer.AssemblyComparisonResult result;

            int hr = FusionAssemblyIdentityComparer.DefaultModelCompareAssemblyIdentity(
                reference,
                false,
                ported,
                false,
                out equivalent,
                out result,
                policy.ConfigCookie);

            return result == FusionAssemblyIdentityComparer.AssemblyComparisonResult.EquivalentFullMatch;
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_Errors()
        {
            var appConfig = Temp.CreateFile();
            var stream = new FileStream(appConfig.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // not XML:
            appConfig.WriteAllText("garbage");
            stream.Position = 0;
            Assert.Throws<COMException>(() => FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path));
            Assert.Throws<XmlException>(() => AssemblyPortabilityPolicy.LoadFromXml(stream));

            // missing root element:
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
");
            stream.Position = 0;

            Assert.Throws<COMException>(() => FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path));
            Assert.Throws<XmlException>(() => AssemblyPortabilityPolicy.LoadFromXml(stream));

            // duplicate attribute:
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" PKT=""31bf3856ad364e35"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>
");
            stream.Position = 0;

            Assert.Throws<COMException>(() => FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path));
            Assert.Throws<XmlException>(() => AssemblyPortabilityPolicy.LoadFromXml(stream));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_LeadingWhitespace()
        {
            var appConfig = Temp.CreateFile();

            // whitespace in front of header:
            appConfig.WriteAllText(
@"   <?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/> <!-- platform -->
    </assemblyBinding>
  </runtime>
</configuration>
");
            var stream = new FileStream(appConfig.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            AssertIsEnabled(appConfig.Path, platform: false, nonPlatform: true, fusionOnly: true);
            Assert.Throws<XmlException>(() => AssemblyPortabilityPolicy.LoadFromXml(stream));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_LinkedConfiguration()
        {
            var appConfig1 = Temp.CreateFile();
            var appConfig2 = Temp.CreateFile();

            appConfig1.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
        <linkedConfiguration href=""file://" + appConfig2.Path + @"""/>       
        <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/> <!-- platform -->
    </assemblyBinding>
  </runtime>
</configuration>
");

            appConfig2.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""31bf3856ad364e35"" enable=""false""/> <!-- non-platform -->
    </assemblyBinding>
  </runtime>
</configuration>
");

            // Linked configuration isn't supported by fusion when reading portability elements (see CreateAssemblyConfigCookie)
            AssertIsEnabled(appConfig1.Path, platform: false, nonPlatform: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_NoValues()
        {
            var appConfig = Temp.CreateFile();

            // ok, but no configuration:
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<goo>
</goo>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: true);

            // ok, but no runtime
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: true);

            // ok, but no assembly binding
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>    
  </runtime>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: true);

            // no namespace
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <goo/>
  <runtime>
    <goo/>
    <assemblyBinding>
    </assemblyBinding>    
    <goo/>
  </runtime>
  <goo/>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_Values_MissingNamespace()
        {
            var appConfig = Temp.CreateFile();
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding>
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/> <!-- platform -->
       <supportPortability PKT=""31bf3856ad364e35"" enable=""false""/> <!-- non-platform -->
    </assemblyBinding>
  </runtime>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_Values1()
        {
            var appConfig = Temp.CreateFile();

            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>   <!-- platform -->
    </assemblyBinding>
  </runtime>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: false, nonPlatform: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_Values2()
        {
            var appConfig = Temp.CreateFile();

            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""31bf3856ad364e35"" enable=""false""/>  <!-- non-platform -->
    </assemblyBinding>
  </runtime>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: false);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_Values4()
        {
            var appConfig = Temp.CreateFile();
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""true""/> <!-- platform -->
       <supportPortability PKT=""31bf3856ad364e35"" enable=""false""/> <!-- nonplatform -->
    </assemblyBinding>
  </runtime>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: false);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_DuplicateSupportPortability1()
        {
            var appConfig = Temp.CreateFile();
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""true""/>   <!-- platform -->
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>  <!-- platform -->
       <supportPortability PKT=""31bf3856ad364e35"" enable=""false""/> <!-- nonplatform -->
    </assemblyBinding>
  </runtime>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: false, nonPlatform: false);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_DuplicateSupportPortability2()
        {
            var appConfig = Temp.CreateFile();
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/> <!-- platform -->
       <supportPortability PKT=""31bf3856ad364e35"" enable=""false""/> <!-- nonplatform -->
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""true""/>  <!-- platform -->
       <supportPortability PKT=""31bf3856ad364e35"" enable=""false""/> <!-- nonplatform -->
    </assemblyBinding>
  </runtime>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: false);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_UnknownAttributes2()
        {
            var appConfig = Temp.CreateFile();
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" goo=""false""/> <!-- platform -->
       <supportPortability pkt=""7cec85d7bea7798e"" enable=""false""/> <!-- platform -->
       <supportPortability PKT=""31bf3856ad364e35"" Enable=""false""/> <!-- nonplatform -->
       <supportPortability enable=""false""/> <!-- platform -->
       <supportPortability PKT=""31Zf3856ad364e35"" enable=""false""/> <!-- garbage -->
       <supportPortability PKT=""9999999999999999"" enable=""false""/> <!-- garbage -->
       <supportPortability PKT="""" enable=""false""/> <!-- garbage -->
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""""/> <!-- garbage -->
    </assemblyBinding>
  </runtime>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_InterleavingElements()
        {
            var appConfig = Temp.CreateFile();
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <goo>
    <runtime>
      <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
         <supportPortability PKT=""7cec85d7bea7798e"" enable=""true""/> <!-- platform -->
         <supportPortability PKT=""31bf3856ad364e35"" enable=""false""/> <!-- nonplatform -->
      </assemblyBinding>
    </runtime>
  </goo>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void LoadFromFile_EmptyElement()
        {
            var appConfig = Temp.CreateFile();
            appConfig.WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <goo>
    <runtime>
      <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"" />
    </runtime>
  </goo>
</configuration>
");
            AssertIsEnabled(appConfig.Path, platform: true, nonPlatform: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Fusion_Dispose()
        {
            var appConfig = Temp.CreateFile().WriteAllText(correctAppConfigText);

            var policy = FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path);
            Assert.NotEqual(IntPtr.Zero, policy.ConfigCookie);
            policy.Dispose();
            Assert.Equal(IntPtr.Zero, policy.ConfigCookie);
            policy.Dispose();
            Assert.Equal(IntPtr.Zero, policy.ConfigCookie);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Fusion_TestEquals()
        {
            var appConfig = Temp.CreateFile().WriteAllText(correctAppConfigText);

            var policy1 = FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path);
            var policy2 = FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path);
            Assert.Equal(policy1, policy2);
            Assert.Equal(policy1.GetHashCode(), policy2.GetHashCode());

            appConfig.WriteAllText(correctAppConfigText);
            policy2 = FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path);
            Assert.Equal(policy1, policy2);
            Assert.Equal(policy1.GetHashCode(), policy2.GetHashCode());

            appConfig = Temp.CreateFile().WriteAllText(correctAppConfigText);
            policy2 = FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path);
            Assert.Equal(policy1, policy2);
            Assert.Equal(policy1.GetHashCode(), policy2.GetHashCode());

            appConfig.WriteAllText(@"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""01234567890abcdef"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>");

            policy2 = FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig.Path);
            Assert.NotEqual(policy1, policy2);

            var appConfig2 = Temp.CreateFile().WriteAllText(correctAppConfigText);

            policy2 = FusionAssemblyPortabilityPolicy.LoadFromFile(appConfig2.Path);
            Assert.Equal(policy1, policy2);
            Assert.Equal(policy1.GetHashCode(), policy2.GetHashCode());
        }
    }
}

