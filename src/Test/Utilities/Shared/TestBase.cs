// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Base class for all unit test classes.
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        private TempRoot _temp;

        protected TestBase()
        {
        }

        public static string GetUniqueName()
        {
            return Guid.NewGuid().ToString("D");
        }

        public TempRoot Temp
        {
            get
            {
                if (_temp == null)
                {
                    _temp = new TempRoot();
                }

                return _temp;
            }
        }

        public virtual void Dispose()
        {
            if (_temp != null)
            {
                _temp.Dispose();
            }
        }

        #region Metadata References

        private static MetadataReference[] s_lazyDefaultVbReferences;
        private static MetadataReference[] s_lazyLatestVbReferences;

        public static MetadataReference[] DefaultVbReferences => s_lazyDefaultVbReferences ??
            (s_lazyDefaultVbReferences = new[] { MscorlibRef, SystemRef, SystemCoreRef, MsvbRef });

        public static MetadataReference[] LatestVbReferences = s_lazyLatestVbReferences ??
            (s_lazyLatestVbReferences = new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, MsvbRef_v4_0_30319_17929 });

        private static MetadataReference[] s_winRtRefs;
        private static MetadataReference[] s_portableRefsMinimal;

        /// <summary>
        /// The array of 7 metadataimagereferences that are required to compile
        /// against windows.winmd (including windows.winmd itself).
        /// </summary>
        public static MetadataReference[] WinRtRefs
        {
            get
            {
                if (s_winRtRefs == null)
                {
                    var winmd = AssemblyMetadata.CreateFromImage(TestResources.WinRt.Windows).GetReference(display: "Windows");

                    var windowsruntime =
                        AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_Runtime_WindowsRuntime).GetReference(display: "System.Runtime.WindowsRuntime.dll");

                    var runtime =
                        AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_Runtime).GetReference(display: "System.Runtime.dll");

                    var objectModel =
                        AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_ObjectModel).GetReference(display: "System.ObjectModel.dll");


                    var uixaml = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_Runtime_WindowsRuntime_UI_Xaml).
                        GetReference(display: "System.Runtime.WindowsRuntime.UI.Xaml.dll");


                    var interop = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_Runtime_InteropServices_WindowsRuntime).
                        GetReference(display: "System.Runtime.InteropServices.WindowsRuntime.dll");

                    //Not mentioned in the adapter doc but pointed to from System.Runtime, so we'll put it here.
                    var system = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System).GetReference(display: "System.dll");

                    var mscor = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30316_17626.mscorlib).GetReference(display: "mscorlib");

                    s_winRtRefs = new MetadataReference[] { winmd, windowsruntime, runtime, objectModel, uixaml, interop, system, mscor };
                }

                return s_winRtRefs;
            }
        }

        /// <summary>
        /// The array of minimal references for portable library (mscorlib.dll and System.Runtime.dll)
        /// </summary>
        public static MetadataReference[] PortableRefsMinimal
        {
            get
            {
                if (s_portableRefsMinimal == null)
                {
                    s_portableRefsMinimal = new MetadataReference[] { MscorlibPP7Ref, SystemRuntimePP7Ref };
                }

                return s_portableRefsMinimal;
            }
        }


        /// <summary>
        /// Reference to an assembly that defines Expression Trees.
        /// </summary>
        public static MetadataReference ExpressionAssemblyRef => SystemCoreRef;

        /// <summary>
        /// Reference to an assembly that defines LINQ operators.
        /// </summary>
        public static MetadataReference LinqAssemblyRef => SystemCoreRef;

        /// <summary>
        /// Reference to an assembly that defines ExtensionAttribute.
        /// </summary>
        public static MetadataReference ExtensionAssemblyRef => SystemCoreRef;

        private static MetadataReference s_systemCoreRef_v4_0_30319_17929;
        public static MetadataReference SystemCoreRef_v4_0_30319_17929
        {
            get
            {
                if (s_systemCoreRef_v4_0_30319_17929 == null)
                {
                    s_systemCoreRef_v4_0_30319_17929 = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_Core).GetReference(display: "System.Core.v4_0_30319_17929.dll");
                }

                return s_systemCoreRef_v4_0_30319_17929;
            }
        }

        private static MetadataReference s_systemCoreRef;
        public static MetadataReference SystemCoreRef
        {
            get
            {
                if (s_systemCoreRef == null)
                {
                    s_systemCoreRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Core).GetReference(display: "System.Core.v4_0_30319.dll");
                }

                return s_systemCoreRef;
            }
        }

        private static MetadataReference s_systemWindowsFormsRef;
        public static MetadataReference SystemWindowsFormsRef
        {
            get
            {
                if (s_systemWindowsFormsRef == null)
                {
                    s_systemWindowsFormsRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Windows_Forms).GetReference(display: "System.Windows.Forms.v4_0_30319.dll");
                }

                return s_systemWindowsFormsRef;
            }
        }

        private static MetadataReference s_systemDrawingRef;
        public static MetadataReference SystemDrawingRef
        {
            get
            {
                if (s_systemDrawingRef == null)
                {
                    s_systemDrawingRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Drawing).GetReference(display: "System.Drawing.v4_0_30319.dll");
                }

                return s_systemDrawingRef;
            }
        }

        private static MetadataReference s_systemDataRef;
        public static MetadataReference SystemDataRef
        {
            get
            {
                if (s_systemDataRef == null)
                {
                    s_systemDataRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Data).GetReference(display: "System.Data.v4_0_30319.dll");
                }

                return s_systemDataRef;
            }
        }

        private static MetadataReference s_mscorlibRef;
        public static MetadataReference MscorlibRef
        {
            get
            {
                if (s_mscorlibRef == null)
                {
                    s_mscorlibRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib).GetReference(display: "mscorlib.v4_0_30319.dll");
                }

                return s_mscorlibRef;
            }
        }

        private static MetadataReference s_mscorlibRefPortable;
        public static MetadataReference MscorlibRefPortable
        {
            get
            {
                if (s_mscorlibRefPortable == null)
                {
                    s_mscorlibRefPortable = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib_portable).GetReference(display: "mscorlib.v4_0_30319.portable.dll");
                }

                return s_mscorlibRefPortable;
            }
        }

        private static MetadataReference s_aacorlibRef;
        public static MetadataReference AacorlibRef
        {
            get
            {
                if (s_aacorlibRef == null)
                {
                    var source = TestResources.NetFX.aacorlib_v15_0_3928.aacorlib_v15_0_3928_cs;
                    var syntaxTree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(source);

                    var compilationOptions = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

                    var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("aacorlib.v15.0.3928.dll", new[] { syntaxTree }, null, compilationOptions);

                    Stream dllStream = new MemoryStream();
                    var emitResult = compilation.Emit(dllStream);
                    if (!emitResult.Success)
                    {
                        emitResult.Diagnostics.Verify();
                    }
                    dllStream.Seek(0, SeekOrigin.Begin);

                    s_aacorlibRef = AssemblyMetadata.CreateFromStream(dllStream).GetReference(display: "mscorlib.v4_0_30319.dll");
                }

                return s_aacorlibRef;
            }
        }

        private static MetadataReference s_mscorlibRef_v4_0_30316_17626;
        public static MetadataReference MscorlibRef_v4_0_30316_17626
        {
            get
            {
                if (s_mscorlibRef_v4_0_30316_17626 == null)
                {
                    s_mscorlibRef_v4_0_30316_17626 = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30316_17626.mscorlib).GetReference(display: "mscorlib.v4_0_30319_17626.dll", filePath: @"Z:\FxReferenceAssembliesUri");
                }

                return s_mscorlibRef_v4_0_30316_17626;
            }
        }

        private static MetadataReference s_mscorlibRef_v20;
        public static MetadataReference MscorlibRef_v20
        {
            get
            {
                if (s_mscorlibRef_v20 == null)
                {
                    s_mscorlibRef_v20 = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v2_0_50727.mscorlib).GetReference(display: "mscorlib.v2.0.50727.dll");
                }

                return s_mscorlibRef_v20;
            }
        }

        /// <summary>
        /// Reference to an mscorlib silverlight assembly in which the System.Array does not contain the special member LongLength.
        /// </summary>
        private static MetadataReference s_mscorlibRef_silverlight;
        public static MetadataReference MscorlibRefSilverlight
        {
            get
            {
                if (s_mscorlibRef_silverlight == null)
                {
                    s_mscorlibRef_silverlight = AssemblyMetadata.CreateFromImage(TestResources.NetFX.silverlight_v5_0_5_0.mscorlib_v5_0_5_0_silverlight).GetReference(display: "mscorlib.v5.0.5.0_silverlight.dll");
                }

                return s_mscorlibRef_silverlight;
            }
        }

        private static MetadataReference s_minCorlibRef;
        public static MetadataReference MinCorlibRef
        {
            get
            {
                if (s_minCorlibRef == null)
                {
                    s_minCorlibRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.Minimal.mincorlib).GetReference(display: "minCorLib.dll");
                }

                return s_minCorlibRef;
            }
        }

        private static MetadataReference s_msvbRef;
        public static MetadataReference MsvbRef
        {
            get
            {
                if (s_msvbRef == null)
                {
                    s_msvbRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.Microsoft_VisualBasic).GetReference(display: "Microsoft.VisualBasic.v4_0_30319.dll");
                }

                return s_msvbRef;
            }
        }

        private static MetadataReference s_msvbRef_v4_0_30319_17929;
        public static MetadataReference MsvbRef_v4_0_30319_17929
        {
            get
            {
                if (s_msvbRef_v4_0_30319_17929 == null)
                {
                    s_msvbRef_v4_0_30319_17929 = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.Microsoft_VisualBasic).GetReference(display: "Microsoft.VisualBasic.v4_0_30319_17929.dll");
                }

                return s_msvbRef_v4_0_30319_17929;
            }
        }

        private static MetadataReference s_csharpRef;
        public static MetadataReference CSharpRef
        {
            get
            {
                if (s_csharpRef == null)
                {
                    s_csharpRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.Microsoft_CSharp).GetReference(display: "Microsoft.CSharp.v4.0.30319.dll");
                }

                return s_csharpRef;
            }
        }


        private static MetadataReference s_systemRef;
        public static MetadataReference SystemRef
        {
            get
            {
                if (s_systemRef == null)
                {
                    s_systemRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System).GetReference(display: "System.v4_0_30319.dll");
                }

                return s_systemRef;
            }
        }

        private static MetadataReference s_systemRef_v4_0_30319_17929;
        public static MetadataReference SystemRef_v4_0_30319_17929
        {
            get
            {
                if (s_systemRef_v4_0_30319_17929 == null)
                {
                    s_systemRef_v4_0_30319_17929 = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System).GetReference(display: "System.v4_0_30319_17929.dll");
                }

                return s_systemRef_v4_0_30319_17929;
            }
        }

        private static MetadataReference s_systemRef_v20;
        public static MetadataReference SystemRef_v20
        {
            get
            {
                if (s_systemRef_v20 == null)
                {
                    s_systemRef_v20 = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v2_0_50727.System).GetReference(display: "System.v2_0_50727.dll");
                }

                return s_systemRef_v20;
            }
        }

        private static MetadataReference s_systemXmlRef;
        public static MetadataReference SystemXmlRef
        {
            get
            {
                if (s_systemXmlRef == null)
                {
                    s_systemXmlRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Xml).GetReference(display: "System.Xml.v4_0_30319.dll");
                }

                return s_systemXmlRef;
            }
        }

        private static MetadataReference s_systemXmlLinqRef;
        public static MetadataReference SystemXmlLinqRef
        {
            get
            {
                if (s_systemXmlLinqRef == null)
                {
                    s_systemXmlLinqRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Xml_Linq).GetReference(display: "System.Xml.Linq.v4_0_30319.dll");
                }

                return s_systemXmlLinqRef;
            }
        }

        private static MetadataReference s_mscorlibFacadeRef;
        public static MetadataReference MscorlibFacadeRef
        {
            get
            {
                if (s_mscorlibFacadeRef == null)
                {
                    s_mscorlibFacadeRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.ReferenceAssemblies_V45.mscorlib).GetReference(display: "mscorlib.dll");
                }

                return s_mscorlibFacadeRef;
            }
        }

        private static MetadataReference s_systemRuntimeFacadeRef;
        public static MetadataReference SystemRuntimeFacadeRef
        {
            get
            {
                if (s_systemRuntimeFacadeRef == null)
                {
                    s_systemRuntimeFacadeRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.ReferenceAssemblies_V45_Facades.System_Runtime).GetReference(display: "System.Runtime.dll");
                }

                return s_systemRuntimeFacadeRef;
            }
        }

        private static MetadataReference s_systemThreadingFacadeRef;
        public static MetadataReference SystemThreadingFacadeRef
        {
            get
            {
                if (s_systemThreadingFacadeRef == null)
                {
                    s_systemThreadingFacadeRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.ReferenceAssemblies_V45_Facades.System_Threading).GetReference(display: "System.Threading.dll");
                }

                return s_systemThreadingFacadeRef;
            }
        }

        private static MetadataReference s_systemThreadingTasksFacadeRef;
        public static MetadataReference SystemThreadingTaskFacadeRef
        {
            get
            {
                if (s_systemThreadingTasksFacadeRef == null)
                {
                    s_systemThreadingTasksFacadeRef = AssemblyMetadata.CreateFromImage(TestResources.NetFX.ReferenceAssemblies_V45_Facades.System_Threading_Tasks).GetReference(display: "System.Threading.Tasks.dll");
                }

                return s_systemThreadingTasksFacadeRef;
            }
        }

        private static MetadataReference s_mscorlibPP7Ref;
        public static MetadataReference MscorlibPP7Ref
        {
            get
            {
                if (s_mscorlibPP7Ref == null)
                {
                    s_mscorlibPP7Ref = AssemblyMetadata.CreateFromImage(TestResources.NetFX.ReferenceAssemblies_PortableProfile7.mscorlib).GetReference(display: "mscorlib.dll");
                }

                return s_mscorlibPP7Ref;
            }
        }

        private static MetadataReference s_systemRuntimePP7Ref;
        public static MetadataReference SystemRuntimePP7Ref
        {
            get
            {
                if (s_systemRuntimePP7Ref == null)
                {
                    s_systemRuntimePP7Ref = AssemblyMetadata.CreateFromImage(TestResources.NetFX.ReferenceAssemblies_PortableProfile7.System_Runtime).GetReference(display: "System.Runtime.dll");
                }

                return s_systemRuntimePP7Ref;
            }
        }

        private static MetadataReference s_FSharpTestLibraryRef;
        public static MetadataReference FSharpTestLibraryRef
        {
            get
            {
                if (s_FSharpTestLibraryRef == null)
                {
                    s_FSharpTestLibraryRef = AssemblyMetadata.CreateFromImage(TestResources.General.FSharpTestLibrary).GetReference(display: "FSharpTestLibrary.dll");
                }

                return s_FSharpTestLibraryRef;
            }
        }

        public static MetadataReference InvalidRef = new TestMetadataReference(fullPath: @"R:\Invalid.dll");

        #endregion

        #region Diagnostics

        internal static DiagnosticDescription Diagnostic(
            object code,
            string squiggledText = null,
            object[] arguments = null,
            LinePosition? startLocation = null,
            Func<SyntaxNode, bool> syntaxNodePredicate = null,
            bool argumentOrderDoesNotMatter = false)
        {
            Debug.Assert(code is ErrorCode || code is ERRID || code is int || code is string);

            return new DiagnosticDescription(
                code as string ?? (object)(int)code,
                false,
                squiggledText,
                arguments,
                startLocation,
                syntaxNodePredicate,
                argumentOrderDoesNotMatter,
                code.GetType());
        }

        internal static DiagnosticDescription Diagnostic(
           object code,
           XCData squiggledText,
           object[] arguments = null,
           LinePosition? startLocation = null,
           Func<SyntaxNode, bool> syntaxNodePredicate = null,
           bool argumentOrderDoesNotMatter = false)
        {
            return Diagnostic(
                code,
                NormalizeDiagnosticString(squiggledText.Value),
                arguments,
                startLocation,
                syntaxNodePredicate,
                argumentOrderDoesNotMatter);
        }

        protected static string NormalizeDiagnosticString(string inputString)
        {
            if (!inputString.Contains("\r\n") && inputString.Contains("\n"))
            {
                return inputString.Replace("\n", "\r\n");
            }

            return inputString;
        }

        #endregion
    }
}
