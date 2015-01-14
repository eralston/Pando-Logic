using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

using PandoLogic.Controllers;

namespace PandoLogic
{
    /// <summary>
    /// Extension method for the HtmlHelper object, used in CSHTML files
    /// </summary>
    public static class HtmlHelpers
    {
        /// <summary>
        /// Gets the Google analytics key from the app settings section of the web.config file
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static string GoogleAnalyticsKey(this HtmlHelper helper)
        {
            return ConfigurationManager.AppSettings["google-analytics-key"];
        }

        /// <summary>
        /// Gets the Google analytics key from the app settings section of the web.config file
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static string StripePublishableKey(this HtmlHelper helper)
        {
            return ConfigurationManager.AppSettings["StripeApiPublishableKey"];
        }

        /// <summary>
        /// Returns true if the system currently has context for a company; otherwise, returns false
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <returns></returns>
        public static bool IsInCompanyContext(this HtmlHelper htmlHelper)
        {
            BaseController controller = htmlHelper.ViewContext.Controller as BaseController;

            if (controller == null)
                return false;

            return controller.UserCache.SelectedCompanyName != null;
        }

        public static IHtmlString RemoveLink(this HtmlHelper htmlHelper, string linkText, string container, string deleteElement)
        {
            var js = string.Format("javascript:removeNestedForm(this,'{0}','{1}');return false;", container, deleteElement);
            TagBuilder tb = new TagBuilder("a");
            tb.Attributes.Add("href", "#");
            tb.Attributes.Add("onclick", js);
            tb.Attributes.Add("class", "remove-link");
            tb.InnerHtml = linkText;
            var tag = tb.ToString(TagRenderMode.Normal);
            return MvcHtmlString.Create(tag);
        }

        public static IHtmlString AddLink<TModel>(this HtmlHelper<TModel> htmlHelper,
                                                        string linkText,
                                                        string containerElement,
                                                        string counterElement,
                                                        string collectionProperty,
                                                        Type nestedType,
                                                        string cssClasses = "")
        {
            var ticks = DateTime.UtcNow.Ticks;
            var nestedObject = Activator.CreateInstance(nestedType);
            var partial = htmlHelper.EditorFor(x => nestedObject).ToHtmlString().JsEncode();
            partial = partial.Replace("id=\\\"nestedObject", "id=\\\"" + collectionProperty + "_" + ticks + "_");
            partial = partial.Replace("name=\\\"nestedObject", "name=\\\"" + collectionProperty + "[" + ticks + "]");
            var js = string.Format("javascript:addNestedForm('{0}','{1}','{2}','{3}');return false;", containerElement, counterElement, ticks, partial);
            TagBuilder tb = new TagBuilder("a");
            tb.Attributes.Add("href", "#");
            tb.Attributes.Add("onclick", js);
            tb.Attributes.Add("class", cssClasses);
            tb.InnerHtml = linkText;
            var tag = tb.ToString(TagRenderMode.Normal);
            return MvcHtmlString.Create(tag);
        }

        private static string JsEncode(this string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int i;
            int len = s.Length;
            StringBuilder sb = new StringBuilder(len + 4);
            string t;
            for (i = 0; i < len; i += 1)
            {
                char c = s[i];
                switch (c)
                {
                    case '>':
                    case '"':
                    case '\\':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        break;
                    default:
                        if (c < ' ')
                        {
                            string tmp = new string(c, 1);
                            t = "000" + int.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
                            sb.Append("\\u" + t.Substring(t.Length - 4));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}