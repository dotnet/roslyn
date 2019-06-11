// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
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

        internal static MetadataReference CreateReferenceFromFile(string path)
        {
            var documentationFile = Path.ChangeExtension(path, ".xml");
            return MetadataReference.CreateFromFile(path, documentation: s_createDocumentationProvider(documentationFile));
        }
    }
}
