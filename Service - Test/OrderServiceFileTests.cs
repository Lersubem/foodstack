using FoodStack.Domain.Menu;
using FoodStack.Domain.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodStack.Test {
    /// <summary>
    /// Unit tests for <see cref="OrderServiceFile"/> using a stubbed <see cref="IMenuService"/>
    /// and a temporary directory for file-based order persistence.
    /// </summary>
    public class OrderServiceFileTests {
        /// <summary>
        /// Creates a default <see cref="OrderServiceFile"/> instance with an in-memory menu service
        /// exposing two valid meals (meal-1, meal-2) under a single menu.
        /// </summary>
        /// <param name="ordersDirectoryPath">
        /// Output parameter that receives the path of the temporary directory used to store orders.
        /// </param>
        /// <returns>An <see cref="OrderServiceFile"/> configured with a test menu.</returns>
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

        /// <summary>
        /// Helper to build a request with a given ID and a set of meal/quantity pairs.
        /// </summary>
        /// <param name="requestID">The client request ID to use for idempotency.</param>
        /// <param name="items">Tuples of (MealID, Quantity) to attach to the request.</param>
        /// <returns>A populated <see cref="FoodStackOrderRequest"/> instance.</returns>
        private static FoodStackOrderRequest CreateOrderRequest(string requestID, params (string MealID, int Quantity)[] items) {
            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = requestID;

            foreach ((string MealID, int Quantity) pair in items) {
                FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
                item.MealID = pair.MealID;
                item.Quantity = pair.Quantity;
                request.Meals.Add(item);
            }

            return request;
        }

        /// <summary>
        /// Ensures a fully valid order request returns null (no validation errors).
        /// </summary>
        [Fact]
        public void OrderValidateRequestParameters_ValidRequest_ReturnsNull() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-valid";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-1";
            item.Quantity = 2;
            request.Meals.Add(item);

            // Act
            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Ensures negative quantities are rejected and surfaced as a parameter validation error
        /// for the specific meal.
        /// </summary>
        [Fact]
        public void OrderValidateRequestParameters_NegativeQuantity_ReturnsErrorForThatMeal() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-negative";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-NEG";
            item.Quantity = -1;
            request.Meals.Add(item);

            // Act
            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            // Assert
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

        /// <summary>
        /// Ensures a request where all quantities are zero is rejected with a dedicated error.
        /// </summary>
        [Fact]
        public void OrderValidateRequestParameters_AllZeroQuantity_ReturnsAllZeroError() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-zero";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-1";
            item.Quantity = 0;
            request.Meals.Add(item);

            // Act
            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            // Assert
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

        /// <summary>
        /// Verifies that a null request passed into parameter validation
        /// returns a validation error result instead of throwing.
        /// </summary>
        [Fact]
        public void OrderValidateRequestParameters_NullRequest_ReturnsValidationError() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            // Act
            OrderPlacementResultErrorParametersValidation? result =
                service.OrderValidateRequestParameters(null!);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("InvalidOrderRequest", result.Status);
            Assert.True(string.Equals(result.Message, "Order request is invalid.", StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(result.Message));
        }

        /// <summary>
        /// Verifies that a request with no meals is rejected as invalid.
        /// </summary>
        [Fact]
        public void OrderValidateRequestParameters_EmptyMeals_ReturnsError() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-empty";

            // Act
            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("InvalidOrderRequest", result.Status);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);
        }

        /// <summary>
        /// Verifies that a mixed request (negative + zero) produces an error for the negative item,
        /// and does not trigger the AllZeroQuantity error (because not all items are zero).
        /// </summary>
        [Fact]
        public void OrderValidateRequestParameters_MixedNegativeAndZero_ProducesErrorForNegativeOnly() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-mixed";

            FoodStackOrderRequestItem negative = new FoodStackOrderRequestItem();
            negative.MealID = "meal-neg";
            negative.Quantity = -2;
            request.Meals.Add(negative);

            FoodStackOrderRequestItem zero = new FoodStackOrderRequestItem();
            zero.MealID = "meal-zero";
            zero.Quantity = 0;
            request.Meals.Add(zero);

            // Act
            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);

            bool hasNegativeError = false;
            bool hasAllZeroError = false;

            foreach (OrderPlacementResultErrorParametersValidationItem error in result.Errors) {
                if (error.MealID == "meal-neg") {
                    hasNegativeError = true;
                }

                if (error.Code == "AllZeroQuantity") {
                    hasAllZeroError = true;
                }
            }

            Assert.True(hasNegativeError);
            Assert.False(hasAllZeroError);
        }

        /// <summary>
        /// Verifies that whitespace-only request IDs are treated as invalid and generate a validation error.
        /// </summary>
        [Fact]
        public void OrderValidateRequestParameters_WhitespaceRequestID_ReturnsError() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = CreateOrderRequest("  ", ("meal-1", 1));

            // Act
            OrderPlacementResultErrorParametersValidation? result = service.OrderValidateRequestParameters(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("InvalidOrderRequest", result.Status);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);
        }

        /// <summary>
        /// Ensures that when no prior order exists with a given request ID,
        /// duplication validation returns null.
        /// </summary>
        [Fact]
        public async Task OrderValidateRequestDuplication_WhenNoExistingOrder_ReturnsNull() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-new";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-1";
            item.Quantity = 1;
            request.Meals.Add(item);

            // Act
            OrderPlacementResultErrorDuplication? result = await service.OrderValidateRequestDuplicationAsync(request);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Ensures that a null request passed into duplication validation throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public async Task OrderValidateRequestDuplication_NullRequest_ThrowsArgumentNullException() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            // Act
            Task act = service.OrderValidateRequestDuplicationAsync(null!);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await act);
        }

        /// <summary>
        /// Ensures that two different request IDs with different payloads are not treated as duplicates.
        /// </summary>
        [Fact]
        public async Task OrderValidateRequestDuplication_DifferentRequestIDs_NotDuplicate() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest first = CreateOrderRequest("req-a", ("meal-1", 1));
            FoodStackOrderRequest second = CreateOrderRequest("req-b", ("meal-1", 1));

            OrderPlacementResult firstResult = await service.OrderPlaceAsync(first);
            Assert.IsType<OrderPlacementResultSuccess>(firstResult);

            // Act
            OrderPlacementResultErrorDuplication? duplicationResult = await service.OrderValidateRequestDuplicationAsync(second);

            // Assert
            Assert.Null(duplicationResult);
        }

        /// <summary>
        /// Ensures that the same request sent twice with identical payload returns an existing order
        /// and is not reported as a conflict.
        /// </summary>
        [Fact]
        public async Task OrderValidateRequestDuplication_SameRequest_ReturnsExistingOrderNoConflict() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-dup-same";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "meal-1";
            item.Quantity = 2;
            request.Meals.Add(item);

            OrderPlacementResult firstResult = await service.OrderPlaceAsync(request);

            // Act
            OrderPlacementResultErrorDuplication? duplicationResult = await service.OrderValidateRequestDuplicationAsync(request);

            // Assert
            Assert.NotNull(duplicationResult);
            Assert.True(duplicationResult.HasExistingOrder);
            Assert.False(duplicationResult.IsConflict);
            Assert.NotNull(duplicationResult.ExistingOrder);

            OrderPlacementResultSuccess success = Assert.IsType<OrderPlacementResultSuccess>(firstResult);
            Assert.Equal(success.Order.OrderID, duplicationResult.ExistingOrder!.OrderID);
        }

        /// <summary>
        /// Ensures that the same request ID with different quantities is treated as a conflict.
        /// </summary>
        [Fact]
        public async Task OrderValidateRequestDuplication_DifferentContent_ReturnsConflict() {
            // Arrange
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

            // Act
            OrderPlacementResultErrorDuplication? duplicationResult = await service.OrderValidateRequestDuplicationAsync(conflictingRequest);

            // Assert
            Assert.NotNull(duplicationResult);
            Assert.True(duplicationResult.HasExistingOrder);
            Assert.True(duplicationResult.IsConflict);
            Assert.Equal("OrderConflict", duplicationResult.Status);
        }

        /// <summary>
        /// Ensures that item order does not matter when comparing two requests for duplication.
        /// Same meals and quantities in different order should not be treated as a conflict.
        /// </summary>
        [Fact]
        public async Task OrderValidateRequestDuplication_SameContentDifferentOrder_NoConflict() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest first = CreateOrderRequest(
                "req-dup-order",
                ("meal-1", 1),
                ("meal-2", 2));

            FoodStackOrderRequest second = CreateOrderRequest(
                "req-dup-order",
                ("meal-2", 2),
                ("meal-1", 1));

            OrderPlacementResult firstResult = await service.OrderPlaceAsync(first);
            OrderPlacementResultSuccess success = Assert.IsType<OrderPlacementResultSuccess>(firstResult);

            // Act
            OrderPlacementResultErrorDuplication? duplicationResult = await service.OrderValidateRequestDuplicationAsync(second);

            // Assert
            Assert.NotNull(duplicationResult);
            Assert.True(duplicationResult.HasExistingOrder);
            Assert.False(duplicationResult.IsConflict);
            Assert.NotNull(duplicationResult.ExistingOrder);
            Assert.Equal(success.Order.OrderID, duplicationResult.ExistingOrder!.OrderID);
        }

        /// <summary>
        /// Ensures placing an order with an invalid meal ID returns a MealNotValid result
        /// and does not persist any order files.
        /// </summary>
        [Fact]
        public async Task OrderPlaceAsync_InvalidMeal_ReturnsMealNotValidResultAndNoOrderSaved() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = new FoodStackOrderRequest();
            request.RequestID = "req-invalid-meal";

            FoodStackOrderRequestItem item = new FoodStackOrderRequestItem();
            item.MealID = "unknown-meal";
            item.Quantity = 1;
            request.Meals.Add(item);

            // Act
            OrderPlacementResult result = await service.OrderPlaceAsync(request);

            // Assert
            OrderPlacementResultErrorMealNotValid mealResult = Assert.IsType<OrderPlacementResultErrorMealNotValid>(result);
            Assert.Equal("MealNotValid", mealResult.Status);
            Assert.Contains("unknown-meal", mealResult.InvalidMeals);

            if (Directory.Exists(ordersDirectoryPath)) {
                string[] files = Directory.GetFiles(ordersDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
                Assert.Empty(files);
            }
        }

        /// <summary>
        /// Ensures that parameter validation failures returned from <see cref="OrderPlaceAsync"/>
        /// do not write any order files.
        /// </summary>
        [Fact]
        public async Task OrderPlaceAsync_InvalidParameters_ReturnsValidationErrorAndDoesNotPersist() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = CreateOrderRequest("req-param-invalid", ("meal-1", 0));

            // Act
            OrderPlacementResult result = await service.OrderPlaceAsync(request);

            // Assert
            OrderPlacementResultErrorParametersValidation validation = Assert.IsType<OrderPlacementResultErrorParametersValidation>(result);
            Assert.Equal("InvalidOrderRequest", validation.Status);

            if (Directory.Exists(ordersDirectoryPath)) {
                string[] files = Directory.GetFiles(ordersDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
                Assert.Empty(files);
            }
        }

        /// <summary>
        /// Ensures that calling <see cref="OrderPlaceAsync"/> with a null request
        /// returns a parameter validation error result.
        /// </summary>
        [Fact]
        public async Task OrderPlaceAsync_NullRequest_ReturnsValidationError() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            // Act
            OrderPlacementResult result = await service.OrderPlaceAsync(null!);

            // Assert
            OrderPlacementResultErrorParametersValidation validation =
                Assert.IsType<OrderPlacementResultErrorParametersValidation>(result);

            Assert.Equal("Order request is invalid.", validation.Message);
        }

        /// <summary>
        /// Ensures that a valid request is saved and can be retrieved by both order ID and request ID.
        /// </summary>
        [Fact]
        public async Task OrderPlaceAsync_ValidRequest_SavesOrderAndCanBeRetrieved() {
            // Arrange
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

            // Act
            OrderPlacementResult result = await service.OrderPlaceAsync(request);

            // Assert
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

        /// <summary>
        /// Verifies that the stored order time is close to the current time when the order is placed.
        /// </summary>
        [Fact]
        public async Task OrderPlaceAsync_SetsOrderTimeCloseToNow() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);
            DateTimeOffset before = DateTimeOffset.UtcNow;

            FoodStackOrderRequest request = CreateOrderRequest("req-time", ("meal-1", 1));

            // Act
            OrderPlacementResult result = await service.OrderPlaceAsync(request);

            // Assert
            OrderPlacementResultSuccess success = Assert.IsType<OrderPlacementResultSuccess>(result);
            DateTimeOffset after = DateTimeOffset.UtcNow;

            Assert.True(success.Order.OrderTime >= before);
            Assert.True(success.Order.OrderTime <= after);
        }

        /// <summary>
        /// Verifies that placing multiple orders with different request IDs
        /// creates multiple persisted order files.
        /// </summary>
        [Fact]
        public async Task OrderPlaceAsync_MultipleOrders_CreateMultipleFiles() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest req1 = CreateOrderRequest("req-a", ("meal-1", 1));
            FoodStackOrderRequest req2 = CreateOrderRequest("req-b", ("meal-2", 2));

            // Act
            await service.OrderPlaceAsync(req1);
            await service.OrderPlaceAsync(req2);

            // Assert
            if (Directory.Exists(ordersDirectoryPath)) {
                string[] files = Directory.GetFiles(ordersDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
                Assert.True(files.Length >= 2);
            }
        }

        /// <summary>
        /// Verifies that placing the same request twice through <see cref="OrderPlaceAsync"/>
        /// returns a duplication result on the second call rather than creating a new order.
        /// </summary>
        [Fact]
        public async Task OrderPlaceAsync_DuplicateRequestID_ReturnsDuplicationOnSecondCall() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest request = CreateOrderRequest("req-dup-place", ("meal-1", 1));

            // Act
            OrderPlacementResult first = await service.OrderPlaceAsync(request);
            OrderPlacementResult second = await service.OrderPlaceAsync(request);

            // Assert
            OrderPlacementResultSuccess success = Assert.IsType<OrderPlacementResultSuccess>(first);
            OrderPlacementResultErrorDuplication duplication = Assert.IsType<OrderPlacementResultErrorDuplication>(second);

            Assert.True(duplication.HasExistingOrder);
            Assert.False(duplication.IsConflict);
            Assert.NotNull(duplication.ExistingOrder);
            Assert.Equal(success.Order.OrderID, duplication.ExistingOrder!.OrderID);
        }

        /// <summary>
        /// Verifies that trying to place a conflicting request (same request ID, different payload)
        /// returns a duplication error with conflict reported.
        /// </summary>
        [Fact]
        public async Task OrderPlaceAsync_DuplicateRequestIDWithDifferentContent_ReturnsConflictDuplication() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            FoodStackOrderRequest first = CreateOrderRequest("req-dup-place-conflict", ("meal-1", 1));
            FoodStackOrderRequest second = CreateOrderRequest("req-dup-place-conflict", ("meal-1", 2));

            // Act
            OrderPlacementResult firstResult = await service.OrderPlaceAsync(first);
            OrderPlacementResult secondResult = await service.OrderPlaceAsync(second);

            // Assert
            Assert.IsType<OrderPlacementResultSuccess>(firstResult);
            OrderPlacementResultErrorDuplication duplication = Assert.IsType<OrderPlacementResultErrorDuplication>(secondResult);

            Assert.True(duplication.HasExistingOrder);
            Assert.True(duplication.IsConflict);
            Assert.Equal("OrderConflict", duplication.Status);
        }

        /// <summary>
        /// Ensures that querying for an unknown order ID returns null.
        /// </summary>
        [Fact]
        public async Task GetOrderByOrderID_NonExisting_ReturnsNull() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            // Act
            FoodStackOrder? order = await service.GetOrderByOrderIDAsync("does-not-exist");

            // Assert
            Assert.Null(order);
        }

        /// <summary>
        /// Ensures that querying for an unknown request ID returns null.
        /// </summary>
        [Fact]
        public async Task GetOrderByRequestID_NonExisting_ReturnsNull() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            // Act
            FoodStackOrder? order = await service.GetOrderByRequestIDAsync("does-not-exist");

            // Assert
            Assert.Null(order);
        }

        /// <summary>
        /// Verifies that whitespace order IDs are rejected and cause an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public async Task GetOrderByOrderID_Whitespace_ThrowsArgumentException() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            // Act
            Task act = service.GetOrderByOrderIDAsync("  ");

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await act);
        }

        /// <summary>
        /// Verifies that whitespace request IDs are rejected and cause an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public async Task GetOrderByRequestID_Whitespace_ThrowsArgumentException() {
            // Arrange
            string ordersDirectoryPath;
            OrderServiceFile service = this.CreateServiceWithDefaultMenu(out ordersDirectoryPath);

            // Act
            Task act = service.GetOrderByRequestIDAsync("  ");

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await act);
        }

        /// <summary>
        /// Simple stub implementation of <see cref="IMenuService"/> returning a fixed menu set.
        /// Only <see cref="IMenuService.GetAllMenusAsync"/> is used by <see cref="OrderServiceFile"/>.
        /// </summary>
        private sealed class MenuServiceStub : IMenuService {
            private readonly IReadOnlyList<FoodStackMenu> menus;

            /// <summary>
            /// Initializes a new instance of <see cref="MenuServiceStub"/> with the given menus.
            /// </summary>
            /// <param name="menus">The menus to expose from this stub.</param>
            public MenuServiceStub(IReadOnlyList<FoodStackMenu> menus) {
                this.menus = menus;
            }

            /// <inheritdoc />
            public Task<IReadOnlyList<FoodStackMenu>> GetAllMenusAsync() {
                return Task.FromResult<IReadOnlyList<FoodStackMenu>>(this.menus);
            }

            /// <inheritdoc />
            public Task<FoodStackMenu?> GetMenuAsync(string menuID) {
                return Task.FromResult<FoodStackMenu?>(null);
            }

            /// <inheritdoc />
            public Task<IReadOnlyList<string>> GetMenuIDsAsync() {
                IReadOnlyList<string> ids = Array.Empty<string>();
                return Task.FromResult(ids);
            }
        }
    }
}
