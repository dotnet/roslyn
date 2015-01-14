// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Build Beta.dll with: csc.exe /t:library /r:Gamma.dll Beta.cs

using System.Text;
using Gamma;

namespace Beta
{
    public class B
    {
        public void Write(StringBuilder sb, string s)
        {
            G g = new G();

            g.Write(sb, "Beta: " + s);
        }
    }
}
