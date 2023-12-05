// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedPrimaryConstructor : SourceConstructorSymbolBase
    {
        private IReadOnlyDictionary<ParameterSymbol, FieldSymbol>? _capturedParameters = null;

        public SynthesizedPrimaryConstructor(
             SourceMemberContainerTypeSymbol containingType,
             TypeDeclarationSyntax syntax) :
             base(containingType, syntax.Identifier.GetLocation(), syntax, isIterator: false)
        {
            Debug.Assert(syntax.Kind() is SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration);

            this.MakeFlags(
                MethodKind.Constructor,
                containingType.IsAbstract ? DeclarationModifiers.Protected : DeclarationModifiers.Public,
                returnsVoid: true,
                isExtensionMethod: false,
                isNullableAnalysisEnabled: false); // IsNullableAnalysisEnabled uses containing type instead.
        }

        internal TypeDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (TypeDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override ParameterListSyntax GetParameterList()
        {
            return GetSyntax().ParameterList!;
        }

        protected override CSharpSyntaxNode? GetInitializer()
        {
            return GetSyntax().PrimaryConstructorBaseTypeIfClass;
        }

        public new SourceMemberContainerTypeSymbol ContainingType => (SourceMemberContainerTypeSymbol)base.ContainingType;

        protected override bool AllowRefOrOut => !(ContainingType is { IsRecord: true } or { IsRecordStruct: true });

        internal override bool IsExpressionBodied => false;

        internal override bool IsNullableAnalysisEnabled()
        {
            return ContainingType.IsNullableEnabledForConstructorsAndInitializers(IsStatic);
        }

        protected override bool IsWithinExpressionOrBlockBody(int position, out int offset)
        {
            offset = -1;
            return false;
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            TypeDeclarationSyntax typeDecl = GetSyntax();
            Debug.Assert(typeDecl.ParameterList is not null);
            InMethodBinder result = (binderFactoryOpt ?? this.DeclaringCompilation.GetBinderFactory(typeDecl.SyntaxTree)).GetPrimaryConstructorInMethodBinder(this);
            return new ExecutableCodeBinder(SyntaxNode, this, result.WithAdditionalFlags(ignoreAccessibility ? BinderFlags.IgnoreAccessibility : BinderFlags.None));
        }

        public IEnumerable<FieldSymbol> GetBackingFields()
        {
            IReadOnlyDictionary<ParameterSymbol, FieldSymbol> capturedParameters = GetCapturedParameters();

            if (capturedParameters.Count == 0)
            {
                return SpecializedCollections.EmptyEnumerable<FieldSymbol>();
            }

            return capturedParameters.OrderBy(static pair => pair.Key.Ordinal).Select(static pair => pair.Value);
        }

        public IReadOnlyDictionary<ParameterSymbol, FieldSymbol> GetCapturedParameters()
        {
            if (_capturedParameters != null)
            {
                return _capturedParameters;
            }

            if (ContainingType is { IsRecord: true } or { IsRecordStruct: true } || ParameterCount == 0)
            {
                _capturedParameters = SpecializedCollections.EmptyReadOnlyDictionary<ParameterSymbol, FieldSymbol>();
                return _capturedParameters;
            }

            Interlocked.CompareExchange(ref _capturedParameters, Binder.CapturedParametersFinder.GetCapturedParameters(this), null);
            return _capturedParameters;
        }
    }
}
