Option Explicit On
Option Infer On
Option Strict On

Imports System.Text

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text

#Disable Warning RS1035 ' Do not use APIs banned for analyzers

Namespace SourceGeneratorSamples

    <Generator(LanguageNames.VisualBasic)>
    Public Class HelloWorldGenerator
        Implements ISourceGenerator

        Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
            ' No initialization required
        End Sub

        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute

            ' begin creating the source we'll inject into the users compilation

            Dim sourceBuilder = New StringBuilder("Option Explicit On
Option Strict On
Option Infer On

Namespace Global.HelloWorldGenerated

    Public Module HelloWorld

        Public Sub SayHello()

            Console.WriteLine(""Hello from generated code!"")
            Console.WriteLine(""The following syntax trees existed in the compilation that created this program:"")
")

            ' for testing... let's include a comment with the current date/time.
            sourceBuilder.AppendLine($"            ' Generated at {DateTime.Now}")

            ' using the context, get a list of syntax trees in the users compilation
            ' add the filepath of each tree to the class we're building

            For Each tree In context.Compilation.SyntaxTrees
                sourceBuilder.AppendLine($"            Console.WriteLine("" - {tree.FilePath}"")")
            Next

            ' finish creating the source to inject

            sourceBuilder.Append("

        End Sub

    End Module

End Namespace")

            ' inject the created source into the users compilation

            context.AddSource("HelloWorldGenerated.g.vb", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8))

        End Sub

    End Class

End Namespace
