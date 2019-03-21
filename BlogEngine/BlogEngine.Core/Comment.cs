#region Using

using System;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;

#endregion

namespace BlogEngine.Core
{
	/// <summary>
	/// Represents a comment to a blog post.
	/// </summary>
	[Serializable]
	public sealed class Comment : IComparable<Comment>, IPublishable
	{

		#region Properties

		private Guid _Id;
		/// <summary>
		/// The Id of the comment.
		/// </summary>
		public Guid Id
		{
			get { return _Id; }
			set { _Id = value; }
		}

		private string _Author;
		/// <summary>
		/// Gets or sets the author.
		/// </summary>
		/// <value>The author.</value>
		public string Author
		{
			get { return _Author; }
			set { _Author = value; }
		}

		private string _Email;
		/// <summary>
		/// Gets or sets the email.
		/// </summary>
		/// <value>The email.</value>
		public string Email
		{
			get { return _Email; }
			set { _Email = value; }
		}

		private Uri _Website;

		/// <summary>
		/// Gets or sets the website.
		/// </summary>
		/// <value>The website.</value>
		public Uri Website
		{
			get { return _Website; }
			set { _Website = value; }
		}

		private string _Content;
		/// <summary>
		/// Gets or sets the content.
		/// </summary>
		/// <value>The content.</value>
		public string Content
		{
			get { return _Content; }
			set { _Content = value; }
		}

		private string _Country;
		/// <summary>
		/// Gets or sets the country.
		/// </summary>
		/// <value>The country.</value>
		public string Country
		{
			get { return _Country; }
			set { _Country = value; }
		}

		private string _IP;
		/// <summary>
		/// Gets or sets the IP address.
		/// </summary>
		/// <value>The IP.</value>
		public string IP
		{
			get { return _IP; }
			set { _IP = value; }
		}

		private IPublishable _Post;

		/// <summary>
		/// 
		/// </summary>
		public IPublishable Parent
		{
			get { return _Post; }
			set { _Post = value; }
		}

		private bool _IsApproved;
		/// <summary>
		/// Gets or sets the Comment approval status
		/// </summary>
		public bool IsApproved
		{
			get { return _IsApproved; }
			set { _IsApproved = value; }
		}

		/// <summary>
		/// Gets whether or not this comment should be shown
		/// </summary>
		/// <value></value>
		public bool IsVisible
		{
			get { return IsApproved || IsApproved; }
		}

		private DateTime _DateCreated = DateTime.MinValue;
		/// <summary>
		/// Gets or sets when the comment was created.
		/// </summary>
		public DateTime DateCreated
		{
			get
			{
				if (_DateCreated == DateTime.MinValue)
					return _DateCreated;

				return _DateCreated.AddHours(BlogSettings.Instance.Timezone);

			}
			set { _DateCreated = value; }
		}

		DateTime IPublishable.DateModified
		{
			get { return DateCreated; }
		}

		/// <summary>
		/// Gets the title of the object
		/// </summary>
		/// <value></value>
		public String Title
		{
			get { return Author + " on " + Parent.Title; }
		}

		/// <summary>
		/// Gets the relative link of the comment.
		/// </summary>
		/// <value>The relative link.</value>
		public string RelativeLink
		{
			get { return Parent.RelativeLink.ToString() + "#id_" + Id; }
		}

		/// <summary>
		/// Gets the absolute link.
		/// </summary>
		/// <value>The absolute link.</value>
		public Uri AbsoluteLink
		{
			get { return new Uri(Parent.AbsoluteLink + "#id_" + Id); }
		}

		/// <summary>
		/// Gets the description. Returns always string.empty.
		/// </summary>
		/// <value>The description.</value>
		public String Description
		{
			get { return string.Empty; }
		}

		StateList<Category> IPublishable.Categories
		{
			get { return null; }
		}

		#endregion

		#region IComparable<Comment> Members

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the 
		/// objects being compared. The return value has the following meanings: 
		/// Value Meaning Less than zero This object is less than the other parameter.
		/// Zero This object is equal to other. Greater than zero This object is greater than other.
		/// </returns>
		public int CompareTo(Comment other)
		{
			return this.DateCreated.CompareTo(other.DateCreated);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the post is being served to the output stream.
		/// </summary>
		public static event EventHandler<ServingEventArgs> Serving;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		public static void OnServing(Comment comment, ServingEventArgs arg)
		{
			if (Serving != null)
			{
				Serving(comment, arg);
			}
		}

		/// <summary>
		/// Raises the Serving event
		/// </summary>
		public void OnServing(ServingEventArgs eventArgs)
		{
			if (Serving != null)
			{
				Serving(this, eventArgs);
			}
		}

		/// <summary>
		/// Occurs just before a comment is approved by the comment moderator.
		/// </summary>
		public static event EventHandler<CancelEventArgs> Approving;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		internal static void OnApproving(Comment comment, CancelEventArgs e)
		{
			if (Approving != null)
			{
				Approving(comment, e);
			}
		}

		/// <summary>
		/// Occurs after a comment is approved by the comment moderator.
		/// </summary>
		public static event EventHandler<EventArgs> Approved;
		/// <summary>
		/// Raises the event in a safe way
		/// </summary>
		internal static void OnApproved(Comment comment)
		{
			if (Approved != null)
			{
				Approved(comment, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Occurs when the page is being attacked by robot spam.
		/// </summary>
		public static event EventHandler<EventArgs> SpamAttack;
		/// <summary>
		/// Raises the SpamAttack event in a safe way
		/// </summary>
		public static void OnSpamAttack()
		{
			if (SpamAttack != null)
			{
				SpamAttack(null, new EventArgs());
			}
		}

		#endregion
	}
}
