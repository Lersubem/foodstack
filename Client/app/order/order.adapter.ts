import { apiGetJson, apiUrl } from "../domain/adapter";
import type {
    Order,
    OrderPlacementResult,
    OrderRequest
} from "./order.type";

export async function placeOrder(request: OrderRequest): Promise<OrderPlacementResult> {
    const url: string = apiUrl("/api/orders");

    const response: Response = await fetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    });

    const data = (await response.json()) as OrderPlacementResult;
    return data;
}

export async function getOrderByOrderID(orderID: string): Promise<Order> {
    const order: Order = await apiGetJson<Order>("/api/orders/" + encodeURIComponent(orderID));
    return order;
}
