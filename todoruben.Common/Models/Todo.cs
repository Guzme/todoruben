using System;

namespace todoruben.Common.Models
{
    public class Todo
    {
        public DateTime CreatedTme { get; set; }

        public string TaskDescription { get; set; }

        public bool IsCompleted { get; set; }

    }
}
