namespace ImpactoTextil.Api.Services;

public enum CodigoErrorAdmin
{
    Validacion,
    PermisoDenegado,
    NoEncontrado,
    ConflictoEstado,
    Concurrencia
}

public sealed record ErrorOperacionAdmin(
    CodigoErrorAdmin codigo,
    string mensaje,
    string? campo = null);

public sealed class ResultadoOperacionAdmin<T>
{
    public bool EsExito => Error is null;
    public T? Valor { get; }
    public ErrorOperacionAdmin? Error { get; }

    private ResultadoOperacionAdmin(T? valor, ErrorOperacionAdmin? error)
    {
        Valor = valor;
        Error = error;
    }

    public static ResultadoOperacionAdmin<T> Exito(T valor) => new(valor, null);

    public static ResultadoOperacionAdmin<T> Fallo(CodigoErrorAdmin codigo, string mensaje, string? campo = null) =>
        new(default, new ErrorOperacionAdmin(codigo, mensaje, campo));
}
