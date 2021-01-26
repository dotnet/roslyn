' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure VBCompilerOptions
        ''' <summary>
        ''' The name of the output EXE (base filename + ext).
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszExeName As String

        ''' <summary>
        ''' The name of the XML documentation file (base filename + ext).
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszXMLDocName As String

        ''' <summary>
        ''' The path to build the outputs to. This is the directory to build the set of outputs
        ''' produced by this project.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszOutputPath As String

        ''' <summary>
        ''' The path to store temporary goo.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszTemporaryPath As String

        ''' <summary>
        ''' The project output type.
        ''' </summary>
        Public OutputType As VBCompilerOutputTypes

        ''' <summary>
        ''' The default namespace for types not declared within a Namespace statement.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszDefaultNamespace As String

        ''' <summary>
        ''' Start startup module contains the full namespace name of the module we will create the
        ''' first entry point into. If this is NULL or the empty string, the compiler will find a
        ''' "Sub Main" in the project and use that.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszStartup As String

        ''' <summary>
        ''' The list of project-level conditional compilation symbols.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszCondComp As String

        ''' <summary>
        ''' Don't emit integer overflow checks on integer operations.
        ''' </summary>
        Public bRemoveIntChecks As Boolean

        ''' <summary>
        ''' Is Option Strict on or off by default?
        ''' </summary>
        Public bOptionStrictOff As Boolean

        ''' <summary>
        ''' Is Option Explicit on or off by default?
        ''' </summary>
        Public bOptionExplicitOff As Boolean

        ''' <summary>
        ''' Is Option Compare Text or Binary by default?
        ''' </summary>
        Public bOptionCompareText As Boolean

        ''' <summary>
        ''' Is Option Infer On or Off by default?
        ''' </summary>
        Public bOptionInferOff As Boolean

        ''' <summary>
        ''' Generate debuggable code (insert NOPs for easy stepping, etc) and a PDB.
        ''' </summary>
        Public bGenerateSymbolInfo As Boolean

        ''' <summary>
        ''' Don't mess with the code at all, but do generate a PDB. This makes "real" retail
        ''' debugging possible.
        ''' </summary>
        Public bGeneratePdbOnly As Boolean

        ''' <summary>
        ''' Optimize the code generated.
        ''' </summary>
        Public bOptimize As Boolean

        ''' <summary>
        ''' Full path to key pair file used to create strong-named (public) assemblies
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszStrongNameKeyFile As String

        ''' <summary>
        ''' Name of the key container to use.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszStrongNameContainer As String

        ''' <summary>
        ''' Whether the assembly is being delay signed.
        ''' </summary>
        Public bDelaySign As Boolean

        ''' <summary>
        ''' Win32 resource file. Mutually exclusive with wszIconFile.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszWin32ResFile As String

        ''' <summary>
        ''' Win32 icon file. Mutually exclusive with wszWin32ResFile.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszIconFile As String

        ''' <summary>
        ''' Preferred load address.
        ''' </summary>
        Public dwLoadAddress As UIntPtr

        Public WarningLevel As WarningLevel

        ''' <summary>
        ''' List of Disabled Warnings
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszDisabledWarnings As String

        ''' <summary>
        ''' List of Warnings as Errors
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszWarningsAsErrors As String

        ''' <summary>
        ''' List of Warnings Not as Errors
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszWarningsNotAsErrors As String

        ''' <summary>
        ''' Obsolete, but apparently was used by the native compiler to determine if it's an
        ''' App_Code project from Venus. Such a hack should be removed from their project system.
        ''' See SetCompilerOptionsInternal in CompilerProject.cpp for details.
        ''' </summary>
        <Obsolete()>
        Public bEnableIncrementalCompilation As Boolean

        ''' <summary>
        ''' Preferred file alignment.
        ''' </summary>
        Public dwAlign As UInteger

        ''' <summary>
        ''' Default codepage to use when the compiler loads a file. 0 = none.
        ''' </summary>
        Public dwDefaultCodePage As UInteger

        ''' <summary>
        ''' String indicating the platform type to limit the compiling assembly to.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszPlatformType As String

        ''' <summary>
        ''' Don't add standard libraries.
        ''' </summary>
        Public bNoStandardLibs As Boolean

        ''' <summary>
        ''' Manifest full file path. If null, no manifest will be used.
        ''' </summary>
        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszUacManifestFile As String

        ''' <summary>
        ''' Language version option.
        ''' </summary>
        Public langVersion As LanguageVersion

        ''' <summary>
        ''' The VB Runtime Kind
        ''' </summary>
        Public vbRuntimeKind As VBRuntimeKind

        <MarshalAs(UnmanagedType.LPWStr)>
        Public wszSpecifiedVBRuntime As String

        ''' <summary>
        ''' Emit with the HighEntropyVA bit set in PE
        ''' </summary>
        Public bHighEntropyVA As Boolean

        ''' <summary>
        ''' Subsystem version in PE
        ''' </summary>
        Public wszSubsystemVersion As String
    End Structure
End Namespace
