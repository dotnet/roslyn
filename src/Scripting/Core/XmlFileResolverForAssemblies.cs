// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal static class XmlFileResolverForAssemblies
    {
        public static bool TryFindXmlDocumentationFile(string assemblyFilePath, out string xmlDocumentationFilePath)
        {
            // TODO(DustinCa): This is a workaround  and we'll need to update this to handle getting the correct
            // reference assembly for different framework versions and profiles.

            string xmlFilePath = string.Empty;

            // 1. Look in subdirectories based on the current culture
            string xmlFileName = Path.ChangeExtension(Path.GetFileName(assemblyFilePath), ".xml");
            string originalDirectory = Path.GetDirectoryName(assemblyFilePath);

            var culture = CultureInfo.CurrentCulture;
            while (culture != CultureInfo.InvariantCulture)
            {
                xmlFilePath = Path.Combine(originalDirectory, culture.Name, xmlFileName);
                if (File.Exists(xmlFilePath))
                {
                    break;
                }

                culture = culture.Parent;
            }

            if (File.Exists(xmlFilePath))
            {
                xmlDocumentationFilePath = xmlFilePath;
                return true;
            }

            // 2. Look in the same directory as the assembly itself
            var extension = Path.GetExtension(assemblyFilePath);
            if (string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".winmd", StringComparison.OrdinalIgnoreCase))
            {
                xmlFilePath = Path.ChangeExtension(assemblyFilePath, ".xml");
            }
            else
            {
                xmlFilePath = assemblyFilePath + ".xml";
            }

            if (File.Exists(xmlFilePath))
            {
                xmlDocumentationFilePath = xmlFilePath;
                return true;
            }

#if !SCRIPTING
            // 3. Look for reference assemblies

            string referenceAssemblyFilePath;
            if (!Roslyn.Utilities.ReferencePathUtilities.TryGetReferenceFilePath(assemblyFilePath, out referenceAssemblyFilePath))
            {
                xmlDocumentationFilePath = null;
                return false;
            }

            xmlFilePath = Path.ChangeExtension(referenceAssemblyFilePath, ".xml");

            if (File.Exists(xmlFilePath))
            {
                xmlDocumentationFilePath = xmlFilePath;
                return true;
            }
#endif

            xmlDocumentationFilePath = null;
            return false;
        }
    }
}