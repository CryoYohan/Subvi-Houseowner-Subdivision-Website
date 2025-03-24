using System.Text.Json.Serialization;

public class PayMongoPaymentIntentResponse
{
    [JsonPropertyName("data")]
    public PaymentIntentData Data { get; set; }
}

public class PaymentIntentData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("attributes")]
    public PaymentIntentAttributes Attributes { get; set; }
}

public class PaymentIntentAttributes
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("next_action")]
    public PaymentIntentNextAction NextAction { get; set; }
}

public class PaymentIntentNextAction
{
    [JsonPropertyName("redirect")]
    public PaymentIntentRedirect Redirect { get; set; }
}

public class PaymentIntentRedirect
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
