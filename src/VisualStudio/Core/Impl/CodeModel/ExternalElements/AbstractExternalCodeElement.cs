﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            => CodeModelService.GetAccess(LookupSymbol());

        private static bool TryParseDocCommentXml(string text, out XElement xml)
        {
            try
            {
                xml = XElement.Parse(text);
                return true;
            }
            catch (XmlException)
            {
                xml = null;
                return false;
            }
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

            if (!TryParseDocCommentXml(documentationCommentXml, out var xml))
            {
                // If we failed to parse, maybe it was because the XML fragment represents multiple elements.
                // Try surrounding with <doc></doc> and parse again.

                if (!TryParseDocCommentXml($"<doc>{documentationCommentXml}</doc>", out xml))
                {
                    return string.Empty;
                }
            }

            // Surround with <doc> element. Or replace <member> element with <doc>, if it exists.
            if (xml.Name == "member")
            {
                xml.Name = "doc";
                xml.RemoveAttributes();
            }
            else if (xml.Name != "doc")
            {
                xml = new XElement("doc", xml);
            }

            return xml.ToString();
        }

        protected virtual string GetFullName()
            => CodeModelService.GetExternalSymbolFullName(LookupSymbol());

        protected virtual bool GetIsShared()
        {
            var symbol = LookupSymbol();
            return symbol.IsStatic;
        }

        protected virtual string GetName()
            => CodeModelService.GetExternalSymbolName(LookupSymbol());

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
            => Children;

        protected virtual EnvDTE.CodeElements GetCollection()
            => GetCollection<AbstractExternalCodeElement>(this.Parent);

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
            => throw Exceptions.ThrowEFail();

        public EnvDTE.TextPoint GetStartPoint(EnvDTE.vsCMPart part)
            => throw Exceptions.ThrowEFail();

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
            => throw Exceptions.ThrowENotImpl();

        public object ExtenderNames
        {
            get { return GetExtenderNames(); }
        }

        protected virtual object GetExtender(string name)
            => throw Exceptions.ThrowENotImpl();

        public object get_Extender(string extenderName)
            => GetExtender(extenderName);

        public string ElementID
        {
            get { throw new NotImplementedException(); }
        }

#pragma warning disable IDE0060 // Remove unused parameter - Implements interface methods for sub-types.
        public EnvDTE.CodeAttribute AddAttribute(string name, string value, object position)
            => throw Exceptions.ThrowEFail();

        public EnvDTE.CodeParameter AddParameter(string name, object type, object position)
            => throw Exceptions.ThrowEFail();

        public void RenameSymbol(string newName)
            => throw Exceptions.ThrowEFail();

        public void RemoveParameter(object element)
            => throw Exceptions.ThrowEFail();

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Required by interface")]
        public string get_Prototype(int flags = 0)
            => CodeModelService.GetPrototype(null, LookupSymbol(), (PrototypeFlags)flags);
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
