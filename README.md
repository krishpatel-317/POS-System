# 🛒 Enterprise POS & Retail Management System

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-blue?logo=dotnet)](https://learn.microsoft.com/aspnet/core)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-10.0-blueviolet?logo=nuget)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/Database-SQL_Server_LocalDB-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
[![Bootstrap](https://img.shields.io/badge/Frontend-Bootstrap_5-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A robust, full-featured **Point of Sale (POS) and Retail Management System** built with **ASP.NET Core MVC** and **Entity Framework Core**. Engineered for retail counters, superstores, and commercial outlets, it streamlines staff operations, inventory tracking, customer relationships, and real-time transaction processing.

---

## 🌟 Key Features

### 🛍️ 1. Interactive POS Billing Counter
- **Dynamic Cart Interface:** Add in-stock products, adjust quantities dynamically with `+` and `-` controls, and preview itemized subtotals.
- **Automated Inventory Synchronization:** Automatically decrements stock quantities in SQL Server upon sale completion.
- **Line Item History:** Creates atomic `Sale` and `SaleItem` records capturing unit price, purchase quantity, and subtotal at the moment of checkout.
- **Multiple Settlement Channels:** Integrated with strongly-typed payment channels (`Cash`, `Card`, `UPI`) linked 1-to-1 with each sale.

### 📦 2. Catalog & Inventory Management
- **Departmental Categorization:** Group items into structured aisles and categories with real-time product counts.
- **Product Search & Filtering:** Dynamic LINQ-powered filtering by product name and category dropdown without page reloads.
- **Inventory Telemetry:** Real-time stock status badges (🟢 In Stock, 🟡 Low Stock $\le 5$, 🔴 Out of Stock).

### 👥 3. Staff & Customer Directories
- **Role-Based Security:** Predefined permissions enforced via C# Enums (`Admin`, `Cashier`, `Manager`) preventing typographical errors in business logic.
- **Customer CRM:** Record shopper contact details and track lifetime purchasing history.
- **Walk-in Support:** Supports anonymous walk-in purchases without mandatory customer registration.

### 📊 4. Executive Analytics Dashboard
- **Revenue Calculation:** Computes today's real-time earnings directly from database timestamps.
- **Catalog Overview:** High-level metrics for active products, departments, and registered patrons.
- **Operational Feeds:** Recent 5 transactions feed and automated low-stock reordering warnings.

### 🛡️ 5. Relational Integrity & Safety Guards
- **Protected Category Deletion:** Blocks deletion if child products exist in the department.
- **Historical Sales Protection:** Blocks product deletion if linked to past transaction receipts, safeguarding accounting history.

---

## 🏗️ Architecture & Technology Stack

| Tier | Technologies |
|---|---|
| **Backend Framework** | ASP.NET Core 10.0 (C# 13) |
| **Architectural Pattern** | Model-View-Controller (MVC) |
| **ORM & Data Access** | Entity Framework Core 10.0 (Code-First Migrations, Fluent API) |
| **Database** | Microsoft SQL Server (LocalDB / Express) |
| **Front-End & UI** | Razor Views (`.cshtml`), HTML5, CSS3, JavaScript / jQuery |
| **Styling & Icons** | Custom Warm Fintech Theme, Bootstrap 5, FontAwesome 6, Plus Jakarta Sans Typography |

---

## 🗄️ Database Entity-Relationship (ER) Schema

The database model consists of **8 normalized relational entities** configured via `ApplicationDbContext`:

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│      Role       │ 1   * │      User       │ 1   * │      Sale       │
├─────────────────┤───────├─────────────────┤───────├─────────────────┤
│ RoleId (PK)     │       │ UserId (PK)     │       │ SaleId (PK)     │
│ Name            │       │ Name            │       │ SaleDate        │
└─────────────────┘       │ Username        │       │ TotalAmount     │
                          │ Password        │       │ CustomerId (FK) │
                          │ RoleId (FK)     │       │ UserId (FK)     │
                          └─────────────────┘       └────────┬────────┘
                                                             │ 1
┌─────────────────┐       ┌─────────────────┐                │
│    Customer     │ 1   * │    SaleItem     │ *            1 │
├─────────────────┤───────├─────────────────┤────────────────┤
│ CustomerId (PK) │       │ SaleItemId (PK) │                │
│ Name            │       │ SaleId (FK)     │                │
│ Phone           │       │ ProductId (FK)  │                │
│ Email           │       │ Quantity        │       ┌────────┴────────┐
└─────────────────┘       │ UnitPrice       │       │     Payment     │
                          │ TotalPrice      │       ├─────────────────┤
                          └────────┬────────┘       │ PaymentId (PK)  │
                                   │ *              │ Amount          │
┌─────────────────┐       ┌────────┴────────┐       │ PaymentMethod   │
│    Category     │ 1   * │     Product     │ 1     │ PaymentDate     │
├─────────────────┤───────├─────────────────┤       │ SaleId (FK, UQ) │
│ CategoryId (PK) │───────│ ProductId (PK)  │       └─────────────────┘
│ Name            │       │ Name            │
└─────────────────┘       │ Price           │
                          │ StockQuantity   │
                          │ CategoryId (FK) │
                          └─────────────────┘
```

---

## 📂 Project Structure

```text
POSSystem/
├── Controllers/
│   ├── HomeController.cs        # Executive telemetry dashboard
│   ├── CategoryController.cs    # Product category CRUD operations
│   ├── ProductController.cs     # Inventory management & stock search
│   ├── CustomerController.cs    # Shopper records management
│   ├── RoleController.cs        # Role definition & Enum management
│   ├── UserController.cs        # Staff accounts & credential handling
│   └── SaleController.cs        # POS billing cart, checkout & receipt details
├── Models/
│   ├── Category.cs              # Category entity
│   ├── Product.cs               # Product entity
│   ├── Customer.cs              # Customer entity
│   ├── Role.cs                  # Security role entity
│   ├── User.cs                  # User/Staff entity
│   ├── Sale.cs                  # Bill header entity
│   ├── SaleItem.cs              # Line item junction entity
│   ├── Payment.cs               # Financial settlement entity
│   └── Enums.cs                 # UserRole & PaymentMethodType enums
├── Data/
│   └── ApplicationDbContext.cs  # EF Core context & Fluent API configuration
├── Views/
│   ├── Home/                    # Dashboard views
│   ├── Category/                # Department management views
│   ├── Product/                 # Product catalog views
│   ├── Customer/                # Customer directory views
│   ├── Role/                    # Role management views
│   ├── User/                    # Staff management views
│   ├── Sale/                    # Interactive cart & receipt details
│   └── Shared/
│       ├── _Layout.cshtml       # Responsive fintech master layout
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/                     # Static assets (CSS, JS, Fonts, Images)
├── Program.cs                   # Pipeline configuration, auto-migration & seeding
└── appsettings.json             # Database connection strings
```

---

## 🚀 Getting Started

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or higher
* [Visual Studio 2022 / 2026](https://visualstudio.microsoft.com/) (with *ASP.NET and web development* workload) or VS Code
* SQL Server LocalDB (installed automatically with Visual Studio)

### Installation & Run

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/krishpatel-317/POS-System.git
   cd POS-System/POSSystem
   ```

2. **Verify Database Connection:**
   Open `appsettings.json` and ensure the connection string points to your local SQL Server instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=POSSystemDb;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

3. **Restore Dependencies & Build:**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Launch the Application:**
   ```bash
   dotnet run
   ```
   *Note: On initial startup, `Program.cs` automatically executes pending EF Core migrations and seeds foundational roles (`Admin`, `Cashier`, `Manager`).*

5. **Navigate in Browser:**
   Open [https://localhost:7257](https://localhost:7257) or [http://localhost:5241](http://localhost:5241).

---

## 👨‍💻 Author

**Krish Patel**
* GitHub: [@krishpatel-317](https://github.com/krishpatel-317)
