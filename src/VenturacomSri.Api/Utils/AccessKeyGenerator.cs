namespace VenturacomSri.Api.Utils;

/// <summary>
/// Generación de Clave de Acceso SRI Ecuador (49 dígitos).
///
/// Estructura:
///   [0..7]   fechaEmision     8 dígitos  ddmmaaaa
///   [8..9]   tipoComprobante  2 dígitos  01=factura
///   [10..22] ruc             13 dígitos
///   [23]     ambiente         1 dígito   1=pruebas, 2=producción
///   [24..29] serie            6 dígitos  estab(3)+ptoEmi(3)
///   [30..38] secuencial       9 dígitos
///   [39..46] codigoNumerico   8 dígitos  (aleatorio)
///   [47]     tipoEmision      1 dígito   1=normal
///   [48]     digitoVerificador           (módulo 11)
/// </summary>
public static class AccessKeyGenerator
{
    public static string Generate(
        DateTime issueDate,
        string ruc,
        int environment,
        string establishment,
        string emissionPoint,
        string sequential,
        string documentType = "01",
        string? numericCode = null)
    {
        var fecha = issueDate.ToString("ddMMyyyy");              // 8 dígitos
        var tipo  = documentType.PadLeft(2, '0');                // 2 dígitos
        var rucPad = ruc.PadLeft(13, '0');                       // 13 dígitos
        var amb   = environment.ToString();                      // 1 dígito
        var serie = establishment.PadLeft(3, '0')
                  + emissionPoint.PadLeft(3, '0');               // 6 dígitos
        var seq   = sequential.PadLeft(9, '0');                  // 9 dígitos

        var numCode = numericCode != null
            ? numericCode.PadLeft(8, '0')
            : Random.Shared.Next(0, 99_999_999).ToString().PadLeft(8, '0'); // 8 dígitos

        var tipoEmision = "1";                                   // 1 dígito

        var baseKey = $"{fecha}{tipo}{rucPad}{amb}{serie}{seq}{numCode}{tipoEmision}";
        // baseKey = 48 dígitos
        var check = ComputeCheckDigit(baseKey);

        return $"{baseKey}{check}";
    }

    private static int ComputeCheckDigit(string digits)
    {
        // Módulo 11 con pesos 2..7 (cíclico de derecha a izquierda)
        int sum = 0;
        int weight = 2;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            sum += int.Parse(digits[i].ToString()) * weight;
            weight = weight == 7 ? 2 : weight + 1;
        }
        int remainder = sum % 11;
        return remainder switch
        {
            0 => 0,
            1 => 1,
            _ => 11 - remainder
        };
    }

    public static string FormatNumber(string establishment, string emissionPoint, string sequential)
        => $"{establishment.PadLeft(3, '0')}-{emissionPoint.PadLeft(3, '0')}-{sequential.PadLeft(9, '0')}";
}
