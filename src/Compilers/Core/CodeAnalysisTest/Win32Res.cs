// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class Win32ResTests
    {
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void BasicResources2()
        {
            //confirm that we can read resources produced by RC.EXE. 
            var res = Resources.ResourceManager.GetObject("VerResourceBuiltByRC");
            var list = CvtResFile.ReadResFile(new System.IO.MemoryStream((byte[])res));
            Assert.Equal(3, list.Count);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void BasicResourcesWithStringTypes()
        {
            //confirm that we can read resources produced by RC.EXE. 
            var res = Resources.ResourceManager.GetObject("nativeWithStringIDsAndTypesAndIntTypes");
            var list = CvtResFile.ReadResFile(new System.IO.MemoryStream((byte[])res));

            Assert.Equal(3, list.Count);
            Assert.Equal(16, list[0].pstringType.Ordinal);
            Assert.Equal("IMAGE", list[1].pstringType.theString);
            Assert.Equal(0xFFFF, list[1].pstringType.Ordinal);
            Assert.Equal("BACKGROUND_GRADIENT.JPG", list[1].pstringName.theString);
            Assert.Equal(0xFFFF, list[1].pstringName.Ordinal);
            Assert.Equal("IMAGE", list[2].pstringType.theString);
            Assert.Equal(0xFFFF, list[2].pstringType.Ordinal);
            Assert.Equal("INFO.PNG", list[2].pstringName.theString);
            Assert.Equal(0xFFFF, list[2].pstringName.Ordinal);
        }

        private IEnumerable<Microsoft.Cci.IWin32Resource> BuildResources()
        {
            yield return new Win32Resource(null, 0, 0, -1, "goo", 1, null);//4
            yield return new Win32Resource(null, 0, 0, -1, "b", -1, "a");//0
            yield return new Win32Resource(null, 0, 0, 1, null, 1, null);//5
            yield return new Win32Resource(null, 0, 0, -1, "b", 2, null);//6
            yield return new Win32Resource(null, 0, 0, -1, "B", 1, null);//3
            yield return new Win32Resource(null, 0, 0, -1, "b", -1, "A");//1
            yield return new Win32Resource(null, 0, 0, 1, null, -1, "A");//2
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void EnsureResourceSorting()
        {
            //confirm that we sort the resources in the order required by the serialization format.
            var resources = Cci.NativeResourceWriter.SortResources(BuildResources()).ToArray();

            var elem = resources[0];
            Assert.Equal("a", elem.TypeName);
            Assert.Equal("b", elem.Name);
            elem = resources[1];
            Assert.Equal("A", elem.TypeName);
            Assert.Equal("b", elem.Name);
            elem = resources[2];
            Assert.Equal("A", elem.TypeName);
            Assert.Equal(1, elem.Id);
            elem = resources[3];
            Assert.Equal(1, elem.TypeId);
            Assert.Equal("B", elem.Name);
            elem = resources[4];
            Assert.Equal(1, elem.TypeId);
            Assert.Equal("goo", elem.Name);
            elem = resources[5];
            Assert.Equal(1, elem.TypeId);
            Assert.Equal(1, elem.Id);
            elem = resources[6];
            Assert.Equal(2, elem.TypeId);
            Assert.Equal("b", elem.Name);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void BasicResources()
        {
            System.IO.MemoryStream strm = new System.IO.MemoryStream();
            Microsoft.CodeAnalysis.Compilation.AppendNullResource(strm);

            //choose version values such that they would cause overflow when shifted as an int
            Win32ResourceConversions.AppendVersionToResourceStream(strm,
                true,
                "41220.41221.41222.41223",
                "originalFilenameMuddy.dll",
                "internalNameZep.dll",
                "41224.41225.41226.41227",  //4 ints
                new Version(41220, 41220, 41220, 41220),
                "this is the file description",   //the old compiler put blank here if nothing was user-supplied
                "this is the legal copyright",    //the old compiler put blank here if nothing was user-supplied
                "this is the legal trademark",
                "product name the testproduct",
                "some comments",
                "testcompany");

            var xmlExpectedVersion = @"<?xml version=""1.0"" encoding=""utf-16""?>
<VersionResource Size=""1124"">
  <VS_FIXEDFILEINFO FileVersionMS=""a104a105"" FileVersionLS=""a106a107"" ProductVersionMS=""a108a109"" ProductVersionLS=""a10aa10b"" />
  <KeyValuePair Key=""Comments"" Value=""some comments"" />
  <KeyValuePair Key=""CompanyName"" Value=""testcompany"" />
  <KeyValuePair Key=""FileDescription"" Value=""this is the file description"" />
  <KeyValuePair Key=""FileVersion"" Value=""41220.41221.41222.41223"" />
  <KeyValuePair Key=""InternalName"" Value=""internalNameZep.dll"" />
  <KeyValuePair Key=""LegalCopyright"" Value=""this is the legal copyright"" />
  <KeyValuePair Key=""LegalTrademarks"" Value=""this is the legal trademark"" />
  <KeyValuePair Key=""OriginalFilename"" Value=""originalFilenameMuddy.dll"" />
  <KeyValuePair Key=""ProductName"" Value=""product name the testproduct"" />
  <KeyValuePair Key=""ProductVersion"" Value=""41224.41225.41226.41227"" />
  <KeyValuePair Key=""Assembly Version"" Value=""41220.41220.41220.41220"" />
</VersionResource>";

            System.IO.MemoryStream mft = new System.IO.MemoryStream();
            var manifestContents = Resources.ResourceManager.GetObject("defaultWin32Manifest");

            Win32ResourceConversions.AppendManifestToResourceStream(strm, new System.IO.MemoryStream((byte[])manifestContents), false);

            var icon = Resources.ResourceManager.GetObject("Roslyn_ico");

            Win32ResourceConversions.AppendIconToResourceStream(strm, new System.IO.MemoryStream((byte[])icon));

            strm.Position = 0;

            var resources = CvtResFile.ReadResFile(strm);

            foreach (var r in resources)
            {
                if (r.pstringType.Ordinal == 16)    //version
                {
                    string rsrcInXml;

                    unsafe
                    {
                        fixed (byte* p = (r.data))
                            rsrcInXml = Win32Res.VersionResourceToXml((IntPtr)p);
                    }

                    Assert.Equal(xmlExpectedVersion, rsrcInXml);
                }
                else if (r.pstringType.Ordinal == 24)    //manifest
                {
                    Assert.Equal((byte[])manifestContents, r.data);
                }
                else if (r.pstringType.Ordinal == 14)    //icon group
                {
                    //first three words of the data contain 0, 1, iconCount.
                    short[] threeWords = new short[3];

                    unsafe
                    {
                        fixed (byte* p = (r.data))
                        {
                            Marshal.Copy((IntPtr)p, threeWords, 0, 3);
                        }
                    }

                    //find all of the individual icons in the resources.

                    for (short i = 0; i < threeWords[2]; i++)
                    {
                        bool found = false;
                        foreach (var rInner in resources)
                        {
                            if ((rInner.pstringName.Ordinal == i + 1) &&    //ICON IDs start at 1
                                (rInner.pstringType.Ordinal == 3))  //RT_ICON
                            {
                                found = true;
                                break;
                            }
                        }

                        Assert.True(found);
                    }
                }
            }
        }
    }
}
