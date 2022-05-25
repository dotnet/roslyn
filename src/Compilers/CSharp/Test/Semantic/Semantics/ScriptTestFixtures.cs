// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public static class ScriptTestFixtures
    {
        internal static MetadataReference HostRef = MetadataReference.CreateFromAssemblyInternal(typeof(ScriptTestFixtures).GetTypeInfo().Assembly);

        public class B
        {
            public int x = 1, w = 4;
        }

        public class C : B, I
        {
            public static readonly int StaticField = 123;
            public int Y => 2;
            public string N { get; set; } = "2";
            public int Z() => 3;
            public override int GetHashCode() => 123;
        }

        public interface I
        {
            string N { get; set; }
            int Z();
        }

        private class PrivateClass : I
        {
            public string N { get; set; } = null;
            public int Z() => 3;
        }

        public class B2
        {
            public int x = 1, w = 4;
        }
    }
}
