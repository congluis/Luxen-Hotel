using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuxenHotel.Helpers.Assets
{
    public static class PartialHelper
    {
        public static Task<IHtmlContent> PartialAsyncFromArea(
            this IHtmlHelper htmlHelper,
            string areaName,
            string relativePartialPath)
        {
            var fullPath = $"~/Areas/{areaName}/Views/Shared/{relativePartialPath}.cshtml";
            return htmlHelper.PartialAsync(fullPath);
        }

        public static IHtmlContent? RenderIf(this IHtmlHelper htmlHelper, bool condition, Func<IHtmlContent> content)
        {
            return condition ? content() : HtmlString.Empty;
        }
    }
}
