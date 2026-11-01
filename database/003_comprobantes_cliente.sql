BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101194643_HuellaComprobanteCliente'
)
BEGIN
    ALTER TABLE [Pago] ADD [huellaComprobante] varbinary(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101194643_HuellaComprobanteCliente'
)
BEGIN
    EXEC(N'ALTER TABLE [Pago] ADD CONSTRAINT [CK_Pago_huellaComprobante] CHECK (([huellaComprobante] IS NULL OR DATALENGTH([huellaComprobante]) = 32))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101194643_HuellaComprobanteCliente'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101194643_HuellaComprobanteCliente', N'10.0.14');
END;

COMMIT;
GO
