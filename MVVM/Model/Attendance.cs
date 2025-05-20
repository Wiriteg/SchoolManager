using System;
using SchoolManager.MVVM.ViewModel;

namespace SchoolManager.Model
{
    public class Attendance
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public DateTime Date { get; set; }
        public string Presence { get; set; }
        public string Reason { get; set; }
        public Student Student { get; set; }
        public PerformanceAttendanceViewModel ViewModel { get; set; }
    }
}