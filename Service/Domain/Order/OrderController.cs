// OrdersController.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FoodStack.Domain.Order {
    /// <summary>
    /// Provides endpoints for placing and retrieving orders.
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase {
        private readonly IOrderService orderService;
        private readonly ILogger<OrdersController> logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger) {
            this.orderService = orderService;
            this.logger = logger;
        }

        /// <summary>
        /// Places a new order or returns an existing order for the same requestID.
        /// </summary>
        /// <param name="request">Order request payload.</param>
        /// <returns>Unified result with status and details.</returns>
        /// <response code="200">Order placed, or existing order returned.</response>
        /// <response code="400">Invalid request payload or invalid meals.</response>
        /// <response code="409">RequestID already used for a different order.</response>
        /// <response code="500">Unexpected error.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderPlacementResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(OrderPlacementResult))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(OrderPlacementResultErrorDuplication))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderPlacementResult>> PlaceOrder([FromBody] FoodStackOrderRequest request) {
            try {
                OrderPlacementResultErrorParametersValidation? parametersResult = this.orderService.OrderValidateRequestParameters(request);

                if (parametersResult != null) {
                    return this.BadRequest(parametersResult);
                }

                OrderPlacementResultErrorDuplication? duplicationResult = await this.orderService.OrderValidateRequestDuplicationAsync(request);

                if (duplicationResult != null && duplicationResult.HasExistingOrder) {
                    if (duplicationResult.IsConflict) {
                        return this.Conflict(duplicationResult);
                    } else {
                        return this.Ok(duplicationResult);
                    }
                }

                OrderPlacementResult placeResult = await this.orderService.OrderPlaceAsync(request);

                if (placeResult is OrderPlacementResultSuccess) {
                    return this.Ok(placeResult);
                } else if (placeResult is OrderPlacementResultErrorMealNotValid) {
                    return this.BadRequest(placeResult);
                } else {
                    return this.BadRequest(placeResult);
                }
            } catch (Exception exception) {
                this.logger.LogError(exception, "Unexpected error while placing order.");
                throw;
            }
        }

        /// <summary>
        /// Gets an order by its orderID.
        /// </summary>
        /// <param name="orderID">The order identifier.</param>
        /// <returns>The requested order.</returns>
        /// <response code="200">Returns the requested order.</response>
        /// <response code="400">orderID is missing or invalid.</response>
        /// <response code="404">Order not found.</response>
        /// <response code="500">Unexpected error.</response>
        [HttpGet("{orderID}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FoodStackOrder))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FoodStackOrder>> GetOrderByOrderID(string orderID) {
            try {
                if (string.IsNullOrWhiteSpace(orderID)) {
                    return this.BadRequest("orderID is required.");
                }

                FoodStackOrder? order = await this.orderService.GetOrderByOrderIDAsync(orderID);

                if (order == null) {
                    return this.NotFound();
                }

                return this.Ok(order);
            } catch (Exception exception) {
                this.logger.LogError(exception, "Error while getting order {OrderID}.", orderID);
                throw;
            }
        }

        /// <summary>
        /// Gets an order by its requestID.
        /// </summary>
        /// <param name="requestID">The request identifier provided by the client.</param>
        /// <returns>The requested order.</returns>
        /// <response code="200">Returns the requested order.</response>
        /// <response code="400">requestID is missing or invalid.</response>
        /// <response code="404">Order not found.</response>
        /// <response code="500">Unexpected error.</response>
        [HttpGet("by-request/{requestID}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FoodStackOrder))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FoodStackOrder>> GetOrderByRequestID(string requestID) {
            try {
                if (string.IsNullOrWhiteSpace(requestID)) {
                    return this.BadRequest("requestID is required.");
                }

                FoodStackOrder? order = await this.orderService.GetOrderByRequestIDAsync(requestID);

                if (order == null) {
                    return this.NotFound();
                }

                return this.Ok(order);
            } catch (Exception exception) {
                this.logger.LogError(exception, "Error while getting order by requestID {RequestID}.", requestID);
                throw;
            }
        }
    }
}
