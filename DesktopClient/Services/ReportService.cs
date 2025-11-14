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


        public async Task<string> NewDailyReport(DateTime? Start, DateTime? Stop)
        {
            //List<Card> cards = await _repository.GetCardsForInterval(DateTime.Parse("2025-08-20 00:00:00"), DateTime.Parse("2025-08-29 00:00:00"));
            List<Card> cards = await _repository.GetCardsForInterval(Start, Stop);
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Отчёт");

            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;

            var cols = worksheet.Columns("A:I");
            cols.AdjustToContents();
            worksheet.Rows().AdjustToContents();

            foreach (var c in cols)
                c.Width += 5.0;

            var r1 = worksheet.Range("A1:B1").Merge(); //Организация
            var r2 = worksheet.Range("A2:B2").Merge(); //Подразделение

            var r3 = worksheet.Range("D1:E1").Merge(); //ООО МакПром
            var r4 = worksheet.Range("D2:E2").Merge(); //МиПЭ

            var r5 = worksheet.Range("B3:I3").Merge(); //Название листа

            var r6 = worksheet.Range("F4:G4").Merge(); //ФИО оператора

            var r7 = worksheet.Range("A8:A9").Merge(); //Номер операции
            var r8 = worksheet.Range("B8:D8").Merge(); //Перемещение зерна
            var r9 = worksheet.Range("E8:F8").Merge(); //Время перемещения
            var r10 = worksheet.Range("G8:H8").Merge(); //Показания весов
            var r11 = worksheet.Range("I8:I9").Merge(); //Перемещено за операцию

            //Шапка листа
            worksheet.Cell("A1").Value = "Организация:";
            worksheet.Cell("A2").Value = "Подразделение:";
            worksheet.Cell("D1").Value = "ООО \"МакПром\"";
            r3.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            worksheet.Cell("D2").Value = "МиПЭ";
            r4.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            worksheet.Cell("B3").Value = "Отчет перемещения зерна с элеваторных сооружений на мельницы";
            r5.Style.Font.Bold = true;
            r5.Style.Font.SetFontSize(15);

            worksheet.Cell("A4").Value = "Дата:";
            worksheet.Cell("A5").Value = "Время начала смены:";
            worksheet.Cell("A6").Value = "Время окончания смены:";
            worksheet.Cell("E4").Value = "Смена:";

            var date = worksheet.Cell("B4");
            var shiftStart = worksheet.Cell("E5");
            var shiftEnd = worksheet.Cell("E6");
            var person = worksheet.Cell("F4");


            date.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            shiftStart.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            shiftEnd.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            person.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            //Шапка таблицы
            worksheet.Cell("A8").Value = "№ операции";
            worksheet.Cell("A8").Style.Alignment.SetWrapText(true);
            worksheet.Cell("B8").Value = "Перемещение зерна";
            worksheet.Cell("B8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("E8").Value = "Время перемещения";
            worksheet.Cell("E8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("G8").Value = "Показания весов";
            worksheet.Cell("G8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("I8").Value = "Перемещено за операцию";
            worksheet.Cell("A8").Style.Alignment.SetWrapText(true);

            worksheet.Cell("B9").Value = "Из силоса элеватора, №";
            worksheet.Cell("B9").Style.Alignment.SetWrapText(true);
            worksheet.Cell("C9").Value = "В мельницу";
            worksheet.Cell("C9").Style.Alignment.SetWrapText(true);
            worksheet.Cell("D9").Value = "Силос мельницы";
            worksheet.Cell("B9").Style.Alignment.SetWrapText(true);
            worksheet.Cell("E9").Value = "Начало";
            worksheet.Cell("F9").Value = "Окончание";
            worksheet.Cell("G9").Value = "Весы 1";
            worksheet.Cell("H9").Value = "Весы 2";

            int rowCounter = 10;
            int operationNumber = 1;

            decimal? WeightM1 = 0;
            decimal? WeightM2 = 0;
            decimal? totalWeight = 0;

            date.Value = DateTime.Now.ToString("dd.MM.yyyy");
            shiftStart.Value = Start;
            shiftEnd.Value = Stop;
            ;

            foreach (Card card in cards)
            {
                worksheet.Cell(rowCounter, 1).Value = operationNumber;
                worksheet.Cell(rowCounter, 2).Value = card.SourceSilo;
                worksheet.Cell(rowCounter, 3).Value = card.Direction;
                worksheet.Cell(rowCounter, 4).Value = card.TargetSilo;
                worksheet.Cell(rowCounter, 5).Value = card.StartTime;
                worksheet.Cell(rowCounter, 6).Value = card.EndTime;
                worksheet.Cell(rowCounter, 7).Value = card.Weight1;
                worksheet.Cell(rowCounter, 8).Value = card.Weight2;
                worksheet.Cell(rowCounter, 9).Value = card.TotalWeight;

                if (card.Direction == "М1")
                {
                    WeightM1 += card.TotalWeight;
                }
                else if (card.Direction == "М2")
                {
                    WeightM2 += card.TotalWeight;
                }

                totalWeight += card.TotalWeight;

                rowCounter++;
                operationNumber++;
            }

            var range = worksheet.Range(1, 8, rowCounter, 9);
            //range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            //range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            //Итоговые значения в конце таблицы
            worksheet.Range(rowCounter, 2, rowCounter, 6);
            worksheet.Cell(rowCounter, 2).Value = "Итого суммарно на конец смены:  ";

            worksheet.Range(rowCounter, 7, rowCounter, 8);
            worksheet.Cell(rowCounter, 7).Value = totalWeight;
            worksheet.Cell(rowCounter, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            worksheet.Cell(rowCounter, 9).Value = "тонн";

            rowCounter++;

            worksheet.Range(rowCounter, 2, rowCounter, 6);
            worksheet.Cell(rowCounter, 2).Value = "В том числе на мельницу 1:  ";

            worksheet.Range(rowCounter, 7, rowCounter, 8);
            worksheet.Cell(rowCounter, 7).Value = WeightM1;
            worksheet.Cell(rowCounter, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            worksheet.Cell(rowCounter, 9).Value = "тонн";

            rowCounter++;

            worksheet.Range(rowCounter, 2, rowCounter, 6);
            worksheet.Cell(rowCounter, 2).Value = "В том числе на мельницу 2:  ";

            worksheet.Range(rowCounter, 7, rowCounter, 8);
            worksheet.Cell(rowCounter, 7).Value = WeightM2;
            worksheet.Cell(rowCounter, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            worksheet.Cell(rowCounter, 9).Value = "тонн";

            rowCounter += 2;

            worksheet.Cell(rowCounter, 6).Value = "Оператор ПУЭ";
            worksheet.Range(rowCounter, 8, rowCounter, 9);
            worksheet.Cell(rowCounter, 8).Style.Border.BottomBorder = XLBorderStyleValues.Thin;


            string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"Report_{DateTime.Now:yyyy_MM_dd_HHmm}.xlsx");

            workbook.SaveAs(filePath);

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });

            return filePath;
        }

        public async Task SendReportAsync(string reportPath, CancellationToken ct = default)
        {
            if (!File.Exists(reportPath))
                throw new FileNotFoundException("Report not found", reportPath);

            var s = _mailSettings.Settings;

            var subject = $"Отчёт за {DateTime.Today.AddDays(-1):dd.MM.yyyy}";
            var msg = new MimeMessage();

            msg.From.Add(new MailboxAddress(s.Sender?.Name ?? "", s.Sender?.Email ?? ""));
            // чтобы не раскрывать список — лучше Bcc:
            foreach (var r in s.Recipients)
                msg.Bcc.Add(new MailboxAddress(r.Name ?? "", r.Email ?? ""));

            msg.Subject = subject;

            var body = new BodyBuilder
            {
                TextBody =
                        $@"{s.Sender?.Name ?? "Организация"}
                        {subject}

                        Отчёт находится во вложении.
                        Пожалуйста, не отвечайте на это сообщение.

                        Служба автоматической отправки сообщений ООО «МакПром»
                        Тел. для справок: +7-(961)-671-41-45"
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
