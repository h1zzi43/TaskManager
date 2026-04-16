using Serilog;
using Serilog.Formatting.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace TaskManager
{
    internal class Program
    {
        private static string _sessionFolder;
        private static string _consoleLogPath;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    ExceptionHandler.HandleException(ex, "UnhandledException",
                        new { IsTerminating = e.IsTerminating }, LogLevel.Fatal);
                }

                Console.WriteLine("\n!!! КРИТИЧЕСКАЯ ОШИБКА !!!");
                Console.WriteLine("Приложение будет закрыто.");
                Thread.Sleep(3000);
            };

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _sessionFolder = Path.Combine(Directory.GetCurrentDirectory(), $"Logs_{timestamp}");
            Directory.CreateDirectory(_sessionFolder);

            _consoleLogPath = Path.Combine(_sessionFolder, "console_output.txt");
            var consoleWriter = new StreamWriter(_consoleLogPath, false) { AutoFlush = true };
            Console.SetOut(new TeeWriter(Console.Out, consoleWriter));
            Console.SetError(new TeeWriter(Console.Error, consoleWriter));

            Console.WriteLine($"=== СЕССИЯ ЗАПУЩЕНА: {DateTime.Now} ===");
            Console.WriteLine($"Папка логов: {_sessionFolder}\n");

            string jsonLogPath = Path.Combine(_sessionFolder, "structured_logs.json");
            string textLogPath = Path.Combine(_sessionFolder, "structured_logs.txt");

            var jsonFormatter = new JsonFormatter();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Properties}")
                .WriteTo.File(jsonFormatter, jsonLogPath,
                    rollingInterval: RollingInterval.Infinite)
                .WriteTo.File(textLogPath,
                    rollingInterval: RollingInterval.Infinite,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Приложение TaskManager запущено. Сессия: {SessionFolder}", _sessionFolder);

            SetupLogging();

            var taskManager = new TaskManagerService();

            Console.WriteLine("=== TaskManager с ЦЕНТРАЛИЗОВАННОЙ ОБРАБОТКОЙ ОШИБОК ===");
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("  add    - добавить задачу");
            Console.WriteLine("  remove - удалить задачу");
            Console.WriteLine("  list   - показать все задачи");
            Console.WriteLine("  help   - показать справку");
            Console.WriteLine("  error  - симулировать ошибку (демонстрация)");
            Console.WriteLine("  exit   - выход из программы");
            Console.WriteLine("====================================================\n");

            bool isRunning = true;

            while (isRunning)
            {
                Console.Write("Введите команду: ");
                string command = Console.ReadLine()?.Trim().ToLower();

                switch (command)
                {
                    case "add":
                        Console.Write("Введите название задачи: ");
                        string title = Console.ReadLine()?.Trim();
                        taskManager.AddTask(title);
                        break;

                    case "remove":
                        Console.Write("Введите название задачи для удаления: ");
                        string taskToRemove = Console.ReadLine()?.Trim();
                        taskManager.RemoveTask(taskToRemove);
                        break;

                    case "list":
                        taskManager.ListTasks();
                        break;

                    case "error":
                        Console.WriteLine("Демонстрация ошибки...");
                        ExceptionHandler.TryExecute(() =>
                        {
                            throw new InvalidOperationException("Это тестовая ошибка для демонстрации работы обработчика!");
                        }, "TestError", new { Demo = true }, LogLevel.Error);
                        break;

                    case "help":
                        ShowHelp();
                        break;

                    case "exit":
                        isRunning = false;
                        break;

                    default:
                        Console.WriteLine("Неизвестная команда. Введите 'help' для справки.");
                        break;
                }

                Console.WriteLine();
            }

            Log.Information("Приложение TaskManager завершено корректно");
            taskManager.Close();

            Log.CloseAndFlush();
            consoleWriter.Close();

            Console.WriteLine($"\n✅ Приложение завершено. Логи сохранены в: {_sessionFolder}");
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static void SetupLogging()
        {
            try
            {
                Trace.Listeners.Clear();

                ConsoleTraceListener consoleListener = new ConsoleTraceListener();
                consoleListener.Filter = new EventTypeFilter(SourceLevels.All);
                Trace.Listeners.Add(consoleListener);

                TextWriterTraceListener infoListener = new TextWriterTraceListener(Path.Combine(_sessionFolder, "taskmanager-info.log"));
                infoListener.Filter = new EventTypeFilter(SourceLevels.Information);
                Trace.Listeners.Add(infoListener);

                TextWriterTraceListener errorListener = new TextWriterTraceListener(Path.Combine(_sessionFolder, "taskmanager-error.log"));
                errorListener.Filter = new EventTypeFilter(SourceLevels.Error);
                Trace.Listeners.Add(errorListener);

                TextWriterTraceListener warningListener = new TextWriterTraceListener(Path.Combine(_sessionFolder, "taskmanager-warning.log"));
                warningListener.Filter = new EventTypeFilter(SourceLevels.Warning);
                Trace.Listeners.Add(warningListener);

                TextWriterTraceListener traceListener = new TextWriterTraceListener(Path.Combine(_sessionFolder, "taskmanager-trace.log"));
                traceListener.Filter = new EventTypeFilter(SourceLevels.Verbose);
                Trace.Listeners.Add(traceListener);

                Trace.AutoFlush = true;

                Trace.TraceInformation("Логирование настроено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка настройки логирования: {ex.Message}");
                Log.Fatal(ex, "Ошибка настройки Trace логирования");
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("\n=== Справка по командам ===");
            Console.WriteLine("add    - Добавить новую задачу");
            Console.WriteLine("remove - Удалить задачу");
            Console.WriteLine("list   - Показать все задачи");
            Console.WriteLine("error  - Симулировать ошибку (тест)");
            Console.WriteLine("exit   - Выход");
            Console.WriteLine("===========================\n");
        }
    }

    public class TeeWriter : TextWriter
    {
        private readonly TextWriter _original;
        private readonly TextWriter _file;

        public TeeWriter(TextWriter original, TextWriter file)
        {
            _original = original;
            _file = file;
        }

        public override void Write(char value)
        {
            _original.Write(value);
            _file.Write(value);
        }

        public override void Write(string value)
        {
            _original.Write(value);
            _file.Write(value);
        }

        public override void WriteLine(string value)
        {
            _original.WriteLine(value);
            _file.WriteLine(value);
        }

        public override Encoding Encoding => _original.Encoding;
    }
}