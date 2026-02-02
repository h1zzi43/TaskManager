using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
    public class TaskModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }

        public TaskModel(string title, string description = "")
        {
            Title = title;
            Description = description;
            CreatedDate = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Title} (Создано: {CreatedDate:yyyy-MM-dd HH:mm})";
        }
    }
}

