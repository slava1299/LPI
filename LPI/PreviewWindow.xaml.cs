using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace LPI
{
    /// <summary>
    /// Логика взаимодействия для PreviewWindow.xaml
    /// </summary>
    /// 
    // Класс модели для строк таблицы
    public class InvoiceItem
    {
        public int RowIndex { get; set; }
        public string CodeSL { get; set; }
        public string Name { get; set; }
        public string UnitMeasure { get; set; }
        public decimal Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? TotalAmount => Quantity * Price;
    }

    public partial class PreviewWindow : Window
    {   
        public PreviewWindow(DateTime printDate, string sotrudnik, List<InvoiceItem> items)
        {
            InitializeComponent();
            string invoiceNumber = GetInvoiceNumber();
            lblNum.Content = invoiceNumber;
            string onlyFIO = ExtractOnlyFIO(sotrudnik);
            string shortForm = ConvertToShortForm(onlyFIO);
            lblSotr.Content = shortForm;
            this.DataContext = new { CurrentDateTime = $"{printDate:dd/MM/yyyy}" };

            // Прямо передаем сгруппированные данные в ItemsSource
            dgItems.ItemsSource = items;
        }
        public static string ExtractOnlyFIO(string fullInfo)
        {
            int indexOfPipe = fullInfo.IndexOf('|');

            if (indexOfPipe > 0)
            {
                return fullInfo.Substring(0, indexOfPipe).Trim(); // Убираем лишнее пробельное пространство слева и справа
            }
            else
            {
                return fullInfo; // Возвращаем исходную строку, если символ '|' не найден
            }
        }
        public static string ConvertToShortForm(string fullName)
        {
            // Разбиваем строку на массив элементов (имя состоит из трёх частей)
            string[] parts = fullName.Split(' ');

            if (parts.Length >= 3)
            {
                // Берём фамилию целиком
                string surname = parts[0];

                // Формируем инициал имени (первая буква + точка)
                char firstLetterOfFirstName = parts[1][0]; // Первая буква имени
                string initialForFirstName = firstLetterOfFirstName + ".";

                // Формируем инициал отчества (первая буква + точка)
                char firstLetterOfPatronymic = parts[2][0]; // Первая буква отчества
                string initialForPatronymic = firstLetterOfPatronymic + ".";

                // Объединяем фамилию и инициалы
                return $"{surname} {initialForFirstName}{initialForPatronymic}";
            }
            else
            {
                // Если структура отличается от ожидаемой, возвращаем оригинальную строку
                return fullName;
            }
        }
        private string GetInvoiceNumber()
        {
            string number = string.Empty;

            using (SqlConnection conn = new SqlConnection("Initial Catalog=SLHistory;Data Source=s04-sp04;Packet Size=4096;Connection Timeout=60;Integrated Security=SSPI"))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("[dbo].[kam_lpi_invioce_num]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Определяем выходной параметр
                    SqlParameter paramOutput = new SqlParameter("@num", SqlDbType.Char, 11);
                    paramOutput.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramOutput);

                    cmd.ExecuteNonQuery();

                    number = (string)paramOutput.Value;
                }
            }

            return number;
        }
        //private void PrintButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Создаем объект PrintDialog для настройки печати
        //    var pd = new PrintDialog();

        //    // Показываем диалог выбора принтера и настроек печати
        //    bool? result = pd.ShowDialog();

        //    if (result.HasValue && result.Value)
        //    {
        //        try
        //        {
        //            // Вычисляем размер страницы в пикселях
        //            double dpiX = VisualTreeHelper.GetDpi(this).PixelsPerInchX;
        //            double dpiY = VisualTreeHelper.GetDpi(this).PixelsPerInchY;

        //            // Настройка размера бумаги А4 (210 мм × 297 мм)
        //            double pageWidth = 210 / 25.4 * dpiX; // Переводим миллиметры в дюймы, потом в пиксели
        //            double pageHeight = 297 / 25.4 * dpiY;

        //            // Создаем визуальное представление страницы
        //            DrawingVisual visual = new DrawingVisual();
        //            using (DrawingContext dc = visual.RenderOpen())
        //            {
        //                // Масштабируем страницу для соответствия формату А4
        //                Transform transform = new MatrixTransform(dpiX / 96d, 0, 0, dpiY / 96d, 0, 0); // Масштабирование относительно DPI экрана

        //                // Рисуем элемент DataGrid и остальное содержимое окна внутри визуального представления
        //                Rect bounds = VisualTreeHelper.GetDescendantBounds(this);
        //                dc.PushTransform(transform);
        //                dc.DrawRectangle(new VisualBrush(this), null, bounds);
        //                dc.Pop();
        //            }

        //            // Отправляем визуализацию на печать
        //            pd.PrintVisual(visual, "Накладная");
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show($"Ошибка печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //    }
        //}
        //private void PrintButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Показываем диалог выбора принтера
        //    var pd = new PrintDialog();
        //    bool? result = pd.ShowDialog();

        //    if (result.HasValue && result.Value)
        //    {
        //        try
        //        {
        //            // Ищем элемент Grid с вашим дизайном накладной
        //            Grid contentGrid = this.FindName("contentGrid") as Grid;

        //            if (contentGrid != null)
        //            {
        //                // Получаем размеры бумаги A4 в пикселях
        //                double dpiX = VisualTreeHelper.GetDpi(contentGrid).PixelsPerInchX;
        //                double dpiY = VisualTreeHelper.GetDpi(contentGrid).PixelsPerInchY;

        //                // Формат A4 (210 mm x 297 mm)
        //                double widthInPx = 210 / 25.4 * dpiX; // ширина в пикселях
        //                double heightInPx = 297 / 25.4 * dpiY; // высота в пикселях

        //                // Создаем новую визуальную поверхность
        //                DrawingVisual visual = new DrawingVisual();

        //                using (DrawingContext ctx = visual.RenderOpen())
        //                {
        //                    // Растягиваем содержимое на весь лист
        //                    Rect rect = new Rect(0, 0, widthInPx, heightInPx);

        //                    // Заполняем лист содержанием формы накладной
        //                    ctx.DrawRectangle(new VisualBrush(contentGrid), null, rect);
        //                }

        //                // Отправляем на печать подготовленную визуализацию
        //                pd.PrintVisual(visual, "Накладная");
        //            }
        //            else
        //            {
        //                MessageBox.Show("Форма накладной не найдена!");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show($"Ошибка печати: {ex.Message}");
        //        }
        //    }
        //}

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var pd = new PrintDialog();
            bool? result = pd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    // Контейнер дизайна накладной
                    Grid contentGrid = this.FindName("contentGrid") as Grid;

                    if (contentGrid != null)
                    {
                        // Коэффициент масштабирования (уменьшение на 20%)
                        const double scaleFactor = 0.8;

                        // Получаем исходные размеры сетки
                        double originalWidth = contentGrid.ActualWidth;
                        double originalHeight = contentGrid.ActualHeight;

                        // Новый размер после масштабирования
                        double scaledWidth = originalWidth * scaleFactor;
                        double scaledHeight = originalHeight * scaleFactor;

                        // Центрируем начало рисования
                        double offsetX = (originalWidth - scaledWidth) / 2;
                        double offsetY = (originalHeight - scaledHeight) / 2;

                        TransformGroup group = new TransformGroup();
                        group.Children.Add(new TranslateTransform(offsetX, offsetY));      // центровка позиции
                        group.Children.Add(new ScaleTransform(scaleFactor, scaleFactor));  // само масштабирование

                        // Визуализация для печати
                        DrawingVisual visual = new DrawingVisual();

                        using (DrawingContext ctx = visual.RenderOpen())
                        {   
                            // Получаем DPI и рассчитываем размеры A4
                            double dpiX = VisualTreeHelper.GetDpi(contentGrid).PixelsPerInchX;
                            double dpiY = VisualTreeHelper.GetDpi(contentGrid).PixelsPerInchY;

                            double widthInPx = 210 / 25.4 * dpiX;
                            double heightInPx = 297 / 25.4 * dpiY;

                            // Площадь рисования равна размеру листа A4
                            Rect rect = new Rect(0, 0, widthInPx, heightInPx);

                            ctx.PushTransform(group);

                            ctx.DrawRectangle(new VisualBrush(contentGrid), null, rect);

                            ctx.Pop();
                        }

                        // Печать
                        pd.PrintVisual(visual, "Накладная");
                    }
                    else
                    {
                        MessageBox.Show("Форма накладной не найдена!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка печати: {ex.Message}");
                }
            }
        }
    }
}
