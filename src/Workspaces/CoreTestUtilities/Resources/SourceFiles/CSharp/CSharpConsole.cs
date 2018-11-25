// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
