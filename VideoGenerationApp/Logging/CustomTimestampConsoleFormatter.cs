using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using System;
using System.IO;

namespace VideoGenerationApp.Logging
{
    /// <summary>
    /// Custom console formatter that adds timestamps to log messages
    /// and filters out component rendering/disposing messages
    /// </summary>
    public sealed class CustomTimestampConsoleFormatter : ConsoleFormatter
    {
        public CustomTimestampConsoleFormatter() : base("custom-timestamp")
        {
        }

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider? scopeProvider,
            TextWriter textWriter)
        {
            string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
            
            if (message == null)
            {
                return;
            }

            // Filter out component rendering and disposing messages
            string category = logEntry.Category ?? "";
            if (category.Contains("Microsoft.AspNetCore.Components.RenderTree.Renderer") ||
                category.Contains("Microsoft.AspNetCore.Components.Endpoints.RazorComponentEndpointInvoker") ||
                message.Contains("Rendering component") || 
                message.Contains("Disposing component") ||
                message.Contains("Initializing component") ||
                message.Contains("Begin render root component"))
            {
                return;
            }

            // Write timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            textWriter.Write($"[{timestamp}] ");

            // Write log level
            string logLevel = logEntry.LogLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => "none"
            };
            textWriter.Write($"{logLevel}: ");

            // Write category
            textWriter.Write($"{logEntry.Category}[{logEntry.EventId.Id}]");
            textWriter.WriteLine();

            // Write message with indentation
            textWriter.Write("      ");
            textWriter.WriteLine(message);

            // Write exception if present
            if (logEntry.Exception != null)
            {
                textWriter.Write("      ");
                textWriter.WriteLine(logEntry.Exception.ToString());
            }
        }
    }
}
