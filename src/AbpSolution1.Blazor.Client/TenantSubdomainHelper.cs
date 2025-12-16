using System;
using System.Linq;

namespace AbpSolution1.Blazor.Client;

public static class TenantSubdomainHelper
{
    private static readonly string[] ProtocolPrefixes = { "http://", "https://" };

    /// <summary>
    /// Convierte una URL de configuración con placeholder {0} reemplazándolo con el subdominio del tenant actual
    /// </summary>
    public static string ConvertToTenantSubDomain(string baseUrl, string configUrl)
    {
        if (string.IsNullOrEmpty(configUrl))
            return configUrl;

        return configUrl.Replace("{0}.", GetTenantNamePrefix(baseUrl));
    }

    /// <summary>
    /// Obtiene el prefijo del tenant desde la URL base (ej: "tenant1." o "" si no hay tenant)
    /// </summary>
    private static string GetTenantNamePrefix(string baseUrl)
    {
        var hostName = baseUrl.RemovePreFix(ProtocolPrefixes);
        var host = hostName.TrimEnd('/').Split('/')[0];
        var parts = host.Split('.');

        // Si tiene más de 2 partes (tenant.localhost:44366 o tenant.midominio.com)
        // el primer segmento es el tenant
        return parts.Length > 2 ? $"{parts[0]}." : string.Empty;
    }

    private static string RemovePreFix(this string str, params string[] prefixes)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        foreach (var prefix in prefixes)
        {
            if (str.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(prefix.Length);
            }
        }

        return str;
    }
}
