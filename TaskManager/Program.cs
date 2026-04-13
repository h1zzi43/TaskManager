using Serilog;
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
            // СОЗДАЁМ ПАПКУ ДЛЯ СЕССИИ
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _sessionFolder = Path.Combine(Directory.GetCurrentDirectory(), $"Logs_{timestamp}");
            Directory.CreateDirectory(_sessionFolder);

            // ПЕРЕХВАТ КОНСОЛЬНОГО ВЫВОДА В ФАЙЛ
            _consoleLogPath = Path.Combine(_sessionFolder, "console_output.txt");
            var consoleWriter = new StreamWriter(_consoleLogPath, false) { AutoFlush = true };
            Console.SetOut(new TeeWriter(Console.Out, consoleWriter));
            Console.SetError(new TeeWriter(Console.Error, consoleWriter));

            Console.WriteLine($"=== СЕССИЯ ЗАПУЩЕНА: {DateTime.Now} ===");
            Console.WriteLine($"Папка логов: {_sessionFolder}\n");

            // НАСТРОЙКА STRUCTURED LOGGING (SERILOG)
            string jsonLogPath = Path.Combine(_sessionFolder, "structured_logs.json");
            string textLogPath = Path.Combine(_sessionFolder, "structured_logs.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Properties}")
                .WriteTo.File(
                    path: jsonLogPath,
                    rollingInterval: RollingInterval.Infinite,
                    formatter: new Serilog.Formatting.Json.JsonFormatter())
                .WriteTo.File(
                    path: textLogPath,
                    rollingInterval: RollingInterval.Infinite,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Приложение TaskManager запущено. Сессия: {SessionFolder}", _sessionFolder);

            // НАСТРОЙКА TRACE ЛОГИРОВАНИЯ В ПАПКУ СЕССИИ
            SetupLogging();

            Trace.TraceInformation($"[INFO] Приложение TaskManager запущено. Сессия: {_sessionFolder}");
            Trace.WriteLine($"[TRACE] Инициализация менеджера задач...");

            var taskManager = new TaskManagerService();

            Console.WriteLine("=== TaskManager с СТРУКТУРИРОВАННЫМ логированием ===");
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("  add    - добавить задачу");
            Console.WriteLine("  remove - удалить задачу");
            Console.WriteLine("  list   - показать все задачи");
            Console.WriteLine("  help   - показать справку");
            Console.WriteLine("  exit   - выход из программы");
            Console.WriteLine("================================================\n");

            bool isRunning = true;

            while (isRunning)
            {
                Console.Write("Введите команду: ");
                string command = Console.ReadLine()?.Trim().ToLower();

                switch (command)
                {
                    case "add":
                        Log.Debug("Пользователь выбрал команду Add");
                        Trace.WriteLine("[TRACE] Обработка команды Add...");
                        Console.Write("Введите название задачи: ");
                        string title = Console.ReadLine()?.Trim();
                        taskManager.AddTask(title);
                        break;

                    case "remove":
                        Log.Debug("Пользователь выбрал команду Remove");
                        Trace.WriteLine("[TRACE] Обработка команды Remove...");
                        Console.Write("Введите название задачи для удаления: ");
                        string taskToRemove = Console.ReadLine()?.Trim();
                        taskManager.RemoveTask(taskToRemove);
                        break;

                    case "list":
                        Log.Debug("Пользователь выбрал команду List");
                        Trace.WriteLine("[TRACE] Обработка команды List...");
                        taskManager.ListTasks();
                        break;

                    case "help":
                        ShowHelp();
                        break;

                    case "exit":
                        Log.Information("Пользователь завершил работу приложения");
                        Trace.TraceInformation("[INFO] Завершение работы приложения...");
                        Trace.WriteLine("[TRACE] Выполнение команды Exit...");
                        isRunning = false;
                        break;

                    default:
                        Log.Warning("Введена неизвестная команда: {Command}", command);
                        Trace.TraceWarning($"[WARN] Введена неизвестная команда: {command}");
                        Console.WriteLine("Неизвестная команда. Введите 'help' для справки.");
                        break;
                }

                Console.WriteLine();
            }

            Log.Information("Приложение TaskManager завершено корректно");
            Trace.TraceInformation("[INFO] Приложение TaskManager завершено корректно.");

            taskManager.Close();
            Thread.Sleep(100);
            Trace.Flush();
            Log.CloseAndFlush();

            // ЗАКРЫВАЕМ ПЕРЕХВАТ КОНСОЛИ
            consoleWriter.Close();

            Console.WriteLine($"\n✅ Приложение завершено. Все логи сохранены в папку:");
            Console.WriteLine($"   {_sessionFolder}");
            Console.WriteLine($"\n📁 Файлы в папке:");
            Console.WriteLine($"   - structured_logs.json     (структурированные логи)");
            Console.WriteLine($"   - structured_logs.txt      (текстовые логи)");
            Console.WriteLine($"   - console_output.txt       (всё, что вы видели в консоли)");
            Console.WriteLine($"   - taskmanager-info.log     (Trace Info)");
            Console.WriteLine($"   - taskmanager-error.log    (Trace Errors)");
            Console.WriteLine($"   - taskmanager-trace.log    (Trace All)");
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static void SetupLogging()
        {
            try
            {
                Trace.Listeners.Clear();

                Trace.Listeners.Add(new ConsoleTraceListener());

                // ЛОГИ ТЕПЕРЬ СОХРАНЯЮТСЯ В ПАПКУ СЕССИИ
                var infoLogFile = new TextWriterTraceListener(Path.Combine(_sessionFolder, "taskmanager-info.log"));
                infoLogFile.Filter = new EventTypeFilter(SourceLevels.Information);
                Trace.Listeners.Add(infoLogFile);

                var errorLogFile = new TextWriterTraceListener(Path.Combine(_sessionFolder, "taskmanager-error.log"));
                errorLogFile.Filter = new EventTypeFilter(SourceLevels.Warning);
                Trace.Listeners.Add(errorLogFile);

                var traceLogFile = new TextWriterTraceListener(Path.Combine(_sessionFolder, "taskmanager-trace.log"));
                traceLogFile.Filter = new EventTypeFilter(SourceLevels.All);
                Trace.Listeners.Add(traceLogFile);

                Trace.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка настройки логирования: {ex.Message}");
                Log.Fatal(ex, "Ошибка настройки Trace логирования");
                Trace.TraceError($"[ERROR] Ошибка настройки логирования: {ex.Message}");
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("\n=== Справка по командам ===");
            Console.WriteLine("add    - Добавить новую задачу");
            Console.WriteLine("         После ввода команды будет запрошено название задачи");
            Console.WriteLine("remove - Удалить существующую задачу");
            Console.WriteLine("         После ввода команды будет запрошено название задачи для удаления");
            Console.WriteLine("list   - Показать все текущие задачи");
            Console.WriteLine("exit   - Завершить работу приложения");
            Console.WriteLine("===========================\n");

            Log.Information("Выведена справка по командам пользователю {User}", Environment.UserName);
            Trace.TraceInformation("[INFO] Выведена справка по командам.");
        }
    }

    // ВСПОМОГАТЕЛЬНЫЙ КЛАСС ДЛЯ ДУБЛИРОВАНИЯ КОНСОЛЬНОГО ВЫВОДА
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