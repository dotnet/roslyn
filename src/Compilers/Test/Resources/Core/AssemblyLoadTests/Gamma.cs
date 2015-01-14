// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
