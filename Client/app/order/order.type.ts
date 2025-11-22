export type OrderPlacementStatus =
    | "Success"
    | "InvalidOrderRequest"
    | "ExistingOrder"
    | "OrderConflict"
    | "MealNotValid";

export type OrderRequestItem = {
    id: string;
    quantity: number;
};

export type OrderRequest = {
    requestID: string;
    meals: OrderRequestItem[];
};

export type Order = {
    orderID: string;
    orderTime: string;
    request: OrderRequest;
};

export type OrderValidationError = {
    code: string;
    message: string;
    mealID?: string | null;
};

export type OrderPlacementResultBase = {
    status: OrderPlacementStatus;
    message?: string | null;
};

export type OrderPlacementResultSuccess = OrderPlacementResultBase & {
    status: "Success";
    order: Order;
};

export type OrderPlacementResultErrorParametersValidation = OrderPlacementResultBase & {
    status: "InvalidOrderRequest";
    errors: OrderValidationError[];
};

export type OrderPlacementResultErrorDuplication = OrderPlacementResultBase & {
    status: "ExistingOrder" | "OrderConflict";
    hasExistingOrder: boolean;
    isConflict: boolean;
    existingOrder?: Order | null;
};

export type OrderPlacementResultErrorMealNotValid = OrderPlacementResultBase & {
    status: "MealNotValid";
    invalidMeals: string[];
};

export type OrderPlacementResult =
    | OrderPlacementResultSuccess
    | OrderPlacementResultErrorParametersValidation
    | OrderPlacementResultErrorDuplication
    | OrderPlacementResultErrorMealNotValid;
