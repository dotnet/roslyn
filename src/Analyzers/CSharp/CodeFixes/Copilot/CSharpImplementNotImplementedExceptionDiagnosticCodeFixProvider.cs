// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementNotImplementedException), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpImplementNotImplementedExceptionCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.CopilotImplementNotImplementedExceptionDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Implement_with_Copilot, nameof(CSharpAnalyzersResources.Implement_with_Copilot));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
            await FixOneAsync(editor, document, diagnostic, cancellationToken).ConfigureAwait(false);
    }

    private static async Task FixOneAsync(
    SyntaxEditor editor, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        // Find the throw statement node
        var throwStatement = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken).AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        if (throwStatement == null)
        {
            return;
        }

        // Find the containing method
        var containingMethod = throwStatement.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod == null)
        {
            return;
        }

        var containingClass = containingMethod.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass == null)
        {
            return;
        }

        var containingMethodName = containingMethod.Identifier.Text;
        var referencedMethods = new List<string>();

        // Traverse the syntax tree to find all method invocations
        var root = containingMethod.SyntaxTree.GetRoot(cancellationToken);
        var methodInvocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in methodInvocations)
        {
            string? invokedMethodName = null;

            // Check if the invocation is a simple identifier
            if (invocation.Expression is IdentifierNameSyntax identifierName)
            {
                invokedMethodName = identifierName.Identifier.Text;
            }
            // Check if the invocation is a member access expression (e.g., this.MethodName or ClassName.MethodName)
            else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                invokedMethodName = memberAccess.Name.Identifier.Text;
            }

            if (invokedMethodName == containingMethodName)
            {
                var invokingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (invokingMethod != null)
                {
                    referencedMethods.Add(invokingMethod.Identifier.Text);
                }
            }
        }

        // Get the semantic model
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        // Determine the logging mechanism
        string? loggerFieldName = null;
        string? loggerFieldTypeName = null;
        var isConsoleLoggingUsed = IsConsoleLoggingUsed(containingClass);

        foreach (var member in containingClass.Members)
        {
            if (member is FieldDeclarationSyntax field)
            {
                var variable = field.Declaration.Variables.FirstOrDefault();
                if (variable != null && IsLoggerType(field.Declaration.Type, semanticModel, cancellationToken, out loggerFieldTypeName))
                {
                    loggerFieldName = variable.Identifier.Text;
                    break;
                }
            }
            else if (member is PropertyDeclarationSyntax property)
            {
                if (IsLoggerType(property.Type, semanticModel, cancellationToken, out loggerFieldTypeName))
                {
                    loggerFieldName = property.Identifier.Text;
                    break;
                }
            }
        }

        // Gather information about referenced packages and using directives
        var referencedPackages = GetRelevantReferencedPackages(document.Project);
        var usingDirectives = GetUsingDirectives(root);

        // Initialize the comment with a basic TODO message
        var referencesComment = "// TODO: Implement this method\n";
        if (referencedMethods.Any())
        {
            referencesComment += "// Referenced by methods:\n";
            foreach (var method in referencedMethods)
            {
                referencesComment += $"// - {method}\n";
            }
        }

        // Add method attributes
        var attributes = containingMethod.AttributeLists.SelectMany(attrList => attrList.Attributes);
        if (attributes.Any())
        {
            referencesComment += "// Attributes:\n";
            foreach (var attribute in attributes)
            {
                referencesComment += $"// - {attribute.Name}\n";
            }
        }

        // Add containing class information
        var baseTypes = containingClass.BaseList?.Types.Select(type => type.ToString());
        if (baseTypes != null && baseTypes.Any())
        {
            referencesComment += "// Containing class inherits from:\n";
            foreach (var baseType in baseTypes)
            {
                referencesComment += $"// - {baseType}\n";
            }
        }

        // Add containing class properties
        var properties = containingClass.Members.OfType<PropertyDeclarationSyntax>();
        if (properties.Any())
        {
            referencesComment += "// Containing class properties:\n";
            foreach (var property in properties)
            {
                referencesComment += $"// - {property.Identifier}\n";
            }
        }

        // Add containing class methods
        var methods = containingClass.Members.OfType<MethodDeclarationSyntax>();
        if (methods.Any())
        {
            referencesComment += "// Containing class methods:\n";
            foreach (var method in methods)
            {
                // ignore the current method
                if (method.Identifier.Text == containingMethodName)
                {
                    continue;
                }

                referencesComment += $"// - {method.Identifier}\n";
            }
        }

        // Add method parameters
        var parameters = containingMethod.ParameterList.Parameters;
        if (parameters.Any())
        {
            referencesComment += "// Parameters:\n";
            foreach (var parameter in parameters)
            {
                referencesComment += $"// - {parameter.Identifier.Text} ({parameter.Type})\n";
            }
        }

        // Add method return type
        referencesComment += $"// Return type: {containingMethod.ReturnType}\n";

        // Add referenced packages
        if (referencedPackages.Any())
        {
            referencesComment += "// Referenced packages:\n";
            foreach (var package in referencedPackages)
            {
                referencesComment += $"// - {package}\n";
            }
        }

        // Add using directives
        if (usingDirectives.Any())
        {
            referencesComment += "// Using directives:\n";
            foreach (var usingDirective in usingDirectives)
            {
                referencesComment += $"// - {usingDirective}\n";
            }
        }

        // Split the comment into individual lines
        var commentLines = referencesComment.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

        // Get the leading trivia of the throw statement
        var leadingTrivia = throwStatement.GetLeadingTrivia();

        // Create a new trivia list with the comment lines, preserving indentation
        var newLeadingTrivia = leadingTrivia;
        foreach (var line in commentLines)
        {
            newLeadingTrivia = newLeadingTrivia.Add(SyntaxFactory.Comment(line)).Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
        }

        if (isConsoleLoggingUsed)
        {
            var logPlaceholderComment = SyntaxFactory.Comment("// TODO: Add logging (e.g., Console.WriteLine)");
            newLeadingTrivia = newLeadingTrivia.Add(logPlaceholderComment).Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
        }
        else if (loggerFieldName != null && !string.IsNullOrEmpty(loggerFieldTypeName))
        {
            var logPlaceholderComment = SyntaxFactory.Comment($"// TODO: Add logging using {loggerFieldName} ({loggerFieldTypeName})");
            newLeadingTrivia = newLeadingTrivia.Add(logPlaceholderComment).Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
        }
        else
        {
            var logPlaceholderComment = SyntaxFactory.Comment("// TODO: Add appropriate logging");
            newLeadingTrivia = newLeadingTrivia.Add(logPlaceholderComment).Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
        }

        // Add a placeholder for input validation
        newLeadingTrivia = newLeadingTrivia.Add(SyntaxFactory.Comment("// TODO: Add input validation here")).Add(SyntaxFactory.ElasticCarriageReturnLineFeed);

        // Add a placeholder for business logic
        newLeadingTrivia = newLeadingTrivia.Add(SyntaxFactory.Comment("// TODO: Add business logic here")).Add(SyntaxFactory.ElasticCarriageReturnLineFeed);

        // Replace the throw statement with the new leading trivia
        editor.ReplaceNode(throwStatement, (currentNode, generator) =>
        {
            return currentNode.WithLeadingTrivia(newLeadingTrivia);
        });

        // Remove the throw statement but keep its leading trivia
        editor.RemoveNode(throwStatement, SyntaxRemoveOptions.KeepLeadingTrivia);
    }

    private static bool IsLoggerType(TypeSyntax typeSyntax, SemanticModel semanticModel, CancellationToken cancellationToken, out string fullyQualifiedName)
    {
        var typeInfo = semanticModel.GetTypeInfo(typeSyntax, cancellationToken);
        var typeSymbol = typeInfo.Type;
        fullyQualifiedName = string.Empty;

        if (typeSymbol == null)
        {
            return false;
        }

        fullyQualifiedName = typeSymbol.ToDisplayString();

        return fullyQualifiedName == "Microsoft.Extensions.Logging.ILogger" ||
               fullyQualifiedName.StartsWith("Microsoft.Extensions.Logging.ILogger<");
    }

    private static bool IsConsoleLoggingUsed(ClassDeclarationSyntax classDeclaration)
    {
        // Check if there are any Console.WriteLine statements in the class
        var consoleWriteLineInvocations = classDeclaration.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.Text == "Console" &&
                memberAccess.Name.Identifier.Text == "WriteLine");

        return consoleWriteLineInvocations.Any();
    }

    private static IEnumerable<string> GetRelevantReferencedPackages(Project project)
    {
        // Get the referenced packages from the project and filter out irrelevant ones
        var referencedPackages = new List<string>();
        foreach (var reference in project.MetadataReferences)
        {
            if (reference is PortableExecutableReference portableReference)
            {
                var display = portableReference.Display;
                if (!string.IsNullOrEmpty(display))
                {
                    var packageName = System.IO.Path.GetFileNameWithoutExtension(display);
                    if (IsRelevantPackage(packageName))
                    {
                        referencedPackages.Add(packageName);
                    }
                }
            }
        }

        // Apply priority-based reduction if the list exceeds 15 packages
        if (referencedPackages.Count > 15)
        {
            referencedPackages = ApplyPriorityReduction(referencedPackages);
        }

        return referencedPackages;
    }

    private static bool IsRelevantPackage(string packageName)
    {
        // Filter out standard library packages and include only relevant ones
        var irrelevantPackages = new HashSet<string>
    {
        "mscorlib",
        "System",
        "netstandard",
        "System.Core",
        "System.Xml",
        "System.Xml.Linq",
        "System.Data",
        "System.Data.DataSetExtensions",
        "System.Net.Http",
        "System.Net.Http.Json",
        "System.Net.HttpListener",
        "System.Net.Mail",
        "System.Net.NameResolution",
        "System.Net.NetworkInformation",
        "System.Net.Ping",
        "System.Net.Primitives",
        "System.Net.Requests",
        "System.Net.Security",
        "System.Net.ServicePoint",
        "System.Net.Sockets",
        "System.Net.WebClient",
        "System.Net.WebHeaderCollection",
        "System.Net.WebProxy",
        "System.Net.WebSockets.Client",
        "System.Net.WebSockets",
        "System.Runtime",
        "System.Runtime.CompilerServices.Unsafe",
        "System.Runtime.Extensions",
        "System.Runtime.Handles",
        "System.Runtime.InteropServices",
        "System.Runtime.InteropServices.RuntimeInformation",
        "System.Runtime.Loader",
        "System.Runtime.Numerics",
        "System.Runtime.Serialization",
        "System.Runtime.Serialization.Formatters",
        "System.Runtime.Serialization.Json",
        "System.Runtime.Serialization.Primitives",
        "System.Runtime.Serialization.Xml",
        "System.Security.Claims",
        "System.Security.Cryptography.Algorithms",
        "System.Security.Cryptography.Csp",
        "System.Security.Cryptography.Encoding",
        "System.Security.Cryptography.Primitives",
        "System.Security.Cryptography.X509Certificates",
        "System.Security.Principal",
        "System.Security.SecureString",
        "System.ServiceModel.Web",
        "System.ServiceProcess",
        "System.Text.Encoding.CodePages",
        "System.Text.Encoding.Extensions",
        "System.Text.Encoding",
        "System.Text.Encodings.Web",
        "System.Text.Json",
        "System.Text.RegularExpressions",
        "System.Threading.Channels",
        "System.Threading",
        "System.Threading.Overlapped",
        "System.Threading.Tasks.Dataflow",
        "System.Threading.Tasks",
        "System.Threading.Tasks.Extensions",
        "System.Threading.Tasks.Parallel",
        "System.Threading.Thread",
        "System.Threading.ThreadPool",
        "System.Threading.Timer",
        "System.Transactions",
        "System.Transactions.Local",
        "System.ValueTuple",
        "System.Web",
        "System.Web.HttpUtility",
        "System.Windows",
        "System.Xml.ReaderWriter",
        "System.Xml.Serialization",
        "System.Xml.XmlDocument",
        "System.Xml.XmlSerializer",
        "System.Xml.XPath",
        "System.Xml.XPath.XDocument",
        "Microsoft.CSharp",
        "Microsoft.VisualBasic.Core",
        "Microsoft.VisualBasic",
        "Microsoft.Win32.Primitives",
        "WindowsBase"
    };

        return !irrelevantPackages.Contains(packageName);
    }

    private static List<string> ApplyPriorityReduction(List<string> packages)
    {
        // Define package priorities (higher priority packages should be kept)
        var priorityLevels = new List<HashSet<string>>
    {
        new HashSet<string> // P0: Almost never relevant
        {
            "System.Collections.Concurrent",
            "System.Globalization.Extensions",
            "System.Diagnostics.FileVersionInfo",
            "System.Configuration",
            "System.Linq.Expressions",
            "System.Net",
            "System.ComponentModel",
            "WindowsBase",
            "System.Reflection.Extensions",
            "System.Resources.ResourceManager",
            "System.Diagnostics.Tools",
            "System.Data.Common",
            "System.Reflection.Emit",
            "System.Diagnostics.Tracing",
            "System.Reflection.Metadata",
            "System.Diagnostics.TraceSource",
            "System.IO.FileSystem",
            "System.Resources.Reader",
            "System.Buffers",
            "System.Linq.Queryable",
            "System.Diagnostics.StackTrace",
            "System.IO.FileSystem.Primitives",
            "System.ComponentModel.Primitives",
            "System.Runtime.Intrinsics",
            "System.Xml.XDocument",
            "System.IO.Compression.Brotli",
            "System.Reflection.Emit.Lightweight",
            "System.Collections.NonGeneric",
            "System.Reflection.DispatchProxy",
            "System.Console",
            "System.Diagnostics.TextWriterTraceListener",
            "System.IO.Pipes",
            "System.Resources.Writer",
            "System.ComponentModel.Annotations",
            "System.Reflection.Primitives",
            "System.Numerics.Vectors",
            "System.AppContext",
            "System.Collections",
            "System.Runtime.CompilerServices.VisualC",
            "System.IO.UnmanagedMemoryStream",
            "System.Diagnostics.Process",
            "System.ComponentModel.EventBasedAsync",
            "System.ComponentModel.TypeConverter",
            "System.ObjectModel",
            "System.Diagnostics.Contracts",
            "System.Memory",
            "System.Dynamic.Runtime",
            "System.Reflection",
            "System.ComponentModel.DataAnnotations",
            "System.Globalization.Calendars",
            "System.IO.Compression.FileSystem",
            "System.Reflection.Emit.ILGeneration",
            "System.Security",
            "System.Collections.Specialized",
            "System.Numerics",
            "System.Drawing.Primitives",
            "System.IO.IsolatedStorage",
            "System.Drawing",
            "System.IO",
            "System.IO.FileSystem.DriveInfo",
            "System.Collections.Immutable",
            "System.Diagnostics.Debug",
            "System.Formats.Asn1",
            "System.IO.FileSystem.Watcher",
            "System.IO.Compression",
            "System.IO.Compression.ZipFile",
            "System.IO.MemoryMappedFiles",
            "System.Linq",
            "System.Reflection.TypeExtensions",
            "System.Linq.Parallel",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "System.Diagnostics.DiagnosticSource",
            "Microsoft.Extensions.Logging.Abstractions"
        },
        new HashSet<string> // P1: Less commonly used
        {
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.DependencyInjection",
            "Newtonsoft.Json",
            "EntityFramework",
            "Dapper",
            "AutoMapper",
            "Serilog",
            "NLog",
            "FluentValidation",
            "MediatR",
            "Polly",
            "Swashbuckle.AspNetCore",
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Extensions.Configuration"
        }
        // Add more priority levels if needed
    };

        foreach (var priorityLevel in priorityLevels)
        {
            packages.RemoveAll(pkg => priorityLevel.Contains(pkg));
            if (packages.Count <= 15)
            {
                break;
            }
        }

        return packages.Take(15).ToList();
    }

    private static IEnumerable<string> GetUsingDirectives(SyntaxNode root)
    {
        // Get the using directives from the syntax tree
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Select(usingDirective => usingDirective.Name.ToString());
        return usingDirectives;
    }
}
