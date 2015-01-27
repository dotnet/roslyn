// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal class GenerateTypeOptionsResult
    {
        public static readonly GenerateTypeOptionsResult Cancelled = new GenerateTypeOptionsResult(isCancelled: true);

        public Accessibility Accessibility { get; private set; }
        public Document ExistingDocument { get; private set; }
        public bool IsCancelled { get; private set; }
        public bool IsNewFile { get; private set; }
        public IList<string> Folders { get; private set; }
        public string NewFileName { get; private set; }
        public Project Project { get; private set; }
        public TypeKind TypeKind { get; private set; }
        public string FullFilePath { get; private set; }
        public string TypeName { get; private set; }
        public bool AreFoldersValidIdentifiers { get; private set; }

        public GenerateTypeOptionsResult(
            Accessibility accessibility,
            TypeKind typeKind,
            string typeName,
            Project project,
            bool isNewFile,
            string newFileName,
            IList<string> folders,
            string fullFilePath,
            Document existingDocument,
            bool areFoldersValidIdentifiers,
            bool isCancelled = false)
        {
            this.Accessibility = accessibility;
            this.TypeKind = typeKind;
            this.TypeName = typeName;
            this.Project = project;
            this.IsNewFile = isNewFile;
            this.NewFileName = newFileName;
            this.Folders = folders;
            this.FullFilePath = fullFilePath;
            this.ExistingDocument = existingDocument;
            this.AreFoldersValidIdentifiers = areFoldersValidIdentifiers;
            this.IsCancelled = isCancelled;
        }

        private GenerateTypeOptionsResult(bool isCancelled)
        {
            this.IsCancelled = isCancelled;
        }
    }
}
