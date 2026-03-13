using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;

namespace LuxenHotel.Helpers.Assets
{
    public static class StyleHelper
    {
        public static IHtmlContent LibStyle(this IHtmlHelper htmlHelper, string relativePath)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext);
            var path = urlHelper.Content($"~/lib/{relativePath}");
            return new HtmlString($"<link rel=\"stylesheet\" href=\"{path}\" />");
        }

        public static IHtmlContent CssStyle(this IHtmlHelper htmlHelper, string relativePath)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext);
            var path = urlHelper.Content($"~/css/{relativePath}");
            return new HtmlString($"<link rel=\"stylesheet\" href=\"{path}\" />");
        }

        public static IHtmlContent CdnCss(this IHtmlHelper htmlHelper, string cdnUrl)
        {
            return new HtmlString($"<link rel=\"stylesheet\" href=\"{cdnUrl}\" />");
        }

        public static IHtmlContent Favicon(this IHtmlHelper htmlHelper, string path, string size = "64x64")
        {
            return new HtmlString($"<link rel=\"icon\" href=\"{path}\" sizes=\"{size}\" type=\"image/png\" />");
        }
    }
}
