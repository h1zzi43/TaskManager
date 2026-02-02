using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
    public class TaskManagerService
    {
        private List<TaskModel> tasks = new List<TaskModel>();
        private TraceSource traceSource;

        public TaskManagerService()
        {
            traceSource = new TraceSource("TaskManagerTraceSource");

            traceSource.Switch = new SourceSwitch("TaskManagerSwitch", "All");
        }

        public void AddTask(string title)
        {
            Trace.WriteLine("[TRACE] Начало операции AddTask.");
            traceSource.TraceEvent(TraceEventType.Start, 1001, "Операция AddTask начата");

            try
            {
                traceSource.TraceInformation($"[TRACE] Проверка введённого названия: \"{title}\"");

                if (string.IsNullOrWhiteSpace(title))
                {
                    Trace.TraceWarning("[WARN] Пользователь ввёл пустое название. Операция add не выполнена.");
                    traceSource.TraceEvent(TraceEventType.Warning, 2001,
                        "Попытка добавить задачу с пустым названием");
                    Console.WriteLine("Ошибка: название задачи не может быть пустым!");
                    return;
                }

                var task = new TaskModel(title);
                tasks.Add(task);

                Trace.TraceInformation($"[INFO] Задача \"{title}\" успешно добавлена.");
                traceSource.TraceEvent(TraceEventType.Information, 1002,
                    $"Задача '{title}' добавлена. Всего задач: {tasks.Count}");

                Console.WriteLine($"Задача \"{title}\" добавлена!");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[ERROR] Ошибка при добавлении задачи: {ex.Message}");
                traceSource.TraceEvent(TraceEventType.Error, 3001,
                    $"Ошибка добавления задачи: {ex.Message}");
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                Trace.WriteLine("[TRACE] Конец операции AddTask.");
                traceSource.TraceEvent(TraceEventType.Stop, 1003, "Операция AddTask завершена");
            }
        }

        public void RemoveTask(string title)
        {
            Trace.WriteLine("[TRACE] Начало операции RemoveTask.");
            traceSource.TraceEvent(TraceEventType.Start, 2001, "Операция RemoveTask начата");

            try
            {
                traceSource.TraceInformation($"[TRACE] Поиск задачи для удаления: \"{title}\"");

                var taskToRemove = tasks.FirstOrDefault(t =>
                    t.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

                if (taskToRemove == null)
                {
                    Trace.TraceError($"[ERROR] Задача \"{title}\" не найдена для удаления.");
                    traceSource.TraceEvent(TraceEventType.Error, 3002,
                        $"Задача '{title}' не найдена для удаления");
                    Console.WriteLine($"Задача \"{title}\" не найдена!");
                    return;
                }

                tasks.Remove(taskToRemove);

                Trace.TraceInformation($"[INFO] Задача \"{title}\" успешно удалена.");
                traceSource.TraceEvent(TraceEventType.Information, 2002,
                    $"Задача '{title}' удалена. Осталось задач: {tasks.Count}");

                Console.WriteLine($"Задача \"{title}\" удалена!");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[ERROR] Ошибка при удалении задачи: {ex.Message}");
                traceSource.TraceEvent(TraceEventType.Error, 3003,
                    $"Ошибка удаления задачи: {ex.Message}");
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                Trace.WriteLine("[TRACE] Конец операции RemoveTask.");
                traceSource.TraceEvent(TraceEventType.Stop, 2003, "Операция RemoveTask завершена");
            }
        }

        public void ListTasks()
        {
            Trace.WriteLine("[TRACE] Начало операции ListTasks.");
            traceSource.TraceEvent(TraceEventType.Start, 3001, "Операция ListTasks начата");

            try
            {
                traceSource.TraceInformation($"[TRACE] Проверка количества задач: {tasks.Count}");

                if (!tasks.Any())
                {
                    Trace.TraceInformation("[INFO] Список задач пуст.");
                    traceSource.TraceEvent(TraceEventType.Information, 3002, "Список задач пуст");
                    Console.WriteLine("Список задач пуст!");
                    return;
                }

                Console.WriteLine("\n=== Список задач ===");
                for (int i = 0; i < tasks.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {tasks[i]}");
                }
                Console.WriteLine("===================\n");

                Trace.TraceInformation($"[INFO] Выведен список из {tasks.Count} задач.");
                traceSource.TraceEvent(TraceEventType.Information, 3003,
                    $"Выведено {tasks.Count} задач");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[ERROR] Ошибка при выводе списка задач: {ex.Message}");
                traceSource.TraceEvent(TraceEventType.Error, 3004,
                    $"Ошибка вывода списка задач: {ex.Message}");
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                Trace.WriteLine("[TRACE] Конец операции ListTasks.");
                traceSource.TraceEvent(TraceEventType.Stop, 3005, "Операция ListTasks завершена");
            }
        }

        public int GetTaskCount()
        {
            return tasks.Count;
        }

        public void Close()
        {
            traceSource.Close();
        }
    }
}

