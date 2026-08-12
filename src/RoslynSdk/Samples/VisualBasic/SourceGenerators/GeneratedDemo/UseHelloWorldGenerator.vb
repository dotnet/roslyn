Public Module UseHelloWorldGenerator

    Public Sub Run()
        ' The static call below is generated at build time, and will list the syntax trees used in the compilation
        HelloWorldGenerated.HelloWorld.SayHello()
    End Sub

End Module
