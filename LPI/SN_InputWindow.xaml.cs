using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LPI
{
    /// <summary>
    /// Логика взаимодействия для SN_InputWindow.xaml
    /// </summary>
    public partial class SN_InputWindow : Window, IDataReceiver 
    {
        private readonly int _recordNM;
        private SerialPort serialPort;
        private bool isConnected = false;
        private int maxSerialNumbersAllowed; // Переменная для отслеживания максимального числа серийных номеров
        public SN_InputWindow(int recordNumber, int kolvo_sn)
        {
            InitializeComponent();
            _recordNM = recordNumber;
            LoadTableSNFromDataBase(recordNumber);
            labelRecordNumber.Content = $"Номер записи: {recordNumber}; Количество СН: {kolvo_sn}";
            // Сохраняем значение кол-ва серийных номеров
            maxSerialNumbersAllowed = kolvo_sn;
            LoadCOMPorts(); // загружаем доступные COM-порты
        }
        // Загрузка доступных COM-портов
        private void LoadCOMPorts()
        {
            comboPorts.Items.Clear(); // очищаем предыдущий список
            string[] ports = SerialPort.GetPortNames(); // получаем доступные порты
            foreach (string port in ports)
            {
                comboPorts.Items.Add(port); // добавляем в комбобокс
            }
        }
        // Обработчик события нажатия кнопки "Соединить"
        private void ConnectToSelectedPort(object sender, RoutedEventArgs e)
        {
            string selectedPort = comboPorts.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedPort))
            {
                MessageBox.Show("COM-порт не выбран.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Создаем объект SerialPort
            serialPort = new SerialPort(selectedPort, 9600, Parity.None, 8, StopBits.One);
            try
            {
                serialPort.Open();
                // Подписываемся на получение данных
                serialPort.DataReceived += SerialPort_DataReceived;
                MessageBox.Show($"Подключение к порту {selectedPort} успешно.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                isConnected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к порту: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Обработка полученных данных
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string receivedData = serialPort.ReadExisting();
            ReceiveBarcode(receivedData);
        }
        // При закрытии окна закрываем соединение с портом
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (isConnected && serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
        public void ReceiveBarcode(string barcode)
        {
            // Обновляем TextBox с данными штрихкода 
            Dispatcher.Invoke(() =>
            {
                txtBarcode.Text = barcode;
            });

            Task.Run(() =>
            {
                try
                {
                    AddSN(_recordNM, barcode);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Не выполнено:\n\n{ex.Message}\n\nПодробная информация:\n{ex.StackTrace}", "Ошибка");
                    });
                }
            });
        }
        private void AddSN(int recordNumber, string barcode)
        {   // Проверяем количество текущих серийных номеров
            var currentCount = dataGrid.Items.Count;

            if (currentCount >= maxSerialNumbersAllowed)
            {
                // Лимит превышен, показываем ошибку
                MessageBox.Show("Превышено допустимое количество серийных номеров.", "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            const string connectionString = "Initial Catalog=SLHistory;Data Source=s04-sp04;Packet Size=4096;Connection Timeout=60; Integrated Security=SSPI";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("[dbo].[mas_lpi_snum_status_ins]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@VYA", recordNumber);
                    command.Parameters.AddWithValue("@Snum", barcode);
                    command.Parameters.AddWithValue("@Result", 0);
                    command.Parameters.AddWithValue("@Status", 0);
                    command.Parameters.Add("@Result_char", SqlDbType.VarChar, 30).Direction = ParameterDirection.Output;
                
                    command.ExecuteNonQuery();

                    string result = command.Parameters["@Result_char"].Value.ToString();
                    if (result == "Good")
                    {
                        try
                        {
                            Dispatcher.BeginInvoke(new Action(() => LoadTableSNFromDataBase(recordNumber)));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Не выполнено обновление:\n\n{ex.Message}\n\nПодробная информация:\n{ex.StackTrace}", "Ошибка");

                        }
                    }
                    else
                    {
                        MessageBox.Show($"Операция не выполнена {result}");
                    }
                }
            }
        }
        private void LoadTableSNFromDataBase(int recordNumber)
        {
            const string connectionString = "Initial Catalog=SLHistory;Data Source=s04-sp04;Packet Size=4096;Connection Timeout=60; Integrated Security=SSPI";

            var dbHelper = new DatabaseHelper(connectionString, recordNumber);
            var data = dbHelper.GetAcceptanceItems();

            this.dataGrid.ItemsSource = data;
        }

        public class AcceptanceItem
        {
            public string Snum { get; set; }
            public string ItemCode { get; set; }
            public string nazvanie { get; set; }
            public string kommentarii { get; set; }

            public string Podrazdelenie { get; set; }
            public string Tip_isp { get; set; }
            public string Osnovanie { get; set; }
            public string res { get; set; }
            public string stat { get; set; }
        }

        class DatabaseHelper
        {
            private readonly string _connectionString;

            private readonly int _recordNum;
            public DatabaseHelper(string connectionString, int recordNum)
            {
                _connectionString = connectionString;
                _recordNum = recordNum;
            }

            public List<AcceptanceItem> GetAcceptanceItems()
            {
                var items = new List<AcceptanceItem>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    try
                    {
                        conn.Open();

                        using (var cmd = new SqlCommand("[dbo].[mas_lpi_snum_status_reed]", conn))
                        {
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@VYA", _recordNum);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var item = new AcceptanceItem
                                    {
                                        Snum = reader.IsDBNull(reader.GetOrdinal("snum")) ? "" : reader.GetString(reader.GetOrdinal("snum")).Trim(),
                                        ItemCode = reader.IsDBNull(reader.GetOrdinal("item")) ? "" : reader.GetString(reader.GetOrdinal("item")).Trim(),
                                        nazvanie = reader.IsDBNull(reader.GetOrdinal("nazv")) ? "" : reader.GetString(reader.GetOrdinal("nazv")).Trim(),
                                        kommentarii = reader.IsDBNull(reader.GetOrdinal("komment")) ? "" : reader.GetString(reader.GetOrdinal("komment")).Trim(),
                                        Podrazdelenie = reader.IsDBNull(reader.GetOrdinal("podr")) ? "" : reader.GetString(reader.GetOrdinal("podr")).Trim(),
                                        Tip_isp = reader.IsDBNull(reader.GetOrdinal("lpi_tip")) ? "" : reader.GetString(reader.GetOrdinal("lpi_tip")).Trim(),
                                        Osnovanie = reader.IsDBNull(reader.GetOrdinal("osnov")) ? "" : reader.GetString(reader.GetOrdinal("osnov")).Trim(),
                                        res = reader.IsDBNull(reader.GetOrdinal("result")) ? "" : reader.GetString(reader.GetOrdinal("result")).Trim(),
                                        stat = reader.IsDBNull(reader.GetOrdinal("status")) ? "" : reader.GetString(reader.GetOrdinal("status")).Trim(),
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
        private void OnDeleteIconClicked(object sender, MouseButtonEventArgs e)
        {
            var img = sender as Image;
            var cell = FindVisualParent<DataGridCell>(img);
            var row = FindVisualParent<DataGridRow>(cell);
            // Получаем объект строки, связанный с текущей записью
            var item = row?.Item as AcceptanceItem;
            if (item != null)
            {
                // Формируем сообщение с серийным номером
                string message = $"Удалить СН №{item.Snum}?";

                if (MessageBox.Show(message, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() => ProcedureDelSN(item.Snum, _recordNM)));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не выполнено обновление:\n\n{ex.Message}\n\nПодробная информация:\n{ex.StackTrace}", "Ошибка");
                    }
                }
            }
        }
        
        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while ((child != null) && !(child is T))
            {
                child = VisualTreeHelper.GetParent(child);
            }
            return child as T;
        }
        private void ProcedureDelSN(string Snum, int _recdNM)
        {
            const string connectionString = "Initial Catalog=SLHistory;Data Source=s04-sp04;Packet Size=4096;Connection Timeout=60; Integrated Security=SSPI";

            using (var conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    using (var command = new SqlCommand("[dbo].[mas_lpi_snum_status_del]", conn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Snum", Snum);
                        command.Parameters.Add("@Result_char", SqlDbType.VarChar, 30).Direction = ParameterDirection.Output;
                        // Выполнение команды
                        command.ExecuteNonQuery();

                        string result = command.Parameters["@Result_char"].Value.ToString();
                        if (result == "Good")
                        {
                            try
                            {
                                Dispatcher.BeginInvoke(new Action(() => LoadTableSNFromDataBase(_recdNM)));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Не выполнено обновление при удалении:\n\n{ex.Message}\n\nПодробная информация:\n{ex.StackTrace}", "Ошибка");

                            }

                        }
                        else
                        {
                            MessageBox.Show($"Операция не выполнена {result}"); 
                        }
                    }
                    conn.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AcceptanceWindow acceptanceItem = new AcceptanceWindow();
            acceptanceItem.Show();
            this.Close();
        }

        public void UpdateSNStatus(int recordNumber, string snum, string result)
        {
            const string connectionString = "Initial Catalog=SLHistory;Data Source=s04-sp04;Packet Size=4096;Connection Timeout=60; Integrated Security=SSPI";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("[dbo].[mas_lpi_snum_status_upd]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@VYA", recordNumber);
                    command.Parameters.AddWithValue("@Snum", snum);
                    command.Parameters.AddWithValue("@Result", result);
                    command.ExecuteNonQuery();

                }
            }
        }

        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = dataGrid.SelectedItem; // Получаем выбранную строку
            if (selectedItem != null)
            {   
                ResultSelectionWindow testingRes = new ResultSelectionWindow(selectedItem);
                bool? dialogResult = testingRes.ShowDialog(); // Показываем окно и получаем результат
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    // Обновляем таблицу, если возвращено подтверждение
                    dataGrid.Items.Refresh();
                    try
                    {

                        var item = (AcceptanceItem)selectedItem;
                       // MessageBox.Show(item.res);
                        UpdateSNStatus(_recordNM, item.Snum, item.res);
                    }
                    catch { MessageBox.Show("Не выполнено"); }

                }
            }
        }

        //private void VYfind_TextChanged(object sender, TextChangedEventArgs e)
        //{

        //}
    }

}
