using System.Text.Json.Serialization;

namespace FoodStack.Domain.Menu {
    public class FoodStackMenu {
        [JsonPropertyName("menuID")]
        public string MenuID { get; set; }

        [JsonPropertyName("menuName")]
        public string MenuName { get; set; }

        [JsonPropertyName("meals")]
        public List<FoodStackMeal> Meals { get; set; }

        public FoodStackMenu() {
            this.MenuID = string.Empty;
            this.MenuName = string.Empty;
            this.Meals = new List<FoodStackMeal>();
        }
    }

    public class FoodStackMeal {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        public FoodStackMeal() {
            this.ID = string.Empty;
            this.Name = string.Empty;
            this.ImageUrl = string.Empty;
            this.Category = string.Empty;
            this.Price = 0;
        }
    }
}
