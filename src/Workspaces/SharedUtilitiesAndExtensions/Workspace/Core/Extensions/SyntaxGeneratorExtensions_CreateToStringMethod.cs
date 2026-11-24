// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class SyntaxGeneratorExtensions
{
    private const string ToStringName = nameof(object.ToString);
    private const string PrintMembersName = WellKnownMemberNames.PrintMembersMethodName;
    private const string BuilderName = "builder";
    private const string AppendName = "Append";
    private const string EnsureSufficientExecutionStackName = "EnsureSufficientExecutionStack";

    /// <summary>
    /// Generates an override of <see cref="object.ToString()"/> matching the one the
    /// compiler synthesizes for records. For example, for <c>record Program(int a)</c>:
    /// <code>
    /// public override string ToString()
    /// {
    ///     StringBuilder builder = new StringBuilder();
    ///     builder.Append("Program");
    ///     builder.Append(" { ");
    ///     if (PrintMembers(builder))
    ///     {
    ///         builder.Append(' ');
    ///     }
    ///     builder.Append('}');
    ///     return builder.ToString();
    /// }
    /// </code>
    /// </summary>
    public static IMethodSymbol CreateToStringMethod(
        this SyntaxGenerator factory,
        SyntaxGeneratorInternal generatorInternal,
        Compilation compilation,
        INamedTypeSymbol containingType,
        bool isReadOnly)
    {
        var stringBuilderType = compilation.GetTypeByMetadataName("System.Text.StringBuilder");
        Contract.ThrowIfNull(stringBuilderType);

        var statements = CreateToStringMethodStatements(factory, generatorInternal, stringBuilderType, containingType);

        return CodeGenerationSymbolFactory.CreateMethodSymbol(
            attributes: default,
            accessibility: Accessibility.Public,
            modifiers: DeclarationModifiers.Override.WithIsReadOnly(isReadOnly),
            returnType: compilation.GetSpecialType(SpecialType.System_String),
            refKind: RefKind.None,
            explicitInterfaceImplementations: default,
            name: ToStringName,
            typeParameters: default,
            parameters: default,
            statements: statements);
    }

    private static ImmutableArray<SyntaxNode> CreateToStringMethodStatements(
        SyntaxGenerator factory,
        SyntaxGeneratorInternal generatorInternal,
        INamedTypeSymbol stringBuilderType,
        INamedTypeSymbol containingType)
    {
        using var statements = TemporaryArray<SyntaxNode>.Empty;

        var builderName = factory.IdentifierName(BuilderName);

        // StringBuilder builder = new StringBuilder();
        statements.Add(factory.SimpleLocalDeclarationStatement(
            generatorInternal, stringBuilderType, BuilderName,
            factory.ObjectCreationExpression(stringBuilderType)));

        // builder.Append("<TypeName>");
        statements.Add(CreateAppendStringCall(factory, builderName, containingType.Name));

        // builder.Append(" { ");
        statements.Add(CreateAppendStringCall(factory, builderName, " { "));

        // if (PrintMembers(builder)) builder.Append(' ');
        statements.Add(factory.IfStatement(
            CreatePrintMembersCall(factory, builderName),
            [CreateAppendCharCall(factory, builderName, ' ')]));

        // builder.Append('}');
        statements.Add(CreateAppendCharCall(factory, builderName, '}'));

        // return builder.ToString();
        statements.Add(factory.ReturnStatement(
            factory.InvocationExpression(
                factory.MemberAccessExpression(builderName, factory.IdentifierName(ToStringName)))));

        return statements.ToImmutableAndClear();
    }

    /// <summary>
    /// Generates a <c>bool PrintMembers(StringBuilder builder)</c> method matching the
    /// one the compiler synthesizes for records: it prints the supplied members in the
    /// form <c>"&lt;name&gt; = &lt;value&gt;"</c>, separated by <c>", "</c>, delegating to
    /// the base record's <c>PrintMembers</c> first when the record derives from another
    /// record, and returns <see langword="true"/> if any member was printed.
    /// </summary>
    public static IMethodSymbol CreatePrintMembersMethod(
        this SyntaxGenerator factory,
        Compilation compilation,
        INamedTypeSymbol containingType,
        ImmutableArray<ISymbol> members,
        bool isReadOnly)
    {
        var stringBuilderType = compilation.GetTypeByMetadataName("System.Text.StringBuilder");
        Contract.ThrowIfNull(stringBuilderType);

        var statements = CreatePrintMembersMethodStatements(
            factory, compilation, containingType, members);

        GetPrintMembersModifiers(containingType, out var accessibility, out var modifiers);

        return CodeGenerationSymbolFactory.CreateMethodSymbol(
            attributes: default,
            accessibility: accessibility,
            modifiers: modifiers.WithIsReadOnly(isReadOnly),
            returnType: compilation.GetSpecialType(SpecialType.System_Boolean),
            refKind: RefKind.None,
            explicitInterfaceImplementations: default,
            name: PrintMembersName,
            typeParameters: default,
            parameters: [CodeGenerationSymbolFactory.CreateParameterSymbol(stringBuilderType, BuilderName)],
            statements: statements);
    }

    private static void GetPrintMembersModifiers(
        INamedTypeSymbol containingType, out Accessibility accessibility, out DeclarationModifiers modifiers)
    {
        // Matches the modifiers of the synthesized PrintMembers method (see
        // SynthesizedRecordPrintMembers.MakeDeclarationModifiers):
        //
        //  - record struct: private
        //  - derived record: protected override
        //  - sealed record: private
        //  - otherwise: protected virtual
        if (containingType.IsValueType)
        {
            accessibility = Accessibility.Private;
            modifiers = DeclarationModifiers.None;
        }
        else if (containingType.BaseType is { } baseType && baseType.SpecialType != SpecialType.System_Object)
        {
            accessibility = Accessibility.Protected;
            modifiers = DeclarationModifiers.Override;
        }
        else if (containingType.IsSealed)
        {
            accessibility = Accessibility.Private;
            modifiers = DeclarationModifiers.None;
        }
        else
        {
            accessibility = Accessibility.Protected;
            modifiers = DeclarationModifiers.Virtual;
        }
    }

    private static ImmutableArray<SyntaxNode> CreatePrintMembersMethodStatements(
        SyntaxGenerator factory,
        Compilation compilation,
        INamedTypeSymbol containingType,
        ImmutableArray<ISymbol> members)
    {
        using var statements = TemporaryArray<SyntaxNode>.Empty;

        var builderName = factory.IdentifierName(BuilderName);
        var hasBaseRecord = !containingType.IsValueType &&
            containingType.BaseType is { } baseType && baseType.SpecialType != SpecialType.System_Object;

        if (hasBaseRecord)
        {
            var baseCall = CreateBasePrintMembersCall(factory, builderName);

            if (members.Length == 0)
            {
                // return base.PrintMembers(builder);
                statements.Add(factory.ReturnStatement(baseCall));
                return statements.ToImmutableAndClear();
            }

            // if (base.PrintMembers(builder)) builder.Append(", ");
            statements.Add(factory.IfStatement(
                baseCall,
                [CreateAppendStringCall(factory, builderName, ", ")]));
        }
        else if (members.Length == 0)
        {
            // return false;
            statements.Add(factory.ReturnStatement(factory.FalseLiteralExpression()));
            return statements.ToImmutableAndClear();
        }
        else if (!containingType.IsValueType)
        {
            // The compiler emits this guard in the synthesized PrintMembers of a record
            // whose base type is object, to fail fast with an insufficient execution
            // stack exception instead of overflowing the stack on recursive ToString
            // calls. It is only available on frameworks that define the method, so it
            // is omitted when the method cannot be found.
            var ensureStackCall = CreateEnsureSufficientExecutionStackCall(factory, compilation);
            if (ensureStackCall != null)
                statements.Add(ensureStackCall);
        }

        for (var i = 0; i < members.Length; i++)
        {
            var member = members[i];

            // builder.Append(", <name> = "); // if previous members exist
            // builder.Append("<name> = "); // if it is the first member
            var memberHeader = (i > 0 ? ", " : "") + member.Name + " = ";
            statements.Add(CreateAppendStringCall(factory, builderName, memberHeader));

            var memberAccess = factory.MemberAccessExpression(
                    factory.ThisExpression(), factory.IdentifierName(member.Name))
                .WithAdditionalAnnotations(Simplifier.Annotation);

            // builder.Append(<value>.ToString()); // for value types
            // builder.Append((object)<value>); // otherwise
            //
            // This matches the synthesized method: value types are printed via
            // ToString() as there is no StringBuilder.Append overload for arbitrary
            // value types, while reference types go through Append(object).
            var memberType = member.GetSymbolType();
            if (memberType?.IsValueType == true)
            {
                statements.Add(CreateAppendCall(factory, builderName,
                    factory.InvocationExpression(
                        factory.MemberAccessExpression(memberAccess, factory.IdentifierName(ToStringName)))));
            }
            else
            {
                statements.Add(CreateAppendCall(factory, builderName,
                    factory.CastExpression(compilation.GetSpecialType(SpecialType.System_Object), memberAccess)));
            }
        }

        statements.Add(factory.ReturnStatement(factory.TrueLiteralExpression()));

        return statements.ToImmutableAndClear();
    }

    private static SyntaxNode CreatePrintMembersCall(SyntaxGenerator factory, SyntaxNode builderName)
        => factory.InvocationExpression(
            factory.IdentifierName(PrintMembersName),
            builderName);

    private static SyntaxNode CreateBasePrintMembersCall(SyntaxGenerator factory, SyntaxNode builderName)
        => factory.InvocationExpression(
            factory.MemberAccessExpression(
                factory.BaseExpression(),
                factory.IdentifierName(PrintMembersName)),
            builderName);

    private static SyntaxNode? CreateEnsureSufficientExecutionStackCall(SyntaxGenerator factory, Compilation compilation)
    {
        var runtimeHelpersType = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.RuntimeHelpers");
        var ensureMethod = runtimeHelpersType?.GetMembers(EnsureSufficientExecutionStackName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m is { Parameters.Length: 0, IsStatic: true });

        if (runtimeHelpersType == null || ensureMethod == null)
            return null;

        return factory.ExpressionStatement(
            factory.InvocationExpression(
                factory.MemberAccessExpression(
                    factory.TypeExpression(runtimeHelpersType),
                    factory.IdentifierName(EnsureSufficientExecutionStackName))));
    }

    private static SyntaxNode CreateAppendStringCall(SyntaxGenerator factory, SyntaxNode builderName, string value)
        => factory.ExpressionStatement(
            factory.InvocationExpression(
                factory.MemberAccessExpression(builderName, AppendName),
                factory.LiteralExpression(value)));

    private static SyntaxNode CreateAppendCharCall(SyntaxGenerator factory, SyntaxNode builderName, char value)
        => factory.ExpressionStatement(
            factory.InvocationExpression(
                factory.MemberAccessExpression(builderName, AppendName),
                factory.LiteralExpression(value)));

    private static SyntaxNode CreateAppendCall(SyntaxGenerator factory, SyntaxNode builderName, SyntaxNode value)
        => factory.ExpressionStatement(
            factory.InvocationExpression(
                factory.MemberAccessExpression(builderName, AppendName),
                value));
}
