using ISO11820.Core.Events;
using ISO11820.Data;
using System.Text;

namespace ISO11820.Services;

public class ExportService
{
    private readonly ConfigurationService _config;

    public ExportService(ConfigurationService config)
    {
        _config = config;
    }

    public string SaveCsv(string productId, string testId, List<SensorDataPoint> data)
    {
        var dir = Path.Combine(_config.TestDataDirectory, productId, testId);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "sensor_data.csv");

        var sb = new StringBuilder();
        sb.AppendLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
        foreach (var point in data)
        {
            sb.AppendLine($"{point.Time},{point.Temp1:F1},{point.Temp2:F1},{point.TempSurface:F1},{point.TempCenter:F1},{point.TempCalibration:F1}");
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public string SaveExcelReport(TestMaster test, ProductMaster product, List<SensorDataPoint> data)
    {
        var dir = _config.ReportOutputDirectory;
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{test.TestId}_报告.xlsx");

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using var package = new OfficeOpenXml.ExcelPackage();
        var sheet1 = package.Workbook.Worksheets.Add("试验信息");
        sheet1.Cells["A1"].Value = "ISO 11820 不燃性试验报告";
        sheet1.Cells["A1"].Style.Font.Size = 16;
        sheet1.Cells["A1"].Style.Font.Bold = true;

        int row = 3;
        var info = new (string, object)[]
        {
            ("样品编号", test.ProductId), ("样品名称", product.ProductName),
            ("规格型号", product.Specific), ("试验日期", test.TestDate.ToString("yyyy-MM-dd")),
            ("操作员", test.Operator), ("环境温度(°C)", test.AmbTemp),
            ("环境湿度(%)", test.AmbHumi), ("试验前质量(g)", test.PreWeight),
            ("试验后质量(g)", test.PostWeight), ("失重率(%)", $"{test.LostWeightPer:F2}"),
            ("样品温升(°C)", $"{test.DeltaTf:F1}"), ("试验时长(秒)", test.TotalTestTime),
            ("判定结论", test.LostWeightPer <= 50 && test.DeltaTf <= 50 ? "通过" : "不通过")
        };
        foreach (var (label, value) in info)
        {
            sheet1.Cells[row, 1].Value = label;
            sheet1.Cells[row, 2].Value = value;
            row++;
        }

        var sheet2 = package.Workbook.Worksheets.Add("温度数据");
        sheet2.Cells["A1"].Value = "Time(s)";
        sheet2.Cells["B1"].Value = "炉温1(°C)";
        sheet2.Cells["C1"].Value = "炉温2(°C)";
        sheet2.Cells["D1"].Value = "表面温(°C)";
        sheet2.Cells["E1"].Value = "中心温(°C)";
        for (int i = 0; i < data.Count; i++)
        {
            sheet2.Cells[i + 2, 1].Value = data[i].Time;
            sheet2.Cells[i + 2, 2].Value = data[i].Temp1;
            sheet2.Cells[i + 2, 3].Value = data[i].Temp2;
            sheet2.Cells[i + 2, 4].Value = data[i].TempSurface;
            sheet2.Cells[i + 2, 5].Value = data[i].TempCenter;
        }

        // Chart sheet
        if (data.Count > 0)
        {
            var chartSheet = package.Workbook.Worksheets.Add("_ChartData");
            for (int i = 0; i < data.Count; i++)
            {
                chartSheet.Cells[i + 1, 1].Value = data[i].Time;
                chartSheet.Cells[i + 1, 2].Value = data[i].Temp1;
                chartSheet.Cells[i + 1, 3].Value = data[i].Temp2;
                chartSheet.Cells[i + 1, 4].Value = data[i].TempSurface;
                chartSheet.Cells[i + 1, 5].Value = data[i].TempCenter;
            }

            var chartSheet3 = package.Workbook.Worksheets.Add("温度曲线");
            var chart = chartSheet3.Drawings.AddChart("TempChart", OfficeOpenXml.Drawing.Chart.eChartType.XYScatterLines);
            chart.Title.Text = "温度曲线";
            chart.SetPosition(0, 0, 0, 0);
            chart.SetSize(800, 600);

            var s1 = chart.Series.Add(chartSheet.Cells[1, 2, data.Count, 2], chartSheet.Cells[1, 1, data.Count, 1]);
            s1.Header = "炉温1";
            var s2 = chart.Series.Add(chartSheet.Cells[1, 3, data.Count, 3], chartSheet.Cells[1, 1, data.Count, 1]);
            s2.Header = "炉温2";
            var s3 = chart.Series.Add(chartSheet.Cells[1, 4, data.Count, 4], chartSheet.Cells[1, 1, data.Count, 1]);
            s3.Header = "表面温";
            var s4 = chart.Series.Add(chartSheet.Cells[1, 5, data.Count, 5], chartSheet.Cells[1, 1, data.Count, 1]);
            s4.Header = "中心温";
        }

        package.SaveAs(filePath);
        return filePath;
    }

    public string? SavePdfReport(TestMaster test, ProductMaster product, List<SensorDataPoint> data)
    {
        if (!_config.EnablePdfExport) return null;

        var dir = _config.ReportOutputDirectory;
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{test.TestId}_报告.pdf");

        var document = new PdfSharp.Pdf.PdfDocument();
        document.Info.Title = "ISO 11820 不燃性试验报告";

        var page = document.AddPage();
        var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
        var font = new PdfSharp.Drawing.XFont("SimHei", 12);
        var fontTitle = new PdfSharp.Drawing.XFont("SimHei", 18);

        double y = 40;
        gfx.DrawString("ISO 11820 不燃性试验报告", fontTitle, PdfSharp.Drawing.XBrushes.Black, 40, y);
        y += 30;

        var lines = new[]
        {
            $"样品编号: {test.ProductId}",
            $"样品名称: {product.ProductName}",
            $"试验日期: {test.TestDate:yyyy-MM-dd}",
            $"操作员: {test.Operator}",
            $"环境温度: {test.AmbTemp}°C",
            $"环境湿度: {test.AmbHumi}%",
            $"试验前质量: {test.PreWeight}g",
            $"试验后质量: {test.PostWeight}g",
            $"失重率: {test.LostWeightPer:F2}%",
            $"样品温升: {test.DeltaTf:F1}°C",
            $"试验时长: {test.TotalTestTime}秒",
            $"判定: {(test.LostWeightPer <= 50 && test.DeltaTf <= 50 ? "通过" : "不通过")}"
        };

        foreach (var line in lines)
        {
            y += 20;
            gfx.DrawString(line, font, PdfSharp.Drawing.XBrushes.Black, 40, y);
        }

        document.Save(filePath);
        return filePath;
    }
}
