// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Build Alpha.dll with: csc.exe /t:library /r:Gamma.dll Alpha.cs

using System.Text;
using Gamma;

namespace Alpha
{
    public class A
    {
        public void Write(StringBuilder sb, string s)
        {
            G g = new G();

            g.Write(sb, "Alpha: " + s);
        }
    }
}
