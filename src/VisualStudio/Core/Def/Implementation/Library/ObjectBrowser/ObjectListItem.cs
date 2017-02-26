﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract class ObjectListItem
    {
        private readonly ProjectId _projectId;
        private ObjectList _parentList;
        private readonly ushort _glyphIndex;
        private readonly bool _isHidden;

        protected ObjectListItem(
            ProjectId projectId,
            StandardGlyphGroup glyphGroup,
            StandardGlyphItem glyphItem = StandardGlyphItem.GlyphItemPublic,
            bool isHidden = false)
        {
            _projectId = projectId;

            _glyphIndex = glyphGroup < StandardGlyphGroup.GlyphGroupError
                ? (ushort)((int)glyphGroup + (int)glyphItem)
                : (ushort)glyphGroup;

            _isHidden = isHidden;
        }

        internal void SetParentList(ObjectList parentList)
        {
            Debug.Assert(_parentList == null);
            _parentList = parentList;
        }

        public virtual bool SupportsGoToDefinition => false;

        public virtual bool SupportsFindAllReferences => false;

        public abstract string DisplayText { get; }

        public abstract string FullNameText { get; }

        public abstract string SearchText { get; }

        public override string ToString()
        {
            return DisplayText;
        }

        public ObjectList ParentList => _parentList;

        public ObjectListKind ParentListKind
        {
            get
            {
                return _parentList != null
                    ? _parentList.Kind
                    : ObjectListKind.None;
            }
        }

        public ProjectId ProjectId => _projectId;

        public Compilation GetCompilation(Workspace workspace)
        {
            var project = workspace.CurrentSolution.GetProject(_projectId);
            if (project == null)
            {
                return null;
            }

            return project
                .GetCompilationAsync(CancellationToken.None)
                .WaitAndGetResult_ObjectBrowser(CancellationToken.None);
        }

        public ushort GlyphIndex => _glyphIndex;

        public bool IsHidden => _isHidden;
    }
}
