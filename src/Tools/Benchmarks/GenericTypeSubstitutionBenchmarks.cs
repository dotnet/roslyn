// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Basic.Reference.Assemblies;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Benchmarks;

public enum GenericTypeSubstitutionCase
{
    NoChange0,
    NoChange1,
    NoChange2,
    NoChange8,
    FirstChanged1,
    FirstChanged2,
    FirstChanged8,
    LastChanged2,
    LastChanged8,
    ContainingTypeChanged0,
    ContainingTypeChanged1,
    ContainingTypeChanged2,
    ContainingTypeChanged8,
    NestedArgumentChanged,
}

[MemoryDiagnoser]
public class GenericTypeSubstitutionBenchmarks
{
    private TypeMap _map = null!;
    private NamedTypeSymbol _type = null!;

    [ParamsAllValues]
    public GenericTypeSubstitutionCase Case { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var arity = Case switch
        {
            GenericTypeSubstitutionCase.NoChange0 or GenericTypeSubstitutionCase.ContainingTypeChanged0 => 0,
            GenericTypeSubstitutionCase.NoChange1 or GenericTypeSubstitutionCase.FirstChanged1 or
                GenericTypeSubstitutionCase.ContainingTypeChanged1 => 1,
            GenericTypeSubstitutionCase.NoChange2 or GenericTypeSubstitutionCase.FirstChanged2 or
                GenericTypeSubstitutionCase.LastChanged2 or GenericTypeSubstitutionCase.ContainingTypeChanged2 or
                GenericTypeSubstitutionCase.NestedArgumentChanged => 2,
            GenericTypeSubstitutionCase.NoChange8 or GenericTypeSubstitutionCase.FirstChanged8 or
                GenericTypeSubstitutionCase.LastChanged8 or GenericTypeSubstitutionCase.ContainingTypeChanged8 => 8,
            _ => throw new InvalidOperationException(),
        };
        var typeParameters = arity == 0
            ? ""
            : "<" + string.Join(", ", Enumerable.Range(0, arity).Select(i => $"T{i}")) + ">";
        var source = $$"""
            public class Parameters<T> { }
            public class G{{typeParameters}} { }
            public class Outer<T>
            {
                public class Inner{{typeParameters}} { }
            }
            """;
        var compilation = CSharpCompilation.Create(
            nameof(GenericTypeSubstitutionBenchmarks),
            [CSharpSyntaxTree.ParseText(SourceText.From(source))],
            Net90.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(d => d.ToString())));
        }

        var parameter = compilation.GlobalNamespace.GetTypeMembers("Parameters").Single().TypeParameters.Single();
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        _map = new TypeMap(
            ImmutableArray.Create(parameter),
            ImmutableArray.Create(TypeWithAnnotations.Create(intType)));

        var definition = compilation.GlobalNamespace.GetTypeMembers("G").Single();
        if (Case is GenericTypeSubstitutionCase.ContainingTypeChanged0 or
            GenericTypeSubstitutionCase.ContainingTypeChanged1 or
            GenericTypeSubstitutionCase.ContainingTypeChanged2 or
            GenericTypeSubstitutionCase.ContainingTypeChanged8)
        {
            definition = compilation.GlobalNamespace.GetTypeMembers("Outer").Single()
                .Construct(parameter).GetTypeMembers("Inner").Single();
        }

        var arguments = Enumerable.Repeat<TypeSymbol>(stringType, arity).ToArray();
        switch (Case)
        {
            case GenericTypeSubstitutionCase.FirstChanged1:
            case GenericTypeSubstitutionCase.FirstChanged2:
            case GenericTypeSubstitutionCase.FirstChanged8:
                arguments[0] = parameter;
                break;
            case GenericTypeSubstitutionCase.LastChanged2:
            case GenericTypeSubstitutionCase.LastChanged8:
                arguments[arity - 1] = parameter;
                break;
            case GenericTypeSubstitutionCase.NestedArgumentChanged:
                arguments[0] = definition.Construct(parameter, stringType);
                break;
        }

        _type = arity == 0 ? definition : definition.Construct(arguments);
    }

    [Benchmark]
    public object Substitute() => _map.SubstituteNamedType(_type);
}
