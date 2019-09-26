// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A simple, forward-only JSON writer to avoid adding dependencies to the compiler.
    /// Used to generate /errorlogger output.
    /// 
    /// Does not guarantee well-formed JSON if misused. It is the caller's responsibility 
    /// to balance array/object start/end, to only write key-value pairs to objects and
    /// elements to arrays, etc.
    /// 
    /// Takes ownership of the given <see cref="TextWriter" /> at construction and handles its disposal.
    /// </summary>
    internal sealed class JsonWriter : IDisposable
    {
        private readonly TextWriter _output;
        private int _indent;
        private Pending _pending;

        private enum Pending { None, NewLineAndIndent, CommaNewLineAndIndent };
        private const string Indentation = "  ";

        public JsonWriter(TextWriter output)
        {
            _output = output;
            _pending = Pending.None;
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
            _output.Write(": ");
            _pending = Pending.None;
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
            _output.Write('"');
            _output.Write(EscapeString(value));
            _output.Write('"');
            _pending = Pending.CommaNewLineAndIndent;
        }

        public void Write(int value)
        {
            WritePending();
            _output.Write(value.ToString(CultureInfo.InvariantCulture));
            _pending = Pending.CommaNewLineAndIndent;
        }

        public void Write(bool value)
        {
            WritePending();
            _output.Write(value ? "true" : "false");
            _pending = Pending.CommaNewLineAndIndent;
        }

        private void WritePending()
        {
            if (_pending == Pending.None)
            {
                return;
            }

            Debug.Assert(_pending == Pending.NewLineAndIndent || _pending == Pending.CommaNewLineAndIndent);
            if (_pending == Pending.CommaNewLineAndIndent)
            {
                _output.Write(',');
            }

            _output.WriteLine();

            for (int i = 0; i < _indent; i++)
            {
                _output.Write(Indentation);
            }
        }

        private void WriteStart(char c)
        {
            WritePending();
            _output.Write(c);
            _pending = Pending.NewLineAndIndent;
            _indent++;
        }

        private void WriteEnd(char c)
        {
            _pending = Pending.NewLineAndIndent;
            _indent--;
            WritePending();
            _output.Write(c);
            _pending = Pending.CommaNewLineAndIndent;
        }

        public void Dispose()
        {
            _output.Dispose();
        }

        // String escaping implementation forked from System.Runtime.Serialization.Json to 
        // avoid a large dependency graph for this small amount of code:
        //
        // https://github.com/dotnet/corefx/blob/master/src/System.Private.DataContractSerialization/src/System/Runtime/Serialization/Json/JavaScriptString.cs
        //
        private static string EscapeString(string? value)
        {
            PooledStringBuilder? pooledBuilder = null;
            StringBuilder? b = null;

            if (RoslynString.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

#nullable disable // Needs work...
            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (c == '\"' || c == '\\' || ShouldAppendAsUnicode(c))
                {
                    if (b == null)
                    {
                        RoslynDebug.Assert(pooledBuilder == null);
                        pooledBuilder = PooledStringBuilder.GetInstance();
                        b = pooledBuilder.Builder;
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

            return pooledBuilder.ToStringAndFree();
#nullable enable
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
