/*
  MirasPro — Optional seed data (run AFTER MirasPro_FullDatabase.sql)
  Default admin account for first login without using /register
*/
USE [InheritanceDB];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@miraspro.pk')
BEGIN
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, Phone, CreatedAt, IsEditable)
    VALUES (
        N'System Administrator',
        N'admin@miraspro.pk',
        N'$2a$11$6fWsyDju7AAgDhpVgv52vuo2K7ZfcOstgKgOPU6limLO.qt/u2l6a',
        N'Admin',
        N'+92 300 0000000',
        SYSUTCDATETIME(),
        1
    );
    PRINT 'Seeded admin: admin@miraspro.pk / Admin@123';
END
ELSE
    PRINT 'Admin user already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE [Key] = N'AdminAccessSecret')
BEGIN
    INSERT INTO dbo.SystemSettings ([Key], [Value], UpdatedAt)
    VALUES (N'AdminAccessSecret', N'MirasPro@Admin2025', SYSUTCDATETIME());
END
GO

PRINT 'Seed complete. Login: admin@miraspro.pk  Password: Admin@123  Admin key: MirasPro@Admin2025';
GO
