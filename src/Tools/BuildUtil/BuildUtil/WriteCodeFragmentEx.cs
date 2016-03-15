using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Roslyn.MSBuild.Util
{
    public class WriteCodeFragmentEx : Task
    {
        [Required]
        public string Language { get; set; }

        public ITaskItem[] AssemblyAttributes { get; set; }

        [Output]
        public ITaskItem OutputFile { get; set; }

        public override bool Execute()
        {
            if (String.IsNullOrEmpty(Language))
            {
                Log.LogError($"The {nameof(Language)} parameter is required");
                return false;
            }

            if (OutputFile == null)
            {
                Log.LogError($"The {nameof(OutputFile)} parameter is required");
                return false;
            }

            try
            {
                string extension;

                var code = GenerateCodeCoreClr(out extension);
                if (code == null)
                {
                    Log.LogError("No code");
                }

                File.WriteAllText(OutputFile.ItemSpec, code);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true, showDetail: true, file: null);
            }

            return false;
        }

        /// <summary>
        /// Generates the code into a string.
        /// If it fails, logs an error and returns null.
        /// If no meaningful code is generated, returns empty string.
        /// Returns the default language extension as an out parameter.
        /// </summary>
        private string GenerateCodeCoreClr(out string extension)
        {
            extension = null;
            bool haveGeneratedContent = false;

            StringBuilder code = new StringBuilder();
            switch (Language.ToLowerInvariant())
            {
                case "c#":
                    if (AssemblyAttributes == null) return string.Empty;

                    extension = "cs";
                    code.AppendLine("// WriteCodeFragment.Comment");
                    code.AppendLine();
                    code.AppendLine("using System;");
                    code.AppendLine("using System.Reflection;");
                    code.AppendLine();

                    foreach (ITaskItem attributeItem in AssemblyAttributes)
                    {
                        string args = GetAttributeArguments(attributeItem, "=");
                        if (args == null) return null;

                        code.AppendLine(string.Format($"[assembly: {attributeItem.ItemSpec}({args})]"));
                        haveGeneratedContent = true;
                    }

                    break;
                case "visual basic":
                case "visualbasic":
                case "vb":
                    if (AssemblyAttributes == null) return string.Empty;

                    extension = "vb";
                    code.AppendLine("' WriteCodeFragment.Comment");
                    code.AppendLine();
                    code.AppendLine("Option Strict Off");
                    code.AppendLine("Option Explicit On");
                    code.AppendLine();
                    code.AppendLine("Imports System");
                    code.AppendLine("Imports System.Reflection");

                    foreach (ITaskItem attributeItem in AssemblyAttributes)
                    {
                        string args = GetAttributeArguments(attributeItem, ":=");
                        if (args == null) return null;

                        code.AppendLine(string.Format($"<Assembly: {attributeItem.ItemSpec}({args})>"));
                        haveGeneratedContent = true;
                    }
                    break;
                default:
                    Log.LogError($"No language provider for language \"{Language}\"");
                    return null;
            }

            // If we just generated infrastructure, don't bother returning anything
            // as there's no point writing the file
            return haveGeneratedContent ? code.ToString() : string.Empty;
        }

        private string GetAttributeArguments(ITaskItem attributeItem, string namedArgumentString)
        {
            // Some attributes only allow positional constructor arguments, or the user may just prefer them.
            // To set those, use metadata names like "_Parameter1", "_Parameter2" etc.
            // If a parameter index is skipped, it's an error.
            IDictionary customMetadata = attributeItem.CloneCustomMetadata();
            
            // Initialize count + 1 to access starting at 1
            List<string> orderedParameters = new List<string>(new string[customMetadata.Count + 1]);
            List<string> namedParameters = new List<string>();

            foreach (DictionaryEntry entry in customMetadata)
            {
                string name = (string) entry.Key;
                string value = entry.Value is string ? $@"""{entry.Value}""" : entry.Value.ToString();

                if (name.StartsWith("_Parameter", StringComparison.OrdinalIgnoreCase))
                {
                    int index;

                    if (!int.TryParse(name.Substring("_Parameter".Length), out index))
                    {
                        Log.LogErrorWithCodeFromResources("General.InvalidValue", name, "WriteCodeFragment");
                        return null;
                    }

                    if (index > orderedParameters.Count || index < 1)
                    {
                        Log.LogErrorWithCodeFromResources("WriteCodeFragment.SkippedNumberedParameter", index);
                        return null;
                    }

                    // "_Parameter01" and "_Parameter1" would overwrite each other
                    orderedParameters[index - 1] = value;
                }
                else
                {
                    namedParameters.Add($"{name}{namedArgumentString}{value}");
                }
            }

            bool encounteredNull = false;
            
            for (int i = 0; i < orderedParameters.Count; i++)
            {
                if (orderedParameters[i] == null)
                {
                    // All subsequent args should be null, else a slot was missed
                    encounteredNull = true;
                    continue;
                }

                if (encounteredNull)
                {
                    Log.LogErrorWithCodeFromResources("WriteCodeFragment.SkippedNumberedParameter", i + 1 /* back to 1 based */);
                    return null;
                }
            }

            return string.Join(", ", orderedParameters.Union(namedParameters).Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}
