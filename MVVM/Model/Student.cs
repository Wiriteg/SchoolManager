using SchoolManager.MVVM.ViewModel;
using System;
using System.Collections.Generic;

namespace SchoolManager.Model
{
    public class Student : IComparable<Student>
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string ClassName { get; set; }
        public Dictionary<int, Dictionary<string, Attendance>> Attendances { get; set; }
        public Dictionary<int, Dictionary<string, string>> Performances { get; set; }

        public string FullName => $"{LastName}\n{FirstName}\n{MiddleName}".Trim();

        public void SetAttendances(Dictionary<int, Dictionary<string, Attendance>> attendances, PerformanceAttendanceViewModel viewModel)
        {
            Attendances = attendances;
            foreach (var subjectAttendances in Attendances.Values)
            {
                foreach (var attendance in subjectAttendances.Values)
                {
                    attendance.ViewModel = viewModel;
                    attendance.Student = this;
                }
            }
        }

        public void SetPerformances(Dictionary<int, Dictionary<string, string>> performances, PerformanceAttendanceViewModel viewModel)
        {
            Performances = performances;
        }

        public string this[int subjectId, string date]
        {
            set
            {
                if (!Performances.ContainsKey(subjectId))
                    Performances[subjectId] = new Dictionary<string, string>();
                Performances[subjectId][date] = value;
            }
        }

        public int CompareTo(Student other)
        {
            if (other == null) return 1;

            int lastNameComparison = string.Compare(LastName, other.LastName, StringComparison.OrdinalIgnoreCase);
            if (lastNameComparison != 0) return lastNameComparison;

            int firstNameComparison = string.Compare(FirstName, other.FirstName, StringComparison.OrdinalIgnoreCase);
            if (firstNameComparison != 0) return firstNameComparison;

            return string.Compare(MiddleName, other.MiddleName, StringComparison.OrdinalIgnoreCase);
        }
    }
}