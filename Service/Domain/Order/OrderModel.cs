// Order.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FoodStack.Domain.Order {

    public class FoodStackOrder {
        [JsonPropertyName("orderID")]
        public string OrderID { get; set; }

        [JsonPropertyName("orderTime")]
        public DateTimeOffset OrderTime { get; set; }

        [JsonPropertyName("request")]
        public FoodStackOrderRequest Request { get; set; }

        public FoodStackOrder() {
            this.OrderID = string.Empty;
            this.OrderTime = DateTimeOffset.MinValue;
            this.Request = new FoodStackOrderRequest();
        }
    }

    public class FoodStackOrderRequest {
        [JsonPropertyName("requestID")]
        public string RequestID { get; set; }

        [JsonPropertyName("meals")]
        public List<FoodStackOrderRequestItem> Meals { get; set; }

        public FoodStackOrderRequest() {
            this.RequestID = string.Empty;
            this.Meals = new List<FoodStackOrderRequestItem>();
        }
    }

    public class FoodStackOrderRequestItem {
        [JsonPropertyName("id")]
        public string MealID { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public FoodStackOrderRequestItem() {
            this.MealID = string.Empty;
            this.Quantity = 0;
        }
    }
}
