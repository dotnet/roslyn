// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// This is the root class for all code model objects. It contains methods that
    /// are common to everything.
    /// </summary>
    public abstract partial class AbstractCodeModelObject : ApartmentSensitiveComObject
    {
        private CodeModelState _state;
        private bool _zombied;

        internal AbstractCodeModelObject(CodeModelState state)
        {
            Debug.Assert(state != null);

            _state = state;
        }

        protected bool IsZombied
        {
            get { return _zombied; }
        }

        internal CodeModelState State
        {
            get
            {
                if (IsZombied)
                {
                    Debug.Fail("Cannot access " + this.GetType().FullName + " after it has been ShutDown!");
                    throw Exceptions.ThrowEUnexpected();
                }

                return _state;
            }
        }

        internal ICodeGenerationService CodeGenerationService
        {
            get { return this.State.CodeGenerator; }
        }

        internal ICodeModelService CodeModelService
        {
            get { return this.State.CodeModelService; }
        }

        internal IServiceProvider ServiceProvider
        {
            get { return this.State.ServiceProvider; }
        }

        internal ISyntaxFactsService SyntaxFactsService
        {
            get { return this.State.SyntaxFactsService; }
        }

        internal VisualStudioWorkspace Workspace
        {
            get { return this.State.Workspace; }
        }

        internal virtual void Shutdown()
        {
            _state = null;
            _zombied = true;
        }

        public DTE DTE
        {
            get { return (DTE)this.ServiceProvider.GetService(typeof(SDTE)); }
        }

        public string Language
        {
            get { return this.CodeModelService.Language; }
        }

        protected EnvDTE.CodeElements GetCollection<T>(object parentObject)
        {
            var parentInstance = ComAggregate.GetManagedObject<object>(parentObject);
            Debug.Assert(!Marshal.IsComObject(parentInstance), "We should have a pure managed object!");

            if (parentInstance is ICodeElementContainer<T> container)
            {
                return container.GetCollection();
            }

            throw Exceptions.ThrowEFail();
        }
    }
}
