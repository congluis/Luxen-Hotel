using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;

namespace LuxenHotel.Helpers.Assets
{
    public static class ScriptHelper
    {
        public static IHtmlContent LibScript(this IHtmlHelper htmlHelper, string relativePath)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext);
            var path = urlHelper.Content($"~/lib/{relativePath}");
            return new HtmlString($"<script src=\"{path}\"></script>");
        }

        public static IHtmlContent JsScript(this IHtmlHelper htmlHelper, string relativePath)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext);
            var path = urlHelper.Content($"~/js/{relativePath}");
            return new HtmlString($"<script src=\"{path}\"></script>");
        }

        public static IHtmlContent CdnScript(this IHtmlHelper htmlHelper, string cdnUrl)
        {
            return new HtmlString($"<script src=\"{cdnUrl}\"></script>");
        }
    }
}
