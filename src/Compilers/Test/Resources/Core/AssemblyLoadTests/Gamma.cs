// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Build Gamma.dll with: csc.exe /t:library /r:Delta.dll Gamma.cs

using System.Text;
using Delta;

namespace Gamma
{
    public class G
    {
        public void Write(StringBuilder sb, string s)
        {
            D d = new D();

            d.Write(sb, "Gamma: " + s);
        }
    }
}
