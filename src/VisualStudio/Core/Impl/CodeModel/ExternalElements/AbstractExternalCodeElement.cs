// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    public abstract class AbstractExternalCodeElement : AbstractCodeModelObject, ICodeElementContainer<AbstractExternalCodeElement>, EnvDTE.CodeElement, EnvDTE80.CodeElement2
    {
        protected readonly ProjectId ProjectId;
        internal readonly SymbolKey SymbolKey;

        internal AbstractExternalCodeElement(CodeModelState state, ProjectId projectId, ISymbol symbol)
            : base(state)
        {
            Debug.Assert(projectId != null);
            Debug.Assert(symbol != null);

            this.ProjectId = projectId;
            this.SymbolKey = symbol.GetSymbolKey();
        }

        internal Compilation GetCompilation()
        {
            var project = this.State.Workspace.CurrentSolution.GetProject(this.ProjectId);

            if (project == null)
            {
                throw Exceptions.ThrowEFail();
            }

            return project.GetCompilationAsync(CancellationToken.None).Result;
        }

        internal ISymbol LookupSymbol()
        {
            var symbol = CodeModelService.ResolveSymbol(this.State.Workspace, this.ProjectId, this.SymbolKey);

            if (symbol == null)
            {
                throw Exceptions.ThrowEFail();
            }

            return symbol;
        }

        protected virtual EnvDTE.vsCMAccess GetAccess()
        {
            return CodeModelService.GetAccess(LookupSymbol());
        }

        protected virtual string GetDocComment()
        {
            var symbol = LookupSymbol();

            if (symbol == null)
            {
                throw Exceptions.ThrowEFail();
            }

            var documentationCommentXml = symbol.OriginalDefinition.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(documentationCommentXml))
            {
                return string.Empty;
            }

            XElement xml;
            try
            {
                xml = XElement.Parse(documentationCommentXml);
            }
            catch (XmlException)
            {
                return string.Empty;
            }

            // Surround with <doc> element. Or replace <member> element with <doc>, if it exists.
            if (xml.Name == "member")
            {
                xml.Name = "doc";
                xml.RemoveAttributes();
            }
            else
            {
                xml = new XElement("doc", xml);
            }

            return xml.ToString();
        }

        protected virtual string GetFullName()
        {
            return CodeModelService.GetExternalSymbolFullName(LookupSymbol());
        }

        protected virtual bool GetIsShared()
        {
            var symbol = LookupSymbol();
            return symbol.IsStatic;
        }

        protected virtual string GetName()
        {
            return CodeModelService.GetExternalSymbolName(LookupSymbol());
        }

        protected virtual object GetParent()
        {
            var symbol = LookupSymbol();

            if (symbol.Kind == SymbolKind.Namespace &&
                ((INamespaceSymbol)symbol).IsGlobalNamespace)
            {
                // TODO: We should be returning the RootCodeModel object here.
                throw new NotImplementedException();
            }

            if (symbol.ContainingType != null)
            {
                return CodeModelService.CreateCodeType(this.State, this.ProjectId, symbol.ContainingType);
            }
            else if (symbol.ContainingNamespace != null)
            {
                return CodeModelService.CreateExternalCodeElement(this.State, this.ProjectId, symbol.ContainingNamespace);
            }

            throw Exceptions.ThrowEFail();
        }

        public EnvDTE.vsCMAccess Access
        {
            get
            {
                return GetAccess();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public EnvDTE.CodeElements Attributes
        {
            get { return EmptyCollection.Create(this.State, this); }
        }

        public virtual EnvDTE.CodeElements Children
        {
            get { throw new NotImplementedException(); }
        }

        EnvDTE.CodeElements ICodeElementContainer<AbstractExternalCodeElement>.GetCollection()
        {
            return Children;
        }

        protected virtual EnvDTE.CodeElements GetCollection()
        {
            return GetCollection<AbstractExternalCodeElement>(this.Parent);
        }

        public EnvDTE.CodeElements Collection
        {
            get { return GetCollection(); }
        }

        public string Comment
        {
            get
            {
                throw Exceptions.ThrowEFail();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public string DocComment
        {
            get
            {
                return GetDocComment();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public object Parent
        {
            get { return GetParent(); }
        }

        public EnvDTE.TextPoint EndPoint
        {
            get { throw Exceptions.ThrowEFail(); }
        }

        public string FullName
        {
            get { return GetFullName(); }
        }

        public bool IsShared
        {
            get
            {
                return GetIsShared();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public EnvDTE.TextPoint GetEndPoint(EnvDTE.vsCMPart part)
        {
            throw Exceptions.ThrowEFail();
        }

        public EnvDTE.TextPoint GetStartPoint(EnvDTE.vsCMPart part)
        {
            throw Exceptions.ThrowEFail();
        }

        public EnvDTE.vsCMInfoLocation InfoLocation
        {
            get { return EnvDTE.vsCMInfoLocation.vsCMInfoLocationExternal; }
        }

        public virtual bool IsCodeType
        {
            get { return false; }
        }

        public abstract EnvDTE.vsCMElement Kind { get; }

        public string Name
        {
            get
            {
                return GetName();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public EnvDTE.ProjectItem ProjectItem
        {
            get { throw Exceptions.ThrowEFail(); }
        }

        public EnvDTE.TextPoint StartPoint
        {
            get { throw Exceptions.ThrowEFail(); }
        }

        public string ExtenderCATID
        {
            get { throw new NotImplementedException(); }
        }

        protected virtual object GetExtenderNames()
        {
            throw Exceptions.ThrowENotImpl();
        }

        public object ExtenderNames
        {
            get { return GetExtenderNames(); }
        }

        protected virtual object GetExtender(string name)
        {
            throw Exceptions.ThrowENotImpl();
        }

        public object get_Extender(string extenderName)
        {
            return GetExtender(extenderName);
        }

        public string ElementID
        {
            get { throw new NotImplementedException(); }
        }

        public EnvDTE.CodeAttribute AddAttribute(string name, string value, object position)
        {
            throw Exceptions.ThrowEFail();
        }

        public EnvDTE.CodeParameter AddParameter(string name, object type, object position)
        {
            throw Exceptions.ThrowEFail();
        }

        public void RenameSymbol(string newName)
        {
            throw Exceptions.ThrowEFail();
        }

        public void RemoveParameter(object element)
        {
            throw Exceptions.ThrowEFail();
        }

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Required by interface")]
        public string get_Prototype(int flags = 0)
        {
            return CodeModelService.GetPrototype(null, LookupSymbol(), (PrototypeFlags)flags);
        }
    }
}
