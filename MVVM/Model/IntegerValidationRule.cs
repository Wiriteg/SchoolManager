using System.Globalization;
using System.Windows.Controls;

namespace SchoolManager.Model
{
    public class IntegerValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, "Поле не может быть пустым.");
            }

            if (!int.TryParse(input, out int result))
            {
                return new ValidationResult(false, "Введите целое число.");
            }

            if (result < 0)
            {
                return new ValidationResult(false, "Стаж не может быть отрицательным.");
            }

            return ValidationResult.ValidResult;
        }
    }
}