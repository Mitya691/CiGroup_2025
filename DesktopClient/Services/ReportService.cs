using ClosedXML.Excel;
using DesktopClient.Helpers;
using DesktopClient.Model;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using DesktopClient.Config;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace DesktopClient.Services
{
    class SiloInfo
    {
        public decimal? WeightM1 { get; set; }
        public decimal? WeightM2 { get; set; }
    }
    /// <summary>
    /// Реализация сервиса создания отчета в форматет xlsx 
    /// SDK NanoXLSX
    /// </summary>
    class ReportService : IReportService
    {
        private readonly ILogger<ReportService> _logger;
        private readonly CurrentUserStore _currentUser;
        private readonly ISQLRepository _repository;
        private readonly string _person;
        private readonly IMailSettingsStore _mailSettings;

        // внедрение зависимости через конструктор
        public ReportService(ISQLRepository repository, CurrentUserStore currentUser, IMailSettingsStore mailSettings, ILogger<ReportService> logger)
        {
            _repository = repository;
            _currentUser = currentUser;
            _mailSettings = mailSettings;
            _logger = logger;
        }

        public async Task<string> NewReport(DateTime? Start, DateTime? Stop, string shiftOperator)
        {
            List<Card> cards = await _repository.GetCardsForInterval(Start, Stop);

            if (cards.Count == 0)
            {
                _logger.LogWarning("Нет карточек за период {Start} - {Stop}", Start, Stop);
                return null; // просто сигнал наверх
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Отчёт");

            // Настройки страницы
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;

            // ------------------ МЕРЖИ ШАПКИ ------------------

            var orgLabelRange = ws.Range("A1:B1").Merge(); // "Организация:"
            var deptLabelRange = ws.Range("A2:B2").Merge(); // "Подразделение:"

            var beginTime = ws.Range("A5:B5").Merge(); //Время начала смены
            var endTime = ws.Range("A6:B6").Merge(); //Время окончания смены

            var orgValueRange = ws.Range("D1:E1").Merge(); // ООО "МакПром"
            var deptValueRange = ws.Range("D2:E2").Merge(); // МиПЭ

            var titleRange = ws.Range("B3:I3").Merge(); // Название отчёта

            var operatorRange = ws.Range("F4:G4").Merge(); // ФИО оператора

            // Шапка таблицы
            var colOpNumRange = ws.Range("A8:A9").Merge(); // № операции
            var colMoveRange = ws.Range("B8:D8").Merge(); // Перемещение зерна
            var colTimeRange = ws.Range("E8:F8").Merge(); // Время перемещения
            var colScaleRange = ws.Range("G8:H8").Merge(); // Показания весов
            var colMovedRange = ws.Range("I8:I9").Merge(); // Перемещено за операцию

            // ------------------ ТЕКСТ ШАПКИ ------------------

            ws.Cell("A1").Value = "Организация:";
            ws.Cell("A2").Value = "Подразделение:";

            ws.Cell("D1").Value = "ООО \"МакПром\"";
            orgValueRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            ws.Cell("D2").Value = "МиПЭ";
            deptValueRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            ws.Cell("B3").Value = "Отчет перемещения зерна с элеваторных сооружений на мельницы";
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 15;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A4").Value = "Дата:";
            ws.Cell("A5").Value = "Время начала смены:";
            ws.Cell("A6").Value = "Время окончания смены:";
            ws.Cell("E4").Value = "Смена:";

            var dateCell = ws.Cell("B4");
            var shiftStartCell = ws.Cell("E5");
            var shiftEndCell = ws.Cell("E6");
            var operatorCell = ws.Cell("F4");

            dateCell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            shiftStartCell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            shiftEndCell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            operatorCell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // Значения шапки
            dateCell.Value = DateTime.Now.Date;
            dateCell.Style.DateFormat.Format = "dd.MM.yyyy";

            shiftStartCell.Value = Start;
            shiftEndCell.Value = Stop;
            shiftStartCell.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            shiftEndCell.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

            operatorCell.Value = shiftOperator;

            // ------------------ ШАПКА ТАБЛИЦЫ (8–9 строки) ------------------

            ws.Cell("A8").Value = "№ операции";
            colOpNumRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            colOpNumRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            colOpNumRange.Style.Alignment.WrapText = true;

            ws.Cell("B8").Value = "Перемещение зерна";
            colMoveRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            colMoveRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            colTimeRange.Value = "Время перемещения";
            colTimeRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            colTimeRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            colScaleRange.Value = "Показания весов";
            colScaleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            colScaleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            colMovedRange.Value = "Перемещено за операцию";
            colMovedRange.Style.Alignment.WrapText = true;
            colMovedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            colMovedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell("B9").Value = "Из силоса №";
            ws.Cell("B9").Style.Alignment.WrapText = true;

            ws.Cell("C9").Value = "В мельницу";
            ws.Cell("C9").Style.Alignment.WrapText = true;

            ws.Cell("D9").Value = "Силос мельницы";
            ws.Cell("D9").Style.Alignment.WrapText = true;

            ws.Cell("E9").Value = "Начало";
            ws.Cell("F9").Value = "Окончание";
            ws.Cell("G9").Value = "Весы 1";
            ws.Cell("H9").Value = "Весы 2";

            // ------------------ ДАННЫЕ ------------------

            int row = 10;
            int operationNumber = 1;

            decimal? weightM1 = 0;
            decimal? weightM2 = 0;
            decimal? totalWeight = 0;

            foreach (Card card in cards)
            {
                ws.Cell(row, 1).Value = operationNumber;
                ws.Cell(row, 2).Value = card.SourceSilo;
                ws.Cell(row, 3).Value = card.Direction;
                ws.Cell(row, 4).Value = card.TargetSilo;

                ws.Cell(row, 5).Value = card.StartTime;
                ws.Cell(row, 6).Value = card.EndTime;

                ws.Cell(row, 7).Value = card.Weight1;
                ws.Cell(row, 8).Value = card.Weight2;
                ws.Cell(row, 9).Value = card.TotalWeight;

                ws.Row(row).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                if (card.TotalWeight.HasValue)
                {
                    totalWeight += card.TotalWeight;

                    if (card.Direction == "М1")
                        weightM1 += card.TotalWeight;
                    else if (card.Direction == "М2")
                        weightM2 += card.TotalWeight;
                }

                row++;
                operationNumber++;
            }

            int lastDataRow = row - 1;

            // ------------------ ИТОГИ ------------------

            // Итого суммарно
            var totalRow = row;
            var totalLabel = ws.Range(totalRow, 2, totalRow, 6).Merge();
            var totalValue = ws.Range(totalRow, 7, totalRow, 8).Merge();

            totalLabel.Value = "Итого суммарно на конец смены:";
            totalValue.Value = totalWeight ?? 0;
            totalValue.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            ws.Cell(totalRow, 9).Value = "кг";

            // В том числе М1
            totalRow++;
            var m1Label = ws.Range(totalRow, 2, totalRow, 6).Merge();
            var m1Value = ws.Range(totalRow, 7, totalRow, 8).Merge();

            m1Label.Value = "В том числе на мельницу 1:";
            m1Value.Value = weightM1 ?? 0;
            m1Value.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            ws.Cell(totalRow, 9).Value = "кг";

            // В том числе М2
            totalRow++;
            var m2Label = ws.Range(totalRow, 2, totalRow, 6).Merge();
            var m2Value = ws.Range(totalRow, 7, totalRow, 8).Merge();

            m2Label.Value = "В том числе на мельницу 2:";
            m2Value.Value = weightM2 ?? 0;
            m2Value.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            ws.Cell(totalRow, 9).Value = "кг";

            // Строка подписи
            totalRow += 2;
            ws.Cell(totalRow, 6).Value = "Оператор ПУЭ";

            var signRange = ws.Range(totalRow, 7, totalRow, 8).Merge();
            signRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            ws.Cell(totalRow, 9).Value = shiftOperator;

            int lastUsedRow = totalRow;

            // ------------------ ОБЩЕЕ ФОРМАТИРОВАНИЕ ТАБЛИЦЫ ------------------

            // Форматы дат/времени и чисел
            ws.Column("E").Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            ws.Column("F").Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

            ws.Column("G").Style.NumberFormat.Format = "0";
            ws.Column("H").Style.NumberFormat.Format = "0";
            ws.Column("I").Style.NumberFormat.Format = "0";

            // Границы таблицы (только сама таблица: с заголовка до последней строки с данными)
            var tableRange = ws.Range(8, 1, lastDataRow, 9);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Автоподбор ширины только по области таблицы, чтобы заголовок в B3 не растягивал колонку
            int firstAutoRow = 8;
            // Автоподбор только для B:I
            ws.Columns("B:I").AdjustToContents(firstAutoRow, lastUsedRow);

            // Немного запаса для остальных
            foreach (var c in ws.Columns("B:I"))
                c.Width += 1.5;

            // Колонку A задаём руками
            ws.Column("A").Width = 9;
            ws.Column("A").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Высота строк – по содержимому таблицы
            ws.Rows(firstAutoRow, lastUsedRow).AdjustToContents();

            // ------------------ СОХРАНЕНИЕ ------------------

            var baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ShiftReports");

            Directory.CreateDirectory(baseFolder);

            string filePath = Path.Combine(
                baseFolder,
                $"Report_{DateTime.Now:yyyy_MM_dd_HHmm}.xlsx");

            workbook.SaveAs(filePath);

            return filePath;
        }


        public async Task<string> NewDailyReport(DateTime? Start, DateTime? Stop, string firstOperator, string secondOperator)
        {
            List<Card> cards = await _repository.GetCardsForInterval(Start, Stop);

            if (cards.Count == 0)
            {
                _logger?.LogWarning("Нет карточек за период {Start} - {Stop}", Start, Stop);
                return null;
            }

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Отчёт");

            ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;

            // ширины под макет
            ws.Column("A").Width = 9.0;  
            ws.Column("B").Width = 13.0;  
            ws.Column("C").Width = 13.0; 
            ws.Column("D").Width = 13.0;  
            ws.Column("E").Width = 12.0;  
            ws.Column("F").Width = 13.0;  
            ws.Column("G").Width = 9.0;
            ws.Column("H").Width = 9.0;

            // --------- ШАПКА ---------
            ws.Range("A1:B1").Merge().Value = "Организация:";
            ws.Range("C1:D1").Merge().Value = "ООО \"МакПром\"";

            ws.Range("A2:B2").Merge().Value = "Подразделение:";
            ws.Range("C2:D2").Merge().Value = "МиПЭ";

            var title = ws.Range("A3:G3").Merge();
            title.Value = "Отчет по хранению и перемещению зерна на элеваторных сооружениях";
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            title.Style.Font.Bold = true;

            var generateDate = ws.Range("A4:B4").Merge(); 
            generateDate.Value = "Дата составления:";
            generateDate.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("C4").Value = DateTime.Now.Date;
            ws.Cell("C4").Style.DateFormat.Format = "dd.MM.yyyy";
            ws.Cell("C4").Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            ws.Range("D4").Merge().Value = "Смены:";
            var fio1 = ws.Range("E4:F4").Merge();
            fio1.Value = firstOperator;
            var fio2 = ws.Range("E5:F5").Merge(); 
            fio2.Value = secondOperator;
            fio1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            fio2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range("A6:D6").Merge().Value = "Время, дата начала формирования отчета:";
            ws.Cell("E6").Value = Start;
            ws.Cell("E6").Style.DateFormat.Format = "HH:mm";
            ws.Cell("F6").Value = Start;
            ws.Cell("F6").Style.DateFormat.Format = "dd.MM.yyyy";

            ws.Range("A7:D7").Merge().Value = "Время, дата окончания формирования отчета:";
            ws.Cell("E7").Value = Stop;
            ws.Cell("E7").Style.DateFormat.Format = "HH:mm";
            ws.Cell("F7").Value = Stop;
            ws.Cell("F7").Style.DateFormat.Format = "dd.MM.yyyy";

            var range = ws.Range("E4:F7");
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Medium;

            // --------- ШАПКА ТАБЛИЦЫ---------
            var siloField = ws.Range("A9:A10").Merge();
            siloField.Value = "Силос, №";

            var flow = ws.Range("B9:D9").Merge();
            flow.Value = "Расход, кг";
            flow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("B10").Value = "Мельница 1";
            ws.Cell("C10").Value = "Мельница 2";
            ws.Cell("D10").Value = "Итого";

            // --------- АГРЕГАЦИЯ ---------
            var dict = new Dictionary<string, SiloInfo>();
            foreach (var card in cards)
            {
                string silo = card.SourceSilo;

                if (!dict.TryGetValue(silo, out var data))
                {
                    data = new SiloInfo();
                    dict.Add(silo, data);   // вставка происходит только один раз
                }

                var w = card.TotalWeight ?? 0m; // если null, берём 0

                if (card.Direction == "М1")
                {
                    data.WeightM1 = (data.WeightM1 ?? 0m) + w;
                }
                else if (card.Direction == "М2")
                {
                    data.WeightM2 = (data.WeightM2 ?? 0m) + w;
                }
            }

            // --------- ВСТАВКА В ТАБЛИЦУ ---------
            int num = 11;
            foreach (var d in dict)
            {
                ws.Cell(num, 1).Value = d.Key;
                ws.Cell(num, 2).Value = d.Value.WeightM1;
                ws.Cell(num, 3).Value = d.Value.WeightM2;
                ws.Cell(num, 4).Value = d.Value.WeightM1 + d.Value.WeightM2;
                num++;
            }

            // ---------------- ФОРМА ПОСЛЕ ТАБЛИЦЫ ----------------

            ws.Cell(num, 1).Value = "Итого:";
            ws.Cell(num, 2).FormulaA1 = $"SUM(B11:B{num - 1})";
            ws.Cell(num, 3).FormulaA1 = $"SUM(C11:C{num - 1})";
            ws.Cell(num, 4).FormulaA1 = $"SUM(D11:D{num - 1})";
            num++;

            var table = ws.Range(9, 1, num-1, 4);
            table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Range(num, 1, num, 2).Merge().Value = "Отчет подготовил";
            num++;
            ws.Cell(num, 2).Value = "Оператор ПУЭ";
            var sig1 = ws.Range(num, 4, num, 5).Merge();
            sig1.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(num, 6, num, 7).Merge().Value = firstOperator;
            num++;
            ws.Cell(num, 2).Value = "Оператор ПУЭ";
            var sig2 = ws.Range(num, 4, num, 5).Merge();
            sig2.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(num, 6, num, 7).Merge().Value = secondOperator;

            num += 2;

            ws.Range(num, 1, num, 2).Merge().Value = "Согласовано:";
            var sig3 = ws.Range(num, 4, num, 5).Merge();
            sig3.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(num, 6, num, 7).Merge().Value = "Синица Е.Ю.";

            num++;
            ws.Range(num, 1, num, 3).Merge().Value = "Зам. директора по производству МиПЭ";
            // ---------------- СОХРАНЕНИЕ ----------------
            var baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "DailyReports");

            Directory.CreateDirectory(baseFolder);

            string filePath = Path.Combine(
                baseFolder,
                $"DailyReport_{DateTime.Now:yyyy_MM_dd_HHmm}.xlsx");

            wb.SaveAs(filePath);

            return filePath;
        }

        public async Task SendReportAsync(string reportPath, DateTime? date, DateTime? date1, CancellationToken ct = default)
        {
            if (!File.Exists(reportPath))
                throw new FileNotFoundException("Report not found", reportPath);

            var s = _mailSettings.Settings;
            var generationDate = $"Дата создания отчета: {DateTime.Now}";
            var subject = $"Отчёт за {date:dd.MM.yyyy HH:mm} - {date1:dd.MM.yyyy HH:mm}";
            var msg = new MimeMessage();

            msg.From.Add(new MailboxAddress(s.Sender?.Name ?? "", s.Sender?.Email ?? ""));
            // чтобы не раскрывать список — лучше Bcc:
            foreach (var r in s.Recipients)
                msg.Bcc.Add(new MailboxAddress(r.Name ?? "", r.Email ?? ""));

            msg.Subject = subject;

            var lines = new[]
            {
                s.Sender?.Name ?? "Организация",
                generationDate,
                subject,
                "",
                "Отчёт находится во вложении.",
                "Пожалуйста, не отвечайте на это сообщение.",
                "",
                "Служба автоматической отправки сообщений ООО «МакПром»",
                "Тел. для справок: +7-(961)-671-41-45"
            };

            var body = new BodyBuilder
            {
                TextBody = string.Join(Environment.NewLine, lines)
            };

            // читаем файл в память — чтобы не держать файловый дескриптор
            var bytes = await File.ReadAllBytesAsync(reportPath, ct);
            body.Attachments.Add(
                Path.GetFileName(reportPath),
                bytes,
                new ContentType("application", "vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
            msg.Body = body.ToMessageBody();

            var socket =
                s.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect :
                s.SmtpPort == 587 ? SecureSocketOptions.StartTls :
                                    SecureSocketOptions.Auto;

            using var client = new SmtpClient { Timeout = 30000 };

            await client.ConnectAsync(s.SmtpServer, s.SmtpPort, socket, ct).ConfigureAwait(false);

            // логин может совпадать с Sender.Email, но лучше брать SmtpLogin
            if (!string.IsNullOrWhiteSpace(s.SmtpLogin))
                await client.AuthenticateAsync(s.SmtpLogin, s.SmtpPassword, ct).ConfigureAwait(false);

            await client.SendAsync(msg, ct).ConfigureAwait(false);
            await client.DisconnectAsync(true, ct).ConfigureAwait(false);
        }


    }
}