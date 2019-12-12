// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.IO;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private class PkgDefFileReader : DisposableObject
        {
            private readonly TextReader _file;
            private string _currentSection = string.Empty;

            public PkgDefFileReader(TextReader file)
            {
                _file = file;
            }

            public bool EndOfFile { get; private set; } = false;

            protected override void DisposeManagedResources()
            {
                _file.Close();
                base.DisposeManagedResources();
            }

            public PkgDefItem? Read()
            {
                if (EndOfFile)
                    return null;

                var currentLine = string.Empty;
                do
                {
                    do
                    {
                        currentLine = currentLine.TrimEnd(Constants.ContinuationChar);
                        if (EndOfFile && currentLine == string.Empty)
                            return null;

                        var tempLine = _file.ReadLine();
                        if (tempLine == null)
                        {
                            EndOfFile = true;
                            tempLine = string.Empty;
                        }
                        currentLine += tempLine;
                    }
                    while (currentLine.EndsWith(@"\"));

                    currentLine = currentLine.TrimStart();

                    if (currentLine == string.Empty)
                        continue;

                    if (currentLine.StartsWith(Constants.CommentChars) || currentLine.StartsWith(Constants.IniCommentChar))
                        continue;

                    if (IsSectionLine(currentLine))
                    {
                        SetNewSection(currentLine);
                        currentLine = string.Empty;
                        continue;
                    }
                    else
                    {
                        return ParseNameValue(currentLine, _currentSection);
                    }
                }
                while (!EndOfFile);

                return null;
            }

            private string ParseSectionName(string line)
            {
                if (line.LastIndexOf(Constants.SectionEndChar) < 0)
                    return string.Empty;

                return line.Substring(1, line.LastIndexOf(Constants.SectionEndChar) - 1);
            }

            private PkgDefItem? ParseNameValue(string line, string sectionName)
            {
                if (line == string.Empty)
                    return null;

                var equalsIndex = line.IndexOf("=", (line[0] == '"') ? line.IndexOf("\"", 1) + 1 : 1);
                if (equalsIndex < 0)
                    return null;

                var valueName = line.Substring(0, equalsIndex).Trim();
                var valueDataString = line.Substring(equalsIndex + 1).Trim();

                if ((valueName != "@") && !StripQuotes(ref valueName))
                    return null;

                if (StripQuotes(ref valueDataString))
                {
                    return new PkgDefItem(sectionName, valueName, valueDataString, PkgDefItem.PkgDefValueType.String);
                }
                else if (valueDataString.StartsWith(Constants.ExpandSzPrefix))
                {
                    valueDataString = valueDataString.Substring(Constants.ExpandSzPrefix.Length);
                    valueDataString = valueDataString.Replace("\\\\", "\\");
                    StripQuotes(ref valueDataString);
                    return new PkgDefItem(sectionName, valueName, valueDataString, PkgDefItem.PkgDefValueType.ExpandSz);
                }
                else if (valueDataString.StartsWith("hex"))
                {
                    var colonIndex = valueDataString.IndexOf(":");
                    if (colonIndex < 0)
                        return null;

                    var binaryDataString = valueDataString.Substring(colonIndex + 1);
                    var binaryData = TransformHexToBinary(binaryDataString);

                    switch (valueDataString.Substring(0, colonIndex + 1))
                    {
                        case Constants.DwordBinaryPrefix:
                            {
                                if (binaryData.Length != 8)
                                    return null;

                                ulong val = 0;
                                for (var i = 0; i < binaryData.Length; i++)
                                {
                                    val = (val << 8) | binaryData[i];
                                }
                                return new PkgDefItem(sectionName, valueName, val, PkgDefItem.PkgDefValueType.QWord);
                            }
                        case Constants.ExpandSzBinaryPrefix:
                            {
                                return new PkgDefItem(sectionName, valueName,
                                                        System.Text.Encoding.UTF8.GetString(binaryData),
                                                        PkgDefItem.PkgDefValueType.ExpandSz);
                            }
                        case Constants.MultiSzPrefix1:
                        case Constants.MultiSzPrefix2:
                            {
                                var binaryDataLength = binaryData.Length;
                                if ((Convert.ToUInt32(binaryData.GetValue(binaryDataLength - 1)) != 0) &&
                                    (Convert.ToUInt32(binaryData.GetValue(binaryDataLength - 2)) != 0))
                                    return null;
                                return new PkgDefItem(sectionName, valueName,
                                                        System.Text.Encoding.UTF8.GetString(binaryData),
                                                        PkgDefItem.PkgDefValueType.MultiSz);
                            }
                        default:
                            break;
                    }

                    return new PkgDefItem(sectionName, valueName, binaryData, PkgDefItem.PkgDefValueType.Binary);
                }
                else if (valueDataString.StartsWith(Constants.DwordPrefix))
                {
                    var numericString = valueDataString.Substring(Constants.DwordPrefix.Length);
                    if (numericString.Length > 8)
                        return null;

                    var dword = Convert.ToUInt32(numericString, 16);
                    return new PkgDefItem(sectionName, valueName, dword, PkgDefItem.PkgDefValueType.DWord);
                }
                else if (valueDataString.StartsWith(Constants.QwordPrefix))
                {
                    var numericString = valueDataString.Substring(Constants.QwordPrefix.Length);
                    if (numericString.Length > 16)
                        return null;

                    var qword = Convert.ToUInt64(numericString, 16);
                    return new PkgDefItem(sectionName, valueName, qword, PkgDefItem.PkgDefValueType.QWord);
                }
                else
                {
                    return null;
                }
            }

            private void SetNewSection(string line)
            {
                _currentSection = ParseSectionName(line);
            }

            private bool IsSectionLine(string line)
            {
                return line.StartsWith(Constants.SectionStartChar);
            }

            private bool StripQuotes(ref string line)
            {
                if ((line.Length > 1) && line.StartsWith("\"") && line.EndsWith("\""))
                {
                    line = line.Substring(1, line.Length - 2);
                    return true;
                }
                return false;
            }

            private byte[] TransformHexToBinary(string hexString)
            {
                var normalizedString = string.Empty;

                var curPos = 0;
                while (curPos < hexString.Length)
                {
                    if (!IsValidHexChar(hexString[curPos]))
                    {
                        curPos++;
                        continue;
                    }

                    if ((curPos + 1 >= hexString.Length) ||
                        !IsValidHexChar(hexString[curPos + 1]))
                    {
                        //expecting a 2nd hex character
                        //throw?
                    }

                    normalizedString += hexString[curPos].ToString() + hexString[curPos + 1].ToString();
                    curPos += 2;
                }

                if (normalizedString.Length % 2 != 0)
                {
                    //throw?
                }

                var data = new byte[normalizedString.Length / 2];
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)((HexCharToByte(normalizedString[(2 * i)]) << 4) | HexCharToByte(normalizedString[(2 * i) + 1]));
                }

                return data;
            }

            private bool IsValidHexChar(char ch)
            {
                return (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F') || (ch >= '0' && ch <= '9');
            }

            private byte HexCharToByte(char ch)
            {
                if (ch >= '0' && ch <= '9')
                    return (byte)(ch - '0');
                else if (ch >= 'a' && ch <= 'f')
                    return (byte)(ch - 'a' + 10);
                else if (ch >= 'A' && ch <= 'F')
                    return (byte)(ch - 'A' + 10);
                else
                    return (byte)0;
            }

            private class Constants
            {
                public const string SectionStartChar = @"[";
                public const string SectionEndChar = @"]";
                public const string CommentChars = "//";
                public const string IniCommentChar = ";";
                public const string ExpandSzPrefix = "e:";
                public const string BinaryPrefix = "hex:";
                public const string DwordBinaryPrefix = "hex(b):";
                public const string ExpandSzBinaryPrefix = "hex(2):";
                public const string MultiSzPrefix1 = "hex(7):";
                public const string MultiSzPrefix2 = "hex(m):";
                public const string DwordPrefix = "dword:";
                public const string QwordPrefix = "qword:";

                public const char ContinuationChar = '\\';
            }
        }
    }
}
