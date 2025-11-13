using System.Text;

namespace EFCore.ExprGenerator;

internal class GenerateDtoClassInfo
{
    public required DtoStructure Structure { get; set; }

    public required string Accessibility { get; set; }

    public required string ClassName { get; set; }

    public required string Namespace { get; set; }

    public string FullName => $"{Namespace}.{ClassName}";

    public string BuildCode()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Accessibility} class {ClassName}");
        sb.AppendLine("{");

        foreach (var prop in Structure.Properties)
        {
            var propertyType = prop.TypeName;

            // For nested structures, recursively generate DTOs (add first)
            if (prop.NestedStructure is not null)
            {
                // Extract the base type (e.g., IEnumerable from IEnumerable<T>)
                var baseType = propertyType;
                if (propertyType.Contains("<"))
                {
                    baseType = propertyType[..propertyType.IndexOf("<")];
                }

                var nestedDtoName = prop.NestedStructure.SourceTypeName;
                propertyType = $"{baseType}<{nestedDtoName}>";
            }
            sb.AppendLine($"    public required {propertyType} {prop.Name} {{ get; set; }}");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }
}
