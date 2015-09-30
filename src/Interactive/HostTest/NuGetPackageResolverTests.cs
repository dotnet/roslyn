// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    public class NuGetPackageResolverTests : TestBase
    {
        /// <summary>
        /// Valid reference.
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly))]
        public void ResolveReference()
        {
            var expectedProjectJson =
@"{
  ""dependencies"": {
    ""A.B.C"": ""1.2""
  },
  ""frameworks"": {
    ""net46"": {}
  }
}";
            var expectedConfig =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageRestore>
    <add key=""enabled"" value=""True"" />
    <add key=""automatic"" value=""False"" />
  </packageRestore>
  <packageSources/>
</configuration>";
            var actualProjectLockJson =
@"{
  ""locked"": false,
  ""version"": 1,
  ""targets"": {
    "".NETFramework,Version=v4.5"": { },
    "".NETFramework,Version=v4.6"": {
      ""System.Collections/4.0.10"": {
        ""dependencies"": {
          ""System.Runtime"": """"
        },
        ""compile"": {
          ""ref/dotnet/System.Runtime.dll"": {}
        },
        ""runtime"": {
          ""ref/dotnet/System.Collections.dll"": {}
        }
      },
      ""System.Diagnostics.Debug/4.0.10"": {
        ""dependencies"": {
          ""System.Runtime"": """"
        },
      },
      ""System.Runtime/4.0.0"": {
        ""runtime"": {
          ""ref/dotnet/_._"": {}
        }
      },
      ""System.IO/4.0.10"": {
        ""dependencies"": {},
        ""runtime"": {
          ""ref/dotnet/System.Runtime.dll"": {},
          ""ref/dotnet/System.IO.dll"": {}
        }
      }
    }
  }
}";
            using (var directory = new DisposableDirectory(Temp))
            {
                var packagesDirectory = directory.Path;
                var resolver = new NuGetPackageResolverImpl(
                    packagesDirectory,
                    startInfo =>
                    {
                        // Verify arguments.
                        var arguments = startInfo.Arguments.Split('"');
                        Assert.Equal(7, arguments.Length);
                        Assert.Equal("restore ", arguments[0]);
                        Assert.Equal("project.json", PathUtilities.GetFileName(arguments[1]));
                        Assert.Equal(" -ConfigFile ", arguments[2]);
                        Assert.Equal("nuget.config", PathUtilities.GetFileName(arguments[3]));
                        Assert.Equal(" -PackagesDirectory ", arguments[4]);
                        Assert.Equal(packagesDirectory, arguments[5]);
                        Assert.Equal("", arguments[6]);
                        // Verify project.json contents.
                        var projectJsonPath = arguments[1];
                        var actualProjectJson = File.ReadAllText(projectJsonPath);
                        Assert.Equal(expectedProjectJson, actualProjectJson);
                        // Verify config file contents.
                        var configPath = arguments[3];
                        var actualConfig = File.ReadAllText(configPath);
                        Assert.Equal(expectedConfig, actualConfig);
                        // Generate project.lock.json.
                        var projectLockJsonPath = PathUtilities.CombineAbsoluteAndRelativePaths(PathUtilities.GetDirectoryName(projectJsonPath), "project.lock.json");
                        using (var writer = new StreamWriter(projectLockJsonPath))
                        {
                            writer.Write(actualProjectLockJson);
                        }
                    });
                var actualPaths = resolver.ResolveNuGetPackage("A.B.C", "1.2");
                AssertEx.SetEqual(actualPaths,
                    PathUtilities.CombineAbsoluteAndRelativePaths(packagesDirectory, PathUtilities.CombinePossiblyRelativeAndRelativePaths("System.Collections/4.0.10", "ref/dotnet/System.Collections.dll")),
                    PathUtilities.CombineAbsoluteAndRelativePaths(packagesDirectory, PathUtilities.CombinePossiblyRelativeAndRelativePaths("System.IO/4.0.10", "ref/dotnet/System.Runtime.dll")),
                    PathUtilities.CombineAbsoluteAndRelativePaths(packagesDirectory, PathUtilities.CombinePossiblyRelativeAndRelativePaths("System.IO/4.0.10", "ref/dotnet/System.IO.dll")));
            }
        }

        /// <summary>
        /// Expected exception thrown during restore.
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly))]
        public void HandledException()
        {
            using (var directory = new DisposableDirectory(Temp))
            {
                bool restored = false;
                var resolver = new NuGetPackageResolverImpl(directory.Path, startInfo => { restored = true; throw new IOException(); });
                var actualPaths = resolver.ResolveNuGetPackage("A.B.C", "1.2");
                Assert.True(actualPaths.IsEmpty);
                Assert.True(restored);
            }
        }

        /// <summary>
        /// Unexpected exception thrown during restore.
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly))]
        public void UnhandledException()
        {
            using (var directory = new DisposableDirectory(Temp))
            {
                bool restored = false;
                var resolver = new NuGetPackageResolverImpl(directory.Path, startInfo => { restored = true; throw new InvalidOperationException(); });
                Assert.Throws<InvalidOperationException>(() => resolver.ResolveNuGetPackage("A.B.C", "1.2"));
                Assert.True(restored);
            }
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ParsePackageNameAndVersion()
        {
            ParseInvalidPackageReference("A");
            ParseInvalidPackageReference("A/1");
            ParseInvalidPackageReference("nuget");
            ParseInvalidPackageReference("nuget:");
            ParseInvalidPackageReference("NUGET:");
            ParseInvalidPackageReference("nugetA/1");

            ParseValidPackageReference("nuget:A", "A", "");
            ParseValidPackageReference("nuget:A.B", "A.B", "");
            ParseValidPackageReference("nuget:  ", "  ", "");

            ParseInvalidPackageReference("nuget:A/");
            ParseInvalidPackageReference("nuget:A//1.0");
            ParseInvalidPackageReference("nuget:/1.0.0");
            ParseInvalidPackageReference("nuget:A/B/2.0.0");

            ParseValidPackageReference("nuget::nuget/1", ":nuget", "1");
            ParseValidPackageReference("nuget:A/1", "A", "1");
            ParseValidPackageReference("nuget:A.B/1.0.0", "A.B", "1.0.0");
            ParseValidPackageReference("nuget:A/B.C", "A", "B.C");
            ParseValidPackageReference("nuget:  /1", "  ", "1");
            ParseValidPackageReference("nuget:A\t/\n1.0\r ", "A\t", "\n1.0\r ");
        }

        private static void ParseValidPackageReference(string reference, string expectedName, string expectedVersion)
        {
            string name;
            string version;
            Assert.True(NuGetPackageResolverImpl.TryParsePackageReference(reference, out name, out version));
            Assert.Equal(expectedName, name);
            Assert.Equal(expectedVersion, version);
        }

        private static void ParseInvalidPackageReference(string reference)
        {
            string name;
            string version;
            Assert.False(NuGetPackageResolverImpl.TryParsePackageReference(reference, out name, out version));
            Assert.Null(name);
            Assert.Null(version);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WriteProjectJson()
        {
            WriteProjectJsonPackageReference("A.B", "4.0.1",
@"{
  ""dependencies"": {
    ""A.B"": ""4.0.1""
  },
  ""frameworks"": {
    ""net46"": {}
  }
}");
            WriteProjectJsonPackageReference("A", "",
@"{
  ""dependencies"": {
    ""A"": """"
  },
  ""frameworks"": {
    ""net46"": {}
  }
}");
            WriteProjectJsonPackageReference("\n\t", "\"'",
@"{
  ""dependencies"": {
    ""\n\t"": ""\""'""
  },
  ""frameworks"": {
    ""net46"": {}
  }
}");
        }

        private static void WriteProjectJsonPackageReference(string packageName, string packageVersion, string expectedJson)
        {
            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                NuGetPackageResolverImpl.WriteProjectJson(writer, packageName, packageVersion);
            }
            var actualJson = builder.ToString();
            Assert.Equal(expectedJson, actualJson);
        }
    }
}
