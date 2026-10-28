using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HospitalAppointments.Web.Models;

/// <summary>Turkish decimal input without implicit thousands grouping or rounding.</summary>
public sealed class TryFeeModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var input = context.ValueProvider.GetValue(context.ModelName);
        if (input == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, input);
        var raw = input.FirstValue?.Trim();
        // Check before parsing: decimal.TryParse can round excess fractional digits to a valid fee.
        var separator = raw?.IndexOf(',') ?? -1;
        var exactPrecision = separator < 0 || raw!.Length - separator - 1 is 1 or 2;
        if (string.IsNullOrEmpty(raw)) context.Result = ModelBindingResult.Success(null);
        else if (exactPrecision && decimal.TryParse(raw, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.GetCultureInfo("tr-TR"), out var fee)) context.Result = ModelBindingResult.Success(fee);
        else context.ModelState.TryAddModelError(context.ModelName, "Ücreti binlik ayırıcı olmadan, virgülle ve en fazla iki ondalık basamakla girin (ör. 750,50).");
        return Task.CompletedTask;
    }
}
