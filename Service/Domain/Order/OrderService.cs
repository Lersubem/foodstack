namespace FoodStack.Domain.Order {

    public interface IOrderService {
        Task<FoodStackOrder?> GetOrderByOrderIDAsync(string orderID);
        Task<FoodStackOrder?> GetOrderByRequestIDAsync(string requestID);
        OrderPlacementResultErrorParametersValidation? OrderValidateRequestParameters(FoodStackOrderRequest request);
        Task<OrderPlacementResultErrorDuplication?> OrderValidateRequestDuplicationAsync(FoodStackOrderRequest request);
        Task<OrderPlacementResult> OrderPlaceAsync(FoodStackOrderRequest request);
    }
}
