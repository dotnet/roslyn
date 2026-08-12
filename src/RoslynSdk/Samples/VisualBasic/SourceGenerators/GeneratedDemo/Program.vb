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
