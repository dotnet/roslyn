// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
