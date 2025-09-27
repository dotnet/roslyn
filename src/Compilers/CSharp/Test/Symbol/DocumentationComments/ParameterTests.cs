// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using SymbolExtensions = Microsoft.CodeAnalysis.Test.Utilities.SymbolExtensions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ParameterTests : CSharpTestBase
    {
        #region Basic cases

        [Fact]
        public void ClassTypeParameter()
        {
            var source = @"
/// <typeparam name=""T""/>
/// <typeparamref name=""T""/>
class C<T>
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var typeParameter = type.TypeParameters.Single().ISymbol;

            Assert.Equal(typeParameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(typeParameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)).Symbol);
        }

        [Fact]
        public void MethodParameter()
        {
            var source = @"
class C
{
    /// <param name=""x""/>
    /// <paramref name=""x""/>
    void M(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");
            var parameter = method.Parameters.Single().ISymbol;

            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)).Symbol);
        }

        [Fact]
        public void MethodTypeParameter()
        {
            var source = @"
class C
{
    /// <typeparam name=""T""/>
    /// <typeparamref name=""T""/>
    void M<T>(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");
            var typeParameter = method.TypeParameters.Single().ISymbol;

            Assert.Equal(typeParameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(typeParameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)).Symbol);
        }

        [Fact]
        public void IndexerParameter()
        {
            var source = @"
class C
{
    /// <param name=""x""/>
    /// <paramref name=""x""/>
    int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var indexer = type.Indexers.Single();
            var parameter = indexer.Parameters.Single().ISymbol;

            // NOTE: indexer parameter, not accessor parameter.
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)).Symbol);
        }

        #endregion Basic cases

        #region Accessor value parameter

        [Fact]
        public void PropertyValueParameter()
        {
            var source = @"
class C
{
    /// <param name=""value""/>
    /// <paramref name=""value""/>
    int P { get; set; }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var property = type.GetMember<PropertySymbol>("P");
            var parameter = property.SetMethod.Parameters.Single().ISymbol;

            // NOTE: indexer parameter, not accessor parameter.
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)).Symbol);
        }

        [Fact]
        public void IndexerValueParameter()
        {
            var source = @"
class C
{
    /// <param name=""value""/>
    /// <paramref name=""value""/>
    int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var indexer = type.Indexers.Single();
            var parameter = indexer.SetMethod.Parameters.Last().ISymbol;

            // NOTE: accessor parameter - there is no corresponding indexer parameter.
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)).Symbol);
        }

        [Fact]
        public void CustomEventValueParameter()
        {
            var source = @"
class C
{
    /// <param name=""value""/>
    /// <paramref name=""value""/>
    event System.Action E { add { } remove { } };
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            // As in dev11, this is not supported.
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)));
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)));
        }

        [Fact]
        public void FieldLikeEventValueParameter()
        {
            var source = @"
class C
{
    /// <param name=""value""/>
    /// <paramref name=""value""/>
    event System.Action E;
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            // As in dev11, this is not supported.
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)));
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)));
        }

        [Fact]
        public void ReadonlyPropertyValueParameter()
        {
            var source = @"
class C
{
    /// <param name=""value""/>
    /// <paramref name=""value""/>
    int P { get { return 0; } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            // BREAK: Dev11 supports this, but we don't have a symbol.
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)));
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)));
        }

        [Fact]
        public void ReadonlyIndexerValueParameter()
        {
            var source = @"
class C
{
    /// <param name=""value""/>
    /// <paramref name=""value""/>
    int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            // BREAK: Dev11 supports this, but we don't have a symbol.
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)));
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)));
        }

        #endregion Accessor value parameter

        #region Complex parameter names

        [Fact]
        public void VerbatimKeyword()
        {
            var source = @"
class C
{
    /// <param name=""int""/>
    /// <param name=""@int""/>
    void M(int @int) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");
            var parameter = method.Parameters.Single().ISymbol;

            // NOTE: "@" is neither required nor supported in name attributes.
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)));
        }

        [Fact]
        public void UnicodeEscape()
        {
            var source = @"
class C
{
    /// <param name=""a""/>
    /// <param name=""\u0062""/>
    /// <param name=""\u0063""/>
    void M(int \u0061, int b, int \u0063) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(3, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");
            var parameters = method.Parameters.GetPublicSymbols();

            Assert.Equal(parameters.ElementAt(0), model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(parameters.ElementAt(1), model.GetSymbolInfo(nameSyntaxes.ElementAt(1)).Symbol);
            Assert.Equal(parameters.ElementAt(2), model.GetSymbolInfo(nameSyntaxes.ElementAt(2)).Symbol);
        }

        #endregion Complex parameter names

        #region Ambiguities

        [Fact]
        public void AmbiguousParameter()
        {
            var source = @"
class C
{
    /// <param name=""a""/>
    void M(int a, int a) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntax = GetNameAttributeValues(compilation).Single();

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");
            var parameters = method.Parameters.GetPublicSymbols();

            var info = model.GetSymbolInfo(nameSyntax);
            Assert.Equal(CandidateReason.Ambiguous, info.CandidateReason);
            AssertEx.SetEqual(parameters, info.CandidateSymbols);
        }

        [Fact]
        public void AmbiguousTypeParameter()
        {
            var source = @"
class C
{
    /// <typeparam name=""T""/>
    void M<T, T>() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntax = GetNameAttributeValues(compilation).Single();

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");
            var typeParameters = method.TypeParameters.GetPublicSymbols();

            var info = model.GetSymbolInfo(nameSyntax);
            Assert.Equal(CandidateReason.Ambiguous, info.CandidateReason);
            AssertEx.SetEqual(typeParameters, info.CandidateSymbols);
        }

        [Fact]
        public void AmbiguousParameterAndTypeParameter()
        {
            var source = @"
class C
{
    /// <typeparam name=""T""/>
    /// <param name=""T""/>
    void M<T>(int T) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation);
            Assert.Equal(2, nameSyntaxes.Count());

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");
            var typeParameter = method.TypeParameters.Single().ISymbol;
            var parameter = method.Parameters.Single().ISymbol;

            // No problem because the context determines which are visible.
            Assert.Equal(typeParameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(0)).Symbol);
            Assert.Equal(parameter, model.GetSymbolInfo(nameSyntaxes.ElementAt(1)).Symbol);
        }

        #endregion Ambiguities

        #region Lookup

        [Fact]
        public void ClassLookup()
        {
            var source = @"
/// <param name=""pos1""/>
/// <paramref name=""pos2""/>
/// <typeparam name=""pos3""/>
/// <typeparamref name=""pos4""/>
class C<T>
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            int pos1 = source.IndexOf("pos1", StringComparison.Ordinal);
            int pos2 = source.IndexOf("pos2", StringComparison.Ordinal);
            int pos3 = source.IndexOf("pos3", StringComparison.Ordinal);
            int pos4 = source.IndexOf("pos4", StringComparison.Ordinal);

            AssertEx.SetEqual(model.LookupSymbols(pos1).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos2).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos3).Select(SymbolExtensions.ToTestDisplayString), "T");
            AssertEx.SetEqual(model.LookupSymbols(pos4).Select(SymbolExtensions.ToTestDisplayString), "T");
        }

        [Fact]
        public void MethodLookup()
        {
            var source = @"
class C
{
    /// <param name=""pos1""/>
    /// <paramref name=""pos2""/>
    /// <typeparam name=""pos3""/>
    /// <typeparamref name=""pos4""/>
    void M<T>(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            int pos1 = source.IndexOf("pos1", StringComparison.Ordinal);
            int pos2 = source.IndexOf("pos2", StringComparison.Ordinal);
            int pos3 = source.IndexOf("pos3", StringComparison.Ordinal);
            int pos4 = source.IndexOf("pos4", StringComparison.Ordinal);

            AssertEx.SetEqual(model.LookupSymbols(pos1).Select(SymbolExtensions.ToTestDisplayString), "System.Int32 x");
            AssertEx.SetEqual(model.LookupSymbols(pos2).Select(SymbolExtensions.ToTestDisplayString), "System.Int32 x");
            AssertEx.SetEqual(model.LookupSymbols(pos3).Select(SymbolExtensions.ToTestDisplayString), "T");
            AssertEx.SetEqual(model.LookupSymbols(pos4).Select(SymbolExtensions.ToTestDisplayString), "T");
        }

        [Fact]
        public void PropertyLookup()
        {
            var source = @"
class C
{
    /// <param name=""pos1""/>
    /// <paramref name=""pos2""/>
    /// <typeparam name=""pos3""/>
    /// <typeparamref name=""pos4""/>
    int P { get; set; }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            int pos1 = source.IndexOf("pos1", StringComparison.Ordinal);
            int pos2 = source.IndexOf("pos2", StringComparison.Ordinal);
            int pos3 = source.IndexOf("pos3", StringComparison.Ordinal);
            int pos4 = source.IndexOf("pos4", StringComparison.Ordinal);

            AssertEx.SetEqual(model.LookupSymbols(pos1).Select(SymbolExtensions.ToTestDisplayString), "System.Int32 value");
            AssertEx.SetEqual(model.LookupSymbols(pos2).Select(SymbolExtensions.ToTestDisplayString), "System.Int32 value");
            AssertEx.SetEqual(model.LookupSymbols(pos3).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos4).Select(SymbolExtensions.ToTestDisplayString));
        }

        [Fact]
        public void ReadonlyPropertyLookup()
        {
            var source = @"
class C
{
    /// <param name=""pos1""/>
    /// <paramref name=""pos2""/>
    /// <typeparam name=""pos3""/>
    /// <typeparamref name=""pos4""/>
    int P { get { return 0; } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            int pos1 = source.IndexOf("pos1", StringComparison.Ordinal);
            int pos2 = source.IndexOf("pos2", StringComparison.Ordinal);
            int pos3 = source.IndexOf("pos3", StringComparison.Ordinal);
            int pos4 = source.IndexOf("pos4", StringComparison.Ordinal);

            AssertEx.SetEqual(model.LookupSymbols(pos1).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos2).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos3).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos4).Select(SymbolExtensions.ToTestDisplayString));
        }

        [Fact]
        public void IndexerLookup()
        {
            var source = @"
class C
{
    /// <param name=""pos1""/>
    /// <paramref name=""pos2""/>
    /// <typeparam name=""pos3""/>
    /// <typeparamref name=""pos4""/>
    int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            int pos1 = source.IndexOf("pos1", StringComparison.Ordinal);
            int pos2 = source.IndexOf("pos2", StringComparison.Ordinal);
            int pos3 = source.IndexOf("pos3", StringComparison.Ordinal);
            int pos4 = source.IndexOf("pos4", StringComparison.Ordinal);

            AssertEx.SetEqual(model.LookupSymbols(pos1).Select(SymbolExtensions.ToTestDisplayString), "System.Int32 x", "System.Int32 value");
            AssertEx.SetEqual(model.LookupSymbols(pos2).Select(SymbolExtensions.ToTestDisplayString), "System.Int32 x", "System.Int32 value");
            AssertEx.SetEqual(model.LookupSymbols(pos3).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos4).Select(SymbolExtensions.ToTestDisplayString));
        }

        [Fact]
        public void ReadonlyIndexerLookup()
        {
            var source = @"
class C
{
    /// <param name=""pos1""/>
    /// <paramref name=""pos2""/>
    /// <typeparam name=""pos3""/>
    /// <typeparamref name=""pos4""/>
    int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            int pos1 = source.IndexOf("pos1", StringComparison.Ordinal);
            int pos2 = source.IndexOf("pos2", StringComparison.Ordinal);
            int pos3 = source.IndexOf("pos3", StringComparison.Ordinal);
            int pos4 = source.IndexOf("pos4", StringComparison.Ordinal);

            AssertEx.SetEqual(model.LookupSymbols(pos1).Select(SymbolExtensions.ToTestDisplayString), "System.Int32 x");
            AssertEx.SetEqual(model.LookupSymbols(pos2).Select(SymbolExtensions.ToTestDisplayString), "System.Int32 x");
            AssertEx.SetEqual(model.LookupSymbols(pos3).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos4).Select(SymbolExtensions.ToTestDisplayString));
        }

        [Fact]
        public void CustomEventLookup()
        {
            var source = @"
class C<T>
{
    /// <param name=""pos1""/>
    /// <paramref name=""pos2""/>
    /// <typeparam name=""pos3""/>
    /// <typeparamref name=""pos4""/>
    event System.Action E { add { } remove { } }
}
";
            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            int pos1 = source.IndexOf("pos1", StringComparison.Ordinal);
            int pos2 = source.IndexOf("pos2", StringComparison.Ordinal);
            int pos3 = source.IndexOf("pos3", StringComparison.Ordinal);
            int pos4 = source.IndexOf("pos4", StringComparison.Ordinal);

            // As in Dev11, we do not consider the value parameter.
            AssertEx.SetEqual(model.LookupSymbols(pos1).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos2).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos3).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos4), compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").TypeParameters.Single());
        }

        [Fact]
        public void FieldLikeEventLookup()
        {
            var source = @"
class C<T>
{
    /// <param name=""pos1""/>
    /// <paramref name=""pos2""/>
    /// <typeparam name=""pos3""/>
    /// <typeparamref name=""pos4""/>
    event System.Action E;
}
";
            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            int pos1 = source.IndexOf("pos1", StringComparison.Ordinal);
            int pos2 = source.IndexOf("pos2", StringComparison.Ordinal);
            int pos3 = source.IndexOf("pos3", StringComparison.Ordinal);
            int pos4 = source.IndexOf("pos4", StringComparison.Ordinal);

            // As in Dev11, we do not consider the value parameter.
            AssertEx.SetEqual(model.LookupSymbols(pos1).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos2).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos3).Select(SymbolExtensions.ToTestDisplayString));
            AssertEx.SetEqual(model.LookupSymbols(pos4), compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").TypeParameters.Single());
        }

        #endregion Lookup

        [Fact]
        public void CrefAttributeNameCaseMismatch()
        {
            var source = @"
class C
{
    /// <Param name=""x"">Fine - case of element name doesn't matter.</Param>
    /// <param Name=""y"">Doesn't count - attribute name must be lowercase.</param>
    void M(int x, int y) { }
}
";

            // Element names don't have to be lowercase, but "name" does.
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (6,23): warning CS1573: Parameter 'y' has no matching param tag in the XML comment for 'C.M(int, int)' (but other parameters do)
                //     void M(int x, int y) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "y").WithArguments("y", "C.M(int, int)"));
            Assert.Equal(1, GetNameAttributeValues(compilation).Count());
        }

        [Fact]
        public void ContainingSymbol()
        {
            var source = @"
class C
{
    /// <param name=""x"">Comment.</param>
    void M(int x) { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            var type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");

            int start = source.IndexOf("param", StringComparison.Ordinal);
            int end = source.LastIndexOf("param", StringComparison.Ordinal);
            for (int position = start; position < end; position++)
            {
                Assert.Equal(type, model.GetEnclosingSymbol(position));
            }
        }

        [WorkItem(531161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531161")]
        [Fact]
        public void AttributeNameHasPrefix()
        {
            var source = @"
class Program
{
    /// <param xmlns:name=""Invalid""/>
    void M() { } 
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();
            Assert.Equal(0, GetNameAttributeValues(compilation).Count());
        }

        [WorkItem(531160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531160")]
        [Fact]
        public void DuplicateAttribute()
        {
            var source = @"
class Program
{
    /// <param name=""x"" name=""y""/>
    void M(int x, int y) { } 
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (4,25): warning CS1570: XML comment has badly formed XML -- 'Duplicate 'name' attribute'
                //     /// <param name="x" name="y"/>
                Diagnostic(ErrorCode.WRN_XMLParseError, @"name=""y""").WithArguments("name").WithLocation(4, 25));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var nameSyntaxes = GetNameAttributeValues(compilation).ToArray();

            var method = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetMember<MethodSymbol>("M").GetPublicSymbol();

            Assert.Equal(method.Parameters[0], model.GetSymbolInfo(nameSyntaxes[0]).Symbol);
            Assert.Equal(method.Parameters[1], model.GetSymbolInfo(nameSyntaxes[1]).Symbol);
        }

        [WorkItem(531233, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531233")]
        [Fact]
        public void NameInOtherElement()
        {
            var source = @"
class C
{
    /// <other name=""C""/>
    void M() { }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            Assert.Equal(0, GetNameAttributeValues(compilation).Count());
        }

        [WorkItem(531337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531337")]
        [Fact]
        public void NamesInMethodBody()
        {
            var source = @"
class C
{
    void M<T>(T t)
    {
        /// <param name=""t""/>
        /// <paramref name=""t""/>
        /// <typeparam name=""T""/>
        /// <typeparamref name=""T""/>
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (6,9): warning CS1587: XML comment is not placed on a valid language element
                //         /// <param name=""t""/>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));

            var tree = compilation.SyntaxTrees.Single();
            var names = GetNameAttributeValues(compilation).ToArray();
            var model = compilation.GetSemanticModel(tree);

            Assert.Null(model.GetSymbolInfo(names[0]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[1]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[2]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[3]).Symbol);
        }

        [WorkItem(531337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531337")]
        [Fact]
        public void NamesOnAccessor()
        {
            var source = @"
class C<T>
{
    int this[T t]
    {
        /// <typeparam name=""T""/>
        /// <typeparamref name=""T""/>
        /// <param name=""t""/>
        /// <paramref name=""t""/>
        /// <param name=""value""/>
        /// <paramref name=""value""/>
        get { return 0; }

        /// <typeparam name=""T""/>
        /// <typeparamref name=""T""/>
        /// <param name=""t""/>
        /// <paramref name=""t""/>
        /// <param name=""value""/>
        /// <paramref name=""value""/>
        set { }
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (6,9): warning CS1587: XML comment is not placed on a valid language element
                //         /// <typeparam name="T"/>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (14,9): warning CS1587: XML comment is not placed on a valid language element
                //         /// <typeparam name="T"/>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));

            var tree = compilation.SyntaxTrees.Single();
            var names = GetNameAttributeValues(compilation).ToArray();
            var model = compilation.GetSemanticModel(tree);

            // Getter

            //T
            Assert.Null(model.GetSymbolInfo(names[0]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[1]).Symbol);

            //t
            Assert.Null(model.GetSymbolInfo(names[2]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[3]).Symbol);

            //value
            Assert.Null(model.GetSymbolInfo(names[4]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[5]).Symbol);

            // Setter

            //T
            Assert.Null(model.GetSymbolInfo(names[6]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[7]).Symbol);

            //t
            Assert.Null(model.GetSymbolInfo(names[8]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[9]).Symbol);

            //value
            Assert.Null(model.GetSymbolInfo(names[10]).Symbol);
            Assert.Null(model.GetSymbolInfo(names[11]).Symbol);
        }

        [Fact]
        public void ExtensionMethodsAreNotAvailableInEarlierCSharpVersions()
        {
            var code = @"
 public static class Test
 {
     public static void DoSomething(this int x) { }
 }";

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp2)).VerifyDiagnostics(
                // (4,37): error CS8023: Feature 'extension method' is not available in C# 2. Please use language version 3 or greater.
                //      public static void DoSomething(this int x) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "this").WithArguments("extension method", "3").WithLocation(4, 37));

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.Latest)).VerifyDiagnostics();
        }

        private static IEnumerable<IdentifierNameSyntax> GetNameAttributeValues(CSharpCompilation compilation)
        {
            return compilation.SyntaxTrees.SelectMany(tree =>
            {
                var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
                return docComments.SelectMany(docComment => docComment.DescendantNodes().OfType<XmlNameAttributeSyntax>().Select(attr => attr.Identifier));
            });
        }
    }
}
