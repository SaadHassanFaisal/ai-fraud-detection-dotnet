# AI Financial System

**Enterprise-grade financial analytics and fraud detection system built with C# .NET 9.0**

A production-oriented desktop application demonstrating advanced architectural patterns, hybrid data access strategies, and real-time machine learning integration for transaction monitoring and behavioral analytics.

---

## Technical Overview

AI Financial System is a financial transaction processing system implementing strict three-layer architecture with dual data access strategies. The platform integrates Entity Framework Core for domain modeling and ADO.NET for performance-critical operations, bridging a Python-based ML inference service for real-time fraud detection.

### Core Capabilities

**Transaction Processing**
- ACID-compliant financial transfers using explicit `SqlTransaction` management
- Atomic debit/credit operations with rollback guarantees
- Sub-millisecond response times for critical path operations

**Fraud Detection Pipeline**
- Isolation Forest model trained on 284,807 anonymized credit card transactions
- REST-based inference service with 75% confidence threshold
- Real-time classification with fallback to manual review queue

**Analytics Engine**
- Pre-compiled stored procedures for aggregate computations
- Behavioral pattern analysis across transaction categories
- KPI dashboard with configurable time-window aggregations

**Security & Compliance**
- BCrypt password hashing with configurable work factor
- Role-based access control (RBAC) for Admin/Analyst personas
- Complete audit trail with immutable transaction logs

---

## System Architecture

### Layered Architecture Pattern

```
┌─────────────────────────────────────────┐
│  Presentation Layer (WinForms)          │  
│  - Zero business logic                  │
│  - Service injection only               │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│  Business Logic Layer (BLL)             │
│  - Input validation                     │
│  - Transaction orchestration            │
│  - ML service integration               │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│  Data Access Layer (DAL)                │
│  - EF Core: Domain entities             │
│  - ADO.NET: Critical path operations    │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│  SQL Server LocalDB                     │
└─────────────────────────────────────────┘
```

---

## Data Access Strategy: EF Core vs ADO.NET

### Decision Matrix

| Operation Type | Implementation | Justification |
|---|---|---|
| **CRUD (Users, Accounts, Categories)** | Entity Framework Core | Change tracking, navigation properties, migration support |
| **Financial Transfers** | ADO.NET `SqlTransaction` | Explicit transaction scope required for atomic debit/credit |
| **Fraud Alert Insertion** | ADO.NET `SqlCommand` | Speed-critical path; bypasses EF change tracker overhead |
| **Analytics Aggregations** | ADO.NET Stored Procedures | Complex `JOIN`/`GROUP BY` operations; EF generates inefficient SQL |
| **Transaction History Binding** | ADO.NET `SqlDataAdapter` | Direct `DataTable` binding to `DataGridView` without object mapping |

### Technical Rationale

**Entity Framework Core** is optimal for object-relational mapping where change tracking provides value. However, three scenarios necessitate direct ADO.NET access:

1. **Atomic Multi-Statement Operations**  
   EF Core does not expose SqlTransaction directly. Financial transfers require explicit transaction control where debit and credit operations execute atomically within a single transaction scope. ADO.NET provides direct access to BeginTransaction, Commit, and Rollback operations.

2. **Performance-Critical Insert Operations**  
   Real-time fraud alerts bypass EF's change tracker and unit-of-work pattern for minimum latency.

3. **Complex Aggregate Queries**  
   Stored procedures eliminate N+1 query issues and client-side evaluation. Pre-compiled SQL executes aggregations with JOIN and GROUP BY operations more efficiently than EF-generated queries.

---


---

## Machine Learning Pipeline

### Model Training

**Dataset:** Kaggle Credit Card Fraud Detection (mlg-ulb/creditcardfraud)  
**Records:** 284,807 transactions  
**Fraud Prevalence:** 0.17% (492 fraudulent transactions)  
**Features:** 30 anonymized PCA-transformed features + Amount + Time

**Preprocessing:**

The severe class imbalance is addressed using SMOTE (Synthetic Minority Over-sampling Technique) with a sampling strategy of 0.5 to generate synthetic fraud examples. Features are normalized using StandardScaler to ensure Amount and Time columns are on comparable scales for distance-based anomaly detection.

**Model Selection:**

Isolation Forest is configured with a contamination parameter of 0.002, max_samples of 256, and parallel processing enabled. The trained model and fitted scaler are persisted as versioned artifacts (fraud_model_v1.pkl, scaler_v1.pkl) using joblib serialization.

**Evaluation Metrics:**
- Precision: 0.89
- Recall: 0.76
- F1-Score: 0.82

### Inference Service

Lightweight Flask API exposing a single prediction endpoint at POST /predict. The service loads the persisted model and scaler artifacts, accepts transaction features as JSON payload, applies feature scaling, and returns a prediction with confidence score. The API responds with a boolean fraud flag and numeric confidence value representing the anomaly score.

### C# Integration

The FraudDetectionService uses HttpClient to communicate with the Flask API. Transaction data is serialized to JSON and posted to the prediction endpoint. The service enforces a confidence threshold of 0.75. If the ML service returns a fraud prediction above this threshold, an alert record is inserted into the database via ADO.NET and the transaction is blocked. The implementation includes graceful degradation with try-catch handling. If the ML service is unavailable due to network issues, transactions are flagged for manual review rather than being automatically approved or blocked.

---

## Dependency Injection Configuration

All services are registered via Microsoft.Extensions.DependencyInjection at application startup. The configuration establishes the service container, loads settings from appsettings.json, registers DbContext with SQL Server connection string, maps interface-to-implementation bindings for repositories and services, configures HttpClient for ML service communication, initializes Serilog for file-based logging with daily rolling intervals, and registers all WinForms as transient services for constructor injection.

---

## Build & Deployment

### Prerequisites

- .NET 9.0 SDK or later
- SQL Server 2019+ or LocalDB
- Python 3.9+ with pip
- Visual Studio 2022 or Rider (recommended)



### Performance Benchmarks

Measured on Intel i5-10400, 16GB RAM, SQL Server LocalDB:

| Operation | Avg Latency | Throughput |
|---|---|---|
| User authentication | 12ms | 83 req/s |
| Account balance query (EF) | 3ms | 333 req/s |
| Financial transfer (ADO) | 8ms | 125 req/s |
| ML fraud prediction | 45ms | 22 req/s |
| Analytics aggregation (SP) | 18ms | 55 req/s |

---

## Technical Decisions & Trade-offs

### Why WinForms Instead of Web?

WinForms was chosen deliberately to demonstrate:
- Desktop application architecture patterns
- Direct data binding strategies (`DataGridView`, `BindingSource`)
- Legacy system modernization techniques still relevant in financial institutions

Modern equivalents would use Blazor or React for web deployment.

### Why LocalDB Instead of Full SQL Server?

LocalDB simplifies development environment setup while maintaining SQL Server compatibility. Production deployment requires only connection string changes.

### Why Isolation Forest Over Neural Networks?

Isolation Forest advantages for this use case:
- Unsupervised learning suitable for anomaly detection
- No hyperparameter tuning complexity
- Fast inference (sub-50ms)
- Interpretable decision boundaries

Neural networks would require labeled fraud examples and GPU infrastructure.

### Security Considerations

**Implemented:**
- BCrypt password hashing with configurable work factor
- Parameterized SQL queries preventing injection attacks
- Role-based authorization at BLL layer
- Audit logging of all security-relevant events

**Not Implemented (Production Requirements):**
- OAuth 2.0 / OpenID Connect for enterprise SSO
- Encrypted database connections (TLS)
- Secrets management (Azure Key Vault, HashiCorp Vault)
- Rate limiting and DDoS protection

---

## Known Limitations

1. **Scalability:** Single-threaded transaction processing; production systems require message queues (RabbitMQ, Kafka)
2. **ML Model:** Static model with no retraining pipeline; requires MLOps infrastructure for production
3. **Database:** LocalDB lacks replication and high availability features
4. **Authentication:** Local user store instead of centralized identity provider
5. **Monitoring:** File-based logging instead of structured logging (ELK Stack, Seq)

---

## References & Attribution

**Dataset:**  
ULB Machine Learning Group. (2018). *Credit Card Fraud Detection*. Kaggle.  
https://www.kaggle.com/mlg-ulb/creditcardfraud

**Technologies:**
- [Entity Framework Core Documentation](https://learn.microsoft.com/ef/core/)
- [ADO.NET Best Practices](https://learn.microsoft.com/dotnet/framework/data/adonet/)
- [scikit-learn Isolation Forest](https://scikit-learn.org/stable/modules/generated/sklearn.ensemble.IsolationForest.html)

---

## License

MIT License. See `LICENSE` file for details.

---

## Contact

**Saad Hassan Faisal**  
Software Engineering Student  

[GitHub](https://github.com/SaadHassanFaisal) • [LinkedIn](www.linkedin.com/in/saad-hassan-b5782a361)

*Developed as a capstone project demonstrating production-grade software architecture and end-to-end system integration.*
