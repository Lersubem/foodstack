# Full Stack Developer Assignment

## Introduction

The goal of this assignment is not to see how much code you can produce in a few hours, but to understand how you think, structure your code, and reason about architecture.

You’ll implement a small full-stack prototype consisting of:
- A lightweight backend built with C# and .NET (8 or newer).
- A simple frontend built with React, Next.js and TypeScript.

We’re primarily interested in clarity, design choices, and how you reason about scalability and maintainability, rather than the number of features.

Good luck and thank you for taking the time to complete this technical exercise.

---

## Assignment

You’ll build a simplified version of a food-ordering platform.

---

## Functional Requirements

- As a user, I can scroll through a basic menu with meals.
- As a user, I can order one or more meals.

---

## Expected Behavior

- The backend exposes the menu data to the frontend.
- The frontend fetches and displays the list of meals.
- When the user clicks a meal, they can increment or decrement the order quantity using plus/minus buttons.
- The current order amount is displayed and updated immediately in the UI.
- When pressing the “Order” button, the frontend sends the selected meal ID and quantity to the backend.

---

## Considerations

- Assume that the menu would be heavily cached by something like a CDN.
- The functionality to place orders could experience high load during peak hours (e.g., evening).
- Both parts should remain available and responsive under these conditions.
- For this exercise, you can store data in-memory, but design your code so that it could be replaced with a database or external service later.

---

## Technical Guidelines

- Use C# and .NET 8 or newer for the backend.
- Include at least one unit test on the backend to demonstrate how you structure and write tests.
- Use React with Next.js and TypeScript for the frontend.
- Include a README file that briefly describes:
  - Your implementation approach and reasoning.
  - How to run the application locally.
