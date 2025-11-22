# FoodStack

Small full-stack prototype of a food-ordering platform built with **.NET 8** and **Next.js + React + TypeScript**.

- [Assignment](./assignment.md)
- [Requirements / Scope](./requirements.md)

---

## How to Run Everything (Root)

This project has one root script that starts **both backend and frontend**.

### Prerequisites

- **.NET 8 SDK** installed
- **Node.js (LTS)** installed
- `npm` available in your PATH
- Windows (for the `.cmd` scripts)

### Command

From the repository root:

    server.cmd

`server.cmd` should:

- Start the **backend** via: `backend\server-backend.cmd`
- Start the **frontend** via: `frontend\server-frontend.cmd`

After both are running:

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5000` and/or `https://localhost:5001` (depending on `launchSettings.json`)

---

## Backend (.NET 8 API)

**Location:** `./backend`  
**Goal:** Expose menu data and accept orders, using in-memory storage but structured so a DB can be plugged in later.

### First-time Setup

    cd backend
    dotnet restore

### Start Backend (Directly)

    cd backend
    server-backend.cmd

`server-backend.cmd` will internally run something like:

    dotnet run --project FoodStack.Api

API URLs are defined in `FoodStack.Api/Properties/launchSettings.json`  
(for example `http://localhost:5000` and/or `https://localhost:5001`).

### Backend Tests

    cd backend
    dotnet test

---

## Frontend (Next.js + React + TypeScript)

**Location:** `./frontend`  
**Goal:** Show the menu, manage quantities/cart, and send orders to the backend.

### First-time Setup

    cd frontend
    npm install

### Start Frontend (Directly)

    cd frontend
    server-frontend.cmd

`server-frontend.cmd` will internally run:

    npm run dev

Frontend will be available at:

- `http://localhost:3000`

If the backend runs on a different URL/port, adjust the frontend API base URL  
(e.g. environment variable or config file inside `frontend`).

---

## Project Structure

    /server.cmd               # Root script: starts backend + frontend

    /backend
      /server-backend.cmd     # Starts .NET API (FoodStack.Api)
      FoodStack.Api.sln
      FoodStack.Api/...
      tests/...

    /frontend
      /server-frontend.cmd    # Starts Next.js dev server
      package.json
      app/...
      components/...
      styles/...

---

## What This Project Does

- **Backend**
  - `GET /api/menu` – serves a basic list of meals (menu).
  - `POST /api/orders` – accepts an order with meal IDs and quantities, generates an `orderId`.
  - In-memory repositories behind interfaces to allow future DB/external service.

- **Frontend**
  - Fetches the menu from the backend and displays a scrollable list of meals.
  - Lets the user increment/decrement quantities with plus/minus buttons.
  - Shows the current order amount and updates it immediately.
  - Sends selected meals and quantities to the backend when ordering.
