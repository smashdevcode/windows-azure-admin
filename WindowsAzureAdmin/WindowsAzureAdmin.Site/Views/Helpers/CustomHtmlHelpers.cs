using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WindowsAzureAdmin.Site.Views.Helpers
{
	public class GridColumn<TModel> where TModel : class
	{
		public string Header { get; set; }
		public Func<TModel, MvcHtmlString> Value { get; set; }
	}
	public static class CustomHtmlHelpers
	{
		#region RenderDetails
		public static MvcHtmlString RenderDetails<TModel>(this HtmlHelper html,
			TModel item,
			string fieldSize = "input-xxlarge") where TModel : class
		{
			var url = new UrlHelper(html.ViewContext.RequestContext);
			var propertiesToDisplay = GetDisplayProperties<TModel>();
			var sb = new StringBuilder();

			sb.AppendLine("<form class=\"form-horizontal\">");
			sb.AppendLine("<fieldset>");
			sb.AppendLine("<legend>Details</legend>");

			foreach (var property in propertiesToDisplay)
			{
				sb.AppendLine("<div class=\"control-group\">");
				sb.AppendFormat("<label class=\"control-label\" for=\"input{0}\">{0}</label>", property.Name);
				sb.AppendLine("<div class=\"controls\">");
				sb.AppendFormat("<input type=\"text\" id=\"input{0}\" value=\"{1}\" class=\"{2} uneditable-input\"/>",
					property.Name,
					html.AttributeEncode(property.GetValue(item, null)),
					fieldSize);
				sb.AppendLine("</div>");
				sb.AppendLine("</div>");
			}

			sb.AppendLine("</fieldset>");
			sb.AppendLine("</form>");

			return MvcHtmlString.Create(sb.ToString());
		}
		#endregion
		#region RenderGrid
		public static MvcHtmlString RenderGrid<TModel>(this HtmlHelper html,
			string idPrefix,
			IEnumerable<TModel> items,
			List<string> propertyNamesToIgnore = null,
			string header = null,
			string message = null,
			List<GridColumn<TModel>> extraColumns = null,
			Func<TModel, string> detailsAction = null,
			Func<TModel, string> otherAction = null,
			string otherActionText = null,
			Func<TModel, string> deleteAction = null,
			string deleteActionText = "Delete",
			Func<TModel, string> additionalRowAttributes = null) where TModel : class
		{
			// enumerate the items
			var itemsList = items.ToList();

			// if we don't any items to display return an empty string
			if (itemsList.Count == 0)
				return MvcHtmlString.Empty;

			var url = new UrlHelper(html.ViewContext.RequestContext);
			var propertiesToDisplay = GetDisplayProperties<TModel>(propertyNamesToIgnore);
			var sb = new StringBuilder();

			if (!string.IsNullOrEmpty(header))
			{
				sb.AppendLine("<form>");
				sb.AppendLine("<fieldset>");
				sb.AppendFormat("<legend>{0}</legend{1}", header, Environment.NewLine);
				sb.AppendLine("</fieldset>");
				sb.AppendLine("</form>");
			}

			if (!string.IsNullOrEmpty(message))
				sb.AppendFormat("<p>{0}</p>{1}", message, Environment.NewLine);

			sb.AppendLine("<table class=\"table table-striped table-bordered table-hover table-condensed\">");

			sb.AppendLine("<thead>");
			sb.AppendLine("<tr>");
			if (detailsAction != null || otherAction != null || deleteAction != null)
				sb.AppendLine("<th style=\"width: 150px\"></th>");
			foreach (var property in propertiesToDisplay)
				sb.AppendFormat("<th>{0}</th>{1}", property.Name, Environment.NewLine);
			if (extraColumns != null)
			{
				foreach (var column in extraColumns)
					sb.AppendFormat("<th>{0}</th>{1}", column.Header, Environment.NewLine);
			}
			sb.AppendLine("</tr>");
			sb.AppendLine("</thead>");

			sb.AppendLine("<tbody>");
			foreach (var item in itemsList)
			{
				var rowAttributes = string.Empty;
				if (additionalRowAttributes != null)
					rowAttributes = additionalRowAttributes(item);
				sb.AppendFormat("<tr{0}>{1}", !string.IsNullOrEmpty(rowAttributes) ? " " + rowAttributes : string.Empty, Environment.NewLine);
				if (detailsAction != null || otherAction != null || deleteAction != null)
				{
					sb.Append("<td>");
					if (detailsAction != null)
						sb.AppendFormat("<a href=\"{0}\" class=\"btn btn-info btn-mini\">Details</a>&nbsp;",
							html.AttributeEncode(detailsAction(item)));
					if (otherAction != null)
					{
						if (otherActionText == null)
							throw new ApplicationException("Please provide a value for the otherActionText parameter.");
						var otherActionLink = otherAction(item);
						sb.AppendFormat("<a id=\"{2}\" href=\"{0}\" class=\"btn btn-info btn-mini\">{1}</a>&nbsp;",
							html.AttributeEncode(otherActionLink), otherActionText,
							string.Format("{0}otherlink{1}", idPrefix, itemsList.IndexOf(item)));
					}
					if (deleteAction != null)
						sb.AppendFormat("<a href=\"{0}\" class=\"btn btn-danger btn-mini\">{1}</a>&nbsp;",
							html.AttributeEncode(deleteAction(item)), deleteActionText);
					sb.Append("</td>");
				}
				foreach (var property in propertiesToDisplay)
					sb.AppendFormat("<td>{0}</td>{1}", property.GetValue(item, null), Environment.NewLine);
				if (extraColumns != null)
				{
					foreach (var column in extraColumns)
						sb.AppendFormat("<td>{0}</td>{1}", column.Value(item), Environment.NewLine);
				}
				sb.AppendLine("</tr>");
			}
			sb.AppendLine("</tbody>");

			sb.AppendLine("</table>");

			return MvcHtmlString.Create(sb.ToString());
		}
		private static List<PropertyInfo> GetDisplayProperties<T>(List<string> propertyNamesToIgnore = null)
		{
			var properties = typeof(T).GetProperties();
			var propertiesToDisplay = new List<System.Reflection.PropertyInfo>();
			foreach (var property in properties)
			{
				if (propertyNamesToIgnore != null)
				{
					if (propertyNamesToIgnore.Contains(property.Name))
						continue;
				}
				if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
					propertiesToDisplay.Add(property);
			}
			return propertiesToDisplay;
		}
		#endregion
		#region RenderSubMenu
		public static MvcHtmlString RenderSubMenu(this HtmlHelper html, string controllerName)
		{
			var url = new UrlHelper(html.ViewContext.RequestContext);
			var sb = new StringBuilder();
			switch (controllerName)
			{
				case "MediaServices":
					sb.AppendLine("<ul class=\"nav nav-pills\">");
					sb.AppendLine(GetSubMenuItem(html, url, "Assets", "Assets", controllerName));
					sb.AppendLine(GetSubMenuItem(html, url, "Content Keys", "ContentKeys", controllerName));
					sb.AppendLine(GetSubMenuItem(html, url, "Files", "Files", controllerName));
					sb.AppendLine(GetSubMenuItem(html, url, "Jobs", "Jobs", controllerName));
					sb.AppendLine(GetSubMenuItem(html, url, "Locators", "Locators", controllerName));
					sb.AppendLine(GetSubMenuItem(html, url, "Media Processors", "MediaProcessors", controllerName));
					sb.AppendLine("</ul>");
					break;
				case "Storage":
					sb.AppendLine("<ul class=\"nav nav-pills\">");
					sb.AppendLine(GetSubMenuItem(html, url, "Blobs", "Blobs", controllerName));
					sb.AppendLine(GetSubMenuItem(html, url, "Containers", "Containers", controllerName));
					sb.AppendLine("</ul>");
					break;
				default:
					throw new ApplicationException("Unexpected controller name: " + controllerName);
			}
			return MvcHtmlString.Create(sb.ToString());
		}
		private static string GetSubMenuItem(HtmlHelper html, UrlHelper url, string linkText, string actionName, string controllerName)
		{
			string currentAction = html.ViewContext.RouteData.GetRequiredString("action");
			return string.Format("<li{2}><a href=\"{0}\">{1}</a></li>",
				html.AttributeEncode(url.Action(actionName, controllerName)),
				linkText,
				actionName == currentAction ? " class=\"active\"" : string.Empty);
		}
		#endregion
		#region MenuItem
		public static MvcHtmlString MenuItem(this HtmlHelper html, string linkText, string actionName, string controllerName)
		{
			var url = new UrlHelper(html.ViewContext.RequestContext);
			string currentController = html.ViewContext.RouteData.GetRequiredString("controller");

			var anchorBuilder = new TagBuilder("a");
			anchorBuilder.Attributes.Add("href", url.Action(actionName, controllerName));
			anchorBuilder.InnerHtml = linkText;

			var lineItemBuilder = new TagBuilder("li");
			if (controllerName == currentController)
				lineItemBuilder.AddCssClass("active");
			lineItemBuilder.InnerHtml = anchorBuilder.ToString();

			return MvcHtmlString.Create(lineItemBuilder.ToString());
		}
		#endregion
	}
}