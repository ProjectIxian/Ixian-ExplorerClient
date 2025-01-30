# Ixian Explorer API Documentation

## Overview

The **Ixian Explorer API** provides a RESTful interface for interacting with the Ixian Explorer. The API supports retrieving details about the blockchain network, blocks, transactions, and addresses.

- **Base URL:** `https://explorer.ixian.io/api/v1`
- **Version:** `1.0.1`
- **Contact:** [contact@ixilabs.com](mailto:contact@ixilabs.com)
- **External Documentation:** [Find out more about Ixian](http://ixian.io)

## Authentication

The API uses an API key for authentication. Include the API key in the header of your request as follows:

```
API-KEY: your_api_key
```

---

## Endpoints

### 1. **Get Network Status**
Retrieve the current network status, including block height, hash rate, and node statistics.

- **Endpoint:** `GET /network/status`
- **Security:** Requires `API-KEY`
- **Response:**
  - **200 OK**: Returns network status details.
  - **500 Internal Server Error**: Failed to retrieve network status.

#### Example Response:
```json
{
  "blockheight": 4043166,
  "date": "2024-03-13 21:12:31",
  "totalixi": "9982084855",
  "hashrate": 37463198,
  "difficulty": "18446206245528829067",
  "blockratio": 50.17,
  "nodes_m": 266,
  "nodes_r": 5,
  "nodes_c": 8
}
```

---

### 2. **Get Block by Height**
Retrieve detailed information about a specific block.

- **Endpoint:** `GET /blocks/{height}`
- **Parameters:**
  - `height` (required, integer): The height of the block.
- **Security:** Requires `API-KEY`
- **Response:**
  - **200 OK**: Returns block details.
  - **404 Not Found**: Block not found.

#### Example Response:
```json
{
  "id": 4043144,
  "blockChecksum": "6cfd990d6fcc6ee77d2f17db3fe6f56eea62d2146b6e86a1abe7873e4057a1cd...",
  "lastBlockChecksum": "183ce965201f904c5c2021cdc868a9f0c5ab096070032c8dd1ef0175a712b6ee...",
  "wsChecksum": "55d0bea00df7aaf786f957ec6ef46a3916d79bd591a90260b5acd97a05f80ea2...",
  "difficulty": "18445768702423840888",
  "timestamp": "1710356483",
  "txCount": 29
}
```

---

### 3. **Get Latest Block**
Retrieve detailed information about the latest block.

- **Endpoint:** `GET /blocks/latest`
- **Security:** Requires `API-KEY`
- **Response:**
  - **200 OK**: Returns the latest block details.
  - **500 Internal Server Error**: Failed to retrieve the latest block.

---

### 4. **Get Transaction by ID**
Retrieve detailed information about a specific transaction.

- **Endpoint:** `GET /transactions/{tx_id}`
- **Parameters:**
  - `tx_id` (required, string): The transaction ID.
- **Security:** Requires `API-KEY`
- **Response:**
  - **200 OK**: Returns transaction details.
  - **404 Not Found**: Transaction not found.

---

### 5. **Get Address Details**
Retrieve details about a specific blockchain address.

- **Endpoint:** `GET /addresses/{addr}`
- **Parameters:**
  - `addr` (required, string): The blockchain address.
- **Security:** Requires `API-KEY`
- **Response:**
  - **200 OK**: Returns address details.
  - **404 Not Found**: Address not found. 

#### Example Response:
```json
{
  "address": "162jKGsGuakWrg37TNVg8puLUavJ8WrdiTwnd3XeosZGcXNhv",
  "amount": "48955615.21703791",
  "lastblock": 2120108,
  "txcount": 107
}
```

---

### 6. **Get Transactions for an Address**
Retrieve a list of transactions associated with a specific address.

- **Endpoint:** `GET /addresses/{addr}/transactions`
- **Parameters:**
  - `addr` (required, string): Blockchain address.
  - `limit` (optional, integer): Number of transactions per page (default: 100).
  - `page` (optional, integer): Page number (default: 1).
  - `sort` (optional, string): Sorting order (`asc` or `desc`, default: `asc`).
- **Security:** Requires `API-KEY`
- **Response:**
  - **200 OK**: Returns a list of transactions.
  - **404 Not Found**: No transactions found.

---

### 7. **Get Recent Transactions for an Address**
Retrieve the most recent transactions for an address.

- **Endpoint:** `GET /addresses/{addr}/recent`
- **Parameters:**
  - `addr` (required, string): Blockchain address.
  - `limit` (optional, integer): Number of transactions to retrieve (default: 100).
- **Security:** Requires `API-KEY`
- **Response:**
  - **200 OK**: Returns recent transactions.
  - **404 Not Found**: No transactions found.

---

### 8. **Get Address Updates Since Last Transaction**
Retrieve transactions for an address that occurred after a specified transaction ID.

- **Endpoint:** `GET /addresses/{addr}/updates`
- **Parameters:**
  - `addr` (required, string): Blockchain address.
  - `lastTx` (required, string): The transaction ID after which updates should be fetched.
  - `limit` (optional, integer): Number of transactions to retrieve (default: 100).
- **Security:** Requires `API-KEY`
- **Response:**
  - **200 OK**: Returns transactions since the specified transaction ID.
  - **404 Not Found**: No transactions found.

---

## Error Codes

- **200**: Request was successful.
- **404**: Data was not found. The response includes an error message in the following format:

```json
{
  "error": "No transactions found"
}
```

- **500**: Server encountered an error. The response includes an error message in the following format:

```json
{
  "error": "Failed to retrieve data"
}
```

---

## External Documentation

For more information about Ixian, visit the [Ixian website](http://ixian.io).