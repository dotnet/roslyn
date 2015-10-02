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
using NuGetPackageResolver = WORKSPACES::Microsoft.CodeAnalysis.Scripting.Hosting.NuGetPackageResolver;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal sealed class NuGetPackageResolverImpl : NuGetPackageResolver
    {
        private const string ProjectJsonFramework = "net46";
        private const string ProjectLockJsonFramework = ".NETFramework,Version=v4.6";
        private const string EmptyNuGetConfig =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageRestore>
    <add key=""enabled"" value=""True"" />
    <add key=""automatic"" value=""False"" />
  </packageRestore>
  <packageSources/>
</configuration>";

        private readonly string _packagesDirectory;
        private readonly Action<ProcessStartInfo> _restore;

        internal NuGetPackageResolverImpl(string packagesDirectory, Action<ProcessStartInfo> restore = null)
        {
            Debug.Assert(PathUtilities.IsAbsolute(packagesDirectory));
            _packagesDirectory = packagesDirectory;
            _restore = restore ?? NuGetRestore;
        }

        internal new static bool TryParsePackageReference(string reference, out string name, out string version)
        {
            return NuGetPackageResolver.TryParsePackageReference(reference, out name, out version);
        }

        internal override ImmutableArray<string> ResolveNuGetPackage(string packageName, string packageVersion)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
                var tempDir = Directory.CreateDirectory(tempPath);
                try
                {
                    // Create project.json.
                    var projectJsonPath = Path.Combine(tempPath, "project.json");
                    using (var stream = File.OpenWrite(projectJsonPath))
                    using (var writer = new StreamWriter(stream))
                    {
                        WriteProjectJson(writer, packageName, packageVersion);
                    }

                    // Create nuget.config with no package sources so restore
                    // uses the local cache only, no downloading.
                    var configPath = Path.Combine(tempPath, "nuget.config");
                    File.WriteAllText(configPath, EmptyNuGetConfig);

                    // Copy nuget.exe resource to temp directory. (nuget.exe is an
                    // implementation detail here and embedding nuget.exe as a
                    // resource rather than installing nuget.exe as a separate exe
                    // means we won't add nuget.exe unnecessarily to the path.)
                    var nugetExePath = Path.Combine(tempPath, "nuget.exe");
                    File.WriteAllBytes(nugetExePath, Resources.NuGetExe);

                    // Run "nuget.exe restore project.json -configfile nuget.config"
                    // to generate project.lock.json.
                    NuGetRestore(nugetExePath, projectJsonPath, configPath);

                    // Read the references from project.lock.json.
                    var projectLockJsonPath = Path.Combine(tempPath, "project.lock.json");
                    using (var stream = File.OpenRead(projectLockJsonPath))
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
            return ImmutableArray<string>.Empty;
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
                        var packageRoot = Path.Combine(packagesDirectory, package.Key);
                        var runtime = (JObject)GetPropertyValue((JObject)package.Value, "runtime");
                        if (runtime == null)
                        {
                            continue;
                        }
                        foreach (var item in runtime)
                        {
                            var relativePath = item.Key;
                            // Ignore placeholder "_._" files.
                            var name = Path.GetFileName(relativePath);
                            if (string.Equals(name, "_._", StringComparison.InvariantCulture))
                            {
                                continue;
                            }
                            builder.Add(Path.Combine(packageRoot, relativePath));
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

        private void NuGetRestore(string nugetExePath, string projectJsonPath, string configPath)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = nugetExePath,
                Arguments = $"restore \"{projectJsonPath}\" -ConfigFile \"{configPath}\" -PackagesDirectory \"{_packagesDirectory}\"",
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
