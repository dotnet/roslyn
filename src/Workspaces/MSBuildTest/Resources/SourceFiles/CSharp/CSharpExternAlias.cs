// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
