// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias ProjAlias;

using System;

namespace CSharpProject_ProjectReference
{
    class Program
    {
        static ProjAlias::CSharpProject.CSharpClass field;
        static void Main()
        {
            field = new ProjAlias.CSharpProject.CSharpClass();
        }
    }
}
