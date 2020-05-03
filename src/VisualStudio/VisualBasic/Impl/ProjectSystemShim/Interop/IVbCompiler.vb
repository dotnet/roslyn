' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <Guid("7E59809E-4680-11D2-B48A-0000F87572EB"), ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IVbCompiler
        ''' <summary>
        ''' Create an instance of a VB compiler project. The caller is responsible for ensuring this project is unique.
        ''' </summary>
        ''' <param name="wszName">The name of the project.</param>
        ''' <param name="punkProject">May be NULL, must allow us to navigate to an instance of ILangReferenceManager in the IDE.</param>
        ''' <param name="pProjHier">The project's IVsHierarchy. May be null.</param>
        ''' <param name="pVbCompilerHost">The IVbCompilerHost for this project.</param>
        ''' <returns>A new instance of IVbCompilerProject.</returns>
        Function CreateProject(
            <MarshalAs(UnmanagedType.LPWStr)> wszName As String,
            <MarshalAs(UnmanagedType.IUnknown)> punkProject As Object,
            <MarshalAs(UnmanagedType.Interface)> pProjHier As IVsHierarchy,
            <MarshalAs(UnmanagedType.Interface)> pVbCompilerHost As IVbCompilerHost) As <MarshalAs(UnmanagedType.Interface)> IVbCompilerProject

        ''' <summary>
        ''' Synchronously compile all projects in this compiler. Not to be used from the IDE.
        ''' </summary>
        <PreserveSig>
        Function Compile(
            ByVal pcWarnings As IntPtr,
            ByVal pcErrors As IntPtr,
            ByVal ppErrors As IntPtr) As Integer

        ''' <summary>
        ''' Sets the output level specified at the command line.
        ''' </summary>
        Sub SetOutputLevel(OutputLevel As OutputLevel)

        ''' <summary>
        ''' Array of DEBUG_SWITCHES. NOTE: These switches are for debugging purposes only and are
        ''' set process-wide. Any calls to this method after the first one will overwrite the values
        ''' from the previous calls.
        ''' </summary>
        Sub SetDebugSwitches(dbgSwitches() As Boolean)

        ' // 
        ' // Other methods.
        ' //

        Function IsValidIdentifier(<MarshalAs(UnmanagedType.LPWStr)> wszIdentifier As String) As Boolean

        ' // 
        ' // Support for targeting multiple platforms.
        ' //

        Sub RegisterVbCompilerHost(<MarshalAs(UnmanagedType.Interface)> pVbCompilerHost As IVbCompilerHost)

        ''' <summary>
        ''' Set Watson behavior.
        ''' </summary>
        Sub SetWatsonType(WatsonType As WatsonType, WatsonLcid As Integer, <MarshalAs(UnmanagedType.LPWStr)> wszAdditionalFiles As String)

        ''' <summary>
        ''' Signal the background compiler to stop.
        ''' </summary>
        Sub StopBackgroundCompiler()

        ''' <summary>
        ''' Signal the background compiler to start.
        ''' </summary>
        Sub StartBackgroundCompiler()

        ''' <summary>
        ''' Set the logging options for the compiler. Should only be called once and from the
        ''' command line compiler.
        ''' </summary>
        Sub SetLoggingOptions(options As UInteger)
    End Interface
End Namespace
