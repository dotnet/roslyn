// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class CrefCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public CrefCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new CrefCompletionProvider();
        }

        protected override void VerifyWorker(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyAtPosition(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            VerifyAtEndOfFile(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                VerifyAtPosition_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
                VerifyAtEndOfFile_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NameCref()
        {
            var text = @"using System;
namespace Foo
{
    /// <see cref=""$$""/> 
    class Program
    {
    }
}";
            VerifyItemExists(text, "AccessViolationException");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void QualifiedCref()
        {
            var text = @"using System;
namespace Foo
{

    class Program
    {
        /// <see cref=""Program.$$""/> 
        void foo() { }
    }
}";
            VerifyItemExists(text, "foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CrefArgumentList()
        {
            var text = @"using System;
namespace Foo
{

    class Program
    {
        /// <see cref=""Program.foo($$""/> 
        void foo(int i) { }
    }
}";
            VerifyItemIsAbsent(text, "foo(int)");
            VerifyItemExists(text, "int");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CrefTypeParameterInArgumentList()
        {
            var text = @"using System;
namespace Foo
{

    class Program<T>
    {
        /// <see cref=""Program{Q}.foo($$""/> 
        void foo(T i) { }
    }
}";
            VerifyItemExists(text, "Q");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion), WorkItem(530887)]
        public void PrivateMember()
        {
            var text = @"using System;
namespace Foo
{
    /// <see cref=""C.$$""/> 
    class Program<T>
    {
    }

    class C
    {
        private int Private;
        public int Public;
    }
}";
            VerifyItemExists(text, "Private");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterSingleQuote()
        {
            var text = @"using System;
namespace Foo
{
    /// <see cref='$$'/> 
    class Program
    {
    }
}";
            VerifyItemExists(text, "Exception");
        }

        [WorkItem(531315)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EscapePredefinedTypeName()
        {
            var text = @"using System;
/// <see cref=""@vo$$""/>
class @void { }
";
            VerifyItemExists(text, "@void");
        }

        [WorkItem(531345)]
        [WorkItem(598159)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowParameterNames()
        {
            var text = @"/// <see cref=""C.$$""/>
class C
{
    void M(int x) { }
    void M(ref long x) { }
    void M<T>(T x) { }
}

";
            VerifyItemExists(text, "M(int)");
            VerifyItemExists(text, "M(ref long)");
            VerifyItemExists(text, "M{T}(T)");
        }

        [WorkItem(531345)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowTypeParameterNames()
        {
            var text = @"/// <see cref=""C$$""/>
class C<TFoo>
{
    void M(int x) { }
    void M(long x) { }
    void M(string x) { }
}

";
            VerifyItemExists(text, "C{TFoo}");
        }

        [WorkItem(531156)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowConstructors()
        {
            var text = @"using System;

/// <see cref=""C.$$""/>
class C<T>
{
    public C(int x) { }

    public C() { }

    public C(T x) { }
}

";
            VerifyItemExists(text, "C");
            VerifyItemExists(text, "C(T)");
            VerifyItemExists(text, "C(int)");
        }

        [WorkItem(598679)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoParamsModifier()
        {
            var text = @"/// <summary>
/// <see cref=""C.$$""/>
/// </summary>
class C
        {
            void M(int x) { }
            void M(params long[] x) { }
        }


";
            VerifyItemExists(text, "M(long[])");
        }

        [WorkItem(607773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UnqualifiedTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List{T}.$$""/>
class C { }
";
            VerifyItemExists(text, "Enumerator");
        }

        [WorkItem(607773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitUnqualifiedTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List{T}.$$""/>
class C { }
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""List{T}.Enumerator ""/>
class C { }
";
            VerifyProviderCommit(text, "Enumerator", expected, ' ', "Enum");
        }

        [WorkItem(642285)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestOperators()
        {
            var text = @"
class Test
{
    /// <see cref=""$$""/>
    public static Test operator !(Test t)
    {
        return new Test();
    }
    public static int operator +(Test t1, Test t2) // Invoke FAR here on operator
    {
        return 1;
    }
    public static bool operator true(Test t)
    {
        return true;
    }
    public static bool operator false(Test t)
    {
        return false;
    }
}
";
            VerifyItemExists(text, "operator !(Test)");
            VerifyItemExists(text, "operator +(Test, Test)");
            VerifyItemExists(text, "operator true(Test)");
            VerifyItemExists(text, "operator false(Test)");
        }

        [WorkItem(641096)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestIndexers()
        {
            var text = @"
/// <see cref=""thi$$""/>
class Program
{
    int[] arr;

    public int this[int i]
    {
        get { return arr[i]; }
    }
}
";
            VerifyItemExists(text, "this[int]");
        }

        [WorkItem(531315)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitEscapedPredefinedTypeName()
        {
            var text = @"using System;
/// <see cref=""@vo$$""/>
class @void { }
";

            var expected = @"using System;
/// <see cref=""@void ""/>
class @void { }
";
            VerifyProviderCommit(text, "@void", expected, ' ', "@vo");
        }

        [WorkItem(598159)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void RefOutModifiers()
        {
            var text = @"/// <summary>
/// <see cref=""C.$$""/>
/// </summary>
class C
{
    void M(ref int x) { }
    void M(out long x) { }
}

";
            VerifyItemExists(text, "M(ref int)");
            VerifyItemExists(text, "M(out long)");
        }

        [WorkItem(673587)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedNamespaces()
        {
            var text = @"namespace N
{
    class C
    {
        void sub() { }
    }
    namespace N
    {
        class C
        { }
    }
}
class Program
{
    /// <summary>
    /// <see cref=""N.$$""/> // type N. here
    /// </summary>
    static void Main(string[] args)
    {

    }
}";
            VerifyItemExists(text, "N");
            VerifyItemExists(text, "C");
        }

        [WorkItem(730338)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PermitTypingTypeParameters()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List$$""/>
class C { }
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""List{""/>
class C { }
";
            VerifyProviderCommit(text, "List{T}", expected, '{', "List");
        }

        [WorkItem(730338)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PermitTypingParameterTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""foo$$""/>
class C 
{ 
    public void foo(int x) { }
}
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""foo(""/>
class C 
{ 
    public void foo(int x) { }
}
";
            VerifyProviderCommit(text, "foo(int)", expected, '(', "foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CrefCompletionSpeculatesOutsideTrivia()
        {
            var text = @"
/// <see cref=""$$
class C
{
}";
            var exportProvider = MinimalTestExportProvider.CreateExportProvider(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(PickySemanticFactsService)));
            using (var workspace = TestWorkspaceFactory.CreateWorkspaceFromFiles(LanguageNames.CSharp, new CSharpCompilationOptions(OutputKind.ConsoleApplication), new CSharpParseOptions(), new[] { text }, exportProvider))
            {
                // This test uses MEF to compose in an ISyntaxFactsService that 
                // asserts it isn't asked to speculate on nodes inside documentation trivia.
                // This verifies that the provider is asking for a speculative SemanticModel
                // by walking to the node the documentation is attached to. 

                var provider = new CrefCompletionProvider();
                var hostDocument = workspace.DocumentWithCursor;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var completionList = GetCompletionList(provider, document, hostDocument.CursorPosition.Value, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo());
            }
        }

        [ExportLanguageService(typeof(ISyntaxFactsService), LanguageNames.CSharp, ServiceLayer.Host), System.Composition.Shared]
        internal class PickySemanticFactsService : ISyntaxFactsService
        {
            public bool IsCaseSensitive
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool ContainsInMemberBody(SyntaxNode node, TextSpan span)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode ConvertToSingleLine(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public SyntaxToken FindTokenOnLeftOfPosition(SyntaxNode node, int position, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false)
            {
                throw new NotImplementedException();
            }

            public SyntaxToken FindTokenOnRightOfPosition(SyntaxNode node, int position, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode GetBindableParent(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<SyntaxNode> GetConstructors(SyntaxNode root, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode GetContainingMemberDeclaration(SyntaxNode root, int position, bool useFullSpan = true)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode GetContainingTypeDeclaration(SyntaxNode root, int position)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode GetExpressionOfArgument(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode GetExpressionOfConditionalMemberAccessExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode GetExpressionOfMemberAccessExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public SyntaxToken GetIdentifierOfGenericName(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public TextSpan GetMemberBodySpanForSpeculativeBinding(SyntaxNode node)
            {
                var parent = node.GetAncestor<DocumentationCommentTriviaSyntax>();
                Assert.Null(parent);
                return default(TextSpan);
            }

            public SyntaxNode GetMethodLevelMember(SyntaxNode root, int memberId)
            {
                throw new NotImplementedException();
            }

            public int GetMethodLevelMemberId(SyntaxNode root, SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public List<SyntaxNode> GetMethodLevelMembers(SyntaxNode root)
            {
                throw new NotImplementedException();
            }

            public void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode GetNameOfAttribute(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public RefKind GetRefKindOfArgument(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public string GetText(int kind)
            {
                throw new NotImplementedException();
            }

            public bool HasIncompleteParentMember(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsAnonymousFunction(SyntaxNode n)
            {
                throw new NotImplementedException();
            }

            public bool IsAttribute(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsAttributeName(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsAttributeNamedArgumentIdentifier(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsAwaitKeyword(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsBaseConstructorInitializer(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsBindableToken(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsConditionalMemberAccessExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsContextualKeyword(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsDirective(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsElementAccessExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsEntirelyWithinStringOrCharOrNumericLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public bool IsForEachStatement(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsGenericName(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsGlobalNamespaceKeyword(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsHashToken(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsIdentifier(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsIdentifierEscapeCharacter(char c)
            {
                throw new NotImplementedException();
            }

            public bool IsIdentifierPartCharacter(char c)
            {
                throw new NotImplementedException();
            }

            public bool IsIdentifierStartCharacter(char c)
            {
                throw new NotImplementedException();
            }

            public bool IsInConstantContext(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsInConstructor(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsIndexerMemberCRef(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsInInactiveRegion(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public bool IsInNamespaceOrTypeContext(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsInNonUserCode(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public bool IsInStaticContext(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsInvocationExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsKeyword(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsLiteral(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsLockStatement(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsMemberAccessExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsMemberAccessExpressionName(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsMethodLevelMember(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsNamedParameter(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsObjectCreationExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsObjectCreationExpressionType(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsOperator(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsPointerMemberAccessExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsPredefinedOperator(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op)
            {
                throw new NotImplementedException();
            }

            public bool IsPredefinedType(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsPredefinedType(SyntaxToken token, PredefinedType type)
            {
                throw new NotImplementedException();
            }

            public bool IsPreprocessorKeyword(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsQueryExpression(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsRightSideOfQualifiedName(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsSkippedTokensTrivia(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsStartOfUnicodeEscapeSequence(char c)
            {
                throw new NotImplementedException();
            }

            public bool IsStringLiteral(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsThisConstructorInitializer(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public bool IsTopLevelNodeWithMembers(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsTypeCharacter(char c)
            {
                throw new NotImplementedException();
            }

            public bool IsTypeNamedDynamic(SyntaxToken token, SyntaxNode parent)
            {
                throw new NotImplementedException();
            }

            public bool IsTypeNamedVarInVariableOrFieldDeclaration(SyntaxToken token, SyntaxNode parent)
            {
                throw new NotImplementedException();
            }

            public bool IsUnsafeContext(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsUsingDirectiveName(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsUsingStatement(SyntaxNode node)
            {
                throw new NotImplementedException();
            }

            public bool IsValidIdentifier(string identifier)
            {
                throw new NotImplementedException();
            }

            public bool IsVerbatimIdentifier(string identifier)
            {
                throw new NotImplementedException();
            }

            public bool IsVerbatimIdentifier(SyntaxToken token)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode Parenthesize(SyntaxNode expression, bool includeElasticTrivia = true)
            {
                throw new NotImplementedException();
            }

            public SyntaxToken ToIdentifierToken(string name)
            {
                throw new NotImplementedException();
            }

            public bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace)
            {
                throw new NotImplementedException();
            }

            public bool TryGetDeclaredSymbolInfo(SyntaxNode node, out DeclaredSymbolInfo declaredSymbolInfo)
            {
                throw new NotImplementedException();
            }

            public string GetDisplayName(SyntaxNode node, DisplayNameOptions options, string rootNamespace = null)
            {
                throw new NotImplementedException();
            }

            public bool TryGetExternalSourceInfo(SyntaxNode directive, out ExternalSourceInfo info)
            {
                throw new NotImplementedException();
            }

            public bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op)
            {
                throw new NotImplementedException();
            }

            public bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type)
            {
                throw new NotImplementedException();
            }

            public TextSpan GetInactiveRegionSpanAroundPosition(SyntaxTree tree, int position, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}

