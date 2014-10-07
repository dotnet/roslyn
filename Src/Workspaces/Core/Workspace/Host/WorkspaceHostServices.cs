using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Utilities;
using Roslyn.Workspaces;

namespace Roslyn.Workspaces.Host
{
    public abstract class WorkspaceHostServices
    {
        public ILanguageServiceProviderFactory LanguageServiceProviderFactory { get; private set; }
        public ITextFactory TextFactory { get; private set; }
        public IWorkspaceTaskSchedulerFactory TaskSchedulerFactory { get; private set; }
        public IBackgroundCompilerFactory BackgroundCompilerFactory { get; private set; }
        public IBackgroundParserFactory BackgroundParserFactory { get; private set; }
        public IPersistenceService PersistenceService { get; private set; }
        public ITemporaryStorageService TemporaryStorageService { get; private set; }
        public IProjectDependencyService ProjectDependencyService { get; private set; }
        public IRetainerFactory<IText> TextRetainerFactory { get; private set; }
        public IRetainerFactory<ISyntaxTree> SyntaxTreeRetainerFactory { get; private set; }
        public IRetainerFactory<ICompilation> CompilationRetainerFactory { get; private set; }
        public ISolutionFactory SolutionFactory { get; private set; }

        protected WorkspaceHostServices(ILanguageServiceProviderFactory languageServiceProviderFactory)
        {
            this.LanguageServiceProviderFactory = languageServiceProviderFactory;
            this.TextFactory = this.CreateTextFactory();
            this.TaskSchedulerFactory = this.CreateTaskSchedulerFactory();
            
            this.BackgroundCompilerFactory = this.CreateBackgroundCompilerFactory();
            this.BackgroundParserFactory = this.CreateBackgroundParserFactory();
            this.PersistenceService = this.CreatePersistenceService();
            this.TemporaryStorageService = this.CreateTemporaryStorageService();
            this.ProjectDependencyService = this.CreateProjectDependencyService();

            this.TextRetainerFactory = this.CreateTextRetainerFactory();
            this.SyntaxTreeRetainerFactory = this.CreateSyntaxTreeRetainerFactory();
            this.CompilationRetainerFactory = this.CreateCompilationRetainerFactory();
            
            this.SolutionFactory = this.CreateSolutionFactory();
        }

        protected virtual ITextFactory CreateTextFactory()
        {
            return new StringTextFactory();
        }

        protected virtual IRetainerFactory<IText> CreateTextRetainerFactory()
        {
            return new TextRetainerFactory();
        }

        protected virtual IRetainerFactory<ISyntaxTree> CreateSyntaxTreeRetainerFactory()
        {
            return new SyntaxTreeRetainerFactory();
        }

        protected virtual IRetainerFactory<ICompilation> CreateCompilationRetainerFactory()
        {
            return new CompilationRetainerFactory();
        }

        protected virtual IWorkspaceTaskSchedulerFactory CreateTaskSchedulerFactory()
        {
            return new WorkspaceTaskSchedulerFactory();
        }

        protected virtual IBackgroundCompilerFactory CreateBackgroundCompilerFactory()
        {
            return new BackgroundCompilerFactory(this.TaskSchedulerFactory);
        }

        protected virtual IBackgroundParserFactory CreateBackgroundParserFactory()
        {
            return new BackgroundParserFactory(this.TaskSchedulerFactory);
        }

        protected virtual IPersistenceService CreatePersistenceService()
        {
            return new PersistenceService();
        }

        protected virtual IProjectDependencyService CreateProjectDependencyService()
        {
            return new ProjectDependencyService(this.PersistenceService);
        }

        protected virtual ITemporaryStorageService CreateTemporaryStorageService()
        {
            return new TemporaryStorageService(this.TextFactory);
        }

        protected virtual ISolutionFactory CreateSolutionFactory()
        {
            return new SolutionFactory(this.LanguageServiceProviderFactory, this.TextFactory, this.TextRetainerFactory, this.CompilationRetainerFactory);
        }
    }
}