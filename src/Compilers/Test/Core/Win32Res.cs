// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class Win32Res
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
        [DllImport("kernel32.dll")]
        private static extern IntPtr LockResource(IntPtr hResData);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        public static IntPtr GetResource(IntPtr lib, string resourceId, string resourceType, out uint size)
        {
            IntPtr hrsrc = FindResource(lib, resourceId, resourceType);
            if (hrsrc == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            size = SizeofResource(lib, hrsrc);
            IntPtr resource = LoadResource(lib, hrsrc);
            if (resource == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            IntPtr manifest = LockResource(resource);
            if (resource == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return manifest;
        }

        private static string GetManifestString(IntPtr ptr, int offset, int length, Encoding encoding)
        {
            byte[] fullmanif = new byte[length];
            Marshal.Copy((IntPtr)(ptr.ToInt64() + offset), fullmanif, 0, length);
            return encoding.GetString(fullmanif, 0, length);
        }

        private static string GetDecodedManifest(IntPtr mfRsrc, uint rsrcSize)
        {
            byte[] ar = new byte[3];
            Marshal.Copy(mfRsrc, ar, 0, 3);
            string xmlManif;
            Encoding encoding;
            //If unicode (little endian)
            if (ar[0] == 0xFF && ar[1] == 0xFE)
            {
                encoding = Encoding.Unicode;
                xmlManif = GetManifestString(mfRsrc, 2, (int)rsrcSize - 2, encoding);
            }
            // if unicode (big endian)
            else if (ar[0] == 0xFE && ar[1] == 0xFF)
            {
                encoding = Encoding.BigEndianUnicode;
                xmlManif = GetManifestString(mfRsrc, 2, (int)rsrcSize - 2, encoding);
            }
            // if UTF-8
            else if (ar[0] == 0xEF && ar[1] == 0xBB && ar[2] == 0xBF)
            {
                encoding = Encoding.UTF8;
                xmlManif = GetManifestString(mfRsrc, 3, (int)rsrcSize - 3, encoding);
            }
            // We give up! Assume ASCII (which may or may not always be true...)
            else
            {
                encoding = Encoding.ASCII;
                xmlManif = GetManifestString(mfRsrc, 0, (int)rsrcSize, encoding);
            }

            return xmlManif;
        }

        public static string ManifestResourceToXml(IntPtr mftRsrc, uint size)
        {
            var doc = new XDocument();
            using (XmlWriter xw = doc.CreateWriter())
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("ManifestResource");
                xw.WriteAttributeString("Size", size.ToString());
                xw.WriteStartElement("Contents");
                xw.WriteCData(GetDecodedManifest(mftRsrc, size));
                xw.WriteEndElement();

                //xw.WriteAttributeString("Data", GetDecodedManifest(mftRsrc, size));

                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
            var sw = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            doc.Save(sw);
            return sw.ToString();
        }

        private static string ReadString(BinaryReader reader)
        {
            int i = 0;
            var cbuffer = new char[16];
            do
            {
                cbuffer[i] = reader.ReadChar();
            }
            while (cbuffer[i] != '\0' && ++i < cbuffer.Length);

            return new string(cbuffer).TrimEnd(new char[] { '\0' });
        }

        private static void ReadVarFileInfo(BinaryReader reader)
        {
            ushort us;
            string s;

            us = reader.ReadUInt16();
            us = reader.ReadUInt16();    //0
            us = reader.ReadUInt16();    //1
            s = ReadString(reader);            //"Translation"
            reader.BaseStream.Position = (reader.BaseStream.Position + 3) & ~3; //round up to 32bit boundary

            us = reader.ReadUInt16();    //langId; MAKELANGID(LANG_NEUTRAL, SUBLANG_NEUTRAL)) = 0
            us = reader.ReadUInt16();    //codepage; 1200 = CP_WINUNICODE
        }

        public static IEnumerable<Tuple<string, string>> ReadStringFileInfo(BinaryReader reader, int sizeTotalStringFileInfo)
        {
            var result = new List<Tuple<string, string>>();
            int sizeConsumed = 2 + 2 + 2 + (16 * 2);
            long startPosition = reader.BaseStream.Position;

            string s;

            reader.ReadUInt16();   //length
            reader.ReadUInt16();    //0
            reader.ReadUInt16();    //1
            s = ReadString(reader);    //"12345678"
            reader.BaseStream.Position = (reader.BaseStream.Position + 3) & ~3; //round up to 32bit boundary

            while (reader.BaseStream.Position - startPosition + sizeConsumed < sizeTotalStringFileInfo)
            {
                result.Add(GetVerStringPair(reader));
                reader.BaseStream.Position = (reader.BaseStream.Position + 3) & ~3; //round up to 32bit boundary
            }

            return result;
        }

        public static string VersionResourceToXml(IntPtr versionRsrc)
        {
            var shortArray = new short[1];
            Marshal.Copy(versionRsrc, shortArray, 0, 1);
            int size = shortArray[0];

            var entireResourceBytes = new byte[size];
            Marshal.Copy(versionRsrc, entireResourceBytes, 0, entireResourceBytes.Length);

            var memoryStream = new MemoryStream(entireResourceBytes);
            var reader = new BinaryReader(memoryStream, Encoding.Unicode);

            var doc = new XDocument();
            using (XmlWriter xw = doc.CreateWriter())
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("VersionResource");
                xw.WriteAttributeString("Size", size.ToString());

                //0x28 is the start of the VS_FIXEDFILEINFO
                reader.BaseStream.Seek(0x28, SeekOrigin.Begin);
                //skip the first two dwords of VS_FIXEDFILEINFO.
                reader.BaseStream.Seek(0x8, SeekOrigin.Current);

                xw.WriteStartElement("VS_FIXEDFILEINFO");
                xw.WriteAttributeString("FileVersionMS", String.Format("{0:x8}", reader.ReadUInt32()));
                xw.WriteAttributeString("FileVersionLS", String.Format("{0:x8}", reader.ReadUInt32()));
                xw.WriteAttributeString("ProductVersionMS", String.Format("{0:x8}", reader.ReadUInt32()));
                xw.WriteAttributeString("ProductVersionLS", String.Format("{0:x8}", reader.ReadUInt32()));
                xw.WriteEndElement();

                long l;
                l = reader.ReadUInt32(); //((DWORD)0x0000003F);   //VS_FFI_FILEFLAGSMASK  (EDMAURER) really? all these bits are valid?
                l = reader.ReadUInt32(); //((DWORD)0);    //file flags
                l = reader.ReadUInt32(); //((DWORD)0x00000004);   //VOS__WINDOWS32
                l = reader.ReadUInt32(); //((DWORD)this.FileType);
                l = reader.ReadUInt32(); //((DWORD)0);    //file subtype
                l = reader.ReadUInt32(); //((DWORD)0);    //date most sig
                l = reader.ReadUInt32(); //((DWORD)0);    //date least sig

                //VS_VERSIONINFO can have zero or one VarFileInfo
                //and zero or one StringFileInfo
                while (reader.BaseStream.Position < size)
                {
                    ushort us;
                    string s;

                    ushort length = reader.ReadUInt16();
                    us = reader.ReadUInt16();    //0
                    us = reader.ReadUInt16();    //1
                    //must decide if this is a VarFileInfo or a StringFileInfo

                    s = ReadString(reader);
                    reader.BaseStream.Position = (reader.BaseStream.Position + 3) & ~3; //round up to 32bit boundary

                    IEnumerable<Tuple<string, string>> keyValPairs;
                    if (s == "VarFileInfo")
                        ReadVarFileInfo(reader);
                    else if (s == "StringFileInfo")
                    {
                        keyValPairs = ReadStringFileInfo(reader, length);
                        foreach (var pair in keyValPairs)
                        {
                            xw.WriteStartElement("KeyValuePair");
                            xw.WriteAttributeString("Key", pair.Item1.TrimEnd(new char[] { '\0' }));
                            xw.WriteAttributeString("Value", pair.Item2.TrimEnd(new char[] { '\0' }));
                            xw.WriteEndElement();
                        }
                    }
                }

                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
            var sw = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            doc.Save(sw);
            return sw.ToString();
        }

        private static Tuple<string, string> GetVerStringPair(BinaryReader reader)
        {
            System.Diagnostics.Debug.Assert((reader.BaseStream.Position & 3) == 0);
            long startPos = reader.BaseStream.Position;

            int length = reader.ReadUInt16();       //the whole structure in bytes, does not include padding
                                                    //at the end to bring the structure to a dword boundary.

            int valueLength = reader.ReadUInt16();  //in words

            System.Diagnostics.Debug.Assert(length > valueLength);

            int type = reader.ReadUInt16();

            System.Diagnostics.Debug.Assert(type == 1); //string

            int keyLength = length - (valueLength * 2) - (3 * sizeof(short));

            char[] key = new char[keyLength / sizeof(char)];
            for (int i = 0; i < key.Length; i++)
                key[i] = reader.ReadChar();

            reader.BaseStream.Position = (reader.BaseStream.Position + 3) & ~3;

            char[] value = new char[valueLength];
            for (int i = 0; i < value.Length; i++)
                value[i] = reader.ReadChar();

            System.Diagnostics.Debug.Assert(length == (reader.BaseStream.Position - startPos));
            return new Tuple<string, string>(new string(key), new string(value));
        }
    }
}
