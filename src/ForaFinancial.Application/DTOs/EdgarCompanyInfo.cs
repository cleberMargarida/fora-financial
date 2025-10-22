using System.Text.Json.Serialization;

namespace ForaFinancial.Application.DTOs;

public class EdgarCompanyInfo
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]//"cik": "0001858912",
    public int Cik { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public InfoFact? Facts { get; set; }

    public class InfoFact
    {
        [JsonPropertyName("us-gaap")]
        public InfoFactUsGaap? UsGaap { get; set; }
    }

    public class InfoFactUsGaap
    {
        public InfoFactUsGaapNetIncomeLoss? NetIncomeLoss { get; set; }
    }

    public class InfoFactUsGaapNetIncomeLoss
    {
        public InfoFactUsGaapIncomeLossUnits? Units { get; set; }
    }

    public class InfoFactUsGaapIncomeLossUnits
    {
        [JsonPropertyName("USD")]
        public InfoFactUsGaapIncomeLossUnitsUsd[]? Usd { get; set; }
    }

    public class InfoFactUsGaapIncomeLossUnitsUsd
    {
        /// <summary>
        /// Possibilities include 10-Q, 10-K, 8-K, 20-F, 40-F, 6-K, and their variants.
        /// YOU ARE INTERESTED ONLY IN 10-K DATA!
        /// </summary>
        public string Form { get; set; } = string.Empty;

        /// <summary>
        /// For yearly information, the format is CY followed by the year number.
        /// For example: CY2021.
        /// YOU ARE INTERESTED ONLY IN YEARLY INFORMATION WHICH FOLLOWS THIS FORMAT!
        /// </summary>
        public string Frame { get; set; } = string.Empty;

        /// <summary>
        /// The income/loss amount.
        /// </summary>
        public decimal Val { get; set; }
    }
}
