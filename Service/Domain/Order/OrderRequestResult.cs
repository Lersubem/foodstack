// FoodStackOrderPlaceResult.cs
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

namespace FoodStack.Domain.Order {

    public class OrderPlacementResultSchemaFilter : ISchemaFilter {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (context.Type != typeof(OrderPlacementResult)) {
                return;
            }

            OpenApiSchema successSchema = context.SchemaGenerator.GenerateSchema(typeof(OrderPlacementResultSuccess), context.SchemaRepository);
            OpenApiSchema parametersSchema = context.SchemaGenerator.GenerateSchema(typeof(OrderPlacementResultErrorParametersValidation), context.SchemaRepository);
            OpenApiSchema duplicationSchema = context.SchemaGenerator.GenerateSchema(typeof(OrderPlacementResultErrorDuplication), context.SchemaRepository);
            OpenApiSchema mealNotValidSchema = context.SchemaGenerator.GenerateSchema(typeof(OrderPlacementResultErrorMealNotValid), context.SchemaRepository);

            schema.OneOf = new List<OpenApiSchema> {
                successSchema,
                parametersSchema,
                duplicationSchema,
                mealNotValidSchema
            };

            schema.Discriminator = new OpenApiDiscriminator();
            schema.Discriminator.PropertyName = "status";
            schema.Discriminator.Mapping.Add("Success", "#/components/schemas/OrderPlacementResultSuccess");
            schema.Discriminator.Mapping.Add("InvalidOrderRequest", "#/components/schemas/OrderPlacementResultErrorParametersValidation");
            schema.Discriminator.Mapping.Add("OrderConflict", "#/components/schemas/OrderPlacementResultErrorDuplication");
            schema.Discriminator.Mapping.Add("MealNotValid", "#/components/schemas/OrderPlacementResultErrorMealNotValid");
        }
    }

    public abstract class OrderPlacementResult {
        [JsonPropertyName("status")]
        public abstract string Status { get; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        protected OrderPlacementResult() {
            this.Message = null;
        }
    }

    public sealed class OrderPlacementResultSuccess : OrderPlacementResult {
        [JsonPropertyName("order")]
        public FoodStackOrder Order { get; set; }

        public override string Status {
            get { return "Success"; }
        }

        public OrderPlacementResultSuccess() {
            this.Order = new FoodStackOrder();
        }
    }

    public sealed class OrderPlacementResultErrorParametersValidation : OrderPlacementResult {
        [JsonPropertyName("errors")]
        public IReadOnlyList<OrderPlacementResultErrorParametersValidationItem> Errors { get; set; }

        public override string Status {
            get { return "InvalidOrderRequest"; }
        }

        public OrderPlacementResultErrorParametersValidation() {
            this.Errors = new List<OrderPlacementResultErrorParametersValidationItem>();
        }
    }

    public sealed class OrderPlacementResultErrorDuplication : OrderPlacementResult {
        [JsonPropertyName("hasExistingOrder")]
        public bool HasExistingOrder { get; set; }

        [JsonPropertyName("isConflict")]
        public bool IsConflict { get; set; }

        [JsonPropertyName("existingOrder")]
        public FoodStackOrder? ExistingOrder { get; set; }

        public override string Status {
            get {
                if (this.HasExistingOrder && this.IsConflict == false) {
                    return "ExistingOrder";
                }

                return "OrderConflict";
            }
        }

        public OrderPlacementResultErrorDuplication() {
            this.HasExistingOrder = false;
            this.IsConflict = false;
            this.ExistingOrder = null;
        }
    }

    public sealed class OrderPlacementResultErrorMealNotValid : OrderPlacementResult {
        [JsonPropertyName("invalidMeals")]
        public IReadOnlyList<string> InvalidMeals { get; set; }

        public override string Status {
            get { return "MealNotValid"; }
        }

        public OrderPlacementResultErrorMealNotValid() {
            this.InvalidMeals = new List<string>();
        }
    }

    public class OrderPlacementResultErrorParametersValidationItem {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("mealID")]
        public string? MealID { get; set; }

        public OrderPlacementResultErrorParametersValidationItem() {
            this.Code = string.Empty;
            this.Message = string.Empty;
            this.MealID = null;
        }
    }
}
