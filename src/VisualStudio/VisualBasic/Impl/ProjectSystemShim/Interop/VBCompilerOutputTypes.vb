' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    Friend Enum VBCompilerOutputTypes
        ''' <summary>
        ''' This indicates that the compiler will not attempt to generate an output assembly. The
        ''' compiler can be used for UI features (e.g. Intellisense), but not for building an output
        ''' assembly. The compiler assumes Library output so it will not generate any errors for
        ''' missing Sub Main.
        ''' </summary>
        OUTPUT_None

        ''' <summary>
        ''' The current default. Produces an application that has a console window. The classes
        ''' defined inside of the EXE cannot be expose outside the EXE.
        ''' </summary>
        OUTPUT_ConsoleEXE

        ''' <summary>
        ''' Produces an application that does not have a console window. The classes defined inside
        ''' of the EXE cannot be exposed outside of the EXE.
        ''' </summary>
        OUTPUT_WindowsEXE

        ''' <summary>
        ''' Produces a DLL that may expose classes outside of itself.
        ''' </summary>
        OUTPUT_Library

        ''' <summary>
        ''' Produces a module that must be consumed by another assembly.
        ''' </summary>
        OUTPUT_Module

        ''' <summary>
        ''' Produces an app that runs in Appcontainer
        ''' </summary>
        OUTPUT_AppContainerEXE

        ''' <summary>
        ''' Produces the intermediary file that feeds into WinMDExp to produce a Windows
        ''' Runtime Metadata assembly
        ''' </summary>
        OUTPUT_WinMDObj
    End Enum
End Namespace
