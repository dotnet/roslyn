// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Not threadsafe.
    /// </summary>
    internal struct IndentingStringBuilder : IDisposable
    {
        private const string DefaultIndentation = "    ";
        private const string DefaultNewLine = "\r\n";
        private const int DefaultIndentationCount = 8;

        private static readonly ImmutableArray<string> s_defaultIndentationStrings;

        static IndentingStringBuilder()
        {
            var builder = ArrayBuilder<string>.GetInstance(DefaultIndentationCount);

            PopulateIndentationStrings(builder, DefaultIndentation);

            s_defaultIndentationStrings = builder.ToImmutableAndFree();
        }

        private readonly PooledStringBuilder _builder = PooledStringBuilder.GetInstance();

        private readonly ArrayBuilder<string> _indentationStrings = ArrayBuilder<string>.GetInstance();

        private readonly string _indentationString;
        private readonly string _newLine;

        /// <summary>
        /// The current indentation level.
        /// </summary>
        private int _currentIndentationLevel = 0;

        /// <summary>
        /// The current indentation, as text.
        /// </summary>
        private string _currentIndentation = "";

        public IndentingStringBuilder(string indentationString, string newLine)
        {
            _indentationString = indentationString;
            _newLine = newLine;

            // Avoid allocating indentation strings in the common case where the client is using the defaults.
            if (indentationString == DefaultIndentation)
            {
                _indentationStrings.AddRange(s_defaultIndentationStrings);
            }
            else
            {
                PopulateIndentationStrings(_indentationStrings, indentationString);
            }
        }

        public static IndentingStringBuilder Create(string indentation = DefaultIndentation, string newLine = DefaultNewLine)
            => new(indentation, newLine);

        private static void PopulateIndentationStrings(ArrayBuilder<string> builder, string indentation)
        {
            builder.Count = builder.Capacity;
            builder[0] = "";
            for (int i = 1; i < builder.Capacity; i++)
                builder[i] = builder[i - 1] + indentation;
        }

        public void Dispose()
        {
            _indentationStrings.Free();
            _builder.Free();
        }

        public void IncreaseIndent()
        {
            _currentIndentationLevel++;
            if (_currentIndentationLevel == _indentationStrings.Count)
                _indentationStrings.Add(_indentationStrings.Last() + _indentationString);
        }

        public void DecreaseIndent()
        {
            if (_currentIndentationLevel == 0)
                throw new InvalidOperationException($"Current indent is already zero.");

            _currentIndentationLevel--;
        }

        public Block StartBlock()
        {
            this.WriteLine("{");
            this.IncreaseIndent();

            return new Block(this);
        }

        public struct Block(IndentingStringBuilder builder) : IDisposable
        {
            public void Dispose()
            {
                builder.DecreaseIndent();
                builder.WriteLine("}");
            }
        }
    }
}
