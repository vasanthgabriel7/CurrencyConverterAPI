# CurrencyConverterAPI

A robust and scalable API for currency conversion and exchange rate retrieval. The API supports various endpoints to fetch the latest exchange rates, historical rates, and convert currency between different currencies.

## Table of Contents
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Setup and Installation](#setup-and-installation)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
  - [GET /api/currency/latest/{baseCurrency}](#get-apicurrencylatestbasecurrency)
  - [POST /api/currency/convert](#post-apicurrencyconvert)
  - [GET /api/currency/history](#get-apicurrencyhistory)
- [Roles and Authorization](#roles-and-authorization)
- [Testing](#testing)
- [Logging](#logging)

## Features
- **Get Latest Exchange Rates**: Retrieve the latest exchange rates for a given base currency.
- **Convert Currency**: Convert an amount from one currency to another.
- **Get Historical Exchange Rates**: Fetch historical exchange rates between two dates for a specified base currency.
- **Role-Based Access Control (RBAC)**: Only authorized users with appropriate roles can access certain endpoints.

## Prerequisites
Before setting up the API, ensure you have the following:
- .NET Core 8.0 SDK and Runtime
- An IDE (e.g., Visual Studio or Visual Studio Code)
- Postman or Swagger for testing API endpoints

## Setup and Installation
1. **Clone the Repository**:
    ```bash
    git clone https://github.com/vasanthgabriel7/CurrencyConverterAPI.git
    cd CurrencyConverterAPI
    ```

2. **Restore Dependencies**: Run the following command to restore all required packages:
    ```bash
    dotnet restore
    ```

3. **Build the Project**:
    ```bash
    dotnet build
    ```

4. **Run the API**: To run the API locally:
    ```bash
    dotnet run
    ```
    By default, the API will be available at `https://localhost:5001`.

## Authentication
This API uses JWT (JSON Web Token) for authentication.

1. **Generate Token**:
    - To obtain a token, make a POST request to `/api/auth/token` with the username and password.
    - On success, a JWT token will be returned.
    - Example:
      ```json
      {
          "token": "JWT_TOKEN"
      }
      ```

2. **Authorization Header**: Include the JWT token in the Authorization header when making requests to protected endpoints:
    ```bash
    Authorization: Bearer YOUR_JWT_TOKEN
    ```

## Endpoints

### GET /api/currency/latest/{baseCurrency}
Retrieves the latest exchange rates for a given base currency.
- **Parameters**: 
  - `baseCurrency` (string) - The base currency for which exchange rates are requested.
- **Roles**: Accessible to `User` and `Admin` roles.
- **Response**:
  - `200 OK`: Returns the exchange rates.
  - `400 BadRequest`: Invalid base currency or error in request.

### POST /api/currency/convert
Converts an amount from one currency to another.
- **Parameters**:
  - **Body**: A `ConversionRequest` object containing `Amount`, `FromCurrency`, and `ToCurrency`.
- **Roles**: Accessible to `Admin` role only.
- **Response**:
  - `200 OK`: Returns the conversion result.
  - `400 BadRequest`: Invalid conversion request.
  - `500 Internal Server Error`: Internal error during conversion.

### GET /api/currency/history
Retrieves historical exchange rates for a specified base currency between a date range.
- **Parameters**:
  - **Query Params**: `baseCurrency`, `startDate`, `endDate`, `page`, `pageSize`.
- **Roles**: Accessible to `Admin` role only.
- **Response**:
  - `200 OK`: Returns the historical exchange rates.
  - `400 BadRequest`: Invalid date range or other request errors.
  - `404 NotFound`: No data found for the given date range.
  - `500 Internal Server Error`: Internal error fetching data.

## Roles and Authorization
The API supports role-based access control (RBAC) to limit access to specific methods based on user roles:
- **User Role**:
  - Can access the `GET /api/currency/latest/{baseCurrency}` endpoint.
- **Admin Role**:
  - Can access all endpoints (`GET /api/currency/latest/{baseCurrency}`, `POST /api/currency/convert`, and `GET /api/currency/history`).

Roles are assigned via JWT token claims, which are set during token generation.

## Testing
The API is tested using unit and integration tests. You can use the following command to run the tests:
dotnet test

## Logging
The API uses Serilog for logging to capture detailed logs for requests and errors.
