using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// An XML parser that is designed to parse small fragments of XML such as those that appear in documentation comments.
    /// PERF: We try to re-use the same underlying <see cref="XmlReader"/> to reduce the allocation costs of multiple parses.
    /// </summary>
    internal sealed class XmlFragmentParser
    {
        private XmlReader xmlReader;
        private readonly Reader textReader = new Reader();

        private static readonly ObjectPool<XmlFragmentParser> pool =
            new ObjectPool<XmlFragmentParser>(() => new XmlFragmentParser(), size: 2);

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
        public static void ParseFragment<TArg>(string xmlFragment, Action<XmlReader, TArg> callback, TArg arg)
        {
            var instance = pool.Allocate();
            try
            {
                instance.ParseInternal(xmlFragment, callback, arg);
            }
            finally
            {
                pool.Free(instance);
            }
        }

        private static readonly XmlReaderSettings XmlSettings = new XmlReaderSettings()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        private void ParseInternal<TArg>(string text, Action<XmlReader, TArg> callback, TArg arg)
        {
            textReader.SetText(text);

            if (xmlReader == null)
            {
                xmlReader = XmlReader.Create(textReader, XmlSettings);
            }

            try
            {
                while (!ReachedEnd)
                {
                    if (BeforeStart)
                    {
                        // Skip over the synthetic root element and first node
                        xmlReader.Read();
                    }
                    else
                    {
                        callback(xmlReader, arg);
                    }
                }

                // Read the final EndElement to reset things for the next user.
                xmlReader.ReadEndElement();
            }
            catch
            {
                // The reader is in a bad state, so dispose of it and recreate a new one next time we get called.
                xmlReader.Dispose();
                xmlReader = null;
                textReader.Reset();
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
                return xmlReader.Depth < 2;
            }
        }

        private bool ReachedEnd
        {
            get
            {
                return xmlReader.Depth == 1
                    && xmlReader.NodeType == XmlNodeType.EndElement
                    && xmlReader.LocalName == Reader.CurrentElementName;
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
            private string text;

            private int position;

            // Base the root element name on a GUID to avoid accidental (or intentional) collisions. An underscore is
            // prefixed because element names must not start with a number.
            private static readonly string RootElementName = "_" + Guid.NewGuid().ToString("N");

            // We insert an extra synthetic element name to allow for raw text at the root
            internal static readonly string CurrentElementName = "_" + Guid.NewGuid().ToString("N");

            private static readonly string RootStart = "<" + RootElementName + ">";
            private static readonly string CurrentStart = "<" + CurrentElementName + ">";
            private static readonly string CurrentEnd = "</" + CurrentElementName + ">";

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
