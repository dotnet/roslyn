// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#pragma warning disable CA1801 // Remove unused parameter
#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Linq;
using System.Text;

namespace OtherDll
{
    /// <summary>
    /// Aids with testing dataflow analysis _not_ doing interprocedural DFA.
    /// </summary>
    /// <remarks>
    /// Since Roslyn doesn't support cross-binary DFA, and this class is
    /// defined in a different binary, using this class from test source code
    /// is a way to test handling of non-interprocedural results in dataflow
    /// analysis implementations.
    /// </remarks>
    public class OtherDllClass<T>
        where T : class
    {
        public OtherDllClass(T? constructedInput)
        {
            this.ConstructedInput = constructedInput;
        }

        public T? ConstructedInput { get; set; }

        public T? Default
        {
            get => null;
            set { }
        }

        public string RandomString
        {
            get
            {
                Random r = new Random();
                byte[] bytes = new byte[r.Next(20) + 10];
                r.NextBytes(bytes);
                bytes = bytes.Where(b => b is >= ((byte)' ') and <= ((byte)'~')).ToArray();
                return Encoding.ASCII.GetString(bytes);
            }

            set { }
        }

        public T? ReturnsConstructedInput()
        {
            return this.ConstructedInput;
        }

        public T? ReturnsDefault()
        {
            return null;
        }

        public T? ReturnsInput(T? input)
        {
            return input;
        }

        public T? ReturnsDefault(T? input)
        {
            return null;
        }

        public string ReturnsRandom(string input)
        {
            Random r = new Random();
            byte[] bytes = new byte[r.Next(20) + 10];
            r.NextBytes(bytes);
            bytes = bytes.Where(b => b is >= ((byte)' ') and <= ((byte)'~')).ToArray();
            return Encoding.ASCII.GetString(bytes);
        }

        public void SetsOutputToConstructedInput(out T? output)
        {
            output = this.ConstructedInput;
        }

        public void SetsOutputToDefault(out T? output)
        {
            output = null;
        }

        public void SetsOutputToInput(T? input, out T? output)
        {
            output = input;
        }

        public void SetsOutputToDefault(T? input, out T? output)
        {
            output = null;
        }

        public void SetsOutputToRandom(string input, out string output)
        {
            Random r = new Random();
            byte[] bytes = new byte[r.Next(20) + 10];
            r.NextBytes(bytes);
            bytes = bytes.Where(b => b is >= ((byte)' ') and <= ((byte)'~')).ToArray();
            output = Encoding.ASCII.GetString(bytes);
        }

        public void SetsReferenceToConstructedInput(ref T? output)
        {
            output = this.ConstructedInput;
        }

        public void SetsReferenceToDefault(ref T? output)
        {
            output = null;
        }

        public void SetsReferenceToInput(T? input, ref T? output)
        {
            output = input;
        }

        public void SetsReferenceToDefault(T? input, ref T? output)
        {
            output = null;
        }

        public void SetsReferenceToRandom(string input, ref string output)
        {
            Random r = new Random();
            byte[] bytes = new byte[r.Next(20) + 10];
            r.NextBytes(bytes);
            bytes = bytes.Where(b => b is >= ((byte)' ') and <= ((byte)'~')).ToArray();
            output = Encoding.ASCII.GetString(bytes);
        }
    }
}
