// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias Sys1;
extern alias Sys2;

class Program
{
    static Sys1.System.Uri f1 = null;
    static Sys2::System.Uri f2;
    static void Main()
    {
        f2 = f1;
    }
}
