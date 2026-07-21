using System;

namespace ExcelServer;

public class ExcelDataRow
{
    public int Day { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // SECTION 1: KPIs
    public double FeedWeight1 { get; set; }
    public double FeedGrade1 { get; set; }
    public double RecoveryStage1 { get; set; }
    public double RecoveryStage2 { get; set; }
    public double RecoveryScavenger { get; set; }
    public double RecoveryTotal { get; set; }

    // SECTION 2: Mass Balance
    public double MassBalanceStage1 { get; set; }
    public double MassBalanceStage2 { get; set; }
    public double MassBalanceScavenger { get; set; }

    // SECTION 3: Sales and Stock
    public double CumulativeSales { get; set; }
    public double CumulativeCarryStock { get; set; }
    public double RemainingPileStock { get; set; }
    public double AverageSalesGrade { get; set; }
    public double AverageCarryStockGrade { get; set; }

    // SECTION 4: Resources and Consumption
    public int PersonnelCount { get; set; }
    public double DieselConsumption { get; set; }
    public double LineOperatingHours { get; set; }

    // SECTION 5: Separator Status
    public string PreProcessingSeparator1 { get; set; } = "خاموش";
    public string PreProcessingSeparator2 { get; set; } = "خاموش";
    public string ProcessingSeparator1 { get; set; } = "خاموش";
    public string ProcessingSeparator2 { get; set; } = "خاموش";
    public string ScavengerSeparator { get; set; } = "خاموش";

    // SECTION 6: Text Report
    public string ReportText { get; set; } = string.Empty;
}
