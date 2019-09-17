// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

#if NETSTANDARD1_5
using System.IO;
#endif

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Metadata references used to create test projects.
    /// </summary>
    public static class MetadataReferences
    {
        public static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location).WithAliases(ImmutableArray.Create("global", "corlib"));
        public static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly.Location).WithAliases(ImmutableArray.Create("global", "system"));
        public static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location);
        public static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).GetTypeInfo().Assembly.Location);
        public static readonly MetadataReference SystemCollectionsImmutableReference = MetadataReference.CreateFromFile(typeof(ImmutableArray).GetTypeInfo().Assembly.Location);

        internal static readonly MetadataReference? MscorlibFacadeReference;
        internal static readonly MetadataReference? SystemRuntimeReference;
        internal static readonly MetadataReference? SystemValueTupleReference;

        private static MetadataReference? _microsoftVisualBasicReference;

        static MetadataReferences()
        {
#if NETSTANDARD1_5
            if (typeof(string).GetTypeInfo().Assembly.ExportedTypes.Any(x => x.Name == "System.ValueTuple"))
            {
                // mscorlib contains ValueTuple, so no need to add a separate reference
                MscorlibFacadeReference = null;
                SystemRuntimeReference = null;
                SystemValueTupleReference = null;
            }
            else
            {
                MscorlibFacadeReference = MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(Lazy<,>).GetTypeInfo().Assembly.Location), "mscorlib.dll"));
                SystemRuntimeReference = MetadataReference.CreateFromFile(typeof(Lazy<,>).GetTypeInfo().Assembly.Location);
                SystemValueTupleReference = MetadataReference.CreateFromFile(typeof(ValueTuple<,>).GetTypeInfo().Assembly.Location);
            }
#elif NETSTANDARD2_0
            // mscorlib contains ValueTuple, so no need to add a separate reference
            MscorlibFacadeReference = null;
            SystemRuntimeReference = null;
            SystemValueTupleReference = null;
#elif NET452
            // System.Object is already in mscorlib
            MscorlibFacadeReference = null;

            var systemRuntime = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "System.Runtime");
            if (systemRuntime != null)
            {
                SystemRuntimeReference = MetadataReference.CreateFromFile(systemRuntime.Location);
            }

            var systemValueTuple = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "System.ValueTuple");
            if (systemValueTuple != null)
            {
                SystemValueTupleReference = MetadataReference.CreateFromFile(systemValueTuple.Location);
            }
#else
#error Unsupported target framework.
#endif
        }

        public static MetadataReference MicrosoftVisualBasicReference
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (_microsoftVisualBasicReference is null)
                {
                    try
                    {
                        _microsoftVisualBasicReference = MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Strings).GetTypeInfo().Assembly.Location);
                    }
                    catch (Exception e)
                    {
                        throw new PlatformNotSupportedException("Microsoft.VisualBasic is not supported on this platform", e);
                    }
                }

                return _microsoftVisualBasicReference;
            }
        }
    }
}
