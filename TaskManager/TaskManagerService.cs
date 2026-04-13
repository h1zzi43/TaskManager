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
            // === НАЧАЛО ОПЕРАЦИИ ===
            var operationId = Guid.NewGuid().ToString().Substring(0, 8);
            var stopwatch = Stopwatch.StartNew();

            Trace.WriteLine($"[TRACE] === НАЧАЛО AddTask [{operationId}] ===");
            Trace.TraceInformation($"[START] Операция AddTask. ID: {operationId}, Title: \"{title}\"");
            _logger.Debug("Начало операции AddTask. OperationId: {OperationId}, Title: {Title}",
                operationId, title);

            traceSource.TraceEvent(TraceEventType.Start, 1001,
                $"Операция AddTask начата. ID: {operationId}, Title: {title}");

            try
            {
                // === ВАЛИДАЦИЯ ===
                Trace.WriteLine($"[TRACE] [{operationId}] Проверка входных данных...");
                _logger.Debug("Проверка входных данных: {Title}", title);

                if (string.IsNullOrWhiteSpace(title))
                {
                    // === ПРЕДУПРЕЖДЕНИЕ ===
                    stopwatch.Stop();
                    Trace.TraceWarning($"[WARN] [{operationId}] Попытка добавить задачу с пустым названием. Время: {stopwatch.ElapsedMilliseconds}ms");
                    _logger.Warning("Операция AddTask завершена с ПРЕДУПРЕЖДЕНИЕМ: пустое название. OperationId: {OperationId}, Время: {ElapsedMs}ms",
                        operationId, stopwatch.ElapsedMilliseconds);
                    traceSource.TraceEvent(TraceEventType.Warning, 2001,
                        $"Попытка добавить задачу с пустым названием. ID: {operationId}");

                    Console.WriteLine("Ошибка: название задачи не может быть пустым!");
                    LogOperationResult("AddTask", operationId, "WARNING", stopwatch.ElapsedMilliseconds, "Пустое название");
                    return;
                }

                // === ОСНОВНАЯ ЛОГИКА ===
                Trace.WriteLine($"[TRACE] [{operationId}] Создание новой задачи...");
                var task = new TaskModel(title);
                tasks.Add(task);

                stopwatch.Stop();

                // === УСПЕХ ===
                Trace.TraceInformation($"[INFO] [{operationId}] Задача \"{title}\" успешно добавлена. ID задачи: {task.Id}, Всего задач: {tasks.Count}");
                Trace.WriteLine($"[TRACE] [{operationId}] Время выполнения: {stopwatch.ElapsedMilliseconds}ms");

                _logger.Information("Операция AddTask ВЫПОЛНЕНА УСПЕШНО: {@Task}, Всего задач: {TotalCount}, OperationId: {OperationId}, Время: {ElapsedMs}ms",
                    task, tasks.Count, operationId, stopwatch.ElapsedMilliseconds);

                traceSource.TraceEvent(TraceEventType.Information, 1002,
                    $"Задача '{title}' добавлена. ID: {task.Id}, Всего задач: {tasks.Count}, Время: {stopwatch.ElapsedMilliseconds}ms");

                Console.WriteLine($"Задача \"{title}\" добавлена!");
                LogOperationResult("AddTask", operationId, "SUCCESS", stopwatch.ElapsedMilliseconds, $"Задача: {title}");
            }
            catch (Exception ex)
            {
                // === ОШИБКА ===
                stopwatch.Stop();
                Trace.TraceError($"[ERROR] [{operationId}] Ошибка при добавлении задачи: {ex.Message}");
                Trace.WriteLine($"[TRACE] [{operationId}] StackTrace: {ex.StackTrace}");

                _logger.Error(ex, "Операция AddTask ЗАВЕРШЕНА С ОШИБКОЙ. OperationId: {OperationId}, Title: {Title}, Время: {ElapsedMs}ms",
                    operationId, title, stopwatch.ElapsedMilliseconds);

                traceSource.TraceEvent(TraceEventType.Error, 3001,
                    $"Ошибка добавления задачи: {ex.Message}. ID: {operationId}, Время: {stopwatch.ElapsedMilliseconds}ms");

                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                LogOperationResult("AddTask", operationId, "ERROR", stopwatch.ElapsedMilliseconds, ex.Message);
            }
            finally
            {
                // === КОНЕЦ ОПЕРАЦИИ ===
                Trace.WriteLine($"[TRACE] === КОНЕЦ AddTask [{operationId}] ===");
                traceSource.TraceEvent(TraceEventType.Stop, 1003,
                    $"Операция AddTask завершена. ID: {operationId}");
            }
        }

        public void RemoveTask(string title)
        {
            // === НАЧАЛО ОПЕРАЦИИ ===
            var operationId = Guid.NewGuid().ToString().Substring(0, 8);
            var stopwatch = Stopwatch.StartNew();

            Trace.WriteLine($"[TRACE] === НАЧАЛО RemoveTask [{operationId}] ===");
            Trace.TraceInformation($"[START] Операция RemoveTask. ID: {operationId}, Title: \"{title}\"");
            _logger.Debug("Начало операции RemoveTask. OperationId: {OperationId}, Title: {Title}",
                operationId, title);

            traceSource.TraceEvent(TraceEventType.Start, 2001,
                $"Операция RemoveTask начата. ID: {operationId}, Title: {title}");

            try
            {
                // === ПОИСК ЗАДАЧИ ===
                Trace.WriteLine($"[TRACE] [{operationId}] Поиск задачи для удаления...");
                _logger.Debug("Поиск задачи для удаления: {Title}", title);

                var taskToRemove = tasks.FirstOrDefault(t =>
                    t.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

                if (taskToRemove == null)
                {
                    // === ПРЕДУПРЕЖДЕНИЕ ===
                    stopwatch.Stop();
                    Trace.TraceWarning($"[WARN] [{operationId}] Задача \"{title}\" не найдена для удаления. Время: {stopwatch.ElapsedMilliseconds}ms");
                    _logger.Warning("Операция RemoveTask завершена с ПРЕДУПРЕЖДЕНИЕМ: задача не найдена. OperationId: {OperationId}, Title: {Title}, Время: {ElapsedMs}ms",
                        operationId, title, stopwatch.ElapsedMilliseconds);
                    traceSource.TraceEvent(TraceEventType.Warning, 2002,
                        $"Задача '{title}' не найдена для удаления. ID: {operationId}");

                    Console.WriteLine($"Задача \"{title}\" не найдена!");
                    LogOperationResult("RemoveTask", operationId, "WARNING", stopwatch.ElapsedMilliseconds, "Задача не найдена");
                    return;
                }

                // === ОСНОВНАЯ ЛОГИКА ===
                Trace.WriteLine($"[TRACE] [{operationId}] Задача найдена. Удаление...");
                var taskTitle = taskToRemove.Title;
                var taskCreatedDate = taskToRemove.CreatedDate;
                tasks.Remove(taskToRemove);

                stopwatch.Stop();

                // === УСПЕХ ===
                Trace.TraceInformation($"[INFO] [{operationId}] Задача \"{title}\" успешно удалена. Осталось задач: {tasks.Count}");
                Trace.WriteLine($"[TRACE] [{operationId}] Время выполнения: {stopwatch.ElapsedMilliseconds}ms");

                _logger.Information("Операция RemoveTask ВЫПОЛНЕНА УСПЕШНО: Задача '{TaskTitle}', Создана: {CreatedDate}, Осталось задач: {RemainingCount}, OperationId: {OperationId}, Время: {ElapsedMs}ms",
                    taskTitle, taskCreatedDate, tasks.Count, operationId, stopwatch.ElapsedMilliseconds);

                traceSource.TraceEvent(TraceEventType.Information, 2003,
                    $"Задача '{title}' удалена. Осталось задач: {tasks.Count}, Время: {stopwatch.ElapsedMilliseconds}ms");

                Console.WriteLine($"Задача \"{title}\" удалена!");
                LogOperationResult("RemoveTask", operationId, "SUCCESS", stopwatch.ElapsedMilliseconds, $"Задача: {title}");
            }
            catch (Exception ex)
            {
                // === ОШИБКА ===
                stopwatch.Stop();
                Trace.TraceError($"[ERROR] [{operationId}] Ошибка при удалении задачи: {ex.Message}");
                Trace.WriteLine($"[TRACE] [{operationId}] StackTrace: {ex.StackTrace}");

                _logger.Error(ex, "Операция RemoveTask ЗАВЕРШЕНА С ОШИБКОЙ. OperationId: {OperationId}, Title: {Title}, Время: {ElapsedMs}ms",
                    operationId, title, stopwatch.ElapsedMilliseconds);

                traceSource.TraceEvent(TraceEventType.Error, 3002,
                    $"Ошибка удаления задачи: {ex.Message}. ID: {operationId}, Время: {stopwatch.ElapsedMilliseconds}ms");

                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                LogOperationResult("RemoveTask", operationId, "ERROR", stopwatch.ElapsedMilliseconds, ex.Message);
            }
            finally
            {
                // === КОНЕЦ ОПЕРАЦИИ ===
                Trace.WriteLine($"[TRACE] === КОНЕЦ RemoveTask [{operationId}] ===");
                traceSource.TraceEvent(TraceEventType.Stop, 2004,
                    $"Операция RemoveTask завершена. ID: {operationId}");
            }
        }

        public void ListTasks()
        {
            // === НАЧАЛО ОПЕРАЦИИ ===
            var operationId = Guid.NewGuid().ToString().Substring(0, 8);
            var stopwatch = Stopwatch.StartNew();

            Trace.WriteLine($"[TRACE] === НАЧАЛО ListTasks [{operationId}] ===");
            Trace.TraceInformation($"[START] Операция ListTasks. ID: {operationId}");
            _logger.Debug("Начало операции ListTasks. OperationId: {OperationId}", operationId);

            traceSource.TraceEvent(TraceEventType.Start, 3001,
                $"Операция ListTasks начата. ID: {operationId}");

            try
            {
                // === ПРОВЕРКА ===
                Trace.WriteLine($"[TRACE] [{operationId}] Проверка списка задач. Всего задач: {tasks.Count}");
                _logger.Debug("Запрошен список задач, текущее количество: {Count}", tasks.Count);

                if (!tasks.Any())
                {
                    // === ПРЕДУПРЕЖДЕНИЕ (пустой список) ===
                    stopwatch.Stop();
                    Trace.TraceWarning($"[WARN] [{operationId}] Список задач пуст. Время: {stopwatch.ElapsedMilliseconds}ms");
                    _logger.Warning("Операция ListTasks завершена с ПРЕДУПРЕЖДЕНИЕМ: список пуст. OperationId: {OperationId}, Время: {ElapsedMs}ms",
                        operationId, stopwatch.ElapsedMilliseconds);
                    traceSource.TraceEvent(TraceEventType.Warning, 3002,
                        $"Список задач пуст. ID: {operationId}");

                    Console.WriteLine("Список задач пуст!");
                    LogOperationResult("ListTasks", operationId, "WARNING", stopwatch.ElapsedMilliseconds, "Список пуст");
                    return;
                }

                // === ОСНОВНАЯ ЛОГИКА (вывод списка) ===
                Trace.WriteLine($"[TRACE] [{operationId}] Вывод списка задач...");

                Console.WriteLine("\n=== Список задач ===");
                for (int i = 0; i < tasks.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {tasks[i]}");
                    Trace.WriteLine($"[TRACE] [{operationId}] Задача {i + 1}: {tasks[i]}");
                }
                Console.WriteLine("===================\n");

                stopwatch.Stop();

                // === УСПЕХ ===
                Trace.TraceInformation($"[INFO] [{operationId}] Выведен список из {tasks.Count} задач. Время: {stopwatch.ElapsedMilliseconds}ms");

                _logger.Information("Операция ListTasks ВЫПОЛНЕНА УСПЕШНО: Количество задач: {Count}, Задачи: {@Tasks}, OperationId: {OperationId}, Время: {ElapsedMs}ms",
                    tasks.Count, tasks.Select(t => new { t.Title, t.CreatedDate }), operationId, stopwatch.ElapsedMilliseconds);

                traceSource.TraceEvent(TraceEventType.Information, 3003,
                    $"Выведено {tasks.Count} задач. Время: {stopwatch.ElapsedMilliseconds}ms");

                LogOperationResult("ListTasks", operationId, "SUCCESS", stopwatch.ElapsedMilliseconds, $"Выведено {tasks.Count} задач");
            }
            catch (Exception ex)
            {
                // === ОШИБКА ===
                stopwatch.Stop();
                Trace.TraceError($"[ERROR] [{operationId}] Ошибка при выводе списка задач: {ex.Message}");
                Trace.WriteLine($"[TRACE] [{operationId}] StackTrace: {ex.StackTrace}");

                _logger.Error(ex, "Операция ListTasks ЗАВЕРШЕНА С ОШИБКОЙ. OperationId: {OperationId}, Время: {ElapsedMs}ms",
                    operationId, stopwatch.ElapsedMilliseconds);

                traceSource.TraceEvent(TraceEventType.Error, 3004,
                    $"Ошибка вывода списка задач: {ex.Message}. ID: {operationId}, Время: {stopwatch.ElapsedMilliseconds}ms");

                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                LogOperationResult("ListTasks", operationId, "ERROR", stopwatch.ElapsedMilliseconds, ex.Message);
            }
            finally
            {
                // === КОНЕЦ ОПЕРАЦИИ ===
                Trace.WriteLine($"[TRACE] === КОНЕЦ ListTasks [{operationId}] ===");
                traceSource.TraceEvent(TraceEventType.Stop, 3005,
                    $"Операция ListTasks завершена. ID: {operationId}");
            }
        }

        // ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ ЛОГИРОВАНИЯ РЕЗУЛЬТАТА ОПЕРАЦИИ
        private void LogOperationResult(string operation, string operationId, string result, long elapsedMs, string details)
        {
            var logMessage = $"[RESULT] {operation} | ID: {operationId} | RESULT: {result} | TIME: {elapsedMs}ms | DETAILS: {details}";
            Trace.WriteLine(logMessage);
            _logger.Debug("Результат операции: {Operation}, {Result}, Время: {ElapsedMs}ms, Детали: {Details}",
                operation, result, elapsedMs, details);
        }

        public int GetTaskCount()
        {
            _logger.Verbose("Запрошено количество задач: {Count}", tasks.Count);
            return tasks.Count;
        }

        public void Close()
        {
            _logger.Debug("Закрытие TaskManagerService");
            Trace.TraceInformation("[CLOSE] TaskManagerService закрыт");
            traceSource.Close();
        }
    }
}