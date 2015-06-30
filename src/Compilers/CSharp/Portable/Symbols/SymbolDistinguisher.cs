﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Some error messages are particularly confusing if multiple placeholders are substituted
    /// with the same string.  For example, "cannot convert from 'Foo' to 'Foo'".  Usually, this
    /// occurs because there are two types in different contexts with the same qualified name.
    /// The solution is to provide additional qualification on each symbol - either a source
    /// location, an assembly path, or an assembly identity.
    /// </summary>
    /// <remarks>
    /// Performs the same function as ErrArgFlags::Unique in the native compiler.
    /// </remarks>
    internal sealed class SymbolDistinguisher
    {
        private readonly Compilation _compilation;
        private readonly Symbol _symbol0;
        private readonly Symbol _symbol1;

        private ImmutableArray<string> _lazyDescriptions;

        public SymbolDistinguisher(Compilation compilation, Symbol symbol0, Symbol symbol1)
        {
            Debug.Assert(symbol0 != symbol1);
            CheckSymbolKind(symbol0);
            CheckSymbolKind(symbol1);

            _compilation = compilation;
            _symbol0 = symbol0;
            _symbol1 = symbol1;
        }

        public IMessageSerializable First
        {
            get { return new Description(this, 0); }
        }

        public IMessageSerializable Second
        {
            get { return new Description(this, 1); }
        }

        [Conditional("DEBUG")]
        private static void CheckSymbolKind(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.TypeParameter:
                    break; // Can sensibly append location.
                case SymbolKind.ArrayType:
                case SymbolKind.PointerType:
                case SymbolKind.Parameter:
                    break; // Can sensibly append location, after unwrapping.
                case SymbolKind.DynamicType:
                    break; // Can't sensibly append location, but it should never be ambiguous.
                case SymbolKind.Namespace:
                case SymbolKind.Alias:
                case SymbolKind.Assembly:
                case SymbolKind.NetModule:
                case SymbolKind.Label:
                case SymbolKind.Local:
                case SymbolKind.RangeVariable:
                case SymbolKind.Preprocessing:
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        private void MakeDescriptions()
        {
            if (!_lazyDescriptions.IsDefault) return;

            string description0 = _symbol0.ToDisplayString();
            string description1 = _symbol1.ToDisplayString();

            if (description0 == description1)
            {
                Symbol unwrappedSymbol0 = UnwrapSymbol(_symbol0);
                Symbol unwrappedSymbol1 = UnwrapSymbol(_symbol1);

                string location0 = GetLocationString(_compilation, unwrappedSymbol0);
                string location1 = GetLocationString(_compilation, unwrappedSymbol1);

                // The locations should not be equal, but they might be if the same
                // SyntaxTree is referenced by two different compilations.
                if (location0 == location1)
                {
                    var containingAssembly0 = unwrappedSymbol0.ContainingAssembly;
                    var containingAssembly1 = unwrappedSymbol1.ContainingAssembly;

                    // May not be the case if there are error types.
                    if ((object)containingAssembly0 != null && (object)containingAssembly1 != null)
                    {
                        location0 = containingAssembly0.Identity.ToString();
                        location1 = containingAssembly1.Identity.ToString();

                        // Even if the friendly locations produced by GetLocationString aren't
                        // distinct, the containing assembly identities should be.
                        Debug.Assert(location0 != location1);
                    }
                }

                if (location0 != null)
                {
                    description0 = $"{description0} [{location0}]";
                }

                if (location1 != null)
                {
                    description1 = $"{description1} [{location1}]";
                }
            }

            Debug.Assert(description0 != description1);

            if (!_lazyDescriptions.IsDefault) return;

            ImmutableInterlocked.InterlockedInitialize(ref _lazyDescriptions, ImmutableArray.Create(description0, description1));
        }

        private static Symbol UnwrapSymbol(Symbol symbol)
        {
            while (true)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Parameter:
                        symbol = ((ParameterSymbol)symbol).Type;
                        continue;
                    case SymbolKind.PointerType:
                        symbol = ((PointerTypeSymbol)symbol).PointedAtType;
                        continue;
                    case SymbolKind.ArrayType:
                        symbol = ((ArrayTypeSymbol)symbol).ElementType;
                        continue;
                    default:
                        return symbol;
                }
            }
        }


        private static string GetLocationString(Compilation compilation, Symbol unwrappedSymbol)
        {
            Debug.Assert((object)unwrappedSymbol == UnwrapSymbol(unwrappedSymbol));

            ImmutableArray<SyntaxReference> syntaxReferences = unwrappedSymbol.DeclaringSyntaxReferences;
            if (syntaxReferences.Length > 0)
            {
                var tree = syntaxReferences[0].SyntaxTree;
                var span = syntaxReferences[0].Span;
                string path = tree.GetDisplayPath(span, (compilation != null) ? compilation.Options.SourceReferenceResolver : null);
                if (!string.IsNullOrEmpty(path))
                {
                    return $"{path}({tree.GetDisplayLineNumber(span)})";
                }
            }

            AssemblySymbol containingAssembly = unwrappedSymbol.ContainingAssembly;
            if ((object)containingAssembly != null)
            {
                if (compilation != null)
                {
                    PortableExecutableReference metadataReference = compilation.GetMetadataReference(containingAssembly) as PortableExecutableReference;
                    if (metadataReference != null)
                    {
                        string path = metadataReference.FilePath;
                        if (!string.IsNullOrEmpty(path))
                        {
                            return path;
                        }
                    }
                }

                return containingAssembly.Identity.ToString();
            }

            Debug.Assert(unwrappedSymbol.Kind == SymbolKind.DynamicType || unwrappedSymbol.Kind == SymbolKind.ErrorType);
            return null;
        }

        private string GetDescription(int index)
        {
            MakeDescriptions();
            return _lazyDescriptions[index];
        }

        private class Description : IMessageSerializable
        {
            private readonly SymbolDistinguisher _distinguisher;
            private readonly int _index;

            public Description(SymbolDistinguisher distinguisher, int index)
            {
                _distinguisher = distinguisher;
                _index = index;
            }

            public override string ToString()
            {
                return _distinguisher.GetDescription(_index);
            }
        }
    }
}
