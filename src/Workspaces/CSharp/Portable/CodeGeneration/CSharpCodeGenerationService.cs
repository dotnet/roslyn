// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal partial class CSharpCodeGenerationService : AbstractCodeGenerationService<CSharpCodeGenerationOptions>
    {
        public CSharpCodeGenerationService(HostLanguageServices languageServices)
            : base(languageServices.GetRequiredService<ISymbolDeclarationService>())
        {
        }

        public override CodeGenerationPreferences GetPreferences(ParseOptions parseOptions, OptionSet documentOptions)
            => new CSharpCodeGenerationPreferences((CSharpParseOptions)parseOptions, documentOptions);

        public override CodeGenerationDestination GetDestination(SyntaxNode node)
            => CSharpCodeGenerationHelpers.GetDestination(node);

        protected override IComparer<SyntaxNode> GetMemberComparer()
            => CSharpDeclarationComparer.WithoutNamesInstance;

        protected override IList<bool>? GetAvailableInsertionIndices(SyntaxNode destination, CancellationToken cancellationToken)
        {
            if (destination is TypeDeclarationSyntax typeDeclaration)
            {
                return GetInsertionIndices(typeDeclaration, cancellationToken);
            }

            // TODO(cyrusn): This will make is so that we can't generate into an enum, namespace, or
            // compilation unit, if it overlaps a hidden region.  We can consider relaxing that
            // restriction in the future.
            return null;
        }

        private static IList<bool> GetInsertionIndices(TypeDeclarationSyntax destination, CancellationToken cancellationToken)
            => destination.GetInsertionIndices(cancellationToken);

        public override async Task<Document> AddEventAsync(
            Solution solution, INamedTypeSymbol destination, IEventSymbol @event,
            CodeGenerationContext context, CancellationToken cancellationToken)
        {
            var newDocument = await base.AddEventAsync(
                solution, destination, @event, context, cancellationToken).ConfigureAwait(false);

            var namedType = @event.Type as INamedTypeSymbol;
            if (namedType?.AssociatedSymbol != null)
            {
                // This is a VB event that declares its own type.  i.e. "Public Event E(x As Object)"
                // We also have to generate "public void delegate EEventHandler(object x)"
                var compilation = await newDocument.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var newDestinationSymbol = destination.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol;

                if (newDestinationSymbol?.ContainingType != null)
                {
                    return await this.AddNamedTypeAsync(
                        newDocument.Project.Solution, newDestinationSymbol.ContainingType,
                        namedType, context, cancellationToken).ConfigureAwait(false);
                }
                else if (newDestinationSymbol?.ContainingNamespace != null)
                {
                    return await this.AddNamedTypeAsync(
                        newDocument.Project.Solution, newDestinationSymbol.ContainingNamespace,
                        namedType, context, cancellationToken).ConfigureAwait(false);
                }
            }

            return newDocument;
        }

        protected override TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CSharpCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken)
        {
            CheckDeclarationNode<TypeDeclarationSyntax>(destination);

            return Cast<TDeclarationNode>(EventGenerator.AddEventTo(Cast<TypeDeclarationSyntax>(destination), @event, options, availableIndices, cancellationToken));
        }

        protected override TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CSharpCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken)
        {
            CheckDeclarationNode<EnumDeclarationSyntax, TypeDeclarationSyntax, CompilationUnitSyntax>(destination);

            if (destination is EnumDeclarationSyntax)
            {
                return Cast<TDeclarationNode>(EnumMemberGenerator.AddEnumMemberTo(Cast<EnumDeclarationSyntax>(destination), field, options, cancellationToken));
            }
            else if (destination is TypeDeclarationSyntax)
            {
                return Cast<TDeclarationNode>(FieldGenerator.AddFieldTo(Cast<TypeDeclarationSyntax>(destination), field, options, availableIndices, cancellationToken));
            }
            else
            {
                return Cast<TDeclarationNode>(FieldGenerator.AddFieldTo(Cast<CompilationUnitSyntax>(destination), field, options, availableIndices, cancellationToken));
            }
        }

        protected override TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CSharpCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken)
        {
            // https://github.com/dotnet/roslyn/issues/44425: Add handling for top level statements
            if (destination is GlobalStatementSyntax)
            {
                return destination;
            }

            CheckDeclarationNode<TypeDeclarationSyntax, CompilationUnitSyntax, BaseNamespaceDeclarationSyntax>(destination);

            // Synthesized methods for properties/events are not things we actually generate 
            // declarations for.
            if (method.AssociatedSymbol is IEventSymbol)
            {
                return destination;
            }
            // we will ignore the method if the associated property can be generated.

            if (method.AssociatedSymbol is IPropertySymbol property)
            {
                if (PropertyGenerator.CanBeGenerated(property))
                {
                    return destination;
                }
            }

            var csharpOptions = options;

            if (destination is TypeDeclarationSyntax typeDeclaration)
            {
                if (method.IsConstructor())
                {
                    return Cast<TDeclarationNode>(ConstructorGenerator.AddConstructorTo(
                        typeDeclaration, method, csharpOptions, availableIndices, cancellationToken));
                }

                if (method.IsDestructor())
                {
                    return Cast<TDeclarationNode>(DestructorGenerator.AddDestructorTo(typeDeclaration, method, csharpOptions, availableIndices, cancellationToken));
                }

                if (method.MethodKind == MethodKind.Conversion)
                {
                    return Cast<TDeclarationNode>(ConversionGenerator.AddConversionTo(
                        typeDeclaration, method, csharpOptions, availableIndices, cancellationToken));
                }

                if (method.MethodKind == MethodKind.UserDefinedOperator)
                {
                    return Cast<TDeclarationNode>(OperatorGenerator.AddOperatorTo(
                        typeDeclaration, method, csharpOptions, availableIndices, cancellationToken));
                }

                return Cast<TDeclarationNode>(MethodGenerator.AddMethodTo(
                    typeDeclaration, method, csharpOptions, availableIndices, cancellationToken));
            }

            if (method.IsConstructor() ||
                method.IsDestructor())
            {
                return destination;
            }

            if (destination is CompilationUnitSyntax compilationUnit)
            {
                return Cast<TDeclarationNode>(
                    MethodGenerator.AddMethodTo(compilationUnit, method, csharpOptions, availableIndices, cancellationToken));
            }

            var ns = Cast<BaseNamespaceDeclarationSyntax>(destination);
            return Cast<TDeclarationNode>(
                MethodGenerator.AddMethodTo(ns, method, csharpOptions, availableIndices, cancellationToken));
        }

        protected override TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CSharpCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken)
        {
            CheckDeclarationNode<TypeDeclarationSyntax, CompilationUnitSyntax>(destination);

            // Can't generate a property with parameters.  So generate the setter/getter individually.
            if (!PropertyGenerator.CanBeGenerated(property))
            {
                var members = new List<ISymbol>();
                if (property.GetMethod != null)
                {
                    var getMethod = property.GetMethod;

                    if (property is CodeGenerationSymbol codeGenSymbol)
                    {
                        foreach (var annotation in codeGenSymbol.GetAnnotations())
                        {
                            getMethod = annotation.AddAnnotationToSymbol(getMethod);
                        }
                    }

                    members.Add(getMethod);
                }

                if (property.SetMethod != null)
                {
                    var setMethod = property.SetMethod;

                    if (property is CodeGenerationSymbol codeGenSymbol)
                    {
                        foreach (var annotation in codeGenSymbol.GetAnnotations())
                        {
                            setMethod = annotation.AddAnnotationToSymbol(setMethod);
                        }
                    }

                    members.Add(setMethod);
                }

                if (members.Count > 1)
                {
                    options = CreateOptionsForMultipleMembers(options);
                }

                return AddMembers(destination, members, availableIndices, options, cancellationToken);
            }

            if (destination is TypeDeclarationSyntax)
            {
                return Cast<TDeclarationNode>(PropertyGenerator.AddPropertyTo(
                    Cast<TypeDeclarationSyntax>(destination), property, options, availableIndices, cancellationToken));
            }
            else
            {
                return Cast<TDeclarationNode>(PropertyGenerator.AddPropertyTo(
                    Cast<CompilationUnitSyntax>(destination), property, options, availableIndices, cancellationToken));
            }
        }

        protected override TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CSharpCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken)
        {
            CheckDeclarationNode<TypeDeclarationSyntax, BaseNamespaceDeclarationSyntax, CompilationUnitSyntax>(destination);

            var csharpOptions = options;

            if (destination is TypeDeclarationSyntax typeDeclaration)
            {
                return Cast<TDeclarationNode>(NamedTypeGenerator.AddNamedTypeTo(this, typeDeclaration, namedType, csharpOptions, availableIndices, cancellationToken));
            }
            else if (destination is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                return Cast<TDeclarationNode>(NamedTypeGenerator.AddNamedTypeTo(this, namespaceDeclaration, namedType, csharpOptions, availableIndices, cancellationToken));
            }
            else
            {
                return Cast<TDeclarationNode>(NamedTypeGenerator.AddNamedTypeTo(this, Cast<CompilationUnitSyntax>(destination), namedType, csharpOptions, availableIndices, cancellationToken));
            }
        }

        protected override TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CSharpCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken)
        {
            CheckDeclarationNode<CompilationUnitSyntax, BaseNamespaceDeclarationSyntax>(destination);

            if (destination is CompilationUnitSyntax compilationUnit)
            {
                return Cast<TDeclarationNode>(NamespaceGenerator.AddNamespaceTo(this, compilationUnit, @namespace, options, availableIndices, cancellationToken));
            }
            else
            {
                return Cast<TDeclarationNode>(NamespaceGenerator.AddNamespaceTo(this, Cast<BaseNamespaceDeclarationSyntax>(destination), @namespace, options, availableIndices, cancellationToken));
            }
        }

        public override TDeclarationNode AddParameters<TDeclarationNode>(
            TDeclarationNode destination,
            IEnumerable<IParameterSymbol> parameters,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var currentParameterList = destination.GetParameterList();

            var parameterCount = currentParameterList != null ? currentParameterList.Parameters.Count : 0;
            var seenOptional = currentParameterList != null && parameterCount > 0 && currentParameterList.Parameters[^1].Default != null;
            var isFirstParam = parameterCount == 0;

            var editor = new SyntaxEditor(destination, CSharpSyntaxGenerator.Instance);
            foreach (var parameter in parameters)
            {
                var parameterSyntax = ParameterGenerator.GetParameter(parameter, options, isExplicit: false, isFirstParam: isFirstParam, seenOptional: seenOptional);

                AddParameterEditor.AddParameter(
                    CSharpSyntaxFacts.Instance,
                    editor,
                    destination,
                    parameterCount,
                    parameterSyntax,
                    cancellationToken);

                parameterCount++;
                isFirstParam = false;
                seenOptional = seenOptional || parameterSyntax.Default != null;
            }

            var finalMember = editor.GetChangedRoot();

            return Cast<TDeclarationNode>(finalMember);
        }

        public override TDeclarationNode AddAttributes<TDeclarationNode>(
            TDeclarationNode destination,
            IEnumerable<AttributeData> attributes,
            SyntaxToken? target,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            if (target.HasValue && !target.Value.IsValidAttributeTarget())
            {
                throw new ArgumentException("target");
            }

            var attributeSyntaxList = AttributeGenerator.GenerateAttributeLists(attributes.ToImmutableArray(), options, target).ToArray();

            return destination switch
            {
                MemberDeclarationSyntax member => Cast<TDeclarationNode>(member.AddAttributeLists(attributeSyntaxList)),
                AccessorDeclarationSyntax accessor => Cast<TDeclarationNode>(accessor.AddAttributeLists(attributeSyntaxList)),
                CompilationUnitSyntax compilationUnit => Cast<TDeclarationNode>(compilationUnit.AddAttributeLists(attributeSyntaxList)),
                ParameterSyntax parameter => Cast<TDeclarationNode>(parameter.AddAttributeLists(attributeSyntaxList)),
                TypeParameterSyntax typeParameter => Cast<TDeclarationNode>(typeParameter.AddAttributeLists(attributeSyntaxList)),
                _ => destination,
            };
        }

        protected override TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> members)
        {
            CheckDeclarationNode<EnumDeclarationSyntax, TypeDeclarationSyntax, BaseNamespaceDeclarationSyntax, CompilationUnitSyntax>(destination);

            if (destination is EnumDeclarationSyntax enumDeclaration)
            {
                return Cast<TDeclarationNode>(enumDeclaration.AddMembers(members.Cast<EnumMemberDeclarationSyntax>().ToArray()));
            }
            else if (destination is TypeDeclarationSyntax typeDeclaration)
            {
                return Cast<TDeclarationNode>(typeDeclaration.AddMembers(members.Cast<MemberDeclarationSyntax>().ToArray()));
            }
            else if (destination is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                return Cast<TDeclarationNode>(namespaceDeclaration.AddMembers(members.Cast<MemberDeclarationSyntax>().ToArray()));
            }
            else
            {
                return Cast<TDeclarationNode>(Cast<CompilationUnitSyntax>(destination)
                    .AddMembers(members.Cast<MemberDeclarationSyntax>().ToArray()));
            }
        }

        public override TDeclarationNode RemoveAttribute<TDeclarationNode>(
            TDeclarationNode destination,
            AttributeData attributeToRemove,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            if (attributeToRemove.ApplicationSyntaxReference == null)
            {
                throw new ArgumentException("attributeToRemove");
            }

            var attributeSyntaxToRemove = attributeToRemove.ApplicationSyntaxReference.GetSyntax(cancellationToken);
            return RemoveAttribute(destination, attributeSyntaxToRemove, options, cancellationToken);
        }

        public override TDeclarationNode RemoveAttribute<TDeclarationNode>(
            TDeclarationNode destination,
            SyntaxNode attributeToRemove,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            if (attributeToRemove == null)
            {
                throw new ArgumentException("attributeToRemove");
            }

            // Removed node could be AttributeSyntax or AttributeListSyntax.
            int positionOfRemovedNode;
            SyntaxTriviaList triviaOfRemovedNode;

            switch (destination)
            {
                case MemberDeclarationSyntax member:
                    {
                        // Handle all members including types.
                        var newAttributeLists = RemoveAttributeFromAttributeLists(member.GetAttributes(), attributeToRemove, out positionOfRemovedNode, out triviaOfRemovedNode);
                        var newMember = member.WithAttributeLists(newAttributeLists);
                        return Cast<TDeclarationNode>(AppendTriviaAtPosition(newMember, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode));
                    }

                case AccessorDeclarationSyntax accessor:
                    {
                        // Handle accessors
                        var newAttributeLists = RemoveAttributeFromAttributeLists(accessor.AttributeLists, attributeToRemove, out positionOfRemovedNode, out triviaOfRemovedNode);
                        var newAccessor = accessor.WithAttributeLists(newAttributeLists);
                        return Cast<TDeclarationNode>(AppendTriviaAtPosition(newAccessor, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode));
                    }

                case CompilationUnitSyntax compilationUnit:
                    {
                        // Handle global attributes
                        var newAttributeLists = RemoveAttributeFromAttributeLists(compilationUnit.AttributeLists, attributeToRemove, out positionOfRemovedNode, out triviaOfRemovedNode);
                        var newCompilationUnit = compilationUnit.WithAttributeLists(newAttributeLists);
                        return Cast<TDeclarationNode>(AppendTriviaAtPosition(newCompilationUnit, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode));
                    }

                case ParameterSyntax parameter:
                    {
                        // Handle parameters
                        var newAttributeLists = RemoveAttributeFromAttributeLists(parameter.AttributeLists, attributeToRemove, out positionOfRemovedNode, out triviaOfRemovedNode);
                        var newParameter = parameter.WithAttributeLists(newAttributeLists);
                        return Cast<TDeclarationNode>(AppendTriviaAtPosition(newParameter, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode));
                    }

                case TypeParameterSyntax typeParameter:
                    {
                        var newAttributeLists = RemoveAttributeFromAttributeLists(typeParameter.AttributeLists, attributeToRemove, out positionOfRemovedNode, out triviaOfRemovedNode);
                        var newTypeParameter = typeParameter.WithAttributeLists(newAttributeLists);
                        return Cast<TDeclarationNode>(AppendTriviaAtPosition(newTypeParameter, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode));
                    }
            }

            return destination;
        }

        private static SyntaxList<AttributeListSyntax> RemoveAttributeFromAttributeLists(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxNode attributeToRemove,
            out int positionOfRemovedNode,
            out SyntaxTriviaList triviaOfRemovedNode)
        {
            foreach (var attributeList in attributeLists)
            {
                var attributes = attributeList.Attributes;
                if (attributes.Contains(attributeToRemove))
                {
                    IEnumerable<SyntaxTrivia> trivia;
                    IEnumerable<AttributeListSyntax> newAttributeLists;
                    if (attributes.Count == 1)
                    {
                        // Remove the entire attribute list.
                        ComputePositionAndTriviaForRemoveAttributeList(attributeList, (SyntaxTrivia t) => t.IsKind(SyntaxKind.EndOfLineTrivia), out positionOfRemovedNode, out trivia);
                        newAttributeLists = attributeLists.Where(aList => aList != attributeList);
                    }
                    else
                    {
                        // Remove just the given attribute from the attribute list.
                        ComputePositionAndTriviaForRemoveAttributeFromAttributeList(attributeToRemove, (SyntaxToken t) => t.IsKind(SyntaxKind.CommaToken), out positionOfRemovedNode, out trivia);
                        var newAttributes = SyntaxFactory.SeparatedList(attributes.Where(a => a != attributeToRemove));
                        var newAttributeList = attributeList.WithAttributes(newAttributes);
                        newAttributeLists = attributeLists.Select(attrList => attrList == attributeList ? newAttributeList : attrList);
                    }

                    triviaOfRemovedNode = trivia.ToSyntaxTriviaList();
                    return newAttributeLists.ToSyntaxList();
                }
            }

            throw new ArgumentException("attributeToRemove");
        }

        public override TDeclarationNode AddStatements<TDeclarationNode>(
            TDeclarationNode destinationMember,
            IEnumerable<SyntaxNode> statements,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            if (destinationMember is MemberDeclarationSyntax memberDeclaration)
            {
                return AddStatementsToMemberDeclaration<TDeclarationNode>(destinationMember, statements, memberDeclaration);
            }
            else if (destinationMember is LocalFunctionStatementSyntax localFunctionDeclaration)
            {
                return (localFunctionDeclaration.Body == null) ? destinationMember : Cast<TDeclarationNode>(localFunctionDeclaration.AddBodyStatements(StatementGenerator.GenerateStatements(statements).ToArray()));
            }
            else if (destinationMember is AccessorDeclarationSyntax accessorDeclaration)
            {
                return (accessorDeclaration.Body == null) ? destinationMember : Cast<TDeclarationNode>(accessorDeclaration.AddBodyStatements(StatementGenerator.GenerateStatements(statements).ToArray()));
            }
            else if (destinationMember is CompilationUnitSyntax compilationUnit && options.Context.BestLocation is null)
            {
                // This path supports top-level statement insertion. It only applies when best location is unspecified
                // so the fallback code below can handle cases where the insertion location is provided.
                //
                // Insert the new global statement(s) at the end of any current global statements.
                // This code relies on 'LastIndexOf' returning -1 when no matching element is found.
                var insertionIndex = compilationUnit.Members.LastIndexOf(memberDeclaration => memberDeclaration.IsKind(SyntaxKind.GlobalStatement)) + 1;
                var wrappedStatements = StatementGenerator.GenerateStatements(statements).Select(generated => SyntaxFactory.GlobalStatement(generated)).ToArray();
                return Cast<TDeclarationNode>(compilationUnit.WithMembers(compilationUnit.Members.InsertRange(insertionIndex, wrappedStatements)));
            }
            else if (destinationMember is StatementSyntax statement && statement.IsParentKind(SyntaxKind.GlobalStatement))
            {
                // We are adding a statement to a global statement in script, where the CompilationUnitSyntax is not a
                // statement container. If the global statement is not already a block, create a block which can hold
                // both the original statement and any new statements we are adding to it.
                var block = statement as BlockSyntax ?? SyntaxFactory.Block(statement);
                return Cast<TDeclarationNode>(block.AddStatements(StatementGenerator.GenerateStatements(statements).ToArray()));
            }
            else
            {
                return AddStatementsWorker(destinationMember, statements, options, cancellationToken);
            }
        }

        private static TDeclarationNode AddStatementsWorker<TDeclarationNode>(
            TDeclarationNode destinationMember,
            IEnumerable<SyntaxNode> statements,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            var location = options.Context.BestLocation;
            CheckLocation(destinationMember, location);

            var token = location.FindToken(cancellationToken);

            var block = token.Parent.GetAncestorsOrThis<BlockSyntax>().FirstOrDefault();
            if (block != null)
            {
                var blockStatements = block.Statements.ToSet();
                var containingStatement = token.GetAncestors<StatementSyntax>().Single(blockStatements.Contains);
                var index = block.Statements.IndexOf(containingStatement);

                var newStatements = statements.OfType<StatementSyntax>().ToArray();
                BlockSyntax newBlock;
                if (options.Context.BeforeThisLocation != null)
                {
                    var newContainingStatement = containingStatement.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out var strippedTrivia);

                    newStatements[0] = newStatements[0].WithLeadingTrivia(strippedTrivia);

                    newBlock = block.ReplaceNode(containingStatement, newContainingStatement);
                    newBlock = newBlock.WithStatements(newBlock.Statements.InsertRange(index, newStatements));
                }
                else
                {
                    newBlock = block.WithStatements(block.Statements.InsertRange(index + 1, newStatements));
                }

                return destinationMember.ReplaceNode(block, newBlock);
            }

            throw new ArgumentException(CSharpWorkspaceResources.No_available_location_found_to_add_statements_to);
        }

        private static TDeclarationNode AddStatementsToMemberDeclaration<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<SyntaxNode> statements, MemberDeclarationSyntax memberDeclaration) where TDeclarationNode : SyntaxNode
        {
            var body = memberDeclaration.GetBody();
            if (body == null)
            {
                return destinationMember;
            }

            var statementNodes = body.Statements.ToList();
            statementNodes.AddRange(StatementGenerator.GenerateStatements(statements));

            var finalBody = body.WithStatements(SyntaxFactory.List<StatementSyntax>(statementNodes));
            var finalMember = memberDeclaration.WithBody(finalBody);

            return Cast<TDeclarationNode>(finalMember);
        }

        public override SyntaxNode CreateEventDeclaration(
            IEventSymbol @event, CodeGenerationDestination destination, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return EventGenerator.GenerateEventDeclaration(@event, destination, options, cancellationToken);
        }

        public override SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return destination == CodeGenerationDestination.EnumType
                ? EnumMemberGenerator.GenerateEnumMemberDeclaration(field, destination: null, options, cancellationToken)
                : FieldGenerator.GenerateFieldDeclaration(field, options, cancellationToken);
        }

        // TODO: Change to not return null (https://github.com/dotnet/roslyn/issues/58243)
        public override SyntaxNode? CreateMethodDeclaration(
            IMethodSymbol method, CodeGenerationDestination destination, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            // Synthesized methods for properties/events are not things we actually generate 
            // declarations for.
            if (method.AssociatedSymbol is IEventSymbol)
            {
                return null;
            }
            // we will ignore the method if the associated property can be generated.

            if (method.AssociatedSymbol is IPropertySymbol property)
            {
                if (PropertyGenerator.CanBeGenerated(property))
                {
                    return null;
                }
            }

            var csharpOptions = options;

            if (method.IsDestructor())
            {
                return DestructorGenerator.GenerateDestructorDeclaration(method, csharpOptions, cancellationToken);
            }

            if (method.IsConstructor())
            {
                return ConstructorGenerator.GenerateConstructorDeclaration(method, csharpOptions, cancellationToken);
            }

            if (method.IsUserDefinedOperator())
            {
                return OperatorGenerator.GenerateOperatorDeclaration(method, csharpOptions, cancellationToken);
            }

            if (method.IsConversion())
            {
                return ConversionGenerator.GenerateConversionDeclaration(method, csharpOptions, cancellationToken);
            }

            if (method.IsLocalFunction())
            {
                return MethodGenerator.GenerateLocalFunctionDeclaration(method, destination, csharpOptions, cancellationToken);
            }

            return MethodGenerator.GenerateMethodDeclaration(method, destination, csharpOptions, cancellationToken);
        }

        public override SyntaxNode CreatePropertyDeclaration(
            IPropertySymbol property, CodeGenerationDestination destination, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return PropertyGenerator.GeneratePropertyOrIndexer(
                property, destination, options, cancellationToken);
        }

        public override SyntaxNode CreateNamedTypeDeclaration(
            INamedTypeSymbol namedType, CodeGenerationDestination destination, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return NamedTypeGenerator.GenerateNamedTypeDeclaration(this, namedType, destination, options, cancellationToken);
        }

        public override SyntaxNode CreateNamespaceDeclaration(
            INamespaceSymbol @namespace, CodeGenerationDestination destination, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return NamespaceGenerator.GenerateNamespaceDeclaration(this, @namespace, destination, options, cancellationToken);
        }

        private static TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, Func<SyntaxTokenList, SyntaxTokenList> computeNewModifiersList)
            => declaration switch
            {
                BaseTypeDeclarationSyntax typeDeclaration => Cast<TDeclarationNode>(typeDeclaration.WithModifiers(computeNewModifiersList(typeDeclaration.Modifiers))),
                BaseFieldDeclarationSyntax fieldDeclaration => Cast<TDeclarationNode>(fieldDeclaration.WithModifiers(computeNewModifiersList(fieldDeclaration.Modifiers))),
                BaseMethodDeclarationSyntax methodDeclaration => Cast<TDeclarationNode>(methodDeclaration.WithModifiers(computeNewModifiersList(methodDeclaration.Modifiers))),
                BasePropertyDeclarationSyntax propertyDeclaration => Cast<TDeclarationNode>(propertyDeclaration.WithModifiers(computeNewModifiersList(propertyDeclaration.Modifiers))),
                _ => declaration,
            };

        public override TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            SyntaxTokenList computeNewModifiersList(SyntaxTokenList modifiersList) => newModifiers.ToSyntaxTokenList();
            return UpdateDeclarationModifiers(declaration, computeNewModifiersList);
        }

        public override TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            SyntaxTokenList computeNewModifiersList(SyntaxTokenList modifiersList) => UpdateDeclarationAccessibility(modifiersList, newAccessibility, options);
            return UpdateDeclarationModifiers(declaration, computeNewModifiersList);
        }

        private static SyntaxTokenList UpdateDeclarationAccessibility(SyntaxTokenList modifiersList, Accessibility newAccessibility, CSharpCodeGenerationOptions options)
        {
            using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var newModifierTokens);
            CSharpCodeGenerationHelpers.AddAccessibilityModifiers(newAccessibility, newModifierTokens, options, Accessibility.NotApplicable);
            if (newModifierTokens.Count == 0)
            {
                return modifiersList;
            }

            // TODO: Move more APIs to use pooled ArrayBuilder
            // https://github.com/dotnet/roslyn/issues/34960
            return GetUpdatedDeclarationAccessibilityModifiers(
                newModifierTokens, modifiersList,
                modifier => SyntaxFacts.IsAccessibilityModifier(modifier.Kind()));
        }

        public override TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            if (declaration is not CSharpSyntaxNode syntaxNode)
            {
                return declaration;
            }

            TypeSyntax newTypeSyntax;
            switch (syntaxNode.Kind())
            {
                case SyntaxKind.DelegateDeclaration:
                    // Handle delegate declarations.
                    var delegateDeclarationSyntax = (DelegateDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(delegateDeclarationSyntax.ReturnType.GetLeadingTrivia())
                        .WithTrailingTrivia(delegateDeclarationSyntax.ReturnType.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(delegateDeclarationSyntax.WithReturnType(newTypeSyntax));

                case SyntaxKind.MethodDeclaration:
                    // Handle method declarations.
                    var methodDeclarationSyntax = (MethodDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(methodDeclarationSyntax.ReturnType.GetLeadingTrivia())
                        .WithTrailingTrivia(methodDeclarationSyntax.ReturnType.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(methodDeclarationSyntax.WithReturnType(newTypeSyntax));

                case SyntaxKind.OperatorDeclaration:
                    // Handle operator declarations.
                    var operatorDeclarationSyntax = (OperatorDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(operatorDeclarationSyntax.ReturnType.GetLeadingTrivia())
                        .WithTrailingTrivia(operatorDeclarationSyntax.ReturnType.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(operatorDeclarationSyntax.WithReturnType(newTypeSyntax));

                case SyntaxKind.ConversionOperatorDeclaration:
                    // Handle conversion operator declarations.
                    var conversionOperatorDeclarationSyntax = (ConversionOperatorDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(conversionOperatorDeclarationSyntax.Type.GetLeadingTrivia())
                        .WithTrailingTrivia(conversionOperatorDeclarationSyntax.Type.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(conversionOperatorDeclarationSyntax.WithType(newTypeSyntax));

                case SyntaxKind.PropertyDeclaration:
                    // Handle properties.
                    var propertyDeclaration = (PropertyDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(propertyDeclaration.Type.GetLeadingTrivia())
                        .WithTrailingTrivia(propertyDeclaration.Type.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(propertyDeclaration.WithType(newTypeSyntax));

                case SyntaxKind.EventDeclaration:
                    // Handle events.
                    var eventDeclarationSyntax = (EventDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(eventDeclarationSyntax.Type.GetLeadingTrivia())
                        .WithTrailingTrivia(eventDeclarationSyntax.Type.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(eventDeclarationSyntax.WithType(newTypeSyntax));

                case SyntaxKind.IndexerDeclaration:
                    // Handle indexers.
                    var indexerDeclarationSyntax = (IndexerDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(indexerDeclarationSyntax.Type.GetLeadingTrivia())
                        .WithTrailingTrivia(indexerDeclarationSyntax.Type.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(indexerDeclarationSyntax.WithType(newTypeSyntax));

                case SyntaxKind.Parameter:
                    // Handle parameters.
                    var parameterSyntax = (ParameterSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax();

                    if (parameterSyntax.Type != null)
                    {
                        newTypeSyntax = newTypeSyntax
                            .WithLeadingTrivia(parameterSyntax.Type.GetLeadingTrivia())
                            .WithTrailingTrivia(parameterSyntax.Type.GetTrailingTrivia());
                    }

                    return Cast<TDeclarationNode>(parameterSyntax.WithType(newTypeSyntax));

                case SyntaxKind.IncompleteMember:
                    // Handle incomplete members.
                    var incompleteMemberSyntax = (IncompleteMemberSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax();

                    if (incompleteMemberSyntax.Type != null)
                    {
                        newTypeSyntax = newTypeSyntax
                            .WithLeadingTrivia(incompleteMemberSyntax.Type.GetLeadingTrivia())
                            .WithTrailingTrivia(incompleteMemberSyntax.Type.GetTrailingTrivia());
                    }

                    return Cast<TDeclarationNode>(incompleteMemberSyntax.WithType(newTypeSyntax));

                case SyntaxKind.ArrayType:
                    // Handle array type.
                    var arrayTypeSyntax = (ArrayTypeSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(arrayTypeSyntax.ElementType.GetLeadingTrivia())
                        .WithTrailingTrivia(arrayTypeSyntax.ElementType.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(arrayTypeSyntax.WithElementType(newTypeSyntax));

                case SyntaxKind.PointerType:
                    // Handle pointer type.
                    var pointerTypeSyntax = (PointerTypeSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(pointerTypeSyntax.ElementType.GetLeadingTrivia())
                        .WithTrailingTrivia(pointerTypeSyntax.ElementType.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(pointerTypeSyntax.WithElementType(newTypeSyntax));

                case SyntaxKind.VariableDeclaration:
                    // Handle variable declarations.
                    var variableDeclarationSyntax = (VariableDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(variableDeclarationSyntax.Type.GetLeadingTrivia())
                        .WithTrailingTrivia(variableDeclarationSyntax.Type.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(variableDeclarationSyntax.WithType(newTypeSyntax));

                case SyntaxKind.CatchDeclaration:
                    // Handle catch declarations.
                    var catchDeclarationSyntax = (CatchDeclarationSyntax)syntaxNode;
                    newTypeSyntax = newType.GenerateTypeSyntax()
                        .WithLeadingTrivia(catchDeclarationSyntax.Type.GetLeadingTrivia())
                        .WithTrailingTrivia(catchDeclarationSyntax.Type.GetTrailingTrivia());
                    return Cast<TDeclarationNode>(catchDeclarationSyntax.WithType(newTypeSyntax));

                default:
                    return declaration;
            }
        }

        public override TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, CSharpCodeGenerationOptions options, CancellationToken cancellationToken)
        {
            if (declaration is MemberDeclarationSyntax memberDeclaration)
            {
                return Cast<TDeclarationNode>(NamedTypeGenerator.UpdateNamedTypeDeclaration(this, memberDeclaration, newMembers, options, cancellationToken));
            }

            if (declaration is CSharpSyntaxNode syntaxNode)
            {
                switch (syntaxNode.Kind())
                {
                    case SyntaxKind.CompilationUnit:
                    case SyntaxKind.NamespaceDeclaration:
                    case SyntaxKind.FileScopedNamespaceDeclaration:
                        return Cast<TDeclarationNode>(NamespaceGenerator.UpdateCompilationUnitOrNamespaceDeclaration(this, syntaxNode, newMembers, options, cancellationToken));
                }
            }

            return declaration;
        }
    }
}
