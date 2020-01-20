// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roslyn.Test.Utilities.Syntax
{
    internal sealed class RandomizedSourceText : SourceText
    {
        private char[] _buffer = new char[2048];

        public RandomizedSourceText()
        {
            var random = new Random(12345);
            for (var i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = (char)random.Next();
            }
        }

        public override char this[int position] => _buffer[position % _buffer.Length];

        public override Encoding Encoding => Encoding.UTF8;

        public override int Length
        {
            get
            {
                return 40 * 1000 * 1000;
            }
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            for (var i = 0; i < count; i++)
            {
                destination[destinationIndex + i] = this[sourceIndex + i];
            }
        }
    }
}
