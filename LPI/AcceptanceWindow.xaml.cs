using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static LPI.AcceptanceWindow;
using System.ComponentModel;

namespace LPI
{
    /// <summary>
    /// Логика взаимодействия для AcceptanceWindow.xaml
    /// </summary>
    public partial class AcceptanceWindow : Window
    {
        public AcceptanceWindow()
        {
            InitializeComponent();
            LoadDataFromDatabase();
        }

        private void LoadDataFromDatabase()
        {
            
            const string connectionString = "Initial Catalog=SLHistory;Data Source=s04-sp04;Packet Size=4096;Connection Timeout=60; Integrated Security=SSPI";

            var dbHelper = new DatabaseHelper(connectionString);
            var data = dbHelper.GetAcceptanceItems();

            // источник данных для DataGrid
            this.dataGrid.ItemsSource = data;
        }
    
        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dataGrid.SelectedItem != null)
            {
                
                var selectedItem = dataGrid.SelectedItem as AcceptanceItem;

                // Извлекаем ID (Номер записи)
                int recordNumber = selectedItem.Id;
                int kolvoSN = selectedItem.Quantity;

                // Открытие нового окна и передача номера записи
                SN_InputWindow sn_inputwin = new SN_InputWindow(recordNumber, kolvoSN);
                sn_inputwin.Show();
                this.Close();
            }
        }

        private void SearchText_Changed(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(searchBox.Text);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ApplyFilter(string filterText)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
            if (view != null)
            {
                view.Filter = o =>
                {
                    if (string.IsNullOrEmpty(filterText))
                        return true;

                    var item = o as AcceptanceItem;
                    if (item == null)
                        return false;

                    return item.Id.ToString().Contains(filterText) ||
                           item.CreateDate.ToString().Contains(filterText) ||
                           item.Status.Contains(filterText) ||
                           item.ItemCode.Contains(filterText) ||
                           item.ItemName.Contains(filterText) ||
                           item.Quantity.ToString().Contains(filterText) ||
                           item.Department.Contains(filterText) ||
                           item.ResponsiblePerson.Contains(filterText) ||
                           item.Reason.Contains(filterText) ||
                           item.Comment.Contains(filterText);
                };
            }
        }


        public class AcceptanceItem
        {
            public int Id { get; set; }
            public DateTime CreateDate { get; set; }
            public string Status { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public int Quantity { get; set; }
            public string Department { get; set; }
            public string ResponsiblePerson { get; set; }
            public string Reason { get; set; }
            public string Comment { get; set; }
        }
       

    }


    class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<AcceptanceItem> GetAcceptanceItems()
        {
            var items = new List<AcceptanceItem>();

            using (var conn = new SqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();

                    using (var cmd = new SqlCommand("[dbo].[kam_lpi_registry]", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new AcceptanceItem
                                {
                                    Id = reader.IsDBNull(reader.GetOrdinal("id")) ? default(int) : reader.GetInt32(reader.GetOrdinal("id")), // проверка на null
                                    CreateDate = reader.IsDBNull(reader.GetOrdinal("create_date")) ? default(DateTime) : reader.GetDateTime(reader.GetOrdinal("create_date")),
                                    Status = reader.IsDBNull(reader.GetOrdinal("stat")) ? "" : reader.GetString(reader.GetOrdinal("stat")).Trim(),
                                    ItemCode = reader.IsDBNull(reader.GetOrdinal("item")) ? "" : reader.GetString(reader.GetOrdinal("item")).Trim(),
                                    ItemName = reader.IsDBNull(reader.GetOrdinal("item_desc")) ? "" : reader.GetString(reader.GetOrdinal("item_desc")).Trim(),
                                    Quantity = reader.IsDBNull(reader.GetOrdinal("kolvo")) ? default(int) : reader.GetInt32(reader.GetOrdinal("kolvo")),
                                    Department = reader.IsDBNull(reader.GetOrdinal("podr")) ? "" : reader.GetString(reader.GetOrdinal("podr")).Trim(),
                                    ResponsiblePerson = reader.IsDBNull(reader.GetOrdinal("fio")) ? "" : reader.GetString(reader.GetOrdinal("fio")).Trim(),
                                    Reason = reader.IsDBNull(reader.GetOrdinal("reason")) ? "" : reader.GetString(reader.GetOrdinal("reason")).Trim(),
                                    Comment = reader.IsDBNull(reader.GetOrdinal("comment")) ? "" : reader.GetString(reader.GetOrdinal("comment")).Trim()
                                };

                                items.Add(item);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при получении данных:\n\n{ex.Message}\n\nПодробная информация:\n{ex.StackTrace}", "Ошибка");
                }
            }

            return items;
        }


    }
}