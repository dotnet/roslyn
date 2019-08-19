// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class WellKnownTypes
    {
        public static INamedTypeSymbol ICollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsICollection);
        }

        public static INamedTypeSymbol GenericICollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericICollection1);
        }

        public static INamedTypeSymbol GenericIReadOnlyCollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIReadOnlyCollection1);
        }

        public static INamedTypeSymbol IEnumerable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsIEnumerable);
        }

        public static INamedTypeSymbol IEnumerator(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsIEnumerator);
        }

        public static INamedTypeSymbol Enumerable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemLinqEnumerable);
        }

        public static INamedTypeSymbol GenericIEnumerable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIEnumerable1);
        }

        public static INamedTypeSymbol GenericIEnumerator(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIEnumerator1);
        }

        public static INamedTypeSymbol Queryable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemLinqQueryable);
        }

        public static INamedTypeSymbol IList(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsIList);
        }

        internal static INamedTypeSymbol HttpRequest(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebHttpRequest);
        }

        internal static INamedTypeSymbol NameValueCollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsSpecializedNameValueCollection);
        }

        public static INamedTypeSymbol GenericIList(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIList1);
        }

        public static INamedTypeSymbol Array(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_Array);
        }

        public static INamedTypeSymbol FlagsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.FlagsAttribute).FullName);
        }

        public static INamedTypeSymbol StringComparison(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.StringComparison).FullName);
        }

        public static INamedTypeSymbol CharSet(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.CharSet).FullName);
        }

        public static INamedTypeSymbol DllImportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.DllImportAttribute).FullName);
        }

        public static INamedTypeSymbol MarshalAsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.MarshalAsAttribute).FullName);
        }

        public static INamedTypeSymbol StringBuilder(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Text.StringBuilder).FullName);
        }

        public static INamedTypeSymbol UnmanagedType(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.UnmanagedType).FullName);
        }

        public static INamedTypeSymbol MarshalByRefObject(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemMarshalByRefObject);
        }

        public static INamedTypeSymbol ExecutionEngineException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemExecutionEngineException);
        }

        public static INamedTypeSymbol OutOfMemoryException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.OutOfMemoryException).FullName);
        }

        public static INamedTypeSymbol StackOverflowException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemStackOverflowException);
        }

        public static INamedTypeSymbol MemberInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Reflection.MemberInfo).FullName);
        }

        public static INamedTypeSymbol ParameterInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Reflection.ParameterInfo).FullName);
        }

        public static INamedTypeSymbol Monitor(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingMonitor);
        }

        public static INamedTypeSymbol Thread(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingThread);
        }

        public static INamedTypeSymbol Task(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask);
        }

        public static INamedTypeSymbol WebMethodAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebServicesWebMethodAttribute);
        }

        public static INamedTypeSymbol WebUIControl(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebUIControl);
        }

        public static INamedTypeSymbol WebUILiteralControl(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebUILiteralControl);
        }

        public static INamedTypeSymbol WinFormsUIControl(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWindowsFormsControl);
        }

        public static INamedTypeSymbol NotImplementedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.NotImplementedException).FullName);
        }

        public static INamedTypeSymbol IDisposable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
        }

        public static INamedTypeSymbol IDeserializationCallback(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationIDeserializationCallback);
        }

        public static INamedTypeSymbol ISerializable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationISerializable);
        }

        public static INamedTypeSymbol SerializationInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationSerializationInfo);
        }

        public static INamedTypeSymbol StreamingContext(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationStreamingContext);
        }

        public static INamedTypeSymbol OnDeserializingAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationOnDeserializingAttribute);
        }

        public static INamedTypeSymbol OnDeserializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationOnDeserializedAttribute);
        }

        public static INamedTypeSymbol OnSerializingAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationOnSerializingAttribute);
        }

        public static INamedTypeSymbol OnSerializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationOnSerializedAttribute);
        }

        public static INamedTypeSymbol SerializableAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemSerializableAttribute);
        }

        public static INamedTypeSymbol NonSerializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemNonSerializedAttribute);
        }

        public static INamedTypeSymbol Attribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Attribute).FullName);
        }

        public static INamedTypeSymbol AttributeUsageAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.AttributeUsageAttribute).FullName);
        }

        public static INamedTypeSymbol AssemblyVersionAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Reflection.AssemblyVersionAttribute).FullName);
        }

        public static INamedTypeSymbol CLSCompliantAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.CLSCompliantAttribute).FullName);
        }

        public static INamedTypeSymbol ConditionalAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Diagnostics.ConditionalAttribute).FullName);
        }

        public static INamedTypeSymbol IComparable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.IComparable).FullName);
        }

        public static INamedTypeSymbol GenericIComparable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.IComparable<>).FullName);
        }

        public static INamedTypeSymbol ComSourceInterfaceAttribute(Compilation compilation)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.ComSourceInterfacesAttribute).FullName);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static INamedTypeSymbol GenericEventHandler(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.EventHandler<>).FullName);
        }

        public static INamedTypeSymbol EventArgs(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.EventArgs).FullName);
        }

        public static INamedTypeSymbol Uri(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Uri).FullName);
        }

        public static INamedTypeSymbol ComVisibleAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.ComVisibleAttribute).FullName);
        }

        public static INamedTypeSymbol NeutralResourcesLanguageAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Resources.NeutralResourcesLanguageAttribute).FullName);
        }

        public static INamedTypeSymbol GeneratedCodeAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName);
        }

        public static INamedTypeSymbol Console(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Console).FullName);
        }

        public static INamedTypeSymbol String(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_String);
        }

        public static INamedTypeSymbol Object(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_Object);
        }

        public static INamedTypeSymbol X509Certificate(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Security.Cryptography.X509Certificates.X509Certificate).FullName);
        }

        public static INamedTypeSymbol X509Chain(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Security.Cryptography.X509Certificates.X509Chain).FullName);
        }

        public static INamedTypeSymbol SslPolicyErrors(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Net.Security.SslPolicyErrors).FullName);
        }

        public static INamedTypeSymbol Exception(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemExceptionFullName);
        }

        public static INamedTypeSymbol SystemException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemSystemException);
        }

        public static INamedTypeSymbol InvalidOperationException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.InvalidOperationException).FullName);
        }

        public static INamedTypeSymbol ArgumentException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.ArgumentException).FullName);
        }

        public static INamedTypeSymbol NotSupportedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.NotSupportedException).FullName);
        }

        public static INamedTypeSymbol KeyNotFoundException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Collections.Generic.KeyNotFoundException).FullName);
        }

        public static INamedTypeSymbol GenericIEqualityComparer(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Collections.Generic.IEqualityComparer<>).FullName);
        }

        public static INamedTypeSymbol GenericIEquatable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemIEquatable1);
        }

        public static INamedTypeSymbol IHashCodeProvider(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsIHashCodeProvider);
        }

        public static INamedTypeSymbol IntPtr(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_IntPtr);
        }

        public static INamedTypeSymbol UIntPtr(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_UIntPtr);
        }

        public static INamedTypeSymbol HandleRef(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeInteropServicesHandleRef);
        }

        public static INamedTypeSymbol DataMemberAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationDataMemberAttribute);
        }

        public static INamedTypeSymbol ObsoleteAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.ObsoleteAttribute).FullName);
        }

        public static INamedTypeSymbol PureAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemDiagnosticsContractsPureAttribute);
        }

        public static INamedTypeSymbol MEFV1ExportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionExportAttribute);
        }

        public static INamedTypeSymbol MEFV2ExportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCompositionExportAttribute);
        }

        public static INamedTypeSymbol InheritedExportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionInheritedExportAttribute);
        }

        public static INamedTypeSymbol MEFV1ImportingConstructorAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionImportingConstructorAttribute);
        }

        public static INamedTypeSymbol MEFV2ImportingConstructorAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCompositionImportingConstructorAttribute);
        }

        public static INamedTypeSymbol LocalizableAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemComponentModelLocalizableAttribute);
        }

        public static INamedTypeSymbol FieldOffsetAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.FieldOffsetAttribute).FullName);
        }

        public static INamedTypeSymbol StructLayoutAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.StructLayoutAttribute).FullName);
        }

        public static INamedTypeSymbol IDbCommand(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemDataIDbCommand);
        }

        public static INamedTypeSymbol IDataAdapter(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemDataIDataAdapter);
        }

        public static INamedTypeSymbol MvcController(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcController);
        }

        public static INamedTypeSymbol MvcControllerBase(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcControllerBase);
        }

        public static INamedTypeSymbol ActionResult(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcActionResult);
        }

        public static INamedTypeSymbol ValidateAntiforgeryTokenAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcValidateAntiForgeryTokenAttribute);
        }

        public static INamedTypeSymbol HttpGetAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcHttpGetAttribute);
        }

        public static INamedTypeSymbol HttpPostAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcHttpPostAttribute);
        }

        public static INamedTypeSymbol HttpPutAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcHttpPutAttribute);
        }

        public static INamedTypeSymbol HttpDeleteAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcHttpDeleteAttribute);
        }

        public static INamedTypeSymbol HttpPatchAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcHttpPatchAttribute);
        }

        public static INamedTypeSymbol AcceptVerbsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcAcceptVerbsAttribute);
        }

        public static INamedTypeSymbol NonActionAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcNonActionAttribute);
        }

        public static INamedTypeSymbol ChildActionOnlyAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcChildActionOnlyAttribute);
        }

        public static INamedTypeSymbol HttpVerbs(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemWebMvcHttpVerbs);
        }

        public static INamedTypeSymbol ImmutableArray(Compilation compilation)
        {
            return compilation.GetVisibleTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableImmutableArray);
        }

        public static INamedTypeSymbol IImmutableDictionary(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableIImmutableDictionary);
        }

        public static INamedTypeSymbol IImmutableList(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableIImmutableList);
        }

        public static INamedTypeSymbol IImmutableQueue(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableIImmutableQueue);
        }

        public static INamedTypeSymbol IImmutableSet(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableIImmutableSet);
        }

        public static INamedTypeSymbol IImmutableStack(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableIImmutableStack);
        }

        public static INamedTypeSymbol SystemIOFile(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.IO.File).FullName);
        }

        public static INamedTypeSymbol SystemReflectionAssembly(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(System.Reflection.Assembly).FullName);
        }

        public static System.Collections.Immutable.ImmutableHashSet<INamedTypeSymbol> IImmutableInterfaces(Compilation compilation)
        {
            var builder = System.Collections.Immutable.ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
            AddIfNotNull(IImmutableDictionary(compilation));
            AddIfNotNull(IImmutableList(compilation));
            AddIfNotNull(IImmutableQueue(compilation));
            AddIfNotNull(IImmutableSet(compilation));
            AddIfNotNull(IImmutableStack(compilation));
            return builder.ToImmutable();

            // Local functions.
            void AddIfNotNull(INamedTypeSymbol type)
            {
                if (type != null)
                {
                    builder.Add(type);
                }
            }
        }

        public static INamedTypeSymbol SystemSecurityCryptographyCipherMode(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemSecurityCryptographyCipherMode);
        }

        public static INamedTypeSymbol SystemNetSecurityRemoteCertificateValidationCallback(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemNetSecurityRemoteCertificateValidationCallback);
        }

        public static INamedTypeSymbol XmlWriter(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemXmlXmlWriter);
        }

        #region Test Framework Types
        public static INamedTypeSymbol TestCleanupAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingTestCleanupAttribute);
        }

        public static INamedTypeSymbol TestInitializeAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingTestInitializeAttribute);
        }

        public static INamedTypeSymbol TestMethodAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingTestMethodAttribute);
        }

        public static INamedTypeSymbol DataTestMethodAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingDataTestMethodAttribute);
        }

        public static INamedTypeSymbol ExpectedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingExpectedExceptionAttribute);
        }

        public static INamedTypeSymbol UnitTestingAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingAssert);
        }

        public static INamedTypeSymbol UnitTestingCollectionAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingCollectionAssert);
        }

        public static INamedTypeSymbol UnitTestingCollectionStringAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingStringAssert);
        }

        public static INamedTypeSymbol XunitAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.XunitAssert);
        }

        public static INamedTypeSymbol XunitFact(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.XunitFactAttribute);
        }

        public static INamedTypeSymbol XunitTheory(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.XunitTheoryAttribute);
        }

        public static INamedTypeSymbol NunitAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkAssert);
        }

        public static INamedTypeSymbol NunitOneTimeSetUp(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkOneTimeSetUpAttribute);
        }

        public static INamedTypeSymbol NunitOneTimeTearDown(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkOneTimeTearDownAttribute);
        }

        public static INamedTypeSymbol NunitSetUp(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkSetUpAttribute);
        }

        public static INamedTypeSymbol NunitSetUpFixture(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkSetUpFixtureAttribute);
        }

        public static INamedTypeSymbol NunitTearDown(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkTearDownAttribute);
        }

        public static INamedTypeSymbol NunitTest(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkTestAttribute);
        }

        public static INamedTypeSymbol NunitTestCase(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkTestCaseAttribute);
        }

        public static INamedTypeSymbol NunitTestCaseSource(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkTestCaseSourceAttribute);
        }

        public static INamedTypeSymbol NunitTheory(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(WellKnownTypeNames.NUnitFrameworkTheoryAttribute);
        }

        #endregion
    }
}
