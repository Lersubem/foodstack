# FoodStack – Full Stack Developer Assignment

A small full-stack prototype of a food-ordering platform built with:

- Backend: C# / .NET 8 (ASP.NET Core)
- Frontend: React, Next.js (App Router) and TypeScript

## Table of Contents

- [1. Overview](#1-overview)
- [2. Backend](#2-backend)
  - [2.1 Technologies & Architecture](#21-technologies--architecture)
  - [2.2 Replaceable infrastructure](#replaceable-infrastructure)
  - [2.3 Domain Models & API Design](#22-domain-models--api-design)
  - [2.4 Endpoints](#23-endpoints)
  - [2.5 High-Load / Degraded Behavior Simulation](#24-high-load--degraded-behavior-simulation)
- [3. Frontend](#3-frontend)
  - [3.1 Technologies & Structure](#31-technologies--structure)
  - [3.2 Routes, Data Adapters & Cart](#32-routes-data-adapters--cart)
  - [3.3 Extra Functionalities](#33-extra-functionalities)
  - [3.4 High Load & Resilience (End-to-End)](#34-high-load--resilience-end-to-end)
- [4. Testing](#4-testing)
- [5. Live Preview](#5-live-preview)
- [6. Running the Application Locally](#6-running-the-application-locally)

---

## 1. Overview

User flow:

- Users can browse a menu grouped by menu (e.g. "Baguette", "Doritos").
- Users can add meals to a cart, adjust quantities, and place an order.
- After ordering, they see an order confirmation screen with a breakdown and total.

High-level behavior:

- Backend exposes menu and order endpoints.
- Frontend fetches menus, manages cart state, and posts orders.
- Backend includes simulated high-load behavior for POST endpoints.
- Frontend includes UI handling for slow / flaky responses.

A live version of both backend and frontend is deployed under:

- Frontend: https://madengines.xyz/foodstack
- Backend API (via PathBase): https://madengines.xyz/foodstack/service/api/...

This allows testing the solution without running anything locally.

---

## 2. Backend

### 2.1 Technologies & Architecture

- ASP.NET Core 8 Web API.

### 2.2 Replaceable infrastructure

The backend is built around service interfaces:

- IMenuService
  - GetAllMenusAsync()
  - GetMenuAsync(menuID)
  - GetMenuIDsAsync()
- IOrderService
  - OrderValidateRequestParameters(...)
  - OrderValidateRequestDuplicationAsync(...)
  - OrderPlaceAsync(...)
  - GetOrderByOrderIDAsync(orderID)
  - GetOrderByRequestIDAsync(requestID)

Current implementations (MenuServiceFile, OrderServiceFile) use JSON files on disk, but the controllers depend only on the interfaces. Swapping to a real database or external service does not require controller changes.

### 2.3 Domain Models & API Design

#### Domain models

Menus

- FoodStackMenu
  - MenuID: string
  - MenuName: string
  - Meals: List<FoodStackMeal>

- FoodStackMeal
  - ID: string
  - Name: string
  - Category: string
  - ImageUrl: string
  - Price: decimal

Orders

- FoodStackOrder
  - OrderID: string
  - OrderTime: DateTimeOffset
  - Request: FoodStackOrderRequest

- FoodStackOrderRequest
  - RequestID: string (idempotency key from frontend)
  - Meals: List<FoodStackOrderRequestItem>

- FoodStackOrderRequestItem
  - MealID: string
  - Quantity: int

#### Unified order result – OrderPlacementResult

Order placement uses a discriminated union style result:

Base class: OrderPlacementResult

- Status: string (abstract – discriminator)
- Message: string?

Implementations:

- OrderPlacementResultSuccess
  - Status = "Success"
  - Order: FoodStackOrder
- OrderPlacementResultErrorParametersValidation
  - Status = "InvalidOrderRequest"
  - Errors: IReadOnlyList<OrderPlacementResultErrorParametersValidationItem>
    - Each item has Code, Message, optional MealID.
- OrderPlacementResultErrorDuplication
  - HasExistingOrder: bool
  - IsConflict: bool
  - ExistingOrder: FoodStackOrder?
  - Status:
    - "ExistingOrder" when requestID already used and payload matches.
    - "OrderConflict" when requestID clashes with different payload.
- OrderPlacementResultErrorMealNotValid
  - Status = "MealNotValid"
  - InvalidMeals: IReadOnlyList<string>

### 2.4 Endpoints

In production all endpoints are served under the PathBase `/foodstack/service`, so the full path becomes `/foodstack/service/api/...`.

#### Menu endpoints – MenuController

Base route: api/menu

- GET /api/menu
  - Returns: IReadOnlyList<FoodStackMenu>.
  - 200: all menus with their meals.
- GET /api/menu/{menuID}
  - Returns: single FoodStackMenu.
  - 200: menu found.
  - 400: missing/invalid menuID.
  - 404: menu not found.
- GET /api/menu/ids
  - Returns: IReadOnlyList<string> of all menu IDs.

All read operations delegate to IMenuService.

#### Order endpoints – OrdersController

Base route: api/orders

- POST /api/orders
  - Body: FoodStackOrderRequest.
  - Returns: OrderPlacementResult.
  - Behaviors:
    - Valid request → OrderPlacementResultSuccess (200).
    - Invalid parameters → OrderPlacementResultErrorParametersValidation (400).
    - Duplicate requestID:
      - Same payload → OrderPlacementResultErrorDuplication with HasExistingOrder=true, IsConflict=false, ExistingOrder (200).
      - Conflicting payload → OrderPlacementResultErrorDuplication with IsConflict=true (409).
    - Invalid meals → OrderPlacementResultErrorMealNotValid (400) with invalid meal IDs listed.

- GET /api/orders/{orderID}
  - Returns: FoodStackOrder.
  - 200: order found.
  - 400: invalid orderID.
  - 404: not found.

- GET /api/orders/by-request/{requestID}
  - Returns: FoodStackOrder found by client requestID.
  - 200/400/404 same pattern as above.

#### Exception handling

ExceptionHandlingMiddleware ensures:

- All unhandled exceptions are logged.
- In production:
  - Clients get a generic JSON payload:
    - status: 500
    - error: "InternalServerError"
    - message: "An unexpected error occurred."
- In development:
  - Response content-type is text/plain with full exception text.

This keeps diagnostics rich internally but does not expose sensitive details externally.

### 2.5 High-Load / Degraded Behavior Simulation

RequestSimulationMiddleware is added early in the pipeline and is central to exploring the assignment’s “high-load” aspect.

It applies only to:

- HTTP POST requests.
- Paths under `/api` (after PathBase).

For each matching request, it randomly picks one of these scenarios:

1. Queue / delayed processing (about 35%)
   - Simulates a request waiting in a queue.
   - Random delay up to 30 seconds, between half and full MaxQueueDelay.
   - Then forwards to the next middleware (normal processing afterwards).

2. Partial response and abort (about 35%)
   - Writes a small JSON fragment (status "partial", message "Simulated dropped connection") to the response body.
   - Flushes it.
   - Immediately calls context.Abort().
   - Simulates a mid-stream connection drop or proxy abort.

3. Normal behavior (remaining ~30%)
   - Simply calls the next middleware and returns the real response without simulation.

This middleware is intentionally mentioned in both the architecture section and here to make sure the reviewer sees the explicit handling of high-load and bad-network scenarios.

---

## 3. Frontend

### 3.1 Technologies & Structure

Frontend app

- Next.js App Router with TypeScript and strict typing.
- CSS Modules with SCSS for styling and dynamic theming.

### 3.2 Routes, Data Adapters & Cart

Main routes:

- `/` (Menu page)
  - Loads menu IDs from the backend.
  - Asynchronously fetches each menu and renders grouped meals per menu.
  - Manages cart state:
    - Click meal to add.
    - Plus/minus buttons to adjust quantity.
  - Displays total and triggers order placement.
  - Shows high-load overlay when placing an order.

- `/order` (Order confirmation)
  - On load:
    - Tries to read last order from localStorage (key "lastOrder").
    - If not present:
      - Checks URL query for `orderID`.
      - If present:
        - Fetches order from GET /api/orders/{orderID}.
        - If not found or invalid, redirects back to `/`.
      - If no orderID: redirects back to `/`.
  - Shows order ID, time, item breakdown, and total.

#### Data adapters

API calls are wrapped in small adapter functions to keep components clean:

- Adapter base:

  - apiBaseUrl: development vs production base.
  - apiUrl(path): combines base with path.
  - apiGetJson<T>(path): fetch + JSON parsing with error handling.

- menu.adapter.ts:
  - getMenuIDs(): GET /api/menu/ids.
  - getMenu(menuID): GET /api/menu/{menuID}.

- order.adapter.ts:
  - placeOrder(request: OrderRequest): POST /api/orders.
  - getOrderByOrderID(orderID): GET /api/orders/{orderID}.

The frontend is configured so that in development it calls the local backend, and in production it targets the hosted backend under `/foodstack/service`.

#### Cart behavior (CartProvider / useCart)

Cart logic is encapsulated in a separate file:

- CartItem:
  - id, name, price, quantity.

- CartContext value:
  - items: CartItem[]
  - totalQuantity: number
  - addMeal(meal: MenuMeal): add or increment.
  - increment(mealID: string).
  - decrement(mealID: string) with removal when quantity hits zero.
  - clear(): empties the cart.

MenuPageClient consumes useCart() to connect UI elements (Add / + / - / Remove) to the central cart state.

### 3.3 Extra Functionalities

These are not required by the assignment but added to show extensibility and UX care.

#### 3.3.1 Cart popup

- Clicking “Items in cart” opens a popup under the header.
- Displayed per cart item

#### 3.3.2 Theme picker

- A theme button in the footer opens a theme popup.
- Palettes are defined as:

  - name: string
  - accentHex: string
  - highlightHex: string
  - backgroundHex: string

#### 3.3.3 Responsive layout

- The UI is fully responsive and works on mobile phones, tablets, and desktop screens.
- The menu list collapses into smaller viewports for easy scrolling.
- Cart and theme popups adapt to viewport height and remain usable on touch devices.

---

### 3.4 High Load & Resilience (End-to-End)

Backend and frontend are wired together to explicitly demonstrate resiliency under load.

#### Backend

- `RequestSimulationMiddleware` randomly delays POST `/api` calls or aborts them mid-response.

#### Frontend

- A full-screen submit overlay and global `isLoading` styling indicate long-running requests and high-load states (including a delayed “high load” message).
- Order placement has a capped retry loop so transient failures result in a clear error instead of a half-broken UI.

Together with the simulated middleware, this gives a complete, observable high-load story.

## 4. Testing

### Backend unit tests

- Project: FoodStack Service - Test
  - Contains unit tests focusing on:
    - Order parameter validation.
    - Meal validation (invalid meals).
    - Idempotent behavior with requestID.
    - Core service logic separate from controllers and HTTP concerns.

### Running tests

From the solution root (or the test project folder):

    dotnet test

Or, if needed:

    dotnet test .\tests\FoodStack.Service.Test\FoodStack.Service.Test.csproj

---

## 5. Live Preview

A live deployment is available at:

- Frontend base: https://madengines.xyz/foodstack
- Backend APIs: https://madengines.xyz/foodstack/service/api/...

You can:

- Browse menus.
- Add/remove meals in the cart.
- Change themes.
- Place orders and experience simulated high-load behavior.
- Reload /foodstack/order to see confirmation behavior using localStorage and the orderID query parameter.

---

## 6. Running the Application Locally

### Prerequisites

- .NET SDK 8.0+.
- Node.js (LTS) and npm.

Assumed structure:

- FoodStack.Server/ – backend.
- FoodStack.Client/ – frontend (Next.js).

### Backend (API)

    cd FoodStack.Server
    dotnet restore
    dotnet build
    dotnet run

- The app starts on a local port (e.g. http://localhost:5124)
- Swagger (development only) is available at:

    http://localhost:5124/swagger

### Frontend (Next.js)

    cd FoodStack.Client
    npm install
    npm run dev

- Dev server: http://localhost:3000
- The frontend adapters are configured so that in development they target the local backend, and in production they target `/foodstack/service/api/...` on the same host.