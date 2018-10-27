// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CA1801

using System;

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
    {
        public OtherDllClass(T constructedInput)
        {
            this.ConstructedInput = constructedInput;
        }

        public T ConstructedInput { get; set; }

        public T Default
        {
            get { return default(T); }
            set { }
        }

        public string RandomString
        {
            get { return OtherDllStaticMethods.ReturnsRandom(String.Empty); }
            set { }
        }

        public T ReturnsInput(T input)
        {
            return input;
        }

        public T ReturnsDefault(T input)
        {
            return default(T);
        }

        public string ReturnsRandom(string input)
        {
            return OtherDllStaticMethods.ReturnsRandom(input);
        }

        public void SetsOutputToInput(T input, out T output)
        {
            output = input;
        }

        public void SetsOutputToDefault(T input, out T output)
        {
            output = default(T);
        }

        public void SetsOutputToRandom(string input, out string output)
        {
            output = OtherDllStaticMethods.ReturnsRandom(input);
        }
    }
}
