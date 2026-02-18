namespace HotelAPIMiddleware.Contracts.Responses;

public class RateResult
{
    public string RateId { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty; // "STUBA" | "RATEHAWK"

    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";

    public bool Refundable { get; set; }

    // Optional extras (safe for UI)
    public string Board { get; set; } = "";     // RO/BB/HB...
    public string PaymentType { get; set; } = ""; // Prepay/PayAtHotel

    // RateHawk specific fields
    public string MatchHash { get; set; } = "";
    public string SearchHash { get; set; } = "";
    public string BookHash { get; set; } = "";  // From /search/hp/ endpoint
    public List<string> DailyPrices { get; set; } = new();
    public string Meal { get; set; } = "";
    public MealData? MealData { get; set; }
    public PaymentOptions? PaymentOptions { get; set; }
    public RgExt? RgExt { get; set; }
    public string RoomNameInfo { get; set; } = "";
    public List<string> SerpFilters { get; set; } = new();
    public int Allotment { get; set; }
    public List<string> AmenitiesData { get; set; } = new();
    public bool AnyResidency { get; set; }
    public RoomDataTrans? RoomDataTrans { get; set; }
    public string LegalInfo { get; set; } = "";
    public bool IsPackage { get; set; }

    // Stuba specific fields
    public string QuoteId { get; set; } = "";
    public string RoomTypeCode { get; set; } = "";
    public string RoomTypeText { get; set; } = "";
    public string MealTypeCode { get; set; } = "";
    public string MealTypeText { get; set; } = "";
    public string CancellationPolicyStatus { get; set; } = "";
    public string CancelPolicyFrom { get; set; } = "";
}

// RateHawk nested objects
public class MealData
{
    public string Value { get; set; } = "";
    public bool HasBreakfast { get; set; }
    public bool NoChildMeal { get; set; }
}

public class PaymentOptions
{
    public List<PaymentType> PaymentTypes { get; set; } = new();
}

public class PaymentType
{
    public string Amount { get; set; } = "";
    public string ShowAmount { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public string ShowCurrencyCode { get; set; } = "";
    public string By { get; set; } = "";
    public bool IsNeedCreditCardData { get; set; }
    public bool IsNeedCvc { get; set; }
    public string Type { get; set; } = "";
    public VatData? VatData { get; set; }
    public TaxData? TaxData { get; set; }
    public CommissionInfo? CommissionInfo { get; set; }
    public CancellationPenalties? CancellationPenalties { get; set; }
    public string RecommendedPrice { get; set; } = "";
}

public class VatData
{
    public bool Included { get; set; }
    public bool Applied { get; set; }
    public string Amount { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public string Value { get; set; } = "";
}

public class TaxData
{
    public List<Tax> Taxes { get; set; } = new();
}

public class Tax
{
    public string Name { get; set; } = "";
    public bool IncludedBySupplier { get; set; }
    public string Amount { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
}

public class CommissionInfo
{
    public CommissionAmount? Show { get; set; }
    public CommissionAmount? Charge { get; set; }
}

public class CommissionAmount
{
    public string AmountGross { get; set; } = "";
    public string AmountNet { get; set; } = "";
    public string AmountCommission { get; set; } = "";
}

public class CancellationPenalties
{
    public List<CancellationPolicy> Policies { get; set; } = new();
    public string FreeCancellationBefore { get; set; } = "";
}

public class CancellationPolicy
{
    public string StartAt { get; set; } = "";
    public string EndAt { get; set; } = "";
    public string AmountCharge { get; set; } = "";
    public string AmountShow { get; set; } = "";
    public CommissionInfo? CommissionInfo { get; set; }
}

public class RgExt
{
    public int Class { get; set; }
    public int Quality { get; set; }
    public int Sex { get; set; }
    public int Bathroom { get; set; }
    public int Bedding { get; set; }
    public int Family { get; set; }
    public int Capacity { get; set; }
    public int Club { get; set; }
    public int Bedrooms { get; set; }
    public int Balcony { get; set; }
    public int View { get; set; }
    public int Floor { get; set; }
}

public class RoomDataTrans
{
    public string MainRoomType { get; set; } = "";
    public string MainName { get; set; } = "";
    public string Bathroom { get; set; } = "";
    public string BeddingType { get; set; } = "";
    public string MiscRoomType { get; set; } = "";
}