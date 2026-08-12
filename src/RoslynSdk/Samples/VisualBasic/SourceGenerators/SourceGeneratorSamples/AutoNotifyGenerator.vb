Option Explicit On
Option Infer On
Option Strict On

Imports System.Text

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#Disable Warning RS1035 ' Do not use APIs banned for analyzers

Namespace SourceGeneratorSamples

    <Generator(LanguageNames.VisualBasic)>
    Public Class AutoNotifyGenerator
        Implements ISourceGenerator

        Private Const ATTRIBUTE_TEXT As String = "
Imports System

Namespace Global.AutoNotify
    <AttributeUsage(AttributeTargets.Field, Inherited:=False, AllowMultiple:=False)>
    Friend NotInheritable Class AutoNotifyAttribute
        Inherits Attribute

        Public Sub New()
        End Sub

        Public Property PropertyName As String
    End Class
End Namespace
"

        Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
            ' Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(Function() As ISyntaxReceiver
                                                       Return New SyntaxReceiver
                                                   End Function)
        End Sub

        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute

            ' add the attribute text
            context.AddSource("AutoNotifyAttribute.g.vb", SourceText.From(ATTRIBUTE_TEXT, Encoding.UTF8))

            ' retreive the populated receiver 
            Dim tempVar = TypeOf context.SyntaxReceiver Is SyntaxReceiver
            Dim receiver = TryCast(context.SyntaxReceiver, SyntaxReceiver)
            If Not tempVar Then
                Return
            End If

            ' we're going to create a new compilation that contains the attribute.
            ' TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            Dim options1 = context.Compilation.SyntaxTrees.First().Options
            Dim compilation1 = context.Compilation.AddSyntaxTrees(VisualBasicSyntaxTree.ParseText(SourceText.From(ATTRIBUTE_TEXT, Encoding.UTF8), CType(options1, VisualBasicParseOptions)))

            ' get the newly bound attribute, and INotifyPropertyChanged
            Dim attributeSymbol = compilation1.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute")
            Dim notifySymbol = compilation1.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged")

            ' loop over the candidate fields, and keep the ones that are actually annotated
            Dim fieldSymbols As New List(Of IFieldSymbol)

            For Each field In receiver.CandidateFields
                Dim model = compilation1.GetSemanticModel(field.SyntaxTree)
                For Each variable In field.Declarators
                    For Each name In variable.Names
                        ' Get the symbol being decleared by the field, and keep it if its annotated
                        Dim fieldSymbol = TryCast(model.GetDeclaredSymbol(name), IFieldSymbol)
                        If fieldSymbol.GetAttributes().Any(Function(ad) ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.[Default])) Then
                            fieldSymbols.Add(fieldSymbol)
                        End If
                    Next
                Next
            Next

            ' group the fields by class, and generate the source
            For Each group In fieldSymbols.GroupBy(Function(f) f.ContainingType, SymbolEqualityComparer.Default)
                Dim classSource = ProcessClass(CType(group.Key, INamedTypeSymbol), group.ToList(), attributeSymbol, notifySymbol)
                context.AddSource($"{group.Key.Name}_AutoNotify.g.vb", SourceText.From(classSource, Encoding.UTF8))
            Next

        End Sub

        Private Function ProcessClass(classSymbol As INamedTypeSymbol, fields As List(Of IFieldSymbol), attributeSymbol As ISymbol, notifySymbol As ISymbol) As String

            If Not classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.[Default]) Then
                Return Nothing 'TODO: issue a diagnostic that it must be top level
            End If

            Dim namespaceName = classSymbol.ContainingNamespace.ToDisplayString()

            ' begin building the generated source
            Dim source = New StringBuilder($"Option Explicit On
Option Strict On
Option Infer On

Namespace Global.{namespaceName}

  Partial Public Class {classSymbol.Name}
    Implements {notifySymbol.ToDisplayString()}

")

            ' if the class doesn't implement INotifyPropertyChanged already, add it
            If Not classSymbol.Interfaces.Contains(CType(notifySymbol, INamedTypeSymbol)) Then
                source.Append("    Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
")
            End If

            ' create properties for each field 
            For Each fieldSymbol In fields
                ProcessField(source, fieldSymbol, attributeSymbol)
            Next

            source.Append("
  End Class

End Namespace")

            Return source.ToString()

        End Function

        Private Sub ProcessField(source As StringBuilder, fieldSymbol As IFieldSymbol, attributeSymbol As ISymbol)

            Dim chooseName As Func(Of String, TypedConstant, String) =
        Function(fieldName1 As String, overridenNameOpt1 As TypedConstant) As String

            If Not overridenNameOpt1.IsNull Then
                Return overridenNameOpt1.Value.ToString()
            End If

            fieldName1 = fieldName1.TrimStart("_"c)
            If fieldName1.Length = 0 Then
                Return String.Empty
            End If

            If fieldName1.Length = 1 Then
                Return fieldName1.ToUpper()
            End If

            Return fieldName1.Substring(0, 1).ToUpper() & fieldName1.Substring(1)

        End Function

            ' get the name and type of the field
            Dim fieldName = fieldSymbol.Name
            Dim fieldType = fieldSymbol.Type

            ' get the AutoNotify attribute from the field, and any associated data
            Dim attributeData = fieldSymbol.GetAttributes().[Single](Function(ad) ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.[Default]))
            Dim overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(Function(kvp) kvp.Key = "PropertyName").Value

            Dim propertyName = chooseName(fieldName, overridenNameOpt)
            If propertyName.Length = 0 OrElse propertyName = fieldName Then
                'TODO: issue a diagnostic that we can't process this field
                Return
            End If

            source.Append($"
    Public Property {propertyName} As {fieldType}
      Get
        Return Me.{fieldName}
      End Get
      Set(value As {fieldType})
        Me.{fieldName} = value
        RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(NameOf({propertyName})))
      End Set
    End Property
")

        End Sub

        ''' <summary>
        ''' Created on demand before each generation pass
        ''' </summary>
        Class SyntaxReceiver
            Implements ISyntaxReceiver

            Public ReadOnly Property CandidateFields As List(Of FieldDeclarationSyntax) = New List(Of FieldDeclarationSyntax)

            ''' <summary>
            ''' Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            ''' </summary>
            Public Sub OnVisitSyntaxNode(syntaxNode As SyntaxNode) Implements ISyntaxReceiver.OnVisitSyntaxNode
                ' any field with at least one attribute is a candidate for property generation
                If TypeOf syntaxNode Is FieldDeclarationSyntax Then
                    Dim fieldDeclarationSyntax = TryCast(syntaxNode, FieldDeclarationSyntax)
                    If fieldDeclarationSyntax.AttributeLists.Count > 0 Then
                        CandidateFields.Add(fieldDeclarationSyntax)
                    End If
                End If
            End Sub

        End Class

    End Class

End Namespace
