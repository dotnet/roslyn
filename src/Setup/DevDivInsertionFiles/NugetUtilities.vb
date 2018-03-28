Imports System.IO
Imports System.Runtime.InteropServices

Public Class NugetUtilities
    Private Const PackageNamePrefix = "VS.ExternalAPIs."
    Private Const PackageExtension = ".nupkg"

    Public Shared Sub ParsePackageFileName(fileName As String, <Out> ByRef libraryName As String, <Out> ByRef versionStr As String)
        If Not fileName.EndsWith(PackageExtension) Then
            Throw New InvalidDataException($"Invalid package name: '{fileName}'")
        End If

        Dim nameStart = If(fileName.StartsWith(PackageNamePrefix), PackageNamePrefix.Length, 0)
        Dim parts = fileName.Substring(nameStart, fileName.Length - nameStart - PackageExtension.Length).Split("."c)

        Dim firstNumber As Integer = IndexOfNumericPart(parts)
        If firstNumber = -1 Then
            Throw New InvalidDataException($"Invalid package name: '{fileName}'")
        End If

        libraryName = String.Join(".", parts.Take(firstNumber))
        versionStr = String.Join(".", parts.Skip(firstNumber))
    End Sub

    Private Shared Function IndexOfNumericPart(parts As String()) As Integer
        For i = 0 To parts.Length - 1
            Dim number As Integer
            If Integer.TryParse(parts(i), number) Then
                Return i
            End If
        Next

        Return -1
    End Function
End Class
