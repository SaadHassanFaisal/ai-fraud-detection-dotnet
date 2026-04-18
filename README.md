# Financial Intelligence Platform

**Enterprise-Grade Financial Analytics & Fraud Detection System**

[cite_start]AI Financial System is a portfolio-grade desktop application [cite: 1] [cite_start]simulating a mini banking intelligence platform[cite: 1]. [cite_start]Designed for academic evaluators and technical recruiters [cite: 1][cite_start], this capstone project demonstrates a strict layered architecture, hybrid data access strategies, and real-time machine learning integration[cite: 1]. 

[cite_start]The core objective of this system is to bridge the gap between application development and data science, showcasing how to efficiently route ACID-compliant transactions while synchronously evaluating them against an AI model[cite: 1].

---

## 🏗️ System Architecture

[cite_start]The platform strictly adheres to a **3-Layer Separation of Concerns** (Presentation → Business Logic → Data Access) ensuring that no layer skipping occurs[cite: 1]. 

* [cite_start]**Presentation Layer (WinForms):** Contains zero business logic and zero SQL[cite: 1]. [cite_start]All data operations and form behaviors are orchestrated through injected services[cite: 1].
* [cite_start]**Business Logic Layer (BLL):** Orchestrates operations, validates all inputs before they reach the database, and manages external microservice communications[cite: 1].
* [cite_start]**Data Access Layer (DAL):** Implements the Repository Pattern (`IRepository<T>`) to decouple the BLL from specific data access technologies [cite: 1][cite_start], allowing for highly modular infrastructure[cite: 1].

[cite_start]The entire application lifecycle is managed via `Microsoft.Extensions.DependencyInjection` [cite: 1][cite_start], ensuring components are injected via constructors rather than instantiated locally[cite: 1]. 

---

## 🧠 The Dual Data-Access Strategy

[cite_start]A standout technical implementation of Aegis Intelligence is the deliberate, situational separation of data-access tools[cite: 2]. [cite_start]The system utilizes both Entity Framework (EF) Core and raw ADO.NET, justifying the choice of each based on performance and data integrity requirements[cite: 1, 2].

### Entity Framework Core (Domain & State Management)
[cite_start]EF Core is utilized where its unit-of-work pattern and change tracking provide the most value[cite: 1, 2]. 
* [cite_start]**Schema Management:** Handles database generation and schema versioning via Code First Migrations[cite: 1].
* [cite_start]**Standard CRUD:** Manages user profiles, categories, and account creation[cite: 1].
* [cite_start]**Relational Mapping:** Leverages Fluent API to configure complex `1:many` and `many:1` entity relationships[cite: 1].

### ADO.NET (Performance & Critical Paths)
[cite_start]Where ORMs introduce limitations, the system drops down to raw ADO.NET to guarantee performance and ACID compliance[cite: 1].
* [cite_start]**Atomic Escrow Operations:** Because EF does not expose `SqlTransaction` directly, financial transfers are routed through ADO.NET[cite: 1]. [cite_start]This ensures that a debit and credit occur as a single, atomic operation with explicit rollback guarantees on failure[cite: 1].
* [cite_start]**Analytic Aggregations:** Complex reporting operations are pushed directly to the SQL Server via Stored Procedures[cite: 1]. [cite_start]This avoids the highly inefficient SQL generation and N+1 evaluation issues commonly associated with complex LINQ queries[cite: 1].
* [cite_start]**Real-Time Data Binding:** Transaction ledger histories utilize classic enterprise patterns, mapping directly from `SqlDataAdapter` to `DataTable` for memory-efficient UI rendering[cite: 1].

---

## 🤖 Machine Learning Integration

[cite_start]All financial transfers route through a local Python Flask REST API acting as a fraud detection microservice[cite: 1]. 

* [cite_start]**The Model:** Utilizes a `scikit-learn` Isolation Forest/Random Forest model, explicitly trained to identify anomalies[cite: 1]. 
* [cite_start]**The Dataset:** Trained on the industry-standard Kaggle *Credit Card Fraud Detection* dataset, encompassing over 284,000 anonymized, real-world transactions[cite: 1]. [cite_start]Data preprocessing included handling severe class imbalance via SMOTE and rigorous feature scaling[cite: 1].
* [cite_start]**Graceful Degradation:** The C# integration is designed for resilience[cite: 1]. [cite_start]If the Flask API microservice is unreachable, the transaction logic catches the exception, completes the transfer, but flags the transaction for manual review, ensuring the platform does not suffer a hard crash[cite: 1].

---

## 🛡️ Security & Enterprise Compliance

* [cite_start]**SQL Injection Prevention:** String concatenation is strictly prohibited in the DAL[cite: 1]. [cite_start]All raw ADO.NET queries utilize parameterized inputs (`SqlParameter`)[cite: 1].
* [cite_start]**Immutable Audit Logging:** Every transaction execution, successful authentication, and failed login attempt is written to an immutable AuditLog table[cite: 1].
* [cite_start]**Role-Based Access Control (RBAC):** Passwords are never stored in plaintext[cite: 1]. [cite_start]Authentication relies on BCrypt hashing [cite: 1][cite_start], with strict boundary enforcement between `Admin` capabilities and read-only `Analyst` views[cite: 1].
* [cite_start]**Global Exception Handling:** Application-level handlers catch all unhandled exceptions, writing full stack traces to local files via `Serilog` [cite: 1] [cite_start]while presenting sanitized, user-friendly dialogues to the frontend[cite: 1].

---

## 📊 Core Application Features

1.  [cite_start]**Executive Dashboard:** Real-time KPIs tracking monthly spending, total balances, and open fraud alerts[cite: 1].
2.  [cite_start]**Ledger Management:** Deposit, withdraw, and transfer capabilities with inline ML inference responses[cite: 1].
3.  [cite_start]**Visual Analytics:** Thread-safe chart rendering covering category aggregations and spending trends[cite: 1].
4.  [cite_start]**Alert Management Lifecycle:** Dedicated queues for Analysts to review flagged transactions and monitor system integrity[cite: 1].
5.  [cite_start]**Documentation Export:** One-click integration exporting historical transaction ledgers to Excel via the `ClosedXML` library[cite: 1].
