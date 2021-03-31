// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static class SyntaxExtensions
    {
        //TODO; to make this work, you need a strongly typed SyntaxContext
        //internal static ISingleValueSource<T> FilterSyntaxByType<T>(this Scratch.SyntaxNodeValueSource source) where T : SyntaxNode => throw null;
    }

    class Scratch
    {
        public class SyntaxNodeValueSource
        {
            public IncrementalValueSource<T> CreateSyntaxTransform<T>(Func<GeneratorSyntaxContext, IEnumerable<T>> func) => throw null!;

            public IncrementalValueSource<GeneratorSyntaxContext> CreateSyntaxFilter(Func<GeneratorSyntaxContext, bool> applies) => throw null!;
        }



        //class AdditionalTextSource : SingleValueSource<IEnumerable<AdditionalText>> { }

        //class CompilationSource : SingleValueSource<Compilation> { }

        public static void M()
        {
            SyntaxNodeValueSource nodeValueSource = null!;
            ValueSources sources = null!;


            //var fields = nodeValueSource.Filter(ctx => ctx.Node is FieldDeclarationSyntax fds && fds.AttributeLists.Count > 0);
            var fields = nodeValueSource.CreateSyntaxTransform(SelectFieldSymbols);


            var fields2 = nodeValueSource.CreateSyntaxFilter(ctx => ctx.Node is FieldDeclarationSyntax fds && fds.AttributeLists.Count > 0);


            var fields3 = fields2.TransformMany(ctx => (((FieldDeclarationSyntax)ctx.Node).Declaration.Variables.Select(v => (v, ctx.SemanticModel))));



            var notifyAttribute = sources.CompilationSource.Transform(c => c.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute"));

            var collectionOfNotify = notifyAttribute.BatchTransform(t => t);
            var streamOfNotify = collectionOfNotify.BatchTransformMany(t => t);

            var stringFields = fields.Join(notifyAttribute).TransformMany(input => GenerateField(input.Item1, input.Item2));

            var fieldsByClass = stringFields.BatchTransformMany(fields => fields.GroupBy(f => f.field.ContainingType, f => f.output));

            var producer = fieldsByClass.GenerateSource((p, group) =>
            {
                string s = "start class";
                foreach (var field in group)
                {
                    s += field;
                }
                s += "end class";

                //p.AddSource(s);

            });

            //context.Add(producer);



            //var fields2 = nodeValueSource.Filter(ctx => ctx.Node is FieldDeclarationSyntax fds && fds.AttributeLists.Count > 0)
            //                             .Transform(ctx => (ctx.SemanticModel, (IEnumerable<VariableDeclaratorSyntax>)(((FieldDeclarationSyntax)ctx.Node).Declaration.Variables)));


        }


        private static IEnumerable<IFieldSymbol> SelectFieldSymbols(GeneratorSyntaxContext context)
        {
            if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
            {
                foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                {
                    // Get the symbol being declared by the field, and keep it if its annotated
                    IFieldSymbol? fieldSymbol = (IFieldSymbol?)context.SemanticModel.GetDeclaredSymbol(variable);
                    if (fieldSymbol?.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "AutoNotify.AutoNotifyAttribute") == true)
                    {
                        yield return fieldSymbol;
                    }
                }
            }
        }

        private static IEnumerable<(IFieldSymbol field, string output)> GenerateField(IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                yield break;
            }

            yield return (fieldSymbol, $@"
public {fieldType} {propertyName} 
{{
    get 
    {{
        return this.{fieldName};
    }}
    set
    {{
        this.{fieldName} = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof({propertyName})));
    }}
}}");

            string chooseName(string fieldName, TypedConstant overridenNameOpt)
            {
                if (!overridenNameOpt.IsNull)
                {
                    return overridenNameOpt.Value.ToString();
                }

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }
        }





        static void M2(GeneratorInitializationContext initContext)
        {
            initContext.RegisterForPipelineExecution((pipelineContext) =>
            {

                var input = sources.SyntaxTrees;
                var named = input.Transform(t => t.FilePath);

                var generate = named.GenerateSourceBatch((context, names) =>
                {
                    StringBuilder sourceBuilder = new StringBuilder(@"
using System;
namespace HelloWorldGenerated
{
    public static class HelloWorld
    {
        public static void SayHello() 
        {
            Console.WriteLine(""Hello from generated code!"");
            Console.WriteLine(""The following syntax trees existed in the compilation that created this program:"");
");
                    foreach (var name in names)
                    {
                        sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {name}"");");
                    }

                    // finish creating the source to inject
                    sourceBuilder.Append(@"
        }
    }
}");

                    context.AddSource("helloWorldGenerated", sourceBuilder.ToString());
                });

                pipelineContext.AddProducer(generate);
            });
        }



    }


    //public static IValueSource<GeneratorSyntaxContext> FilterBySyntaxType<T>(this IValueSource<GeneratorSyntaxContext> source) where T : SyntaxNode
    //{



    //    return 
    //}
}
