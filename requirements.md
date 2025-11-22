# Requirements

This document lists the **minimal requirements** from the assignment and the **extra features** I will implement.

---

## 1. Official Assignment Scope (Minimum)

### 1.1 Backend

- **Tech stack**
  - C# with **.NET 8** (or newer).
  - Lightweight Web API.

- **Endpoints**
  - `GET /api/menu`
    - Returns the list of meals.
    - Each meal includes at least: `id`, `name`, `description`, `price`.
    - Read-only, suitable for heavy caching (CDN-friendly).
  - `POST /api/orders`
    - Request body: `{ items: [{ mealId: string, quantity: number }] }`.
    - Backend generates an `orderId`.
    - Response body (minimum): `{ orderId: string, createdAt: string, items: [...] }`.

- **Behavior**
  - Menu data is exposed to the frontend via `GET /api/menu`.
  - Order placement is done via `POST /api/orders`.
  - Data can be stored **in-memory**, but code must be structured so a database/external service can be plugged in later (repository/service abstractions).

- **Testing**
  - At least **one unit test** on the backend.
  - Example target: order service logic (validating items, calculating totals, generating orderId, etc.).

---

### 1.2 Frontend

- **Tech stack**
  - **Next.js** with **React** and **TypeScript**.
  - App Router.
  - Default styling (CSS/SCSS, no mandatory UI framework).

- **Pages (minimum required)**
  - **Menu Page** (`/`)
    - Fetches meals from `GET /api/menu`.
    - Displays scrollable list of meals.
    - When a user clicks a meal:
      - Show **+ / −** buttons to increment/decrement quantity.
    - Show **current order amount** (sum of selected quantities) and update it immediately in the UI.
    - Show an **“Order”** button:
      - When clicked, sends the current selection `{ items: [{ mealId, quantity }] }` to `POST /api/orders`.

- **State / interaction**
  - Keep cart/order items in React state.
  - Quantities update immediately on button click.
  - Basic handling of the order request (success/failure) in the UI.

---

### 1.3 Non-Functional / Architecture

- Menu is assumed to be **heavily cached** (e.g. via CDN).
- Order placement may experience **high load during peak hours**.
- Both backend and frontend should remain **available and responsive** under those conditions (stateless endpoints, separation of read/write concerns).
- Code should be **readable**, structured, and easy to extend (e.g. to plug in real persistence).

---

## 2. Extra Scope (My Additions)

These are **not required** by the original assignment but will be implemented to show extra structure and UX.

### 2.1 Extra Frontend Pages

1. **Menu Page** (`/` or `/menu`)
   - Same as minimum requirements, plus:
     - Shows a small **cart summary** (total items and total price).
     - Includes a **“Go to cart”** button that navigates to the Cart page.

2. **Cart Page** (`/cart`)
   - Displays the current selection:
     - List of selected meals with name, price, quantity, line total.
   - Allows adjusting quantities again (**+ / −**) from the cart.
   - Shows **order total**.
   - Has a **“Place order”** button:
     - Sends `{ items: [{ mealId, quantity }] }` to `POST /api/orders`.
     - On success, redirects to `/order/confirmation?orderId=...`.

3. **Order Confirmation Page** (`/order/confirmation`)
   - Shows a confirmation message.
   - Shows **summary of the order** (orderId, items, quantities, totals).
   - Includes a **“Back to menu”** link/button.

---

### 2.2 Extra Backend Endpoints / Behavior

- **Additional endpoint**
  - `GET /api/orders/{orderId}`
    - Returns details of a specific order.
    - Used by the confirmation page to fetch the latest order data.
    - Response includes at least: `orderId`, `createdAt`, and ordered items.

---

### 2.3 Extra Architecture Decisions

- **Layering**
  - Controllers → Services → Repositories (in-memory implementation).
  - Interfaces for menu and order repositories so they can be swapped with DB implementations later.

- **DTOs / Models**
  - Separate **domain models** (e.g. `Meal`, `Order`, `OrderItem`) from **API DTOs** to keep the boundary clean.

---

### 2.4 Extra UX / Quality

- Show basic **loading** and **error** states when:
  - Fetching the menu.
  - Placing an order.
- Disable “Place order” / “Order” buttons when no items are selected.
- Basic responsive layout so the menu and cart are usable on smaller screens.

