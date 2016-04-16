// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed partial class DesktopAssemblyIdentityComparer : AssemblyIdentityComparer
    {
        // Portability:
        //   If a reference or definition identity is strong, not retargetable, and portable (i.e. one of a few names hard-coded in Fusion) 
        //   and the portability for its PublicKeyToken is not disabled in app.config file then the identity is replaced by a different identity 
        //   before the match is performed.
        //
        //   Portable identities:
        //    - System, PKT=7cec85d7bea7798e, Version=2.0.0.0-5.9.0.0                            -> System, PKT=b77a5c561934e089, Version=<current framework version>
        //    - System.Core, PKT=7cec85d7bea7798e, Version=2.0.0.0-5.9.0.0                       -> System.Core, PKT=b77a5c561934e089, Version=<current framework version>
        //    - System.ComponentModel.Composition, PKT=31bf3856ad364e35, Version=2.0.0.0-5.9.0.0 -> System.ComponentModel.Composition, PKT=b77a5c561934e089, Version=<current framework version> 
        //    - Microsoft.VisualBasic, PKT=31bf3856ad364e35, Version=2.0.0.0-5.9.0.0             -> Microsoft.VisualBasic, PKT=b03f5f7f11d50a3a, Version=10.0.0.0
        //
        // Retargetability:
        //   If the reference identity specifies property Retargetable=Yes the identity is looked up in a hard-coded Fusion table.
        //   The table maps it to an identity that is used for identity matching.
        //   Note: Retargeting may change name, version and public key token of the identity.
        //   
        // Version unification:
        //   FX identities (hard-coded list in Fusion):
        //     FX references are unified regardless of the isUnified flags, returns EquivalentFxUnified.
        //   Non-FX identities:
        //     if (isUnified1 && version1 > version2 || isUnified2 && version1 < version2) return EquivalentUnified.

        public static new DesktopAssemblyIdentityComparer Default { get; } = new DesktopAssemblyIdentityComparer(default(AssemblyPortabilityPolicy));

        internal readonly AssemblyPortabilityPolicy policy;

        /// <param name="policy">Assembly portability policy, usually provided through an app.config file.</param>
        internal DesktopAssemblyIdentityComparer(AssemblyPortabilityPolicy policy)
        {
            this.policy = policy;
        }

        /// <summary>
        /// Loads <see cref="AssemblyPortabilityPolicy"/> information from XML with app.config schema.
        /// </summary>
        /// <exception cref="System.Xml.XmlException">The stream doesn't contain a well formed XML.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        /// <remarks>
        /// Tries to find supportPortability elements in the given XML:
        /// <![CDATA[
        /// <configuration>
        ///    <runtime>
        ///       <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
        ///          <supportPortability PKT="7cec85d7bea7798e" enable="false"/>
        ///          <supportPortability PKT="31bf3856ad364e35" enable="false"/>
        ///       </assemblyBinding>
        ///    </runtime>
        /// </configuration>
        /// ]]>
        /// 
        /// Keeps the stream open.
        /// </remarks>
        public static DesktopAssemblyIdentityComparer LoadFromXml(Stream input)
        {
            return new DesktopAssemblyIdentityComparer(AssemblyPortabilityPolicy.LoadFromXml(input));
        }

        internal AssemblyPortabilityPolicy PortabilityPolicy
        {
            get
            {
                return this.policy;
            }
        }

        internal override bool ApplyUnificationPolicies(
            ref AssemblyIdentity reference,
            ref AssemblyIdentity definition,
            AssemblyIdentityParts referenceParts,
            out bool isDefinitionFxAssembly)
        {
            if (reference.ContentType == AssemblyContentType.Default &&
                SimpleNameComparer.Equals(reference.Name, definition.Name) &&
                SimpleNameComparer.Equals(reference.Name, "mscorlib"))
            {
                isDefinitionFxAssembly = true;
                reference = definition;
                return true;
            }

            if (!reference.IsRetargetable && definition.IsRetargetable)
            {
                // Reference is not retargetable, but definition is retargetable.
                // Non-equivalent.
                isDefinitionFxAssembly = false;
                return false;
            }

            // Notes:
            // an assembly might be both retargetable and portable
            // in that case retargetable table acts as an override.

            // Apply portability policy transforms first (e.g. rewrites references to SL assemblies to their desktop equivalents)
            // If the reference is partial and is missing version or PKT it is not ported.
            reference = Port(reference);
            definition = Port(definition);

            if (reference.IsRetargetable && !definition.IsRetargetable)
            {
                if (!AssemblyIdentity.IsFullName(referenceParts))
                {
                    isDefinitionFxAssembly = false;
                    return false;
                }

                // Reference needs to be retargeted before comparison, 
                // unless it's optionally retargetable and we already match the PK
                bool skipRetargeting = IsOptionallyRetargetableAssembly(reference) &&
                                       AssemblyIdentity.KeysEqual(reference, definition);

                if (!skipRetargeting)
                {
                    reference = Retarget(reference);
                }
            }

            // At this point we are in one of the following states:
            //
            //   1) Both ref/def are not retargetable
            //   2) Both ref/def are retargetable
            //   3) Ref is retargetable (and has been retargeted)
            //
            // We can do a straight compare of ref/def at this point using the
            // regular rules

            if (reference.IsRetargetable && definition.IsRetargetable)
            {
                isDefinitionFxAssembly = IsRetargetableAssembly(definition);
            }
            else
            {
                isDefinitionFxAssembly = IsFrameworkAssembly(definition);
            }

            return true;
        }

        /// <summary>
        /// Returns true if the identity is a Framework 4.5 or lower assembly.
        /// </summary>
        private static bool IsFrameworkAssembly(AssemblyIdentity identity)
        {
            // Note:
            // FrameworkAssemblyTable::IsFrameworkAssembly returns false if culture is not neutral.
            // However its caller doesn't initialize the culture and hence the culture is ignored.
            //   PrepQueryMatchData(pName, wzName, &dwSizeName, wzVersion, &dwSizeVer, wzPublicKeyToken, &dwSizePKT, NULL, NULL, NULL);. 

            if (identity.ContentType != AssemblyContentType.Default)
            {
                return false;
            }

            FrameworkAssemblyDictionary.Value value;
            if (!s_arFxPolicy.TryGetValue(identity.Name, out value) ||
                !value.PublicKeyToken.SequenceEqual(identity.PublicKeyToken))
            {
                return false;
            }

            // build and revision numbers are ignored
            uint thisVersion = ((uint)identity.Version.Major << 16) | (uint)identity.Version.Minor;
            uint fxVersion = ((uint)value.Version.Major << 16) | (uint)value.Version.Minor;
            return thisVersion <= fxVersion;
        }

        private static bool IsRetargetableAssembly(AssemblyIdentity identity)
        {
            bool retargetable, portable;
            IsRetargetableAssembly(identity, out retargetable, out portable);
            return retargetable;
        }

        private static bool IsOptionallyRetargetableAssembly(AssemblyIdentity identity)
        {
            if (!identity.IsRetargetable)
            {
                return false;
            }

            bool retargetable, portable;
            IsRetargetableAssembly(identity, out retargetable, out portable);
            return retargetable && portable;
        }

        private static bool IsTriviallyNonRetargetable(AssemblyIdentity identity)
        {
            // Short-circuit zero-version/non-neutral culture/weak name, 
            // which will never match retargeted identities.
            return identity.CultureName.Length != 0
                || identity.ContentType != AssemblyContentType.Default
                || !identity.IsStrongName;
        }

        private static void IsRetargetableAssembly(AssemblyIdentity identity, out bool retargetable, out bool portable)
        {
            retargetable = portable = false;

            if (IsTriviallyNonRetargetable(identity))
            {
                return;
            }

            FrameworkRetargetingDictionary.Value value;
            retargetable = s_arRetargetPolicy.TryGetValue(identity, out value);
            portable = value.IsPortable;
        }

        private static AssemblyIdentity Retarget(AssemblyIdentity identity)
        {
            if (IsTriviallyNonRetargetable(identity))
            {
                return identity;
            }

            FrameworkRetargetingDictionary.Value value;
            if (s_arRetargetPolicy.TryGetValue(identity, out value))
            {
                return new AssemblyIdentity(
                    value.NewName ?? identity.Name,
                    (Version)value.NewVersion,
                    identity.CultureName,
                    value.NewPublicKeyToken,
                    hasPublicKey: false,
                    isRetargetable: identity.IsRetargetable,
                    contentType: AssemblyContentType.Default);
            }

            return identity;
        }

        private AssemblyIdentity Port(AssemblyIdentity identity)
        {
            if (identity.IsRetargetable || !identity.IsStrongName || identity.ContentType != AssemblyContentType.Default)
            {
                return identity;
            }

            Version newVersion = null;
            ImmutableArray<byte> newPublicKeyToken = default(ImmutableArray<byte>);

            var version = (AssemblyVersion)identity.Version;
            if (version >= new AssemblyVersion(2, 0, 0, 0) && version <= new AssemblyVersion(5, 9, 0, 0))
            {
                if (identity.PublicKeyToken.SequenceEqual(s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L))
                {
                    if (!policy.SuppressSilverlightPlatformAssembliesPortability)
                    {
                        if (SimpleNameComparer.Equals(identity.Name, "System") ||
                            SimpleNameComparer.Equals(identity.Name, "System.Core"))
                        {
                            newVersion = (Version)s_VER_ASSEMBLYVERSION_STR_L;
                            newPublicKeyToken = s_ECMA_PUBLICKEY_STR_L;
                        }
                    }
                }
                else if (identity.PublicKeyToken.SequenceEqual(s_SILVERLIGHT_PUBLICKEY_STR_L))
                {
                    if (!policy.SuppressSilverlightLibraryAssembliesPortability)
                    {
                        if (SimpleNameComparer.Equals(identity.Name, "Microsoft.VisualBasic"))
                        {
                            newVersion = new Version(10, 0, 0, 0);
                            newPublicKeyToken = s_MICROSOFT_PUBLICKEY_STR_L;
                        }

                        if (SimpleNameComparer.Equals(identity.Name, "System.ComponentModel.Composition"))
                        {
                            newVersion = (Version)s_VER_ASSEMBLYVERSION_STR_L;
                            newPublicKeyToken = s_ECMA_PUBLICKEY_STR_L;
                        }
                    }
                }
            }

            if (newVersion == null)
            {
                return identity;
            }

            return new AssemblyIdentity(
                identity.Name,
                newVersion,
                identity.CultureName,
                newPublicKeyToken,
                hasPublicKey: false,
                isRetargetable: identity.IsRetargetable,
                contentType: AssemblyContentType.Default);
        }
    }
}
