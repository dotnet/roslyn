// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop
{
    [ComImport]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5D41CF84-3A90-48d6-A445-CA22E125B691")]
    internal interface ICSharpProjectSite
    {
        /// This function is called when the project is being closed.  The site
        /// should release any referenced pointers it has on the project object.
        void Disconnect();

        // Returns the C# compiler
        void GetCompiler(out ICSCompiler compiler, out ICSInputSet inputSet);

        // Check the timestamps for all input files (including all references
        // and resource files) against the given timestamp and set pfMustBuild
        // accordingly.
        bool CheckInputFileTimes(System.Runtime.InteropServices.ComTypes.FILETIME output);

        // Build the project using current configuration
        void BuildProject([MarshalAs(UnmanagedType.IUnknown)] object progress);

        /// <summary>
        /// This function is unused and unimplemented. It is just a placeholder in the vtable for this public interface.
        /// </summary>
        /// <remarks>See http://psph/devdiv~bugs/114172. </remarks>
        [PreserveSig]
        void Unused();

        // Called when source files are added/removed to/from project
        void OnSourceFileAdded([MarshalAs(UnmanagedType.LPWStr)] string filename);
        void OnSourceFileRemoved([MarshalAs(UnmanagedType.LPWStr)] string filename);

        // Called when resource files are added/removed to/from project
        [PreserveSig]
        int OnResourceFileAdded([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string resourceName, bool embedded);
        [PreserveSig]
        int OnResourceFileRemoved([MarshalAs(UnmanagedType.LPWStr)] string filename);

        // NOTICE: OnImportAdded is superseded by OnImportAddedEx.
        // The function has not been removed due to the hard-dependency on this particular signature in Venus' 
        // templates
        [PreserveSig]
        int OnImportAdded([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string project);

        // Called when references (imports) are removed.  If the reference is a
        // project-reference, project specifies the project uniquely.  Otherwise it
        // is NULL.  On removal, it is up to the callee to know whether the file specified
        // as a project reference. 
        void OnImportRemoved([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string project);

        // Called when the output file name for the active configuration has changed
        void OnOutputFileChanged([MarshalAs(UnmanagedType.LPWStr)] string filename);

        // Called when the active configuration has changed
        void OnActiveConfigurationChanged([MarshalAs(UnmanagedType.LPWStr)] string configName);

        /// <summary>
        /// Called when the project is fully loaded -- used to signal the background parsing thread to begin processing
        /// project files.
        /// </summary>
        void OnProjectLoadCompletion();

        // Called to obtain the top-level Code Model object for this project
        [PreserveSig]
        int CreateCodeModel([MarshalAs(UnmanagedType.IUnknown)] object parent, out EnvDTE.CodeModel codeModel);

        // Called to obtain a file-level Code Model object for the given file
        [PreserveSig]
        int CreateFileCodeModel([MarshalAs(UnmanagedType.LPWStr)] string fileName, [MarshalAs(UnmanagedType.IUnknown)] object parent, out EnvDTE.FileCodeModel fileCodeModel);

        // Called when references (imports) are added/removed
        void OnModuleAdded([MarshalAs(UnmanagedType.LPWStr)] string filename);
        void OnModuleRemoved([MarshalAs(UnmanagedType.LPWStr)] string filename);

        // Called to obtain the list of classes from the project that have a valid Main()
        // method (used to populate the Startup Object list).  If ppszClassNames is NULL,
        // piCount is filled with the number of classes.  Otherwise, up to *piCount names
        // are copied to ppszClassNames.  *piCount is always filled with the total number
        // of startup classes -- the function returns S_FALSE if there wasn't enough
        // room in ppszClassNames provided.  Note that because the names returned are in
        // the name table, they don't need to be released/freed.
        [PreserveSig]
        int GetValidStartupClasses([Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysUInt, SizeParamIndex = 1)] IntPtr[] ppszClassNames, ref int picount);

        // Called when aliases for an import are changed
        // file : The name of the reference file we are changing the aliases for
        // project : If the reference is to a project, then project specifies that project uniquely
        // previousAliasesCount : number of elements in the previousAliases array
        // currentAliasesCount : number of elements in the currentAliases array
        // previousAliases : the previous aliases for this import
        // currentAliases : the current aliases for this import
        void OnAliasesChanged(
            [MarshalAs(UnmanagedType.LPWStr)] string file,
            [MarshalAs(UnmanagedType.LPWStr)] string project,
            int previousAliasesCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)] string[] previousAliases,
            int currentAliasesCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 4)] string[] currentAliases);

        /// <summary>
        /// Called when references (imports) are added. If the reference is a project-reference, project specifies the
        /// project uniquely. Otherwise it is NULL. On removal, it is up to the callee to know whether the file
        /// specified as a project reference.
        /// </summary>
        /// <param name="filename">The filename to add a reference to.</param>
        /// <param name="project">If the reference being added is a project reference, then this is a non-null string
        /// that defines this uniquely.</param>
        /// <param name="optionID">A CompilerOption enumeration indicating whether the reference is a regular reference 
        /// (OPTID_IMPORTS) or the one that needs to be embedded into the target assembly
        /// (OPTID_IMPORTSUSINGNOPIA).</param>
        [PreserveSig]
        int OnImportAddedEx([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string project, CompilerOptions optionID);
    }
}
