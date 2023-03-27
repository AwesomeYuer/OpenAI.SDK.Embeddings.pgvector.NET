using Npgsql.Internal;
using Npgsql.Internal.TypeHandling;
using PgVectors.NET;
namespace PgVectors.Npgsql;

public class VectorTypeHandlerResolverFactory : TypeHandlerResolverFactory
{
    public override TypeHandlerResolver Create(NpgsqlConnector connector)
        => new VectorTypeHandlerResolver(connector);

    public override string? GetDataTypeNameByClrType(Type type)
        => VectorTypeHandlerResolver.ClrTypeToDataTypeName(type);

    public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
        => VectorTypeHandlerResolver.DoGetMappingByDataTypeName(dataTypeName);
}
