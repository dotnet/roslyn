﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class VisualBasicCommandLineArgumentsFactoryServiceTests
        Private Shared ReadOnly s_directory As String = Path.GetTempPath()

        Private ReadOnly _parser As New VisualBasicCommandLineArgumentsFactoryService()

        Private Function GetArguments(ParamArray args As String()) As VisualBasicCommandLineArguments
            Dim arguments = _parser.Parse(args, baseDirectory:=s_directory, isInteractive:=False, sdkDirectory:=s_directory)
            Return DirectCast(arguments, VisualBasicCommandLineArguments)
        End Function

        Private Function GetParseOptions(ParamArray args As String()) As VisualBasicParseOptions
            Return GetArguments(args).ParseOptions
        End Function

        <Fact>
        Public Sub FeaturesSingle()
            Dim options = GetParseOptions("/features:test")
            Assert.Equal("true", options.Features("test"))
        End Sub

        <Fact>
        Public Sub FeaturesSingleWithValue()
            Dim options = GetParseOptions("/features:test=dog")
            Assert.Equal("dog", options.Features("test"))
        End Sub

        <Fact>
        Public Sub FetauresMultiple()
            Dim options = GetParseOptions("/features:test1", "/features:test2")
            Assert.Equal("true", options.Features("test1"))
            Assert.Equal("true", options.Features("test2"))
        End Sub

        <Fact>
        Public Sub FeaturesMultipleWithValue()
            Dim options = GetParseOptions("/features:test1=dog", "/features:test2=cat")
            Assert.Equal("dog", options.Features("test1"))
            Assert.Equal("cat", options.Features("test2"))
        End Sub

    End Class

End Namespace
