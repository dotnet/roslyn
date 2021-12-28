' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <Guid(Guids.VbCompilerProjectIdString), ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IVbCompilerProject
        ''' <summary>
        ''' Set the compiler options. The strings in this structure are only guaranteed to be alive
        ''' during this function call.
        ''' </summary>
        Sub SetCompilerOptions(<[In]> ByRef pCompilerOptions As VBCompilerOptions)

        ''' <summary>
        ''' Called each time a file is added to the project (via Add Item or during project open).
        ''' </summary>
        ''' <param name="wszFileName">The filename to add.</param>
        ''' <param name="itemid">The VSITEMID of the item.</param>
        ''' <param name="fAddDuringOpen">Set if this file is being added during solution
        ''' open.</param>
        Sub AddFile(<MarshalAs(UnmanagedType.LPWStr)> wszFileName As String, itemid As UInteger, fAddDuringOpen As Boolean)

        ''' <summary>
        ''' Called when a file is removed from the project.
        ''' </summary>
        Sub RemoveFile(<MarshalAs(UnmanagedType.LPWStr)> wszFileName As String, itemid As UInteger)

        ''' <summary>
        ''' Called when a file is renamed.
        ''' </summary>
        Sub RenameFile(
            <MarshalAs(UnmanagedType.LPWStr)> wszOldFileName As String,
            <MarshalAs(UnmanagedType.LPWStr)> wszNewFileName As String,
            itemid As UInteger)

        ''' <summary>
        ''' Called when a file is removed from the project, but you don't have an itemid.
        ''' </summary>
        Sub RemoveFileByName(<MarshalAs(UnmanagedType.LPWStr)> wszPath As String)

        ''' <summary>
        ''' Called by VBA to introduce an in-memory buffer to the compiler project.
        ''' </summary>
        Sub AddBuffer(
            <MarshalAs(UnmanagedType.LPWStr)> wszBuffer As String,
            dwLen As Integer,
            <MarshalAs(UnmanagedType.LPWStr)> wszMkr As String,
            itemid As UInteger,
            fAdvise As Boolean,
            fShowErrorsInTaskList As Boolean)

        ''' <summary>
        ''' Called by VBA to give memory-backed IStream to ISymWriter for PDB symbol store MetaEmit
        ''' will AddRef and hold the pointer until MetaEmit is destroyed.
        ''' </summary>
        Sub SetStreamForPDB(pStreamPDB As IStream)

        ''' <summary>
        ''' Add a reference to another project within the current solution.
        ''' </summary>
        ''' <param name="pReferencedCompilerProject">The project to reference.</param>
        Sub AddProjectReference(pReferencedCompilerProject As IVbCompilerProject)

        ''' <summary>
        ''' Removes a reference to another project in the current solution.
        ''' </summary>
        ''' <param name="pReferencedCompilerProject">The project to no longer be referenced.</param>
        Sub RemoveProjectReference(pReferencedCompilerProject As IVbCompilerProject)

        ''' <summary>
        ''' Adds a reference to a MetaData file.
        ''' </summary>
        <PreserveSig()>
        Function AddMetaDataReference(
            <MarshalAs(UnmanagedType.LPWStr)> wszFileName As String,
            bAssembly As Boolean
        ) As <MarshalAs(UnmanagedType.Error)> Integer

        ''' <summary>
        ''' Removes a reference to a Metadata file.
        ''' </summary>
        Sub RemoveMetaDataReference(<MarshalAs(UnmanagedType.LPWStr)> wszFileName As String)

        ''' <summary>
        ''' Removes all project-to-project and metadata references.
        ''' </summary>
        Sub RemoveAllReferences()

        ''' <summary>
        ''' Add an import.
        ''' </summary>
        Sub AddImport(<MarshalAs(UnmanagedType.LPWStr)> wszImport As String)

        ''' <summary>
        ''' Remove an import.
        ''' </summary>
        Sub DeleteImport(<MarshalAs(UnmanagedType.LPWStr)> wszImport As String)

        ''' <summary>
        ''' Add a reference to a resource file
        ''' </summary>
        Sub AddResourceReference(
            <MarshalAs(UnmanagedType.LPWStr)> wszFileName As String,
            <MarshalAs(UnmanagedType.LPWStr)> wszName As String,
            fPublic As Boolean,
            fEmbed As Boolean)

        ''' <summary>
        ''' Removes all resource file references.
        ''' </summary>
        Sub DeleteAllResourceReferences()

        ''' <summary>
        ''' Notification that a "build" is starting. Since the compiler may be running in the
        ''' background, this might not mean anything more than to disable some UI.
        ''' </summary>
        Sub StartBuild(
            <MarshalAs(UnmanagedType.Interface)> pVsOutputWindowPane As IVsOutputWindowPane,
            fRebuildAll As Boolean)

        ''' <summary>
        ''' This is called if the user wishes to stop a "build". Since the compiler will always be
        ''' running, its only effect might be to re-enable UI that is disabled during a build.
        ''' </summary>
        Sub StopBuild()

        ''' <summary>
        ''' Disconnects from the project and event source, etc.
        ''' </summary>
        Sub Disconnect()

        ''' <summary>
        ''' Lists all classes with Sub Main marked as shared (entry points). If called with cItems =
        ''' 0 and pcActualItems != NULL, GetEntryPointsList returns in pcActualItems the number of
        ''' items available. When called with cItems != 0, GetEntryPointsList assumes that there is
        ''' enough space in strList[] for that many items, and fills up the array with those items
        ''' (up to maximum available). Returns in pcActualItems the actual number of items that
        ''' could be put in the array (this can be &lt; or &gt; cItems). Assumes that the caller
        ''' takes care of array allocation and de-allocation. 
        ''' </summary>
        Sub GetEntryPointsList(
            cItems As Integer,
            <Out, MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.BStr, SizeParamIndex:=0)> strList() As String,
            <Out> ByVal pcActualItems As IntPtr)

        ' Between these calls, the project will notify the compiler of multiple file changes. This
        ' is really just an optimization so the compiler can interrupt the background compile thread
        ' once instead of having to interrupt it every time the project adds a file to the list of
        ' compiled things (which happens once for each file during project load).

        Sub StartEdit()
        Sub FinishEdit()

        ''' <summary>
        ''' Advises the IVsBuildStatusCallback of build events.
        ''' </summary>
        ''' <returns>The cookie to be passed to UnadviseBuildStatusCallback.</returns>
        Function AdviseBuildStatusCallback(
            <MarshalAs(UnmanagedType.Interface)> pIVbBuildStatusCallback As IVbBuildStatusCallback) As UInteger
        Sub UnadviseBuildStatusCallback(
            dwCookie As UInteger)

        ''' <summary>This method call is synchronous.</summary>
        ''' <returns>
        ''' S_FALSE: this project doesn't need rebuilding in this program. S_OK: rebuild succeeded
        ''' must set out_ppUpdate. Any FAILED(hr): build errors.</returns>
        <PreserveSig()>
        Function ENCRebuild(
            <MarshalAs(UnmanagedType.IUnknown)> in_pProgram As Object,
            <Out, MarshalAs(UnmanagedType.IUnknown)> ByRef out_ppUpdate As Object
        ) As <MarshalAs(UnmanagedType.Error)> Integer

        Sub StartDebugging()
        Sub StopDebugging()

        ''' <summary>
        ''' Get the in-memory PE image. Will return NULL if we are not compiling to memory.
        ''' </summary>
        Sub GetPEImage(<Out> ByRef ppImage As IntPtr)

        ''' <summary>
        ''' Creates a CodeModel object.
        ''' </summary>
        <PreserveSig()>
        Function CreateCodeModel(
            <MarshalAs(UnmanagedType.Interface)> pProject As EnvDTE.Project,
            <MarshalAs(UnmanagedType.Interface)> pProjectItem As EnvDTE.ProjectItem,
            <Out, MarshalAs(UnmanagedType.Interface)> ByRef pCodeModel As EnvDTE.CodeModel
        ) As <MarshalAs(UnmanagedType.Error)> Integer

        ''' <summary>
        ''' Creates a FileCodeModel object.
        ''' </summary>
        <PreserveSig()>
        Function CreateFileCodeModel(
            <MarshalAs(UnmanagedType.Interface)> pProject As EnvDTE.Project,
            <MarshalAs(UnmanagedType.Interface)> pProjectItem As EnvDTE.ProjectItem,
            <Out, MarshalAs(UnmanagedType.Interface)> ByRef pFileCodeModel As EnvDTE.FileCodeModel
        ) As <MarshalAs(UnmanagedType.Error)> Integer

        ''' <summary>
        ''' Gets the proc and class for the indicated SourceFile at the indicated line number (used
        ''' by VSA) BSTR's are (potentially) allocated by the method, and, if so, it is then freed
        ''' by caller.
        ''' </summary>
        Sub GetMethodFromLine(
            itemid As UInteger,
            iLine As Integer,
            <Out, MarshalAs(UnmanagedType.BStr)> ByRef pBstrProcName As String,
            <Out, MarshalAs(UnmanagedType.BStr)> ByRef pBstrClassName As String)

        ''' <summary>
        ''' Add an "application object" variable to the project. These variables have special
        ''' binding rules and are only used by VBA. You should do a "StartEdit" and "FinishEdit"
        ''' around a set of calls to these methods.
        ''' </summary>
        Sub AddApplicationObjectVariable(
            <MarshalAs(UnmanagedType.LPStr)> wszClassName As String,
            <MarshalAs(UnmanagedType.LPStr)> wszMemberName As String)

        ''' <summary>
        ''' Remove all of the above variables.
        ''' </summary>
        Sub RemoveAllApplicationObjectVariables()

        ''' <summary>
        ''' Removes all imports
        ''' </summary>
        Sub DeleteAllImports()

        ''' <summary>
        ''' Returns the background compiler to a normal thread priority.
        ''' </summary>
        Sub SetBackgroundCompilerPriorityNormal()

        ''' <summary>
        ''' Sets the background compiler to a low thread priority. The caller is responsible for
        ''' calling SetBackgroundCompilerPriorityNormal later.
        ''' </summary>
        Sub SetBackgroundCompilerPriorityLow()

        ''' <summary>
        ''' Called when the project is renamed. If it fails, it should throw an exception for
        ''' E_FAIL.
        ''' </summary>
        ''' <param name="wszNewProjectName">The new name of the project.</param>
        Sub RenameProject(<MarshalAs(UnmanagedType.LPWStr)> wszNewProjectName As String)

        ''' <summary>
        ''' Blocks the foreground thread until the background is in bound state for this project.
        ''' </summary>
        Sub WaitUntilBound()

        Sub RenameDefaultNamespace(<MarshalAs(UnmanagedType.BStr)> bstrDefaultNamespace As String)

        ''' <summary>
        ''' Call with a NULL pointer to obtain the number of references, then pass in an array and
        ''' the size to receive the actual references.
        ''' </summary>
        <PreserveSig()> _
        Function GetDefaultReferences(
            cElements As Integer,
            <Out, MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.BStr, SizeParamIndex:=0)> ByRef rgbstrReferences() As String,
            <Out> ByVal cActualReferences As IntPtr
        ) As <MarshalAs(UnmanagedType.Error)> Integer

        ''' <summary>
        ''' Disable posting compiler messages to avoid filling up the message queue.
        ''' </summary>
        Sub SuspendPostedNotifications()

        ''' <summary>
        ''' Enable posting compiler messages.
        ''' </summary>
        Sub ResumePostedNotifications()

        ''' Set the module assembly name option. Not set above since its a VBC only setting.
        Sub SetModuleAssemblyName(<MarshalAs(UnmanagedType.LPWStr)> wszName As String)

        <PreserveSig()>
        Function AddEmbeddedMetaDataReference(
            <MarshalAs(UnmanagedType.LPWStr)> wszFileName As String) As Integer

        Sub AddEmbeddedProjectReference(
            <MarshalAs(UnmanagedType.Interface)> pReferencedCompilerProject As IVbCompilerProject)
    End Interface
End Namespace
