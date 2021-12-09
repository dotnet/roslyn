// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PostSharp.Engineering.BuildTools.Utilities;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

#pragma warning disable 8765

namespace PostSharp.Engineering.BuildTools.Coverage
{
    public class AnalyzeCoverageCommand : Command<AnalyzeCoverageSettings>
    {
        public override int Execute( CommandContext context, AnalyzeCoverageSettings settings )
        {
            ConsoleHelper console = new();

            return Execute( console, settings ) ? 0 : 1;
        }

        public static bool Execute( ConsoleHelper console, AnalyzeCoverageSettings settings )
        {
            console.WriteHeading( "Analyzing test coverage" );

            var totalInvalidDeclarations = 0;

            var document = JsonDocument.Parse( File.ReadAllText( settings.Path ) );

            foreach ( var packageNode in document.RootElement.EnumerateObject() )
            {
                ProcessPackage( console, packageNode, ref totalInvalidDeclarations );
            }

            console.WriteImportantMessage( $"The whole solution has {totalInvalidDeclarations} declaration(s) with insufficient test coverage." );

            return totalInvalidDeclarations == 0;
        }

        private static void ProcessPackage(
            ConsoleHelper console,
            JsonProperty packageNode,
            ref int totalInvalidDeclarations )
        {
            var invalidDeclarations = 0;
            var packageName = packageNode.Name;

            if ( packageName.Contains( ".Tests", StringComparison.OrdinalIgnoreCase ) )
            {
                return;
            }

            HashSet<SyntaxNode> nonCoveredNodes = new();

            foreach ( var fileNode in packageNode.Value.EnumerateObject().OrderBy( n => n.Name ) )
            {
                SyntaxTree? syntaxTree = null;

                foreach ( var classNode in fileNode.Value.EnumerateObject().OrderBy( n => n.Name ) )
                {
                    // ReSharper disable once UnusedVariable
                    var className = classNode.Name;

                    foreach ( var methodNode in classNode.Value.EnumerateObject().OrderBy( n => n.Name ) )
                    {
                        // Getting the method name is convenient to set a conditional breakpoint.
                        // ReSharper disable once UnusedVariable
                        var methodName = methodNode.Name;

                        // Enumerate lines.
                        foreach ( var lineNode in methodNode.Value.GetProperty( "Lines" ).EnumerateObject() )
                        {
                            var hits = lineNode.Value.GetInt32();

                            if ( hits > 0 )
                            {
                                continue;
                            }

                            var lineNumber = int.Parse( lineNode.Name, CultureInfo.InvariantCulture ) - 1;

                            // The sequence point is not covered.
                            syntaxTree ??= CSharpSyntaxTree.ParseText(
                                File.ReadAllText( fileNode.Name ),
                                CSharpParseOptions.Default.WithPreprocessorSymbols( "NET5_0" ),
                                fileNode.Name );

                            var sourceText = syntaxTree.GetText();
                            var line = sourceText.Lines[lineNumber];
                            var span = TextSpan.FromBounds( line.Start, line.End );

                            int spanStart, spanEnd;

                            for ( spanStart = span.Start;
                                  char.IsWhiteSpace( sourceText[spanStart] ) && spanStart < span.End;
                                  spanStart++ )
                            {
                                // Intentionally empty.
                            }

                            for ( spanEnd = span.End;
                                  char.IsWhiteSpace( sourceText[spanEnd] ) && spanEnd > spanStart;
                                  spanEnd-- )
                            {
                                // Intentionally empty.
                            }

                            var trimmedSpan = TextSpan.FromBounds( spanStart, spanEnd + 1 );
                            var spanText = sourceText.GetSubText( trimmedSpan ).ToString();

                            if ( spanText.Length == 1 )
                            {
                                // Ignore single-character lines. Typically it's a line containing { or }.
                                continue;
                            }

                            var node = syntaxTree.GetRoot()
                                .FindNode( TextSpan.FromBounds( spanStart, spanEnd ), true, true );

                            if ( node is CompilationUnitSyntax or ClassDeclarationSyntax )
                            {
                                // THis typically happens because the block is disabled by an inactive #if.
                                continue;
                            }

                            if ( !nonCoveredNodes.Add( node ) )
                            {
                                continue;
                            }

                            var declarations = node.AncestorsAndSelf()
                                .Where( a => a is MemberDeclarationSyntax or AccessorDeclarationSyntax )
                                .Reverse()
                                .ToList();

                            if ( declarations.Count == 0 )
                            {
                                return;
                            }

                            var member = declarations.OfType<MemberDeclarationSyntax>().Last();

                            if ( ShouldIgnoreMember( member ) )
                            {
                                break;
                            }

                            if ( ShouldIgnoreNodeOrAncestor( member, node ) )
                            {
                                continue;
                            }

                            var memberName = string.Join( '.', declarations.Select( GetNodeSignature ) );

                            console.WriteWarning( $"{node.SyntaxTree.FilePath}({lineNumber + 1}): warning COVER01: '{memberName}' has gaps in test coverage." );
                            totalInvalidDeclarations++;
                            invalidDeclarations++;

                            break;
                        }
                    }
                }
            }

            console.WriteWarning( $"Module {packageName} has {invalidDeclarations} declaration(s) with insufficient test coverage." );
        }

        private static readonly Regex _ignoreCommentRegex = new( @"//\s*Coverage:\s*Ignore", RegexOptions.IgnoreCase );

        private static bool ContainsIgnoreComment( string text ) => _ignoreCommentRegex.IsMatch( text );

        private static bool IsSameMemberName( MemberDeclarationSyntax declaringMember, ExpressionSyntax expression )
            => expression switch
            {
                MemberAccessExpressionSyntax name => NormalizeName( GetNodeName( declaringMember ) ) ==
                                                     NormalizeName( name.Name.Identifier.Text ),
                _ => false
            };

        private static string NormalizeName( string s ) => s.Trim( '_' ).ToLowerInvariant();

        private static bool ShouldIgnoreNodeOrAncestor( MemberDeclarationSyntax declaringMember, SyntaxNode? node )
            => ShouldIgnoreNode( declaringMember, node ) ||
               (node != null && node is not MemberDeclarationSyntax &&
                ShouldIgnoreNodeOrAncestor( declaringMember, node.Parent ));

        private static bool ShouldIgnoreNode( MemberDeclarationSyntax declaringMember, SyntaxNode? node )
        {
            switch ( node )
            {
                case null:
                case BlockSyntax block when block.Statements.Count == 0 || (block.Statements.Count == 1 &&
                                                                            ShouldIgnoreNode(
                                                                                declaringMember,
                                                                                block.Statements[0] )):
                case ThrowExpressionSyntax:
                case ThrowStatementSyntax:
                case MemberDeclarationSyntax member when ShouldIgnoreMember( member ):
                case SwitchExpressionArmSyntax arm when ShouldIgnoreNode( declaringMember, arm.Expression ):
                case ArrowExpressionClauseSyntax arrow when ShouldIgnoreNode( declaringMember, arrow.Expression ):
                case ExpressionStatementSyntax statement when ShouldIgnoreNode( declaringMember, statement.Expression ):
                case InvocationExpressionSyntax invocation
                    when IsSameMemberName( declaringMember, invocation.Expression ) ||
                         invocation.ArgumentList.Arguments.Count == 0:
                case MemberAccessExpressionSyntax memberAccess
                    when ShouldIgnoreNode( declaringMember, memberAccess.Expression ):
                case ConditionalAccessExpressionSyntax conditionalAccess
                    when ShouldIgnoreNode( declaringMember, conditionalAccess.Expression ) &&
                         ShouldIgnoreNode( declaringMember, conditionalAccess.WhenNotNull ):
                case QualifiedNameSyntax:
                case IdentifierNameSyntax:
                case LiteralExpressionSyntax:
                case DefaultExpressionSyntax:
                case TypeOfExpressionSyntax:
                case ThisExpressionSyntax:
                case BaseExpressionSyntax:
                case AssignmentExpressionSyntax assignment when ShouldIgnoreNode( declaringMember, assignment.Left ) &&
                                                                ShouldIgnoreNode( declaringMember, assignment.Right ):
                case CastExpressionSyntax cast when ShouldIgnoreNode( declaringMember, cast.Expression ):
                case CatchClauseSyntax:
                case AccessorDeclarationSyntax accessor when ShouldIgnoreNode( declaringMember, accessor.Body ) &&
                                                             ShouldIgnoreNode(
                                                                 declaringMember,
                                                                 accessor.ExpressionBody ):
                    return true;

                default:
                    var parentStatement = GetParentStatement( node );

                    if ( parentStatement != null && ContainsIgnoreComment( parentStatement.ToFullString() ) )
                    {
                        return true;
                    }

                    var parentBlock = GetParentBlock( node );

                    if ( parentBlock != null && ContainsIgnoreComment( parentBlock.ToFullString() ) )
                    {
                        return true;
                    }

                    return false;
            }
        }

        private static bool ShouldIgnoreMember( MemberDeclarationSyntax member )
            => member switch
            {
                // Ignore a few methods that generally have a trivial implementation, or an implementation that is not used in real code. 
                MethodDeclarationSyntax { Identifier: { Text: "ToString" or "GetHashCode" or "Equals" } } => true,

                // Ignore operators because some operators come in pairs, only one of which will be used. 
                OperatorDeclarationSyntax => true,

                // Process properties with accessors.
                PropertyDeclarationSyntax { ExpressionBody: null, AccessorList: { } accessorList }
                    when accessorList.Accessors.All(
                        a =>
                            ShouldIgnoreNode( member, a.Body ) && ShouldIgnoreNode( member, a.ExpressionBody ) )
                    => true,

                // Process properties without accessors.
                PropertyDeclarationSyntax { ExpressionBody: { Expression: { } expression } } when ShouldIgnoreNode(
                        member,
                        expression )
                    => true,

                // Process methods.
                MethodDeclarationSyntax method when ShouldIgnoreNode( member, method.ExpressionBody ) &&
                                                    ShouldIgnoreNode( member, method.Body )
                    => true,

                _ => member.GetLeadingTrivia().Any( t => ContainsIgnoreComment( t.ToFullString() ) )
            };

        private static string GetNodeSignature( SyntaxNode node )
            => node switch
            {
                MethodDeclarationSyntax m => m.Identifier.Text + "(" +
                                             string.Join(
                                                 ",",
                                                 m.ParameterList.Parameters.Select( p => p.Type?.ToString() ) ) + ")",
                ConstructorDeclarationSyntax c => "ctor" + "(" +
                                                  string.Join(
                                                      ",",
                                                      c.ParameterList.Parameters.Select( p => p.Type?.ToString() ) ) +
                                                  ")",
                IndexerDeclarationSyntax i => "this[" +
                                              string.Join(
                                                  ",",
                                                  i.ParameterList.Parameters.Select( p => p.Type?.ToString() ) ) + "]",
                OperatorDeclarationSyntax o => o.OperatorToken.Text + "(" +
                                               string.Join(
                                                   ",",
                                                   o.ParameterList.Parameters.Select( p => p.Type?.ToString() ) ) + ")",
                ConversionOperatorDeclarationSyntax c => "convert(" + string.Join(
                    ",",
                    c.ParameterList.Parameters.Select( p => p.Type?.ToString() ) ) + ")" + "):" + c.Type,
                _ => GetNodeName( node )
            };

        private static string GetNodeName( SyntaxNode node )
            => node switch
            {
                IndexerDeclarationSyntax => "this[]",
                OperatorDeclarationSyntax o => o.OperatorToken.Text,
                AccessorDeclarationSyntax a => a.Keyword.Text,
                MethodDeclarationSyntax m => m.Identifier.Text,
                BaseTypeDeclarationSyntax c => c.Identifier.Text,
                FieldDeclarationSyntax f => string.Join(
                    ",",
                    f.Declaration.Variables.Select( v => v.Identifier.Text ) ),
                EventDeclarationSyntax e => e.Identifier.Text,
                EventFieldDeclarationSyntax e => string.Join(
                    ",",
                    e.Declaration.Variables.Select( v => v.Identifier.Text ) ),
                PropertyDeclarationSyntax p => p.Identifier.Text,
                NamespaceDeclarationSyntax n => n.Name.ToString(),
                ConstructorDeclarationSyntax => "ctor",
                _ => node.Kind().ToString()
            };

        private static StatementSyntax? GetParentStatement( SyntaxNode node )
            => node switch
            {
                StatementSyntax statement => statement,
                MemberDeclarationSyntax => null,
                { Parent: { } parent } => GetParentStatement( parent ),
                _ => null
            };

        private static BlockSyntax? GetParentBlock( SyntaxNode node )
            => node switch
            {
                BlockSyntax block => block,
                MemberDeclarationSyntax => null,
                { Parent: { } parent } => GetParentBlock( parent ),
                _ => null
            };
    }
}