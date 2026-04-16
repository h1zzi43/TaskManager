using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Serilog;

namespace TaskManager
{
    public class TaskManagerService
    {
        private List<TaskModel> tasks = new List<TaskModel>();
        private TraceSource traceSource;
        private readonly ILogger _logger = Log.ForContext<TaskManagerService>();

        public TaskManagerService()
        {
            traceSource = new TraceSource("TaskManagerTraceSource");
            traceSource.Switch = new SourceSwitch("TaskManagerSwitch", "All");

            _logger.Debug("TaskManagerService инициализирован");
            Trace.TraceInformation("[INIT] TaskManagerService создан");
        }

        public void AddTask(string title)
        {
            // Используем централизованную обработку исключений
            ExceptionHandler.TryExecute(() =>
            {
                var operationId = Guid.NewGuid().ToString().Substring(0, 8);
                var stopwatch = Stopwatch.StartNew();

                Trace.WriteLine($"[TRACE] === НАЧАЛО AddTask [{operationId}] ===");
                _logger.Debug("Начало операции AddTask. OperationId: {OperationId}, Title: {Title}", operationId, title);

                traceSource.TraceEvent(TraceEventType.Start, 1001, $"Операция AddTask начата. ID: {operationId}");

                // Валидация
                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Название задачи не может быть пустым!", nameof(title));
                }

                // Основная логика
                var task = new TaskModel(title);
                tasks.Add(task);

                stopwatch.Stop();

                _logger.Information("Задача добавлена: {@Task}, Всего задач: {TotalCount}, Время: {ElapsedMs}ms",
                    task, tasks.Count, stopwatch.ElapsedMilliseconds);

                traceSource.TraceEvent(TraceEventType.Information, 1002, $"Задача '{title}' добавлена. ID: {task.Id}");

                Console.WriteLine($"Задача \"{title}\" добавлена!");

                Trace.WriteLine($"[TRACE] === КОНЕЦ AddTask [{operationId}] ===");

            }, "AddTask", new { Title = title }, LogLevel.Error);
        }

        public void RemoveTask(string title)
        {
            ExceptionHandler.TryExecute(() =>
            {
                var operationId = Guid.NewGuid().ToString().Substring(0, 8);
                var stopwatch = Stopwatch.StartNew();

                Trace.WriteLine($"[TRACE] === НАЧАЛО RemoveTask [{operationId}] ===");
                _logger.Debug("Начало операции RemoveTask. OperationId: {OperationId}, Title: {Title}", operationId, title);

                var taskToRemove = tasks.FirstOrDefault(t =>
                    t.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

                if (taskToRemove == null)
                {
                    throw new KeyNotFoundException($"Задача \"{title}\" не найдена!");
                }

                tasks.Remove(taskToRemove);
                stopwatch.Stop();

                _logger.Information("Задача удалена: '{TaskTitle}', Осталось задач: {RemainingCount}, Время: {ElapsedMs}ms",
                    title, tasks.Count, stopwatch.ElapsedMilliseconds);

                Console.WriteLine($"Задача \"{title}\" удалена!");

                Trace.WriteLine($"[TRACE] === КОНЕЦ RemoveTask [{operationId}] ===");

            }, "RemoveTask", new { Title = title }, LogLevel.Warning); // Warning уровень для "не найдено"
        }

        public void ListTasks()
        {
            ExceptionHandler.TryExecute(() =>
            {
                var operationId = Guid.NewGuid().ToString().Substring(0, 8);
                var stopwatch = Stopwatch.StartNew();

                Trace.WriteLine($"[TRACE] === НАЧАЛО ListTasks [{operationId}] ===");
                _logger.Debug("Начало операции ListTasks. OperationId: {OperationId}", operationId);

                if (!tasks.Any())
                {
                    Console.WriteLine("Список задач пуст!");
                    _logger.Information("Список задач пуст");
                    return;
                }

                Console.WriteLine("\n=== Список задач ===");
                for (int i = 0; i < tasks.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {tasks[i]}");
                }
                Console.WriteLine("===================\n");

                stopwatch.Stop();
                _logger.Information("Выведен список из {Count} задач, Время: {ElapsedMs}ms", tasks.Count, stopwatch.ElapsedMilliseconds);

                Trace.WriteLine($"[TRACE] === КОНЕЦ ListTasks [{operationId}] ===");

            }, "ListTasks", null, LogLevel.Error);
        }

        public int GetTaskCount()
        {
            return ExceptionHandler.TryExecute(() => tasks.Count, "GetTaskCount", null, LogLevel.Error, 0);
        }

        public void Close()
        {
            _logger.Debug("Закрытие TaskManagerService");
            traceSource.Close();
        }
    }
}