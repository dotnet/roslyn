Option Explicit On
Option Infer On
Option Strict On

Imports System.IO
Imports System.Text

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text

Imports NotVisualBasic.FileIO

#Disable Warning RS1035 ' Do not use APIs banned for analyzers

' CsvTextFileParser from https://github.com/22222/CsvTextFieldParser adding suppression rules for default VS config

Namespace SourceGeneratorSamples

    <Generator(LanguageNames.VisualBasic)>
    Public Class CsvGenerator
        Implements ISourceGenerator

        Public Enum CsvLoadType
            Startup
            OnDemand
        End Enum

        Public Sub Initialize(context As GeneratorInitializationContext) Implements Microsoft.CodeAnalysis.ISourceGenerator.Initialize

        End Sub

        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
            Dim options As IEnumerable(Of (CsvLoadType, Boolean, AdditionalText)) = GetLoadOptions(context)
            Dim nameCodeSequence As IEnumerable(Of (Name As String, Code As String)) = SourceFilesFromAdditionalFiles(options)
            For Each entry In nameCodeSequence
                context.AddSource($"Csv_{entry.Name}.g.vb", SourceText.From(entry.Code, Encoding.UTF8))
            Next
        End Sub

        ' Guesses type of property for the object from the value of a csv field
        Public Shared Function GetCsvFieldType(exemplar As String) As String
            Dim garbageBoolean As Boolean
            Dim garbageInteger As Integer
            Dim garbageDouble As Double
            Select Case True
                Case Boolean.TryParse(exemplar, garbageBoolean) : Return "Boolean"
                Case Integer.TryParse(exemplar, garbageInteger) : Return "Integer"
                Case Double.TryParse(exemplar, garbageDouble) : Return "Double"
                Case Else : Return "String"
            End Select
        End Function

        ' Examines the header row and the first row in the csv file to gather all header types and names
        ' Also it returns the first row of data, because it must be read to figure out the types,
        ' As the CsvTextFieldParser cannot 'Peek' ahead of one line. If there is no first line,
        ' it consider all properties as strings. The generator returns an empty list of properly
        ' typed objects in such case. If the file is completely empty, an error is generated.
        Public Shared Function ExtractProperties(parser As CsvTextFieldParser) As (Types As String(), Names As String(), Fields As String())

            Dim headerFields = parser.ReadFields()
            If headerFields Is Nothing Then
                Throw New Exception("Empty csv file!")
            End If

            Dim firstLineFields = parser.ReadFields()
            If firstLineFields Is Nothing Then
                Return (Enumerable.Repeat("String", headerFields.Length).ToArray(), headerFields, firstLineFields)
            Else
                Return (firstLineFields.[Select](Function(field) GetCsvFieldType(field)).ToArray(), headerFields.[Select](New Func(Of String, String)(AddressOf StringToValidPropertyName)).ToArray(), firstLineFields)
            End If

        End Function

        ' Adds a class to the `CSV` namespace for each `csv` file passed in. The class has a static property
        ' named `All` that returns the list of strongly typed objects generated on demand at first access.
        ' There is the slight chance of a race condition in a multi-thread program, but the result is relatively benign
        ' , loading the collection multiple times instead of once. Measures could be taken to avoid that.
        Public Shared Function GenerateClassFile(className As String, csvText As String, loadTime As CsvLoadType, cacheObjects As Boolean) As String

            Dim sb As New StringBuilder
            Dim parser As New CsvTextFieldParser(New StringReader(csvText))

            ''' Imports
            sb.Append("Option Explicit On
Option Strict On
Option Infer On

Imports System.Collections.Generic

Namespace Global.CSV
")

            ''' Class Definition
            sb.Append($"
  Public Class {className}

")

            If loadTime = CsvLoadType.Startup Then
                sb.Append($"    Shared Sub New()
      Dim x = All
    End Sub

")
            End If

            Dim tupleTemp = ExtractProperties(parser) : Dim types = tupleTemp.Types, names = tupleTemp.Names, fields = tupleTemp.Fields
            Dim minLen = Math.Min(types.Length, names.Length)

            For i = 0 To minLen - 1
                sb.AppendLine($"      Public Property {StringToValidPropertyName(names(i))} As {types(i)}")
            Next

            ''' Loading data
            sb.Append($"
      Private Shared m_all As IEnumerable(Of {className})

      Public Shared ReadOnly Property All As IEnumerable(Of {className})
        Get
")

            If cacheObjects Then
                sb.Append("          If m_all IsNot Nothing Then
            Return m_all
          End If
")
            End If

            sb.Append($"          Dim l As New List(Of {className})()
          Dim c As {className}
")

            Do

                If fields Is Nothing Then
                    Continue Do
                End If
                If fields.Length < minLen Then
                    Throw New Exception("Not enough fields in CSV file.")
                End If

                sb.AppendLine($"          c = New {className}()")

                Dim value As String '= ""
                For i As Integer = 0 To minLen - 1
                    ' Wrap strings in quotes.
                    value = If(GetCsvFieldType(fields(i)) = "String", $"""{fields(i).Trim().Trim(New Char() {""""c})}""", fields(i))
                    sb.AppendLine($"          c.{names(i)} = {value}")
                Next

                sb.AppendLine("          l.Add(c)")

                fields = parser.ReadFields()

            Loop While fields IsNot Nothing

            sb.Append($"          m_all = l
          Return l
")

            ' Close things (property, class, namespace)
            sb.Append("      End Get
    End Property

  End Class

End Namespace")

            Return sb.ToString()

        End Function

        Private Shared Function StringToValidPropertyName(s As String) As String
            s = s.Trim()
            s = If(Char.IsLetter(s(0)), Char.ToUpper(s(0)) & s.Substring(1), s)
            s = If(Char.IsDigit(s.Trim()(0)), "_" & s, s)
            s = New String(s.[Select](Function(ch) If(Char.IsDigit(ch) OrElse Char.IsLetter(ch), ch, "_"c)).ToArray())
            Return s
        End Function

        Private Shared Function SourceFilesFromAdditionalFile(loadType As CsvLoadType, cacheObjects As Boolean, file As AdditionalText) As IEnumerable(Of (Name As String, Code As String))
            Dim className = Path.GetFileNameWithoutExtension(file.Path)
            Dim csvText = file.GetText().ToString()
            Return New(String, String)() {(className, GenerateClassFile(className, csvText, loadType, cacheObjects))}
        End Function

        Private Shared Function SourceFilesFromAdditionalFiles(pathsData As IEnumerable(Of (LoadType As CsvLoadType, CacheObjects As Boolean, File As AdditionalText))) As IEnumerable(Of (Name As String, Code As String))
            Return pathsData.SelectMany(Function(d) SourceFilesFromAdditionalFile(d.LoadType, d.CacheObjects, d.File))
        End Function

        Private Shared Iterator Function GetLoadOptions(context As GeneratorExecutionContext) As IEnumerable(Of (LoadType As CsvLoadType, CacheObjects As Boolean, File As AdditionalText))
            For Each file In context.AdditionalFiles
                If Path.GetExtension(file.Path).Equals(".csv", StringComparison.OrdinalIgnoreCase) Then
                    ' are there any options for it?
                    Dim loadTimeString As String = Nothing
                    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.additionalfiles.CsvLoadType", loadTimeString)
                    Dim loadType As CsvLoadType = Nothing
                    [Enum].TryParse(loadTimeString, ignoreCase:=True, loadType)
                    Dim cacheObjectsString As String = Nothing
                    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.additionalfiles.CacheObjects", cacheObjectsString)
                    Dim cacheObjects As Boolean = Nothing
                    Boolean.TryParse(cacheObjectsString, cacheObjects)
                    Yield (loadType, cacheObjects, file)
                End If
            Next
        End Function

    End Class

End Namespace
