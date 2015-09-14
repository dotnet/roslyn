// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias WORKSPACES;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    internal sealed class NuGetPackageResolverImpl : WORKSPACES::Microsoft.CodeAnalysis.Scripting.Hosting.NuGetPackageResolver
    {
        private const string ProjectJsonFramework = "net46";
        private const string ProjectLockJsonFramework = ".NETFramework,Version=v4.6";

        private readonly string _packagesDirectory;
        private readonly Action<ProcessStartInfo> _restore;

        internal NuGetPackageResolverImpl(string packagesDirectory, Action<ProcessStartInfo> restore = null)
        {
            Debug.Assert(PathUtilities.IsAbsolute(packagesDirectory));
            _packagesDirectory = packagesDirectory;
            _restore = restore ?? NuGetRestore;
        }

        internal override ImmutableArray<string> ResolveNuGetPackage(string reference)
        {
            string packageName;
            string packageVersion;
            if (!ParsePackageReference(reference, out packageName, out packageVersion))
            {
                return default(ImmutableArray<string>);
            }

            try
            {
                var tempPath = PathUtilities.CombineAbsoluteAndRelativePaths(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
                var tempDir = Directory.CreateDirectory(tempPath);
                try
                {
                    // Create project.json.
                    var projectJson = PathUtilities.CombineAbsoluteAndRelativePaths(tempPath, "project.json");
                    using (var stream = File.OpenWrite(projectJson))
                    using (var writer = new StreamWriter(stream))
                    {
                        WriteProjectJson(writer, packageName, packageVersion);
                    }

                    // Run "nuget.exe restore project.json" to generate project.lock.json.
                    NuGetRestore(projectJson);

                    // Read the references from project.lock.json.
                    var projectLockJson = PathUtilities.CombineAbsoluteAndRelativePaths(tempPath, "project.lock.json");
                    using (var stream = File.OpenRead(projectLockJson))
                    using (var reader = new StreamReader(stream))
                    {
                        return ReadProjectLockJson(_packagesDirectory, reader);
                    }
                }
                finally
                {
                    tempDir.Delete(recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return default(ImmutableArray<string>);
        }

        /// <summary>
        /// Syntax is "id/version", matching references in project.lock.json.
        /// </summary>
        internal static bool ParsePackageReference(string reference, out string name, out string version)
        {
            var parts = reference.Split('/');
            if ((parts.Length == 2) &&
                (parts[0].Length > 0) &&
                (parts[1].Length > 0))
            {
                name = parts[0];
                version = parts[1];
                return true;
            }
            name = null;
            version = null;
            return false;
        }

        /// <summary>
        /// Generate a project.json file with the packages as "dependencies".
        /// </summary>
        internal static void WriteProjectJson(TextWriter writer, string packageName, string packageVersion)
        {
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;
                jsonWriter.WriteStartObject();
                // "dependencies" : { "packageName" : "packageVersion" }
                jsonWriter.WritePropertyName("dependencies");
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName(packageName, escape: true);
                jsonWriter.WriteValue(packageVersion);
                jsonWriter.WriteEndObject();
                // "frameworks" : { "net46" : {} }
                jsonWriter.WritePropertyName("frameworks");
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName(ProjectJsonFramework, escape: true);
                jsonWriter.WriteStartObject();
                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }
        }

        /// <summary>
        /// Read the references from the project.lock.json file.
        /// </summary>
        internal static ImmutableArray<string> ReadProjectLockJson(string packagesDirectory, TextReader reader)
        {
            JObject obj;
            using (var jsonReader = new JsonTextReader(reader))
            {
                obj = JObject.Load(jsonReader);
            }
            var builder = ArrayBuilder<string>.GetInstance();
            var targets = (JObject)GetPropertyValue(obj, "targets");
            foreach (var target in targets)
            {
                if (target.Key == ProjectLockJsonFramework)
                {
                    foreach (var package in (JObject)target.Value)
                    {
                        var packageRoot = PathUtilities.CombineAbsoluteAndRelativePaths(packagesDirectory, package.Key);
                        var runtime = (JObject)GetPropertyValue((JObject)package.Value, "runtime");
                        if (runtime == null)
                        {
                            continue;
                        }
                        foreach (var item in runtime)
                        {
                            var path = PathUtilities.CombinePossiblyRelativeAndRelativePaths(packageRoot, item.Key);
                            builder.Add(path);
                        }
                    }
                    break;
                }
            }
            return builder.ToImmutableAndFree();
        }

        private static JToken GetPropertyValue(JObject obj, string propertyName)
        {
            JToken value;
            obj.TryGetValue(propertyName, out value);
            return value;
        }

        private void NuGetRestore(string projectJsonPath)
        {
            // Load nuget.exe from same directory as current assembly.
            var nugetExePath = PathUtilities.CombineAbsoluteAndRelativePaths(
                PathUtilities.GetDirectoryName(
                    CorLightup.Desktop.GetAssemblyLocation(typeof(NuGetPackageResolverImpl).GetTypeInfo().Assembly)),
                "nuget.exe");
            var startInfo = new ProcessStartInfo()
            {
                FileName = nugetExePath,
                Arguments = $"restore \"{projectJsonPath}\" -PackagesDirectory \"{_packagesDirectory}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            _restore(startInfo);
        }

        private static void NuGetRestore(ProcessStartInfo startInfo)
        {
            var process = Process.Start(startInfo);
            // Should echo output and errors to InteractiveWindow.
            process.StandardOutput.ReadToEndAsync();
            process.StandardError.ReadToEndAsync();
            process.WaitForExit();
        }
    }
}
