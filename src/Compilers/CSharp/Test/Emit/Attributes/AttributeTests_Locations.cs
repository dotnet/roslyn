// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Locations : CSharpTestBase
    {
        [Fact]
        public void Global1()
        {
            var source1 = @"
[assembly: A]
[module: A]
";
            var source2 = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }
";

            CreateCompilationWithMscorlib(new[] { source1, source2 }).VerifyDiagnostics();
        }

        [Fact]
        public void Global2()
        {
            var source1 = @"
namespace N 
{
    [assembly: A]
}
";
            var source2 = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }
";

            CreateCompilationWithMscorlib(new[] { source1, source2 }).VerifyDiagnostics(
                // (4,6): error CS1730: Assembly and module attributes must precede all other elements defined in a file except using clauses and extern alias declarations
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotFirst, "assembly"));
        }

        [Fact]
        public void Global3()
        {
            var source1 = @"
class X
{
   [A]
}
";
            var source2 = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }
";

            CreateCompilationWithMscorlib(new[] { source1, source2 }).VerifyDiagnostics(
                // (5,1): error CS1519: Unexpected token '}', member declaration expected.
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "}").WithArguments("}"));
        }

        [Fact]
        public void OnClass()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

[A]
[assembly: A]
[module: A]
[type: A]
[method: A]
[field: A]
[property: A]
[event: A]
[return: A]
[param: A]
[typevar: A]
[delegate: A]
class C 
{
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,2): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "type"),
                // (9,2): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "type"),
                // (11,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type"),
                // (12,2): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "type"),
                // (13,2): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "type"),
                // (14,2): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "type"),
                // (15,2): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "type"),
                // (16,2): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "type"),
                // (17,2): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "type"),
                // (18,2): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "type"));
        }

        [Fact]
        public void OnStruct()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

[A]
[assembly: A]
[module: A]
[type: A]
[method: A]
[field: A]
[property: A]
[event: A]
[return: A]
[param: A]
[typevar: A]
[delegate: A]
struct S
{
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,2): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "type"),
                // (9,2): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "type"),
                // (11,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type"),
                // (12,2): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "type"),
                // (13,2): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "type"),
                // (14,2): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "type"),
                // (15,2): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "type"),
                // (16,2): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "type"),
                // (17,2): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "type"),
                // (18,2): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "type"));
        }

        [Fact]
        public void OnEnum()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

[A]
[assembly: A]
[module: A]
[type: A]
[method: A]
[field: A]
[property: A]
[event: A]
[return: A]
[param: A]
[typevar: A]
[delegate: A]
enum E
{
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,2): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "type"),
                // (9,2): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "type"),
                // (11,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type"),
                // (12,2): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "type"),
                // (13,2): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "type"),
                // (14,2): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "type"),
                // (15,2): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "type"),
                // (16,2): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "type"),
                // (17,2): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "type"),
                // (18,2): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "type"));
        }

        [Fact]
        public void OnInterface()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

[A]
[assembly: A]
[module: A]
[type: A]
[method: A]
[field: A]
[property: A]
[event: A]
[return: A]
[param: A]
[typevar: A]
[delegate: A]
interface I
{
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,2): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "type"),
                // (9,2): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "type"),
                // (11,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type"),
                // (12,2): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "type"),
                // (13,2): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "type"),
                // (14,2): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "type"),
                // (15,2): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "type"),
                // (16,2): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "type"),
                // (17,2): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "type"),
                // (18,2): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "type"));
        }

        [Fact]
        public void OnDelegate()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

[A]
[assembly: A]
[module: A]
[type: A]
[method: A]
[field: A]
[property: A]
[event: A]
[return: A]
[param: A]
[typevar: A]
[delegate: A]
delegate void D(int a);
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,2): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "type, return"),
                // (9,2): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "type, return"),
                // (11,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type, return"),
                // (12,2): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "type, return"),
                // (13,2): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "type, return"),
                // (14,2): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "type, return"),
                // (16,2): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "type, return"),
                // (17,2): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "type, return"),
                // (18,2): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'type, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "type, return"));
        }

        [Fact]
        public void OnMethod()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    void M(int a) { }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "method, return"),
                // (11,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "method, return"),
                // (12,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "method, return"),
                // (14,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, return"),
                // (15,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "method, return"),
                // (16,6): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "method, return"),
                // (18,6): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "method, return"),
                // (19,6): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "method, return"),
                // (20,6): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "method, return"));
        }

        [Fact]
        public void OnField()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    int a;
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "field"),
                // (11,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "field"),
                // (12,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "field"),
                // (13,6): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "field"),
                // (15,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "field"),
                // (16,6): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "field"),
                // (17,6): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "field"),
                // (18,6): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "field"),
                // (19,6): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "field"),
                // (20,6): warning CS0658: 'delegate' is not a recognized attribute location. AValid attribute locations for this declaration are 'field'. ll attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "field"),
                // (21,9): warning CS0169: The field 'C.a' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("C.a"));
        }

        [Fact]
        public void OnEnumField()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

enum E
{
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    x
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "field"),
                // (11,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "field"),
                // (12,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "field"),
                // (13,6): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "field"),
                // (15,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "field"),
                // (16,6): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "field"),
                // (17,6): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "field"),
                // (18,6): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "field"),
                // (19,6): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "field"),
                // (20,6): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'field'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "field"));
        }

        [Fact]
        public void OnProperty()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    int a { get; set; }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "property"),
                // (11,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "property"),
                // (12,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "property"),
                // (13,6): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "property"),
                // (14,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "property"),
                // (16,6): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "property"),
                // (17,6): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "property"),
                // (18,6): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "property"),
                // (19,6): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "property"),
                // (20,6): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "property"));
        }

        [Fact]
        public void OnPropertyGetter()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    int Foo
    {
        [A]
        [assembly: A]
        [module: A]
        [type: A]
        [method: A]
        [field: A]
        [property: A]
        [event: A]
        [return: A]
        [param: A]
        [typevar: A]
        [delegate: A]
        get { return 0; }

        set { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,10): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "method, return"),
                // (13,10): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "method, return"),
                // (14,10): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "method, return"),
                // (16,10): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, return"),
                // (17,10): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "method, return"),
                // (18,10): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "method, return"),
                // (20,10): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "method, return"),
                // (21,10): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "method, return"),
                // (22,10): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "method, return"));
        }

        [Fact]
        public void OnPropertySetter()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    int Foo
    {
        get { return 0; }

        [A]
        [assembly: A]
        [module: A]
        [type: A]
        [method: A]
        [field: A]
        [property: A]
        [event: A]
        [return: A]
        [param: A]
        [typevar: A]
        [delegate: A]
        set { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,10): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "method, param, return"),
                // (15,10): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "method, param, return"),
                // (16,10): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "method, param, return"),
                // (18,10): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return"),
                // (19,10): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "method, param, return"),
                // (20,10): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "method, param, return"),
                // (23,10): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "method, param, return"),
                // (24,10): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "method, param, return"));
        }

        [Fact]
        public void OnFieldEvent()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    event System.Action e;
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, field, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "method, field, event"),
                // (11,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, field, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "method, field, event"),
                // (12,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, field, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "method, field, event"),
                // (15,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, field, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "method, field, event"),
                // (17,6): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, field, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "method, field, event"),
                // (18,6): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, field, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "method, field, event"),
                // (19,6): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, field, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "method, field, event"),
                // (20,6): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'method, field, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "method, field, event"),
                // (21,25): warning CS0067: The event 'C.e' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e").WithArguments("C.e"));
        }

        [Fact, WorkItem(543977, "DevDiv")]
        public void OnInterfaceFieldEvent()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

interface I
{
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    event System.Action e;
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "method, event"),
                // (11,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "method, event"),
                // (12,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "method, event"),
                // (14,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, event"),
                // (15,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "method, event"),
                // (17,6): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "method, event"),
                // (18,6): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "method, event"),
                // (19,6): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "method, event"),
                // (20,6): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "method, event"));
        }

        [Fact]
        public void OnCustomEvent()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C 
{
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    event Action E { add { } remove { } }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "event"),
                // (11,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "event"),
                // (12,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "event"),
                // (13,6): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "event"),
                // (14,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "event"),
                // (15,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "event"),
                // (17,6): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "event"),
                // (18,6): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "event"),
                // (19,6): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "event"),
                // (20,6): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "event"));
        }

        [Fact]
        public void OnEventAdder()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    event Action Foo
    {
        [A]
        [assembly: A]
        [module: A]
        [type: A]
        [method: A]
        [field: A]
        [property: A]
        [event: A]
        [return: A]
        [param: A]
        [typevar: A]
        [delegate: A]
        add { }

        remove { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,10): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "method, param, return"),
                // (13,10): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "method, param, return"),
                // (14,10): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "method, param, return"),
                // (16,10): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return"),
                // (17,10): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "method, param, return"),
                // (18,10): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "method, param, return"),
                // (21,10): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "method, param, return"),
                // (22,10): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "method, param, return"));
        }

        [Fact]
        public void OnEventRemover()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    event Action Foo
    {
        add { }

        [A]
        [assembly: A]
        [module: A]
        [type: A]
        [method: A]
        [field: A]
        [property: A]
        [event: A]
        [return: A]
        [param: A]
        [typevar: A]
        [delegate: A]
        remove { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,10): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "method, param, return"),
                // (15,10): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "method, param, return"),
                // (16,10): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "method, param, return"),
                // (18,10): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return"),
                // (19,10): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "method, param, return"),
                // (20,10): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "method, param, return"),
                // (23,10): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "method, param, return"),
                // (24,10): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "method, param, return"));
        }

        [Fact]
        public void OnTypeParameter()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
<
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    T
>
{
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "typevar"),
                // (11,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "typevar"),
                // (12,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "typevar"),
                // (13,6): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "typevar"),
                // (14,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "typevar"),
                // (15,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "typevar"),
                // (16,6): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "typevar"),
                // (17,6): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "typevar"),
                // (18,6): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "typevar"),
                // (20,6): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "typevar"));
        }

        [Fact]
        public void OnMethodParameter()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    void f(
        [A]
        [assembly: A]
        [module: A]
        [type: A]
        [method: A]
        [field: A]
        [property: A]
        [event: A]
        [return: A]
        [param: A]
        [typevar: A]
        [delegate: A]
        int x
    ) { }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,10): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "param"),
                // (12,10): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "param"),
                // (13,10): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "param"),
                // (14,10): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "param"),
                // (15,10): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param"),
                // (16,10): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "param"),
                // (17,10): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "param"),
                // (18,10): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "param"),
                // (20,10): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "param"),
                // (21,10): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "param"));
        }

        [Fact]
        public void OnDelegateParameter()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

delegate void D(
    [A]
    [assembly: A]
    [module: A]
    [type: A]
    [method: A]
    [field: A]
    [property: A]
    [event: A]
    [return: A]
    [param: A]
    [typevar: A]
    [delegate: A]
    int x
);
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,6): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "param"),
                // (10,6): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "param"),
                // (11,6): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "param"),
                // (12,6): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "param"),
                // (13,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param"),
                // (14,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "param"),
                // (15,6): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "param"),
                // (16,6): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "param"),
                // (18,6): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "param"),
                // (19,6): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "param"));
        }

        [Fact]
        public void OnIndexerParameter()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

class C
{
    int this[
        [A]
        [assembly: A]
        [module: A]
        [type: A]
        [method: A]
        [field: A]
        [property: A]
        [event: A]
        [return: A]
        [param: A]
        [typevar: A]
        [delegate: A]
        int x]
    {
        get { return 0; }
        set { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,10): warning CS0657: 'assembly' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "param"),
                // (12,10): warning CS0657: 'module' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "module").WithArguments("module", "param"),
                // (13,10): warning CS0657: 'type' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "type").WithArguments("type", "param"),
                // (14,10): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "param"),
                // (15,10): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param"),
                // (16,10): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "param"),
                // (17,10): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "param"),
                // (18,10): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "param"),
                // (20,10): warning CS0657: 'typevar' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "typevar").WithArguments("typevar", "param"),
                // (21,10): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "param"));
        }

        [Fact]
        public void UnrecognizedLocations()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }

[class: A]
[struct: A]
[interface: A]
[delegate: A]
[enum: A]
[add: A]
[remove: A]
[get: A]
[set: A]
class C
{
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,2): warning CS0658: 'class' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "class").WithArguments("class", "type"),
                // (8,2): warning CS0658: 'struct' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "struct").WithArguments("struct", "type"),
                // (9,2): warning CS0658: 'interface' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "interface").WithArguments("interface", "type"),
                // (10,2): warning CS0658: 'delegate' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "delegate").WithArguments("delegate", "type"),
                // (11,2): warning CS0658: 'enum' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "enum").WithArguments("enum", "type"),
                // (12,2): warning CS0658: 'add' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "add").WithArguments("add", "type"),
                // (13,2): warning CS0658: 'remove' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "remove").WithArguments("remove", "type"),
                // (14,2): warning CS0658: 'get' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "get").WithArguments("get", "type"),
                // (15,2): warning CS0658: 'set' is not a recognized attribute location. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "set").WithArguments("set", "type"));
        }

        [Fact, WorkItem(545555, "DevDiv")]
        public void AttributesWithInvalidLocationNotEmitted()
        {
            var source = @"
using System;

public class foo
{
    public static void Main()
    {
        object[] o = typeof(foo).GetMethod(""Boo"").GetCustomAttributes(typeof(A), false);
        Console.WriteLine(""Attribute Count={0}"", o.Length);
    }

    [foo: A]
    [method: A]
    public int Boo(int i)
    {
        return 1;
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class A : Attribute { }
";

            CompileAndVerify(source, expectedOutput: "Attribute Count=1").VerifyDiagnostics(
                // (12,6): warning CS0658: 'foo' is not a recognized attribute location. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "foo").WithArguments("foo", "method, return"));
        }

        [WorkItem(537613, "DevDiv"), WorkItem(537738, "DevDiv")]
        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound_VerbatimIdentifierAttributeTarget()
        {
            CreateCompilationWithMscorlib(@"class A { [@return:X] void B() { } }").VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                // class A { [@return:X] void B() { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X"));
        }

        [WorkItem(537613, "DevDiv"), WorkItem(537738, "DevDiv")]
        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound_VerbatimIdentifierAttributeTargetAndAttribute()
        {
            var source = @"
using System;

class X: Attribute {}
class XAttribute: Attribute {}

class A { [return:X] void M() { } }  // Ambiguous
class B { [@return:X] void M() { } }  // Ambiguous
class C { [return:@X] void M() { } }  // Fine, binds to X
class D { [@return:@X] void M() { } }  // Fine, binds to X
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,19): error CS1614: 'X' is ambiguous between 'X' and 'XAttribute'; use either '@X' or 'XAttribute'
                // class A { [return:X] void M() { } }  // Ambiguous
                Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "X").WithArguments("X", "X", "XAttribute"),
                // (8,20): error CS1614: 'X' is ambiguous between 'X' and 'XAttribute'; use either '@X' or 'XAttribute'
                // class B { [@return:X] void M() { } }  // Ambiguous
                Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "X").WithArguments("X", "X", "XAttribute"));
        }
    }
}
