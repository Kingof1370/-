using System;
using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;

namespace ExcelServer;

public static class ExcelParser
{
    static ExcelParser()
    {
        // EPPlus NonCommercial license requirement
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public static List<ExcelDataRow> ParseExcelFile(string filePath)
    {
        var list = new List<ExcelDataRow>();
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("فایل اکسل یافت نشد.", filePath);
        }

        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var worksheet = package.Workbook.Worksheets["data"];
            if (worksheet == null)
            {
                throw new Exception("شیت با نام 'data' در فایل اکسل یافت نشد.");
            }

            int rowCount = worksheet.Dimension?.End.Row ?? 0;
            // Assuming row 1 is header, data starts from row 2
            for (int r = 2; r <= rowCount; r++)
            {
                try
                {
                    // If row date columns are empty, skip
                    if (worksheet.Cells[r, 1].Value == null ||
                        worksheet.Cells[r, 2].Value == null ||
                        worksheet.Cells[r, 3].Value == null)
                    {
                        continue;
                    }

                    var row = new ExcelDataRow
                    {
                        Day = GetInt(worksheet.Cells[r, 1].Value),
                        Month = GetInt(worksheet.Cells[r, 2].Value),
                        Year = GetInt(worksheet.Cells[r, 3].Value),

                        FeedWeight1 = GetDouble(worksheet.Cells[r, 7].Value),
                        FeedGrade1 = GetDouble(worksheet.Cells[r, 8].Value),
                        RecoveryStage1 = GetDouble(worksheet.Cells[r, 16].Value),
                        RecoveryStage2 = GetDouble(worksheet.Cells[r, 25].Value),
                        RecoveryScavenger = GetDouble(worksheet.Cells[r, 35].Value),
                        RecoveryTotal = GetDouble(worksheet.Cells[r, 36].Value),

                        MassBalanceStage1 = GetDouble(worksheet.Cells[r, 37].Value),
                        MassBalanceStage2 = GetDouble(worksheet.Cells[r, 38].Value),
                        MassBalanceScavenger = GetDouble(worksheet.Cells[r, 39].Value),

                        CumulativeSales = GetDouble(worksheet.Cells[r, 49].Value),
                        CumulativeCarryStock = GetDouble(worksheet.Cells[r, 50].Value),
                        AverageSalesGrade = GetDouble(worksheet.Cells[r, 51].Value),
                        AverageCarryStockGrade = GetDouble(worksheet.Cells[r, 52].Value),

                        PersonnelCount = GetInt(worksheet.Cells[r, 60].Value),
                        DieselConsumption = GetDouble(worksheet.Cells[r, 61].Value),
                        RemainingPileStock = GetDouble(worksheet.Cells[r, 63].Value),
                        LineOperatingHours = GetDouble(worksheet.Cells[r, 64].Value),

                        PreProcessingSeparator1 = GetString(worksheet.Cells[r, 65].Value) ?? "خاموش",
                        PreProcessingSeparator2 = GetString(worksheet.Cells[r, 66].Value) ?? "خاموش",
                        ProcessingSeparator1 = GetString(worksheet.Cells[r, 67].Value) ?? "خاموش",
                        ProcessingSeparator2 = GetString(worksheet.Cells[r, 68].Value) ?? "خاموش",
                        ScavengerSeparator = GetString(worksheet.Cells[r, 69].Value) ?? "خاموش",

                        ReportText = GetString(worksheet.Cells[r, 70].Value) ?? string.Empty
                    };

                    list.Add(row);
                }
                catch
                {
                    // Skip or log row parse error if necessary
                }
            }
        }

        return list;
    }

    private static int GetInt(object? val)
    {
        if (val == null) return 0;
        if (int.TryParse(val.ToString(), out int res)) return res;
        if (double.TryParse(val.ToString(), out double dRes)) return (int)Math.Round(dRes);
        return 0;
    }

    private static double GetDouble(object? val)
    {
        if (val == null) return 0.0;
        if (double.TryParse(val.ToString(), out double res)) return res;
        return 0.0;
    }

    private static string GetString(object? val)
    {
        return val?.ToString() ?? string.Empty;
    }
}
