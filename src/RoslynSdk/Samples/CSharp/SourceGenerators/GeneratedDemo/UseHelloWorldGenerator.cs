// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GeneratedDemo
{
    public static class UseHelloWorldGenerator
    {
        public static void Run()
        {
            // The static call below is generated at build time, and will list the syntax trees used in the compilation
            HelloWorldGenerated.HelloWorld.SayHello();
        }
    }
}
