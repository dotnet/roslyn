﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal abstract class AbstractContainedLanguage : IDisposable
    {
        public AbstractProject Project { get; }

        /// <summary>
        /// The subject (secondary) buffer that contains the C# or VB code.
        /// </summary>
        public ITextBuffer SubjectBuffer { get; private set; }

        /// <summary>
        /// The underlying buffer that contains C# or VB code. NOTE: This is NOT the "document" buffer
        /// that is saved to disk.  Instead it is the view that the user sees.  The normal buffer graph
        /// in Venus includes 4 buffers:
        /// <code>
        ///            SurfaceBuffer/Databuffer (projection)
        ///             /                               |
        /// Subject Buffer (C#/VB projection)           |
        ///             |                               |
        /// Inert (generated) C#/VB Buffer         Document (aspx) buffer
        /// </code>
        /// In normal circumstance, the Subject and Inert C# buffer are identical in content, and the
        /// Surface and Document are also identical.  The Subject Buffer is the one that is part of the
        /// workspace, that most language operations deal with.  The surface buffer is the one that the
        /// view is created over, and the Document buffer is the one that is saved to disk.
        /// </summary>
        public ITextBuffer DataBuffer { get; private set; }

        public IVsContainedLanguageHost ContainedLanguageHost { get; protected set; }
        public IVsTextBufferCoordinator BufferCoordinator { get; protected set; }

        public AbstractContainedLanguage(
            AbstractProject project)
        {
            this.Project = project ?? throw new ArgumentNullException(nameof(project));
        }

        /// <summary>
        /// To be called from the derived class constructor!
        /// </summary>
        /// <param name="subjectBuffer"></param>
        protected void SetSubjectBuffer(ITextBuffer subjectBuffer)
        {
            this.SubjectBuffer = subjectBuffer ?? throw new ArgumentNullException(nameof(subjectBuffer));
        }

        /// <summary>
        /// To be called from the derived class constructor!
        /// </summary>
        protected void SetDataBuffer(ITextBuffer dataBuffer)
        {
            this.DataBuffer = dataBuffer ?? throw new ArgumentNullException(nameof(dataBuffer));
        }

        public abstract void Dispose();
    }
}
