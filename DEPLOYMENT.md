# GymGuide Deployment & Operations Manual

This document provides complete instructions for deploying the **GymGuide** fitness application on **Internet Information Services (IIS)** with **SQL Server**, fulfilling requirement **3.5 G** (Audace's deployment responsibility).

---

## 1. Prerequisites on the Windows Server / IIS Host
1. **.NET 8.0 ASP.NET Core Hosting Bundle**:
   - Download and install the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0).
   - Run `iisreset` in elevated Command Prompt after installation.
2. **IIS Features Enabled**:
   - Web Server (IIS) > World Wide Web Services > Common HTTP Features, Application Development Features (WebSocket, .NET Extensibility, etc.).
3. **SQL Server & Database**:
   - SQL Server 2019/2022 or SQL Server Express running on the server.

---

## 2. Database Preparation
1. Open SQL Server Management Studio (SSMS).
2. Execute `Script.sql` to generate the `GymGuideDB` database schema and all 12 tables + ASP.NET Identity tables.
3. Execute `seed_data.sql` to populate initial courses, exercises, routines, and membership plans.
4. Verify connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=GymGuideDB;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

---

## 3. Application Publishing
From the project folder, run:
```powershell
dotnet publish -c Release -o "C:\inetpub\wwwroot\GymGuide"
```

---

## 4. IIS Configuration
1. Open **Internet Information Services (IIS) Manager** (`inetmgr`).
2. **Application Pool**:
   - Create a new App Pool named `GymGuidePool`.
   - Set **.NET CLR Version** to **"No Managed Code"**.
   - Set **Managed Pipeline Mode** to **"Integrated"**.
   - In Advanced Settings, ensure Identity has permissions to read/write the publish folder.
3. **Add Website / Application**:
   - Site Name: `GymGuide`
   - Application Pool: `GymGuidePool`
   - Physical Path: `C:\inetpub\wwwroot\GymGuide`
   - Binding: Port 80 (HTTP) or Port 443 with an SSL certificate (HTTPS).
4. Start the website and browse to `https://localhost` or `http://localhost`.

---

## 5. Daily Database Backup Schedule (SQL Server)
Execute the following T-SQL to configure a scheduled backup:
```sql
USE [msdb];
GO
-- Daily full database backup script
BACKUP DATABASE [GymGuideDB]
TO DISK = N'C:\SQLBackups\GymGuideDB_Daily.bak'
WITH INIT, FORMAT, MEDIANAME = 'GymGuideBackups', NAME = 'Full Backup of GymGuideDB';
GO
```

---

## 6. Default Administrative & Test Accounts
- **Admin**: `admin@gymguide.test` / `Admin@12345`
- **Trainer 1 (Build Muscle)**: `john.trainer@gymguide.test` / `Trainer@12345`
- **Trainer 2 (Lose Weight)**: `sarah.trainer@gymguide.test` / `Trainer@12345`
- **Trainer 3 (Stay Fit)**: `mike.trainer@gymguide.test` / `Trainer@12345`
