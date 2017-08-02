// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Roslyn.UnitTestFramework;
using Xunit;

namespace ImplementNotifyPropertyChangedCS.UnitTests
{
    public class CodeGenerationTests : CodeRefactoringProviderTestFixture
    {
        protected override string LanguageName
        {
            get
            {
                return LanguageNames.CSharp;
            }
        }

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider()
        {
            return new ImplementNotifyPropertyChangedCodeRefactoringProvider();
        }

        [Fact]
        public void TestRefactoring1()
        {
            const string Code = @"class C
{
    int f;
    int [|P|]
    {
        get
        {
            return f;
        }
        set
        {
            if (f == value)
            {
                return;
            }
            f = value;
        }
    }
}";
            const string Expected = @"using System.Collections.Generic;
using System.ComponentModel;

class C
: INotifyPropertyChanged
{
    int f;
    int P
    {
        get
        {
            return f;
        }
        set
        {
            SetProperty(ref f, value, ""P"");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void SetProperty<T>(ref T field, T value, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}";
            Test(Code, Expected);
        }

        [Fact]
        public void TestRefactoring2()
        {
            const string Code = @"using System.ComponentModel;

class C : INotifyPropertyChanged
{
    int f;
    int [|P|]
    {
        get
        {
            return f;
        }
        set
        {
            f = value;
        }
    }
}";
            const string Expected = @"using System.ComponentModel;
using System.Collections.Generic;

class C : INotifyPropertyChanged
{
    int f;
    int P
    {
        get
        {
            return f;
        }
        set
        {
            SetProperty(ref f, value, ""P"");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void SetProperty<T>(ref T field, T value, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}";
            Test(Code, Expected);
        }

        [Fact]
        public void TestRefactoring3()
        {
            const string Code = @"using System.ComponentModel;
using System.Collections.Generic;

class C : INotifyPropertyChanged
{
    int f;
    int [|P|]
    {
        get
        {
            return f;
        }
        set
        {
            if (f != value)
                f = value;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}";
            const string Expected = @"using System.ComponentModel;
using System.Collections.Generic;

class C : INotifyPropertyChanged
{
    int f;
    int P
    {
        get
        {
            return f;
        }
        set
        {
            SetProperty(ref f, value, ""P"");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void SetProperty<T>(ref T field, T value, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}";
            Test(Code, Expected);
        }

        [Fact]
        public void TestRefactoring4()
        {
            const string Code = @"using System.ComponentModel;
using System.Collections.Generic;

class C : INotifyPropertyChanged
{
    int f;
    int [|P|]
    {
        get
        {
            return f;
        }
        set
        {
            if (f != value)
            {
                f = value;
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void SetProperty<T>(ref T field, T value, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}";
            const string Expected = @"using System.ComponentModel;
using System.Collections.Generic;

class C : INotifyPropertyChanged
{
    int f;
    int P
    {
        get
        {
            return f;
        }
        set
        {
            SetProperty(ref f, value, ""P"");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void SetProperty<T>(ref T field, T value, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}";
            Test(Code, Expected);
        }

        [Fact]
        public void TestRefactoringNotAvailableInEmptyClass()
        {
            const string Markup = @"public class C { [||] }";
            TestNoActions(Markup);
        }

        [Fact]
        public void TestRefactoringNotAvailableOnMethod()
        {
            const string Markup = @"public class C { public int [|Goo|]() { } }";
            TestNoActions(Markup);
        }
    }
}
