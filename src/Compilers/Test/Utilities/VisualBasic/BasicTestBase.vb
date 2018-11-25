' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Xunit

Public MustInherit Class BasicTestBase
    Inherits CommonTestBase

    Protected Overloads Function GetCompilationForEmit(
        source As IEnumerable(Of String),
        additionalRefs() As MetadataReference,
        options As VisualBasicCompilationOptions,
        parseOptions As VisualBasicParseOptions
    ) As VisualBasicCompilation
        Return DirectCast(MyClass.GetCompilationForEmit(source, additionalRefs, options, parseOptions), VisualBasicCompilation)
    End Function

    Public Function XCDataToString(Optional data As XCData = Nothing) As String
        Return data?.Value.Replace(vbLf, Environment.NewLine)
    End Function

    Private Function Translate(action As Action(Of ModuleSymbol)) As Action(Of IModuleSymbol)
        If action IsNot Nothing Then
            Return Sub(m) action(DirectCast(m, ModuleSymbol))
        Else
            Return Nothing
        End If
    End Function

    Friend Shadows Function CompileAndVerify(
        source As XElement,
        expectedOutput As XCData,
        Optional expectedReturnCode As Integer? = Nothing,
        Optional args As String() = Nothing,
        Optional references As MetadataReference() = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional emitOptions As EmitOptions = Nothing,
        Optional verify As Verification = Verification.Passes
    ) As CompilationVerifier

        Return CompileAndVerify(
            source,
            XCDataToString(expectedOutput),
            expectedReturnCode,
            args,
            references,
            dependencies,
            sourceSymbolValidator,
            validator,
            symbolValidator,
            expectedSignatures,
            options,
            parseOptions,
            emitOptions,
            verify)
    End Function

    Friend Shadows Function CompileAndVerify(
        compilation As Compilation,
        Optional manifestResources As IEnumerable(Of ResourceDescription) = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional expectedOutput As String = Nothing,
        Optional expectedReturnCode As Integer? = Nothing,
        Optional args As String() = Nothing,
        Optional emitOptions As EmitOptions = Nothing,
        Optional verify As Verification = Verification.Passes) As CompilationVerifier

        Return MyBase.CompileAndVerifyCommon(
            compilation,
            manifestResources,
            dependencies,
            Translate(sourceSymbolValidator),
            validator,
            Translate(symbolValidator),
            expectedSignatures,
            expectedOutput,
            expectedReturnCode,
            args,
            emitOptions,
            verify)
    End Function

    Friend Shadows Function CompileAndVerify(
        compilation As Compilation,
        expectedOutput As XCData,
        Optional args As String() = Nothing,
        Optional manifestResources As IEnumerable(Of ResourceDescription) = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional emitOptions As EmitOptions = Nothing,
        Optional verify As Verification = Verification.Passes) As CompilationVerifier

        Return CompileAndVerify(
            compilation,
            manifestResources,
            dependencies,
            sourceSymbolValidator,
            validator,
            symbolValidator,
            expectedSignatures,
            XCDataToString(expectedOutput),
            Nothing,
            args,
            emitOptions,
            verify)
    End Function

    Friend Shadows Function CompileAndVerify(
        source As XElement,
        Optional expectedOutput As String = Nothing,
        Optional expectedReturnCode As Integer? = Nothing,
        Optional args As String() = Nothing,
        Optional references As MetadataReference() = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional emitOptions As EmitOptions = Nothing,
        Optional verify As Verification = Verification.Passes,
        Optional useLatestFramework As Boolean = False
    ) As CompilationVerifier

        Dim defaultRefs = If(useLatestFramework, LatestVbReferences, DefaultVbReferences)
        Dim allReferences = If(references IsNot Nothing, defaultRefs.Concat(references), defaultRefs)

        Return Me.CompileAndVerify(source,
                                   allReferences,
                                   expectedOutput,
                                   expectedReturnCode,
                                   args,
                                   dependencies,
                                   sourceSymbolValidator,
                                   validator,
                                   symbolValidator,
                                   expectedSignatures,
                                   options,
                                   parseOptions,
                                   emitOptions,
                                   verify)

    End Function

    Friend Shadows Function CompileAndVerify(
        source As XElement,
        allReferences As IEnumerable(Of MetadataReference),
        Optional expectedOutput As String = Nothing,
        Optional expectedReturnCode As Integer? = Nothing,
        Optional args As String() = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional emitOptions As EmitOptions = Nothing,
        Optional verify As Verification = Verification.Passes
    ) As CompilationVerifier

        If options Is Nothing Then
            options = If(expectedOutput Is Nothing, TestOptions.ReleaseDll, TestOptions.ReleaseExe)
        End If

        Dim assemblyName As String = Nothing
        Dim sourceTrees = ParseSourceXml(source, parseOptions, assemblyName)
        Dim compilation = CreateEmptyCompilation(sourceTrees.ToArray(), allReferences, options, assemblyName:=assemblyName)

        Return MyBase.CompileAndVerifyCommon(
            compilation,
            Nothing,
            dependencies,
            Translate(sourceSymbolValidator),
            validator,
            Translate(symbolValidator),
            expectedSignatures,
            expectedOutput,
            expectedReturnCode,
            args,
            emitOptions,
            verify)
    End Function

    Friend Shadows Function CompileAndVerify(
        source As String,
        allReferences As IEnumerable(Of MetadataReference),
        Optional expectedOutput As String = Nothing,
        Optional expectedReturnCode As Integer? = Nothing,
        Optional args As String() = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional emitOptions As EmitOptions = Nothing,
        Optional assemblyName As String = Nothing,
        Optional verify As Verification = Verification.Passes
    ) As CompilationVerifier

        If options Is Nothing Then
            options = If(expectedOutput Is Nothing, TestOptions.ReleaseDll, TestOptions.ReleaseExe)
        End If

        Dim compilation = CreateEmptyCompilation(source, allReferences, options, parseOptions, assemblyName)

        Return MyBase.CompileAndVerifyCommon(
            compilation,
            Nothing,
            dependencies,
            Translate(sourceSymbolValidator),
            validator,
            Translate(symbolValidator),
            expectedSignatures,
            expectedOutput,
            expectedReturnCode,
            args,
            emitOptions,
            verify)
    End Function

    Friend Shadows Function CompileAndVerifyOnWin8Only(
        source As XElement,
        allReferences As IEnumerable(Of MetadataReference),
        Optional expectedOutput As String = Nothing,
        Optional expectedReturnCode As Integer? = Nothing,
        Optional args As String() = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional verify As Verification = Verification.Passes
    ) As CompilationVerifier
        Return Me.CompileAndVerify(
            source,
            allReferences,
            If(OSVersion.IsWin8, expectedOutput, Nothing),
            If(OSVersion.IsWin8, expectedReturnCode, Nothing),
            args,
            dependencies,
            sourceSymbolValidator,
            validator,
            symbolValidator,
            expectedSignatures,
            options,
            parseOptions,
            verify:=If(OSVersion.IsWin8, verify, Verification.Skipped))
    End Function

    Friend Shadows Function CompileAndVerifyOnWin8Only(
        source As XElement,
        expectedOutput As XCData,
        Optional expectedReturnCode As Integer? = Nothing,
        Optional args As String() = Nothing,
        Optional allReferences() As MetadataReference = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional verify As Verification = Verification.Passes
    ) As CompilationVerifier
        Return CompileAndVerifyOnWin8Only(
            source,
            allReferences,
            XCDataToString(expectedOutput),
            expectedReturnCode,
            args,
            dependencies,
            sourceSymbolValidator,
            validator,
            symbolValidator,
            expectedSignatures,
            options,
            parseOptions,
            verify)
    End Function

    Friend Shadows Function CompileAndVerifyOnWin8Only(
        source As XElement,
        Optional expectedOutput As String = Nothing,
        Optional references() As MetadataReference = Nothing,
        Optional dependencies As IEnumerable(Of ModuleData) = Nothing,
        Optional sourceSymbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional validator As Action(Of PEAssembly) = Nothing,
        Optional symbolValidator As Action(Of ModuleSymbol) = Nothing,
        Optional expectedSignatures As SignatureDescription() = Nothing,
        Optional options As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional verify As Verification = Verification.Passes,
        Optional useLatestFramework As Boolean = False
    ) As CompilationVerifier
        Return CompileAndVerify(
            source,
            expectedOutput:=If(OSVersion.IsWin8, expectedOutput, Nothing),
            references:=references,
            dependencies:=dependencies,
            sourceSymbolValidator:=sourceSymbolValidator,
            validator:=validator,
            symbolValidator:=symbolValidator,
            expectedSignatures:=expectedSignatures,
            options:=options,
            parseOptions:=parseOptions,
            verify:=If(OSVersion.IsWin8, verify, Verification.Skipped),
            useLatestFramework:=useLatestFramework)
    End Function

    ''' <summary>
    ''' Compile sources and adds a custom reference using a custom IL
    ''' </summary>
    ''' <param name="source">The sources compile according to the following schema
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    Friend Function CompileWithCustomILSource(source As XElement, ilSource As XCData) As CompilationVerifier
        Return CompileWithCustomILSource(source, ilSource.Value)
    End Function

    ''' <summary>
    ''' Compile sources and adds a custom reference using a custom IL
    ''' </summary>
    ''' <param name="source">The sources compile according to the following schema
    ''' &lt;compilation name="assemblyname[optional]"&gt;
    ''' &lt;file name="file1.vb[optional]"&gt;
    ''' source
    ''' &lt;/file&gt;
    ''' &lt;/compilation&gt;
    ''' </param>
    Friend Function CompileWithCustomILSource(source As XElement, ilSource As String,
                                              Optional options As VisualBasicCompilationOptions = Nothing,
                                              Optional compilationVerifier As Action(Of VisualBasicCompilation) = Nothing,
                                              Optional expectedOutput As String = Nothing) As CompilationVerifier
        If expectedOutput IsNot Nothing Then
            options = options.WithOutputKind(OutputKind.ConsoleApplication)
        End If

        If ilSource = Nothing Then
            Return CompileAndVerify(source)
        End If

        Dim reference As MetadataReference = Nothing
        Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
            reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
        End Using

        Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef, MsvbRef, reference}, options)

        If compilationVerifier IsNot Nothing Then
            compilationVerifier(compilation)
        End If


        Return CompileAndVerify(compilation, expectedOutput:=expectedOutput)
    End Function

    Friend Overloads Function CompileAndVerifyFieldMarshal(source As String,
                                                           expectedBlobs As Dictionary(Of String, Byte()),
                                                           Optional getExpectedBlob As Func(Of String, PEAssembly, Byte()) = Nothing,
                                                           Optional expectedSignatures As SignatureDescription() = Nothing,
                                                           Optional isField As Boolean = True) As CompilationVerifier
        Dim xmlSource = <compilation><field><%= source %></field></compilation>
        Return CompileAndVerifyFieldMarshal(xmlSource, expectedBlobs, getExpectedBlob, expectedSignatures, isField)
    End Function

    Friend Overloads Function CompileAndVerifyFieldMarshal(source As XElement,
                                                           expectedBlobs As Dictionary(Of String, Byte()),
                                                           Optional getExpectedBlob As Func(Of String, PEAssembly, Byte()) = Nothing,
                                                           Optional expectedSignatures As SignatureDescription() = Nothing,
                                                           Optional isField As Boolean = True) As CompilationVerifier
        Return CompileAndVerifyFieldMarshal(source,
                                            Function(s, _omitted1)
                                                Assert.True(expectedBlobs.ContainsKey(s), "Expecting marshalling blob for " & If(isField, "field ", "parameter ") & s)
                                                Return expectedBlobs(s)
                                            End Function,
                                            expectedSignatures,
                                            isField)
    End Function

    Friend Overloads Function CompileAndVerifyFieldMarshal(source As XElement,
                                                           getExpectedBlob As Func(Of String, PEAssembly, Byte()),
                                                           Optional expectedSignatures As SignatureDescription() = Nothing,
                                                           Optional isField As Boolean = True) As CompilationVerifier
        Return CompileAndVerify(source,
                                options:=TestOptions.ReleaseDll,
                                validator:=Sub(assembly) MetadataValidation.MarshalAsMetadataValidator(assembly, getExpectedBlob, isField),
                                expectedSignatures:=expectedSignatures)
    End Function

    Public Shared Function CreateSubmission(code As String,
                                            Optional references As IEnumerable(Of MetadataReference) = Nothing,
                                            Optional options As VisualBasicCompilationOptions = Nothing,
                                            Optional parseOptions As VisualBasicParseOptions = Nothing,
                                            Optional previous As VisualBasicCompilation = Nothing,
                                            Optional returnType As Type = Nothing,
                                            Optional hostObjectType As Type = Nothing) As VisualBasicCompilation
        Return VisualBasicCompilation.CreateScriptCompilation(
                GetUniqueName(),
                references:=If(references Is Nothing, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}.Concat(references)),
                options:=options,
                syntaxTree:=Parse(code, options:=If(parseOptions, TestOptions.Script)),
                previousScriptCompilation:=previous,
                returnType:=returnType,
                globalsType:=hostObjectType)
    End Function

    Friend Shared Function GetAttributeNames(attributes As ImmutableArray(Of SynthesizedAttributeData)) As IEnumerable(Of String)
        Return attributes.Select(Function(a) a.AttributeClass.Name)
    End Function

    Friend Shared Function GetAttributeNames(attributes As ImmutableArray(Of VisualBasicAttributeData)) As IEnumerable(Of String)
        Return attributes.Select(Function(a) a.AttributeClass.Name)
    End Function

    Friend Overrides Function VisualizeRealIL(peModule As IModuleSymbol, methodData As CompilationTestData.MethodData, markers As IReadOnlyDictionary(Of Integer, String)) As String
        Throw New NotImplementedException()
    End Function

    Friend Function GetSymbolsFromBinaryReference(bytes() As Byte) As AssemblySymbol
        Return MetadataTestHelpers.GetSymbolsForReferences({bytes}).Single()
    End Function

    Public Shared Shadows Function GetPdbXml(compilation As VisualBasicCompilation, Optional methodName As String = "") As XElement
        Return XElement.Parse(PdbValidation.GetPdbXml(compilation, qualifiedMethodName:=methodName))
    End Function

    Public Shared Shadows Function GetPdbXml(source As XElement, Optional options As VisualBasicCompilationOptions = Nothing, Optional methodName As String = "") As XElement
        Dim compilation = CreateCompilationWithMscorlib40(source, options:=options)
        compilation.VerifyDiagnostics()
        Return GetPdbXml(compilation, methodName)
    End Function

    Public Shared Shadows Function GetSequencePoints(pdbXml As XElement) As XElement
        Return <sequencePoints>
                   <%= From entry In pdbXml.<methods>.<method>.<sequencePoints>.<entry>
                       Select <entry
                                  startLine=<%= entry.@startLine %>
                                  startColumn=<%= entry.@startColumn %>
                                  endLine=<%= entry.@endLine %>
                                  endColumn=<%= entry.@endColumn %>/> %>
               </sequencePoints>
    End Function

    Public Shared ReadOnly ClassesWithReadWriteProperties As XCData = <![CDATA[
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual
          instance int32  get_P_rw_r_w() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method B::get_P_rw_r_w

  .method public hidebysig newslot specialname virtual
          instance void  set_P_rw_r_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method B::set_P_rw_r_w

  .method public hidebysig newslot specialname virtual
          instance int32  get_P_rw_rw_w() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method B::get_P_rw_rw_w

  .method public hidebysig newslot specialname virtual
          instance void  set_P_rw_rw_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method B::set_P_rw_rw_w

  .method public hidebysig newslot specialname virtual
          instance int32  get_P_rw_rw_r() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method B::get_P_rw_rw_r

  .method public hidebysig newslot specialname virtual
          instance void  set_P_rw_rw_r(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method B::set_P_rw_rw_r

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

  .property instance int32 P_rw_r_w()
  {
    .get instance int32 B::get_P_rw_r_w()
    .set instance void B::set_P_rw_r_w(int32)
  } // end of property B::P_rw_r_w
  .property instance int32 P_rw_rw_w()
  {
    .set instance void B::set_P_rw_rw_w(int32)
    .get instance int32 B::get_P_rw_rw_w()
  } // end of property B::P_rw_rw_w
  .property instance int32 P_rw_rw_r()
  {
    .get instance int32 B::get_P_rw_rw_r()
    .set instance void B::set_P_rw_rw_r(int32)
  } // end of property B::P_rw_rw_r
} // end of class B

.class public auto ansi beforefieldinit D1
       extends B
{
  .method public hidebysig specialname virtual
          instance int32  get_P_rw_r_w() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method D1::get_P_rw_r_w

  .method public hidebysig specialname virtual
          instance int32  get_P_rw_rw_w() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method D1::get_P_rw_rw_w

  .method public hidebysig specialname virtual
          instance void  set_P_rw_rw_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method D1::set_P_rw_rw_w

  .method public hidebysig specialname virtual
          instance int32  get_P_rw_rw_r() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method D1::get_P_rw_rw_r

  .method public hidebysig specialname virtual
          instance void  set_P_rw_rw_r(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method D1::set_P_rw_rw_r

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void B::.ctor()
    IL_0006:  ret
  } // end of method D1::.ctor

  .property instance int32 P_rw_r_w()
  {
    .get instance int32 D1::get_P_rw_r_w()
  } // end of property D1::P_rw_r_w
  .property instance int32 P_rw_rw_w()
  {
    .get instance int32 D1::get_P_rw_rw_w()
    .set instance void D1::set_P_rw_rw_w(int32)
  } // end of property D1::P_rw_rw_w
  .property instance int32 P_rw_rw_r()
  {
    .get instance int32 D1::get_P_rw_rw_r()
    .set instance void D1::set_P_rw_rw_r(int32)
  } // end of property D1::P_rw_rw_r
} // end of class D1

.class public auto ansi beforefieldinit D2
       extends D1
{
  .method public hidebysig specialname virtual
          instance void  set_P_rw_r_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method D2::set_P_rw_r_w

  .method public hidebysig specialname virtual
          instance void  set_P_rw_rw_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method D2::set_P_rw_rw_w

  .method public hidebysig specialname virtual
          instance int32  get_P_rw_rw_r() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method D2::get_P_rw_rw_r

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void D1::.ctor()
    IL_0006:  ret
  } // end of method D2::.ctor

  .property instance int32 P_rw_r_w()
  {
    .set instance void D2::set_P_rw_r_w(int32)
  } // end of property D2::P_rw_r_w
  .property instance int32 P_rw_rw_w()
  {
    .set instance void D2::set_P_rw_rw_w(int32)
  } // end of property D2::P_rw_rw_w
  .property instance int32 P_rw_rw_r()
  {
    .get instance int32 D2::get_P_rw_rw_r()
  } // end of property D2::P_rw_rw_r
} // end of class D2
]]>

    Public Class NameSyntaxFinder
        Inherits VisualBasicSyntaxWalker

        Private Sub New()
            MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
        End Sub

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            Dim name = TryCast(node, NameSyntax)
            If name IsNot Nothing Then
                Me._names.Add(name)
            End If

            MyBase.DefaultVisit(node)
        End Sub

        Private ReadOnly _names As New List(Of NameSyntax)

        Public Shared Function FindNames(node As SyntaxNode) As List(Of NameSyntax)
            Dim finder As New NameSyntaxFinder()
            finder.Visit(node)
            Return finder._names
        End Function
    End Class

    Public Class ExpressionSyntaxFinder
        Inherits VisualBasicSyntaxWalker

        Private Sub New()
            MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
        End Sub

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            Dim expr = TryCast(node, ExpressionSyntax)
            If expr IsNot Nothing Then
                Me._expressions.Add(expr)
            End If

            MyBase.DefaultVisit(node)
        End Sub

        Private ReadOnly _expressions As New List(Of ExpressionSyntax)

        Public Shared Function FindExpression(node As SyntaxNode) As List(Of ExpressionSyntax)
            Dim finder As New ExpressionSyntaxFinder()
            finder.Visit(node)
            Return finder._expressions
        End Function
    End Class

    Public Class SyntaxNodeFinder
        Inherits VisualBasicSyntaxWalker

        Private Sub New()
            MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
        End Sub

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            If node IsNot Nothing AndAlso Me._kinds.Contains(node.Kind) Then
                Me._nodes.Add(node)
            End If

            MyBase.DefaultVisit(node)
        End Sub

        Private ReadOnly _nodes As New List(Of SyntaxNode)
        Private ReadOnly _kinds As New HashSet(Of SyntaxKind)(SyntaxFacts.EqualityComparer)

        Public Shared Function FindNodes(Of T As SyntaxNode)(node As SyntaxNode, ParamArray kinds() As SyntaxKind) As List(Of T)
            Return New List(Of T)(From s In FindNodes(node, kinds) Select DirectCast(s, T))
        End Function

        Public Shared Function FindNodes(node As SyntaxNode, ParamArray kinds() As SyntaxKind) As List(Of SyntaxNode)
            Dim finder As New SyntaxNodeFinder()
            finder._kinds.AddAll(kinds)
            finder.Visit(node)
            Return finder._nodes
        End Function
    End Class

    Public Class TypeComparer
        Implements IComparer(Of NamedTypeSymbol)

        Private Function Compare(x As NamedTypeSymbol, y As NamedTypeSymbol) As Integer Implements IComparer(Of NamedTypeSymbol).Compare
            Dim result As Integer = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name)

            If result <> 0 Then
                Return result
            End If

            Return x.Arity - y.Arity
        End Function
    End Class

#Region "IOperation tree validation"

    Friend Shared Function GetOperationTreeForTest(Of TSyntaxNode As SyntaxNode)(compilation As VisualBasicCompilation, fileName As String, Optional which As Integer = 0) As (tree As String, syntax As SyntaxNode, operation As IOperation)
        Dim node As SyntaxNode = CompilationUtils.FindBindingText(Of TSyntaxNode)(compilation, fileName, which, prefixMatch:=True)
        If node Is Nothing Then
            Return Nothing
        End If

        Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
        Dim semanticModel = compilation.GetSemanticModel(tree)
        Dim operation = semanticModel.GetOperation(node)
        If operation IsNot Nothing Then
            Return (OperationTreeVerifier.GetOperationTree(compilation, operation), node, operation)
        Else
            Return (Nothing, Nothing, Nothing)
        End If
    End Function

    Friend Shared Function GetOperationTreeForTest(Of TSyntaxNode As SyntaxNode)(
        source As String,
        Optional compilationOptions As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional which As Integer = 0,
        Optional useLatestFrameworkReferences As Boolean = False) As (tree As String, syntax As SyntaxNode, operation As IOperation, compilation As Compilation)

        Dim fileName = "a.vb"
        Dim syntaxTree = Parse(source, fileName, parseOptions)
        Dim allReferences = TargetFrameworkUtil.Mscorlib45ExtendedReferences.Add(
            If(useLatestFrameworkReferences, TestBase.MsvbRef_v4_0_30319_17929, TestBase.MsvbRef))
        Dim compilation = CreateEmptyCompilation({syntaxTree}, references:=allReferences, options:=If(compilationOptions, TestOptions.ReleaseDll))
        Dim operationTree = GetOperationTreeForTest(Of TSyntaxNode)(compilation, fileName, which)
        Return (operationTree.tree, operationTree.syntax, operationTree.operation, compilation)
    End Function

    Friend Shared Sub VerifyOperationTreeForTest(Of TSyntaxNode As SyntaxNode)(compilation As VisualBasicCompilation, fileName As String, expectedOperationTree As String, Optional which As Integer = 0, Optional additionalOperationTreeVerifier As Action(Of IOperation, Compilation, SyntaxNode) = Nothing)
        Dim operationTree = GetOperationTreeForTest(Of TSyntaxNode)(compilation, fileName, which)
        OperationTreeVerifier.Verify(expectedOperationTree, operationTree.tree)
        If additionalOperationTreeVerifier IsNot Nothing Then
            additionalOperationTreeVerifier(operationTree.operation, compilation, operationTree.syntax)
        End If
    End Sub

    Protected Shared Sub VerifyFlowGraphForTest(Of TSyntaxNode As SyntaxNode)(compilation As VisualBasicCompilation, expectedFlowGraph As String, Optional which As Integer = 0)
        Dim tree = compilation.SyntaxTrees(0)
        Dim syntaxNode As SyntaxNode = CompilationUtils.FindBindingText(Of TSyntaxNode)(compilation, tree.FilePath, which, prefixMatch:=True)
        VerifyFlowGraph(compilation, syntaxNode, expectedFlowGraph)
    End Sub

    Protected Shared Sub VerifyFlowGraph(compilation As VisualBasicCompilation, syntaxNode As SyntaxNode, expectedFlowGraph As String)
        Dim model = compilation.GetSemanticModel(syntaxNode.SyntaxTree)
        Dim graph As FlowAnalysis.ControlFlowGraph = ControlFlowGraphVerifier.GetControlFlowGraph(syntaxNode, model)
        ControlFlowGraphVerifier.VerifyGraph(compilation, expectedFlowGraph, graph)
    End Sub

    Friend Shared Sub VerifyOperationTreeForTest(Of TSyntaxNode As SyntaxNode)(
        source As String,
        expectedOperationTree As String,
        Optional compilationOptions As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional which As Integer = 0,
        Optional additionalOperationTreeVerifier As Action(Of IOperation, Compilation, SyntaxNode) = Nothing,
        Optional useLatestFrameworkReferences As Boolean = False)

        Dim operationTree = GetOperationTreeForTest(Of TSyntaxNode)(source, compilationOptions, parseOptions, which, useLatestFrameworkReferences)
        OperationTreeVerifier.Verify(expectedOperationTree, operationTree.tree)
        If additionalOperationTreeVerifier IsNot Nothing Then
            additionalOperationTreeVerifier(operationTree.operation, operationTree.compilation, operationTree.syntax)
        End If
    End Sub

    Friend Shared Sub VerifyNoOperationTreeForTest(Of TSyntaxNode As SyntaxNode)(
        source As String,
        Optional compilationOptions As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional which As Integer = 0,
        Optional useLatestFrameworkReferences As Boolean = False)

        Dim operationTree = GetOperationTreeForTest(Of TSyntaxNode)(source, compilationOptions, parseOptions, which, useLatestFrameworkReferences)
        Assert.Null(operationTree.tree)
    End Sub

    Friend Shared Sub VerifyOperationTreeAndDiagnosticsForTest(Of TSyntaxNode As SyntaxNode)(compilation As VisualBasicCompilation, fileName As String, expectedOperationTree As String, expectedDiagnostics As String, Optional which As Integer = 0, Optional additionalOperationTreeVerifier As Action(Of IOperation, Compilation, SyntaxNode) = Nothing)
        compilation.AssertTheseDiagnostics(FilterString(expectedDiagnostics))
        VerifyOperationTreeForTest(Of TSyntaxNode)(compilation, fileName, expectedOperationTree, which, additionalOperationTreeVerifier)
    End Sub

    Friend Shared Sub VerifyFlowGraphAndDiagnosticsForTest(Of TSyntaxNode As SyntaxNode)(compilation As VisualBasicCompilation, expectedFlowGraph As String, expectedDiagnostics As String, Optional which As Integer = 0)
        compilation.AssertTheseDiagnostics(FilterString(expectedDiagnostics))
        VerifyFlowGraphForTest(Of TSyntaxNode)(compilation, expectedFlowGraph, which)
    End Sub

    Friend Shared Sub VerifyOperationTreeAndDiagnosticsForTest(Of TSyntaxNode As SyntaxNode)(
        source As String,
        expectedOperationTree As String,
        expectedDiagnostics As String,
        Optional compilationOptions As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional which As Integer = 0,
        Optional references As IEnumerable(Of MetadataReference) = Nothing,
        Optional additionalOperationTreeVerifier As Action(Of IOperation, Compilation, SyntaxNode) = Nothing,
        Optional useLatestFramework As Boolean = False)

        Dim fileName = "a.vb"
        Dim syntaxTree = Parse(source, fileName, parseOptions)
        Dim allReferences As IEnumerable(Of MetadataReference) = TargetFrameworkUtil.Mscorlib45ExtendedReferences.Add(
            If(useLatestFramework, TestBase.MsvbRef_v4_0_30319_17929, TestBase.MsvbRef))

        allReferences = If(references IsNot Nothing, allReferences.Concat(references), allReferences)
        Dim compilation = CreateEmptyCompilation({syntaxTree}, references:=allReferences, options:=If(compilationOptions, TestOptions.ReleaseDll))
        VerifyOperationTreeAndDiagnosticsForTest(Of TSyntaxNode)(compilation, fileName, expectedOperationTree, expectedDiagnostics, which, additionalOperationTreeVerifier)
    End Sub

    Friend Shared Sub VerifyFlowGraphAndDiagnosticsForTest(Of TSyntaxNode As SyntaxNode)(
        testSrc As String,
        expectedFlowGraph As String,
        expectedDiagnostics As String,
        Optional compilationOptions As VisualBasicCompilationOptions = Nothing,
        Optional parseOptions As VisualBasicParseOptions = Nothing,
        Optional which As Integer = 0,
        Optional additionalReferences As IEnumerable(Of MetadataReference) = Nothing,
        Optional useLatestFramework As Boolean = False)

        Dim fileName = "a.vb"
        parseOptions = If(parseOptions?.WithFlowAnalysisFeature(), TestOptions.RegularWithFlowAnalysisFeature)
        Dim syntaxTree = Parse(testSrc, fileName, parseOptions)
        Dim references As IEnumerable(Of MetadataReference) = TargetFrameworkUtil.Mscorlib45ExtendedReferences.Add(
            If(useLatestFramework, TestBase.MsvbRef_v4_0_30319_17929, TestBase.MsvbRef))
        references = If(additionalReferences IsNot Nothing, references.Concat(additionalReferences), references)
        Dim compilation = CreateEmptyCompilation({syntaxTree}, references:=references, options:=If(compilationOptions, TestOptions.ReleaseDll))
        VerifyFlowGraphAndDiagnosticsForTest(Of TSyntaxNode)(compilation, expectedFlowGraph, expectedDiagnostics, which)
    End Sub


    Public Shared Function GetAssertTheseDiagnosticsString(allDiagnostics As ImmutableArray(Of Diagnostic), suppressInfos As Boolean) As String
        Return DumpAllDiagnostics(allDiagnostics, suppressInfos)
    End Function

    Friend Shared Function GetOperationAndSyntaxForTest(Of TSyntaxNode As SyntaxNode)(compilation As Compilation, fileName As String, Optional which As Integer = 0) As (operation As IOperation, syntaxNode As SyntaxNode)
        Dim node As SyntaxNode = CompilationUtils.FindBindingText(Of TSyntaxNode)(compilation, fileName, which, prefixMatch:=True)
        If node Is Nothing Then
            Return (Nothing, Nothing)
        End If

        Dim semanticModel = compilation.GetSemanticModel(node.SyntaxTree)
        Dim operation = semanticModel.GetOperation(node)
        Return (operation, node)
    End Function

#End Region

End Class
