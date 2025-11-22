using FoodStack.Domain.Menu;
using System.Text.Json;

namespace FoodStack.Domain.Order {

    public class OrderServiceFile : IOrderService {
        private readonly string ordersDirectoryPath;
        private readonly IMenuService menuService;
        private readonly JsonSerializerOptions jsonOptions;

        public OrderServiceFile(string ordersDirectoryPath, IMenuService menuService) {
            if (string.IsNullOrWhiteSpace(ordersDirectoryPath)) {
                throw new ArgumentException("ordersDirectoryPath is required.", nameof(ordersDirectoryPath));
            }

            if (menuService == null) {
                throw new ArgumentNullException(nameof(menuService));
            }

            this.ordersDirectoryPath = ordersDirectoryPath;
            this.menuService = menuService;
            this.jsonOptions = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        public async Task<FoodStackOrder?> GetOrderByOrderIDAsync(string orderID) {
            try {
                if (string.IsNullOrWhiteSpace(orderID)) {
                    throw new ArgumentException("orderID is required.", nameof(orderID));
                }

                string filePath = this.GetOrderFilePath(orderID);

                if (File.Exists(filePath) == false) {
                    return null;
                }

                string json = await File.ReadAllTextAsync(filePath);
                FoodStackOrder? order = JsonSerializer.Deserialize<FoodStackOrder>(json, this.jsonOptions);

                return order;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        public async Task<FoodStackOrder?> GetOrderByRequestIDAsync(string requestID) {
            try {
                if (string.IsNullOrWhiteSpace(requestID)) {
                    throw new ArgumentException("requestID is required.", nameof(requestID));
                }

                FoodStackOrder? order = await this.InternalGetOrderByRequestIDAsync(requestID);

                return order;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        public OrderPlacementResultErrorParametersValidation? OrderValidateRequestParameters(FoodStackOrderRequest request) {
            try {
                List<OrderPlacementResultErrorParametersValidationItem> errors = new List<OrderPlacementResultErrorParametersValidationItem>();
                int nonZeroCount = 0;

                if (request == null) {
                    OrderPlacementResultErrorParametersValidationItem error = new OrderPlacementResultErrorParametersValidationItem();
                    error.Code = "NullRequest";
                    error.Message = "Request body is required.";
                    error.MealID = null;
                    errors.Add(error);
                } else {
                    if (string.IsNullOrWhiteSpace(request.RequestID)) {
                        OrderPlacementResultErrorParametersValidationItem error = new OrderPlacementResultErrorParametersValidationItem();
                        error.Code = "MissingRequestID";
                        error.Message = "requestID is required.";
                        error.MealID = null;
                        errors.Add(error);
                    }

                    if (request.Meals == null || request.Meals.Count <= 0) {
                        OrderPlacementResultErrorParametersValidationItem error = new OrderPlacementResultErrorParametersValidationItem();
                        error.Code = "NoMeals";
                        error.Message = "Order must contain at least one meal.";
                        error.MealID = null;
                        errors.Add(error);
                    } else {
                        foreach (FoodStackOrderRequestItem item in request.Meals) {
                            if (item == null) {
                                OrderPlacementResultErrorParametersValidationItem error = new OrderPlacementResultErrorParametersValidationItem();
                                error.Code = "NullMealItem";
                                error.Message = "Meal item cannot be null.";
                                error.MealID = null;
                                errors.Add(error);
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(item.MealID)) {
                                OrderPlacementResultErrorParametersValidationItem error = new OrderPlacementResultErrorParametersValidationItem();
                                error.Code = "MissingMealID";
                                error.Message = "MealID is required.";
                                error.MealID = null;
                                errors.Add(error);
                            }

                            if (item.Quantity < 0) {
                                OrderPlacementResultErrorParametersValidationItem error = new OrderPlacementResultErrorParametersValidationItem();
                                error.Code = "QuantityNegative";
                                error.Message = "Quantity cannot be negative.";
                                error.MealID = item.MealID;
                                errors.Add(error);
                            }

                            if (item.Quantity > 999) {
                                OrderPlacementResultErrorParametersValidationItem error = new OrderPlacementResultErrorParametersValidationItem();
                                error.Code = "QuantityTooHigh";
                                error.Message = "Quantity cannot be greater than 999.";
                                error.MealID = item.MealID;
                                errors.Add(error);
                            }

                            if (item.Quantity > 0) {
                                nonZeroCount++;
                            }
                        }
                    }
                }

                if (errors.Count == 0 && nonZeroCount <= 0) {
                    OrderPlacementResultErrorParametersValidationItem error = new OrderPlacementResultErrorParametersValidationItem();
                    error.Code = "AllZeroQuantity";
                    error.Message = "At least one meal must have quantity greater than zero.";
                    error.MealID = null;
                    errors.Add(error);
                }

                if (errors.Count <= 0) {
                    return null;
                }

                OrderPlacementResultErrorParametersValidation result = new OrderPlacementResultErrorParametersValidation();
                result.Message = "Order request is invalid.";
                result.Errors = errors;

                return result;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        public async Task<OrderPlacementResultErrorDuplication?> OrderValidateRequestDuplicationAsync(FoodStackOrderRequest request) {
            try {
                if (request == null) {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(request.RequestID)) {
                    return null;
                }

                FoodStackOrder? existing = await this.InternalGetOrderByRequestIDAsync(request.RequestID);

                if (existing == null) {
                    return null;
                }

                OrderPlacementResultErrorDuplication result = new OrderPlacementResultErrorDuplication();
                result.HasExistingOrder = true;
                result.ExistingOrder = existing;

                bool equivalent = this.AreRequestsEquivalent(request, existing.Request);

                if (equivalent) {
                    result.IsConflict = false;
                    result.Message = "Existing order for this requestID.";
                } else {
                    result.IsConflict = true;
                    result.Message = "An order with this requestID already exists with different content.";
                }

                return result;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        public async Task<OrderPlacementResult> OrderPlaceAsync(FoodStackOrderRequest request) {
            try {
                if (request == null) {
                    OrderPlacementResultErrorParametersValidation invalidResult = new OrderPlacementResultErrorParametersValidation();
                    invalidResult.Message = "Order request is invalid.";
                    return invalidResult;
                }

                IReadOnlyList<FoodStackMenu> menus = await this.menuService.GetAllMenusAsync();

                Dictionary<string, FoodStackMeal> allMeals = new Dictionary<string, FoodStackMeal>(StringComparer.OrdinalIgnoreCase);

                foreach (FoodStackMenu menu in menus) {
                    if (menu == null) {
                        continue;
                    }

                    if (menu.Meals == null) {
                        continue;
                    }

                    foreach (FoodStackMeal meal in menu.Meals) {
                        if (meal == null) {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(meal.ID)) {
                            continue;
                        }

                        if (allMeals.ContainsKey(meal.ID) == false) {
                            allMeals[meal.ID] = meal;
                        }
                    }
                }

                List<string> invalidMeals = new List<string>();

                if (request.Meals != null) {
                    foreach (FoodStackOrderRequestItem item in request.Meals) {
                        if (item == null) {
                            continue;
                        }

                        if (item.Quantity <= 0) {
                            continue;
                        }

                        if (allMeals.ContainsKey(item.MealID) == false) {
                            if (invalidMeals.Contains(item.MealID) == false) {
                                invalidMeals.Add(item.MealID);
                            }
                        }
                    }
                }

                if (invalidMeals.Count > 0) {
                    OrderPlacementResultErrorMealNotValid invalidResult = new OrderPlacementResultErrorMealNotValid();
                    invalidResult.InvalidMeals = invalidMeals;
                    invalidResult.Message = "One or more requested meals do not exist.";
                    return invalidResult;
                }

                FoodStackOrder order = this.CreateOrderFromRequest(request);
                await this.SaveOrderAsync(order);

                OrderPlacementResultSuccess successResult = new OrderPlacementResultSuccess();
                successResult.Order = order;
                successResult.Message = "Order placed successfully.";

                return successResult;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        private async Task<FoodStackOrder?> InternalGetOrderByRequestIDAsync(string requestID) {
            try {
                if (Directory.Exists(this.ordersDirectoryPath) == false) {
                    return null;
                }

                string[] files = Directory.GetFiles(this.ordersDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in files) {
                    string json = await File.ReadAllTextAsync(file);

                    FoodStackOrder? order = JsonSerializer.Deserialize<FoodStackOrder>(json, this.jsonOptions);

                    if (order == null) {
                        continue;
                    }

                    if (order.Request == null) {
                        continue;
                    }

                    if (string.Equals(order.Request.RequestID, requestID, StringComparison.OrdinalIgnoreCase)) {
                        return order;
                    }
                }

                return null;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        private bool AreRequestsEquivalent(FoodStackOrderRequest left, FoodStackOrderRequest right) {
            try {
                if (left == null || right == null) {
                    return false;
                }

                Dictionary<string, int> leftItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, int> rightItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                if (left.Meals != null) {
                    foreach (FoodStackOrderRequestItem item in left.Meals) {
                        if (item == null) {
                            continue;
                        }

                        if (item.Quantity <= 0) {
                            continue;
                        }

                        leftItems[item.MealID] = item.Quantity;
                    }
                }

                if (right.Meals != null) {
                    foreach (FoodStackOrderRequestItem item in right.Meals) {
                        if (item == null) {
                            continue;
                        }

                        if (item.Quantity <= 0) {
                            continue;
                        }

                        rightItems[item.MealID] = item.Quantity;
                    }
                }

                if (leftItems.Count != rightItems.Count) {
                    return false;
                }

                foreach (KeyValuePair<string, int> pair in leftItems) {
                    if (rightItems.TryGetValue(pair.Key, out int quantity) == false) {
                        return false;
                    }

                    if (quantity != pair.Value) {
                        return false;
                    }
                }

                return true;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        private FoodStackOrder CreateOrderFromRequest(FoodStackOrderRequest request) {
            try {
                FoodStackOrderRequest filteredRequest = new FoodStackOrderRequest();
                filteredRequest.RequestID = request.RequestID;

                if (request.Meals != null) {
                    foreach (FoodStackOrderRequestItem item in request.Meals) {
                        if (item == null) {
                            continue;
                        }

                        if (item.Quantity <= 0) {
                            continue;
                        }

                        FoodStackOrderRequestItem copy = new FoodStackOrderRequestItem();
                        copy.MealID = item.MealID;
                        copy.Quantity = item.Quantity;

                        filteredRequest.Meals.Add(copy);
                    }
                }

                FoodStackOrder order = new FoodStackOrder();
                order.OrderID = Guid.NewGuid().ToString("N");
                order.OrderTime = DateTimeOffset.UtcNow;
                order.Request = filteredRequest;

                return order;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        private async Task SaveOrderAsync(FoodStackOrder order) {
            try {
                if (order == null) {
                    throw new ArgumentNullException(nameof(order));
                }

                if (Directory.Exists(this.ordersDirectoryPath) == false) {
                    Directory.CreateDirectory(this.ordersDirectoryPath);
                }

                string filePath = this.GetOrderFilePath(order.OrderID);
                string json = JsonSerializer.Serialize(order, this.jsonOptions);

                await File.WriteAllTextAsync(filePath, json);
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        private string GetOrderFilePath(string orderID) {
            string fileName = orderID + ".json";
            string filePath = Path.Combine(this.ordersDirectoryPath, fileName);
            return filePath;
        }
    }
}
