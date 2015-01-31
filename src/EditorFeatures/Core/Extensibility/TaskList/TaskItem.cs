// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor
{
    internal class TaskItem : AbstractTaskItem
    {
        private readonly string _message;
        private readonly Workspace _workspace;
        private readonly DocumentId _documentId;
        private readonly string _mappedFilePath;
        private readonly string _originalFilePath;

        private readonly int _mappedLine;
        private readonly int _mappedColumn;
        private readonly int _originalLine;
        private readonly int _originalColumn;

        public TaskItem(
            string message,
            Workspace workspace,
            DocumentId documentId,
            int mappedLine,
            int originalLine,
            int mappedColumn,
            int originalColumn,
            string mappedFilePath,
            string originalFilePath)
        {
            _message = message;
            _workspace = workspace;
            _documentId = documentId;

            _mappedLine = mappedLine;
            _mappedColumn = mappedColumn;
            _mappedFilePath = mappedFilePath;

            _originalLine = originalLine;
            _originalColumn = originalColumn;
            _originalFilePath = originalFilePath;
        }

        public override string Message
        {
            get { return _message; }
        }

        public override Workspace Workspace
        {
            get { return _workspace; }
        }

        public override DocumentId DocumentId
        {
            get { return _documentId; }
        }

        public override string MappedFilePath
        {
            get { return _mappedFilePath; }
        }

        public override string OriginalFilePath
        {
            get { return _originalFilePath; }
        }

        public override int MappedLine
        {
            get { return _mappedLine; }
        }

        public override int MappedColumn
        {
            get { return _mappedColumn; }
        }

        public override int OriginalLine
        {
            get { return _originalLine; }
        }

        public override int OriginalColumn
        {
            get { return _originalColumn; }
        }
    }
}
