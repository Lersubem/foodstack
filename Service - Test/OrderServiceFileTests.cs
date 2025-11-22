using FoodStack.Domain.Menu;
using FoodStack.Domain.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodStack.Test {
    public class OrderServiceFileTests {
        private OrderServiceFile CreateServiceWithDefaultMenu(out string ordersDirectoryPath) {
            ordersDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "FoodStackTests",
                "Orders",
                Guid.NewGuid().ToString("N"));

            FoodStackMeal meal1 = new FoodStackMeal();
            meal1.ID = "meal-1";
            meal1.Name = "Test Meal One";
            meal1.Category = "test";
            meal1.ImageUrl = "image-1.png";
            meal1.Price = 10;

            FoodStackMeal meal2 = new FoodStackMeal();
            meal2.ID = "meal-2";
            meal2.Name = "Test Meal Two";
            meal2.Category = "test";
            meal2.ImageUrl = "image-2.png";
            meal2.Price = 15;

            FoodStackMenu menu = new FoodStackMenu();
            menu.MenuID = "test-menu";
            menu.Meals.Add(meal1);
            menu.Meals.Add(meal2);

            List<FoodStackMenu> menus = new List<FoodStackMenu>();
            menus.Add(menu);

            IMenuService menuService = new MenuServiceStub(menus);

            OrderServiceFile service = new OrderServiceFile(ordersDirectoryPath, menuService);

            return service;
        }

        [Fact]
        public void OrderValidateRequestParameters_ValidRequest_ReturnsNull() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-valid";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-1";
            item.Quantity = 2;
            request.Meals.Add(item);

            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            Assert.Null(result);
        }

        [Fact]
        public void OrderValidateRequestParameters_NegativeQuantity_ReturnsErrorForThatMeal() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-negative";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-NEG";
            item.Quantity = -1;
            request.Meals.Add(item);

            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            Assert.NotNull(result);
            Assert.Equal("InvalidOrderRequest", result.Status);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);

            bool found = false;

            foreach (OrderPlacementResultErrorParametersValidationItem error in result.Errors) {
                if (error.Code == "QuantityNegative" && error.MealID == "meal-NEG") {
                    found = true;
                    break;
                }
            }

            Assert.True(found);
        }

        [Fact]
        public void OrderValidateRequestParameters_AllZeroQuantity_ReturnsAllZeroError() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-zero";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-1";
            item.Quantity = 0;
            request.Meals.Add(item);

            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            Assert.NotNull(result);
            Assert.Equal("InvalidOrderRequest", result.Status);

            bool found = false;

            foreach (OrderPlacementResultErrorParametersValidationItem error in result.Errors) {
                if (error.Code == "AllZeroQuantity") {
                    found = true;
                    break;
                }
            }

            Assert.True(found);
        }

        [Fact]
        public async Task OrderValidateRequestDuplication_WhenNoExistingOrder_ReturnsNull() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-new";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-1";
            item.Quantity = 1;
            request.Meals.Add(item);

            OrderPlacementResultErrorDuplication? result = await service.OrderValidateRequestDuplicationAsync(request);

            Assert.Null(result);
        }

        [Fact]
        public async Task OrderValidateRequestDuplication_SameRequest_ReturnsExistingOrderNoConflict() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-dup-same";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-1";
            item.Quantity = 2;
            request.Meals.Add(item);

            OrderPlacementResult firstResult = await service.OrderPlaceAsync(request);

            OrderPlacementResultErrorDuplication? duplicationResult = await service.OrderValidateRequestDuplicationAsync(request);

            Assert.NotNull(duplicationResult);
            Assert.True(duplicationResult.HasExistingOrder);
            Assert.False(duplicationResult.IsConflict);
            Assert.NotNull(duplicationResult.ExistingOrder);

            OrderPlacementResultSuccess success = Assert.IsType<OrderPlacementResultSuccess>(firstResult);
            Assert.Equal(success.Order.OrderID, duplicationResult.ExistingOrder!.OrderID);
        }

        [Fact]
        public async Task OrderValidateRequestDuplication_DifferentContent_ReturnsConflict() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest initialRequest = new FoodStackOrderRequest();
            initialRequest.RequestID = "req-dup-conflict";

            FoodStackOrderRequestItem item1 = new FoodStackOrderRequestItem();
            item1.MealID = "meal-1";
            item1.Quantity = 1;
            initialRequest.Meals.Add(item1);

            OrderPlacementResult firstResult = await service.OrderPlaceAsync(initialRequest);
            Assert.IsType<OrderPlacementResultSuccess>(firstResult);

            FoodStackOrderRequest conflictingRequest = new FoodStackOrderRequest();
            conflictingRequest.RequestID = "req-dup-conflict";

            FoodStackOrderRequestItem item2 = new FoodStackOrderRequestItem();
            item2.MealID = "meal-1";
            item2.Quantity = 3;
            conflictingRequest.Meals.Add(item2);

            OrderPlacementResultErrorDuplication? duplicationResult = await service.OrderValidateRequestDuplicationAsync(conflictingRequest);

            Assert.NotNull(duplicationResult);
            Assert.True(duplicationResult.HasExistingOrder);
            Assert.True(duplicationResult.IsConflict);
            Assert.Equal("OrderConflict", duplicationResult.Status);
        }

        [Fact]
        public async Task OrderPlaceAsync_InvalidMeal_ReturnsMealNotValidResultAndNoOrderSaved() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-invalid-meal";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "unknown-meal";
            item.Quantity = 1;
            request.Meals.Add(item);

            OrderPlacementResult result = await service.OrderPlaceAsync(request);

            OrderPlacementResultErrorMealNotValid mealResult = Assert.IsType<OrderPlacementResultErrorMealNotValid>(result);
            Assert.Equal("MealNotValid", mealResult.Status);
            Assert.Contains("unknown-meal", mealResult.InvalidMeals);

            if (Directory.Exists(ordersDirectoryPath)) {
                string[] files = Directory.GetFiles(ordersDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
                Assert.Empty(files);
            }
        }

        [Fact]
        public async Task OrderPlaceAsync_ValidRequest_SavesOrderAndCanBeRetrieved() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-success";

            FoodStackOrderRequestItem item1 = new FoodStackOrderRequestItem();
            item1.MealID = "meal-1";
            item1.Quantity = 1;
            request.Meals.Add(item1);

            FoodStackOrderRequestItem item2 = new FoodStackOrderRequestItem();
            item2.MealID = "meal-2";
            item2.Quantity = 2;
            request.Meals.Add(item2);

            OrderPlacementResult result = await service.OrderPlaceAsync(request);

            OrderPlacementResultSuccess success = Assert.IsType<OrderPlacementResultSuccess>(result);
            Assert.Equal("Success", success.Status);
            Assert.False(string.IsNullOrWhiteSpace(success.Order.OrderID));
            Assert.Equal("req-success", success.Order.Request.RequestID);
            Assert.Equal(2, success.Order.Request.Meals.Count);

            FoodStackOrder? byOrderID = await service.GetOrderByOrderIDAsync(success.Order.OrderID);
            Assert.NotNull(byOrderID);
            Assert.Equal(success.Order.OrderID, byOrderID!.OrderID);

            FoodStackOrder? byRequestID = await service.GetOrderByRequestIDAsync("req-success");
            Assert.NotNull(byRequestID);
            Assert.Equal(success.Order.OrderID, byRequestID!.OrderID);
        }

        [Fact]
        public async Task GetOrderByOrderID_NonExisting_ReturnsNull() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrder? order = await service.GetOrderByOrderIDAsync("does-not-exist");

            Assert.Null(order);
        }

        [Fact]
        public async Task GetOrderByRequestID_NonExisting_ReturnsNull() {
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrder? order = await service.GetOrderByRequestIDAsync("does-not-exist");

            Assert.Null(order);
        }

        private sealed class MenuServiceStub : IMenuService {
            private readonly IReadOnlyList<FoodStackMenu> menus;

            public MenuServiceStub(IReadOnlyList<FoodStackMenu> menus) {
                this.menus = menus;
            }

            public Task<IReadOnlyList<FoodStackMenu>> GetAllMenusAsync() {
                return Task.FromResult<IReadOnlyList<FoodStackMenu>>(this.menus);
            }

            public Task<FoodStackMenu?> GetMenuAsync(string menuID) {
                return Task.FromResult<FoodStackMenu?>(null);
            }

            public Task<IReadOnlyList<string>> GetMenuIDsAsync() {
                IReadOnlyList<string> ids = Array.Empty<string>();
                return Task.FromResult(ids);
            }
        }
    }
}
