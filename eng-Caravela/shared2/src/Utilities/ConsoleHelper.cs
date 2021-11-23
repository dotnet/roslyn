using Spectre.Console;
using System;
using System.IO;

namespace PostSharp.Engineering.BuildTools.Utilities
{
    public class ConsoleHelper
    {
        public IAnsiConsole Out { get; }
        public IAnsiConsole Error { get; }

        public void WriteError( string format, params object[] args ) =>
            this.WriteError( string.Format( format, args ) );

        public void WriteError( string message )
        {
            this.Error.MarkupLine( $"[red]{message.EscapeMarkup()}[/]" );
        }

        public void WriteWarning( string message )
        {
            this.Out.MarkupLine( $"[yellow]{message.EscapeMarkup()}[/]" );
        }

        public void WriteWarning( string format, params object[] args ) =>
            this.WriteWarning( string.Format( format, args ) );

        public void WriteMessage( string message )
        {
            this.Out.MarkupLine( "[dim]" + message.EscapeMarkup() + "[/]" );
        }

        public void WriteMessage( string format, params object[] args ) =>
            this.WriteMessage( string.Format( format, args ) );


        public void WriteImportantMessage( string message )
        {
            this.Out.MarkupLine( "[bold]" + message.EscapeMarkup() + "[/]" );
        }

        public void WriteImportantMessage( string format, params object[] args ) =>
            this.WriteImportantMessage( string.Format( format, args ) );


        public void WriteSuccess( string message )
        {
            this.Out.MarkupLine( $"[green]{message.EscapeMarkup()}[/]" );
        }

        public void WriteHeading( string message )
        {
            this.Out.MarkupLine(
                $"[bold cyan]===== {message.EscapeMarkup()} {new string( '=', 160 - message.Length )}[/]" );
        }

        public ConsoleHelper()
        {
            var factory = new AnsiConsoleFactory();

            IAnsiConsole CreateConsole( TextWriter writer )
                => factory.Create( new AnsiConsoleSettings { Out = new AnsiConsoleOutputWrapper( writer ) } );

            this.Out = CreateConsole( Console.Out );
            this.Error = CreateConsole( Console.Error );
        }
    }
}