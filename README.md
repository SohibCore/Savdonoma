# TijoratUz

**TijoratUz** is an offline-first Windows desktop application for managing everyday sales in small retail shops. It brings product management, checkout, inventory tracking, employee accounts, sales reports, Excel product import, and thermal receipt printing into one lightweight application.

The repository is named **Savdonoma**, while the desktop application is branded and built as **TijoratUz**.

<p align="center">
  <img src="Savdonoma/TijoratUzLogo.png" alt="TijoratUz logo" width="180" />
</p>

<p align="center">
  <strong>Built for practical retail workflows.</strong><br/>
  Local data storage · Fast product lookup · Simple checkout
</p>

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Repository Structure](#repository-structure)
- [Requirements](#requirements)
- [Getting Started](#getting-started)
- [Database and Data Storage](#database-and-data-storage)
- [Typical Workflow](#typical-workflow)
- [Excel Import](#excel-import)
- [Barcode Scanning](#barcode-scanning)
- [Receipt Printing](#receipt-printing)
- [Users and Roles](#users-and-roles)
- [Licensing](#licensing)
- [Publishing](#publishing)
- [Troubleshooting](#troubleshooting)
- [Security](#security)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Author](#author)

## Overview

Small shops need a checkout process that is quick, understandable, and dependable. TijoratUz is designed to help shop owners and cashiers maintain a product catalog, process sales, optionally track stock, and review sales activity without depending on a cloud service.

The application is built with **WPF and .NET 8** and stores its operational data locally in **SQLite**.

### Design Goals

- **Offline-first:** Core retail workflows use a local database.
- **Fast checkout:** Search by product name or barcode and calculate sale totals automatically.
- **Flexible inventory:** Choose whether stock quantities should be tracked.
- **Practical reporting:** Review sales and payment summaries for a selected period.
- **Maintainable architecture:** Separate desktop UI, business logic, data access, importing, and printing responsibilities.

## Features

### Product Management

- Create, update, search, and deactivate products.
- Organize products into categories.
- Store product name, unit, price, barcode, and quantity.
- Search by normalized product name, text, or exact barcode.
- Keep inactive products out of the regular active product list.

### Sales and Checkout

- Add products to a sale and specify quantities.
- Automatically calculate line totals and the overall sale amount.
- Support cash and card payment methods.
- Record received amounts and change.
- Save completed sales and their item details.
- Generate receipt data for recorded sales.

### Inventory Management

Inventory tracking can be enabled or disabled during initial setup.

When enabled:

- Maintain product quantities.
- Automatically decrease stock after a completed sale.
- Validate available stock before completing a sale.
- Prevent negative stock quantities.

When disabled, stock quantities are not used for checkout calculations.

### Sales Reports

- View sales for a selected date range.
- Review total sales amount and number of sales.
- See cash and card totals.
- Review average check amount.
- See cancelled sale counts and sale-level details.

### Employee Management

- Create and manage employee accounts.
- Assign Manager and Cashier roles.
- Associate employees with their shop organization.
- Securely hash passwords using BCrypt.

### Excel Product Import

- Import products from Excel workbooks.
- Validate rows before saving.
- Detect duplicate products.
- Validate prices, quantities, units, and category IDs.
- Review validation errors and skipped rows.
- Import valid rows after confirmation.

### Barcode Scanning

- Barcode scanner service integration.
- Product lookup using barcode values.
- Faster checkout workflows.

### Thermal Receipt Printing

- ESC/POS-compatible receipt printing.
- Shop and cashier information.
- Product names, quantities, and prices.
- Payment information and totals.
- Receipt formatting for thermal printers.

### Licensing

- Machine identification.
- License validation and activation.
- License state management.
- Restricted write operations when the license does not permit them.

## Technology Stack

| Area | Technology |
|---|---|
| Language | C# |
| Framework | .NET 8 |
| Desktop UI | WPF |
| UI Components | WPF-UI |
| MVVM | CommunityToolkit.Mvvm |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| ORM | Entity Framework Core 8 |
| Database | SQLite |
| Password Hashing | BCrypt.Net-Next |
| Excel Processing | ClosedXML |
| Receipt Printing | ESCPOS_NET |
| Installer | Inno Setup |

## Architecture

The solution is divided into separate projects with focused responsibilities.

| Project | Responsibility |
|---|---|
| `Savdonoma` | WPF application, views, ViewModels, startup, and dependency injection |
| `SavdonomaCore` | Domain entities, enums, shared logic, printer contracts, and licensing abstractions |
| `Savdonoma.Data` | EF Core, SQLite persistence, migrations, configurations, and application services |
| `Savdonoma.Import` | Excel reading, product validation, and import workflow |
| `Savdonoma.Printing` | ESC/POS receipt formatting and printer communication |
| `Savdonoma.LicenseGen` | License generation utility |
| `Installer` | Inno Setup installer configuration |

The application uses dependency injection to compose its services. Entity Framework Core handles persistence, while the desktop UI follows the MVVM pattern.

## Repository Structure

```text
Savdonoma/
├── Installer/
│   └── Savdonoma.iss
├── Savdonoma/
│   ├── BarcodeService/
│   ├── Converters/
│   ├── ViewModels/
│   ├── Views/
│   ├── App.xaml
│   ├── MainWindow.xaml
│   └── Savdonoma.csproj
├── Savdonoma.Data/
│   ├── Configurations/
│   ├── Licensing/
│   ├── Migrations/
│   ├── Services/
│   └── AppDbContext.cs
├── Savdonoma.Import/
├── Savdonoma.LicenseGen/
├── Savdonoma.Printing/
├── SavdonomaCore/
│   ├── Entity/
│   ├── Enums/
│   ├── Licensing/
│   ├── Logic/
│   └── Printer/
└── Savdonoma.sln
```

## Requirements

- Windows 10 or Windows 11.
- .NET 8 SDK.
- .NET 8 Windows Desktop runtime.
- Visual Studio 2022 with the .NET desktop development workload, or the .NET CLI.
- Optional: ESC/POS-compatible thermal receipt printer.
- Optional: compatible barcode scanner.

> TijoratUz is a Windows WPF desktop application. It is not designed to run natively on Linux or macOS.

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/SohibCore/Savdonoma.git
cd Savdonoma
```

### 2. Restore Dependencies

```bash
dotnet restore Savdonoma.sln
```

### 3. Build the Solution

```bash
dotnet build Savdonoma.sln --configuration Release
```

### 4. Run the Application

```bash
dotnet run --project Savdonoma/Savdonoma.csproj
```

Alternatively, open `Savdonoma.sln` in Visual Studio, set the `Savdonoma` project as the startup project, and press **F5**.

On the first launch, the application creates its local data directory, applies Entity Framework Core migrations, seeds initial data, and proceeds through activation, authentication, and initial shop setup.

## Database and Data Storage

TijoratUz uses SQLite for local persistence.

Default database location:

```text
%APPDATA%\Savdonoma\savdonoma.db
```

Usually:

```text
C:\Users\<username>\AppData\Roaming\Savdonoma\savdonoma.db
```

The application automatically creates the directory and database when needed.

On startup, it:

1. Configures SQLite.
2. Enables write-ahead logging (WAL).
3. Applies pending EF Core migrations.
4. Seeds initial database data.

**Back up the database regularly.** Close the application before manually copying the database file.

## Typical Workflow

1. Start TijoratUz and activate the application if required.
2. Sign in with an authorized account.
3. Complete initial shop setup.
4. Choose whether inventory tracking should be enabled.
5. Create categories and products or import products from Excel.
6. Use the sales screen to search for products and complete checkout.
7. Print receipts using a compatible thermal printer.
8. Review sales reports.
9. Create employee accounts when additional staff need access.

Available actions depend on the signed-in user's role and license state.

## Excel Import

A sample import workbook is included:

```text
Savdonoma/wwwroot/default import file.xlsx
```

The import service supports product information such as:

- Product name
- Price
- Unit
- Quantity
- Category ID
- Barcode

Supported unit labels include:

- `dona`
- `kg`
- `litr`
- `metr`
- `quti`

The importer validates the workbook before committing records to the database. Review the validation results before confirming an import.

## Barcode Scanning

TijoratUz includes a barcode scanner abstraction and barcode-based product lookup.

For best results:

- Configure the scanner according to its manufacturer's instructions.
- Confirm that it sends the complete barcode value.
- Ensure the stored barcode matches the scanned value.
- Test the scanner with a few products before using it during live checkout.

## Receipt Printing

Receipt printing is implemented in `Savdonoma.Printing` using `ESCPOS_NET`.

To configure printing:

1. Connect and install the receipt printer in Windows.
2. Confirm that it supports ESC/POS commands.
3. Configure the correct printer name.
4. Test receipt alignment, paper width, and character rendering.

The current application startup registers a printer named `Xprinter XP-80C`. If you use another model, update the printer configuration accordingly.

## Users and Roles

| Role | Description |
|---|---|
| `Boshliq` | Shop owner |
| `Manager` | Manager |
| `Sotuvchi` | Cashier / salesperson |

Employee management is intended for the shop owner. Access to operations is also controlled through service-level authorization checks.

## Licensing

The solution includes a licensing subsystem in:

- `SavdonomaCore/Licensing`
- `Savdonoma.Data/Licensing`
- `Savdonoma.LicenseGen`

The application checks the current license state during startup and may require activation before normal use.

Write operations are guarded by the license service.

Do not distribute private signing material or embed secrets in the desktop application.

## Publishing

To create a self-contained Windows x64 Release build:

```bash
dotnet publish Savdonoma/Savdonoma.csproj --configuration Release --runtime win-x64 --self-contained true
```

Publish output:

```text
Savdonoma/bin/Release/net8.0-windows/win-x64/publish/
```

The repository includes an Inno Setup script:

```text
Installer/Savdonoma.iss
```

Review local paths and installer settings before building the installer on another development machine.

Test the published application on a clean Windows machine before distributing it.

## Troubleshooting

### Application Does Not Start

- Verify the Windows and .NET Desktop Runtime requirements.
- Run the application from Visual Studio or a terminal.
- Check permissions for the application data directory.

### SQLite Database Is Locked

- Close other TijoratUz instances.
- Close database browser tools accessing the same database.
- Avoid copying or editing the database while the application is running.
- Restart after the lock is released.

### Insufficient Stock

If inventory tracking is enabled, the requested quantity cannot exceed available stock. Update the product quantity and retry.

### Barcode Does Not Find a Product

- Verify the saved barcode.
- Compare it with scanner output.
- Check scanner input mode and suffix settings.

### Receipt Does Not Print

- Verify printer installation and connection.
- Check the configured printer name.
- Confirm ESC/POS compatibility and paper availability.

### Excel Import Errors

Check product names, numeric prices, supported units, non-negative quantities, valid category IDs, and duplicate names.

## Security

- Passwords are hashed using BCrypt.
- Operational data is stored locally.
- Keep Windows user accounts and file permissions secure.
- Back up the database to a protected location.
- Never commit license signing keys, passwords, private certificates, or real customer data.
- A local database is not a substitute for encrypted backups or centralized access control.

## Roadmap

Potential future improvements:

- [ ] Add a screenshot gallery and product demo.
- [ ] Expand automated unit and integration test coverage.
- [ ] Add more printer configuration options.
- [ ] Improve import preview and downloadable error reports.
- [ ] Add configurable receipt layouts and shop branding.
- [ ] Document backup and restore procedures.
- [ ] Add more localization options.

## Contributing

Contributions, bug reports, and suggestions are welcome.

1. Fork the repository.
2. Create a feature branch:

```bash
git checkout -b feature/your-feature
```

3. Make your changes and build the solution.
4. Test the affected workflow on Windows.
5. Open a pull request with a clear description and relevant screenshots.

For larger changes, open an issue first to discuss the proposed implementation.

## License

No license file is currently included in this repository. Unless a license is added, the source code should be treated as **all rights reserved**. Do not assume redistribution, modification, or commercial use is permitted.

## Author

**Sohib Bekmurodov**

- GitHub: [@SohibCore](https://github.com/SohibCore)
- Repository: [SohibCore/Savdonoma](https://github.com/SohibCore/Savdonoma)

---

<p align="center">
  Made with C# and .NET for everyday retail.
</p>
