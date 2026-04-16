using System;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace TaskManager
{
    public static class ExceptionHandler
    {
        public static event Action<Exception, string> OnExceptionCaught;

        static ExceptionHandler()
        {
            Log.Debug("ExceptionHandler инициализирован");
        }

        public static void HandleException(
            Exception ex,
            string operation,
            object contextData = null,
            LogLevel level = LogLevel.Error)
        {
            var stackTrace = new StackTrace(true);
            var callerFrame = stackTrace.GetFrame(1);
            var callerMethod = callerFrame?.GetMethod()?.Name ?? "Unknown";
            var callerClass = callerFrame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";

            var errorInfo = new
            {
                Exception = new
                {
                    Type = ex.GetType().FullName,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Source = ex.Source,
                    TargetSite = ex.TargetSite?.ToString(),
                    InnerException = ex.InnerException != null ? new
                    {
                        Type = ex.InnerException.GetType().FullName,
                        Message = ex.InnerException.Message,
                        StackTrace = ex.InnerException.StackTrace
                    } : null
                },
                Operation = operation,
                Context = contextData,
                Caller = new
                {
                    Class = callerClass,
                    Method = callerMethod
                },
                Timestamp = DateTime.Now,
                ThreadId = Environment.CurrentManagedThreadId
            };

            switch (level)
            {
                case LogLevel.Fatal:
                    Log.Fatal(ex, "[FATAL] Ошибка в операции {Operation}: {ErrorMessage}. Контекст: {@Context}",
                        operation, ex.Message, errorInfo);
                    Trace.TraceError($"[FATAL] {operation} | {ex.Message}");
                    Trace.TraceError($"StackTrace: {ex.StackTrace}");
                    break;

                case LogLevel.Error:
                    Log.Error(ex, "[ERROR] Ошибка в операции {Operation}: {ErrorMessage}. Контекст: {@Context}",
                        operation, ex.Message, errorInfo);
                    Trace.TraceError($"[ERROR] {operation} | {ex.Message}");
                    Trace.TraceError($"StackTrace: {ex.StackTrace}");
                    break;

                case LogLevel.Warning:
                    Log.Warning(ex, "[WARNING] Ошибка в операции {Operation}: {ErrorMessage}",
                        operation, ex.Message);
                    Trace.TraceWarning($"[WARNING] {operation} | {ex.Message}");
                    break;
            }

            WriteToErrorLog(errorInfo, level);
            NotifyUser(ex, operation, level);
            OnExceptionCaught?.Invoke(ex, operation);
        }

        private static void WriteToErrorLog(object errorInfo, LogLevel level)
        {
            try
            {
                string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "errors.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {System.Text.Json.JsonSerializer.Serialize(errorInfo)}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
                File.AppendAllText(errorLogPath, logEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write error log: {ex.Message}");
            }
        }

        private static void NotifyUser(Exception ex, string operation, LogLevel level)
        {
            var originalColor = Console.ForegroundColor;

            if (level == LogLevel.Fatal || level == LogLevel.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n!!! ОШИБКА !!!");
                Console.WriteLine($"Операция: {operation}");
                Console.WriteLine($"Сообщение: {ex.Message}");

                if (level == LogLevel.Fatal)
                {
                    Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА! Приложение может работать некорректно.");
                }
                Console.ForegroundColor = originalColor;

                if (level == LogLevel.Fatal)
                {
                    Console.Beep();
                }
            }
            else if (level == LogLevel.Warning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n!!! ПРЕДУПРЕЖДЕНИЕ !!!");
                Console.WriteLine($"Операция: {operation}");
                Console.WriteLine($"Сообщение: {ex.Message}");
                Console.ForegroundColor = originalColor;
            }
        }

        public static bool TryExecute(Action action, string operation, object context = null, LogLevel level = LogLevel.Error)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, operation, context, level);
                return false;
            }
        }

        public static T TryExecute<T>(Func<T> func, string operation, object context = null, LogLevel level = LogLevel.Error, T defaultValue = default(T))
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                HandleException(ex, operation, context, level);
                return defaultValue;
            }
        }
    }

    public enum LogLevel
    {
        Warning,
        Error,
        Fatal
    }
}