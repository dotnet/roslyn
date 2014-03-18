// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal partial class XmlDocumentationCommentTextReader
    {
        private class XmlStream : Stream
        {
            private string text;    // The XML text
            private int charIndex;  // Current position (in chars) within the (synthesized) text

            // Base the root element name on a GUID to avoid accidental (or intentional) collisions. An underscore is
            // prefixed because element names must not start with a number.
            public static readonly string RootElementName = "_" + Guid.NewGuid().ToString("N");
            private static readonly string prefix = "<" + RootElementName + ">";
            private static readonly string suffix = "</" + RootElementName + ">";

            public void SetText(string text)
            {
                this.text = text;
                this.charIndex = 0;
            }

            public bool ReachedEnd
            {
                get
                {
                    return this.text == null;
                }
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override long Length
            {
                get
                {
                    return (prefix.Length + suffix.Length + text.Length) * sizeof(char);
                }
            }

            public override long Position
            {
                get
                {
                    return this.charIndex * sizeof(char);
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                // The stream synthesizes an XML document with:
                // 1. A root element start tag (prefix)
                // 2. The user text (xml fragments)
                // 3. The root element end tag (suffix)

                int initialCount = count;

                // Prefix
                charIndex += EncodeAndAdvance(prefix, charIndex, buffer, ref offset, ref count);

                // Body
                charIndex += EncodeAndAdvance(text, charIndex - prefix.Length, buffer, ref offset, ref count);

                // Suffix
                charIndex += EncodeAndAdvance(suffix, charIndex - prefix.Length - text.Length, buffer, ref offset, ref count);

                // Return the number of bytes copied
                // NOTE: It should be "initialCount - count", but that seems to confuse CLRProfiler.
                //       Reversing operand somehow fixes the issue.
                return -count + initialCount;
            }

            private static int EncodeAndAdvance(string src, int srcIndex, byte[] dest, ref int destOffset, ref int destCount)
            {
                if (destCount <= 0 || srcIndex < 0 || srcIndex >= src.Length)
                {
                    return 0;
                }

                int charCount = Math.Min(src.Length - srcIndex, destCount / sizeof(char));
                Debug.Assert(charCount > 0);
                int bytesCopied = Encoding.Unicode.GetBytes(src, srcIndex, charCount, dest, destOffset);
                destOffset += bytesCopied;
                destCount -= bytesCopied;
                Debug.Assert(destCount >= 0);
                return charCount;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}