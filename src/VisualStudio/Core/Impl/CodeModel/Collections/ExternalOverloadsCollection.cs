// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public class ExternalOverloadsCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            ExternalCodeFunction parent,
            ProjectId projectId)
        {
            var collection = new ExternalOverloadsCollection(state, parent, projectId);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private readonly ProjectId _projectId;

        private ExternalOverloadsCollection(
            CodeModelState state,
            ExternalCodeFunction parent,
            ProjectId projectId)
            : base(state, parent)
        {
            _projectId = projectId;
        }

        private ExternalCodeFunction ParentElement
        {
            get { return (ExternalCodeFunction)Parent; }
        }

        private ImmutableArray<EnvDTE.CodeElement> EnumerateOverloads()
        {
            var symbol = (IMethodSymbol)ParentElement.LookupSymbol();

            // Only methods and constructors can be overloaded.  However, all functions
            // can successfully return a collection of overloaded functions; if not
            // really overloaded, the collection contains just the original function.
            if (symbol.MethodKind != MethodKind.Ordinary &&
                symbol.MethodKind != MethodKind.Constructor)
            {
                return ImmutableArray.Create((EnvDTE.CodeElement)Parent);
            }

            var overloadsBuilder = ArrayBuilder<EnvDTE.CodeElement>.GetInstance();
            foreach (var method in symbol.ContainingType.GetMembers(symbol.Name))
            {
                if (method.Kind != SymbolKind.Method)
                {
                    continue;
                }

                var element = ExternalCodeFunction.Create(this.State, _projectId, (IMethodSymbol)method);
                if (element != null)
                {
                    overloadsBuilder.Add((EnvDTE.CodeElement)element);
                }
            }

            return overloadsBuilder.ToImmutableAndFree();
        }

        public override int Count
        {
            get
            {
                return EnumerateOverloads().Length;
            }
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            if (index >= 0 && index < EnumerateOverloads().Length)
            {
                element = EnumerateOverloads()[index];
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            element = null;
            return false;
        }
    }
}
