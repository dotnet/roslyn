// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Serialization.Json;

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
        private readonly DataContractJsonSerializer _stringWriter;
        private int _indent;
        private string _pending;

        private static readonly string s_newLine = Environment.NewLine;
        private static readonly string s_commaNewLine = "," + Environment.NewLine;

        private const string Indentation = "  ";

        public JsonWriter(StreamWriter output)
        {
            _outptut = output;
            _stringWriter = new DataContractJsonSerializer(typeof(string));
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
            // Consider switching to custom escaping logic here. Flushing all the time (in
            // order to borrow DataContractJsonSerializer escaping) is expensive. 
            //
            // Also, it would be nicer not to escape the forward slashes in URIs (which is
            // optional in JSON.)

            WritePending();
            _outptut.Flush();
            _stringWriter.WriteObject(_outptut.BaseStream, value);
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
    }
}
