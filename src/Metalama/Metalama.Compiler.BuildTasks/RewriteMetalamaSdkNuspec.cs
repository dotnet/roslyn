// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Metalama.Compiler.BuildTasks;

public class RewriteMetalamaSdkNuspec : Task
{
    [Required] public string File { get; set; } = null!;

    [Required] public ITaskItem[] Replacements { get; set; } = null!;

    public override bool Execute()
    {
        XNamespace ns = "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd";

        // Validating inputs.
        if (this.Replacements.Length == 0)
        {
            this.Log.LogError("The Replacements parameter of the RewriteMetalamaSdkNuspec task is empty.");
            return false;
        }

        var invalidReplacements = this.Replacements.Where(r => r.GetMetadata("ReplacedBy") == null).ToList();
        if (invalidReplacements.Count > 0)
        {
            foreach (var invalidReplacement in invalidReplacements)
            {
                this.Log.LogError($"The ReplacedBy property for Replacement '{invalidReplacement.ItemSpec}' is missing.");
            }

            return false;
        }

        if (!System.IO.File.Exists(this.File))
        {
            this.Log.LogError($"THe file '{this.File}' was not found.");
            return false;
        }

        var doc = XDocument.Load(File);
        var dependencies = doc.Element(ns + "package")?.Element(ns + "metadata")?.Element(ns + "dependencies");

        if (dependencies == null)
        {
            Log.LogError($"Cannot find the 'package/metadata/dependencies' element in '{File}'.");
            return false;
        }

        var groups = dependencies.Elements(ns + "group");

        foreach (var replacement in this.Replacements)
        {
            foreach (var group in groups)
            {
                var originalValue = replacement.ItemSpec;
                var replacedBy = replacement.GetMetadata("ReplacedBy")!;
                
                var metalamaDependencies = group.Elements(ns + "dependency")
                    .Where(e => e.Attribute("id")?.Value == originalValue)
                    .ToList();

                if (metalamaDependencies.Count == 0)
                {
                    continue;
                }

                if (metalamaDependencies.Count > 1)
                {
                    Log.LogError($"Found more than one dependency for '{originalValue}' in '{File}'.");
                    return false;
                }
                
                Log.LogMessage(MessageImportance.Normal, $"Replacing dependencies in '{this.File}': '{originalValue}' -> '{replacedBy}'.");

                var dependency = metalamaDependencies[0];

                dependency.Attribute("id")!.Value = replacedBy;
                dependency.Attribute("exclude")?.Remove();
            }
        }

        doc.Save(File);

        return true;
    }
}
