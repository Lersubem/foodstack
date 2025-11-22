# Full Stack Developer Assignment

## Introduction

The goal of this assignment is not to see how much code you can produce in a few hours, but to understand how you think, structure your code, and reason about architecture.

You’ll implement a small full-stack prototype consisting of:

1. A lightweight backend built with C# and .NET (8 or newer).
1. A simple frontend built with React, Next.js and TypeScript.

---

We’re primarily interested in clarity, design choices, and how you reason about scalability and maintainability, rather than the number of features.

Good luck and thank you for taking the time to complete this technical exercise.

## Assignment

You’ll build a simplified version of a food-ordering platform.

### Functional Requirements

1. As a user, I can scroll through a basic menu with meals.
1. As a user, I can order one or more meals.

### Expected Behavior

1. The backend exposes the menu data to the frontend.
1. The frontend fetches and displays the list of meals.
1. When the user clicks a meal, they can increment or decrement the order quantity using plus/minus buttons.
1. The current order amount is displayed and updated immediately in the UI.
1. When pressing the “Order” button, the frontend sends the selected meal ID and quantity to the backend.

### Considerations

1. Assume that the menu would be heavily cached by something like a CDN.
1. The functionality to place orders could experience high load during peak hours (e.g., evening).
1. Both parts should remain available and responsive under these conditions.
1. For this exercise, you can store data in-memory, but design your code so that it could be replaced with a database or external service later.

### Technical Guidelines

1. Use C# and .NET 8 or newer for the backend.
1. Include at least one unit test on the backend to demonstrate how you structure and write tests.
1. Use React with Next.js and TypeScript for the frontend.
1. Include a README file that briefly describes:
    1. Your implementation approach and reasoning.
    1. How to run the application locally.