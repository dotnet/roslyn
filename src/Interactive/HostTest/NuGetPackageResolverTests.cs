// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Interactive;
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
                        var arguments = startInfo.Arguments.Split('"');
                        Assert.Equal(5, arguments.Length);
                        Assert.Equal("restore ", arguments[0]);
                        Assert.Equal("project.json", PathUtilities.GetFileName(arguments[1]));
                        Assert.Equal(" -PackagesDirectory ", arguments[2]);
                        Assert.Equal(packagesDirectory, arguments[3]);
                        Assert.Equal("", arguments[4]);
                        var projectJsonPath = arguments[1];
                        var actualProjectJson = File.ReadAllText(projectJsonPath);
                        Assert.Equal(expectedProjectJson, actualProjectJson);
                        var projectLockJsonPath = PathUtilities.CombineAbsoluteAndRelativePaths(PathUtilities.GetDirectoryName(projectJsonPath), "project.lock.json");
                        using (var writer = new StreamWriter(projectLockJsonPath))
                        {
                            writer.Write(actualProjectLockJson);
                        }
                    });
                var actualPaths = resolver.ResolveNuGetPackage("A.B.C/1.2");
                AssertEx.SetEqual(actualPaths,
                    PathUtilities.CombineAbsoluteAndRelativePaths(packagesDirectory, PathUtilities.CombinePossiblyRelativeAndRelativePaths("System.Collections/4.0.10", "ref/dotnet/System.Collections.dll")),
                    PathUtilities.CombineAbsoluteAndRelativePaths(packagesDirectory, PathUtilities.CombinePossiblyRelativeAndRelativePaths("System.IO/4.0.10", "ref/dotnet/System.Runtime.dll")),
                    PathUtilities.CombineAbsoluteAndRelativePaths(packagesDirectory, PathUtilities.CombinePossiblyRelativeAndRelativePaths("System.IO/4.0.10", "ref/dotnet/System.IO.dll")));
            }
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void HandledException()
        {
            using (var directory = new DisposableDirectory(Temp))
            {
                var resolver = new NuGetPackageResolverImpl(directory.Path, startInfo => { throw new IOException(); });
                var actualPaths = resolver.ResolveNuGetPackage("A.B.C/1.2");
                Assert.True(actualPaths.IsDefault);
            }
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void UnhandledException()
        {
            using (var directory = new DisposableDirectory(Temp))
            {
                var resolver = new NuGetPackageResolverImpl(directory.Path, startInfo => { throw new InvalidOperationException(); });
                Assert.Throws<InvalidOperationException>(() => resolver.ResolveNuGetPackage("A.B.C/1.2"));
            }
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ParsePackageNameAndVersion()
        {
            ParseInvalidPackageReference("A");
            ParseInvalidPackageReference("A.B");
            ParseInvalidPackageReference("A/");
            ParseInvalidPackageReference("A//1.0");
            ParseInvalidPackageReference("/1.0.0");
            ParseInvalidPackageReference("A/B/2.0.0");

            ParseValidPackageReference("A/1", "A", "1");
            ParseValidPackageReference("A.B/1.0.0", "A.B", "1.0.0");
            ParseValidPackageReference("A/B.C", "A", "B.C");
            ParseValidPackageReference("  /1", "  ", "1");
            ParseValidPackageReference("A\t/\n1.0\r ", "A\t", "\n1.0\r ");
        }

        private static void ParseValidPackageReference(string reference, string expectedName, string expectedVersion)
        {
            string name;
            string version;
            Assert.True(NuGetPackageResolverImpl.ParsePackageReference(reference, out name, out version));
            Assert.Equal(expectedName, name);
            Assert.Equal(expectedVersion, version);
        }

        private static void ParseInvalidPackageReference(string reference)
        {
            string name;
            string version;
            Assert.False(NuGetPackageResolverImpl.ParsePackageReference(reference, out name, out version));
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
