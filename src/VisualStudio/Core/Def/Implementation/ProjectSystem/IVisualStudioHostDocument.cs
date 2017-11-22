// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Represents a source document that comes from the <see cref="DocumentProvider"/> used in Visual Studio.
    /// </summary>
    /// <remarks>
    /// It guarantees the existence of a Dispose method, which allows the workspace/project system layer to clean up file system watchers for this
    /// document when they are no longer needed.
    /// </remarks>
    internal interface IVisualStudioHostDocument : IDisposable
    {
        /// <summary>
        /// The visual studio project this document is part of.
        /// </summary>
        IVisualStudioHostProject Project { get; }

        /// <summary>
        /// The Visual Studio identity of the document within its project.
        /// </summary>
        DocumentKey Key { get; }

        /// <summary>
        /// The workspace document Id for this document.
        /// </summary>
        DocumentId Id { get; }

        /// <summary>
        /// The path to the document's file on disk.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// The name of the document.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The logical folders associated with the document. This may be different than the actual folders
        /// in the file path.
        /// </summary>
        IReadOnlyList<string> Folders { get; }

        /// <summary>
        /// A loader that can access the current stored text of the document.
        /// </summary>
        TextLoader Loader { get; }

        /// <summary>
        /// Returns true if the document is currently open in an editor.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Fired after the file is updated on disk. If the file is open in the editor, this event is not fired.
        /// </summary>
        event EventHandler UpdatedOnDisk;

        /// <summary>
        /// Fired after the document has been opened in Visual Studio. GetTextBuffer() will return the actual live
        /// editor.
        /// </summary>
        event EventHandler<bool> Opened;

        /// <summary>
        /// Fired as the document is being closed in Visual Studio. GetTextBuffer() still returns the editor that was
        /// live in Visual Studio, but is going away shortly.
        /// </summary>
        event EventHandler<bool> Closing;

        /// <summary>
        /// Returns and IDocumentInfo with the initial state of this document when it was first loaded.
        /// </summary>
        /// <returns></returns>
        DocumentInfo GetInitialState();

        /// <summary>
        /// The ItemID for this document. This method must be called on the UI thread, and the 
        /// returned value must be used while still on the UI thread, or must be appropriately
        /// invalidated when the relevant <see cref="IVsHierarchyEvents"/> are triggered. 
        /// Otherwise, this ItemId may be stale or destroyed within its <see cref="IVsHierarchy"/>
        /// before this document is removed from its project. These are only really useful for 
        /// "normal" files, that is regular .cs files that are compiled in a normal project. 
        /// It may be <see cref="VSConstants.VSITEMID.Nil"/> in the case of files that have very
        /// recently been removed or that are in miscellaneous files projects, or it may not even
        /// be stable in the case of strange files like .g.i.cs files.
        /// </summary>
        uint GetItemId();

        /// <summary>
        /// Gets the text container associated with the document when it is in an opened state.
        /// </summary>
        /// <returns></returns>
        SourceTextContainer GetOpenTextContainer();

        /// <summary>
        /// Gets the text buffer associated with the document when it is in an opened state.
        /// </summary>
        ITextBuffer GetOpenTextBuffer();

        /// <summary>
        /// Updates the text of the document.
        /// </summary>
        void UpdateText(SourceText newText);

        /// <summary>
        /// Fetches the <see cref="ITextBuffer"/> that should be used to undo edits to this document.
        /// </summary>
        ITextBuffer GetTextUndoHistoryBuffer();
    }
}
