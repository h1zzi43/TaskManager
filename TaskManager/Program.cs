using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;


namespace TaskManager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SetupLogging();

            Trace.TraceInformation("[INFO] Приложение TaskManager запущено.");
            Trace.WriteLine("[TRACE] Инициализация менеджера задач...");

            var taskManager = new TaskManagerService();

            Console.WriteLine("=== TaskManager с логированием ===");
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("  add    - добавить задачу");
            Console.WriteLine("  remove - удалить задачу");
            Console.WriteLine("  list   - показать все задачи");
            Console.WriteLine("  help   - показать справку");
            Console.WriteLine("  exit   - выход из программы");
            Console.WriteLine("===================================\n");

            bool isRunning = true;

            while (isRunning)
            {
                Console.Write("Введите команду: ");
                string command = Console.ReadLine()?.Trim().ToLower();

                switch (command)
                {
                    case "add":
                        Trace.WriteLine("[TRACE] Обработка команды Add...");
                        Console.Write("Введите название задачи: ");
                        string title = Console.ReadLine()?.Trim();
                        taskManager.AddTask(title);
                        break;

                    case "remove":
                        Trace.WriteLine("[TRACE] Обработка команды Remove...");
                        Console.Write("Введите название задачи для удаления: ");
                        string taskToRemove = Console.ReadLine()?.Trim();
                        taskManager.RemoveTask(taskToRemove);
                        break;

                    case "list":
                        Trace.WriteLine("[TRACE] Обработка команды List...");
                        taskManager.ListTasks();
                        break;

                    case "help":
                        ShowHelp();
                        break;

                    case "exit":
                        Trace.TraceInformation("[INFO] Завершение работы приложения...");
                        Trace.WriteLine("[TRACE] Выполнение команды Exit...");
                        isRunning = false;
                        break;

                    default:
                        Trace.TraceWarning($"[WARN] Введена неизвестная команда: {command}");
                        Console.WriteLine("Неизвестная команда. Введите 'help' для справки.");
                        break;
                }

                Console.WriteLine(); 
            }

            Trace.TraceInformation("[INFO] Приложение TaskManager завершено корректно.");
            taskManager.Close();

            Thread.Sleep(100);
            Trace.Flush();

            Console.WriteLine("Приложение завершено. Логи сохранены в файлы.");
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();

            Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Debug()
           .WriteTo.Console()
           .WriteTo.File("logs/log-.txt",
               rollingInterval: RollingInterval.Day,
               outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
           .CreateLogger();

            try
            {
                Log.Information("Приложение TaskManager запущено");

                // Здесь будет ваш код

                Log.Information("Приложение завершило работу");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Критическая ошибка в приложении");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static void SetupLogging()
        {
            try
            {
                Trace.Listeners.Clear();

                Trace.Listeners.Add(new ConsoleTraceListener());

                var infoLogFile = new TextWriterTraceListener("taskmanager-info.log");
                infoLogFile.Filter = new EventTypeFilter(SourceLevels.Information);
                Trace.Listeners.Add(infoLogFile);

                var errorLogFile = new TextWriterTraceListener("taskmanager-error.log");
                errorLogFile.Filter = new EventTypeFilter(SourceLevels.Warning);
                Trace.Listeners.Add(errorLogFile);

                var traceLogFile = new TextWriterTraceListener("taskmanager-trace.log");
                traceLogFile.Filter = new EventTypeFilter(SourceLevels.All);
                Trace.Listeners.Add(traceLogFile);

                Trace.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка настройки логирования: {ex.Message}");
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

            Trace.TraceInformation("[INFO] Выведена справка по командам.");
        }
    }
}



