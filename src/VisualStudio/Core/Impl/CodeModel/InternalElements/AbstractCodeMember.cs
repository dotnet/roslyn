// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    public abstract class AbstractCodeMember : AbstractKeyedCodeElement
    {
        internal AbstractCodeMember(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        internal AbstractCodeMember(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        protected SyntaxNode GetContainingTypeNode()
        {
            return LookupNode().Ancestors().Where(CodeModelService.IsType).FirstOrDefault();
        }

        public override object Parent
        {
            get
            {
                var containingTypeNode = GetContainingTypeNode();
                if (containingTypeNode == null)
                {
                    throw Exceptions.ThrowEUnexpected();
                }

                return FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(containingTypeNode);
            }
        }

        public EnvDTE.vsCMAccess Access
        {
            get
            {
                var node = LookupNode();
                return CodeModelService.GetAccess(node);
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateAccess, value);
            }
        }

        public EnvDTE.CodeElements Attributes
        {
            get
            {
                return AttributeCollection.Create(this.State, this);
            }
        }

        public string Comment
        {
            get
            {
                var node = CodeModelService.GetNodeWithModifiers(LookupNode());
                return CodeModelService.GetComment(node);
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateComment, value);
            }
        }

        public string DocComment
        {
            get
            {
                var node = CodeModelService.GetNodeWithModifiers(LookupNode());
                return CodeModelService.GetDocComment(node);
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateDocComment, value);
            }
        }

        public bool IsGeneric
        {
            get
            {
                var node = CodeModelService.GetNodeWithModifiers(LookupNode());
                return CodeModelService.GetIsGeneric(node);
            }
        }

        public bool IsShared
        {
            get
            {
                var node = CodeModelService.GetNodeWithModifiers(LookupNode());
                return CodeModelService.GetIsShared(node, LookupSymbol());
            }

            set
            {
                UpdateNodeAndReacquireNodeKey(FileCodeModel.UpdateIsShared, value);
            }
        }

        public bool MustImplement
        {
            get
            {
                var node = CodeModelService.GetNodeWithModifiers(LookupNode());
                return CodeModelService.GetMustImplement(node);
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateMustImplement, value);
            }
        }

        public EnvDTE80.vsCMOverrideKind OverrideKind
        {
            get
            {
                var node = CodeModelService.GetNodeWithModifiers(LookupNode());
                return CodeModelService.GetOverrideKind(node);
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateOverrideKind, value);
            }
        }

        internal virtual ImmutableArray<SyntaxNode> GetParameters()
        {
            throw Exceptions.ThrowEFail();
        }

        public EnvDTE.CodeElements Parameters
        {
            get { return ParameterCollection.Create(this.State, this); }
        }

        public EnvDTE.CodeParameter AddParameter(string name, object type, object position)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                // The parameters are part of the node key, so we need to update it
                // after adding a parameter.
                var node = LookupNode();
                var nodePath = new SyntaxPath(node);

                var parameter = FileCodeModel.AddParameter(this, node, name, type, position);

                ReacquireNodeKey(nodePath, CancellationToken.None);

                return parameter;
            });
        }

        public void RemoveParameter(object element)
        {
            FileCodeModel.EnsureEditor(() =>
            {
                // The parameters are part of the node key, so we need to update it
                // after removing a parameter.
                var node = LookupNode();
                var nodePath = new SyntaxPath(node);

                var codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(element);

                if (codeElement == null)
                {
                    codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(this.Parameters.Item(element));
                }

                if (codeElement == null)
                {
                    throw new ArgumentException(ServicesVSResources.ElementIsNotValid, nameof(element));
                }

                codeElement.Delete();

                ReacquireNodeKey(nodePath, CancellationToken.None);
            });
        }

        public EnvDTE.CodeAttribute AddAttribute(string name, string value, object position)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddAttribute(LookupNode(), name, value, position);
            });
        }
    }
}
