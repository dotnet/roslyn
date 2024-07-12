// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

/// <summary>
/// An XML parser that is designed to parse small fragments of XML such as those that appear in documentation comments.
/// PERF: We try to re-use the same underlying <see cref="XmlReader"/> to reduce the allocation costs of multiple parses.
/// </summary>
internal sealed class XmlFragmentParser
{
    private XmlReader _xmlReader;
    private readonly Reader _textReader = new();

    private static readonly ObjectPool<XmlFragmentParser> s_pool = SharedPools.Default<XmlFragmentParser>();

    /// <summary>
    /// Parse the given XML fragment. The given callback is executed until either the end of the fragment
    /// is reached or an exception occurs.
    /// </summary>
    /// <typeparam name="TArg">Type of an additional argument passed to the <paramref name="callback"/> delegate.</typeparam>
    /// <param name="xmlFragment">The fragment to parse.</param>
    /// <param name="callback">Action to execute while there is still more to read.</param>
    /// <param name="arg">Additional argument passed to the callback.</param>
    /// <remarks>
    /// It is important that the <paramref name="callback"/> action advances the <see cref="XmlReader"/>,
    /// otherwise parsing will never complete.
    /// </remarks>
    public static void ParseFragment<TArg>(string xmlFragment, TArg arg, Action<XmlReader, TArg> callback)
    {
        var instance = s_pool.Allocate();
        try
        {
            instance.ParseInternal(xmlFragment, callback: callback, arg: arg);
        }
        finally
        {
            s_pool.Free(instance);
        }
    }

    private static readonly XmlReaderSettings s_xmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
    };

    private void ParseInternal<TArg>(string text, TArg arg, Action<XmlReader, TArg> callback)
    {
        _textReader.SetText(text);

        _xmlReader ??= XmlReader.Create(_textReader, s_xmlSettings);

        try
        {
            while (!ReachedEnd)
            {
                if (BeforeStart)
                {
                    // Skip over the synthetic root element and first node
                    _xmlReader.Read();
                }
                else
                {
                    callback(_xmlReader, arg);
                }
            }

            // Read the final EndElement to reset things for the next user.
            _xmlReader.ReadEndElement();
        }
        catch
        {
            // The reader is in a bad state, so dispose of it and recreate a new one next time we get called.
            _xmlReader.Dispose();
            _xmlReader = null;
            _textReader.Reset();
            throw;
        }
    }

    private bool BeforeStart
    {
        get
        {
            // Depth 0 = Document root
            // Depth 1 = Synthetic wrapper, "CurrentElement"
            // Depth 2 = Start of user's fragment.
            return _xmlReader.Depth < 2;
        }
    }

    private bool ReachedEnd
    {
        get
        {
            return _xmlReader.Depth == 1
                && _xmlReader.NodeType == XmlNodeType.EndElement
                && _xmlReader.LocalName == Reader.CurrentElementName;
        }
    }

    /// <summary>
    /// A text reader over a synthesized XML stream consisting of a single root element followed by a potentially
    /// infinite stream of fragments. Each time "SetText" is called the stream is rewound to the element immediately
    /// following the synthetic root node.
    /// </summary>
    private sealed class Reader : TextReader
    {
        /// <summary>
        /// Current text to validate.
        /// </summary>
        private string _text;

        private int _position;

        // Base the root element name on a GUID to avoid accidental (or intentional) collisions. An underscore is
        // prefixed because element names must not start with a number.
        private static readonly string s_rootElementName = "_" + Guid.NewGuid().ToString("N");

        // We insert an extra synthetic element name to allow for raw text at the root
        internal static readonly string CurrentElementName = "_" + Guid.NewGuid().ToString("N");

        private static readonly string s_rootStart = "<" + s_rootElementName + ">";
        private static readonly string s_currentStart = "<" + CurrentElementName + ">";
        private static readonly string s_currentEnd = "</" + CurrentElementName + ">";

        public void Reset()
        {
            _text = null;
            _position = 0;
        }

        public void SetText(string text)
        {
            _text = text;

            // The first read shall read the <root>, 
            // the subsequents reads shall start with <current> element
            if (_position > 0)
            {
                _position = s_rootStart.Length;
            }
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

            var initialCount = count;

            // <root>
            _position += EncodeAndAdvance(s_rootStart, _position, buffer, ref index, ref count);

            // <current>
            _position += EncodeAndAdvance(s_currentStart, _position - s_rootStart.Length, buffer, ref index, ref count);

            // text
            _position += EncodeAndAdvance(_text, _position - s_rootStart.Length - s_currentStart.Length, buffer, ref index, ref count);

            // </current>
            _position += EncodeAndAdvance(s_currentEnd, _position - s_rootStart.Length - s_currentStart.Length - _text.Length, buffer, ref index, ref count);

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

            var charCount = Math.Min(src.Length - srcIndex, destCount);
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
            throw ExceptionUtilities.Unreachable();
        }

        public override int Peek()
        {
            // XmlReader does not call this API
            throw ExceptionUtilities.Unreachable();
        }
    }
}
