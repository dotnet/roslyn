// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Xml;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Policy to be used when matching assembly reference to an assembly definition across platforms.
    /// </summary>
    internal readonly struct AssemblyPortabilityPolicy : IEquatable<AssemblyPortabilityPolicy>
    {
        // 7cec85d7bea7798e (System, System.Core)
        public readonly bool SuppressSilverlightPlatformAssembliesPortability;

        // 31bf3856ad364e35 (Microsoft.VisualBasic, System.ComponentModel.Composition)
        public readonly bool SuppressSilverlightLibraryAssembliesPortability;

        public AssemblyPortabilityPolicy(
            bool suppressSilverlightPlatformAssembliesPortability,
            bool suppressSilverlightLibraryAssembliesPortability)
        {
            this.SuppressSilverlightLibraryAssembliesPortability = suppressSilverlightLibraryAssembliesPortability;
            this.SuppressSilverlightPlatformAssembliesPortability = suppressSilverlightPlatformAssembliesPortability;
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyPortabilityPolicy && Equals((AssemblyPortabilityPolicy)obj);
        }

        public bool Equals(AssemblyPortabilityPolicy other)
        {
            return this.SuppressSilverlightLibraryAssembliesPortability == other.SuppressSilverlightLibraryAssembliesPortability
                && this.SuppressSilverlightPlatformAssembliesPortability == other.SuppressSilverlightPlatformAssembliesPortability;
        }

        public override int GetHashCode()
        {
            return (this.SuppressSilverlightLibraryAssembliesPortability ? 1 : 0) |
                   (this.SuppressSilverlightPlatformAssembliesPortability ? 2 : 0);
        }

        private static bool ReadToChild(XmlReader reader, int depth, string elementName, string elementNamespace = "")
        {
            return reader.ReadToDescendant(elementName, elementNamespace) && reader.Depth == depth;
        }

        private static readonly XmlReaderSettings s_xmlSettings = new XmlReaderSettings()
        {
            DtdProcessing = DtdProcessing.Prohibit,
        };

        internal static AssemblyPortabilityPolicy LoadFromXml(Stream input)
        {
            // Note: Unlike Fusion XML reader the XmlReader doesn't allow whitespace in front of <?xml version=""1.0"" encoding=""utf-8"" ?>

            const string ns = "urn:schemas-microsoft-com:asm.v1";

            using (XmlReader xml = XmlReader.Create(input, s_xmlSettings))
            {
                if (!ReadToChild(xml, 0, "configuration") ||
                    !ReadToChild(xml, 1, "runtime") ||
                    !ReadToChild(xml, 2, "assemblyBinding", ns) ||
                    !ReadToChild(xml, 3, "supportPortability", ns))
                {
                    return default(AssemblyPortabilityPolicy);
                }

                // 31bf3856ad364e35
                bool suppressLibrary = false;

                // 7cec85d7bea7798e
                bool suppressPlatform = false;

                do
                {
                    // see CNodeFactory::ProcessSupportPortabilityTag in fusion\inc\nodefact.cpp for details
                    //  - unrecognized attributes ignored.
                    //  - syntax errors within tags causes this tag to be ignored (but not reject entire app.config)
                    //  - multiple <supportPortability> tags ok (if two specify same PKT, all but (implementation defined) one ignored.)
                    string pkt = xml.GetAttribute("PKT");
                    string enableAttribute = xml.GetAttribute("enable");

                    bool? enable =
                        string.Equals(enableAttribute, "false", StringComparison.OrdinalIgnoreCase) ? false :
                        string.Equals(enableAttribute, "true", StringComparison.OrdinalIgnoreCase) ? true :
                        (bool?)null;

                    if (enable != null)
                    {
                        if (string.Equals(pkt, "31bf3856ad364e35", StringComparison.OrdinalIgnoreCase))
                        {
                            suppressLibrary = !enable.Value;
                        }
                        else if (string.Equals(pkt, "7cec85d7bea7798e", StringComparison.OrdinalIgnoreCase))
                        {
                            suppressPlatform = !enable.Value;
                        }
                    }
                } while (xml.ReadToNextSibling("supportPortability", ns));

                return new AssemblyPortabilityPolicy(suppressPlatform, suppressLibrary);
            }
        }
    }
}
