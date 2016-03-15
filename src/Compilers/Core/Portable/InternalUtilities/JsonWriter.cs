// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A simple, forward-only JSON writer to avoid adding dependencies to the compiler.
    /// Used to generate /errorlogger output.
    /// 
    /// Does not guarantee well-formed JSON if misused. It is the caller's reponsibility 
    /// to balance array/object start/end, to only write key-value pairs to objects and
    /// elements to arrays, etc.
    /// 
    /// Takes ownership of the given StreamWriter at construction and handles its disposal.
    /// </summary>
    internal sealed class JsonWriter : IDisposable
    {
        private readonly StreamWriter _outptut;
        private int _indent;
        private string _pending;

        private static readonly string s_newLine = Environment.NewLine;
        private static readonly string s_commaNewLine = "," + Environment.NewLine;

        private const string Indentation = "  ";

        public JsonWriter(StreamWriter output)
        {
            _outptut = output;
            _pending = "";
        }

        public void WriteObjectStart()
        {
            WriteStart('{');
        }

        public void WriteObjectStart(string key)
        {
            WriteKey(key);
            WriteObjectStart();
        }

        public void WriteObjectEnd()
        {
            WriteEnd('}');
        }

        public void WriteArrayStart()
        {
            WriteStart('[');
        }

        public void WriteArrayStart(string key)
        {
            WriteKey(key);
            WriteArrayStart();
        }

        public void WriteArrayEnd()
        {
            WriteEnd(']');
        }

        public void WriteKey(string key)
        {
            Write(key);
            _outptut.Write(": ");
            _pending = "";
        }

        public void Write(string key, string value)
        {
            WriteKey(key);
            Write(value);
        }

        public void Write(string key, int value)
        {
            WriteKey(key);
            Write(value);
        }

        public void Write(string key, bool value)
        {
            WriteKey(key);
            Write(value);
        }

        public void Write(string value)
        {
            WritePending();
            _outptut.Write('"');
            _outptut.Write(EscapeString(value));
            _outptut.Write('"');
            _pending = s_commaNewLine;
        }

        public void Write(int value)
        {
            WritePending();
            _outptut.Write(value);
            _pending = s_commaNewLine;
        }

        public void Write(bool value)
        {
            WritePending();
            _outptut.Write(value ? "true" : "false");
            _pending = s_commaNewLine;
        }

        private void WritePending()
        {
            if (_pending.Length > 0)
            {
                _outptut.Write(_pending);

                for (int i = 0; i < _indent; i++)
                {
                    _outptut.Write(Indentation);
                }
            }
        }

        private void WriteStart(char c)
        {
            WritePending();
            _outptut.Write(c);
            _pending = s_newLine;
            _indent++;
        }

        private void WriteEnd(char c)
        {
            _pending = s_newLine;
            _indent--;
            WritePending();
            _outptut.Write(c);
            _pending = s_commaNewLine;
        }

        public void Dispose()
        {
            _outptut.Dispose();
        }

        // String escaping implementation forked from System.Runtime.Serialization.Json to 
        // avoid a large dependency graph for this small amount of code:
        //
        // https://github.com/dotnet/corefx/blob/master/src/System.Private.DataContractSerialization/src/System/Runtime/Serialization/Json/JavaScriptString.cs
        //
        // Possible future improvements: https://github.com/dotnet/roslyn/issues/9769
        //
        //   - Avoid intermediate StringBuilder and send escaped output directly to the destination.
        //
        //   - Stop escaping '/': it is is optional per JSON spec and several users have expressed
        //     that they don't like the way '\/' looks.
        //
        private static string EscapeString(string value)
        {
            StringBuilder b = null;

            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (c == '\"' || c == '\'' || c == '/' || c == '\\' || ShouldAppendAsUnicode(c))
                {
                    if (b == null)
                    {
                        b = new StringBuilder(value.Length + 5);
                    }

                    if (count > 0)
                    {
                        b.Append(value, startIndex, count);
                    }

                    startIndex = i + 1;
                    count = 0;
                }

                switch (c)
                {
                    case '\"':
                        b.Append("\\\"");
                        break;
                    case '\\':
                        b.Append("\\\\");
                        break;
                    case '/':
                        b.Append("\\/");
                        break;
                    case '\'':
                        b.Append("\'");
                        break;
                    default:
                        if (ShouldAppendAsUnicode(c))
                        {
                            AppendCharAsUnicode(b, c);
                        }
                        else
                        {
                            count++;
                        }
                        break;
                }
            }

            if (b == null)
            {
                return value;
            }

            if (count > 0)
            {
                b.Append(value, startIndex, count);
            }

            return b.ToString();
        }

        private static void AppendCharAsUnicode(StringBuilder builder, char c)
        {
            builder.Append("\\u");
            builder.AppendFormat(CultureInfo.InvariantCulture, "{0:x4}", (int)c);
        }

        private static bool ShouldAppendAsUnicode(char c)
        {
            // Note on newline characters: Newline characters in JSON strings need to be encoded on the way out 
            // See Unicode 6.2, Table 5-1 (http://www.unicode.org/versions/Unicode6.2.0/ch05.pdf]) for the full list. 

            // We only care about NEL, LS, and PS, since the other newline characters are all 
            // control characters so are already encoded. 

            return c < ' ' ||
                c >= (char)0xfffe || // max char 
                (c >= (char)0xd800 && c <= (char)0xdfff) || // between high and low surrogate 
                (c == '\u0085' || c == '\u2028' || c == '\u2029'); // Unicode new line characters 
        }
    }
}
