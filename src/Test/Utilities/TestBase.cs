// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PDB;


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using PDB::Roslyn.Test.PdbUtilities;
using Roslyn.Utilities;
using Xunit;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;
using System.Reflection.PortableExecutable;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Base class for all unit test classes.
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        private TempRoot _temp;

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
                        AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.System_Runtime_WindowsRuntime).GetReference(display: "System.Runtime.WindowsRuntime.dll");

                    var runtime =
                        AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.System_Runtime).GetReference(display: "System.Runtime.dll");

                    var objectModel =
                        AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.System_ObjectModel).GetReference(display: "System.ObjectModel.dll");


                    var uixaml = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.System_Runtime_WindowsRuntime_UI_Xaml).
                        GetReference(display: "System.Runtime.WindowsRuntime.UI.Xaml.dll");


                    var interop = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.System_Runtime_InteropServices_WindowsRuntime).
                        GetReference(display: "System.Runtime.InteropServices.WindowsRuntime.dll");

                    //Not mentioned in the adapter doc but pointed to from System.Runtime, so we'll put it here.
                    var system = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System).GetReference(display: "System.dll");

                    var mscor = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30316_17626.mscorlib).GetReference(display: "mscorlib");

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

        protected TestBase()
        {
        }

        public static string GetUniqueName()
        {
            return Guid.NewGuid().ToString("D");
        }

        #region Metadata References

        /// <summary>
        /// Reference to an assembly that defines Expression Trees.
        /// </summary>
        public static MetadataReference ExpressionAssemblyRef
        {
            get { return SystemCoreRef; }
        }

        /// <summary>
        /// Reference to an assembly that defines LINQ operators.
        /// </summary>
        public static MetadataReference LinqAssemblyRef
        {
            get { return SystemCoreRef; }
        }

        /// <summary>
        /// Reference to an assembly that defines ExtensionAttribute.
        /// </summary>
        public static MetadataReference ExtensionAssemblyRef
        {
            get { return SystemCoreRef; }
        }

        private static MetadataReference s_systemCoreRef_v4_0_30319_17929;
        public static MetadataReference SystemCoreRef_v4_0_30319_17929
        {
            get
            {
                if (s_systemCoreRef_v4_0_30319_17929 == null)
                {
                    s_systemCoreRef_v4_0_30319_17929 = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.System_Core).GetReference(display: "System.Core.v4_0_30319_17929.dll");
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
                    s_systemCoreRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Core).GetReference(display: "System.Core.v4_0_30319.dll");
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
                    s_systemWindowsFormsRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Windows_Forms).GetReference(display: "System.Windows.Forms.v4_0_30319.dll");
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
                    s_systemDrawingRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Drawing).GetReference(display: "System.Drawing.v4_0_30319.dll");
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
                    s_systemDataRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Data).GetReference(display: "System.Data.v4_0_30319.dll");
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
                    s_mscorlibRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).GetReference(display: "mscorlib.v4_0_30319.dll");
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
                    s_mscorlibRefPortable = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib_portable).GetReference(display: "mscorlib.v4_0_30319.portable.dll");
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
                    s_mscorlibRef_v4_0_30316_17626 = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30316_17626.mscorlib).GetReference(display: "mscorlib.v4_0_30319_17626.dll", filePath: @"Z:\FxReferenceAssembliesUri");
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
                    s_mscorlibRef_v20 = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v2_0_50727.mscorlib).GetReference(display: "mscorlib.v2.0.50727.dll");
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
                    s_mscorlibRef_silverlight = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.silverlight_v5_0_5_0.mscorlib_v5_0_5_0_silverlight).GetReference(display: "mscorlib.v5.0.5.0_silverlight.dll");
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
                    s_msvbRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.Microsoft_VisualBasic).GetReference(display: "Microsoft.VisualBasic.v4_0_30319.dll");
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
                    s_msvbRef_v4_0_30319_17929 = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.Microsoft_VisualBasic).GetReference(display: "Microsoft.VisualBasic.v4_0_30319_17929.dll");
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
                    s_csharpRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.Microsoft_CSharp).GetReference(display: "Microsoft.CSharp.v4.0.30319.dll");
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
                    s_systemRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System).GetReference(display: "System.v4_0_30319.dll");
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
                    s_systemRef_v4_0_30319_17929 = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.System).GetReference(display: "System.v4_0_30319_17929.dll");
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
                    s_systemRef_v20 = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v2_0_50727.System).GetReference(display: "System.v2_0_50727.dll");
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
                    s_systemXmlRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Xml).GetReference(display: "System.Xml.v4_0_30319.dll");
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
                    s_systemXmlLinqRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Xml_Linq).GetReference(display: "System.Xml.Linq.v4_0_30319.dll");
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
                    s_mscorlibFacadeRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.ReferenceAssemblies_V45.mscorlib).GetReference(display: "mscorlib.dll");
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
                    s_systemRuntimeFacadeRef = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.ReferenceAssemblies_V45_Facades.System_Runtime).GetReference(display: "System.Runtime.dll");
                }

                return s_systemRuntimeFacadeRef;
            }
        }

        private static MetadataReference s_mscorlibPP7Ref;
        public static MetadataReference MscorlibPP7Ref
        {
            get
            {
                if (s_mscorlibPP7Ref == null)
                {
                    s_mscorlibPP7Ref = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.ReferenceAssemblies_PortableProfile7.mscorlib).GetReference(display: "mscorlib.dll");
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
                    s_systemRuntimePP7Ref = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.ReferenceAssemblies_PortableProfile7.System_Runtime).GetReference(display: "System.Runtime.dll");
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
                    s_FSharpTestLibraryRef = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.FSharpTestLibrary).GetReference(display: "FSharpTestLibrary.dll");
                }

                return s_FSharpTestLibraryRef;
            }
        }

        public static MetadataReference InvalidRef = new TestMetadataReference(fullPath: @"R:\Invalid.dll");

        #endregion

        #region File System

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

        #endregion

        #region Execution

        public static int Execute(System.Reflection.Assembly assembly, string[] args)
        {
            var parameters = assembly.EntryPoint.GetParameters();
            object[] arguments;

            if (parameters.Length == 0)
            {
                arguments = new object[0];
            }
            else if (parameters.Length == 1)
            {
                arguments = new object[] { args };
            }
            else
            {
                throw new InvalidOperationException("Invalid entry point");
            }

            if (assembly.EntryPoint.ReturnType == typeof(int))
            {
                return (int)assembly.EntryPoint.Invoke(null, arguments);
            }
            else if (assembly.EntryPoint.ReturnType == typeof(void))
            {
                assembly.EntryPoint.Invoke(null, arguments);
                return 0;
            }
            else
            {
                throw new InvalidOperationException("Invalid entry point");
            }
        }

        #endregion

        #region Metadata Validation

        /// <summary>
        /// Returns the name of the attribute class 
        /// </summary>
        internal static string GetAttributeName(MetadataReader metadataReader, CustomAttributeHandle customAttribute)
        {
            var ctorHandle = metadataReader.GetCustomAttribute(customAttribute).Constructor;
            if (ctorHandle.Kind == HandleKind.MemberReference) // MemberRef
            {
                var container = metadataReader.GetMemberReference((MemberReferenceHandle)ctorHandle).Parent;
                var name = metadataReader.GetTypeReference((TypeReferenceHandle)container).Name;
                return metadataReader.GetString(name);
            }
            else
            {
                Assert.True(false, "not impl");
                return null;
            }
        }

        internal static CustomAttributeHandle FindCustomAttribute(MetadataReader metadataReader, string attributeClassName)
        {
            foreach (var caHandle in metadataReader.CustomAttributes)
            {
                if (string.Equals(GetAttributeName(metadataReader, caHandle), attributeClassName, StringComparison.Ordinal))
                {
                    return caHandle;
                }
            }

            return default(CustomAttributeHandle);
        }

        /// <summary>
        /// Used to validate metadata blobs emitted for MarshalAs.
        /// </summary>
        internal static void MarshalAsMetadataValidator(PEAssembly assembly, Func<string, PEAssembly, TestEmitters, byte[]> getExpectedBlob, TestEmitters emitters, bool isField = true)
        {
            var metadataReader = assembly.GetMetadataReader();

            // no custom attributes should be emitted on parameters, fields or methods:
            foreach (var ca in metadataReader.CustomAttributes)
            {
                Assert.NotEqual("MarshalAsAttribute", GetAttributeName(metadataReader, ca));
            }

            int expectedMarshalCount = 0;

            if (isField)
            {
                // fields
                foreach (var fieldDef in metadataReader.FieldDefinitions)
                {
                    var field = metadataReader.GetFieldDefinition(fieldDef);
                    string fieldName = metadataReader.GetString(field.Name);

                    byte[] expectedBlob = getExpectedBlob(fieldName, assembly, emitters);
                    if (expectedBlob != null)
                    {
                        BlobHandle descriptor = metadataReader.GetFieldDefinition(fieldDef).GetMarshallingDescriptor();
                        Assert.False(descriptor.IsNil, "Expecting record in FieldMarshal table");

                        Assert.NotEqual(0, (int)(field.Attributes & FieldAttributes.HasFieldMarshal));
                        expectedMarshalCount++;

                        byte[] actualBlob = metadataReader.GetBlobBytes(descriptor);
                        AssertEx.Equal(expectedBlob, actualBlob);
                    }
                    else
                    {
                        Assert.Equal(0, (int)(field.Attributes & FieldAttributes.HasFieldMarshal));
                    }
                }
            }
            else
            {
                // parameters
                foreach (var methodHandle in metadataReader.MethodDefinitions)
                {
                    var methodDef = metadataReader.GetMethodDefinition(methodHandle);
                    string memberName = metadataReader.GetString(methodDef.Name);
                    foreach (var paramHandle in methodDef.GetParameters())
                    {
                        var paramRow = metadataReader.GetParameter(paramHandle);
                        string paramName = metadataReader.GetString(paramRow.Name);

                        byte[] expectedBlob = getExpectedBlob(memberName + ":" + paramName, assembly, emitters);
                        if (expectedBlob != null)
                        {
                            Assert.NotEqual(0, (int)(paramRow.Attributes & ParameterAttributes.HasFieldMarshal));
                            expectedMarshalCount++;

                            BlobHandle descriptor = metadataReader.GetParameter(paramHandle).GetMarshallingDescriptor();
                            Assert.False(descriptor.IsNil, "Expecting record in FieldMarshal table");

                            byte[] actualBlob = metadataReader.GetBlobBytes(descriptor);

                            AssertEx.Equal(expectedBlob, actualBlob);
                        }
                        else
                        {
                            Assert.Equal(0, (int)(paramRow.Attributes & ParameterAttributes.HasFieldMarshal));
                        }
                    }
                }
            }

            Assert.Equal(expectedMarshalCount, metadataReader.GetTableRowCount(TableIndex.FieldMarshal));
        }

        /// <summary>
        /// Creates instance of SignatureDescription for a specified member
        /// </summary>
        /// <param name="fullyQualifiedTypeName">
        /// Fully qualified type name for member
        /// Names must be in format recognized by reflection
        /// e.g. MyType{T}.MyNestedType{T, U} => MyType`1+MyNestedType`2
        /// </param>
        /// <param name="memberName">
        /// Name of member on specified type whose signature needs to be verified
        /// Names must be in format recognized by reflection
        /// e.g. For explicitly implemented member - I1{string}.Method => I1{System.String}.Method
        /// </param>
        /// <param name="expectedSignature">
        /// Baseline string for signature of specified member
        /// Skip this argument to get an error message that shows all available signatures for specified member
        /// </param>
        /// <returns>Instance of SignatureDescription for specified member</returns>
        internal static SignatureDescription Signature(string fullyQualifiedTypeName, string memberName, string expectedSignature = "")
        {
            return new SignatureDescription()
            {
                FullyQualifiedTypeName = fullyQualifiedTypeName,
                MemberName = memberName,
                ExpectedSignature = expectedSignature
            };
        }

        internal static IEnumerable<string> GetFullTypeNames(MetadataReader metadataReader)
        {
            foreach (var typeDefHandle in metadataReader.TypeDefinitions)
            {
                var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                var ns = metadataReader.GetString(typeDef.Namespace);
                var name = metadataReader.GetString(typeDef.Name);

                yield return (ns.Length == 0) ? name : (ns + "." + name);
            }
        }

        #endregion

        #region PDB Validation

        public static string GetPdbXml(Compilation compilation, string qualifiedMethodName = "")
        {
            return SharedCompilationUtils.GetPdbXml(compilation, qualifiedMethodName);
        }

        public static Dictionary<int, string> GetMarkers(string pdbXml)
        {
            return ToDictionary<int, string, string>(EnumerateMarkers(pdbXml), (markers, marker) => markers + marker);
        }

        private static Dictionary<K, V> ToDictionary<K, V, I>(IEnumerable<KeyValuePair<K, I>> pairs, Func<V, I, V> aggregator)
        {
            var result = new Dictionary<K, V>();
            foreach (var pair in pairs)
            {
                V existing;
                if (result.TryGetValue(pair.Key, out existing))
                {
                    result[pair.Key] = aggregator(existing, pair.Value);
                }
                else
                {
                    result.Add(pair.Key, aggregator(default(V), pair.Value));
                }
            }

            return result;
        }

        public static IEnumerable<KeyValuePair<int, string>> EnumerateMarkers(string pdbXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(pdbXml);

            foreach (XmlNode entry in doc.GetElementsByTagName("sequencePoints"))
            {
                foreach (XmlElement item in entry.ChildNodes)
                {
                    yield return KeyValuePair.Create(
                        Convert.ToInt32(item.GetAttribute("offset"), 16),
                        (item.GetAttribute("hidden") == "true") ? "~" : "-");
                }
            }

            foreach (XmlNode entry in doc.GetElementsByTagName("asyncInfo"))
            {
                foreach (XmlElement item in entry.ChildNodes)
                {
                    if (item.Name == "await")
                    {
                        yield return KeyValuePair.Create(Convert.ToInt32(item.GetAttribute("yield"), 16), "<");
                        yield return KeyValuePair.Create(Convert.ToInt32(item.GetAttribute("resume"), 16), ">");
                    }
                    else if (item.Name == "catchHandler")
                    {
                        yield return KeyValuePair.Create(Convert.ToInt32(item.GetAttribute("offset"), 16), "$");
                    }
                }
            }
        }

        public static string GetTokenToLocationMap(Compilation compilation, bool maskToken = false)
        {
            using (var exebits = new MemoryStream())
            {
                using (var pdbbits = new MemoryStream())
                {
                    compilation.Emit(exebits, pdbbits);
                    return Token2SourceLineExporter.TokenToSourceMap2Xml(pdbbits, maskToken);
                }
            }
        }

        protected static string ConsolidateArguments(string[] args)
        {
            var consolidated = new StringBuilder();
            foreach (string argument in args)
            {
                bool surround = Regex.Match(argument, @"[\s+]").Success;
                if (surround)
                {
                    consolidated.AppendFormat("\"{0}\" ", argument);
                }
                else
                {
                    consolidated.AppendFormat("{0} ", argument);
                }
            }
            return consolidated.ToString();
        }

        protected static string RunAndGetOutput(string exeFileName, string arguments = null, int expectedRetCode = 0, string startFolder = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(exeFileName);
            if (arguments != null)
            {
                startInfo.Arguments = arguments;
            }
            string result = null;

            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            if (startFolder != null)
            {
                startInfo.WorkingDirectory = startFolder;
            }

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                // Do not wait for the child process to exit before reading to the end of its
                // redirected stream. Read the output stream first and then wait. Doing otherwise
                // might cause a deadlock.
                result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                Assert.Equal(expectedRetCode, process.ExitCode);
            }

            return result;
        }

        #endregion

        #region Serialization

        public static T VerifySerializability<T>(T obj)
        {
            Assert.True(obj is ISerializable);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);

                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        #endregion
    }
}
