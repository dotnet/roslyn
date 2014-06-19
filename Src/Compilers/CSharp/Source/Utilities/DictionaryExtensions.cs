using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class DictionaryExtensions
    {
        [ObsoleteAttribute("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
        public static void RemoveAll<K, V>(this Dictionary<K, V> dictionary, IEnumerable<K> keys)
        {
            foreach (var key in keys)
            {
                dictionary.Remove(key);
            }
        }
    }
}