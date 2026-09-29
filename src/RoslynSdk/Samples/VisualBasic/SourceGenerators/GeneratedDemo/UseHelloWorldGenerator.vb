' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Public Module UseHelloWorldGenerator

    Public Sub Run()
        ' The static call below is generated at build time, and will list the syntax trees used in the compilation
        HelloWorldGenerated.HelloWorld.SayHello()
    End Sub

End Module
