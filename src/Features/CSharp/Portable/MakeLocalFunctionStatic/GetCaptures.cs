using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;




//Make sure to check for null --> return original solution
namespace Microsoft.CodeAnalysis.CSharp.GetCaptures
{
    internal class GetCaptures : ILanguageService
    {
        private char[] s_underscore;
        private static readonly SyntaxGenerator s_generator = CSharpSyntaxGenerator.Instance; //needed to add CSharp.Extensions and .CodeGenerations, needed for GenerateArguments



        internal async Task<Solution> CreateParameterSymbolAsync(Document document, LocalFunctionStatementSyntax localfunction, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(true);
            if (semanticModel == null)
            {
                return document.Project.Solution;
            }



            var localFunctionSymbol = semanticModel.GetDeclaredSymbol(localfunction, cancellationToken);
            var dataFlow = semanticModel.AnalyzeDataFlow(localfunction);
            var captures = dataFlow.CapturedInside;



            var parameters = ArrayBuilder<IParameterSymbol>.GetInstance();



            //Gets all the variables in the local function and its attributes and puts them in an array
            ImmutableArray<IParameterSymbol> DetermineParameters(ImmutableArray<ISymbol> captures)
            {
                foreach (var symbol in captures)
                {
                    var type = ((ILocalSymbol)symbol).Type;
                    parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default,
                        refKind: RefKind.None,
                        isParams: false,
                        type: type,
                        name: symbol.Name.ToCamelCase().TrimStart(s_underscore)));
                }



                return parameters.ToImmutableAndFree();



            }



            DetermineParameters(captures);




            //Finds all the call sites of the local function
            var workspace = document.Project.Solution.Workspace;
            var arrayNode = await FindReferencesAsync(localFunctionSymbol, document.Project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);




            //Initializes a dictionary to replace the nodes of the call sites to be filled with arguments
            Dictionary<SyntaxNode, SyntaxNode> dict = new Dictionary<SyntaxNode, SyntaxNode>();




            //keep trivia
            foreach (var referenceSymbol in arrayNode)
            {
                foreach (var location in referenceSymbol.Locations)
                {
                    var root = await location.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxNode = root.FindNode(location.Location.SourceSpan); //Node for the identifier syntax




                    var invocation = (syntaxNode as IdentifierNameSyntax).Parent as InvocationExpressionSyntax;
                    if (invocation == null)
                    {
                        return document.Project.Solution;
                    }



                    var arg_List = invocation.ArgumentList;
                    List<ArgumentSyntax> x = new List<ArgumentSyntax>();



                    foreach (var parameter in parameters)
                    {




                        var newArgument = GenerateArgument(parameter, parameter.Name, false);



                        x.Add(newArgument as ArgumentSyntax); //direct cast
                    }



                    var newArgList = arg_List.WithArguments(arg_List.Arguments.AddRange(x));

                    var newInvocation = invocation.WithArgumentList(newArgList);



                    dict.Add(invocation, newInvocation);
                }
            }




            //Updates the declaration with the variables passed in
            var newLC = CodeGenerator.AddParameterDeclarations(localfunction, parameters, workspace);
            dict.Add(localfunction, newLC);
            var syntaxTree = localfunction.SyntaxTree;



            var newRoot = syntaxTree.GetRoot(cancellationToken).ReplaceNodes(dict.Keys, (invocation, _) => dict[invocation]);
            var newDocument = document.WithSyntaxRoot(newRoot);



            return newDocument.Project.Solution;



        }



        internal static Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken = default)
        {
            return FindReferencesAsync(symbol, solution, cancellationToken: cancellationToken);
            //The best overload for "FindReferencesAsync" does not have a parameter named "Progress"
        }



        internal SyntaxNode GenerateArgument(IParameterSymbol p, string name, bool shouldUseNamedArguments = false)
            => s_generator.Argument(shouldUseNamedArguments ? name : null, p.RefKind, name.ToIdentifierName());



    }



}


