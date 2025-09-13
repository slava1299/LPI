using System;
using System.Windows;


namespace LPI
{
    public partial class ResultSelectionWindow : Window
    {
        private readonly object _selectedItem; // Хранится выбранная строка
        public string _result; // Переменная для хранения итогового результата

        public ResultSelectionWindow(object item)
        {
            InitializeComponent();
            _selectedItem = item;
           
        }

        // Обработчик положительного результата
        private void PositiveButton_Click(object sender, RoutedEventArgs e)
        {
            SetResult("Положительный"); // Установка результата

        }

        // Обработчик отрицательного результата
        private void NegativeButton_Click(object sender, RoutedEventArgs e)
        {
            SetResult("Отрицательный"); // Установка результата
        }

        // Обработчик остановки испытания
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            SetResult("Остановка"); // Установка результата
        }

        // Установщик результата
        private void SetResult(string result)
        {
            _result = result; 
            DialogResult = true; // Возвращаем положительный результат закрытия окна
            Close(); 
        }

        // Переопределенный метод закрытия окна
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_selectedItem != null && !string.IsNullOrEmpty(_result))
            {
                ((dynamic)_selectedItem).res = _result; // Применяем результат к полю результирующей строки
            }
        }
    }
}
