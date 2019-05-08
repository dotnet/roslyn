// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Metadata references used to create test projects.
    /// </summary>
    public static class MetadataReferences
    {
        private static readonly Func<string, DocumentationProvider?> s_createDocumentationProvider;

        static MetadataReferences()
        {
            Func<string, DocumentationProvider?> createDocumentationProvider = _ => null;

            var xmlDocumentationProvider = typeof(Workspace).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.XmlDocumentationProvider");
            if (xmlDocumentationProvider is object)
            {
                var createFromFile = xmlDocumentationProvider.GetTypeInfo().GetMethod("CreateFromFile", new[] { typeof(string) });
                if (createFromFile is object)
                {
                    var xmlDocCommentFilePath = Expression.Parameter(typeof(string), "xmlDocCommentFilePath");
                    var body = Expression.Convert(
                        Expression.Call(createFromFile, xmlDocCommentFilePath),
                        typeof(DocumentationProvider));
                    var expression = Expression.Lambda<Func<string, DocumentationProvider>>(body, xmlDocCommentFilePath);
                    createDocumentationProvider = expression.Compile();
                }
            }

            s_createDocumentationProvider = createDocumentationProvider;
        }

#if NETSTANDARD1_5
        internal static string PackagesPath
        {
            get
            {
                var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
                if (!string.IsNullOrEmpty(nugetPackages))
                {
                    return nugetPackages;
                }

                var profile = Environment.GetEnvironmentVariable("USERPROFILE");
                if (!Directory.Exists(profile))
                {
                    throw new PlatformNotSupportedException("Reference assemblies are not supported for .NET Standard 1.x");
                }

                return Path.Combine(profile, ".nuget", "packages");
            }
        }
#else
        internal static string PackagesPath
        {
            get
            {
                var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
                if (!string.IsNullOrEmpty(nugetPackages))
                {
                    return nugetPackages;
                }

                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            }
        }
#endif

        internal static MetadataReference CreateReferenceFromFile(string path)
        {
            var documentationFile = Path.ChangeExtension(path, ".xml");
            return MetadataReference.CreateFromFile(path, documentation: s_createDocumentationProvider(documentationFile));
        }

        public static class NetFramework
        {
            public static class Net20
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "microsoft.netframework.referenceassemblies.net20", "1.0.0-preview.1", "build", ".NETFramework", "v2.0");

                public static MetadataReference Mscorlib => CreateReferenceFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));

                public static MetadataReference MicrosoftVisualBasic => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.VisualBasic.dll"));

                public static MetadataReference System => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.dll"));

                public static MetadataReference SystemData => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.dll"));

                public static MetadataReference SystemDrawing => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Drawing.dll"));

                public static MetadataReference SystemWindowsForms => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Windows.Forms.dll"));

                public static MetadataReference SystemXml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.dll"));
            }

            public static class Net40
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "microsoft.netframework.referenceassemblies.net40", "1.0.0-preview.1", "build", ".NETFramework", "v4.0");

                public static MetadataReference Mscorlib => CreateReferenceFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));

                public static MetadataReference MicrosoftCSharp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.CSharp.dll"));

                public static MetadataReference MicrosoftVisualBasic => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.VisualBasic.dll"));

                public static MetadataReference PresentationCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationCore.dll"));

                public static MetadataReference PresentationFramework => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationFramework.dll"));

                public static MetadataReference System => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.dll"));

                public static MetadataReference SystemCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

                public static MetadataReference SystemData => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.dll"));

                public static MetadataReference SystemDataDataSetExtensions => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.DataSetExtensions.dll"));

                public static MetadataReference SystemDeployment => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Deployment.dll"));

                public static MetadataReference SystemDrawing => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Drawing.dll"));

                public static MetadataReference SystemWindowsForms => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Windows.Forms.dll"));

                public static MetadataReference SystemXaml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xaml.dll"));

                public static MetadataReference SystemXml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.dll"));

                public static MetadataReference SystemXmlLinq => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.Linq.dll"));

                public static MetadataReference WindowsBase => CreateReferenceFromFile(Path.Combine(AssemblyPath, "WindowsBase.dll"));
            }

            public static class Net45
            {
            }

            public static class Net451
            {
            }

            public static class Net452
            {
            }

            public static class Net46
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "microsoft.netframework.referenceassemblies.net46", "1.0.0-preview.1", "build", ".NETFramework", "v4.6");

                public static MetadataReference Mscorlib => CreateReferenceFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));

                public static MetadataReference MicrosoftCSharp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.CSharp.dll"));

                public static MetadataReference MicrosoftVisualBasic => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.VisualBasic.dll"));

                public static MetadataReference PresentationCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationCore.dll"));

                public static MetadataReference PresentationFramework => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationFramework.dll"));

                public static MetadataReference System => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.dll"));

                public static MetadataReference SystemCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

                public static MetadataReference SystemData => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.dll"));

                public static MetadataReference SystemDataDataSetExtensions => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.DataSetExtensions.dll"));

                public static MetadataReference SystemDeployment => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Deployment.dll"));

                public static MetadataReference SystemDrawing => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Drawing.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Net.Http.dll"));

                public static MetadataReference SystemWindowsForms => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Windows.Forms.dll"));

                public static MetadataReference SystemXaml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xaml.dll"));

                public static MetadataReference SystemXml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.dll"));

                public static MetadataReference SystemXmlLinq => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.Linq.dll"));

                public static MetadataReference WindowsBase => CreateReferenceFromFile(Path.Combine(AssemblyPath, "WindowsBase.dll"));
            }

            public static class Net461
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "microsoft.netframework.referenceassemblies.net461", "1.0.0-preview.1", "build", ".NETFramework", "v4.6.1");

                public static MetadataReference Mscorlib => CreateReferenceFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));

                public static MetadataReference MicrosoftCSharp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.CSharp.dll"));

                public static MetadataReference MicrosoftVisualBasic => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.VisualBasic.dll"));

                public static MetadataReference PresentationCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationCore.dll"));

                public static MetadataReference PresentationFramework => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationFramework.dll"));

                public static MetadataReference System => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.dll"));

                public static MetadataReference SystemCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

                public static MetadataReference SystemData => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.dll"));

                public static MetadataReference SystemDataDataSetExtensions => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.DataSetExtensions.dll"));

                public static MetadataReference SystemDeployment => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Deployment.dll"));

                public static MetadataReference SystemDrawing => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Drawing.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Net.Http.dll"));

                public static MetadataReference SystemWindowsForms => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Windows.Forms.dll"));

                public static MetadataReference SystemXaml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xaml.dll"));

                public static MetadataReference SystemXml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.dll"));

                public static MetadataReference SystemXmlLinq => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.Linq.dll"));

                public static MetadataReference WindowsBase => CreateReferenceFromFile(Path.Combine(AssemblyPath, "WindowsBase.dll"));
            }

            public static class Net462
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "microsoft.netframework.referenceassemblies.net462", "1.0.0-preview.1", "build", ".NETFramework", "v4.6.2");

                public static MetadataReference Mscorlib => CreateReferenceFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));

                public static MetadataReference MicrosoftCSharp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.CSharp.dll"));

                public static MetadataReference MicrosoftVisualBasic => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.VisualBasic.dll"));

                public static MetadataReference PresentationCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationCore.dll"));

                public static MetadataReference PresentationFramework => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationFramework.dll"));

                public static MetadataReference System => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.dll"));

                public static MetadataReference SystemCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

                public static MetadataReference SystemData => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.dll"));

                public static MetadataReference SystemDataDataSetExtensions => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.DataSetExtensions.dll"));

                public static MetadataReference SystemDeployment => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Deployment.dll"));

                public static MetadataReference SystemDrawing => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Drawing.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Net.Http.dll"));

                public static MetadataReference SystemWindowsForms => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Windows.Forms.dll"));

                public static MetadataReference SystemXaml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xaml.dll"));

                public static MetadataReference SystemXml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.dll"));

                public static MetadataReference SystemXmlLinq => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.Linq.dll"));

                public static MetadataReference WindowsBase => CreateReferenceFromFile(Path.Combine(AssemblyPath, "WindowsBase.dll"));
            }

            public static class Net47
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "microsoft.netframework.referenceassemblies.net47", "1.0.0-preview.1", "build", ".NETFramework", "v4.7");

                public static MetadataReference Mscorlib => CreateReferenceFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));

                public static MetadataReference MicrosoftCSharp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.CSharp.dll"));

                public static MetadataReference MicrosoftVisualBasic => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.VisualBasic.dll"));

                public static MetadataReference PresentationCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationCore.dll"));

                public static MetadataReference PresentationFramework => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationFramework.dll"));

                public static MetadataReference System => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.dll"));

                public static MetadataReference SystemCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

                public static MetadataReference SystemData => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.dll"));

                public static MetadataReference SystemDataDataSetExtensions => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.DataSetExtensions.dll"));

                public static MetadataReference SystemDeployment => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Deployment.dll"));

                public static MetadataReference SystemDrawing => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Drawing.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Net.Http.dll"));

                public static MetadataReference SystemWindowsForms => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Windows.Forms.dll"));

                public static MetadataReference SystemXaml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xaml.dll"));

                public static MetadataReference SystemXml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.dll"));

                public static MetadataReference SystemXmlLinq => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.Linq.dll"));

                public static MetadataReference WindowsBase => CreateReferenceFromFile(Path.Combine(AssemblyPath, "WindowsBase.dll"));
            }

            public static class Net471
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "microsoft.netframework.referenceassemblies.net471", "1.0.0-preview.1", "build", ".NETFramework", "v4.7.1");

                public static MetadataReference Mscorlib => CreateReferenceFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));

                public static MetadataReference MicrosoftCSharp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.CSharp.dll"));

                public static MetadataReference MicrosoftVisualBasic => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.VisualBasic.dll"));

                public static MetadataReference PresentationCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationCore.dll"));

                public static MetadataReference PresentationFramework => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationFramework.dll"));

                public static MetadataReference System => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.dll"));

                public static MetadataReference SystemCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

                public static MetadataReference SystemData => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.dll"));

                public static MetadataReference SystemDataDataSetExtensions => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.DataSetExtensions.dll"));

                public static MetadataReference SystemDeployment => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Deployment.dll"));

                public static MetadataReference SystemDrawing => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Drawing.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Net.Http.dll"));

                public static MetadataReference SystemWindowsForms => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Windows.Forms.dll"));

                public static MetadataReference SystemXaml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xaml.dll"));

                public static MetadataReference SystemXml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.dll"));

                public static MetadataReference SystemXmlLinq => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.Linq.dll"));

                public static MetadataReference WindowsBase => CreateReferenceFromFile(Path.Combine(AssemblyPath, "WindowsBase.dll"));
            }

            public static class Net472
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "microsoft.netframework.referenceassemblies.net472", "1.0.0-preview.1", "build", ".NETFramework", "v4.7.2");

                public static MetadataReference Mscorlib => CreateReferenceFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));

                public static MetadataReference MicrosoftCSharp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.CSharp.dll"));

                public static MetadataReference MicrosoftVisualBasic => CreateReferenceFromFile(Path.Combine(AssemblyPath, "Microsoft.VisualBasic.dll"));

                public static MetadataReference PresentationCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationCore.dll"));

                public static MetadataReference PresentationFramework => CreateReferenceFromFile(Path.Combine(AssemblyPath, "PresentationFramework.dll"));

                public static MetadataReference System => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.dll"));

                public static MetadataReference SystemCore => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

                public static MetadataReference SystemData => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.dll"));

                public static MetadataReference SystemDataDataSetExtensions => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Data.DataSetExtensions.dll"));

                public static MetadataReference SystemDeployment => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Deployment.dll"));

                public static MetadataReference SystemDrawing => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Drawing.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Net.Http.dll"));

                public static MetadataReference SystemWindowsForms => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Windows.Forms.dll"));

                public static MetadataReference SystemXaml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xaml.dll"));

                public static MetadataReference SystemXml => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.dll"));

                public static MetadataReference SystemXmlLinq => CreateReferenceFromFile(Path.Combine(AssemblyPath, "System.Xml.Linq.dll"));

                public static MetadataReference WindowsBase => CreateReferenceFromFile(Path.Combine(AssemblyPath, "WindowsBase.dll"));
            }

            public static class Net48
            {
            }
        }

        public static class NetStandard
        {
            public static class NetStandard10
            {
                public static MetadataReference SystemCollections => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections", "4.3.0", "ref", "netstandard1.0", "System.Collections.dll"));

                public static MetadataReference SystemDiagnosticsDebug => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.debug", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Debug.dll"));

                public static MetadataReference SystemDiagnosticsTools => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tools", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Tools.dll"));

                public static MetadataReference SystemGlobalization => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization", "4.3.0", "ref", "netstandard1.0", "System.Globalization.dll"));

                public static MetadataReference SystemIO => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io", "4.3.0", "ref", "netstandard1.0", "System.IO.dll"));

                public static MetadataReference SystemLinq => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq", "4.3.0", "ref", "netstandard1.0", "System.Linq.dll"));

                public static MetadataReference SystemLinqExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq.expressions", "4.3.0", "ref", "netstandard1.0", "System.Linq.Expressions.dll"));

                public static MetadataReference SystemNetPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.primitives", "4.3.0", "ref", "netstandard1.0", "System.Net.Primitives.dll"));

                public static MetadataReference SystemObjectModel => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.objectmodel", "4.3.0", "ref", "netstandard1.0", "System.ObjectModel.dll"));

                public static MetadataReference SystemReflection => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection", "4.3.0", "ref", "netstandard1.0", "System.Reflection.dll"));

                public static MetadataReference SystemReflectionExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.extensions", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Extensions.dll"));

                public static MetadataReference SystemReflectionPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.primitives", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Primitives.dll"));

                public static MetadataReference SystemResourcesResourceManager => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.resources.resourcemanager", "4.3.0", "ref", "netstandard1.0", "System.Resources.ResourceManager.dll"));

                public static MetadataReference SystemRuntime => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime", "4.3.0", "ref", "netstandard1.0", "System.Runtime.dll"));

                public static MetadataReference SystemRuntimeExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.extensions", "4.3.0", "ref", "netstandard1.0", "System.Runtime.Extensions.dll"));

                public static MetadataReference SystemTextEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding", "4.3.0", "ref", "netstandard1.0", "System.Text.Encoding.dll"));

                public static MetadataReference SystemTextEncodingExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding.extensions", "4.3.0", "ref", "netstandard1.0", "System.Text.Encoding.Extensions.dll"));

                public static MetadataReference SystemTextRegularExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.regularexpressions", "4.3.0", "ref", "netstandard1.0", "System.Text.RegularExpressions.dll"));

                public static MetadataReference SystemThreading => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading", "4.3.0", "ref", "netstandard1.0", "System.Threading.dll"));

                public static MetadataReference SystemThreadingTasks => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.tasks", "4.3.0", "ref", "netstandard1.0", "System.Threading.Tasks.dll"));

                public static MetadataReference SystemXmlReaderWriter => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.readerwriter", "4.3.0", "ref", "netstandard1.0", "System.Xml.ReaderWriter.dll"));

                public static MetadataReference SystemXmlXDocument => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.xdocument", "4.3.0", "ref", "netstandard1.0", "System.Xml.XDocument.dll"));
            }

            public static class NetStandard11
            {
                public static MetadataReference SystemCollections => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections", "4.3.0", "ref", "netstandard1.0", "System.Collections.dll"));

                public static MetadataReference SystemCollectionsConcurrent => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections.concurrent", "4.3.0", "ref", "netstandard1.1", "System.Collections.Concurrent.dll"));

                public static MetadataReference SystemDiagnosticsDebug => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.debug", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Debug.dll"));

                public static MetadataReference SystemDiagnosticsTools => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tools", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Tools.dll"));

                public static MetadataReference SystemDiagnosticsTracing => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tracing", "4.3.0", "ref", "netstandard1.1", "System.Diagnostics.Tracing.dll"));

                public static MetadataReference SystemGlobalization => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization", "4.3.0", "ref", "netstandard1.0", "System.Globalization.dll"));

                public static MetadataReference SystemIO => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io", "4.3.0", "ref", "netstandard1.0", "System.IO.dll"));

                public static MetadataReference SystemIOCompression => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression", "4.3.0", "ref", "netstandard1.1", "System.IO.Compression.dll"));

                public static MetadataReference SystemLinq => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq", "4.3.0", "ref", "netstandard1.0", "System.Linq.dll"));

                public static MetadataReference SystemLinqExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq.expressions", "4.3.0", "ref", "netstandard1.0", "System.Linq.Expressions.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.http", "4.3.0", "ref", "netstandard1.1", "System.Net.Http.dll"));

                public static MetadataReference SystemNetPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.primitives", "4.3.0", "ref", "netstandard1.1", "System.Net.Primitives.dll"));

                public static MetadataReference SystemObjectModel => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.objectmodel", "4.3.0", "ref", "netstandard1.0", "System.ObjectModel.dll"));

                public static MetadataReference SystemReflection => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection", "4.3.0", "ref", "netstandard1.0", "System.Reflection.dll"));

                public static MetadataReference SystemReflectionExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.extensions", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Extensions.dll"));

                public static MetadataReference SystemReflectionPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.primitives", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Primitives.dll"));

                public static MetadataReference SystemResourcesResourceManager => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.resources.resourcemanager", "4.3.0", "ref", "netstandard1.0", "System.Resources.ResourceManager.dll"));

                public static MetadataReference SystemRuntime => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime", "4.3.0", "ref", "netstandard1.0", "System.Runtime.dll"));

                public static MetadataReference SystemRuntimeExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.extensions", "4.3.0", "ref", "netstandard1.0", "System.Runtime.Extensions.dll"));

                public static MetadataReference SystemRuntimeInteropServices => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices", "4.3.0", "ref", "netstandard1.1", "System.Runtime.InteropServices.dll"));

                public static MetadataReference SystemRuntimeInteropServicesRuntimeInformation => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices.runtimeinformation", "4.3.0", "ref", "netstandard1.1", "System.Runtime.InteropServices.RuntimeInformation.dll"));

                public static MetadataReference SystemRuntimeNumerics => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.numerics", "4.3.0", "ref", "netstandard1.1", "System.Runtime.Numerics.dll"));

                public static MetadataReference SystemTextEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding", "4.3.0", "ref", "netstandard1.0", "System.Text.Encoding.dll"));

                public static MetadataReference SystemTextEncodingExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding.extensions", "4.3.0", "ref", "netstandard1.0", "System.Text.Encoding.Extensions.dll"));

                public static MetadataReference SystemTextRegularExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.regularexpressions", "4.3.0", "ref", "netstandard1.0", "System.Text.RegularExpressions.dll"));

                public static MetadataReference SystemThreading => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading", "4.3.0", "ref", "netstandard1.0", "System.Threading.dll"));

                public static MetadataReference SystemThreadingTasks => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.tasks", "4.3.0", "ref", "netstandard1.0", "System.Threading.Tasks.dll"));

                public static MetadataReference SystemXmlReaderWriter => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.readerwriter", "4.3.0", "ref", "netstandard1.0", "System.Xml.ReaderWriter.dll"));

                public static MetadataReference SystemXmlXDocument => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.xdocument", "4.3.0", "ref", "netstandard1.0", "System.Xml.XDocument.dll"));
            }

            public static class NetStandard12
            {
                public static MetadataReference SystemCollections => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections", "4.3.0", "ref", "netstandard1.0", "System.Collections.dll"));

                public static MetadataReference SystemCollectionsConcurrent => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections.concurrent", "4.3.0", "ref", "netstandard1.1", "System.Collections.Concurrent.dll"));

                public static MetadataReference SystemDiagnosticsDebug => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.debug", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Debug.dll"));

                public static MetadataReference SystemDiagnosticsTools => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tools", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Tools.dll"));

                public static MetadataReference SystemDiagnosticsTracing => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tracing", "4.3.0", "ref", "netstandard1.2", "System.Diagnostics.Tracing.dll"));

                public static MetadataReference SystemGlobalization => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization", "4.3.0", "ref", "netstandard1.0", "System.Globalization.dll"));

                public static MetadataReference SystemIO => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io", "4.3.0", "ref", "netstandard1.0", "System.IO.dll"));

                public static MetadataReference SystemIOCompression => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression", "4.3.0", "ref", "netstandard1.1", "System.IO.Compression.dll"));

                public static MetadataReference SystemLinq => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq", "4.3.0", "ref", "netstandard1.0", "System.Linq.dll"));

                public static MetadataReference SystemLinqExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq.expressions", "4.3.0", "ref", "netstandard1.0", "System.Linq.Expressions.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.http", "4.3.0", "ref", "netstandard1.1", "System.Net.Http.dll"));

                public static MetadataReference SystemNetPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.primitives", "4.3.0", "ref", "netstandard1.1", "System.Net.Primitives.dll"));

                public static MetadataReference SystemObjectModel => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.objectmodel", "4.3.0", "ref", "netstandard1.0", "System.ObjectModel.dll"));

                public static MetadataReference SystemReflection => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection", "4.3.0", "ref", "netstandard1.0", "System.Reflection.dll"));

                public static MetadataReference SystemReflectionExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.extensions", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Extensions.dll"));

                public static MetadataReference SystemReflectionPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.primitives", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Primitives.dll"));

                public static MetadataReference SystemResourcesResourceManager => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.resources.resourcemanager", "4.3.0", "ref", "netstandard1.0", "System.Resources.ResourceManager.dll"));

                public static MetadataReference SystemRuntime => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime", "4.3.0", "ref", "netstandard1.2", "System.Runtime.dll"));

                public static MetadataReference SystemRuntimeExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.extensions", "4.3.0", "ref", "netstandard1.0", "System.Runtime.Extensions.dll"));

                public static MetadataReference SystemRuntimeInteropServices => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices", "4.3.0", "ref", "netstandard1.2", "System.Runtime.InteropServices.dll"));

                public static MetadataReference SystemRuntimeInteropServicesRuntimeInformation => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices.runtimeinformation", "4.3.0", "ref", "netstandard1.1", "System.Runtime.InteropServices.RuntimeInformation.dll"));

                public static MetadataReference SystemRuntimeNumerics => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.numerics", "4.3.0", "ref", "netstandard1.1", "System.Runtime.Numerics.dll"));

                public static MetadataReference SystemTextEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding", "4.3.0", "ref", "netstandard1.0", "System.Text.Encoding.dll"));

                public static MetadataReference SystemTextEncodingExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding.extensions", "4.3.0", "ref", "netstandard1.0", "System.Text.Encoding.Extensions.dll"));

                public static MetadataReference SystemTextRegularExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.regularexpressions", "4.3.0", "ref", "netstandard1.0", "System.Text.RegularExpressions.dll"));

                public static MetadataReference SystemThreading => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading", "4.3.0", "ref", "netstandard1.0", "System.Threading.dll"));

                public static MetadataReference SystemThreadingTasks => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.tasks", "4.3.0", "ref", "netstandard1.0", "System.Threading.Tasks.dll"));

                public static MetadataReference SystemThreadingTimer => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.timer", "4.3.0", "ref", "netstandard1.2", "System.Threading.Timer.dll"));

                public static MetadataReference SystemXmlReaderWriter => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.readerwriter", "4.3.0", "ref", "netstandard1.0", "System.Xml.ReaderWriter.dll"));

                public static MetadataReference SystemXmlXDocument => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.xdocument", "4.3.0", "ref", "netstandard1.0", "System.Xml.XDocument.dll"));
            }

            public static class NetStandard13
            {
                public static MetadataReference MicrosoftWin32Primitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "microsoft.win32.primitives", "4.3.0", "ref", "netstandard1.3", "Microsoft.Win32.Primitives.dll"));

                public static MetadataReference SystemAppContext => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.appcontext", "4.3.0", "ref", "netstandard1.3", "System.AppContext.dll"));

                public static MetadataReference SystemCollections => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections", "4.3.0", "ref", "netstandard1.3", "System.Collections.dll"));

                public static MetadataReference SystemCollectionsConcurrent => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections.concurrent", "4.3.0", "ref", "netstandard1.3", "System.Collections.Concurrent.dll"));

                public static MetadataReference SystemConsole => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.console", "4.3.0", "ref", "netstandard1.3", "System.Console.dll"));

                public static MetadataReference SystemDiagnosticsDebug => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.debug", "4.3.0", "ref", "netstandard1.3", "System.Diagnostics.Debug.dll"));

                public static MetadataReference SystemDiagnosticsTools => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tools", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Tools.dll"));

                public static MetadataReference SystemDiagnosticsTracing => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tracing", "4.3.0", "ref", "netstandard1.3", "System.Diagnostics.Tracing.dll"));

                public static MetadataReference SystemGlobalization => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization", "4.3.0", "ref", "netstandard1.3", "System.Globalization.dll"));

                public static MetadataReference SystemGlobalizationCalendars => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization.calendars", "4.3.0", "ref", "netstandard1.3", "System.Globalization.Calendars.dll"));

                public static MetadataReference SystemIO => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io", "4.3.0", "ref", "netstandard1.3", "System.IO.dll"));

                public static MetadataReference SystemIOCompression => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression", "4.3.0", "ref", "netstandard1.3", "System.IO.Compression.dll"));

                public static MetadataReference SystemIOCompressionZipFile => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression.zipfile", "4.3.0", "ref", "netstandard1.3", "System.IO.Compression.ZipFile.dll"));

                public static MetadataReference SystemIOFileSystem => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.filesystem", "4.3.0", "ref", "netstandard1.3", "System.IO.FileSystem.dll"));

                public static MetadataReference SystemIOFileSystemPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.filesystem.primitives", "4.3.0", "ref", "netstandard1.3", "System.IO.FileSystem.Primitives.dll"));

                public static MetadataReference SystemLinq => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq", "4.3.0", "ref", "netstandard1.0", "System.Linq.dll"));

                public static MetadataReference SystemLinqExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq.expressions", "4.3.0", "ref", "netstandard1.3", "System.Linq.Expressions.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.http", "4.3.0", "ref", "netstandard1.3", "System.Net.Http.dll"));

                public static MetadataReference SystemNetPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.primitives", "4.3.0", "ref", "netstandard1.3", "System.Net.Primitives.dll"));

                public static MetadataReference SystemNetSockets => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.sockets", "4.3.0", "ref", "netstandard1.3", "System.Net.Sockets.dll"));

                public static MetadataReference SystemObjectModel => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.objectmodel", "4.3.0", "ref", "netstandard1.3", "System.ObjectModel.dll"));

                public static MetadataReference SystemReflection => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection", "4.3.0", "ref", "netstandard1.3", "System.Reflection.dll"));

                public static MetadataReference SystemReflectionExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.extensions", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Extensions.dll"));

                public static MetadataReference SystemReflectionPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.primitives", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Primitives.dll"));

                public static MetadataReference SystemResourcesResourceManager => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.resources.resourcemanager", "4.3.0", "ref", "netstandard1.0", "System.Resources.ResourceManager.dll"));

                public static MetadataReference SystemRuntime => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime", "4.3.0", "ref", "netstandard1.3", "System.Runtime.dll"));

                public static MetadataReference SystemRuntimeExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.extensions", "4.3.0", "ref", "netstandard1.3", "System.Runtime.Extensions.dll"));

                public static MetadataReference SystemRuntimeHandles => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.handles", "4.3.0", "ref", "netstandard1.3", "System.Runtime.Handles.dll"));

                public static MetadataReference SystemRuntimeInteropServices => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices", "4.3.0", "ref", "netstandard1.3", "System.Runtime.InteropServices.dll"));

                public static MetadataReference SystemRuntimeInteropServicesRuntimeInformation => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices.runtimeinformation", "4.3.0", "ref", "netstandard1.1", "System.Runtime.InteropServices.RuntimeInformation.dll"));

                public static MetadataReference SystemRuntimeNumerics => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.numerics", "4.3.0", "ref", "netstandard1.1", "System.Runtime.Numerics.dll"));

                public static MetadataReference SystemSecurityCryptographyAlgorithms => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.algorithms", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Algorithms.dll"));

                public static MetadataReference SystemSecurityCryptographyEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.encoding", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Encoding.dll"));

                public static MetadataReference SystemSecurityCryptographyPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.primitives", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Primitives.dll"));

                public static MetadataReference SystemSecurityCryptographyX509Certificates => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.x509certificates", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.X509Certificates.dll"));

                public static MetadataReference SystemTextEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding", "4.3.0", "ref", "netstandard1.3", "System.Text.Encoding.dll"));

                public static MetadataReference SystemTextEncodingExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding.extensions", "4.3.0", "ref", "netstandard1.3", "System.Text.Encoding.Extensions.dll"));

                public static MetadataReference SystemTextRegularExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.regularexpressions", "4.3.0", "ref", "netstandard1.3", "System.Text.RegularExpressions.dll"));

                public static MetadataReference SystemThreading => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading", "4.3.0", "ref", "netstandard1.3", "System.Threading.dll"));

                public static MetadataReference SystemThreadingTasks => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.tasks", "4.3.0", "ref", "netstandard1.3", "System.Threading.Tasks.dll"));

                public static MetadataReference SystemThreadingTimer => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.timer", "4.3.0", "ref", "netstandard1.2", "System.Threading.Timer.dll"));

                public static MetadataReference SystemXmlReaderWriter => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.readerwriter", "4.3.0", "ref", "netstandard1.3", "System.Xml.ReaderWriter.dll"));

                public static MetadataReference SystemXmlXDocument => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.xdocument", "4.3.0", "ref", "netstandard1.3", "System.Xml.XDocument.dll"));
            }

            public static class NetStandard14
            {
                public static MetadataReference MicrosoftWin32Primitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "microsoft.win32.primitives", "4.3.0", "ref", "netstandard1.3", "Microsoft.Win32.Primitives.dll"));

                public static MetadataReference SystemAppContext => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.appcontext", "4.3.0", "ref", "netstandard1.3", "System.AppContext.dll"));

                public static MetadataReference SystemCollections => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections", "4.3.0", "ref", "netstandard1.3", "System.Collections.dll"));

                public static MetadataReference SystemCollectionsConcurrent => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections.concurrent", "4.3.0", "ref", "netstandard1.3", "System.Collections.Concurrent.dll"));

                public static MetadataReference SystemConsole => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.console", "4.3.0", "ref", "netstandard1.3", "System.Console.dll"));

                public static MetadataReference SystemDiagnosticsDebug => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.debug", "4.3.0", "ref", "netstandard1.3", "System.Diagnostics.Debug.dll"));

                public static MetadataReference SystemDiagnosticsTools => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tools", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Tools.dll"));

                public static MetadataReference SystemDiagnosticsTracing => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tracing", "4.3.0", "ref", "netstandard1.3", "System.Diagnostics.Tracing.dll"));

                public static MetadataReference SystemGlobalization => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization", "4.3.0", "ref", "netstandard1.3", "System.Globalization.dll"));

                public static MetadataReference SystemGlobalizationCalendars => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization.calendars", "4.3.0", "ref", "netstandard1.3", "System.Globalization.Calendars.dll"));

                public static MetadataReference SystemIO => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io", "4.3.0", "ref", "netstandard1.3", "System.IO.dll"));

                public static MetadataReference SystemIOCompression => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression", "4.3.0", "ref", "netstandard1.3", "System.IO.Compression.dll"));

                public static MetadataReference SystemIOCompressionZipFile => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression.zipfile", "4.3.0", "ref", "netstandard1.3", "System.IO.Compression.ZipFile.dll"));

                public static MetadataReference SystemIOFileSystem => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.filesystem", "4.3.0", "ref", "netstandard1.3", "System.IO.FileSystem.dll"));

                public static MetadataReference SystemIOFileSystemPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.filesystem.primitives", "4.3.0", "ref", "netstandard1.3", "System.IO.FileSystem.Primitives.dll"));

                public static MetadataReference SystemLinq => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq", "4.3.0", "ref", "netstandard1.0", "System.Linq.dll"));

                public static MetadataReference SystemLinqExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq.expressions", "4.3.0", "ref", "netstandard1.3", "System.Linq.Expressions.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.http", "4.3.0", "ref", "netstandard1.3", "System.Net.Http.dll"));

                public static MetadataReference SystemNetPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.primitives", "4.3.0", "ref", "netstandard1.3", "System.Net.Primitives.dll"));

                public static MetadataReference SystemNetSockets => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.sockets", "4.3.0", "ref", "netstandard1.3", "System.Net.Sockets.dll"));

                public static MetadataReference SystemObjectModel => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.objectmodel", "4.3.0", "ref", "netstandard1.3", "System.ObjectModel.dll"));

                public static MetadataReference SystemReflection => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection", "4.3.0", "ref", "netstandard1.3", "System.Reflection.dll"));

                public static MetadataReference SystemReflectionExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.extensions", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Extensions.dll"));

                public static MetadataReference SystemReflectionPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.primitives", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Primitives.dll"));

                public static MetadataReference SystemResourcesResourceManager => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.resources.resourcemanager", "4.3.0", "ref", "netstandard1.0", "System.Resources.ResourceManager.dll"));

                public static MetadataReference SystemRuntime => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime", "4.3.0", "ref", "netstandard1.3", "System.Runtime.dll"));

                public static MetadataReference SystemRuntimeExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.extensions", "4.3.0", "ref", "netstandard1.3", "System.Runtime.Extensions.dll"));

                public static MetadataReference SystemRuntimeHandles => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.handles", "4.3.0", "ref", "netstandard1.3", "System.Runtime.Handles.dll"));

                public static MetadataReference SystemRuntimeInteropServices => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices", "4.3.0", "ref", "netstandard1.3", "System.Runtime.InteropServices.dll"));

                public static MetadataReference SystemRuntimeInteropServicesRuntimeInformation => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices.runtimeinformation", "4.3.0", "ref", "netstandard1.1", "System.Runtime.InteropServices.RuntimeInformation.dll"));

                public static MetadataReference SystemRuntimeNumerics => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.numerics", "4.3.0", "ref", "netstandard1.1", "System.Runtime.Numerics.dll"));

                public static MetadataReference SystemSecurityCryptographyAlgorithms => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.algorithms", "4.3.0", "ref", "netstandard1.4", "System.Security.Cryptography.Algorithms.dll"));

                public static MetadataReference SystemSecurityCryptographyEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.encoding", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Encoding.dll"));

                public static MetadataReference SystemSecurityCryptographyPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.primitives", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Primitives.dll"));

                public static MetadataReference SystemSecurityCryptographyX509Certificates => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.x509certificates", "4.3.0", "ref", "netstandard1.4", "System.Security.Cryptography.X509Certificates.dll"));

                public static MetadataReference SystemTextEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding", "4.3.0", "ref", "netstandard1.3", "System.Text.Encoding.dll"));

                public static MetadataReference SystemTextEncodingExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding.extensions", "4.3.0", "ref", "netstandard1.3", "System.Text.Encoding.Extensions.dll"));

                public static MetadataReference SystemTextRegularExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.regularexpressions", "4.3.0", "ref", "netstandard1.3", "System.Text.RegularExpressions.dll"));

                public static MetadataReference SystemThreading => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading", "4.3.0", "ref", "netstandard1.3", "System.Threading.dll"));

                public static MetadataReference SystemThreadingTasks => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.tasks", "4.3.0", "ref", "netstandard1.3", "System.Threading.Tasks.dll"));

                public static MetadataReference SystemThreadingTimer => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.timer", "4.3.0", "ref", "netstandard1.2", "System.Threading.Timer.dll"));

                public static MetadataReference SystemXmlReaderWriter => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.readerwriter", "4.3.0", "ref", "netstandard1.3", "System.Xml.ReaderWriter.dll"));

                public static MetadataReference SystemXmlXDocument => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.xdocument", "4.3.0", "ref", "netstandard1.3", "System.Xml.XDocument.dll"));
            }

            public static class NetStandard15
            {
                public static MetadataReference MicrosoftWin32Primitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "microsoft.win32.primitives", "4.3.0", "ref", "netstandard1.3", "Microsoft.Win32.Primitives.dll"));

                public static MetadataReference SystemAppContext => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.appcontext", "4.3.0", "ref", "netstandard1.3", "System.AppContext.dll"));

                public static MetadataReference SystemCollections => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections", "4.3.0", "ref", "netstandard1.3", "System.Collections.dll"));

                public static MetadataReference SystemCollectionsConcurrent => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections.concurrent", "4.3.0", "ref", "netstandard1.3", "System.Collections.Concurrent.dll"));

                public static MetadataReference SystemConsole => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.console", "4.3.0", "ref", "netstandard1.3", "System.Console.dll"));

                public static MetadataReference SystemDiagnosticsDebug => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.debug", "4.3.0", "ref", "netstandard1.3", "System.Diagnostics.Debug.dll"));

                public static MetadataReference SystemDiagnosticsTools => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tools", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Tools.dll"));

                public static MetadataReference SystemDiagnosticsTracing => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tracing", "4.3.0", "ref", "netstandard1.5", "System.Diagnostics.Tracing.dll"));

                public static MetadataReference SystemGlobalization => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization", "4.3.0", "ref", "netstandard1.3", "System.Globalization.dll"));

                public static MetadataReference SystemGlobalizationCalendars => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization.calendars", "4.3.0", "ref", "netstandard1.3", "System.Globalization.Calendars.dll"));

                public static MetadataReference SystemIO => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io", "4.3.0", "ref", "netstandard1.5", "System.IO.dll"));

                public static MetadataReference SystemIOCompression => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression", "4.3.0", "ref", "netstandard1.3", "System.IO.Compression.dll"));

                public static MetadataReference SystemIOCompressionZipFile => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression.zipfile", "4.3.0", "ref", "netstandard1.3", "System.IO.Compression.ZipFile.dll"));

                public static MetadataReference SystemIOFileSystem => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.filesystem", "4.3.0", "ref", "netstandard1.3", "System.IO.FileSystem.dll"));

                public static MetadataReference SystemIOFileSystemPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.filesystem.primitives", "4.3.0", "ref", "netstandard1.3", "System.IO.FileSystem.Primitives.dll"));

                public static MetadataReference SystemLinq => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq", "4.3.0", "ref", "netstandard1.0", "System.Linq.dll"));

                public static MetadataReference SystemLinqExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq.expressions", "4.3.0", "ref", "netstandard1.3", "System.Linq.Expressions.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.http", "4.3.0", "ref", "netstandard1.3", "System.Net.Http.dll"));

                public static MetadataReference SystemNetPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.primitives", "4.3.0", "ref", "netstandard1.3", "System.Net.Primitives.dll"));

                public static MetadataReference SystemNetSockets => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.sockets", "4.3.0", "ref", "netstandard1.3", "System.Net.Sockets.dll"));

                public static MetadataReference SystemObjectModel => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.objectmodel", "4.3.0", "ref", "netstandard1.3", "System.ObjectModel.dll"));

                public static MetadataReference SystemReflection => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection", "4.3.0", "ref", "netstandard1.3", "System.Reflection.dll"));

                public static MetadataReference SystemReflectionExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.extensions", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Extensions.dll"));

                public static MetadataReference SystemReflectionPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.primitives", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Primitives.dll"));

                public static MetadataReference SystemResourcesResourceManager => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.resources.resourcemanager", "4.3.0", "ref", "netstandard1.0", "System.Resources.ResourceManager.dll"));

                public static MetadataReference SystemRuntime => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime", "4.3.0", "ref", "netstandard1.5", "System.Runtime.dll"));

                public static MetadataReference SystemRuntimeExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.extensions", "4.3.0", "ref", "netstandard1.5", "System.Runtime.Extensions.dll"));

                public static MetadataReference SystemRuntimeHandles => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.handles", "4.3.0", "ref", "netstandard1.3", "System.Runtime.Handles.dll"));

                public static MetadataReference SystemRuntimeInteropServices => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices", "4.3.0", "ref", "netstandard1.5", "System.Runtime.InteropServices.dll"));

                public static MetadataReference SystemRuntimeInteropServicesRuntimeInformation => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices.runtimeinformation", "4.3.0", "ref", "netstandard1.1", "System.Runtime.InteropServices.RuntimeInformation.dll"));

                public static MetadataReference SystemRuntimeNumerics => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.numerics", "4.3.0", "ref", "netstandard1.1", "System.Runtime.Numerics.dll"));

                public static MetadataReference SystemSecurityCryptographyAlgorithms => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.algorithms", "4.3.0", "ref", "netstandard1.4", "System.Security.Cryptography.Algorithms.dll"));

                public static MetadataReference SystemSecurityCryptographyEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.encoding", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Encoding.dll"));

                public static MetadataReference SystemSecurityCryptographyPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.primitives", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Primitives.dll"));

                public static MetadataReference SystemSecurityCryptographyX509Certificates => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.x509certificates", "4.3.0", "ref", "netstandard1.4", "System.Security.Cryptography.X509Certificates.dll"));

                public static MetadataReference SystemTextEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding", "4.3.0", "ref", "netstandard1.3", "System.Text.Encoding.dll"));

                public static MetadataReference SystemTextEncodingExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding.extensions", "4.3.0", "ref", "netstandard1.3", "System.Text.Encoding.Extensions.dll"));

                public static MetadataReference SystemTextRegularExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.regularexpressions", "4.3.0", "ref", "netstandard1.3", "System.Text.RegularExpressions.dll"));

                public static MetadataReference SystemThreading => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading", "4.3.0", "ref", "netstandard1.3", "System.Threading.dll"));

                public static MetadataReference SystemThreadingTasks => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.tasks", "4.3.0", "ref", "netstandard1.3", "System.Threading.Tasks.dll"));

                public static MetadataReference SystemThreadingTimer => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.timer", "4.3.0", "ref", "netstandard1.2", "System.Threading.Timer.dll"));

                public static MetadataReference SystemXmlReaderWriter => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.readerwriter", "4.3.0", "ref", "netstandard1.3", "System.Xml.ReaderWriter.dll"));

                public static MetadataReference SystemXmlXDocument => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.xdocument", "4.3.0", "ref", "netstandard1.3", "System.Xml.XDocument.dll"));
            }

            public static class NetStandard16
            {
                public static MetadataReference MicrosoftWin32Primitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "microsoft.win32.primitives", "4.3.0", "ref", "netstandard1.3", "Microsoft.Win32.Primitives.dll"));

                public static MetadataReference SystemAppContext => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.appcontext", "4.3.0", "ref", "netstandard1.6", "System.AppContext.dll"));

                public static MetadataReference SystemCollections => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections", "4.3.0", "ref", "netstandard1.3", "System.Collections.dll"));

                public static MetadataReference SystemCollectionsConcurrent => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.collections.concurrent", "4.3.0", "ref", "netstandard1.3", "System.Collections.Concurrent.dll"));

                public static MetadataReference SystemConsole => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.console", "4.3.0", "ref", "netstandard1.3", "System.Console.dll"));

                public static MetadataReference SystemDiagnosticsDebug => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.debug", "4.3.0", "ref", "netstandard1.3", "System.Diagnostics.Debug.dll"));

                public static MetadataReference SystemDiagnosticsTools => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tools", "4.3.0", "ref", "netstandard1.0", "System.Diagnostics.Tools.dll"));

                public static MetadataReference SystemDiagnosticsTracing => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.diagnostics.tracing", "4.3.0", "ref", "netstandard1.5", "System.Diagnostics.Tracing.dll"));

                public static MetadataReference SystemGlobalization => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization", "4.3.0", "ref", "netstandard1.3", "System.Globalization.dll"));

                public static MetadataReference SystemGlobalizationCalendars => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.globalization.calendars", "4.3.0", "ref", "netstandard1.3", "System.Globalization.Calendars.dll"));

                public static MetadataReference SystemIO => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io", "4.3.0", "ref", "netstandard1.5", "System.IO.dll"));

                public static MetadataReference SystemIOCompression => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression", "4.3.0", "ref", "netstandard1.3", "System.IO.Compression.dll"));

                public static MetadataReference SystemIOCompressionZipFile => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.compression.zipfile", "4.3.0", "ref", "netstandard1.3", "System.IO.Compression.ZipFile.dll"));

                public static MetadataReference SystemIOFileSystem => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.filesystem", "4.3.0", "ref", "netstandard1.3", "System.IO.FileSystem.dll"));

                public static MetadataReference SystemIOFileSystemPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.io.filesystem.primitives", "4.3.0", "ref", "netstandard1.3", "System.IO.FileSystem.Primitives.dll"));

                public static MetadataReference SystemLinq => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq", "4.3.0", "ref", "netstandard1.6", "System.Linq.dll"));

                public static MetadataReference SystemLinqExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.linq.expressions", "4.3.0", "ref", "netstandard1.6", "System.Linq.Expressions.dll"));

                public static MetadataReference SystemNetHttp => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.http", "4.3.0", "ref", "netstandard1.3", "System.Net.Http.dll"));

                public static MetadataReference SystemNetPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.primitives", "4.3.0", "ref", "netstandard1.3", "System.Net.Primitives.dll"));

                public static MetadataReference SystemNetSockets => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.net.sockets", "4.3.0", "ref", "netstandard1.3", "System.Net.Sockets.dll"));

                public static MetadataReference SystemObjectModel => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.objectmodel", "4.3.0", "ref", "netstandard1.3", "System.ObjectModel.dll"));

                public static MetadataReference SystemReflection => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection", "4.3.0", "ref", "netstandard1.3", "System.Reflection.dll"));

                public static MetadataReference SystemReflectionExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.extensions", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Extensions.dll"));

                public static MetadataReference SystemReflectionPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.reflection.primitives", "4.3.0", "ref", "netstandard1.0", "System.Reflection.Primitives.dll"));

                public static MetadataReference SystemResourcesResourceManager => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.resources.resourcemanager", "4.3.0", "ref", "netstandard1.0", "System.Resources.ResourceManager.dll"));

                public static MetadataReference SystemRuntime => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime", "4.3.0", "ref", "netstandard1.5", "System.Runtime.dll"));

                public static MetadataReference SystemRuntimeExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.extensions", "4.3.0", "ref", "netstandard1.5", "System.Runtime.Extensions.dll"));

                public static MetadataReference SystemRuntimeHandles => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.handles", "4.3.0", "ref", "netstandard1.3", "System.Runtime.Handles.dll"));

                public static MetadataReference SystemRuntimeInteropServices => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices", "4.3.0", "ref", "netstandard1.5", "System.Runtime.InteropServices.dll"));

                public static MetadataReference SystemRuntimeInteropServicesRuntimeInformation => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.interopservices.runtimeinformation", "4.3.0", "ref", "netstandard1.1", "System.Runtime.InteropServices.RuntimeInformation.dll"));

                public static MetadataReference SystemRuntimeNumerics => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.runtime.numerics", "4.3.0", "ref", "netstandard1.1", "System.Runtime.Numerics.dll"));

                public static MetadataReference SystemSecurityCryptographyAlgorithms => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.algorithms", "4.3.0", "ref", "netstandard1.6", "System.Security.Cryptography.Algorithms.dll"));

                public static MetadataReference SystemSecurityCryptographyEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.encoding", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Encoding.dll"));

                public static MetadataReference SystemSecurityCryptographyPrimitives => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.primitives", "4.3.0", "ref", "netstandard1.3", "System.Security.Cryptography.Primitives.dll"));

                public static MetadataReference SystemSecurityCryptographyX509Certificates => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.security.cryptography.x509certificates", "4.3.0", "ref", "netstandard1.4", "System.Security.Cryptography.X509Certificates.dll"));

                public static MetadataReference SystemTextEncoding => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding", "4.3.0", "ref", "netstandard1.3", "System.Text.Encoding.dll"));

                public static MetadataReference SystemTextEncodingExtensions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.encoding.extensions", "4.3.0", "ref", "netstandard1.3", "System.Text.Encoding.Extensions.dll"));

                public static MetadataReference SystemTextRegularExpressions => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.text.regularexpressions", "4.3.0", "ref", "netstandard1.6", "System.Text.RegularExpressions.dll"));

                public static MetadataReference SystemThreading => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading", "4.3.0", "ref", "netstandard1.3", "System.Threading.dll"));

                public static MetadataReference SystemThreadingTasks => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.tasks", "4.3.0", "ref", "netstandard1.3", "System.Threading.Tasks.dll"));

                public static MetadataReference SystemThreadingTimer => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.threading.timer", "4.3.0", "ref", "netstandard1.2", "System.Threading.Timer.dll"));

                public static MetadataReference SystemXmlReaderWriter => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.readerwriter", "4.3.0", "ref", "netstandard1.3", "System.Xml.ReaderWriter.dll"));

                public static MetadataReference SystemXmlXDocument => CreateReferenceFromFile(Path.Combine(PackagesPath, "system.xml.xdocument", "4.3.0", "ref", "netstandard1.3", "System.Xml.XDocument.dll"));
            }

            public static class NetStandard20
            {
                private static string AssemblyPath => Path.Combine(PackagesPath, "netstandard.library", "2.0.0", "build", "netstandard2.0", "ref");

                public static MetadataReference Netstandard => CreateReferenceFromFile(Path.Combine(AssemblyPath, "netstandard.dll"));
            }
        }

        public static class Runtime
        {
            private static MetadataReference? _microsoftVisualBasicReference;

            public static MetadataReference MicrosoftCodeAnalysis { get; } = CreateReferenceFromFile(typeof(Compilation).GetTypeInfo().Assembly.Location);

            public static MetadataReference MicrosoftVisualBasic
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                get
                {
                    if (_microsoftVisualBasicReference is null)
                    {
                        try
                        {
                            _microsoftVisualBasicReference = CreateReferenceFromFile(typeof(Microsoft.VisualBasic.Strings).GetTypeInfo().Assembly.Location);
                        }
                        catch (Exception e)
                        {
                            throw new PlatformNotSupportedException("Microsoft.VisualBasic is not supported on this platform", e);
                        }
                    }

                    return _microsoftVisualBasicReference;
                }
            }

            public static MetadataReference SystemCollectionsImmutable { get; } = CreateReferenceFromFile(typeof(ImmutableArray).GetTypeInfo().Assembly.Location);
        }
    }
}
