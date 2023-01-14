// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class SymbolVisitorTests : CSharpTestBase
    {
        private class LoggingSymbolVisitor : SymbolVisitor
        {
            private readonly StringBuilder _output = new();
            private int _indent;

            public override string ToString() => _output.ToString();

            void VisitChildren<T>(params T[] children)
                where T : ISymbol
                => VisitChildren((IEnumerable<T>)children);

            void VisitChildren<T>(IEnumerable<T> children)
                where T : ISymbol
            {
                foreach (var item in children)
                {
                    item.Accept(this);
                }
            }

            public override void DefaultVisit(ISymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
            }

            public override void VisitAlias(IAliasSymbol symbol)
            {
                _output.Append(symbol.GetType().Name + " of ");
                symbol.Target.Accept(this);
            }

            public override void VisitArrayType(IArrayTypeSymbol symbol)
            {
                _output.Append(symbol.GetType().Name + " of ");
                symbol.ElementType.Accept(this);
            }

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.Modules);
                _indent--;
            }

            public override void VisitDiscard(IDiscardSymbol symbol)
            {
                base.VisitDiscard(symbol);
            }

            public override void VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                _output.Append("<dynamic>");
            }

            public override void VisitEvent(IEventSymbol symbol)
            {
                _output.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this);
                _output.AppendLine();
            }

            public override void VisitField(IFieldSymbol symbol)
            {
                _output.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this);
                _output.AppendLine();
            }

            public override void VisitLabel(ILabelSymbol symbol)
            {
                base.VisitLabel(symbol);
            }

            public override void VisitLocal(ILocalSymbol symbol)
            {
                base.VisitLocal(symbol);
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.TypeArguments);
                VisitChildren(symbol.Parameters);
                _indent--;
            }

            public override void VisitModule(IModuleSymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.GlobalNamespace);
                _indent--;
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (_indent < 4)
                {
                    _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                    _indent++;
                    VisitChildren(symbol.TypeArguments);
                    VisitChildren(symbol.GetMembers());
                    _indent--;
                }
                else
                {
                    _output.Append(symbol.GetType().Name + " " + symbol.Name);
                    if (symbol.TypeArguments.Length > 0)
                    {
                        _output.Append(" of ");
                        VisitChildren(symbol.TypeArguments);
                    }
                }
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.GetMembers());
                _indent--;
            }

            public override void VisitParameter(IParameterSymbol symbol)
            {
                _output.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this);
                _output.AppendLine();
            }

            public override void VisitPointerType(IPointerTypeSymbol symbol)
            {
                _output.Append(symbol.GetType().Name + " of ");
                symbol.PointedAtType.Accept(this);
            }

            public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
            {
                base.VisitFunctionPointerType(symbol);
            }

            public override void VisitProperty(IPropertySymbol symbol)
            {
                _output.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                symbol.Type.Accept(this);
                _output.AppendLine();
            }

            public override void VisitRangeVariable(IRangeVariableSymbol symbol)
            {
                base.VisitRangeVariable(symbol);
            }

            public override void VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (_indent < 5)
                {
                    _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                }
                else
                {
                    _output.Append(symbol.GetType().Name + " " + symbol.Name);
                }
            }
        }

        private class LoggingSymbolVisitorWithReturnValue : SymbolVisitor<string>
        {
            private readonly StringBuilder _output = new();
            private int _indent;

            public override string ToString() => _output.ToString();

            void VisitChildren<T>(params T[] children)
                where T : ISymbol
                => VisitChildren((IEnumerable<T>)children);

            void VisitChildren<T>(IEnumerable<T> children)
                where T : ISymbol
            {
                foreach (var item in children)
                {
                    item.Accept(this);
                }
            }

            public override string DefaultVisit(ISymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                return null;
            }

            public override string VisitAlias(IAliasSymbol symbol)
            {
                _output.Append(symbol.GetType().Name + " of ");
                symbol.Target.Accept(this);
                return null;
            }

            public override string VisitArrayType(IArrayTypeSymbol symbol)
            {
                _output.Append(symbol.GetType().Name + " of ");
                symbol.ElementType.Accept(this);
                return null;
            }

            public override string VisitAssembly(IAssemblySymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.Modules);
                _indent--;
                return null;
            }

            public override string VisitDiscard(IDiscardSymbol symbol)
            {
                base.VisitDiscard(symbol);
                return null;
            }

            public override string VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                _output.Append("<dynamic>");
                return null;
            }

            public override string VisitEvent(IEventSymbol symbol)
            {
                _output.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this);
                _output.AppendLine();
                return null;
            }

            public override string VisitField(IFieldSymbol symbol)
            {
                _output.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this);
                _output.AppendLine();
                return null;
            }

            public override string VisitLabel(ILabelSymbol symbol)
            {
                base.VisitLabel(symbol);
                return null;
            }

            public override string VisitLocal(ILocalSymbol symbol)
            {
                base.VisitLocal(symbol);
                return null;
            }

            public override string VisitMethod(IMethodSymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.TypeArguments);
                VisitChildren(symbol.Parameters);
                _indent--;
                return null;
            }

            public override string VisitModule(IModuleSymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.GlobalNamespace);
                _indent--;
                return null;
            }

            public override string VisitNamedType(INamedTypeSymbol symbol)
            {
                if (_indent < 4)
                {
                    _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                    _indent++;
                    VisitChildren(symbol.TypeArguments);
                    VisitChildren(symbol.GetMembers());
                    _indent--;
                }
                else
                {
                    _output.Append(symbol.GetType().Name + " " + symbol.Name);
                    if (symbol.TypeArguments.Length > 0)
                    {
                        _output.Append(" of ");
                        VisitChildren(symbol.TypeArguments);
                    }
                }
                return null;
            }

            public override string VisitNamespace(INamespaceSymbol symbol)
            {
                _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.GetMembers());
                _indent--;
                return null;
            }

            public override string VisitParameter(IParameterSymbol symbol)
            {
                _output.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this);
                _output.AppendLine();
                return null;
            }

            public override string VisitPointerType(IPointerTypeSymbol symbol)
            {
                _output.Append(symbol.GetType().Name + " of ");
                symbol.PointedAtType.Accept(this);
                return null;
            }

            public override string VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
            {
                base.VisitFunctionPointerType(symbol);
                return null;
            }

            public override string VisitProperty(IPropertySymbol symbol)
            {
                _output.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                symbol.Type.Accept(this);
                _output.AppendLine();
                return null;
            }

            public override string VisitRangeVariable(IRangeVariableSymbol symbol)
            {
                base.VisitRangeVariable(symbol);
                return null;
            }

            public override string VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (_indent < 5)
                {
                    _output.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                }
                else
                {
                    _output.Append(symbol.GetType().Name + " " + symbol.Name);
                }
                return null;
            }
        }

        private class LoggingSymbolVisitorWithReturnValueAndContext : SymbolVisitor<StringBuilder, int>
        {
            private int _indent;

            protected override int DefaultResult => -1;

            void VisitChildren<T>(IEnumerable<T> children, StringBuilder argument)
                where T : ISymbol
            {
                foreach (var item in children)
                {
                    item.Accept(this, argument);
                }
            }

            public override int VisitAssembly(IAssemblySymbol symbol, StringBuilder argument)
            {
                argument.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.Modules, argument);
                _indent--;
                return _indent;
            }

            public override int VisitModule(IModuleSymbol symbol, StringBuilder argument)
            {
                argument.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(new[] { symbol.GlobalNamespace }, argument);
                _indent--;
                return _indent;
            }

            public override int VisitNamespace(INamespaceSymbol symbol, StringBuilder argument)
            {
                argument.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.GetMembers(), argument);
                _indent--;
                return _indent;
            }

            public override int VisitNamedType(INamedTypeSymbol symbol, StringBuilder argument)
            {
                if (_indent < 4)
                {
                    argument.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                    _indent++;
                    VisitChildren(symbol.TypeArguments, argument);
                    VisitChildren(symbol.GetMembers(), argument);
                    _indent--;
                }
                else
                {
                    argument.Append(symbol.GetType().Name + " " + symbol.Name);
                    if (symbol.TypeArguments.Length > 0)
                    {
                        argument.Append(" of ");
                        VisitChildren(symbol.TypeArguments, argument);
                    }
                }
                return _indent;
            }

            public override int VisitEvent(IEventSymbol symbol, StringBuilder argument)
            {
                argument.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this, argument);
                argument.AppendLine();
                return _indent;
            }

            public override int VisitField(IFieldSymbol symbol, StringBuilder argument)
            {
                argument.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this, argument);
                argument.AppendLine();
                return _indent;
            }

            public override int VisitMethod(IMethodSymbol symbol, StringBuilder argument)
            {
                argument.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                _indent++;
                VisitChildren(symbol.TypeArguments, argument);
                VisitChildren(symbol.Parameters, argument);
                _indent--;
                return _indent;
            }

            public override int VisitParameter(IParameterSymbol symbol, StringBuilder argument)
            {
                argument.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name + ": ");
                symbol.Type.Accept(this, argument);
                argument.AppendLine();
                return _indent;
            }

            public override int VisitDynamicType(IDynamicTypeSymbol symbol, StringBuilder argument)
            {
                argument.Append("<dynamic>");
                return _indent;
            }

            public override int VisitArrayType(IArrayTypeSymbol symbol, StringBuilder argument)
            {
                argument.Append(symbol.GetType().Name + " of ");
                symbol.ElementType.Accept(this, argument);
                return _indent;
            }

            public override int VisitPointerType(IPointerTypeSymbol symbol, StringBuilder argument)
            {
                argument.Append(symbol.GetType().Name + " of ");
                symbol.PointedAtType.Accept(this, argument);
                return _indent;
            }

            public override int VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol, StringBuilder argument)
            {
                return base.VisitFunctionPointerType(symbol, argument);
            }

            public override int VisitProperty(IPropertySymbol symbol, StringBuilder argument)
            {
                argument.Append(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                symbol.Type.Accept(this, argument);
                argument.AppendLine();
                return _indent;
            }

            public override int VisitRangeVariable(IRangeVariableSymbol symbol, StringBuilder argument)
            {
                return base.VisitRangeVariable(symbol, argument);
            }

            public override int VisitTypeParameter(ITypeParameterSymbol symbol, StringBuilder argument)
            {
                if (_indent < 5)
                {
                    argument.AppendLine(new string(' ', 2 * _indent) + symbol.GetType().Name + " " + symbol.Name);
                }
                else
                {
                    argument.Append(symbol.GetType().Name + " " + symbol.Name);
                }
                return _indent;
            }
        }

        [Fact]
        public void NamedType_LoggingSymbolVisitor()
        {
            var c = CreateCompilation(
                "using System;" +
                "class C { }" +
                "struct S { int i; }" +
                "class Generic<T> {}" +
                "enum ABC { A, B, C }" +
                "delegate TReturn Function<T, TReturn>(T arg);",
                assemblyName: "SymbolVisitorTests");
            IAssemblySymbol asm = new SourceAssemblySymbol(c.SourceAssembly);
            var visitor = new LoggingSymbolVisitor();
            asm.Accept(visitor);

            string expectedOutput = @"SourceAssemblySymbol SymbolVisitorTests
  ModuleSymbol SymbolVisitorTests.dll
    NamespaceSymbol 
      NonErrorNamedTypeSymbol C
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol S
        FieldSymbol i: NonErrorNamedTypeSymbol Int32
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol Generic
        TypeParameterSymbol T
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol ABC
        FieldSymbol A: NonErrorNamedTypeSymbol ABC
        FieldSymbol B: NonErrorNamedTypeSymbol ABC
        FieldSymbol C: NonErrorNamedTypeSymbol ABC
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol Function
        TypeParameterSymbol T
        TypeParameterSymbol TReturn
        MethodSymbol .ctor
          ParameterSymbol object: NonErrorNamedTypeSymbol Object
          ParameterSymbol method: NonErrorNamedTypeSymbol IntPtr
        MethodSymbol Invoke
          ParameterSymbol arg: TypeParameterSymbol T
        MethodSymbol BeginInvoke
          ParameterSymbol arg: TypeParameterSymbol T
          ParameterSymbol callback: NonErrorNamedTypeSymbol AsyncCallback
          ParameterSymbol object: NonErrorNamedTypeSymbol Object
        MethodSymbol EndInvoke
          ParameterSymbol result: NonErrorNamedTypeSymbol IAsyncResult
";

            string resultOutput = visitor.ToString();
            Assert.Equal(expectedOutput, resultOutput);
        }

        [Fact]
        public void NamedType_LoggingSymbolVisitorWithReturnValue()
        {
            var c = CreateCompilation(
                "using System;" +
                "class C { }" +
                "struct S { int i; }" +
                "class Generic<T> {}" +
                "enum ABC { A, B, C }" +
                "delegate TReturn Function<T, TReturn>(T arg);",
                assemblyName: "SymbolVisitorTests");
            IAssemblySymbol asm = new SourceAssemblySymbol(c.SourceAssembly);
            var visitor = new LoggingSymbolVisitorWithReturnValue();
            asm.Accept(visitor);

            string expectedOutput = @"SourceAssemblySymbol SymbolVisitorTests
  ModuleSymbol SymbolVisitorTests.dll
    NamespaceSymbol 
      NonErrorNamedTypeSymbol C
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol S
        FieldSymbol i: NonErrorNamedTypeSymbol Int32
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol Generic
        TypeParameterSymbol T
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol ABC
        FieldSymbol A: NonErrorNamedTypeSymbol ABC
        FieldSymbol B: NonErrorNamedTypeSymbol ABC
        FieldSymbol C: NonErrorNamedTypeSymbol ABC
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol Function
        TypeParameterSymbol T
        TypeParameterSymbol TReturn
        MethodSymbol .ctor
          ParameterSymbol object: NonErrorNamedTypeSymbol Object
          ParameterSymbol method: NonErrorNamedTypeSymbol IntPtr
        MethodSymbol Invoke
          ParameterSymbol arg: TypeParameterSymbol T
        MethodSymbol BeginInvoke
          ParameterSymbol arg: TypeParameterSymbol T
          ParameterSymbol callback: NonErrorNamedTypeSymbol AsyncCallback
          ParameterSymbol object: NonErrorNamedTypeSymbol Object
        MethodSymbol EndInvoke
          ParameterSymbol result: NonErrorNamedTypeSymbol IAsyncResult
";

            string resultOutput = visitor.ToString();
            Assert.Equal(expectedOutput, resultOutput);
        }

        [Fact]
        public void NamedType_LoggingSymbolVisitorWithReturnValueAndContext()
        {
            var c = CreateCompilation(
                "using System;" +
                "class C { }" +
                "struct S { int i; }" +
                "class Generic<T> {}" +
                "enum ABC { A, B, C }" +
                "delegate TReturn Function<T, TReturn>(T arg);",
                assemblyName: "SymbolVisitorTests");
            IAssemblySymbol asm = new SourceAssemblySymbol(c.SourceAssembly);
            var visitor = new LoggingSymbolVisitorWithReturnValueAndContext();
            var sb = new StringBuilder();
            asm.Accept(visitor, sb);

            string expectedOutput = @"SourceAssemblySymbol SymbolVisitorTests
  ModuleSymbol SymbolVisitorTests.dll
    NamespaceSymbol 
      NonErrorNamedTypeSymbol C
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol S
        FieldSymbol i: NonErrorNamedTypeSymbol Int32
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol Generic
        TypeParameterSymbol T
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol ABC
        FieldSymbol A: NonErrorNamedTypeSymbol ABC
        FieldSymbol B: NonErrorNamedTypeSymbol ABC
        FieldSymbol C: NonErrorNamedTypeSymbol ABC
        MethodSymbol .ctor
      NonErrorNamedTypeSymbol Function
        TypeParameterSymbol T
        TypeParameterSymbol TReturn
        MethodSymbol .ctor
          ParameterSymbol object: NonErrorNamedTypeSymbol Object
          ParameterSymbol method: NonErrorNamedTypeSymbol IntPtr
        MethodSymbol Invoke
          ParameterSymbol arg: TypeParameterSymbol T
        MethodSymbol BeginInvoke
          ParameterSymbol arg: TypeParameterSymbol T
          ParameterSymbol callback: NonErrorNamedTypeSymbol AsyncCallback
          ParameterSymbol object: NonErrorNamedTypeSymbol Object
        MethodSymbol EndInvoke
          ParameterSymbol result: NonErrorNamedTypeSymbol IAsyncResult
";

            string resultOutput = sb.ToString();
            Assert.Equal(expectedOutput, resultOutput);
        }

        [Fact]
        public void TypeMembers_LoggingSymbolVisitor()
        {
            var c = CreateCompilation(
                "using System;" +
                "using System.Collections.Generic;" +
                "unsafe class C { " +
                "public C() {} " +
                "int* field; " +
                "string[] field2; " +
                "List<string> generics; " +
                "dynamic d; " +
                "public int Value { get; set; } " +
                "public event EventHandler Event; " +
                "}",
                assemblyName: "SymbolVisitorTests");
            IAssemblySymbol asm = new SourceAssemblySymbol(c.SourceAssembly);
            var visitor = new LoggingSymbolVisitor();
            asm.Accept(visitor);

            string expectedOutput = @"SourceAssemblySymbol SymbolVisitorTests
  ModuleSymbol SymbolVisitorTests.dll
    NamespaceSymbol 
      NonErrorNamedTypeSymbol C
        MethodSymbol .ctor
        FieldSymbol field: PointerTypeSymbol of NonErrorNamedTypeSymbol Int32
        FieldSymbol field2: ArrayTypeSymbol of NonErrorNamedTypeSymbol String
        FieldSymbol generics: NonErrorNamedTypeSymbol List of NonErrorNamedTypeSymbol String
        FieldSymbol d: <dynamic>
        FieldSymbol <Value>k__BackingField: NonErrorNamedTypeSymbol Int32
        PropertySymbol ValueNonErrorNamedTypeSymbol Int32
        MethodSymbol get_Value
        MethodSymbol set_Value
          ParameterSymbol value: NonErrorNamedTypeSymbol Int32
        MethodSymbol add_Event
          ParameterSymbol value: NonErrorNamedTypeSymbol EventHandler
        MethodSymbol remove_Event
          ParameterSymbol value: NonErrorNamedTypeSymbol EventHandler
        EventSymbol Event: NonErrorNamedTypeSymbol EventHandler
";

            string resultOutput = visitor.ToString();
            Assert.Equal(expectedOutput, resultOutput);
        }
        [Fact]
        public void TypeMembers_LoggingSymbolVisitorWithReturnValue()
        {
            var c = CreateCompilation(
                "using System;" +
                "using System.Collections.Generic;" +
                "unsafe class C { " +
                "public C() {} " +
                "int* field; " +
                "string[] field2; " +
                "List<string> generics; " +
                "dynamic d; " +
                "public int Value { get; set; } " +
                "public event EventHandler Event; " +
                "}",
                assemblyName: "SymbolVisitorTests");
            IAssemblySymbol asm = new SourceAssemblySymbol(c.SourceAssembly);
            var visitor = new LoggingSymbolVisitorWithReturnValue();
            asm.Accept(visitor);

            string expectedOutput = @"SourceAssemblySymbol SymbolVisitorTests
  ModuleSymbol SymbolVisitorTests.dll
    NamespaceSymbol 
      NonErrorNamedTypeSymbol C
        MethodSymbol .ctor
        FieldSymbol field: PointerTypeSymbol of NonErrorNamedTypeSymbol Int32
        FieldSymbol field2: ArrayTypeSymbol of NonErrorNamedTypeSymbol String
        FieldSymbol generics: NonErrorNamedTypeSymbol List of NonErrorNamedTypeSymbol String
        FieldSymbol d: <dynamic>
        FieldSymbol <Value>k__BackingField: NonErrorNamedTypeSymbol Int32
        PropertySymbol ValueNonErrorNamedTypeSymbol Int32
        MethodSymbol get_Value
        MethodSymbol set_Value
          ParameterSymbol value: NonErrorNamedTypeSymbol Int32
        MethodSymbol add_Event
          ParameterSymbol value: NonErrorNamedTypeSymbol EventHandler
        MethodSymbol remove_Event
          ParameterSymbol value: NonErrorNamedTypeSymbol EventHandler
        EventSymbol Event: NonErrorNamedTypeSymbol EventHandler
";

            string resultOutput = visitor.ToString();
            Assert.Equal(expectedOutput, resultOutput);
        }
        [Fact]
        public void TypeMembers_LoggingSymbolVisitorWithReturnValueAndContext()
        {
            var c = CreateCompilation(
                "using System;" +
                "using System.Collections.Generic;" +
                "unsafe class C { " +
                "public C() {} " +
                "int* field; " +
                "string[] field2; " +
                "List<string> generics; " +
                "dynamic d; " +
                "public int Value { get; set; } " +
                "public event EventHandler Event; " +
                "}",
                assemblyName: "SymbolVisitorTests");
            IAssemblySymbol asm = new SourceAssemblySymbol(c.SourceAssembly);
            var visitor = new LoggingSymbolVisitorWithReturnValueAndContext();
            var sb = new StringBuilder();
            asm.Accept(visitor, sb);

            string expectedOutput = @"SourceAssemblySymbol SymbolVisitorTests
  ModuleSymbol SymbolVisitorTests.dll
    NamespaceSymbol 
      NonErrorNamedTypeSymbol C
        MethodSymbol .ctor
        FieldSymbol field: PointerTypeSymbol of NonErrorNamedTypeSymbol Int32
        FieldSymbol field2: ArrayTypeSymbol of NonErrorNamedTypeSymbol String
        FieldSymbol generics: NonErrorNamedTypeSymbol List of NonErrorNamedTypeSymbol String
        FieldSymbol d: <dynamic>
        FieldSymbol <Value>k__BackingField: NonErrorNamedTypeSymbol Int32
        PropertySymbol ValueNonErrorNamedTypeSymbol Int32
        MethodSymbol get_Value
        MethodSymbol set_Value
          ParameterSymbol value: NonErrorNamedTypeSymbol Int32
        MethodSymbol add_Event
          ParameterSymbol value: NonErrorNamedTypeSymbol EventHandler
        MethodSymbol remove_Event
          ParameterSymbol value: NonErrorNamedTypeSymbol EventHandler
        EventSymbol Event: NonErrorNamedTypeSymbol EventHandler
";

            string resultOutput = sb.ToString();
            Assert.Equal(expectedOutput, resultOutput);
        }
    }
}
