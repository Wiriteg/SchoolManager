using System;

namespace SchoolManager.Model
{
    public class Performance
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public DateTime Date { get; set; }
        public int Grade { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}