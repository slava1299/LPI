using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.SqlClient;
using System.Windows.Input;
using System.Windows.Media;
using System.Data;


namespace LPI
{
    /// <summary>
    /// Логика взаимодействия для WarehouseDeliveryWindow.xaml
    /// </summary> 
    public partial class WarehouseDeliveryWindow : Window
    {
        
        private int? selectedCount = 0; // Переменная для отслеживания выбранного количества элементов
        public string sotrudnik;
        public WarehouseDeliveryWindow(string sotr)
        {
            InitializeComponent();
            UpdateSelectedCount(); // Первоначальное обновление
            sotrudnik = sotr;
            
        }
        // Метод для обновления текстового поля с количеством выбранных элементов
        private void UpdateSelectedCount()
        {
            NUM.Text = selectedCount.ToString();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void VYfind_TextChanged(object sender, TextChangedEventArgs e)
        {
            ResetSelections(); // Сброс текущих выделений

            if (findVYA.Text.Length < 4 || !int.TryParse(findVYA.Text, out int inputValue))
            {
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection("Initial Catalog=SLHistory;Data Source=s04-sp04;Packet Size=4096;" +
                    "Connection Timeout=60; Integrated Security=SSPI"))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("[dbo].[kam_lpi_list_for_storage]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@VYA", inputValue);

                        var dataAdapter = new SqlDataAdapter(command);
                        var dtResult = new DataTable();
                        dataAdapter.Fill(dtResult);

                        dataGrid.ItemsSource = dtResult.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения процедуры: {ex.Message}");
            }
        }

        // Метод сброса выбора и счётчика

        private void ResetSelections()
        {
            foreach (var row in dataGrid.Items)
            {
                var cellContent = dataGrid.Columns.Last().GetCellContent(row);
                if (cellContent is FrameworkElement element && element.FindName("check") is Image image)
                {
                    image.Opacity = 0.2;
                }
            }

            // Сбрасываем счётчик
            selectedCount = 0;
            UpdateSelectedCount();
        }

        // Обработчик события нажатия мыши на изображение

        //private void checkVYA(object sender, MouseButtonEventArgs e)
        //{
        //    var image = sender as Image;
        //    if (image != null)
        //    {
        //        bool isChecked = (double)image.Opacity  > 0.5; // Текущее состояние (если opacity меньше 0.5 значит не выбрано)

        //        // Переключение непрозрачности
        //        if (!isChecked)
        //        {
        //            image.Opacity = 1.0; // Становится выбранным
        //            selectedCount++;
        //        }
        //        else
        //        {
        //            image.Opacity = 0.2; // Становится невыбранным
        //            selectedCount--;
        //        }

        //        // Обновление текстового поля
        //        UpdateSelectedCount();
        //    }
        //}
        private void checkVYA(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            if (image != null)
            {
                var dgRow = FindAncestor<DataGridRow>(image);
                if (dgRow != null)
                {
                    var drv = dgRow.Item as DataRowView;
                    if (drv != null)
                    {
                        string result = drv["Result"]?.ToString() ?? "";

                        if (string.IsNullOrEmpty(result.Trim()))
                        {
                            MessageBox.Show("Нет результата испытаний.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        // Проверяем, только если нжатие произошло по неотмеченному элементу
                        if ((double)image.Opacity == 0.2)
                        {
                            // Проверка на отрицательный результат или остановку
                            bool hasNegativeOrStoppedResult = result.Equals("Отрицательный", StringComparison.OrdinalIgnoreCase) ||
                                                              result.Equals("Остановка", StringComparison.OrdinalIgnoreCase);

                            if (hasNegativeOrStoppedResult)
                            {
                                // Диалоговое окно с подтверждением
                                MessageBoxResult confirmResult = MessageBox.Show(
                                    "Данный серийный номер имеет Отрицательный результат или остановлен.\nВы уверены, что хотите передать изделие на СПП?",
                                    "Предупреждение",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question);

                                if (confirmResult == MessageBoxResult.No)
                                {
                                    return; // Завершаем выполнение, если пользователь отказался
                                }
                            }
                        }


                        string uniqueId = drv["Snum"].ToString();
                        bool isChecked = (double)image.Opacity > 0.5;

                        if (!isChecked)
                        {
                            image.Opacity = 1.0;
                            MainWindow.GlobalSelectedItems[uniqueId] = drv; // Добавляем в глобальную коллекцию
                            selectedCount++;
                        }
                        else
                        {
                            image.Opacity = 0.2;
                            MainWindow.GlobalSelectedItems.Remove(uniqueId); // Удаляем из глобальной коллекции
                            selectedCount--;
                        }

                        UpdateSelectedCount();
                    }
                }
            }
        }
        private IEnumerable<InvoiceItem> GroupAndSumSelectedItems(List<InvoiceItem> originalItems)
        {
            // Группа уникальных записей по сочетанию Код SL и названия
            var groupedItems = originalItems.GroupBy(x => new { x.CodeSL, x.Name })
                                           .Select(group => new InvoiceItem
                                           {
                                               CodeSL = group.Key.CodeSL,
                                               Name = group.Key.Name,
                                               UnitMeasure = group.First().UnitMeasure,
                                               Quantity = group.Sum(g => g.Quantity),
                                               Price = group.First().Price
                                           });

            // Добавляем правильную нумерацию
            return groupedItems.Select((item, idx) => new InvoiceItem
            {
                RowIndex = idx + 1, // Индексация с единицы
                CodeSL = item.CodeSL,
                Name = item.Name,
                UnitMeasure = item.UnitMeasure,
                Quantity = item.Quantity,
                Price = item.Price
            });
        }

        // Вспомогательная функция для поиска предка определенного типа
        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
        private void DocButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.GlobalSelectedItems.Count <= 0)
            {
                MessageBox.Show("Нет выбранных серийных номеров.");
                return;
            }

            // Получаем изначальный список данных
            var originalItems = MainWindow.GlobalSelectedItems.Values.Select(drv => new InvoiceItem
            {
                CodeSL = drv["item"].ToString(),
                Name = drv["item_desc"].ToString(),
                UnitMeasure = "",
                Quantity = 1m, // Изначально считаем один экземпляр
                Price = null
            }).ToList();

            // Группируем и суммируем данные
            var preparedItems = GroupAndSumSelectedItems(originalItems).ToList();

            DateTime now = DateTime.Now;

            // Создаем окно предварительного просмотра
            PreviewWindow previewWin = new PreviewWindow(now, sotrudnik, preparedItems);
            previewWin.Owner = this;
            previewWin.WindowStartupLocation = WindowStartupLocation.Manual;
            previewWin.Left = 0;
            previewWin.Top = 0;
            //previewWin.WindowState = WindowState.Maximized;

            bool? showResult = previewWin.ShowDialog();

           
        }
        

        
    }
    public static class VisualTreeExtensions
    {
        public static T FindVisualChild<T>(this DependencyObject obj, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T namedChild && namedChild.GetValue(FrameworkElement.NameProperty).Equals(childName))
                {
                    return (T)child;
                }

                var foundChild = FindVisualChild<T>(child, childName);
                if (foundChild != null)
                {
                    return foundChild;
                }
            }
            return null;
        }
    }








}
