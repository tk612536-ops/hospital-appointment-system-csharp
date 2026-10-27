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
        if (string.IsNullOrWhiteSpace(input.FirstValue)) context.Result = ModelBindingResult.Success(null);
        else if (decimal.TryParse(input.FirstValue.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.GetCultureInfo("tr-TR"), out var fee)) context.Result = ModelBindingResult.Success(fee);
        else context.ModelState.TryAddModelError(context.ModelName, "Ücreti binlik ayırıcı olmadan, ondalık için virgül kullanarak girin (ör. 750,50).");
        return Task.CompletedTask;
    }
}
