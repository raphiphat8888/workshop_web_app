namespace TodoApi.Model;

    public class TodoItem
    {
        public  int Id { get; set; }
        public  string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public  bool IsComplete { get; set; }

        public DateTime CreatedAt { get; set; }
    }

