using FastMember;
using Npgsql;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public static class ExtensionMethods
{
    public static void MapTo<T>(this NpgsqlDataReader dr, T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var accessor = TypeAccessor.Create(entity.GetType());
        var props = accessor.GetMembers()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < dr.FieldCount; i++)
        {
            var prop = props.FirstOrDefault(x =>
                x.Equals(dr.GetName(i), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(prop))
            {
                accessor[entity, prop] = dr.IsDBNull(i) ? null : dr.GetValue(i);
            }
        }
    }
}
