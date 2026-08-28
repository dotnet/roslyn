' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Explicit On
Option Strict On
Option Infer On

Module Program

    Public Sub Main()

        Console.WriteLine("Running HelloWorld:
")
        UseHelloWorldGenerator.Run()

        Console.WriteLine("

Running AutoNotify:
")
        UseAutoNotifyGenerator.Run()

        Console.WriteLine("

Running XmlSettings:
")
        UseXmlSettingsGenerator.Run()

        Console.WriteLine("

Running CsvGenerator:
")
        UseCsvGenerator.Run()

    End Sub

End Module
