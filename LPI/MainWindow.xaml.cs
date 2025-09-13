using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace LPI
{
    public partial class MainWindow : Window
    {
        private const string ConnectionString = "Initial Catalog=SLHistory;Data Source=s04-sp04;Packet Size=4096;Connection Timeout=60; Integrated Security=SSPI";
        // Глобальная коллекция выбранных элементов
        public static Dictionary<string, DataRowView> GlobalSelectedItems = new Dictionary<string, DataRowView>();

        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing; 
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
           
            var result = MessageBox.Show("Вы уверены, что хотите закрыть программу?",
                                         "Подтверждение",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // Отменяем закрытие окна
            }
        }
        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            var enteredText = textBox1.Text.Trim();

            if (!string.IsNullOrEmpty(enteredText))
            {
                var matchingSuggestions = GetMatchingFIOsAndPositionsFromDatabase(enteredText);
                UpdateSuggestionListBox(matchingSuggestions);
            }
            else
            {
                suggestionListBox.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSuggestionListBox(List<Suggestion> suggestions)
        {
            suggestionListBox.ItemsSource = suggestions;

            if (suggestions.Any())
            {
                suggestionListBox.Visibility = Visibility.Visible;
            }
            else
            {
                suggestionListBox.Visibility = Visibility.Collapsed;
            }
        }

        private void suggestionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suggestionListBox.SelectedItem != null)
            {
                textBox1.Text = ((Suggestion)suggestionListBox.SelectedItem).FullInfo;
                suggestionListBox.Visibility = Visibility.Collapsed;
            }
        }

        private List<Suggestion> GetMatchingFIOsAndPositionsFromDatabase(string enteredText)
        {
            var results = new List<Suggestion>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("[dbo].[kam_fio_lpi_finding]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@fio", enteredText);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fio = reader.GetString(0);
                            string code = reader.GetString(1);
                            string position = reader.GetString(2);

                            results.Add(new Suggestion { FullInfo = $"{fio} | {code} | {position}" });
                        }
                    }
                }
            }

            return results;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = this.FindName("comboBox") as ComboBox;

            if (comboBox != null && comboBox.SelectedIndex >= 0)
            {   // Получаем текст выбранного имени сотрудника
                string employeeName = textBox1.Text.Trim();

                // Проверяем, введено ли ФИО сотрудника
                if (string.IsNullOrEmpty(employeeName))
                {
                    MessageBox.Show("Необходимо выбрать сотрудника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Выход из метода, если ФИО не выбрано
                }
                string selectedOperation = (comboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                switch (selectedOperation)
                {
                    case "Приемка ЛПИ":
                        AcceptanceWindow acceptanceWindow = new AcceptanceWindow();
                        acceptanceWindow.Show();
                        break;
                    //case "Ввод результатов испытаний":
                    //    TestResultsWindow testResultsWindow = new TestResultsWindow();
                    //    testResultsWindow.Show();
                    //    break;
                    case "Сдача на склад":
                        WarehouseDeliveryWindow warehouseDeliveryWindow = new WarehouseDeliveryWindow(employeeName);
                        warehouseDeliveryWindow.Show();
                        break;
                }
            }
        }
        class Suggestion
        {
            public string FullInfo { get; set; }
        }

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1 != null) 
            {textBox1.Clear();
            }
        }
    }
}
