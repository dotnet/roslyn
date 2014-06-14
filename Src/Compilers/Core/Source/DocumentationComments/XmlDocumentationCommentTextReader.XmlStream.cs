// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class XmlDocumentationCommentTextReader
    {
        internal sealed class Reader : TextReader
        {
            /// <summary>
            /// Current text to validate.
            /// </summary>
            private string text;    

            private int position;

            // Base the root element name on a GUID to avoid accidental (or intentional) collisions. An underscore is
            // prefixed because element names must not start with a number.
            private static readonly string RootElementName = "_" + Guid.NewGuid().ToString("N");
            private static readonly string CurrentElementName = "_" + Guid.NewGuid().ToString("N");

            // internal for testing
            internal static readonly string RootStart = "<" + RootElementName + ">";
            internal static readonly string CurrentStart = "<" + CurrentElementName + ">";
            internal static readonly string CurrentEnd = "</" + CurrentElementName + ">";

            public void Reset()
            {
                this.text = null;
                this.position = 0;
            }

            public void SetText(string text)
            {
                this.text = text;

                // The first read shall read the <root>, 
                // the subsequents reads shall start with <current> element
                if (this.position > 0)
                {
                    this.position = RootStart.Length;
                }
            }

            // for testing
            internal int Position
            {
                get { return position; }
            }

            public static bool ReachedEnd(XmlReader reader)
            {
                return reader.Depth == 1 
                    && reader.NodeType == XmlNodeType.EndElement 
                    && reader.Name == CurrentElementName;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                if (count == 0)
                {
                    return 0;
                }

                // The stream synthesizes an XML document with:
                // 1. A root element start tag
                // 2. Current element start tag
                // 3. The user text (xml fragments)
                // 4. Current element end tag

                int initialCount = count;

                // <root>
                position += EncodeAndAdvance(RootStart, position, buffer, ref index, ref count);

                // <current>
                position += EncodeAndAdvance(CurrentStart, position - RootStart.Length, buffer, ref index, ref count);
                
                // text
                position += EncodeAndAdvance(text, position - RootStart.Length - CurrentStart.Length, buffer, ref index, ref count);

                // </current>
                position += EncodeAndAdvance(CurrentEnd, position - RootStart.Length - CurrentStart.Length - text.Length, buffer, ref index, ref count);

                // Pretend that the stream is infinite, i.e. never return 0 characters read.
                if (initialCount == count)
                {
                    buffer[index++] = ' ';
                    count--;
                }

                return initialCount - count;
            }

            private static int EncodeAndAdvance(string src, int srcIndex, char[] dest, ref int destIndex, ref int destCount)
            {
                if (destCount == 0 || srcIndex < 0 || srcIndex >= src.Length)
                {
                    return 0;
                }

                int charCount = Math.Min(src.Length - srcIndex, destCount);
                Debug.Assert(charCount > 0);
                src.CopyTo(srcIndex, dest, destIndex, charCount);

                destIndex += charCount;
                destCount -= charCount;
                Debug.Assert(destCount >= 0);
                return charCount;
            }

            public override int Read()
            {
                // XmlReader does not call this API
                throw ExceptionUtilities.Unreachable;
            }

            public override int Peek()
            {
                // XmlReader does not call this API
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}