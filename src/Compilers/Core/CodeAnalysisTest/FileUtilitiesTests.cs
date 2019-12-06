// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class FileUtilitiesTests
    {
        [ConditionalFact(typeof(WindowsOnly))]
        public void IsAbsolute()
        {
            Assert.False(PathUtilities.IsAbsolute(null));
            Assert.False(PathUtilities.IsAbsolute(""));
            Assert.False(PathUtilities.IsAbsolute("C"));
            Assert.False(PathUtilities.IsAbsolute("C:"));
            Assert.True(PathUtilities.IsAbsolute(@"C:\"));
            Assert.True(PathUtilities.IsAbsolute(@"C:/"));
            Assert.True(PathUtilities.IsAbsolute(@"C:\\"));
            Assert.False(PathUtilities.IsAbsolute(@"C\"));
            Assert.True(PathUtilities.IsAbsolute(@"\\"));                // incomplete UNC 
            Assert.True(PathUtilities.IsAbsolute(@"\\S"));               // incomplete UNC 
            Assert.True(PathUtilities.IsAbsolute(@"\/C"));               // incomplete UNC 
            Assert.True(PathUtilities.IsAbsolute(@"\/C\"));              // incomplete UNC 
            Assert.True(PathUtilities.IsAbsolute(@"\\server"));          // incomplete UNC 
            Assert.True(PathUtilities.IsAbsolute(@"\\server\share"));    // UNC
            Assert.True(PathUtilities.IsAbsolute(@"\\?\C:\share"));      // long UNC
            Assert.False(PathUtilities.IsAbsolute(@"\C"));
            Assert.False(PathUtilities.IsAbsolute(@"/C"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void GetPathRoot()
        {
            Assert.Null(PathUtilities.GetPathRoot(null));
            Assert.Equal("", PathUtilities.GetPathRoot(""));
            Assert.Equal("", PathUtilities.GetPathRoot("C"));
            Assert.Equal("", PathUtilities.GetPathRoot("abc.txt"));
            Assert.Equal("C:", PathUtilities.GetPathRoot("C:"));
            Assert.Equal(@"C:\", PathUtilities.GetPathRoot(@"C:\"));
            Assert.Equal(@"C:/", PathUtilities.GetPathRoot(@"C:/"));
            Assert.Equal(@"C:\", PathUtilities.GetPathRoot(@"C:\\"));
            Assert.Equal(@"C:/", PathUtilities.GetPathRoot(@"C:/\"));
            Assert.Equal(@"*:/", PathUtilities.GetPathRoot(@"*:/"));
            Assert.Equal(@"0:/", PathUtilities.GetPathRoot(@"0:/"));
            Assert.Equal(@"::/", PathUtilities.GetPathRoot(@"::/"));

            // '/' is an absolute path on unix-like systems
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    Assert.Equal("/", PathUtilities.GetPathRoot(@"/"));
                    Assert.Equal(@"/", PathUtilities.GetPathRoot(@"/x"));
                    // Be permissive of either directory separator, just
                    // like we are in other cases
                    Assert.Equal(@"\", PathUtilities.GetPathRoot(@"\"));
                    Assert.Equal(@"\", PathUtilities.GetPathRoot(@"\x"));
                    break;
                default:
                    Assert.Equal(@"\", PathUtilities.GetPathRoot(@"\"));
                    Assert.Equal(@"\", PathUtilities.GetPathRoot(@"\x"));
                    break;
            }
            Assert.Equal(@"\\", PathUtilities.GetPathRoot(@"\\"));
            Assert.Equal(@"\\x", PathUtilities.GetPathRoot(@"\\x"));
            Assert.Equal(@"\\x\", PathUtilities.GetPathRoot(@"\\x\"));
            Assert.Equal(@"\\x\y", PathUtilities.GetPathRoot(@"\\x\y"));
            Assert.Equal(@"\\\x\y", PathUtilities.GetPathRoot(@"\\\x\y"));
            Assert.Equal(@"\\\\x\y", PathUtilities.GetPathRoot(@"\\\\x\y"));
            Assert.Equal(@"\\x\\y", PathUtilities.GetPathRoot(@"\\x\\y"));
            Assert.Equal(@"\\/x\\/y", PathUtilities.GetPathRoot(@"\\/x\\/y"));
            Assert.Equal(@"\\/x\\/y", PathUtilities.GetPathRoot(@"\\/x\\/y/"));
            Assert.Equal(@"\\/x\\/y", PathUtilities.GetPathRoot(@"\\/x\\/y\/"));
            Assert.Equal(@"\\/x\\/y", PathUtilities.GetPathRoot(@"\\/x\\/y\/zzz"));
            Assert.Equal(@"\\x\y", PathUtilities.GetPathRoot(@"\\x\y"));
            Assert.Equal(@"\\x\y", PathUtilities.GetPathRoot(@"\\x\y\\"));
            Assert.Equal(@"\\abc\xyz", PathUtilities.GetPathRoot(@"\\abc\xyz"));
            Assert.Equal(@"\\server\$c", PathUtilities.GetPathRoot(@"\\server\$c\Public"));
            // TODO (tomat): long UNC paths
            // Assert.Equal(@"\\?\C:\", PathUtilities.GetPathRoot(@"\\?\C:\abc\def"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void CombinePaths()
        {
            Assert.Equal(@"C:\x/y", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x/y", @""));
            Assert.Equal(@"C:\x/y", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x/y", null));
            Assert.Equal(@"C:\x/y", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x/y", null));

            Assert.Null(PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\", @"C:\goo"));
            Assert.Null(PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\", @"C:goo"));
            Assert.Null(PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\", @"\goo"));

            Assert.Equal(@"C:\x\y\goo", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x\y", @"goo"));
            Assert.Equal(@"C:\x/y\goo", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x/y", @"goo"));
            Assert.Equal(@"C:\x/y\.\goo", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x/y", @".\goo"));
            Assert.Equal(@"C:\x/y\./goo", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x/y", @"./goo"));
            Assert.Equal(@"C:\x/y\..\goo", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x/y", @"..\goo"));
            Assert.Equal(@"C:\x/y\../goo", PathUtilities.CombineAbsoluteAndRelativePaths(@"C:\x/y", @"../goo"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ResolveRelativePath()
        {
            string baseDir = @"X:\rootdir\dir";
            string[] noSearchPaths = new string[0];

            // absolute path:
            TestPath(@"C:\abc\def.dll", @"Q:\baz\x.csx", baseDir, noSearchPaths, @"C:\abc\def.dll");
            TestPath(@"C:\abc\\\\\def.dll", @"Q:\baz\x.csx", baseDir, noSearchPaths, @"C:\abc\\\\\def.dll");

            // root-relative path:
            TestPath(@"\abc\def.dll", @"Q:\baz\x.csx", baseDir, noSearchPaths, @"Q:\abc\def.dll");
            TestPath(@"\abc\def.dll", null, baseDir, noSearchPaths, @"X:\abc\def.dll");
            TestPath(@"\abc\def.dll", "goo.csx", null, noSearchPaths, null);
            // TestPath(@"\abc\def.dll", @"C:goo.csx", null, noSearchPaths, null);
            // TestPath(@"/abc\def.dll", @"\goo.csx", null, noSearchPaths, null);
            TestPath(@"/abc\def.dll", null, @"\\x\y\z", noSearchPaths, @"\\x\y\abc\def.dll");
            TestPath(@"/abc\def.dll", null, null, noSearchPaths, null);
            TestPath(@"/**/", null, baseDir, noSearchPaths, @"X:\**/");
            TestPath(@"/a/z.txt", null, @"?:\*\<>", noSearchPaths, @"?:\a/z.txt");

            // UNC path:
            TestPath(@"\abc\def.dll", @"\\mymachine\root\x.csx", baseDir, noSearchPaths, @"\\mymachine\root\abc\def.dll");
            TestPath(@"\abc\def.dll", null, @"\\mymachine\root\x.csx", noSearchPaths, @"\\mymachine\root\abc\def.dll");
            TestPath(@"\\abc\def\baz.dll", null, @"\\mymachine\root\x.csx", noSearchPaths, @"\\abc\def\baz.dll");

            // incomplete UNC paths (considered absolute and returned as they are):
            TestPath(@"\\", null, @"\\mymachine\root\x.csx", noSearchPaths, @"\\");
            TestPath(@"\\goo", null, @"\\mymachine\root\x.csx", noSearchPaths, @"\\goo");

            // long UNC path:
            // TODO (tomat): 
            // Doesn't work since "?" in paths is not handled by BCL
            // TestPath(resolver, @"\abc\def.dll", @"\\?\C:\zzz\x.csx", @"\\?\C:\abc\def.dll");

            TestPath(@"./def.dll", @"Q:\abc\x.csx", baseDir, noSearchPaths, @"Q:\abc\./def.dll");
            TestPath(@"./def.dll", @"Q:\abc\x.csx", baseDir, noSearchPaths, @"Q:\abc\./def.dll");
            TestPath(@".", @"Q:\goo\x.csx", baseDir, noSearchPaths, @"Q:\goo");
            TestPath(@"..", @"Q:\goo\x.csx", baseDir, noSearchPaths, @"Q:\goo\..");  // doesn't normalize
            TestPath(@".\", @"Q:\goo\x.csx", baseDir, noSearchPaths, @"Q:\goo\.\");
            TestPath(@"..\", @"Q:\goo\x.csx", baseDir, noSearchPaths, @"Q:\goo\..\"); // doesn't normalize

            // relative base paths:
            TestPath(@".\y.dll", @"x.csx", baseDir, noSearchPaths, @"X:\rootdir\dir\.\y.dll");
            TestPath(@".\y.dll", @"goo\x.csx", baseDir, noSearchPaths, @"X:\rootdir\dir\goo\.\y.dll");
            TestPath(@".\y.dll", @".\goo\x.csx", baseDir, noSearchPaths, @"X:\rootdir\dir\.\goo\.\y.dll");
            TestPath(@".\y.dll", @"..\x.csx", baseDir, noSearchPaths, @"X:\rootdir\dir\..\.\y.dll"); // doesn't normalize
            TestPath(@".\\y.dll", @"..\x.csx", baseDir, noSearchPaths, @"X:\rootdir\dir\..\.\\y.dll"); // doesn't normalize
            TestPath(@".\/y.dll", @"..\x.csx", baseDir, noSearchPaths, @"X:\rootdir\dir\..\.\/y.dll"); // doesn't normalize
            TestPath(@"..\y.dll", @"..\x.csx", baseDir, noSearchPaths, @"X:\rootdir\dir\..\..\y.dll"); // doesn't normalize

            // unqualified relative path, look in base directory:
            TestPath(@"y.dll", @"x.csx", baseDir, noSearchPaths, @"X:\rootdir\dir\y.dll");
            TestPath(@"y.dll", @"x.csx", baseDir, new[] { @"Z:\" }, @"X:\rootdir\dir\y.dll");

            // drive-relative path (not supported -> null)
            TestPath(@"C:y.dll", @"x.csx", baseDir, noSearchPaths, null);
            TestPath("C:\tools\\", null, @"d:\z", noSearchPaths, null);

            // invalid paths

            Assert.Equal(PathKind.RelativeToCurrentRoot, PathUtilities.GetPathKind(@"/c:x.dll"));
            TestPath(@"/c:x.dll", null, @"d:\", noSearchPaths, @"d:\c:x.dll");

            Assert.Equal(PathKind.RelativeToCurrentRoot, PathUtilities.GetPathKind(@"/:x.dll"));
            TestPath(@"/:x.dll", null, @"d:\", noSearchPaths, @"d:\:x.dll");

            Assert.Equal(PathKind.Absolute, PathUtilities.GetPathKind(@"//:x.dll"));
            TestPath(@"//:x.dll", null, @"d:\", noSearchPaths, @"//:x.dll");

            Assert.Equal(PathKind.RelativeToDriveDirectory, PathUtilities.GetPathKind(@"c::x.dll"));
            TestPath(@"c::x.dll", null, @"d:\", noSearchPaths, null);

            Assert.Equal(PathKind.RelativeToCurrentDirectory, PathUtilities.GetPathKind(@".\:x.dll"));
            TestPath(@".\:x.dll", null, @"d:\z", noSearchPaths, @"d:\z\.\:x.dll");

            Assert.Equal(PathKind.RelativeToCurrentParent, PathUtilities.GetPathKind(@"..\:x.dll"));
            TestPath(@"..\:x.dll", null, @"d:\z", noSearchPaths, @"d:\z\..\:x.dll");

            // empty paths
            Assert.Equal(PathKind.Empty, PathUtilities.GetPathKind(@""));
            TestPath(@"", @"c:\temp", @"d:\z", noSearchPaths, null);

            Assert.Equal(PathKind.Empty, PathUtilities.GetPathKind(" \t\r\n "));
            TestPath(" \t\r\n ", @"c:\temp", @"d:\z", noSearchPaths, null);
        }

        private void TestPath(string path, string basePath, string baseDirectory, IEnumerable<String> searchPaths, string expected)
        {
            string actual = FileUtilities.ResolveRelativePath(path, basePath, baseDirectory, searchPaths, p => true);

            Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        private void TestGetExtension(string path, string expected)
        {
            Assert.Equal(expected, PathUtilities.GetExtension(path));
            Assert.Equal(expected, Path.GetExtension(path));
        }

        private void TestRemoveExtension(string path, string expected)
        {
            Assert.Equal(expected, PathUtilities.RemoveExtension(path));
            Assert.Equal(expected, Path.GetFileNameWithoutExtension(path));
        }

        private void TestChangeExtension(string path, string extension, string expected)
        {
            Assert.Equal(expected, PathUtilities.ChangeExtension(path, extension));
            Assert.Equal(expected, Path.ChangeExtension(path, extension));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void Extension()
        {
            TestGetExtension(path: "a.dll", expected: ".dll");
            TestGetExtension(path: "a.exe.config", expected: ".config");
            TestGetExtension(path: ".goo", expected: ".goo");
            TestGetExtension(path: ".goo.dll", expected: ".dll");
            TestGetExtension(path: "goo", expected: "");
            TestGetExtension(path: "goo.", expected: "");
            TestGetExtension(path: "goo..", expected: "");
            TestGetExtension(path: "goo...", expected: "");

            Assert.Equal(".dll", PathUtilities.GetExtension("*.dll"));

            TestRemoveExtension(path: "a.dll", expected: "a");
            TestRemoveExtension(path: "a.exe.config", expected: "a.exe");
            TestRemoveExtension(path: ".goo", expected: "");
            TestRemoveExtension(path: ".goo.dll", expected: ".goo");
            TestRemoveExtension(path: "goo", expected: "goo");
            TestRemoveExtension(path: "goo.", expected: "goo");
            TestRemoveExtension(path: "goo..", expected: "goo.");
            TestRemoveExtension(path: "goo...", expected: "goo..");

            Assert.Equal("*", PathUtilities.RemoveExtension("*.dll"));

            TestChangeExtension(path: "a.dll", extension: ".exe", expected: "a.exe");
            TestChangeExtension(path: "a.dll", extension: "exe", expected: "a.exe");
            TestChangeExtension(path: "a.dll", extension: "", expected: "a.");
            TestChangeExtension(path: "a.dll", extension: ".", expected: "a.");
            TestChangeExtension(path: "a.dll", extension: "..", expected: "a..");
            TestChangeExtension(path: "a.dll", extension: "...", expected: "a...");
            TestChangeExtension(path: "a.dll", extension: " ", expected: "a. ");

            TestChangeExtension(path: "a", extension: ".exe", expected: "a.exe");
            TestChangeExtension(path: "a.", extension: "exe", expected: "a.exe");
            TestChangeExtension(path: "a..", extension: "exe", expected: "a..exe");
            TestChangeExtension(path: "a.", extension: "e.x.e", expected: "a.e.x.e");
            TestChangeExtension(path: ".", extension: "", expected: ".");
            TestChangeExtension(path: "..", extension: ".", expected: "..");
            TestChangeExtension(path: "..", extension: "..", expected: "...");

            TestChangeExtension(path: "", extension: "", expected: "");
            TestChangeExtension(path: null, extension: "", expected: null);
            TestChangeExtension(path: null, extension: null, expected: null);

            Assert.Equal("*", PathUtilities.RemoveExtension("*.dll"));
        }
    }
}
