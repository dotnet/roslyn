// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;

/// <summary>
/// Implemented to support project-to-project references. Despite its name, it's used for things other than Venus.
/// </summary>
[ComImport]
[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("97604d5d-7935-47fd-91c6-2747a5523855")]
internal interface ICSharpVenusProjectSite
{
    /// <summary>
    /// This function should not be used by any new code; it is now superseded by AddReferenceToCodeDirectoryEx. The
    /// function has not been removed due to the hard-dependency on this particular signature in Venus' templates.
    /// </summary>
    [Obsolete]
    void AddReferenceToCodeDirectory([MarshalAs(UnmanagedType.LPWStr)] string assemblyFileName, ICSharpProjectRoot project);

    /// <summary>
    /// Called by the venus project system to tell the project site to remove a live reference to an existing C#
    /// code directory.
    /// </summary>
    void RemoveReferenceToCodeDirectory([MarshalAs(UnmanagedType.LPWStr)] string assemblyFileName, ICSharpProjectRoot project);

    /// <summary> NOTE: This is not called by any project system in Dev11 according
    /// to http://bang/.  Remove?
    ///
    /// [Optional] Called by the Venus Project system when a file of the Project
    /// site has been updated outside the editor. This is a workaround to allow
    /// the Venus project system implement a FileSystemWatcher for C#, since
    /// the C# language service doesn't support this yet.
    /// Not calling this when a file is updated on disk means code sense
    /// will not be up to date wrt to the changes on disk.
    /// </summary>
    void OnDiskFileUpdated([MarshalAs(UnmanagedType.LPWStr)] string filename, ref System.Runtime.InteropServices.ComTypes.FILETIME pFT);

    /// <summary>Called when aliases for an import are changed</summary>
    /// <param name="project">the project whose aliases we are changing</param>
    /// <param name="previousAliasesCount">number of elements in the previousAliases array</param>
    /// <param name="currentAliasesCount">number of elements in the currentAliases array</param>
    /// <param name="currentAliases">the previous aliases for this import</param>
    /// <param name="previousAliases">the current aliases for this import</param>
    void OnCodeDirectoryAliasesChanged(ICSharpProjectRoot project,
        int previousAliasesCount, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] previousAliases,
         int currentAliasesCount, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 3)] string[] currentAliases);

    /// <summary>
    /// Called by the project system to tell the project site to create a live reference to an existing C# code
    /// directory.
    /// </summary>
    /// <param name="assemblyFileName">The assembly file specified by "assemblyFileName" doesn't need to physically
    /// exist on disk for CodeSense to work, but calling "Build" on the project will fail if the file doesn't exist
    /// at that point.</param>
    /// <param name="project">The project site for "project" must exist (i.e. BindToProject(project) must have been
    /// called prior to this call)</param>
    /// <param name="optionID">Indicates whether the reference is a regular reference or one that needs to be
    /// embedded into the target assembly (as indicated to the compiler through /link compiler option).</param>
    void AddReferenceToCodeDirectoryEx([MarshalAs(UnmanagedType.LPWStr)] string assemblyFileName, ICSharpProjectRoot project, CompilerOptions optionID);
}
