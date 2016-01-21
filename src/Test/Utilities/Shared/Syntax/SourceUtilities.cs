// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roslyn.Test.Utilities.Syntax
{
    internal sealed class RandomizedSourceText: SourceText
    {
        private char[] buffer = new char[2048];

        public RandomizedSourceText()
        {
            var random = new Random(12345);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (char)random.Next();
            }
        }

        public override char this[int position] => buffer[position % buffer.Length];

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
