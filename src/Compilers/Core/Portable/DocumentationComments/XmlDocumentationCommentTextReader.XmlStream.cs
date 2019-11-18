// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
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
            private string _text;

            private int _position;

            /// <summary>
            /// We use <see cref="XmlReader"/> to validate XML doc comments. Unfortunately it cannot be reset and thus can't be pooled. 
            /// Each time we need to validate a fragment of XML we "append" it to the underlying text reader, implemented by this class, 
            /// and advance the reader. By the end of the fragment validation, we keep the reader open in a state 
            /// that is ready for the next fragment validation unless the fragment was invalid, in which case we need to create a new XmlReader.
            /// That is why <see cref="Read(char[], int, int) "/> pretends that the stream has extra <see cref="maxReadsPastTheEnd"/> spaces
            /// at the end. That should be sufficient for <see cref="XmlReader"/> to not reach the end of this reader before the next 
            /// fragment is appended, unless the current fragment is malformed in one way or another. 
            /// </summary>
            private const int maxReadsPastTheEnd = 100;
            private int _readsPastTheEnd;

            // Base the root element name on a GUID to avoid accidental (or intentional) collisions. An underscore is
            // prefixed because element names must not start with a number.
            private static readonly string s_rootElementName = "_" + Guid.NewGuid().ToString("N");
            private static readonly string s_currentElementName = "_" + Guid.NewGuid().ToString("N");

            // internal for testing
            internal static readonly string RootStart = "<" + s_rootElementName + ">";
            internal static readonly string CurrentStart = "<" + s_currentElementName + ">";
            internal static readonly string CurrentEnd = "</" + s_currentElementName + ">";

            public void Reset()
            {
                _text = null;
                _position = 0;
                _readsPastTheEnd = 0;
            }

            public void SetText(string text)
            {
                _text = text;
                _readsPastTheEnd = 0;

                // The first read shall read the <root>, 
                // the subsequents reads shall start with <current> element
                if (_position > 0)
                {
                    _position = RootStart.Length;
                }
            }

            // for testing
            internal int Position
            {
                get { return _position; }
            }

            public static bool ReachedEnd(XmlReader reader)
            {
                return reader is { Depth: 1, NodeType: XmlNodeType.EndElement } && reader.Name == s_currentElementName;
            }

            public bool Eof
            {
                get
                {
                    return _readsPastTheEnd >= maxReadsPastTheEnd;
                }
            }

            public override int Read(char[] buffer, int index, int count)
            {
                if (count == 0 || Eof)
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
                _position += EncodeAndAdvance(RootStart, _position, buffer, ref index, ref count);

                // <current>
                _position += EncodeAndAdvance(CurrentStart, _position - RootStart.Length, buffer, ref index, ref count);

                // text
                _position += EncodeAndAdvance(_text, _position - RootStart.Length - CurrentStart.Length, buffer, ref index, ref count);

                // </current>
                _position += EncodeAndAdvance(CurrentEnd, _position - RootStart.Length - CurrentStart.Length - _text.Length, buffer, ref index, ref count);

                // Pretend that the stream doesn't end right away
                if (initialCount == count)
                {
                    _readsPastTheEnd++;
                    buffer[index] = ' ';
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
