// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace APISampleUnitTestsCS
{
    [TestClass]
    public class FAQ
    {
        [AttributeUsage(AttributeTargets.Method)]
        private class FAQAttribute : Attribute
        {
            public int Id { get; private set; }

            public FAQAttribute(int id)
            {
                Id = id;
            }
        }

        private MetadataReference mscorlib;

        private MetadataReference Mscorlib
        {
            get
            {
                if (mscorlib == null)
                {
                    mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
                }

                return mscorlib;
            }
        }

        #region Section 1 : Getting Information Questions
        [FAQ(1)]
        [TestMethod]
        public void GetTypeForIdentifier()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    public static void Main()
    {
        var i = 0; i += 1;
    }
}");
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            var model = compilation.GetSemanticModel(tree);

            // Get IdentifierNameSyntax corresponding to the identifier 'var' above.
            IdentifierNameSyntax identifier = tree.GetRoot()
                .DescendantNodes().OfType<IdentifierNameSyntax>()
                .Single(i => i.IsVar);

            // Use GetTypeInfo() to get TypeSymbol corresponding to the identifier 'var' above.
            var type = model.GetTypeInfo(identifier).Type;
            Assert.AreEqual(SpecialType.System_Int32, type.SpecialType);
            Assert.AreEqual("int", type.ToDisplayString());

            // Alternately, use GetSymbolInfo() to get TypeSymbol corresponding to identifier 'var' above.
            type = (ITypeSymbol)model.GetSymbolInfo(identifier).Symbol;
            Assert.AreEqual(SpecialType.System_Int32, type.SpecialType);
            Assert.AreEqual("int", type.ToDisplayString());
        }

        [FAQ(2)]
        [TestMethod]
        public void GetTypeForVariableDeclaration()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    public static void Main()
    {
        var i = 0; i += 1;
    }
}");
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(tree);
            var model = compilation.GetSemanticModel(tree);

            // Get VariableDeclaratorSyntax corresponding to the statement 'var i = ...' above.
            VariableDeclaratorSyntax variableDeclarator = tree.GetRoot()
                .DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .Single();

            // Get TypeSymbol corresponding to 'var i' above.
            var type = ((ILocalSymbol)model.GetDeclaredSymbol(variableDeclarator)).Type;
            Assert.AreEqual(SpecialType.System_Int32, type.SpecialType);
            Assert.AreEqual("int", type.ToDisplayString());
        }

        [FAQ(3)]
        [TestMethod]
        public void GetTypeForExpressions()
        {
            var source = @"
using System;
class Program
{
    public void M(short[] s)
    {
        var d = 1.0;
        Console.WriteLine(s[0] + d);
    }
    public static void Main()
    {
    }
}";
            ProjectId projectId = ProjectId.CreateNewId();
            DocumentId documentId = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "MyProject", "MyProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, Mscorlib)
                .AddDocument(documentId, "MyFile.cs", source);
            var document = solution.GetDocument(documentId);
            var model = (SemanticModel)document.GetSemanticModelAsync().Result;

            // Get BinaryExpressionSyntax corresponding to the expression 's[0] + d' above.
            BinaryExpressionSyntax addExpression = document.GetSyntaxRootAsync().Result
                .DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            // Get TypeSymbol corresponding to expression 's[0] + d' above.
            TypeInfo expressionTypeInfo = model.GetTypeInfo(addExpression);
            var expressionType = expressionTypeInfo.Type;
            Assert.AreEqual(SpecialType.System_Double, expressionType.SpecialType);
            Assert.AreEqual("double", expressionType.ToDisplayString());
            Assert.AreEqual(SpecialType.System_Double, expressionTypeInfo.ConvertedType.SpecialType);

            var conversion = model.GetConversion(addExpression);
            Assert.IsTrue(conversion.IsIdentity);

            // Get IdentifierNameSyntax corresponding to the variable 'd' in expression 's[0] + d' above.
            var identifier = (IdentifierNameSyntax)addExpression.Right;

            // Use GetTypeInfo() to get TypeSymbol corresponding to variable 'd' above.
            TypeInfo variableTypeInfo = model.GetTypeInfo(identifier);
            var variableType = variableTypeInfo.Type;
            Assert.AreEqual(SpecialType.System_Double, variableType.SpecialType);
            Assert.AreEqual("double", variableType.ToDisplayString());
            Assert.AreEqual(SpecialType.System_Double, variableTypeInfo.ConvertedType.SpecialType);

            conversion = model.GetConversion(identifier);
            Assert.IsTrue(conversion.IsIdentity);

            // Alternately, use GetSymbolInfo() to get TypeSymbol corresponding to variable 'd' above.
            variableType = ((ILocalSymbol)model.GetSymbolInfo(identifier).Symbol).Type;
            Assert.AreEqual(SpecialType.System_Double, variableType.SpecialType);
            Assert.AreEqual("double", variableType.ToDisplayString());

            // Get ElementAccessExpressionSyntax corresponding to 's[0]' in expression 's[0] + d' above.
            var elementAccess = (ElementAccessExpressionSyntax)addExpression.Left;

            // Use GetTypeInfo() to get TypeSymbol corresponding to 's[0]' above.
            expressionTypeInfo = model.GetTypeInfo(elementAccess);
            expressionType = expressionTypeInfo.Type;
            Assert.AreEqual(SpecialType.System_Int16, expressionType.SpecialType);
            Assert.AreEqual("short", expressionType.ToDisplayString());
            Assert.AreEqual(SpecialType.System_Double, expressionTypeInfo.ConvertedType.SpecialType);

            conversion = model.GetConversion(elementAccess);
            Assert.IsTrue(conversion.IsImplicit && conversion.IsNumeric);

            // Get IdentifierNameSyntax corresponding to the parameter 's' in expression 's[0] + d' above.
            identifier = (IdentifierNameSyntax)elementAccess.Expression;

            // Use GetTypeInfo() to get TypeSymbol corresponding to parameter 's' above.
            variableTypeInfo = model.GetTypeInfo(identifier);
            variableType = variableTypeInfo.Type;
            Assert.AreEqual("short[]", variableType.ToDisplayString());
            Assert.AreEqual("short[]", variableTypeInfo.ConvertedType.ToDisplayString());

            conversion = model.GetConversion(identifier);
            Assert.IsTrue(conversion.IsIdentity);

            // Alternately, use GetSymbolInfo() to get TypeSymbol corresponding to parameter 's' above.
            variableType = ((IParameterSymbol)model.GetSymbolInfo(identifier).Symbol).Type;
            Assert.AreEqual("short[]", variableType.ToDisplayString());
            Assert.AreEqual(SpecialType.System_Int16, ((IArrayTypeSymbol)variableType).ElementType.SpecialType);
        }

        [FAQ(4)]
        [TestMethod]
        public void GetInScopeSymbols()
        {
            var source = @"
class C
{
}
class Program
{
    private static int i = 0;
    public static void Main()
    {
        int j = 0; j += i;
 
        // What symbols are in scope here?
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            var model = compilation.GetSemanticModel(tree);

            // Get position of the comment above.
            var position = source.IndexOf("//");

            // Get 'all' symbols that are in scope at the above position. 
            var symbols = model.LookupSymbols(position);

            // Note: "Windows" only appears as a symbol at this location in Windows 8.1.
            var results = string.Join("\r\n", symbols.Select(symbol => symbol.ToDisplayString()).Where(s => s != "Windows").OrderBy(s => s));

            Assert.AreEqual(@"C
j
Microsoft
object.~Object()
object.Equals(object)
object.Equals(object, object)
object.GetHashCode()
object.GetType()
object.MemberwiseClone()
object.ReferenceEquals(object, object)
object.ToString()
Program
Program.i
Program.Main()
System", results);

            // Filter results - get everything except instance members.
            symbols = model.LookupStaticMembers(position);

            // Note: "Windows" only appears as a symbol at this location in Windows 8.1.
            results = string.Join("\r\n", symbols.Select(symbol => symbol.ToDisplayString()).Where(s => s != "Windows").OrderBy(s => s));

            Assert.AreEqual(@"C
j
Microsoft
object.Equals(object, object)
object.ReferenceEquals(object, object)
Program
Program.i
Program.Main()
System", results);

            // Filter results by looking at Kind of returned symbols (only get locals and fields).
            results = string.Join("\r\n", symbols
                .Where(symbol => symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Field)
                .Select(symbol => symbol.ToDisplayString()).OrderBy(s => s));

            Assert.AreEqual(@"j
Program.i", results);
        }

        [FAQ(5)]
        [TestMethod]
        public void GetSymbolsForAccessibleMembersOfAType()
        {
            var source = @"
using System;
public class C
{
    internal int InstanceField = 0;
    public int InstanceProperty { get; set; }
    internal void InstanceMethod()
    {
        Console.WriteLine(InstanceField);
    }
    protected void InaccessibleInstanceMethod()
    {
        Console.WriteLine(InstanceProperty);
    }
}
public static class ExtensionMethods
{
    public static void ExtensionMethod(this C s)
    {
    }
}
class Program
{
    static void Main()
    {
        C c = new C();
        c.ToString();
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(tree);
            var model = compilation.GetSemanticModel(tree);

            // Get position of 'c.ToString()' above.
            var position = source.IndexOf("c.ToString()");

            // Get IdentifierNameSyntax corresponding to identifier 'c' above.
            var identifier = (IdentifierNameSyntax)tree.GetRoot().FindToken(position).Parent;

            // Get TypeSymbol corresponding to variable 'c' above.
            var type = model.GetTypeInfo(identifier).Type;

            // Get symbols for 'accessible' members on the above TypeSymbol.
            var symbols = model.LookupSymbols(position, container: type,
                includeReducedExtensionMethods: true);
            var results = string.Join("\r\n", symbols
                .Select(symbol => symbol.ToDisplayString())
                .OrderBy(result => result));

            Assert.AreEqual(@"C.ExtensionMethod()
C.InstanceField
C.InstanceMethod()
C.InstanceProperty
object.Equals(object)
object.Equals(object, object)
object.GetHashCode()
object.GetType()
object.ReferenceEquals(object, object)
object.ToString()", results);
        }

        [FAQ(6)]
        [TestMethod]
        public void FindAllInvocationsOfAMethod()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C1
{
    public void M1() { M2(); }
    public void M2() { }
}
class C2
{
    public void M1() { M2(); new C1().M2(); }
    public void M2() { }
}
class Program
{
    static void Main() { }
}");
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            var model = compilation.GetSemanticModel(tree);

            // Get MethodDeclarationSyntax corresponding to method C1.M2() above.
            MethodDeclarationSyntax methodDeclaration = tree.GetRoot()
                .DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.ValueText == "C1")
                .DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M2");

            // Get MethodSymbol corresponding to method C1.M2() above.
            var method = model.GetDeclaredSymbol(methodDeclaration);

            // Get all InvocationExpressionSyntax in the above code.
            var allInvocations = tree.GetRoot()
                .DescendantNodes().OfType<InvocationExpressionSyntax>();

            // Use GetSymbolInfo() to find invocations of method C1.M2() above.
            var matchingInvocations = allInvocations
                .Where(i => model.GetSymbolInfo(i).Symbol.Equals(method));
            Assert.AreEqual(2, matchingInvocations.Count());
        }

        [FAQ(7)]
        [TestMethod]
        public void FindAllReferencesToAMethodInASolution()
        {
            var source1 = @"
namespace NS
{
    public class C
    {
        public void MethodThatWeAreTryingToFind()
        {
        }
        public void AnotherMethod()
        {
            MethodThatWeAreTryingToFind(); // First Reference.
        }
    }
}";
            var source2 = @"
using NS;
using Alias=NS.C;
class Program
{
    static void Main()
    {
        var c1 = new C();
        c1.MethodThatWeAreTryingToFind(); // Second Reference.
        c1.AnotherMethod();
        var c2 = new Alias();
        c2.MethodThatWeAreTryingToFind(); // Third Reference.
    }
}";
            var project1Id = ProjectId.CreateNewId();
            var project2Id = ProjectId.CreateNewId();
            var document1Id = DocumentId.CreateNewId(project1Id);
            var document2Id = DocumentId.CreateNewId(project2Id);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(project1Id, "Project1", "Project1", LanguageNames.CSharp)
                .AddMetadataReference(project1Id, Mscorlib)
                .AddDocument(document1Id, "File1.cs", source1)
                .WithProjectCompilationOptions(project1Id, 
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddProject(project2Id, "Project2", "Project2", LanguageNames.CSharp)
                .AddMetadataReference(project2Id, Mscorlib)
                .AddProjectReference(project2Id, new ProjectReference(project1Id))
                .AddDocument(document2Id, "File2.cs", source2);
            
            // If you wish to try against a real solution you could use code like
            // var solution = Solution.Load("<Path>");
            // OR var solution = Workspace.LoadSolution("<Path>").CurrentSolution;

            var project1 = solution.GetProject(project1Id);
            var document1 = project1.GetDocument(document1Id);
            
            // Get MethodDeclarationSyntax corresponding to the 'MethodThatWeAreTryingToFind'.
            MethodDeclarationSyntax methodDeclaration = document1.GetSyntaxRootAsync().Result
                .DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Single(m => m.Identifier.ValueText == "MethodThatWeAreTryingToFind");

            // Get MethodSymbol corresponding to the 'MethodThatWeAreTryingToFind'.
            var method = (IMethodSymbol)document1.GetSemanticModelAsync().Result
                .GetDeclaredSymbol(methodDeclaration);

            // Find all references to the 'MethodThatWeAreTryingToFind' in the solution.
            IEnumerable<ReferencedSymbol> methodReferences = SymbolFinder.FindReferencesAsync(method, solution).Result;
            Assert.AreEqual(1, methodReferences.Count());
            ReferencedSymbol methodReference = methodReferences.Single();
            Assert.AreEqual(3, methodReference.Locations.Count());

            var methodDefinition = (IMethodSymbol)methodReference.Definition;
            Assert.AreEqual("MethodThatWeAreTryingToFind", methodDefinition.Name);
            Assert.IsTrue(methodReference.Definition.Locations.Single().IsInSource);
            Assert.AreEqual("File1.cs", methodReference.Definition.Locations.Single().SourceTree.FilePath);

            Assert.IsTrue(methodReference.Locations
                .All(referenceLocation => referenceLocation.Location.IsInSource));
            Assert.AreEqual(1, methodReference.Locations
                .Count(referenceLocation => referenceLocation.Document.Name == "File1.cs"));
            Assert.AreEqual(2, methodReference.Locations
                .Count(referenceLocation => referenceLocation.Document.Name == "File2.cs"));
        }

        [FAQ(8)]
        [TestMethod]
        public void FindAllInvocationsToMethodsFromAParticularNamespace()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
using System.Threading.Tasks;
class Program
{
    static void Main()
    {
        Action a = () => {};
        var t = Task.Factory.StartNew(a);
        t.Wait();
        Console.WriteLine(a.ToString());
 
        a = () =>
        {
            t = new Task(a);
            t.Start();
            t.Wait();
        };
        a();
    }
}");
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(tree);
            var model = compilation.GetSemanticModel(tree);

            // Instantiate MethodInvocationWalker (below) and tell it to find invocations to methods from the System.Threading.Tasks namespace.
            var walker = new MethodInvocationWalker()
            {
                SemanticModel = model,
                Namespace = "System.Threading.Tasks"
            };

            walker.Visit(tree.GetRoot());
            Assert.AreEqual(@"
Line 8: Task.Factory.StartNew(a)
Line 9: t.Wait()
Line 14: new Task(a)
Line 15: t.Start()
Line 16: t.Wait()", walker.Results.ToString());
        }

        // Below SyntaxWalker checks all nodes of type ObjectCreationExpressionSyntax or InvocationExpressionSyntax
        // present under the SyntaxNode being visited to detect invocations to methods from the supplied namespace.
        public class MethodInvocationWalker : CSharpSyntaxWalker
        {
            public SemanticModel SemanticModel { get; set; }
            public string Namespace { get; set; }
            public StringBuilder Results { get; private set; }

            public MethodInvocationWalker()
            {
                Results = new StringBuilder();
            }

            private bool CheckWhetherMethodIsFromNamespace(ExpressionSyntax node)
            {
                var isMatch = false;
                if (SemanticModel != null)
                {
                    var symbolInfo = SemanticModel.GetSymbolInfo(node);

                    string ns = symbolInfo.Symbol.ContainingNamespace.ToDisplayString();
                    if (ns == Namespace)
                    {
                        Results.AppendLine();
                        Results.Append("Line ");
                        Results.Append(SemanticModel.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line);
                        Results.Append(": ");
                        Results.Append(node.ToString());
                        isMatch = true;
                    }
                }

                return isMatch;
            }

            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                CheckWhetherMethodIsFromNamespace(node);
                base.VisitObjectCreationExpression(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                CheckWhetherMethodIsFromNamespace(node);
                base.VisitInvocationExpression(node);
            }
        }

        [FAQ(9)]
        [TestMethod]
        public void GetAllFieldAndMethodSymbolsInACompilation()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
namespace NS1
{
    public class C
    {
        int InstanceField = 0;
        internal void InstanceMethod()
        {
            Console.WriteLine(InstanceField);
        }
    }
}
namespace NS2
{
    static class ExtensionMethods
    {
        public static void ExtensionMethod(this NS1.C s)
        {
        }
    }
}
class Program
{
    static void Main()
    {
        NS1.C c = new NS1.C();
        c.ToString();
    }
}");
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            var results = new StringBuilder();

            // Traverse the symbol tree to find all namespaces, types, methods and fields.
            // foreach (NamespaceSymbol ns in compilation.GetReferencedAssemblySymbol(mscorlib).GlobalNamespace.GetNamespaceMembers())
            foreach (var ns in compilation.Assembly.GlobalNamespace.GetNamespaceMembers())
            {
                results.AppendLine();
                results.Append(ns.Kind);
                results.Append(": ");
                results.Append(ns.Name);
                foreach (var type in ns.GetTypeMembers())
                {
                    results.AppendLine();
                    results.Append("    ");
                    results.Append(type.TypeKind);
                    results.Append(": ");
                    results.Append(type.Name);
                    foreach (var member in type.GetMembers())
                    {
                        results.AppendLine();
                        results.Append("       ");
                        if (member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Method)
                        {
                            results.Append(member.Kind);
                            results.Append(": ");
                            results.Append(member.Name);
                        }
                    }
                }
            }

            Assert.AreEqual(@"
Namespace: NS1
    Class: C
       Field: InstanceField
       Method: InstanceMethod
       Method: .ctor
Namespace: NS2
    Class: ExtensionMethods
       Method: ExtensionMethod", results.ToString());
        }

        [FAQ(10)]
        [TestMethod]
        public void TraverseAllExpressionsInASyntaxTreeUsingAWalker()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
class Program
{
    static void Main()
    {
        var i = 0.0;
        i += 1 + 2L;
    }
}");
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(tree);
            var model = compilation.GetSemanticModel(tree);
            var walker = new ExpressionWalker() { SemanticModel = model };

            walker.Visit(tree.GetRoot());
            Assert.AreEqual(@"
PredefinedTypeSyntax void has type void
IdentifierNameSyntax var has type double
LiteralExpressionSyntax 0.0 has type double
AssignmentExpressionSyntax i += 1 + 2L has type double
IdentifierNameSyntax i has type double
BinaryExpressionSyntax 1 + 2L has type long
LiteralExpressionSyntax 1 has type int
LiteralExpressionSyntax 2L has type long", walker.Results.ToString());
        }

        // Below SyntaxWalker traverses all expressions under the SyntaxNode being visited and lists the types of these expressions.
        public class ExpressionWalker : SyntaxWalker
        {
            public SemanticModel SemanticModel { get; set; }
            public StringBuilder Results { get; private set; }

            public ExpressionWalker()
            {
                Results = new StringBuilder();
            }

            public override void Visit(SyntaxNode node)
            {
                if (node is ExpressionSyntax)
                {
                    var type = SemanticModel.GetTypeInfo((ExpressionSyntax)node).Type;
                    if (type != null)
                    {
                        Results.AppendLine();
                        Results.Append(node.GetType().Name);
                        Results.Append(" ");
                        Results.Append(node.ToString());
                        Results.Append(" has type ");
                        Results.Append(type.ToDisplayString());
                    }
                }

                base.Visit(node);
            }
        }

        [FAQ(11)]
        [TestMethod]
        public void CompareSyntax()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        var i = 0.0;
        i += 1 + 2L;
    }
}";
            var tree1 = SyntaxFactory.ParseSyntaxTree(source);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source);
            SyntaxNode node1 = tree1.GetRoot();
            SyntaxNode node2 = tree2.GetRoot();

            // Compare trees and nodes that are identical.
            Assert.IsTrue(tree1.IsEquivalentTo(tree2));
            Assert.IsTrue(node1.IsEquivalentTo(node2));
            Assert.IsTrue(SyntaxFactory.AreEquivalent(node1, node2, topLevel: false));
            Assert.IsTrue(SyntaxFactory.AreEquivalent(tree1, tree2, topLevel: false));

            // tree3 is identical to tree1 except for a single comment.
            var tree3 = SyntaxFactory.ParseSyntaxTree(@"
using System;
class Program
{
    // Additional comment.
    static void Main()
    {
        var i = 0.0;
        i += 1 + 2L;
    }
}");
            SyntaxNode node3 = tree3.GetRoot();

            // Compare trees and nodes that are identical except for trivia.
            Assert.IsTrue(tree1.IsEquivalentTo(tree3)); // Trivia differences are ignored.
            Assert.IsFalse(node1.IsEquivalentTo(node3)); // Trivia differences are considered.
            Assert.IsTrue(SyntaxFactory.AreEquivalent(node1, node3, topLevel: false)); // Trivia differences are ignored.
            Assert.IsTrue(SyntaxFactory.AreEquivalent(tree1, tree3, topLevel: false)); // Trivia differences are ignored.

            // tree4 is identical to tree1 except for method body contents.
            var tree4 = SyntaxFactory.ParseSyntaxTree(@"using System;
class Program
{
    static void Main()
    {
    }
}");
            SyntaxNode node4 = tree4.GetRoot();

            // Compare trees and nodes that are identical at the top-level.
            Assert.IsTrue(tree1.IsEquivalentTo(tree4, topLevel: true)); // Only top-level nodes are considered.
            Assert.IsFalse(node1.IsEquivalentTo(node4)); // Non-top-level nodes are considered.
            Assert.IsTrue(SyntaxFactory.AreEquivalent(node1, node4, topLevel: true)); // Only top-level nodes are considered.
            Assert.IsTrue(SyntaxFactory.AreEquivalent(tree1, tree4, topLevel: true)); // Only top-level nodes are considered.

            // Tokens and Trivia can also be compared.
            SyntaxToken token1 = node1.DescendantTokens().First();
            SyntaxToken token2 = node2.DescendantTokens().First();
            Assert.IsTrue(token1.IsEquivalentTo(token2));
            SyntaxTrivia trivia1 = node1.DescendantTrivia().First(t => t.Kind() == SyntaxKind.WhitespaceTrivia);
            SyntaxTrivia trivia2 = node2.DescendantTrivia().Last(t => t.Kind() == SyntaxKind.EndOfLineTrivia);
            Assert.IsFalse(trivia1.IsEquivalentTo(trivia2));
        }

        [FAQ(29)]
        [TestMethod]
        public void TraverseAllCommentsInASyntaxTreeUsingAWalker()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
/// <summary>First Comment</summary>
class Program
{
    /* Second Comment */
    static void Main()
    {
        // Third Comment
    }
}");
            var walker = new CommentWalker();
            walker.Visit(tree.GetRoot());

            Assert.AreEqual(@"
/// <summary>First Comment</summary> (Parent Token: ClassKeyword) (Structured)
/* Second Comment */ (Parent Token: StaticKeyword)
// Third Comment (Parent Token: CloseBraceToken)", walker.Results.ToString());
        }

        // Below SyntaxWalker traverses all comments present under the SyntaxNode being visited.
        public class CommentWalker : CSharpSyntaxWalker
        {
            public StringBuilder Results { get; private set; }

            public CommentWalker() :
                base(SyntaxWalkerDepth.StructuredTrivia)
            {
                Results = new StringBuilder();
            }

            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                bool isDocComment = SyntaxFacts.IsDocumentationCommentTrivia(trivia.Kind());
                if (isDocComment ||
                    trivia.Kind() == SyntaxKind.SingleLineCommentTrivia ||
                    trivia.Kind() == SyntaxKind.MultiLineCommentTrivia)
                {
                    Results.AppendLine();
                    Results.Append(trivia.ToFullString().Trim());
                    Results.Append(" (Parent Token: ");
                    Results.Append(trivia.Token.Kind());
                    Results.Append(")");
                    if (isDocComment)
                    {
                        // Trivia for xml documentation comments have additional 'structure'
                        // available under a child DocumentationCommentSyntax.
                        Assert.IsTrue(trivia.HasStructure);
                        var documentationComment =
                            (DocumentationCommentTriviaSyntax)trivia.GetStructure();
                        Assert.IsTrue(documentationComment.ParentTrivia == trivia);
                        Results.Append(" (Structured)");
                    }
                }

                base.VisitTrivia(trivia);
            }
        }

        [FAQ(12)]
        [TestMethod]
        public void CompareSymbols()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
class C
{
}
class Program
{
    public static void Main()
    {
        var c = new C(); Console.WriteLine(c.ToString());
    }
}");
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            var model = compilation.GetSemanticModel(tree);

            // Get VariableDeclaratorSyntax corresponding to the statement 'var c = ...' above.
            VariableDeclaratorSyntax variableDeclarator = tree.GetRoot()
                .DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();

            // Get TypeSymbol corresponding to 'var c' above.
            ITypeSymbol type = ((ILocalSymbol)model.GetDeclaredSymbol(variableDeclarator)).Type;

            ITypeSymbol expectedType = compilation.GetTypeByMetadataName("C");
            Assert.IsTrue(type.Equals(expectedType));
        }

        [FAQ(13)]
        [TestMethod]
        public void TestWhetherANodeIsPartOfATreeOrASemanticModel()
        {
            var source = @"
using System;
class C
{
}
class Program
{
    public static void Main()
    {
        var c = new C(); Console.WriteLine(c.ToString());
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var other = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(tree);
            var model = compilation.GetSemanticModel(tree);

            SyntaxNode nodeFromTree = tree.GetRoot();
            SyntaxToken tokenNotFromTree = SyntaxFactory.Token(SyntaxKind.ClassKeyword);
            SyntaxNode nodeNotFromTree = other.GetRoot();

            Assert.IsTrue(nodeFromTree.SyntaxTree == tree);
            Assert.IsTrue(nodeFromTree.SyntaxTree == model.SyntaxTree);
            Assert.IsFalse(tokenNotFromTree.SyntaxTree == tree);
            Assert.IsFalse(nodeNotFromTree.SyntaxTree == model.SyntaxTree);
            Assert.IsTrue(nodeNotFromTree.SyntaxTree == other);
        }

        [FAQ(14)]
        [TestMethod]
        public void ValueVersusValueTextVersusGetTextForTokens()
        {
            var source = @"
using System;
class Program
{
    public static void Main()
    {
        var @long = 1L; Console.WriteLine(@long);
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);

            // Get token corresponding to identifier '@long' above.
            SyntaxToken token1 = tree.GetRoot().FindToken(source.IndexOf("@long"));

            // Get token corresponding to literal '1L' above.
            SyntaxToken token2 = tree.GetRoot().FindToken(source.IndexOf("1L"));

            Assert.AreEqual("String", token1.Value.GetType().Name);
            Assert.AreEqual("long", token1.Value);
            Assert.AreEqual("long", token1.ValueText);
            Assert.AreEqual("@long", token1.ToString());

            Assert.AreEqual("Int64", token2.Value.GetType().Name);
            Assert.AreEqual(1L, token2.Value);
            Assert.AreEqual("1", token2.ValueText);
            Assert.AreEqual("1L", token2.ToString());
        }

        [FAQ(16)]
        [TestMethod]
        public void GetLineAndColumnInfo()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    public static void Main()
    {
    }
}", path: "MyCodeFile.cs");

            // Get BlockSyntax corresponding to the method block for 'void Main()' above.
            var node = (BlockSyntax)tree.GetRoot().DescendantNodes().Last();

            // Use GetLocation() and GetLineSpan() to get file, line and column info for above BlockSyntax.
            Location location = node.GetLocation();
            FileLinePositionSpan lineSpan = location.GetLineSpan();
            Assert.IsTrue(location.IsInSource);
            Assert.AreEqual("MyCodeFile.cs", lineSpan.Path);
            Assert.AreEqual(4, lineSpan.StartLinePosition.Line);
            Assert.AreEqual(4, lineSpan.StartLinePosition.Character);

            // Alternate way to get file, line and column info from any span.
            location = tree.GetLocation(node.Span);
            lineSpan = location.GetLineSpan();
            Assert.AreEqual("MyCodeFile.cs", lineSpan.Path);
            Assert.AreEqual(4, lineSpan.StartLinePosition.Line);
            Assert.AreEqual(4, lineSpan.StartLinePosition.Character);

            // Yet another way to get file, line and column info from any span.
            lineSpan = tree.GetLineSpan(node.Span);
            Assert.AreEqual("MyCodeFile.cs", lineSpan.Path);
            Assert.AreEqual(5, lineSpan.EndLinePosition.Line);
            Assert.AreEqual(5, lineSpan.EndLinePosition.Character);

            // SyntaxTokens also have GetLocation(). 
            // Use GetLocation() to get the position of the '{' token under the above BlockSyntax.
            SyntaxToken token = node.DescendantTokens().First();
            location = token.GetLocation();
            lineSpan = location.GetLineSpan();
            Assert.AreEqual("MyCodeFile.cs", lineSpan.Path);
            Assert.AreEqual(4, lineSpan.StartLinePosition.Line);
            Assert.AreEqual(4, lineSpan.StartLinePosition.Character);

            // SyntaxTrivia also have GetLocation(). 
            // Use GetLocation() to get the position of the first WhiteSpaceTrivia under the above SyntaxToken.
            SyntaxTrivia trivia = token.LeadingTrivia.First();
            location = trivia.GetLocation();
            lineSpan = location.GetLineSpan();
            Assert.AreEqual("MyCodeFile.cs", lineSpan.Path);
            Assert.AreEqual(4, lineSpan.StartLinePosition.Line);
            Assert.AreEqual(0, lineSpan.StartLinePosition.Character);
        }

        [FAQ(17)]
        [TestMethod]
        public void GetEmptySourceLinesFromASyntaxTree()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    public static void Main()
    {
        
    }
}", path: "MyCodeFile.cs");
            SourceText text = tree.GetText();
            Assert.AreEqual(8, text.Lines.Count);

            // Enumerate empty lines.
            var results = string.Join("\r\n", text.Lines
                .Where(line => string.IsNullOrWhiteSpace(line.ToString()))
                .Select(line => string.Format("Line {0} (Span {1}-{2}) is empty", line.LineNumber, line.Start, line.End)));

            Assert.AreEqual(@"Line 0 (Span 0-0) is empty
Line 5 (Span 58-66) is empty", results);
        }

        [FAQ(18)]
        [TestMethod]
        public void UseSyntaxWalker()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    public static void Main()
    {
#if true
#endif
        var b = true;
        if (b) { }
        if (!b) { }
    }
}
struct S
{
}");
            var walker = new IfStatementIfKeywordAndTypeDeclarationWalker();
            walker.Visit(tree.GetRoot());

            Assert.AreEqual(@"
Visiting ClassDeclarationSyntax (Kind = ClassDeclaration)
Visiting SyntaxToken (Kind = IfKeyword): #if true
Visiting IfStatementSyntax (Kind = IfStatement): if (b) { }
Visiting SyntaxToken (Kind = IfKeyword): if (b) { }
Visiting IfStatementSyntax (Kind = IfStatement): if (!b) { }
Visiting SyntaxToken (Kind = IfKeyword): if (!b) { }
Visiting StructDeclarationSyntax (Kind = StructDeclaration)", walker.Results.ToString());
        }

        // Below SyntaxWalker traverses all IfStatementSyntax, IfKeyworkd and TypeDeclarationSyntax present under the SyntaxNode being visited.
        public class IfStatementIfKeywordAndTypeDeclarationWalker : CSharpSyntaxWalker
        {
            public StringBuilder Results { get; private set; }

            // Turn on visiting of nodes, tokens and trivia present under structured trivia.
            public IfStatementIfKeywordAndTypeDeclarationWalker()
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                Results = new StringBuilder();
            }

            // If you need to visit all SyntaxNodes of a particular (derived) type that appears directly
            // in a syntax tree, you can override the Visit* mehtod corresponding to this type.
            // For example, you can override VisitIfStatement to visit all SyntaxNodes of type IfStatementSyntax.
            public override void VisitIfStatement(IfStatementSyntax node)
            {
                Results.AppendLine();
                Results.Append("Visiting ");
                Results.Append(node.GetType().Name);
                Results.Append(" (Kind = ");
                Results.Append(node.Kind().ToString());
                Results.Append("): ");
                Results.Append(node.ToString());
                base.VisitIfStatement(node);
            }

            // Visits all SyntaxTokens.
            public override void VisitToken(SyntaxToken token)
            {
                // We only care about SyntaxTokens with Kind 'IfKeyword'.
                if (token.Kind() == SyntaxKind.IfKeyword)
                {
                    Results.AppendLine();
                    Results.Append("Visiting ");
                    Results.Append(token.GetType().Name);
                    Results.Append(" (Kind = ");
                    Results.Append(token.Kind().ToString());
                    Results.Append("): ");
                    Results.Append(token.Parent.ToString());
                }

                base.VisitToken(token);
            }

            // Visits all SyntaxNodes.
            public override void Visit(SyntaxNode node)
            {
                // If you need to visit all SyntaxNodes of a particular base type that can never
                // appear directly in a syntax tree then this would be the place to check for that.
                // For example, TypeDeclarationSyntax is a base type for all the type declarations (like 
                // ClassDeclarationSyntax and StructDeclarationSyntax) that can appear in a syntax tree.
                if (node is TypeDeclarationSyntax)
                {
                    Results.AppendLine();
                    Results.Append("Visiting ");
                    Results.Append(node.GetType().Name);
                    Results.Append(" (Kind = ");
                    Results.Append(node.Kind().ToString());
                    Results.Append(")");
                }

                base.Visit(node);
            }
        }

        [FAQ(19)]
        [TestMethod]
        public void GetFullyQualifiedName()
        {
            var source = @"
using System;
using Alias=NS.C<int>;
namespace NS
{
    public class C<T>
    {
        public struct S<U>
        {
        }
    }
}
class Program
{
    public static void Main()
    {
        Alias.S<long> s = new Alias.S<long>(); Console.WriteLine(s.ToString());
    }
}";
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "MyProject", "MyProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, Mscorlib)
                .AddDocument(documentId, "MyFile.cs", source);
            var document = solution.GetDocument(documentId);
            var root = document.GetSyntaxRootAsync().Result;
            var model = (SemanticModel)document.GetSemanticModelAsync().Result;

            // Get StructDeclarationSyntax corresponding to 'struct S' above.
            StructDeclarationSyntax structDeclaration = root.DescendantNodes()
                .OfType<StructDeclarationSyntax>().Single();

            // Get TypeSymbol corresponding to 'struct S' above.
            var structType = model.GetDeclaredSymbol(structDeclaration);

            // Use ToDisplayString() to get fully qualified name.
            Assert.AreEqual("NS.C<T>.S<U>", structType.ToDisplayString());
            Assert.AreEqual("global::NS.C<T>.S<U>", structType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            // Get VariableDeclaratorSyntax corresponding to 'Alias.S<long> s = ...' above.
            VariableDeclaratorSyntax variableDeclarator = root.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>().Single();

            // Get TypeSymbol corresponding to above VariableDeclaratorSyntax.
            var variableType = ((ILocalSymbol)model.GetDeclaredSymbol(variableDeclarator)).Type;

            Assert.IsFalse(variableType.Equals(structType)); // Type of variable is a closed generic type while that of the struct is an open generic type.
            Assert.IsTrue(variableType.OriginalDefinition.Equals(structType)); // OriginalDefinition for a closed generic type points to corresponding open generic type.
            Assert.AreEqual("NS.C<int>.S<long>", variableType.ToDisplayString());
            Assert.AreEqual("global::NS.C<int>.S<long>", variableType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        [FAQ(20)]
        [TestMethod]
        public void OverloadBindingDetermination()
        {
            var source = @"
using System;
class Program
{
    private int Identity(int a)
    {
        return a;
    }

    private char Identity(char a)
    {
        return a;
    }

    static void Main()
    {
        var v1 = Identity(3);
        var v2 = Identity('a');        
        var v3 = Identity(""arg1"")
    };
}
";
            
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CSharpCompilation.Create("MyCompilation", new[] { tree }, new[] { Mscorlib });
            var model = compilation.GetSemanticModel(tree);

            var allInvocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

            // Below, we expect to find that the Method taking an int was selected.
            // We can confidently index into the invocations because we are following the source line-by-line. This is not always a safe practice.
            var intInvocation = allInvocations.ElementAt(0);
            var info = model.GetSymbolInfo(intInvocation);
            Assert.IsNotNull(info.Symbol);
            Assert.AreEqual("Program.Identity(int)", info.Symbol.ToDisplayString());

            // Below, we expect to find that the Method taking a char was selected.
            var charInvocation = allInvocations.ElementAt(1);
            info = model.GetSymbolInfo(charInvocation);
            Assert.IsNotNull(info.Symbol);
            Assert.AreEqual("Program.Identity(char)", info.Symbol.ToDisplayString());

            // Below, we expect to find that no suitable Method was found, and therefore none were selected.
            var stringInvocation = allInvocations.ElementAt(2);
            info = model.GetSymbolInfo(stringInvocation);
            Assert.IsNull(info.Symbol);
            Assert.AreEqual(2, info.CandidateSymbols.Length);
            Assert.AreEqual(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
        }

        [FAQ(21)]
        [TestMethod]
        public void ClassifyConversionFromAnExpressionToATypeSymbol()
        {
            var source = @"
using System;
class Program
{
    static void M()
    {
    }
    static void M(long l)
    {
    }
    static void M(short s)
    {
    }
    static void M(int i)
    {
    }
    static void Main()
    {
        int ii = 0;
        Console.WriteLine(ii);
        short jj = 1;
        Console.WriteLine(jj);
        string ss = string.Empty;
        Console.WriteLine(ss);
 
       // Perform conversion classification here.
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(tree);
            var model = compilation.GetSemanticModel(tree);

            // Get VariableDeclaratorSyntax corresponding to variable 'ii' above.
            var variableDeclarator = (VariableDeclaratorSyntax)tree.GetRoot()
                .FindToken(source.IndexOf("ii")).Parent;

            // Get TypeSymbol corresponding to above VariableDeclaratorSyntax.
            var targetType = ((ILocalSymbol)model.GetDeclaredSymbol(variableDeclarator)).Type;

            // Perform ClassifyConversion for expressions from within the above SyntaxTree.
            var sourceExpression1 = (ExpressionSyntax)tree.GetRoot()
                .FindToken(source.IndexOf("jj)")).Parent;
            Conversion conversion = model.ClassifyConversion(sourceExpression1, targetType);
            Assert.IsTrue(conversion.IsImplicit && conversion.IsNumeric);

            var sourceExpression2 = (ExpressionSyntax)tree.GetRoot()
                .FindToken(source.IndexOf("ss)")).Parent;
            conversion = model.ClassifyConversion(sourceExpression2, targetType);
            Assert.IsFalse(conversion.Exists);

            // Perform ClassifyConversion for constructed expressions
            // at the position identified by the comment '// Perform ...' above.
            ExpressionSyntax sourceExpression3 = SyntaxFactory.IdentifierName("jj");
            var position = source.IndexOf("//");
            conversion = model.ClassifyConversion(position, sourceExpression3, targetType);
            Assert.IsTrue(conversion.IsImplicit && conversion.IsNumeric);

            ExpressionSyntax sourceExpression4 = SyntaxFactory.IdentifierName("ss");
            conversion = model.ClassifyConversion(position, sourceExpression4, targetType);
            Assert.IsFalse(conversion.Exists);

            ExpressionSyntax sourceExpression5 = SyntaxFactory.ParseExpression("100L");
            conversion = model.ClassifyConversion(position, sourceExpression5, targetType);
            Assert.IsTrue(conversion.IsExplicit && conversion.IsNumeric);
        }

        [FAQ(22)]
        [TestMethod]
        public void ClassifyConversionFromOneTypeSymbolToAnother()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    static void Main() { }
}");
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });

            var int32Type = compilation.GetSpecialType(SpecialType.System_Int32);
            var int16Type = compilation.GetSpecialType(SpecialType.System_Int16);
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var int64Type = compilation.GetSpecialType(SpecialType.System_Int64);

            Assert.IsTrue(compilation.ClassifyConversion(int32Type, int32Type).IsIdentity);

            var conversion1 = compilation.ClassifyConversion(int16Type, int32Type);

            Assert.IsTrue(conversion1.IsImplicit && conversion1.IsNumeric);

            Assert.IsFalse(compilation.ClassifyConversion(stringType, int32Type).Exists);

            var conversion2 = compilation.ClassifyConversion(int64Type, int32Type);

            Assert.IsTrue(conversion2.IsExplicit && conversion2.IsNumeric);
        }

        [FAQ(23)]
        [TestMethod]
        public void GetTargetFrameworkVersionForCompilation()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    static void Main() { }
}");
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(tree);

            Version version = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly.Identity.Version;
            Assert.AreEqual(4, version.Major);
        }

        [FAQ(24)]
        [TestMethod]
        public void GetAssemblySymbolsAndSyntaxTreesFromAProject()
        {
            var source = @"
class Program
{
    static void Main()
    {
    }
}";
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "MyProject", "MyProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, Mscorlib)
                .AddDocument(documentId, "MyFile.cs", source);

            // If you wish to try against a real project you could use code like
            // var project = Solution.LoadStandaloneProject("<Path>");
            // OR var project = Workspace.LoadStandaloneProject("<Path>").CurrentSolution.Projects.First();

            var project = solution.Projects.Single();
            var compilation = project.GetCompilationAsync().Result;
            
            // Get AssemblySymbols for above compilation and the assembly (mscorlib) referenced by it.
            IAssemblySymbol compilationAssembly = compilation.Assembly;
            IAssemblySymbol referencedAssembly = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(project.MetadataReferences.Single());

            Assert.IsTrue(compilation.GetTypeByMetadataName("Program").ContainingAssembly.Equals(compilationAssembly));
            Assert.IsTrue(compilation.GetTypeByMetadataName("System.Object").ContainingAssembly.Equals(referencedAssembly));

            SyntaxTree tree = project.Documents.Single().GetSyntaxTreeAsync().Result;
            Assert.AreEqual("MyFile.cs", tree.FilePath);
        }

        [FAQ(25)]
        [TestMethod]
        public void UseSyntaxAnnotations()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
class Program
{
    static void Main()
    {
        int i = 0; Console.WriteLine(i);
    }
}");

            // Tag all tokens that contain the letter 'i'.
            var rewriter = new MyAnnotator();
            SyntaxNode oldRoot = tree.GetRoot();
            SyntaxNode newRoot = rewriter.Visit(oldRoot);

            Assert.IsFalse(oldRoot.ContainsAnnotations);
            Assert.IsTrue(newRoot.ContainsAnnotations);

            // Find all tokens that were tagged with annotations of type MyAnnotation.
            IEnumerable<SyntaxNodeOrToken> annotatedTokens = newRoot.GetAnnotatedNodesAndTokens(MyAnnotation.Kind);
            var results = string.Join("\r\n",
                annotatedTokens.Select(nodeOrToken =>
                {
                    Assert.IsTrue(nodeOrToken.IsToken);
                    var annotation = nodeOrToken.GetAnnotations(MyAnnotation.Kind).Single();
                    return string.Format("{0} (position {1})", nodeOrToken.ToString(), MyAnnotation.GetPosition(annotation));
                }));

            Assert.AreEqual(@"using (position 2)
static (position 4)
void (position 2)
Main (position 2)
int (position 0)
i (position 0)
WriteLine (position 2)
i (position 0)", results);
        }

        // Below CSharpSyntaxRewriter tags all SyntaxTokens that contain the lowercase letter 'i' under the SyntaxNode being visited.
        public class MyAnnotator : CSharpSyntaxRewriter
        {
            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var newToken = base.VisitToken(token);
                var position = token.ToString().IndexOf('i');
                if (position >= 0)
                {
                    newToken = newToken.WithAdditionalAnnotations(MyAnnotation.Create(position));
                }

                return newToken;
            }
        }

        public static class MyAnnotation
        {
            public const string Kind = "MyAnnotation";

            public static SyntaxAnnotation Create(int position)
            {
                return new SyntaxAnnotation(Kind, position.ToString());
            }

            public static int GetPosition(SyntaxAnnotation annotation)
            {
                return int.Parse(annotation.Data);
            }
        }

        [FAQ(37)]
        [TestMethod]
        public void GetBaseTypesAndOverridingRelationships()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
abstract class C1
{
    public virtual int F1(short s) { return 0; }
    public abstract int P1 { get; set; }
}
abstract class C2 : C1
{
    public new virtual int F1(short s) { return 1; }
}
class C3 : C2
{
    public override sealed int F1(short s) { return 2; }
    public override int P1 { get; set; }
}");
            var compilation = CSharpCompilation.Create("MyCompilation",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });

            // Get TypeSymbols for types C1, C2 and C3 above.
            var typeC1 = compilation.GetTypeByMetadataName("C1");
            var typeC2 = compilation.GetTypeByMetadataName("C2");
            var typeC3 = compilation.GetTypeByMetadataName("C3");
            var typeObject = compilation.GetSpecialType(SpecialType.System_Object);

            Assert.IsTrue(typeC1.IsAbstract);
            Assert.IsTrue(typeC2.IsAbstract);
            Assert.IsFalse(typeC3.IsAbstract);

            // Get TypeSymbols for base types of C1, C2 and C3 above.
            Assert.IsTrue(typeC1.BaseType.Equals(typeObject));
            Assert.IsTrue(typeC2.BaseType.Equals(typeC1));
            Assert.IsTrue(typeC3.BaseType.Equals(typeC2));

            // Get MethodSymbols for methods named F1 in types C1, C2 and C3 above.
            var methodC1F1 = (IMethodSymbol)typeC1.GetMembers("F1").Single();
            var methodC2F1 = (IMethodSymbol)typeC2.GetMembers("F1").Single();
            var methodC3F1 = (IMethodSymbol)typeC3.GetMembers("F1").Single();

            // Get overriding relationships between above MethodSymbols.
            Assert.IsTrue(methodC1F1.IsVirtual);
            Assert.IsTrue(methodC2F1.IsVirtual);
            Assert.IsFalse(methodC2F1.IsOverride);
            Assert.IsTrue(methodC3F1.IsOverride);
            Assert.IsTrue(methodC3F1.IsSealed);
            Assert.IsTrue(methodC3F1.OverriddenMethod.Equals(methodC2F1));
            Assert.IsFalse(methodC3F1.OverriddenMethod.Equals(methodC1F1));

            // Get PropertySymbols for properties named P1 in types C1 and C3 above.
            var propertyC1P1 = (IPropertySymbol)typeC1.GetMembers("P1").Single();
            var propertyC3P1 = (IPropertySymbol)typeC3.GetMembers("P1").Single();

            // Get overriding relationships between above PropertySymbols.
            Assert.IsTrue(propertyC1P1.IsAbstract);
            Assert.IsFalse(propertyC1P1.IsVirtual);
            Assert.IsTrue(propertyC3P1.IsOverride);
            Assert.IsTrue(propertyC3P1.OverriddenProperty.Equals(propertyC1P1));
        }

        [FAQ(38)]
        [TestMethod]
        public void GetInterfacesAndImplementationRelationships()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
interface I1
{
    void M1();
    int P1 { get; set; }
}
interface I2 : I1
{
    void M2();
}
class C1 : I1
{
    public void M1() { }
    public virtual int P1 { get; set; }
    public void M2() { }
}
class C2 : C1, I2
{
    new public void M1() { }
}
class C3 : C2, I1
{
    public override int P1 { get; set; }
    int I1.P1 { get; set; }
}");
            var compilation = CSharpCompilation.Create("MyCompilation",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });

            // Get TypeSymbols for types I1, I2, C1, C2 and C3 above.
            var typeI1 = compilation.GetTypeByMetadataName("I1");
            var typeI2 = compilation.GetTypeByMetadataName("I2");
            var typeC1 = compilation.GetTypeByMetadataName("C1");
            var typeC2 = compilation.GetTypeByMetadataName("C2");
            var typeC3 = compilation.GetTypeByMetadataName("C3");

            Assert.IsNull(typeI1.BaseType);
            Assert.IsNull(typeI2.BaseType);
            Assert.AreEqual(0, typeI1.Interfaces.Length);
            Assert.IsTrue(typeI2.Interfaces.Single().Equals(typeI1));

            // Get TypeSymbol for interface implemented by C1 above.
            Assert.IsTrue(typeC1.Interfaces.Single().Equals(typeI1));

            // Get TypeSymbols for interfaces implemented by C2 above.
            Assert.IsTrue(typeC2.Interfaces.Single().Equals(typeI2));
            Assert.AreEqual(2, typeC2.AllInterfaces.Length);
            Assert.IsNotNull(typeC2.AllInterfaces.Single(type => type.Equals(typeI1)));
            Assert.IsNotNull(typeC2.AllInterfaces.Single(type => type.Equals(typeI2)));

            // Get TypeSymbols for interfaces implemented by C3 above.
            Assert.IsTrue(typeC3.Interfaces.Single().Equals(typeI1));
            Assert.AreEqual(2, typeC3.AllInterfaces.Length);
            Assert.IsNotNull(typeC3.AllInterfaces.Single(type => type.Equals(typeI1)));
            Assert.IsNotNull(typeC3.AllInterfaces.Single(type => type.Equals(typeI2)));

            // Get MethodSymbols for methods named M1 and M2 in types I1, I2, C1 and C2 above.
            var methodI1M1 = (IMethodSymbol)typeI1.GetMembers("M1").Single();
            var methodI2M2 = (IMethodSymbol)typeI2.GetMembers("M2").Single();
            var methodC1M1 = (IMethodSymbol)typeC1.GetMembers("M1").Single();
            var methodC1M2 = (IMethodSymbol)typeC1.GetMembers("M2").Single();
            var methodC2M1 = (IMethodSymbol)typeC2.GetMembers("M1").Single();

            // Get interface implementation relationships between above MethodSymbols.
            Assert.IsTrue(typeC1.FindImplementationForInterfaceMember(methodI1M1).Equals(methodC1M1));
            Assert.IsTrue(typeC2.FindImplementationForInterfaceMember(methodI1M1).Equals(methodC2M1));
            Assert.IsTrue(typeC2.FindImplementationForInterfaceMember(methodI2M2).Equals(methodC1M2));
            Assert.IsTrue(typeC3.FindImplementationForInterfaceMember(methodI1M1).Equals(methodC2M1));
            Assert.IsTrue(typeC3.FindImplementationForInterfaceMember(methodI2M2).Equals(methodC1M2));

            // Get PropertySymbols for properties named P1 in types I1, C1 and C3 above.
            var propertyI1P1 = (IPropertySymbol)typeI1.GetMembers("P1").Single();
            var propertyC1P1 = (IPropertySymbol)typeC1.GetMembers("P1").Single();
            var propertyC3P1 = (IPropertySymbol)typeC3.GetMembers("P1").Single();
            var propertyC3I1P1 = (IPropertySymbol)typeC3.GetMembers("I1.P1").Single();

            // Get interface implementation relationships between above PropertySymbols.
            Assert.IsTrue(typeC1.FindImplementationForInterfaceMember(propertyI1P1).Equals(propertyC1P1));
            Assert.IsTrue(typeC2.FindImplementationForInterfaceMember(propertyI1P1).Equals(propertyC1P1));
            Assert.IsTrue(typeC3.FindImplementationForInterfaceMember(propertyI1P1).Equals(propertyC3I1P1));
            Assert.IsFalse(typeC3.FindImplementationForInterfaceMember(propertyI1P1).Equals(propertyC3P1));

            Assert.IsTrue(propertyC3I1P1.ExplicitInterfaceImplementations.Single().Equals(propertyI1P1));
        }

        [FAQ(39)]
        [TestMethod]
        public void GetAppliedAttributes()
        {
            var source = @"
using System;

class Class1
{
    [AttributeUsage(AttributeTargets.Method)]
    private class ExampleAttribute : Attribute
    {
        private readonly int _id;
        
        public int Id
        {
            get
            {
                return _id;
            }
        }

        public ExampleAttribute(int id)
        {
            _id = id;
        }
    }

    static void Method1()
    {
        // Intentionally left blank
    }

    [ExampleAttribute(1)]
    static void Method2()
    {
        // Intentionally left blank
    }

    [ExampleAttribute(2)]
    static void Method3()
    {
        // Intentionally left blank
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CSharpCompilation.Create("MyCompilation",
                                                       options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                       syntaxTrees: new[] { tree },
                                                       references: new[] { Mscorlib });
            var diagnostics = compilation.GetDiagnostics();
            Assert.AreEqual(0, diagnostics.Count());
            var model = compilation.GetSemanticModel(tree);

            Func<string, IMethodSymbol> getMethod = (name) => (from declaration in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                                                               where declaration.Identifier.Text.Equals(name)
                                                               select model.GetDeclaredSymbol(declaration)).Single();
            var attributeSymbol = (from declaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                                   where declaration.Identifier.Text.Equals("ExampleAttribute")
                                   select model.GetDeclaredSymbol(declaration)).Single();

            // Verify that a method has no attributes
            var methodSymbol = getMethod("Method1");
            Assert.AreEqual(0, methodSymbol.GetAttributes().Count());

            // Inspect the attributes that have been given to methods 2 and 3
            methodSymbol = getMethod("Method2");
            var appliedAttribute = methodSymbol.GetAttributes().Single();
            Assert.AreEqual(attributeSymbol, appliedAttribute.AttributeClass);
            Assert.AreEqual(TypedConstantKind.Primitive, appliedAttribute.ConstructorArguments[0].Kind);
            Assert.AreEqual(1, (int)appliedAttribute.ConstructorArguments[0].Value);

            methodSymbol = getMethod("Method3");
            appliedAttribute = methodSymbol.GetAttributes().Single();
            Assert.AreEqual(attributeSymbol, appliedAttribute.AttributeClass);
            Assert.AreEqual(TypedConstantKind.Primitive, appliedAttribute.ConstructorArguments[0].Kind);
            Assert.AreEqual(2, (int)appliedAttribute.ConstructorArguments[0].Value);
        }
        #endregion

        #region Section 2 : Constructing & Updating Tree Questions
        [FAQ(26)]
        [TestMethod]
        public void AddMethodToClass()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
}");
            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            // Get ClassDeclarationSyntax corresponding to 'class C' above.
            ClassDeclarationSyntax classDeclaration = compilationUnit.ChildNodes()
                .OfType<ClassDeclarationSyntax>().Single();

            // Construct a new MethodDeclarationSyntax.
            MethodDeclarationSyntax newMethodDeclaration =
                SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "M")
                    .WithBody(SyntaxFactory.Block());

            // Add this new MethodDeclarationSyntax to the above ClassDeclarationSyntax.
            ClassDeclarationSyntax newClassDeclaration =
                classDeclaration.AddMembers(newMethodDeclaration);

            // Update the CompilationUnitSyntax with the new ClassDeclarationSyntax.
            CompilationUnitSyntax newCompilationUnit =
                compilationUnit.ReplaceNode(classDeclaration, newClassDeclaration);

            // normalize the whitespace
            newCompilationUnit = newCompilationUnit.NormalizeWhitespace("    ");

            Assert.AreEqual(
@"class C
{
    void M()
    {
    }
}", newCompilationUnit.ToFullString());
        }

        [FAQ(27)]
        [TestMethod]
        public void ReplaceSubExpression()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    static void Main()
    {
        int i = 0, j = 0;
        Console.WriteLine((i + j) - (i + j));
    }
}");
            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            // Get BinaryExpressionSyntax corresponding to the two addition expressions 'i + j' above.
            BinaryExpressionSyntax addExpression1 = compilationUnit.DescendantNodes()
                .OfType<BinaryExpressionSyntax>().First(b => b.Kind() == SyntaxKind.AddExpression);
            BinaryExpressionSyntax addExpression2 = compilationUnit.DescendantNodes()
                .OfType<BinaryExpressionSyntax>().Last(b => b.Kind() == SyntaxKind.AddExpression);

            // Replace addition expressions 'i + j' with multiplication expressions 'i * j'.
            BinaryExpressionSyntax multipyExpression1 = SyntaxFactory.BinaryExpression(SyntaxKind.MultiplyExpression,
                addExpression1.Left,
                SyntaxFactory.Token(SyntaxKind.AsteriskToken)
                    .WithLeadingTrivia(addExpression1.OperatorToken.LeadingTrivia)
                    .WithTrailingTrivia(addExpression1.OperatorToken.TrailingTrivia),
                addExpression1.Right);
            BinaryExpressionSyntax multipyExpression2 = SyntaxFactory.BinaryExpression(SyntaxKind.MultiplyExpression,
                addExpression2.Left,
                SyntaxFactory.Token(SyntaxKind.AsteriskToken)
                    .WithLeadingTrivia(addExpression2.OperatorToken.LeadingTrivia)
                    .WithTrailingTrivia(addExpression2.OperatorToken.TrailingTrivia),
                addExpression2.Right);

            CompilationUnitSyntax newCompilationUnit = compilationUnit
                .ReplaceNodes(nodes: new[] { addExpression1, addExpression2 },
                              computeReplacementNode:
                                (originalNode, originalNodeWithReplacedDescendants) =>
                                {
                                    SyntaxNode newNode = null;

                                    if (originalNode == addExpression1)
                                    {
                                        newNode = multipyExpression1;
                                    }
                                    else if (originalNode == addExpression2)
                                    {
                                        newNode = multipyExpression2;
                                    }

                                    return newNode;
                                });

            Assert.AreEqual(@"
class Program
{
    static void Main()
    {
        int i = 0, j = 0;
        Console.WriteLine((i * j) - (i * j));
    }
}", newCompilationUnit.ToFullString());
        }

        [FAQ(28)]
        [TestMethod]
        public void UseSymbolicInformationPlusRewriterToMakeCodeChanges()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
class Program
{
    static void Main()
    {
        C x = new C();
        C.ReferenceEquals(x, x);
    }
}
class C
{
    C y = null;
    public C()
    {
        y = new C();
    }
}");
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(tree);
            var model = compilation.GetSemanticModel(tree);

            // Get the ClassDeclarationSyntax corresponding to 'class C' above.
            ClassDeclarationSyntax classDeclaration = tree.GetRoot()
                .DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Single(c => c.Identifier.ToString() == "C");

            // Get Symbol corresponding to class C above.
            var searchSymbol = model.GetDeclaredSymbol(classDeclaration);
            SyntaxNode oldRoot = tree.GetRoot();
            var rewriter = new ClassRenamer()
            {
                SearchSymbol = searchSymbol,
                SemanticModel = model,
                NewName = "C1"
            };
            SyntaxNode newRoot = rewriter.Visit(oldRoot);

            Assert.AreEqual(@"
using System;
class Program
{
    static void Main()
    {
        C1 x = new C1();
        C1.ReferenceEquals(x, x);
    }
}
class C1
{
    C1 y = null;
    public C1()
    {
        y = new C1();
    }
}", newRoot.ToFullString());
        }

        // Below CSharpSyntaxRewriter renames multiple occurances of a particular class name under the SyntaxNode being visited.
        // Note that the below rewriter is not a full / correct implementation of symbolic rename. For example, it doesn't
        // handle destructors / aliases etc. A full implementation for symbolic rename would be more complicated and is
        // beyond the scope of this sample. The intent of this sample is mainly to demonstrate how symbolic info can be used
        // in conjunction a rewriter to make syntactic changes.
        public class ClassRenamer : CSharpSyntaxRewriter
        {
            public ITypeSymbol SearchSymbol { get; set; }
            public SemanticModel SemanticModel { get; set; }
            public string NewName { get; set; }

            // Replace old ClassDeclarationSyntax with new one.
            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var updatedClassDeclaration = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

                // Get TypeSymbol corresponding to the ClassDeclarationSyntax and check whether
                // it is the same as the TypeSymbol we are searching for.
                var classSymbol = SemanticModel.GetDeclaredSymbol(node);
                if (classSymbol.Equals(SearchSymbol))
                {
                    // Replace the identifier token containing the name of the class.
                    SyntaxToken updatedIdentifierToken =
                        SyntaxFactory.Identifier(
                            updatedClassDeclaration.Identifier.LeadingTrivia,
                            NewName,
                            updatedClassDeclaration.Identifier.TrailingTrivia);

                    updatedClassDeclaration = updatedClassDeclaration.WithIdentifier(updatedIdentifierToken);
                }

                return updatedClassDeclaration;
            }

            // Replace old ConstructorDeclarationSyntax with new one.
            public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                var updatedConstructorDeclaration = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node);

                // Get TypeSymbol corresponding to the containing ClassDeclarationSyntax for the 
                // ConstructorDeclarationSyntax and check whether it is the same as the TypeSymbol
                // we are searching for.
                var classSymbol = (ITypeSymbol)SemanticModel.GetDeclaredSymbol(node).ContainingSymbol;
                if (classSymbol.Equals(SearchSymbol))
                {
                    // Replace the identifier token containing the name of the class.
                    SyntaxToken updatedIdentifierToken =
                        SyntaxFactory.Identifier(
                            updatedConstructorDeclaration.Identifier.LeadingTrivia,
                            NewName,
                            updatedConstructorDeclaration.Identifier.TrailingTrivia);

                    updatedConstructorDeclaration = updatedConstructorDeclaration.WithIdentifier(updatedIdentifierToken);
                }

                return updatedConstructorDeclaration;
            }

            // Replace all occurances of old class name with new one.
            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                var updatedIdentifierName = (IdentifierNameSyntax)base.VisitIdentifierName(node);

                // Get TypeSymbol corresponding to the IdentifierNameSyntax and check whether
                // it is the same as the TypeSymbol we are searching for.
                var identifierSymbol = SemanticModel.GetSymbolInfo(node).Symbol;

                // Handle |C| x = new C().
                var isMatchingTypeName = identifierSymbol.Equals(SearchSymbol);

                // Handle C x = new |C|().
                var isMatchingConstructor =
                    identifierSymbol is IMethodSymbol &&
                    ((IMethodSymbol)identifierSymbol).MethodKind == MethodKind.Constructor &&
                    identifierSymbol.ContainingSymbol.Equals(SearchSymbol);

                if (isMatchingTypeName || isMatchingConstructor)
                {
                    // Replace the identifier token containing the name of the class.
                    SyntaxToken updatedIdentifierToken =
                        SyntaxFactory.Identifier(
                            updatedIdentifierName.Identifier.LeadingTrivia,
                            NewName,
                            updatedIdentifierName.Identifier.TrailingTrivia);

                    updatedIdentifierName = updatedIdentifierName.WithIdentifier(updatedIdentifierToken);
                }

                return updatedIdentifierName;
            }
        }

        [FAQ(30)]
        [TestMethod]
        public void DeleteAssignmentStatementsFromASyntaxTree()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    static void Main()
    {
        int x = 1;
        x = 2;
        if (true)
            x = 3;
        else x = 4;
    }
}");
            SyntaxNode oldRoot = tree.GetRoot();
            var rewriter = new AssignmentStatementRemover();
            SyntaxNode newRoot = rewriter.Visit(oldRoot);

            Assert.AreEqual(@"
class Program
{
    static void Main()
    {
        int x = 1;
        if (true)
            ;
        else ;
    }
}", newRoot.ToFullString());
        }

        // Below CSharpSyntaxRewriter removes multiple assignement statements from under the SyntaxNode being visited.
        public class AssignmentStatementRemover : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                SyntaxNode updatedNode = base.VisitExpressionStatement(node);

                if (node.Expression.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    if (node.Parent.Kind() == SyntaxKind.Block)
                    {
                        // There is a parent block so it is ok to remove the statement completely.
                        updatedNode = null;
                    }
                    else
                    {
                        // The parent context is some statement like an if statement without a block.
                        // Return an empty statement.
                        updatedNode = SyntaxFactory.EmptyStatement()
                            .WithLeadingTrivia(updatedNode.GetLeadingTrivia())
                            .WithTrailingTrivia(updatedNode.GetTrailingTrivia());
                    }
                }

                return updatedNode;
            }
        }

        [FAQ(31)]
        [TestMethod]
        public void ConstructPointerOrArrayType()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    static void Main() { }
}");
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });

            var elementType = compilation.GetSpecialType(SpecialType.System_Int32);

            var pointerType = compilation.CreatePointerTypeSymbol(elementType);
            Assert.AreEqual("int*", pointerType.ToDisplayString());

            var arrayType = compilation.CreateArrayTypeSymbol(elementType, rank: 3);
            Assert.AreEqual("int[*,*,*]", arrayType.ToDisplayString());
        }

        [FAQ(32)]
        [TestMethod]
        public void DeleteRegionsUsingRewriter()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
#region Program
class Program
{
    static void Main()
    {
    }
}
#endregion
#region Other
class C
{
}
#endregion");
            SyntaxNode oldRoot = tree.GetRoot();

            var expected = @"
using System;
class Program
{
    static void Main()
    {
    }
}
class C
{
}
";
            CSharpSyntaxRewriter rewriter = new RegionRemover1();
            SyntaxNode newRoot = rewriter.Visit(oldRoot);
            Assert.AreEqual(expected, newRoot.ToFullString());

            rewriter = new RegionRemover2();
            newRoot = rewriter.Visit(oldRoot);
            Assert.AreEqual(expected, newRoot.ToFullString());
        }

        // Below CSharpSyntaxRewriter removes all #regions and #endregions from under the SyntaxNode being visited.
        public class RegionRemover1 : CSharpSyntaxRewriter
        {
            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                SyntaxTrivia updatedTrivia = base.VisitTrivia(trivia);
                if (trivia.Kind() == SyntaxKind.RegionDirectiveTrivia ||
                    trivia.Kind() == SyntaxKind.EndRegionDirectiveTrivia)
                {
                    // Remove the trivia entirely by returning default(SyntaxTrivia).
                    updatedTrivia = default(SyntaxTrivia);
                }

                return updatedTrivia;
            }
        }

        // Below CSharpSyntaxRewriter removes all #regions and #endregions from under the SyntaxNode being visited.
        public class RegionRemover2 : CSharpSyntaxRewriter
        {
            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                // Remove all #regions and #endregions from underneath the token.
                return token
                 .WithLeadingTrivia(RemoveRegions(token.LeadingTrivia))
                 .WithTrailingTrivia(RemoveRegions(token.TrailingTrivia));
            }

            private SyntaxTriviaList RemoveRegions(SyntaxTriviaList oldTriviaList)
            {
                return SyntaxFactory.TriviaList(oldTriviaList
                    .Where(trivia => trivia.Kind() != SyntaxKind.RegionDirectiveTrivia &&
                                     trivia.Kind() != SyntaxKind.EndRegionDirectiveTrivia));
            }
        }

        [FAQ(33)]
        [TestMethod]
        public void DeleteRegions()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
#region Program
class Program
{
    static void Main()
    {
    }
}
#endregion
#region Other
class C
{
}
#endregion");
            SyntaxNode oldRoot = tree.GetRoot();

            // Get all RegionDirective and EndRegionDirective trivia.
            IEnumerable<SyntaxTrivia> trivia = oldRoot.DescendantTrivia()
                .Where(t => t.Kind() == SyntaxKind.RegionDirectiveTrivia ||
                            t.Kind() == SyntaxKind.EndRegionDirectiveTrivia);

            SyntaxNode newRoot = oldRoot.ReplaceTrivia(trivia: trivia,
                computeReplacementTrivia:
                    (originalTrivia, originalTriviaWithReplacedDescendants) => default(SyntaxTrivia));

            Assert.AreEqual(@"
using System;
class Program
{
    static void Main()
    {
    }
}
class C
{
}
", newRoot.ToFullString());
        }

        [FAQ(34)]
        [TestMethod]
        public void InsertLoggingStatements()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    static void Main()
    {
        System.Console.WriteLine();
        int total = 0;
        for (int i=0; i < 5; ++i)
        {
            total += i;
        }
        if (true) total += 5;
    }
}");
            SyntaxNode oldRoot = tree.GetRoot();
            var rewriter = new ConsoleWriteLineInserter();
            SyntaxNode newRoot = rewriter.Visit(oldRoot);
            newRoot = newRoot.NormalizeWhitespace(); // fix up the whitespace so it is legible.

            var newTree = SyntaxFactory.SyntaxTree(newRoot, path: "MyCodeFile.cs", encoding: Encoding.UTF8);
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(Mscorlib)
                .AddSyntaxTrees(newTree);

            string output = Execute(compilation);
            Assert.AreEqual(@"
0
1
3
6
10
15
", output);
        }

        // Below CSharpSyntaxRewriter inserts a Console.WriteLine() statement to print the value of the
        // LHS variable for compound assignement statements encountered in the input tree.
        public class ConsoleWriteLineInserter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                SyntaxNode updatedNode = base.VisitExpressionStatement(node);

                if (node.Expression.Kind() == SyntaxKind.AddAssignmentExpression ||
                    node.Expression.Kind() == SyntaxKind.SubtractAssignmentExpression ||
                    node.Expression.Kind() == SyntaxKind.MultiplyAssignmentExpression ||
                    node.Expression.Kind() == SyntaxKind.DivideAssignmentExpression)
                {
                    // Print value of the variable on the 'Left' side of
                    // compound assignement statements encountered.
                    var compoundAssignmentExpression = (AssignmentExpressionSyntax)node.Expression;
                    StatementSyntax consoleWriteLineStatement =
                        SyntaxFactory.ParseStatement(string.Format("System.Console.WriteLine({0});", compoundAssignmentExpression.Left.ToString()));

                    updatedNode =
                        SyntaxFactory.Block(SyntaxFactory.List<StatementSyntax>(
                                                new StatementSyntax[]
                                                {
                                                    node.WithLeadingTrivia().WithTrailingTrivia(), // Remove leading and trailing trivia.
                                                    consoleWriteLineStatement
                                                }))
                            .WithLeadingTrivia(node.GetLeadingTrivia())        // Attach leading trivia from original node.
                            .WithTrailingTrivia(node.GetTrailingTrivia());     // Attach trailing trivia from original node.
                }

                return updatedNode;
            }
        }

        // A simple helper to execute the code present inside a compilation.
        public string Execute(Compilation comp)
        {
            var output = new StringBuilder();
            string exeFilename = "OutputCS.exe", pdbFilename = "OutputCS.pdb", xmlCommentsFilename = "OutputCS.xml";
            EmitResult emitResult = null;

            using (var ilStream = new FileStream(exeFilename, FileMode.OpenOrCreate))
            {
                using (var pdbStream = new FileStream(pdbFilename, FileMode.OpenOrCreate))
                {
                    using (var xmlCommentsStream = new FileStream(xmlCommentsFilename, FileMode.OpenOrCreate))
                    {
                        // Emit IL, PDB and xml documentation comments for the compilation to disk.
                        emitResult = comp.Emit(ilStream, pdbStream, xmlCommentsStream);
                    }
                }
            }

            if (emitResult.Success)
            {
                var p = Process.Start(
                    new ProcessStartInfo()
                    {
                        FileName = exeFilename,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    });
                output.Append(p.StandardOutput.ReadToEnd());
                p.WaitForExit();
            }
            else
            {
                output.AppendLine("Errors:");
                foreach (var diag in emitResult.Diagnostics)
                {
                    output.AppendLine(diag.ToString());
                }
            }

            return output.ToString();
        }

        private class SimplifyNamesAnnotionRewriter : CSharpSyntaxRewriter
        {
            private SyntaxNode AnnotateNodeWithSimplifyAnnotation(SyntaxNode node)
            {
                return node.WithAdditionalAnnotations(Simplifier.Annotation);
            }

            public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            {
                // not descending into node to simplify the whole expression
                return AnnotateNodeWithSimplifyAnnotation(node);
            }

            public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            {
                // not descending into node to simplify the whole expression
                return AnnotateNodeWithSimplifyAnnotation(node);
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                // not descending into node to simplify the whole expression
                return AnnotateNodeWithSimplifyAnnotation(node);
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                // not descending into node to simplify the whole expression
                return AnnotateNodeWithSimplifyAnnotation(node);
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                // not descending into node to simplify the whole expression
                return AnnotateNodeWithSimplifyAnnotation(node);
            }
        }

        [FAQ(35)]
        [TestMethod]
        public void UseServices()
        {
            var source = @"using System.Diagnostics;
using System;
using System.IO;
namespace NS
{
public class C
{
}
}
class Program
{
    public static void Main()
    {
        System.Int32 i = 0;                 System.Console.WriteLine(i.ToString());
        Process p = Process.GetCurrentProcess(); 
            Console.WriteLine(p.Id);
    }
}";
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "MyProject", "MyProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, Mscorlib)
                .AddMetadataReference(projectId, AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => string.Compare(a.GetName().Name, "System", StringComparison.OrdinalIgnoreCase) == 0)
                    .Select(a => MetadataReference.CreateFromFile(a.Location)).Single())
                .AddDocument(documentId, "MyFile.cs", source);
            var document = solution.GetDocument(documentId);

            // Format the document.
            document = Formatter.FormatAsync(document).Result;
            Assert.AreEqual(@"using System.Diagnostics;
using System;
using System.IO;
namespace NS
{
    public class C
    {
    }
}
class Program
{
    public static void Main()
    {
        System.Int32 i = 0; System.Console.WriteLine(i.ToString());
        Process p = Process.GetCurrentProcess();
        Console.WriteLine(p.Id);
    }
}", document.GetSyntaxRootAsync().Result.ToString());

            // Simplify names used in the document i.e. remove unnecessary namespace qualifiers.
            var newRoot = (SyntaxNode)document.GetSyntaxRootAsync().Result;
            newRoot = new SimplifyNamesAnnotionRewriter().Visit(newRoot);
            document = document.WithSyntaxRoot(newRoot);

            document = Simplifier.ReduceAsync(document).Result;
            Assert.AreEqual(@"using System.Diagnostics;
using System;
using System.IO;
namespace NS
{
    public class C
    {
    }
}
class Program
{
    public static void Main()
    {
        int i = 0; Console.WriteLine(i.ToString());
        Process p = Process.GetCurrentProcess();
        Console.WriteLine(p.Id);
    }
}", document.GetSyntaxRootAsync().Result.ToString());
        }
        #endregion
    }
}
