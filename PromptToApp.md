Perfect ✅ I’ll prepare a **generic prompt template** you can reuse for *any* app idea.
This prompt will guide me (the assistant) to always produce the **three steps** in the same structured way:

---

## 🔹 Prompt Template

````
You are my assistant for building apps inside my SQLite + HTML desktop framework.  
I will describe an app idea in plain English (e.g., "Todo app", "Contacts app", "Inventory system").  

You must always respond in **three steps**:

Step 1: SQL Schema  
- Provide the `CREATE TABLE` statements for the required tables in SQLite.  
- Add reasonable constraints (primary key, foreign key, defaults, etc.).  
- Insert some sample records (3–5 rows per table).  

Step 2: SQL Methods  
- Provide reusable SQL queries (Methods) for each core action in the app.  
- Each method has a Name and its SQL statement.  
- Use parameters like `@paramName` wherever input is needed.  
- Cover CRUD actions (List, Insert, Update, Delete) and any other useful queries.  

Step 3: HTML + JS Page  
- Generate a full HTML page with:  
  - A form to create/update records.  
  - A table or list to display records.  
  - Buttons for Update/Delete actions.  
- The page should call the SQL Methods (from Step 2) using:  
  ```js
  window.chrome.webview.postMessage({
      Type: "runMethod",
      Name: "MethodName",
      Params: { "@param": value }
  });
````

* Add styles to make it clean (tables, inputs, buttons).
* Make sure all actions (list, insert, update, delete) reload data appropriately.

⚠️ Important:

* Do **not** put raw SQL directly in the HTML. Always call the saved Methods from Step 2.
* Keep code complete and self-contained for copy-paste.
* Follow the same format for every response.

Now I will describe the app.

```

---

## 🔹 Example Usage

**You say:**  
👉 *“I want a Contacts app with name, phone, email, and group.”*

**I will reply:**  
- **Step 1:** SQL `CREATE TABLE Contacts` + inserts.  
- **Step 2:** Methods → `ListContacts`, `AddContact`, `UpdateContact`, `DeleteContact`.  
- **Step 3:** HTML page with form + table, calling those methods.  

---

This way, no matter the app (Tasks, Inventory, Notes, CRM, etc.), you’ll always get the **same flow**.  

---

Do you want me to also add **Step 4 (Generate Code prompt)** for each method (like we did in SQL editor with “Get Code”), or should we keep it limited to Steps 1–3?
```
--Sql Query
CREATE TABLE IF NOT EXISTS Pages (
    Title TEXT PRIMARY KEY,
    Content TEXT
);
