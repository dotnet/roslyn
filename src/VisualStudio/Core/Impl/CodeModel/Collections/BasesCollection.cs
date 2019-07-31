// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class BasesCollection : AbstractCodeElementCollection
    {
        private readonly bool _interfaces;

        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            object parent,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            bool interfaces)
        {
            var collection = new BasesCollection(state, parent, fileCodeModel, nodeKey, interfaces);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private readonly ComHandle<EnvDTE.FileCodeModel, FileCodeModel> _fileCodeModel;
        private readonly SyntaxNodeKey _nodeKey;

        private BasesCollection(
            CodeModelState state,
            object parent,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            bool interfaces)
            : base(state, parent)
        {
            Debug.Assert(fileCodeModel != null);

            _fileCodeModel = new ComHandle<EnvDTE.FileCodeModel, FileCodeModel>(fileCodeModel);
            _nodeKey = nodeKey;
            _interfaces = interfaces;
        }

        private FileCodeModel FileCodeModel
        {
            get { return _fileCodeModel.Object; }
        }

        private SyntaxNode LookupNode()
        {
            return FileCodeModel.LookupNode(_nodeKey);
        }

        private ITypeSymbol LookupSymbol()
        {
            var node = LookupNode();
            var semanticModel = FileCodeModel.GetSemanticModel();
            return semanticModel.GetDeclaredSymbol(node) as ITypeSymbol;
        }

        private IEnumerable<ITypeSymbol> GetBaseTypes()
        {
            var symbol = LookupSymbol();
            if (symbol == null)
            {
                throw Exceptions.ThrowEFail();
            }

            if (symbol.TypeKind == TypeKind.Interface || _interfaces)
            {
                return symbol.Interfaces;
            }
            else
            {
                return SpecializedCollections.SingletonEnumerable<ITypeSymbol>(symbol.BaseType);
            }
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var baseTypes = GetBaseTypes();
            if (index < baseTypes.Count())
            {
                var child = baseTypes.ElementAt(index);
                var projectId = FileCodeModel.GetProjectId();
                element = CodeModelService.CreateCodeType(this.State, projectId, child);
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            if (name != null)
            {
                // When searching by name it may or may not be the fully qualified named,
                // but we need the fully qualified name for comparison.
                var node = LookupNode();
                var semanticModel = FileCodeModel.GetSemanticModel();

                name = CodeModelService.GetFullyQualifiedName(name, node.SpanStart, semanticModel);
            }

            foreach (var child in GetBaseTypes())
            {
                var childName = child.GetEscapedFullName();
                if (childName == name)
                {
                    var projectId = FileCodeModel.GetProjectId();
                    element = CodeModelService.CreateCodeType(this.State, projectId, child);
                    return true;
                }
            }

            element = null;
            return false;
        }

        public override int Count
        {
            get
            {
                var symbol = LookupSymbol();
                if (symbol == null)
                {
                    throw Exceptions.ThrowEFail();
                }

                if (symbol.TypeKind == TypeKind.Interface || _interfaces)
                {
                    return symbol.Interfaces.Length;
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}
