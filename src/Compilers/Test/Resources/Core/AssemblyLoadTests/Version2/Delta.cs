// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Build Delta.dll with: csc.exe /t:library Delta.cs


using System.Text;

[assembly: System.Reflection.AssemblyTitle("Delta")]
[assembly: System.Reflection.AssemblyVersion("2.0.0.0")]

namespace Delta
{
    public class D
    {
        public void Write(StringBuilder sb, string s)
        {
            sb.AppendLine("Delta.2: " + s);
        }
    }
}
