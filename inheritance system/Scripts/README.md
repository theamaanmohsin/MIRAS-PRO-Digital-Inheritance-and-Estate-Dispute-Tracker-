# MirasPro database scripts

## Fresh database (recommended)

Run **`MirasPro_FullDatabase.sql`** in SQL Server Management Studio.

| Setting | Default |
|--------|---------|
| Database | `InheritanceDB` |
| Drop existing tables | `@DropExisting = 0` by default — set to `1` only for a full wipe |

After running:

1. Confirm `appsettings.json` connection string points to your server and `InheritanceDB`.
2. Run the Blazor app: `dotnet run`
3. Register accounts at `/register`

### Default admin security key

`MirasPro@Admin2025` (stored in `SystemSettings.AdminAccessSecret`)

Use this when registering or logging in as **Administrator**.

### Optional seed admin account

Run **`MirasPro_SeedData.sql`** after the full database script:

| Field | Value |
|-------|--------|
| Email | `admin@miraspro.pk` |
| Password | `Admin@123` |
| Admin login key | `MirasPro@Admin2025` |

To generate a new password hash:  
`dotnet run --project Scripts/HashPassword/HashPassword.csproj -- "YourPassword"`

## Incremental updates only

If you already have a database and only need new columns/tables, the app runs **`DatabaseSchemaInitializer`** on startup. You can also use the older **`UpdateSchema.sql`** for manual patches.

## Word project manual

Generate a branded, interactive `.docx` from `PROJECT_MANUAL.md`:

```bash
python Scripts/build_manual_docx.py
```

Output: `MirasPro_Project_Manual.docx` (cover page, clickable Navigation Index, auto-updating Word TOC, embedded UML diagrams, MirasPro blue theme).

In Word: right-click the Table of Contents → **Update Field** → **Update entire table**.

## Tables

| Table | Purpose |
|-------|---------|
| `Users` | User and Admin accounts |
| `Properties` | Registered properties |
| `Heirs` | Heirs per property + share % |
| `InheritanceCases` | Active/closed inheritance cases |
| `Documents` | Uploaded property documents |
| `Disputes` | Dispute applications + admin review |
| `Transfers` | Property transfer requests |
| `FrcDocuments` | FRC uploads + admin approval |
| `AdminAuditLogs` | Admin action history |
| `SystemSettings` | Admin secret key and config |
| `AdminRegistrationInvites` | Optional invite tokens (legacy) |
