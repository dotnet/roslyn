// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop
{
    [ComImport]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("BD6EE4C6-3BE2-4df9-98D5-4BB2BC874BC1")]
    internal interface ICSCompiler
    {
        ICSSourceModule CreateSourceModule(ICSSourceText text);

        /// <summary>
        /// Get the name table used by the compiler.
        /// </summary>
        ICSNameTable GetNameTable();

        /// <summary>
        /// Shutdown the compiler. This is called by whoever is "in charge" of the compiler (usually the host itself)
        /// when it is done with it.  It is used to relieve circular references between the compiler and its host.
        /// </summary>
        void Shutdown();

        ICSCompilerConfig GetConfiguration();

        ICSInputSet AddInputSet();
        void RemoveInputSet(ICSInputSet inputSet);

        void Compile(ICSCompileProgress progress);

        void BuildForEnc(ICSCompileProgress progress, ICSEncProjectServices encService, [MarshalAs(UnmanagedType.IUnknown)] object pe);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetOutputFileName();

        // WARNING: the next two methods are complicated signatures, and so these two functions (CreateParser and
        // CreateLanguageAnalysisEngine) are declared here to hold the vtable slot only. If they are needed, the
        // declarations will have to be filled out.

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateParser();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateLanguageAnalysisEngine();

        void ReleaseReservedMemory();
    }
}
