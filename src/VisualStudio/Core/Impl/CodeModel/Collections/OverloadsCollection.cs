// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class OverloadsCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            CodeFunction parent)
        {
            var collection = new OverloadsCollection(state, parent);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private OverloadsCollection(
            CodeModelState state,
            CodeFunction parent)
            : base(state, parent)
        {
        }

        private ImmutableArray<EnvDTE.CodeElement> _overloads;

        private CodeFunction ParentElement => (CodeFunction)this.Parent;

        private ImmutableArray<EnvDTE.CodeElement> GetOverloads()
        {
            // Retrieving the overloads is potentially very expensive because it can force multiple FileCodeModels to be instantiated.
            // Here, we cache the result to avoid having to perform these calculations each time GetOverloads() is called.
            // This *could* be an issue because it means that an OverloadsCollection will not necessarily reflect the
            // current state of the user's code. However, because a new OverloadsCollection is created every time the Overloads
            // property is accessed on CodeFunction, consumers would hit this behavior rarely.
            if (_overloads == null)
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

                var solution = this.Workspace.CurrentSolution;

                var overloadsBuilder = ImmutableArray.CreateBuilder<EnvDTE.CodeElement>();
                foreach (var method in symbol.ContainingType.GetMembers(symbol.Name))
                {
                    if (method.Kind != SymbolKind.Method)
                    {
                        continue;
                    }

                    var location = method.Locations.FirstOrDefault(l => l.IsInSource);
                    if (location != null)
                    {
                        var document = solution.GetDocument(location.SourceTree);
                        if (document != null)
                        {
                            var fileCodeModelObject = this.Workspace.GetFileCodeModel(document.Id);
                            if (fileCodeModelObject != null)
                            {
                                var fileCodeModel = ComAggregate.GetManagedObject<FileCodeModel>(fileCodeModelObject);

                                var element = fileCodeModel.CodeElementFromPosition(location.SourceSpan.Start, EnvDTE.vsCMElement.vsCMElementFunction);
                                if (element != null)
                                {
                                    overloadsBuilder.Add(element);
                                }
                            }
                        }
                    }
                }

                _overloads = overloadsBuilder.ToImmutable();
            }

            return _overloads;
        }

        internal override Snapshot CreateSnapshot() => new CodeElementSnapshot(GetOverloads());

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            if (index >= 0 && index < GetOverloads().Length)
            {
                element = GetOverloads()[index];
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

        public override int Count => GetOverloads().Length;
    }
}
