#region Using

using System;
using System.Globalization;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Threading;

#endregion

namespace BlogEngine.Core.Web.Controls
{
	/// <summary>
	/// All pages in the custom themes as well as pre-defined pages in the root
	/// must inherit from this class.
	/// </summary>
	/// <remarks>
	/// The class is responsible for assigning the theme to all
	/// derived pages as well as adding RSS, RSD, tracking script
	/// and a whole lot more.
	/// </remarks>
	public abstract class BlogBasePage : System.Web.UI.Page
	{

		private string _Theme = BlogSettings.Instance.Theme;
		/// <summary>
		/// Assignes the selected theme to the pages.
		/// </summary>
		protected override void OnPreInit(EventArgs e)
		{			
			if (Request.QueryString["theme"] != null)
				_Theme = Request.QueryString["theme"];

			MasterPageFile = Utils.RelativeWebRoot + "themes/" + _Theme + "/site.master";

			base.OnPreInit(e);

			if (!Page.IsPostBack && !string.IsNullOrEmpty(Request.QueryString["deletepost"]))
			{
				if (Page.User.Identity.IsAuthenticated)
				{
					Post post = BlogEngine.Core.Post.GetPost(new Guid(Request.QueryString["deletepost"]));
					if (Page.User.IsInRole(BlogSettings.Instance.AdministratorRole) || Page.User.Identity.Name == post.Author)
					{
						post.Delete();
						post.Save();
						Response.Redirect(Utils.RelativeWebRoot);
					}
				}
			}
		}

		/// <summary>
		/// Adds links and javascript to the HTML header tag.
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (!Page.IsCallback && !Page.IsPostBack)
			{
				// Links
				AddGenericLink("contents", "Archive", Utils.RelativeWebRoot + "archive.aspx");
				AddGenericLink("start", BlogSettings.Instance.Name, Utils.RelativeWebRoot);
				AddGenericLink("application/rdf+xml", "meta", "SIOC", Utils.AbsoluteWebRoot + "sioc.axd");
				AddGenericLink("application/apml+xml", "meta", "APML", Utils.AbsoluteWebRoot + "apml.axd");
				AddGenericLink("application/rdf+xml", "meta", "FOAF", Utils.AbsoluteWebRoot + "foaf.axd");

				if (string.IsNullOrEmpty(BlogSettings.Instance.AlternateFeedUrl))
				{
					AddGenericLink("application/rss+xml", "alternate", BlogSettings.Instance.Name + " (RSS)", Utils.FeedUrl + "?format=rss");
					AddGenericLink("application/atom+xml", "alternate", BlogSettings.Instance.Name + " (ATOM)", Utils.FeedUrl + "?format=atom");
				}
				else
				{
					AddGenericLink("application/rss+xml", "alternate", BlogSettings.Instance.Name, Utils.FeedUrl);
				}				
				
				AddGenericLink("application/rsd+xml", "edituri", "RSD", Utils.AbsoluteWebRoot + "rsd.axd");

				AddMetaContentType();
				AddLocalizationKeys();

				if (BlogSettings.Instance.EnableOpenSearch)
					AddGenericLink("application/opensearchdescription+xml", "search", BlogSettings.Instance.Name, Utils.AbsoluteWebRoot + "opensearch.axd");

				if (!string.IsNullOrEmpty(BlogSettings.Instance.HtmlHeader))
					AddCustomCodeToHead();

				if (!string.IsNullOrEmpty(BlogSettings.Instance.TrackingScript))
					AddTrackingScript();
			}

			AddJavaScriptInclude(Utils.RelativeWebRoot + "blog.js");

			if (User.IsInRole(BlogSettings.Instance.AdministratorRole))
			{
				AddJavaScriptInclude(Utils.RelativeWebRoot + "admin/widget.js");
				AddStylesheetInclude(Utils.RelativeWebRoot + "admin/widget.css");
			}

			if (BlogSettings.Instance.RemoveWhitespaceInStyleSheets)
				CompressCss();
		}

		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.Page.PreRenderComplete"></see> event after 
		/// the <see cref="M:System.Web.UI.Page.OnPreRenderComplete(System.EventArgs)"></see> event and before the page is rendered.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		protected override void OnPreRenderComplete(EventArgs e)
		{
			base.OnPreRenderComplete(e);
			if (BlogSettings.Instance.UseBlogNameInPageTitles)
			{
				Page.Title = (BlogSettings.Instance.Name + " | " + Page.Title);
			}
		}

		/// <summary>
		/// Adds the localization keys to JavaScript for use globally.
		/// </summary>
		protected virtual void AddLocalizationKeys()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("KEYhasRated='{0}';", Translate("youAlreadyRated").Replace("'", "\\'"));
			sb.AppendFormat("KEYwebRoot='{0}';", Utils.RelativeWebRoot);
			sb.AppendFormat("KEYsavingTheComment='{0}';", Translate("savingTheComment").Replace("'", "\\'"));
			sb.AppendFormat("KEYcomments='{0}';", Translate("comments").Replace("'", "\\'"));
			sb.AppendFormat("KEYcommentWasSaved='{0}';", Translate("commentWasSaved").Replace("'", "\\'"));
			sb.AppendFormat("KEYcommentWaitingModeration='{0}';", Translate("commentWaitingModeration").Replace("'", "\\'"));
			sb.AppendFormat("KEYcancel='{0}';", Translate("cancel").Replace("'", "\\'"));
			sb.AppendFormat("KEYfilter='{0}';", Translate("filter").Replace("'", "\\'"));
			sb.AppendFormat("KEYapmlDescription='{0}';", Translate("filterByApmlDescription").Replace("'", "\\'"));

			HtmlGenericControl script = new HtmlGenericControl("script");
			script.Attributes.Add("type", "text/javascript");
			script.InnerHtml = sb.ToString();
			Page.Header.Controls.Add(script);
		}

		/// <summary>
		/// Finds all stylesheets in the header and changes the 
		/// path so it points to css.axd which removes the whitespace.
		/// </summary>
		protected virtual void CompressCss()
		{
			if (Request.QueryString["theme"] != null)
				return;

			string version = "&v=" + BlogSettings.Instance.Version();

			foreach (Control control in Page.Header.Controls)
			{
				HtmlControl c = control as HtmlControl;
				if (c != null && c.Attributes["type"] != null && c.Attributes["type"].Equals("text/css", StringComparison.OrdinalIgnoreCase))
				{
					if (!c.Attributes["href"].StartsWith("http://"))
					{
						c.Attributes["href"] = Utils.RelativeWebRoot + "themes/" + BlogSettings.Instance.Theme + "/css.axd?name=" + c.Attributes["href"] + version;
						c.EnableViewState = false;
					}
				}
			}
		}

		/// <summary>
		/// Adds the content-type meta tag to the header.
		/// </summary>
		protected virtual void AddMetaContentType()
		{
			HtmlMeta meta = new HtmlMeta();
			meta.HttpEquiv = "content-type";
			meta.Content = Response.ContentType + "; charset=" + Response.ContentEncoding.HeaderName;
			Page.Header.Controls.Add(meta);
		}

		/// <summary>
		/// Add a meta tag to the page's header.
		/// </summary>
		protected virtual void AddMetaTag(string name, string value)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
				return;

			HtmlMeta meta = new HtmlMeta();
			meta.Name = name;
			meta.Content = value;
			Page.Header.Controls.Add(meta);
		}

		/// <summary>
		/// Adds the generic link to the header.
		/// </summary>
		public virtual void AddGenericLink(string relation, string title, string href)
		{
			HtmlLink link = new HtmlLink();
			link.Attributes["rel"] = relation;
			link.Attributes["title"] = title;
			link.Attributes["href"] = href;
			Page.Header.Controls.Add(link);
		}

		/// <summary>
		/// Adds the generic link to the header.
		/// </summary>
		public virtual void AddGenericLink(string type, string relation, string title, string href)
		{
			HtmlLink link = new HtmlLink();
			link.Attributes["type"] = type;
			link.Attributes["rel"] = relation;
			link.Attributes["title"] = title;
			link.Attributes["href"] = href;
			Page.Header.Controls.Add(link);
		}

		/// <summary>
		/// Adds a JavaScript reference to the HTML head tag.
		/// </summary>
		public virtual void AddJavaScriptInclude(string url)
		{
			HtmlGenericControl script = new HtmlGenericControl("script");
			script.Attributes["type"] = "text/javascript";
			script.Attributes["src"] = ResolveScriptUrl(url);
			Page.Header.Controls.Add(script);
		}

		/// <summary>
		/// Adds a Stylesheet reference to the HTML head tag.
		/// </summary>
		/// <param name="url">The relative URL.</param>
		public virtual void AddStylesheetInclude(string url)
		{
			HtmlLink link = new HtmlLink();
			link.Attributes["type"] = "text/css";
			link.Attributes["href"] = url;
			link.Attributes["rel"] = "stylesheet";
			Page.Header.Controls.Add(link);
		}

		/// <summary>
		/// Resolves the script URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns></returns>
		public virtual string ResolveScriptUrl(string url)
		{
			return Utils.RelativeWebRoot + "js.axd?path=" + HttpUtility.UrlEncode(url) + "&v=" + BlogSettings.Instance.Version();
		}

		/// <summary>
		/// Adds a JavaScript to the bottom of the page at runtime.    
		/// </summary>
		/// <remarks>
		/// You must add the script tags to the BlogSettings.Instance.TrackingScript.
		/// </remarks>
		protected virtual void AddTrackingScript()
		{
			ClientScript.RegisterStartupScript(this.GetType(), "tracking", "\n" + BlogSettings.Instance.TrackingScript, false);
		}

		/// <summary>
		/// Adds code to the HTML head section.
		/// </summary>
		protected virtual void AddCustomCodeToHead()
		{
			string code = string.Format(CultureInfo.InvariantCulture, "{0}<!-- Start custom code -->{0}{1}{0}<!-- End custom code -->{0}", Environment.NewLine, BlogSettings.Instance.HtmlHeader);
			LiteralControl control = new LiteralControl(code);
			Page.Header.Controls.Add(control);
		}

		/// <summary>
		/// Translates the specified string using the resource files
		/// </summary>
		public virtual string Translate(string text)
		{
			try
			{
				return this.GetGlobalResourceObject("labels", text).ToString();
			}
			catch (NullReferenceException)
			{
				return text;
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.TemplateControl.Error"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		protected override void OnError(EventArgs e)
		{
			HttpContext ctx = HttpContext.Current;
			Exception exception = ctx.Server.GetLastError();

			if (exception != null && exception.Message.Contains("callback"))
			{
				// This is a robot spam attack so we send it a 404 status to make it go away.
				ctx.Response.StatusCode = 404;
				ctx.Server.ClearError();
				Comment.OnSpamAttack();
			}

			base.OnError(e);
		}

		/// <summary>
		/// Initializes the <see cref="T:System.Web.UI.HtmlTextWriter"></see> object and calls on the child
		/// controls of the <see cref="T:System.Web.UI.Page"></see> to render.
		/// </summary>
		/// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter"></see> that receives the page content.</param>
		protected override void Render(HtmlTextWriter writer)
		{
			//if (BlogSettings.Instance.RemoveWhitespaceInPages)
			//{
			//  using (HtmlTextWriter htmlwriter = new HtmlTextWriter(new System.IO.StringWriter()))
			//  {
			//    base.Render(new RewriteFormHtmlTextWriter(htmlwriter));
			//    string html = htmlwriter.InnerWriter.ToString();
			//    html = Utils.RemoveHtmlWhitespace(html);
			//    writer.Write(html);
			//  }
			//}
			//else
			//{
				base.Render(new RewriteFormHtmlTextWriter(writer));
			//}
		}

	}
}