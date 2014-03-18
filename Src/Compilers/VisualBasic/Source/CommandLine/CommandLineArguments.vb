' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' The CommandLineArguments class provides members to Set and Get Visual Basic compilation and parse options.
    ''' </summary>
    Public NotInheritable Class VisualBasicCommandLineArguments
        Inherits CommandLineArguments
        ''' <summary>
        ''' Set and Get the Visual Basic compilation options.
        ''' </summary>
        ''' <returns>The currently set Visual Basic compilation options.</returns>
        Public Overloads Property CompilationOptions As VisualBasicCompilationOptions

        ''' <summary>
        ''' Set and Get  the Visual Basic parse parse options.
        ''' </summary>
        ''' <returns>The currently set Visual Basic parse options.</returns>
        Public Overloads Property ParseOptions As VisualBasicParseOptions

        Friend OutputLevel As OutputLevel

        ''' <summary>
        ''' Gets the core Parse options.
        ''' </summary>
        ''' <returns>The currently set core parse options.</returns>
        Protected Overrides ReadOnly Property ParseOptionsCore As ParseOptions
            Get
                Return ParseOptions
            End Get
        End Property

        ''' <summary>
        ''' Gets the core compilation options.
        ''' </summary>
        ''' <returns>The currently set core compilation options.</returns>
        Protected Overrides ReadOnly Property CompilationOptionsCore As CompilationOptions
            Get
                Return CompilationOptions
            End Get
        End Property

        Friend Property DefaultCoreLibraryReference As CommandLineReference?

        Friend Sub New()
        End Sub
    End Class

    Friend Enum OutputLevel
        Quiet
        Normal
        Verbose
    End Enum

End Namespace

