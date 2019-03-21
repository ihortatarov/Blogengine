#region Using

using System;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using System.Globalization;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Web.Controls
{
	/// <summary>
	/// The PostView.ascx that is located in the themes folder
	/// has to inherit from this class. 
	/// <remarks>
	/// It provides the basic functionaly needed to display a post.
	/// </remarks>
	/// </summary>
	public class PostViewBase : UserControl
	{
		/// <summary>
		/// Lets process our .Body content and build up our controls collection
		/// inside the 'BodyContent' placeholder.
		/// 
		/// User controls are insterted into the blog in the following format..
		/// [UserControl:~/path/usercontrol.ascx]
		/// 
		/// TODO : Expose user control parameters.
		/// 
		/// </summary>
		protected void Page_Load(object sender, EventArgs e)
		{
			// Used to track where we are in the 'Body' as we parse it.
			int currentPosition = 0;
			string content = Body;
			PlaceHolder bodyContent = (PlaceHolder)FindControl("BodyContent");

			if (bodyContent != null)
			{
				MatchCollection myMatches = _BodyRegex.Matches(content);

				foreach (Match myMatch in myMatches)
				{
					// Add literal for content before custom tag should it exist.
					if (myMatch.Index > currentPosition)
					{
						bodyContent.Controls.Add(new LiteralControl(content.Substring(currentPosition, myMatch.Index - currentPosition)));
					}

					// Now lets add our user control.
					try
					{
						string all = myMatch.Groups[1].Value.Trim();
						UserControl usercontrol = null;

						if (!all.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase))
						{
							int index = all.IndexOf(".ascx", StringComparison.OrdinalIgnoreCase) + 5;
							usercontrol = (UserControl)LoadControl(all.Substring(0, index));

							string parameters = Server.HtmlDecode(all.Substring(index));
							Type type = usercontrol.GetType();
							string[] paramCollection = parameters.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

							foreach (string param in paramCollection)
							{
								string name = param.Split('=')[0].Trim();
								string value = param.Split('=')[1].Trim();
								System.Reflection.PropertyInfo property = type.GetProperty(name);
								property.SetValue(usercontrol, Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture), null);
							}
						}
						else
						{
							usercontrol = (UserControl)LoadControl(all);
						}

						bodyContent.Controls.Add(usercontrol);

						// Now we will update our position.
						//currentPosition = myMatch.Index + myMatch.Groups[0].Length;
					}
					catch (Exception)
					{
						// Whoopss, can't load that control so lets output something that tells the developer that theres a problem.
						bodyContent.Controls.Add(new LiteralControl("ERROR - UNABLE TO LOAD CONTROL : " + myMatch.Groups[1].Value));
					}

					currentPosition = myMatch.Index + myMatch.Groups[0].Length;
				}

				// Finally we add any trailing static text.
				bodyContent.Controls.Add(new LiteralControl(content.Substring(currentPosition, content.Length - currentPosition)));
			}
			else
			{
				// We have no placeholder so we assume this is an old style <% =Body %> theme and do nothing.
			}
		}

		private static readonly Regex _BodyRegex = new Regex(@"\[UserControl:(.*?)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// Shows the post if it isn\t published.
		/// </summary>
		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);
			if (!Post.IsVisible && !Page.User.Identity.IsAuthenticated)
			{
				this.Visible = false;
			}
		}

		/// <summary>
		/// The Post object that is displayed through the PostView.ascx control.
		/// </summary>
		/// <value>The Post object that has to be displayed.</value>
		public virtual Post Post
		{
			get { return (Post)(ViewState["Post"] ?? default(Post)); }
			set { ViewState["Post"] = value; }
		}

		private ServingLocation _Location = ServingLocation.None;
		/// <summary>
		/// The location where the serving takes place.
		/// </summary>
		public ServingLocation Location
		{
			get { return _Location; }
			set { _Location = value; }
		}

		private bool _ShowExcerpt;
		/// <summary>
		/// Gets or sets whether or not to show the entire post or just the excerpt/description.
		/// </summary>
		public bool ShowExcerpt
		{
			get { return _ShowExcerpt; }
			set { _ShowExcerpt = value; }
		}

		/// <summary>
		/// Gets the body of the post. Important: use this instead of Post.Content.
		/// </summary>
		public string Body
		{
			get
			{
				string body = Post.Content;
				if (ShowExcerpt)
				{
					string link = " <a href=\"" + Post.RelativeLink.ToString() + "\">[" + (Page as BlogBasePage).Translate("more") + "]</a>";

					if (!string.IsNullOrEmpty(Post.Description))
					{
						body = Post.Description + link;
					}
					else
					{
						body = Utils.StripHtml(Post.Content);
						if (body.Length > 300)
							body = body.Substring(0, 300) + "..." + link;
					}
				}

				ServingEventArgs arg = new ServingEventArgs(body, this.Location);
				Post.OnServing(Post, arg);

				if (arg.Cancel)
				{
					if (arg.Location == ServingLocation.SinglePost)
					{
						Response.Redirect("~/error404.aspx", true);
					}
					else
					{
						this.Visible = false;
					}
				}

				return arg.Body ?? string.Empty;
			}
		}

		/// <summary>
		/// Gets the comment feed link.
		/// </summary>
		/// <value>The comment feed.</value>
		public string CommentFeed
		{
			get { return Utils.RelativeWebRoot + "syndication.axd?post=" + Post.Id; }
		}

		#region Protected methods

		/// <summary>
		/// Displays the Post's categories seperated by the specified string.
		/// </summary>
		protected virtual string CategoryLinks(string separator)
		{
			string[] keywords = new string[Post.Categories.Count];
			string link = "<a href=\"{0}{1}.aspx\">{2}</a>";
			string path = VirtualPathUtility.ToAbsolute("~/category/");
			for (int i = 0; i < Post.Categories.Count; i++)
			{
				if (Category.Categories.Contains((Category)Post.Categories[i]))
				{
					string category = Category.GetCategory(Post.Categories[i].Id).Title;
					keywords[i] = string.Format(CultureInfo.InvariantCulture, link, path, Utils.RemoveIllegalCharacters(category), category);
				}
			}


			return string.Join(separator, keywords);
		}

		/// <summary>
		/// Displays the Post's tags seperated by the specified string.
		/// </summary>
		protected virtual string TagLinks(string separator)
		{
			if (Post.Tags.Count == 0)
				return null;

			string[] tags = new string[Post.Tags.Count];
			string link = "<a href=\"{0}/{1}\" rel=\"tag\">{2}</a>";
			string path = Utils.RelativeWebRoot + "?tag=";
			for (int i = 0; i < Post.Tags.Count; i++)
			{
				string tag = Post.Tags[i];
				tags[i] = string.Format(CultureInfo.InvariantCulture, link, path, HttpUtility.UrlEncode(tag), HttpUtility.HtmlEncode(tag));
			}

			return string.Join(separator, tags);
		}

		/// <summary>
		/// Displays an Edit and Delete link to any 
		/// authenticated user.
		/// </summary>
		protected virtual string AdminLinks
		{
			get
			{
				if (Page.User.IsInRole(BlogSettings.Instance.AdministratorRole) || Page.User.Identity.Name.Equals(Post.Author))
				{
					BlogBasePage page = (BlogBasePage)Page;
					string confirmDelete = string.Format(CultureInfo.InvariantCulture, page.Translate("areYouSure"), page.Translate("delete").ToLowerInvariant(), page.Translate("thePost"));
					StringBuilder sb = new StringBuilder();

					if (Post.NotApprovedComments.Count > 0)
					{
						sb.AppendFormat(CultureInfo.InvariantCulture, "<a href=\"{0}\">{1} ({2})</a> | ", Post.RelativeLink, page.Translate("unapprovedcomments"), Post.NotApprovedComments.Count);
						sb.AppendFormat(CultureInfo.InvariantCulture, "<a href=\"{0}\">{1}</a> | ", Post.RelativeLink + "?approveallcomments=true", page.Translate("approveallcomments"));

					}

					sb.AppendFormat(CultureInfo.InvariantCulture, "<a href=\"{0}\">{1}</a> | ", Utils.AbsoluteWebRoot + "admin/Pages/Add_entry.aspx?id=" + Post.Id.ToString(), page.Translate("edit"));
					sb.AppendFormat(CultureInfo.InvariantCulture, "<a href=\"javascript:void(0);\" onclick=\"if (confirm('{2}')) location.href='{0}?deletepost={1}'\">{3}</a> | ", Post.RelativeLink, Post.Id.ToString(), confirmDelete, page.Translate("delete"));
					return sb.ToString();
				}

				return string.Empty;
			}
		}

		/// <summary>
		/// Enable visitors to rate the post.
		/// </summary>
		protected virtual string Rating
		{
			get
			{
				if (!BlogSettings.Instance.EnableRating)
					return string.Empty;

				float rating = Post.Rating / 5 * 100;
				StringBuilder sb = new StringBuilder();
				sb.Append("<div class=\"rating\">");

				BlogBasePage page = (BlogBasePage)Page;

				if (Post.Raters > 0)
					sb.AppendFormat("<p>" + page.Translate("currentlyRated") + "</p>", Post.Rating.ToString("#.0", CultureInfo.InvariantCulture), Post.Raters);
				else
					sb.Append("<p>" + page.Translate("beTheFirstToRate") + "</p>");

				string script = "void(Rate('{0}', {1}))";

				sb.Append("<ul class=\"star-rating small-star\">");
				sb.AppendFormat("<li class=\"current-rating\" style=\"width:{0}%\">Currently {1}/5 Stars.</li>", Math.Round(rating, 0), Post.Rating);
				sb.AppendFormat("<li><a href=\"javascript:" + script + "\" rev=\"vote-against\" title=\"Rate this 1 star out of 5\" class=\"one-star\">1</a></li>", Post.Id.ToString(), 1);
				sb.AppendFormat("<li><a href=\"javascript:" + script + "\" rev=\"vote-against\" title=\"Rate this 2 stars out of 5\" class=\"two-stars\">2</a></li>", Post.Id.ToString(), 2);
				sb.AppendFormat("<li><a href=\"javascript:" + script + "\" rev=\"vote-abstain\" title=\"Rate this 3 stars out of 5\" class=\"three-stars\">3</a></li>", Post.Id.ToString(), 3);
				sb.AppendFormat("<li><a href=\"javascript:" + script + "\" rev=\"vote-for\" title=\"Rate this 4 stars out of 5\" class=\"four-stars\">4</a></li>", Post.Id.ToString(), 4);
				sb.AppendFormat("<li><a href=\"javascript:" + script + "\" rev=\"vote-for\" title=\"Rate this 5 stars out of 5\" class=\"five-stars\">5</a></li>", Post.Id.ToString(), 5);
				sb.Append("</ul>");
				sb.Append("</div>");
				return sb.ToString();
			}
		}

		#endregion
	}
}