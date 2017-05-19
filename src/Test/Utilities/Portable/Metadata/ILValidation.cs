// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using Microsoft.Metadata.Tools;

namespace Roslyn.Test.Utilities
{
    public static class ILValidation
    {
        public static unsafe string GetMethodIL(this ImmutableArray<byte> ilArray)
        {
            var result = new StringBuilder();
            fixed (byte* ilPtr = ilArray.ToArray())
            {
                int offset = 0;
                while (true)
                {
                    // skip padding:
                    while (offset < ilArray.Length && ilArray[offset] == 0)
                    {
                        offset++;
                    }

                    if (offset == ilArray.Length)
                    {
                        break;
                    }

                    var reader = new BlobReader(ilPtr + offset, ilArray.Length - offset);
                    var methodIL = MethodBodyBlock.Create(reader);

                    if (methodIL == null)
                    {
                        result.AppendFormat("<invalid byte 0x{0:X2} at offset {1}>", ilArray[offset], offset);
                        offset++;
                    }
                    else
                    {
                        ILVisualizer.Default.DumpMethod(
                            result,
                            methodIL.MaxStack,
                            methodIL.GetILContent(),
                            ImmutableArray.Create<ILVisualizer.LocalInfo>(),
                            ImmutableArray.Create<ILVisualizer.HandlerSpan>());

                        offset += methodIL.Size;
                    }
                }
            }

            return result.ToString();
        }

        public static Dictionary<int, string> GetSequencePointMarkers(string pdbXml, string source = null)
        {
            string[] lines = source?.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var doc = new XmlDocument();
            doc.LoadXml(pdbXml);
            var result = new Dictionary<int, string>();

            if (source == null)
            {
                foreach (XmlNode entry in doc.GetElementsByTagName("sequencePoints"))
                {
                    foreach (XmlElement item in entry.ChildNodes)
                    {
                        Add(result,
                            Convert.ToInt32(item.GetAttribute("offset"), 16),
                            (item.GetAttribute("hidden") == "true") ? "~" : "-");
                    }
                }

                foreach (XmlNode entry in doc.GetElementsByTagName("asyncInfo"))
                {
                    foreach (XmlElement item in entry.ChildNodes)
                    {
                        if (item.Name == "await")
                        {
                            Add(result, Convert.ToInt32(item.GetAttribute("yield"), 16), "<");
                            Add(result, Convert.ToInt32(item.GetAttribute("resume"), 16), ">");
                        }
                        else if (item.Name == "catchHandler")
                        {
                            Add(result, Convert.ToInt32(item.GetAttribute("offset"), 16), "$");
                        }
                    }
                }
            }
            else
            {
                foreach (XmlNode entry in doc.GetElementsByTagName("asyncInfo"))
                {
                    foreach (XmlElement item in entry.ChildNodes)
                    {
                        if (item.Name == "await")
                        {
                            AddTextual(result, Convert.ToInt32(item.GetAttribute("yield"), 16), "async: yield");
                            AddTextual(result, Convert.ToInt32(item.GetAttribute("resume"), 16), "async: resume");
                        }
                        else if (item.Name == "catchHandler")
                        {
                            AddTextual(result, Convert.ToInt32(item.GetAttribute("offset"), 16), "async: catch handler");
                        }
                    }
                }

                foreach (XmlNode entry in doc.GetElementsByTagName("sequencePoints"))
                {
                    foreach (XmlElement item in entry.ChildNodes)
                    {
                        AddTextual(result, Convert.ToInt32(item.GetAttribute("offset"), 16), "sequence point: " + SnippetFromSpan(lines, item));
                    }
                }
            }

            return result;

            void Add(Dictionary<int, string> dict, int key, string value)
            {
                if (dict.TryGetValue(key, out string found))
                {
                    dict[key] = found + value;
                }
                else
                {
                    dict[key] = value;
                }
            }

            void AddTextual(Dictionary<int, string> dict, int key, string value)
            {
                if (dict.TryGetValue(key, out string found))
                {
                    dict[key] = found + ", " + value;
                }
                else
                {
                    dict[key] = "// " + value;
                }
            }
        }

        private static string SnippetFromSpan(string[] lines, XmlElement span)
        {
            if (span.GetAttribute("hidden") != "true")
            {
                var startLine = Convert.ToInt32(span.GetAttribute("startLine"));
                var startColumn = Convert.ToInt32(span.GetAttribute("startColumn"));
                var endLine = Convert.ToInt32(span.GetAttribute("endLine"));
                var endColumn = Convert.ToInt32(span.GetAttribute("endColumn"));
                if (startLine == endLine)
                {
                    return lines[startLine - 1].Substring(startColumn - 1, endColumn - startColumn);
                }
                else
                {
                    var start = lines[startLine - 1].Substring(startColumn - 1);
                    var end = lines[endLine - 1].Substring(0, endColumn - 1);
                    return TruncateStart(start, 12) + " ... " + TruncateEnd(end, 12);
                }
            }
            else
            {
                return "<hidden>";
            }

            string TruncateStart(string text, int maxLength)
            {
                if (text.Length < maxLength) { return text; }
                return text.Substring(0, maxLength);
            }

            string TruncateEnd(string text, int maxLength)
            {
                if (text.Length < maxLength) { return text; }
                return text.Substring(text.Length - maxLength - 1, maxLength);
            }
        }
    }
}
