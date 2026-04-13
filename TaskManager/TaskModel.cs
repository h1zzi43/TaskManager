using System;

namespace TaskManager
{
    public class TaskModel
    {
        private static int _nextId = 1;

        public int Id { get; private set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }

        public TaskModel(string title, string description = "")
        {
            Id = _nextId++;
            Title = title;
            Description = description;
            CreatedDate = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Id}] {Title} (Создано: {CreatedDate:yyyy-MM-dd HH:mm})";
        }
    }
}