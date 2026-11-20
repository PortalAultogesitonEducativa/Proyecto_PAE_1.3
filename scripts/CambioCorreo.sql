-- Cambio de correo con confirmación por enlace (RF-03.2)
-- Ejecutar sobre la base de datos GestionAcademica. Se puede ejecutar varias veces sin problema.
-- Nota: el sistema también crea esta tabla automáticamente la primera vez que se usa el módulo.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GU_CAMBIO_CORREO')
BEGIN
    CREATE TABLE [dbo].[GU_CAMBIO_CORREO](
        [id_cambio]          [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [id_usuario]         [int]           NOT NULL,
        [correo_anterior]    [nvarchar](150) NOT NULL,
        [correo_nuevo]       [nvarchar](150) NOT NULL,
        [token_hash]         [nvarchar](64)  NOT NULL,
        [fecha_solicitud]    [datetime]      NOT NULL,
        [fecha_expiracion]   [datetime]      NOT NULL,
        [usado]              [bit]           NOT NULL DEFAULT 0,
        [fecha_confirmacion] [datetime]      NULL
    );

    CREATE UNIQUE INDEX UX_CAMBIO_CORREO_token   ON [dbo].[GU_CAMBIO_CORREO]([token_hash]);
    CREATE INDEX        IX_CAMBIO_CORREO_usuario ON [dbo].[GU_CAMBIO_CORREO]([id_usuario], [usado]);
END
GO
