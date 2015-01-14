// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Build Delta.dll with: csc.exe /t:library Delta.cs

using System.Text;

namespace Delta
{
    public class D
    {
        public void Write(StringBuilder sb, string s)
        {
            sb.AppendLine("Delta: " + s);
        }
    }
}
