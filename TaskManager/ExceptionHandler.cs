using System;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace TaskManager
{
    /// <summary>
    /// Централизованный обработчик исключений
    /// </summary>
    public static class ExceptionHandler
    {
        // Событие для оповещений (можно подписаться из Program.cs)
        public static event Action<Exception, string> OnExceptionCaught;

        static ExceptionHandler()
        {
            // Инициализация обработчика
            Log.Debug("ExceptionHandler инициализирован");
        }

        /// <summary>
        /// Обработка исключения с контекстом
        /// </summary>
        /// <param name="ex">Исключение</param>
        /// <param name="operation">Операция, во время которой возникла ошибка</param>
        /// <param name="contextData">Дополнительные данные контекста</param>
        /// <param name="level">Уровень ошибки</param>
        public static void HandleException(
            Exception ex,
            string operation,
            object contextData = null,
            LogLevel level = LogLevel.Error)
        {
            // Получаем информацию о вызове
            var stackTrace = new StackTrace(true);
            var callerFrame = stackTrace.GetFrame(1);
            var callerMethod = callerFrame?.GetMethod()?.Name ?? "Unknown";
            var callerClass = callerFrame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";

            // Формируем детальную информацию об ошибке
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

            // Логирование в зависимости от уровня
            switch (level)
            {
                case LogLevel.Fatal:
                    Log.Fatal(ex, "[FATAL] Ошибка в операции {Operation}: {ErrorMessage}. Контекст: {@Context}",
                        operation, ex.Message, errorInfo);
                    Trace.TraceError($"[FATAL] {operation} | {ex.Message} | {ex.StackTrace}");
                    break;

                case LogLevel.Error:
                    Log.Error(ex, "[ERROR] Ошибка в операции {Operation}: {ErrorMessage}. Контекст: {@Context}",
                        operation, ex.Message, errorInfo);
                    Trace.TraceError($"[ERROR] {operation} | {ex.Message}");
                    Trace.WriteLine($"[TRACE] StackTrace: {ex.StackTrace}");
                    break;

                case LogLevel.Warning:
                    Log.Warning(ex, "[WARNING] Ошибка в операции {Operation}: {ErrorMessage}",
                        operation, ex.Message);
                    Trace.TraceWarning($"[WARN] {operation} | {ex.Message}");
                    break;
            }

            // Запись в отдельный файл ошибок
            WriteToErrorLog(errorInfo, level);

            // Оповещение пользователя
            NotifyUser(ex, operation, level);

            // Вызов события для дополнительных обработчиков (Sentry, Email и т.д.)
            OnExceptionCaught?.Invoke(ex, operation);
        }

        /// <summary>
        /// Запись ошибки в отдельный файл
        /// </summary>
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
                // Не логируем ошибку логирования, чтобы избежать рекурсии
                Debug.WriteLine($"Failed to write error log: {ex.Message}");
            }
        }

        /// <summary>
        /// Оповещение пользователя
        /// </summary>
        private static void NotifyUser(Exception ex, string operation, LogLevel level)
        {
            // Цветовое выделение в консоли
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

                // Звуковое оповещение (опционально)
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

        /// <summary>
        /// Попытка выполнить действие с автоматической обработкой ошибок
        /// </summary>
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

        /// <summary>
        /// Попытка выполнить действие с возвратом результата
        /// </summary>
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

    /// <summary>
    /// Уровни логирования ошибок
    /// </summary>
    public enum LogLevel
    {
        Warning,
        Error,
        Fatal
    }
}